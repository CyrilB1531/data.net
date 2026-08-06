using DataNet.Internal.Persistence;

namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// The normalization a SentencePiece model carries in its
/// <c>precompiled_charsmap</c>: a longest-match rewrite of the input, applied
/// before the text is segmented.
/// </summary>
/// <remarks>
/// <para>
/// Matches <c>sentencepiece</c>'s <c>Normalizer</c>. Every built-in rule —
/// <c>nmt_nfkc</c> (the <c>spm_train</c> default), <c>nfkc</c>,
/// <c>nmt_nfkc_cf</c>, <c>nfkc_cf</c> — and any custom
/// <c>--normalization_rule_tsv</c> compiles to this one blob, so interpreting it
/// covers all of them. <c>normalizer_spec.name</c> is only a record of which rule
/// produced the map; nothing here trusts it.
/// </para>
/// <para>
/// Interpreting the blob rather than reimplementing the named rules is what makes
/// parity possible at all: the map is frozen at the Unicode version of the
/// <c>sentencepiece</c> build that compiled it, while <c>string.Normalize</c>
/// follows the runtime's ICU. On <c>nmt_nfkc</c> the two already disagree on 181
/// of the 149 251 assigned code points, and that number grows with every Unicode
/// release. See <c>docs/decisions/0014-precompiled-normalizer.md</c>.
/// </para>
/// <para>
/// The blob is a darts-clone double-array trie over UTF-8 bytes followed by the
/// NUL-terminated replacement strings its values index. Both are read with the
/// same suspicion as the rest of a downloaded file: every traversal is bounds
/// checked, and a malformed map raises <see cref="InvalidDataException"/> rather
/// than reading past the end or looping.
/// </para>
/// <para>Thread-safe: immutable after construction.</para>
/// </remarks>
public sealed class PrecompiledNormalizer : IEquatable<PrecompiledNormalizer>
{
    // darts-clone packs each 32-bit unit as: bit 8 has_leaf, bit 9 offset scale,
    // bits 0-7 label (plus bit 31), bits 10-31 offset.
    private const uint HasLeafBit = 1u << 8;
    private const uint OffsetScaleBit = 1u << 9;
    private const uint LabelMask = 0x8000_00FFu;
    private const uint ValueMask = 0x7FFF_FFFFu;

    private readonly byte[] _charsMap;
    private readonly uint[] _trie;
    private readonly int _replacementsAt;

    private PrecompiledNormalizer(byte[] charsMap, uint[] trie, int replacementsAt)
    {
        _charsMap = charsMap;
        _trie = trie;
        _replacementsAt = replacementsAt;
    }

    /// <summary>Reads a normalizer from a <c>precompiled_charsmap</c>.</summary>
    /// <param name="charsMap">
    /// The blob, as carried by <c>normalizer_spec.precompiled_charsmap</c> in a
    /// <c>spiece.model</c> or, base64-encoded, by a <c>Precompiled</c> normalizer
    /// in a <c>tokenizer.json</c>.
    /// </param>
    /// <exception cref="InvalidDataException">The blob is truncated or its trie is malformed.</exception>
    public static PrecompiledNormalizer FromCharsMap(byte[] charsMap)
    {
        Guard.NotNull(charsMap);

        // 4-byte little-endian trie size, the trie, then the replacements.
        if (charsMap.Length < sizeof(uint))
        {
            throw new InvalidDataException(
                $"The precompiled charsmap is {charsMap.Length} bytes, too short to carry even its own trie size.");
        }
        uint trieBytes = (uint)charsMap[0] | ((uint)charsMap[1] << 8) | ((uint)charsMap[2] << 16) | ((uint)charsMap[3] << 24);
        if (trieBytes % sizeof(uint) != 0)
        {
            throw new InvalidDataException(
                $"The precompiled charsmap declares a {trieBytes}-byte trie, which is not a whole number of 4-byte units.");
        }
        long replacementsAt = sizeof(uint) + (long)trieBytes;
        if (replacementsAt > charsMap.Length)
        {
            throw new InvalidDataException(
                $"The precompiled charsmap declares a {trieBytes}-byte trie but carries only {charsMap.Length - sizeof(uint)} bytes after the header.");
        }

        var trie = new uint[trieBytes / sizeof(uint)];
        for (int i = 0; i < trie.Length; i++)
        {
            int at = sizeof(uint) + (i * sizeof(uint));
            trie[i] = (uint)charsMap[at] | ((uint)charsMap[at + 1] << 8) | ((uint)charsMap[at + 2] << 16) | ((uint)charsMap[at + 3] << 24);
        }
        if (trie.Length == 0)
        {
            throw new InvalidDataException("The precompiled charsmap carries an empty trie.");
        }

        return new PrecompiledNormalizer(charsMap, trie, (int)replacementsAt);
    }

    /// <summary>Number of bytes in the underlying <c>precompiled_charsmap</c>.</summary>
    public int CharsMapLength => _charsMap.Length;

