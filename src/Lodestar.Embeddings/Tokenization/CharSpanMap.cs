using System.Collections.Generic;

namespace Lodestar.Embeddings.Tokenization;

/// <summary>
/// A read-only string-keyed map that can be probed with a <see cref="ReadOnlySpan{T}"/>,
/// so a caller holding a slice of a longer string need not materialize it.
/// </summary>
/// <remarks>
/// The tokenizers probe slices, and a <see cref="Dictionary{TKey, TValue}"/> can only be
/// asked about a string — where issue #498 measured 519 MB of substrings built to be hashed
/// and dropped. .NET 9's `GetAlternateLookup` answers this; netstandard2.0 has nothing, and
/// one behaviour ships on both. Open addressing, never filled past half, built once.
/// </remarks>
/// <typeparam name="TValue">What each key maps to.</typeparam>
internal sealed class CharSpanMap<TValue>
{
    private readonly string?[] _keys;
    private readonly TValue[] _values;
    private readonly int[] _hashes;
    private readonly int _mask;

    /// <summary>Builds the table from <paramref name="entries"/>, last value winning a repeated key.</summary>
    /// <param name="entries">The keys and their values. A null key is refused.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/>, or one of its keys, is null.</exception>
    public CharSpanMap(IReadOnlyCollection<KeyValuePair<string, TValue>> entries)
    {
        Guard.NotNull(entries);

        int capacity = 8;
        while (capacity < (entries.Count + 1) * 2)
        {
            capacity <<= 1;
        }

        _keys = new string?[capacity];
        _values = new TValue[capacity];
        _hashes = new int[capacity];
        _mask = capacity - 1;

        foreach (KeyValuePair<string, TValue> entry in entries)
        {
            Guard.NotNull(entry.Key);
            Insert(entry.Key, entry.Value);
        }
    }

    /// <summary>How many distinct keys the table holds.</summary>
    public int Count { get; private set; }

    /// <summary>Looks <paramref name="key"/> up without materializing it as a string.</summary>
    /// <param name="key">The key to find.</param>
    /// <param name="value">Receives the value when the key is present.</param>
    public bool TryGetValue(ReadOnlySpan<char> key, out TValue value)
    {
        int hash = Hash(key);
        for (int probe = 0; probe < _keys.Length; probe++)
        {
            int slot = (hash + probe) & _mask;
            string? candidate = _keys[slot];
            if (candidate is null)
            {
                value = default!;
                return false;
            }
            if (_hashes[slot] == hash && key.SequenceEqual(candidate.AsSpan()))
            {
                value = _values[slot];
                return true;
            }
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Looks up the concatenation of <paramref name="prefix"/> and <paramref name="key"/>
    /// without building it.
    /// </summary>
    /// <remarks>
    /// WordPiece's continuation prefix is what this exists for: every candidate after the
    /// first is <c>##</c> plus a slice, and concatenating to ask was a second allocation on
    /// top of the slice's own.
    /// </remarks>
    /// <param name="prefix">What precedes the key. Empty asks for the bare key.</param>
    /// <param name="key">The rest of the key.</param>
    /// <param name="value">Receives the value when the key is present.</param>
    public bool TryGetValue(ReadOnlySpan<char> prefix, ReadOnlySpan<char> key, out TValue value)
    {
        if (prefix.IsEmpty)
        {
            return TryGetValue(key, out value);
        }

        int hash = Hash(prefix, key);
        int length = prefix.Length + key.Length;
        for (int probe = 0; probe < _keys.Length; probe++)
        {
            int slot = (hash + probe) & _mask;
            string? candidate = _keys[slot];
            if (candidate is null)
            {
                value = default!;
                return false;
            }
            if (_hashes[slot] == hash
                && candidate.Length == length
                && prefix.SequenceEqual(candidate.AsSpan(0, prefix.Length))
                && key.SequenceEqual(candidate.AsSpan(prefix.Length)))
            {
                value = _values[slot];
                return true;
            }
        }

        value = default!;
        return false;
    }

    // FNV-1a over UTF-16 code units: never persisted and no security boundary, so the only
    // requirement is that a key and the span of the same characters agree.
    private static int Hash(ReadOnlySpan<char> key)
    {
        uint hash = 2166136261;
        for (int i = 0; i < key.Length; i++)
        {
            hash = (hash ^ key[i]) * 16777619;
        }
        return (int)hash & 0x7FFFFFFF;
    }

    private static int Hash(ReadOnlySpan<char> prefix, ReadOnlySpan<char> key)
    {
        uint hash = 2166136261;
        for (int i = 0; i < prefix.Length; i++)
        {
            hash = (hash ^ prefix[i]) * 16777619;
        }
        for (int i = 0; i < key.Length; i++)
        {
            hash = (hash ^ key[i]) * 16777619;
        }
        return (int)hash & 0x7FFFFFFF;
    }

    private void Insert(string key, TValue value)
    {
        int hash = Hash(key.AsSpan());
        for (int probe = 0; probe < _keys.Length; probe++)
        {
            int slot = (hash + probe) & _mask;
            string? candidate = _keys[slot];
            if (candidate is null)
            {
                _keys[slot] = key;
                _values[slot] = value;
                _hashes[slot] = hash;
                Count++;
                return;
            }
            if (_hashes[slot] == hash && string.Equals(candidate, key, StringComparison.Ordinal))
            {
                _values[slot] = value;
                return;
            }
        }
    }
}