    /// <summary>Rewrites <paramref name="text"/> the way the model's normalizer does.</summary>
    /// <param name="text">The text to normalize.</param>
    /// <remarks>
    /// Longest match wins, as in <c>sentencepiece</c>: at each position the longest
    /// prefix the trie knows is replaced, and a position no rule covers keeps its
    /// character. Whitespace handling — <c>remove_extra_whitespaces</c>,
    /// <c>add_dummy_prefix</c>, <c>escape_whitespaces</c> — is <em>not</em> done
    /// here; <see cref="SentencePieceTokenizer"/> applies it after this pass, which
    /// is the order the reference implementation uses.
    /// </remarks>
    /// <exception cref="InvalidDataException">The charsmap points at a replacement it does not contain.</exception>
    public string Normalize(string text)
    {
        Guard.NotNull(text);
        if (text.Length == 0)
        {
            return string.Empty;
        }

        byte[] input = JsonArtifact.Utf8NoBom.GetBytes(text);
        var output = new List<byte>(input.Length);
        int at = 0;
        while (at < input.Length)
        {
            int matched = LongestMatch(input, at, out int replacementAt);
            if (matched == 0)
            {
                // No rule covers this position: the character passes through whole.
                int width = Utf8SequenceLength(input, at);
                for (int i = 0; i < width; i++)
                {
                    output.Add(input[at + i]);
                }
                at += width;
                continue;
            }
            AppendReplacement(output, replacementAt);
            at += matched;
        }

        byte[] normalized = output.ToArray();
        return JsonArtifact.Utf8NoBom.GetString(normalized, 0, normalized.Length);
    }

    /// <summary>Walks the trie from <paramref name="at"/>, returning the length of the longest rule that matches.</summary>
    private int LongestMatch(byte[] input, int at, out int replacementAt)
    {
        replacementAt = 0;
        int matched = 0;

        uint unit = _trie[0];
        long nodePos = Offset(unit);
        for (int i = at; i < input.Length; i++)
        {
            nodePos ^= input[i];
            if (nodePos < 0 || nodePos >= _trie.Length)
            {
                throw Malformed();
            }
            unit = _trie[nodePos];
            if ((unit & LabelMask) != input[i])
            {
                break;
            }
            nodePos ^= Offset(unit);
            if (nodePos < 0 || nodePos >= _trie.Length)
            {
                throw Malformed();
            }
            if ((unit & HasLeafBit) != 0)
            {
                // A rule ends here. Keep walking: a longer one may follow, and
                // sentencepiece takes the longest.
                replacementAt = (int)(_trie[nodePos] & ValueMask);
                matched = i - at + 1;
            }
        }
        return matched;
    }

    private void AppendReplacement(List<byte> output, int replacementAt)
    {
        int from = _replacementsAt + replacementAt;
        if (replacementAt < 0 || from >= _charsMap.Length)
        {
            throw Malformed();
        }
        for (int i = from; i < _charsMap.Length; i++)
        {
            if (_charsMap[i] == 0)
            {
                return;
            }
            output.Add(_charsMap[i]);
        }
        // Every replacement is NUL-terminated; running off the end means the blob
        // was cut short after the trie.
        throw Malformed();
    }

    private static uint Offset(uint unit) => (unit >> 10) << (int)((unit & OffsetScaleBit) >> 6);

    private static int Utf8SequenceLength(byte[] input, int at)
    {
        int width = 1;
        while (at + width < input.Length && (input[at + width] & 0xC0) == 0x80)
        {
            width++;
        }
        return width;
    }

    private static InvalidDataException Malformed() =>
        new("The precompiled charsmap is malformed: its trie points outside the blob that carries it.");

    /// <summary>Compares two normalizers by the charsmap they were read from.</summary>
    /// <param name="other">The normalizer to compare against.</param>
    /// <remarks>
    /// Two vocabularies read from the same file carry equal normalizers, which
    /// reference equality would not give — the same reason
    /// <see cref="SentencePieceVocabulary"/> writes its own equality.
    /// </remarks>
    public bool Equals(PrecompiledNormalizer? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (other is null || _charsMap.Length != other._charsMap.Length)
        {
            return false;
        }
        for (int i = 0; i < _charsMap.Length; i++)
        {
            if (_charsMap[i] != other._charsMap[i])
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as PrecompiledNormalizer);

    /// <summary>Hashes the charsmap's length and its first and last bytes, which is O(1).</summary>
    /// <remarks>
    /// Equal normalizers necessarily agree on all three; unequal ones may collide.
    /// Hashing a quarter of a megabyte would defeat the purpose of a hash — the
    /// same call <see cref="SentencePieceVocabulary.GetHashCode"/> makes.
    /// </remarks>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 31) + _charsMap.Length;
            hash = (hash * 31) + _charsMap[0];
            return (hash * 31) + _charsMap[_charsMap.Length - 1];
        }
    }
}
