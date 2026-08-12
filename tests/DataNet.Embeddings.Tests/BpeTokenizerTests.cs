using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

// SonarLint S2245 / CA5394: a seeded Random builds a reproducible corpus for this
// test; the sequence is fixed by the seed and nothing here is a security decision.
#pragma warning disable S2245, CA5394

public sealed class BpeTokenizerTests
{
    /// <summary>Reads tiny_bpe.json directly: this suite tests merging, not loading.</summary>
    internal static BpeVocabulary TinyVocabulary()
    {
        using JsonDocument doc = OracleLoader.Load("tiny_bpe.json");
        JsonElement model = doc.RootElement.GetProperty("model");

        var vocab = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (JsonProperty entry in model.GetProperty("vocab").EnumerateObject())
        {
            vocab[entry.Name] = entry.Value.GetInt32();
        }

        var merges = new List<MergePair>();
        foreach (JsonElement merge in model.GetProperty("merges").EnumerateArray())
        {
            if (merge.ValueKind == JsonValueKind.Array)
            {
                merges.Add(new MergePair(merge[0].GetString()!, merge[1].GetString()!));
            }
            else
            {
                string[] parts = merge.GetString()!.Split(' ');
                merges.Add(new MergePair(parts[0], parts[1]));
            }
        }

        // added_tokens is a sibling of "model", not nested inside it: HuggingFace
        // records it at the top level of tokenizer.json, alongside the pipeline
        // stages that consult it. "[UNK]" is both a plain vocabulary entry (id 0,
        // in model.vocab above) and a declared added token, so BpeTokenizer.Encode
        // recognizes it as a literal match rather than splitting it letter by letter.
        var addedTokens = new List<AddedToken>();
        foreach (JsonElement added in doc.RootElement.GetProperty("added_tokens").EnumerateArray())
        {
            addedTokens.Add(new AddedToken(
                added.GetProperty("content").GetString()!,
                added.GetProperty("id").GetInt32()));
        }

        return new BpeVocabulary(vocab, merges)
        {
            AddedTokens = addedTokens,
            EndOfWordSuffix = model.GetProperty("end_of_word_suffix").GetString(),
            UnkToken = model.GetProperty("unk_token").GetString(),
        };
    }

    [Fact]
    public void Encode_matches_tokenizers()
    {
        using JsonDocument doc = OracleLoader.Load("bpe.json");
        var tokenizer = new BpeTokenizer(TinyVocabulary());

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            string[] expectedTokens = c.GetProperty("tokens").EnumerateArray().Select(e => e.GetString()!).ToArray();
            int[] expectedIds = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();

            TokenizationResult actual = tokenizer.Encode(text);
            if (!expectedTokens.SequenceEqual(actual.Tokens) || !expectedIds.SequenceEqual(actual.Ids))
            {
                failures.Add($"{JsonSerializer.Serialize(text)}\n  exp: [{string.Join(" | ", expectedTokens)}]\n  got: [{string.Join(" | ", actual.Tokens)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void TryGetId_finds_a_literal_entry()
    {
        var tokenizer = new BpeTokenizer(TinyVocabulary());
        Assert.True(tokenizer.TryGetId("[UNK]", out int unk));
        Assert.Equal(0, unk);
        Assert.False(tokenizer.TryGetId("definitely-not-a-token", out _));
    }

    [Fact]
    public void An_unknown_token_absent_from_the_vocabulary_is_refused()
    {
        BpeVocabulary broken = TinyVocabulary() with { UnkToken = "[NOPE]" };
        Assert.Throws<ArgumentException>(() => new BpeTokenizer(broken));
    }

    [Fact]
    public void A_merge_naming_a_missing_token_is_refused()
    {
        BpeVocabulary vocab = TinyVocabulary();
        var merges = new List<MergePair>(vocab.Merges) { new("zzz", "qqq") };

        Assert.Throws<ArgumentException>(() => new BpeTokenizer(vocab with { Merges = merges }));
    }

    /// <summary>
    /// Regression test for the leftmost/longest added-token rule: the corpus
    /// declares exactly one added token ("[UNK]"), so it cannot distinguish this
    /// rule from any other. Two added tokens sharing a prefix and starting at the
    /// same position must resolve to the longer one, matching HuggingFace's
    /// AddedVocabulary (Aho-Corasick <c>LeftmostLongest</c>).
    /// </summary>
    [Fact]
    public void Added_tokens_prefer_the_longest_match_at_the_same_position()
    {
        BpeVocabulary vocab = TinyVocabulary() with
        {
            AddedTokens = [new AddedToken("<a>", 1000), new AddedToken("<a>b", 1001)],
        };
        var tokenizer = new BpeTokenizer(vocab);

        TokenizationResult result = tokenizer.Encode("<a>b");

        Assert.Equal(["<a>b"], result.Tokens);
        Assert.Equal([1001], result.Ids);
    }

    /// <summary>
    /// Regression test for the other half of the rule: a shorter match that starts
    /// earlier is chosen over a longer match that starts later, even though "longer
    /// wins" is the tie-break at equal positions. Leftmost always wins first.
    /// </summary>
    [Fact]
    public void Added_tokens_prefer_the_earliest_position_over_a_longer_later_match()
    {
        BpeVocabulary vocab = TinyVocabulary() with
        {
            AddedTokens = [new AddedToken("<z>", 2000), new AddedToken("<a><a>", 2001)],
        };
        var tokenizer = new BpeTokenizer(vocab);

        TokenizationResult result = tokenizer.Encode("<z><a><a>");

        Assert.Equal(["<z>", "<a><a>"], result.Tokens);
        Assert.Equal([2000, 2001], result.Ids);
    }

    /// <summary>
    /// A malformed vocabulary can declare an empty added token: the loader this
    /// vocabulary is meant to come from bounds a token's upper length but never
    /// rejects an empty one. Left unfiltered, <c>IndexOf("", pos)</c> always
    /// returns <c>pos</c>, so the scan in <c>Encode</c> never advances.
    /// </summary>
    [Fact]
    public void An_empty_added_token_is_ignored_rather_than_hanging_encode()
    {
        BpeVocabulary vocab = TinyVocabulary() with
        {
            AddedTokens = [new AddedToken(string.Empty, 999)],
        };
        var tokenizer = new BpeTokenizer(vocab);

        TokenizationResult result = tokenizer.Encode("the quick brown fox");

        Assert.Equal(new BpeTokenizer(TinyVocabulary()).Encode("the quick brown fox").Ids, result.Ids);
    }

    [Fact]
    public void Decode_turns_the_end_of_word_marker_back_into_a_space()
    {
        var tokenizer = new BpeTokenizer(TinyVocabulary());
        TokenizationResult encoded = tokenizer.Encode("the quick brown fox");
        Assert.Equal("the quick brown fox", tokenizer.Decode(encoded.Ids));
    }

    /// <summary>
    /// None of the 20 cases in bytelevel_bpe.json ever tokenizes to an added-token
    /// id, so the oracle-driven decode tests cannot exercise
    /// <c>skipSpecialTokens</c> at all -- they would still pass if the flag were a
    /// no-op. An id list is built by hand instead, following the same
    /// <c>TinyVocabulary() with { AddedTokens = ... }</c> pattern the Encode-side
    /// added-token tests already use, so the flag has at least one test where
    /// deleting its branch in <c>Append</c> would turn a passing assertion red.
    /// </summary>
    [Fact]
    public void Decode_renders_or_drops_an_added_token_per_the_flag()
    {
        BpeVocabulary vocab = TinyVocabulary() with
        {
            AddedTokens = [new AddedToken("<sep>", 3000) { Special = true }],
        };
        var tokenizer = new BpeTokenizer(vocab);

        // "the" -> "the</w>" (id 48) and "fox" -> "fox</w>" (id 144) are each a
        // single token on their own, confirmed by bpe.json's
        // "the quick brown fox ..." case. The added token sits between them, so
        // its presence or absence is the only thing that can tell the two
        // Decode calls below apart. It is marked special, which is what
        // skipSpecialTokens now keys on.
        int[] ids = [48, 3000, 144];

        Assert.Equal("the <sep>fox", tokenizer.Decode(ids));
        Assert.Equal("the fox", tokenizer.Decode(ids, skipSpecialTokens: true));
    }

    [Fact]
    public void Decode_skipping_specials_keeps_an_ordinary_added_token()
    {
        var vocabulary = new BpeVocabulary(
            new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 0, ["<s>"] = 1, ["<x>"] = 2 },
            [])
        {
            AddedTokens =
            [
                new AddedToken("<s>", 1) { Special = true },
                new AddedToken("<x>", 2),
            ],
        };
        var tokenizer = new BpeTokenizer(vocabulary);

        Assert.Equal("<x>", tokenizer.Decode([1, 2], skipSpecialTokens: true));
        Assert.Equal("<s><x>", tokenizer.Decode([1, 2], skipSpecialTokens: false));
    }

    /// <summary>
    /// <c>StringBuilder.Replace("", " ")</c> throws, so an empty suffix used to crash the classic
    /// lineage's <c>Decode</c> after the file had loaded cleanly. Issue #118.
    /// </summary>
    [Fact]
    public void Decode_does_not_throw_when_the_end_of_word_suffix_is_empty()
    {
        var vocabulary = new BpeVocabulary(
            new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 0, ["b"] = 1 },
            [])
        { EndOfWordSuffix = "" };
        var tokenizer = new BpeTokenizer(vocabulary);

        string decoded = tokenizer.Decode([0, 1]);

        Assert.Equal("ab", decoded);
    }

    /// <summary>
    /// A hand-built vocabulary: <paramref name="tokens"/> in id order, and
    /// <paramref name="merges"/> in rank order, each written as "left right".
    /// Small enough that the merge order it produces can be worked out by hand,
    /// which is the point — the oracle corpora prove parity, not invariants.
    /// </summary>
    private static BpeVocabulary HandBuiltVocabulary(string[] tokens, params string[] merges)
    {
        var vocab = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int id = 0; id < tokens.Length; id++)
        {
            vocab[tokens[id]] = id;
        }
        var pairs = new List<MergePair>();
        foreach (string merge in merges)
        {
            string[] parts = merge.Split(' ');
            pairs.Add(new MergePair(parts[0], parts[1]));
        }
        return new BpeVocabulary(vocab, pairs);
    }

    /// <summary>
    /// Two occurrences of the same pair share a rank, and the leftmost one is the
    /// one applied — HuggingFace's rule, and what the merge loop's strict
    /// "lower rank wins" comparison produces, since the first position found at
    /// the winning rank is never displaced by a later one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The corpora cannot pin this down: they prove the tokens match, and the
    /// vocabularies in them may simply never contain an input where the choice
    /// between two equally-ranked positions is observable. Stated here instead,
    /// so any reordering of the merge loop's candidate selection has to answer
    /// to it.
    /// </para>
    /// <para>
    /// "abab" starts as <c>a b a b</c>, with the rank-1 pair <c>(a, b)</c> at two
    /// positions and nothing else applicable. Merging the left one first makes
    /// <c>ab a b</c>, which exposes the rank-0 pair <c>(ab, a)</c> and ends at
    /// <c>aba b</c>. Merging the right one first would make <c>a b ab</c>,
    /// exposing nothing, and would end at <c>ab ab</c>. The two are told apart by
    /// the ids alone.
    /// </para>
    /// </remarks>
    [Fact]
    public void Equal_ranked_pairs_merge_at_the_leftmost_position()
    {
        var tokenizer = new BpeTokenizer(
            HandBuiltVocabulary(["a", "b", "ab", "aba"], "ab a", "a b"));

        TokenizationResult result = tokenizer.Encode("abab");

        Assert.Equal(["aba", "b"], result.Tokens);
        Assert.Equal([3, 1], result.Ids);
    }

    /// <summary>
    /// The same rule where the two occurrences overlap, so applying one destroys
    /// the other outright: <c>a a a</c> has the rank-0 pair <c>(a, a)</c> at
    /// positions 0 and 1, and only one of them can ever apply. Leftmost gives
    /// <c>aa a</c>, which the rank-1 pair <c>(aa, a)</c> finishes as a single
    /// "aaa"; rightmost would give <c>a aa</c>, which nothing joins.
    /// </summary>
    [Fact]
    public void Overlapping_equal_ranked_pairs_merge_from_the_left()
    {
        var tokenizer = new BpeTokenizer(
            HandBuiltVocabulary(["a", "aa", "aaa"], "a a", "aa a"));

        TokenizationResult result = tokenizer.Encode("aaa");

        Assert.Equal(["aaa"], result.Tokens);
        Assert.Equal([2], result.Ids);
    }

    /// <summary>
    /// The merge rule, written out the slow and obvious way: scan every adjacent
    /// pair, take the lowest-ranked one and the leftmost of those, apply it,
    /// repeat. Deliberately not the shipped algorithm — it is the definition the
    /// shipped algorithm is answerable to.
    /// </summary>
    /// <remarks>
    /// The rank of a pair is looked up in a dictionary rather than by scanning
    /// the merge list, which is the one thing here that is not naive. It changes
    /// no outcome — the dictionary keeps the first rank a pair is listed at,
    /// which is what scanning for the first match would find — and it is what
    /// makes the reference affordable on a piece long enough to be worth
    /// comparing against. What has to stay slow and obvious is the rescan of
    /// every pair after every merge, and that is exactly what is left.
    /// </remarks>
    private static List<string> NaiveMerge(string piece, List<MergePair> merges)
    {
        var ranks = new Dictionary<(string Left, string Right), int>();
        for (int rank = 0; rank < merges.Count; rank++)
        {
            if (!ranks.ContainsKey((merges[rank].Left, merges[rank].Right)))
            {
                ranks[(merges[rank].Left, merges[rank].Right)] = rank;
            }
        }

        List<string> symbols = [.. piece.Select(c => c.ToString())];
        while (symbols.Count > 1)
        {
            int bestRank = int.MaxValue;
            int bestAt = -1;
            for (int i = 0; i + 1 < symbols.Count; i++)
            {
                if (ranks.TryGetValue((symbols[i], symbols[i + 1]), out int rank) && rank < bestRank)
                {
                    bestRank = rank;
                    bestAt = i;
                }
            }
            if (bestAt < 0)
            {
                break;
            }
            symbols[bestAt] += symbols[bestAt + 1];
            symbols.RemoveAt(bestAt + 1);
        }
        return symbols;
    }

    /// <summary>
    /// A generated model over <paramref name="alphabet"/>: each merge joins two
    /// tokens the vocabulary already has, and adds the result as a new one, so
    /// every merge is applicable and the ranks are the order they were made in.
    /// </summary>
    private static (BpeVocabulary Vocabulary, List<MergePair> Merges) GenerateModel(
        Random random, string alphabet, int mergeCount)
    {
        List<string> tokens = [.. alphabet.Select(c => c.ToString())];
        var merges = new List<MergePair>();
        while (merges.Count < mergeCount)
        {
            string left = tokens[random.Next(tokens.Count)];
            string right = tokens[random.Next(tokens.Count)];
            // A token string is a vocabulary key, so the same spelling cannot be
            // reached two ways; skipping keeps every merge distinct and applicable.
            if (tokens.Contains(left + right, StringComparer.Ordinal))
            {
                continue;
            }
            merges.Add(new MergePair(left, right));
            tokens.Add(left + right);
        }
        return (HandBuiltVocabulary([.. tokens]) with { Merges = merges }, merges);
    }

    /// <summary>
    /// A model where every pair of tokens at one level is a merge producing a
    /// token of the next: 2 letters give 4 two-letter tokens, which give 16
    /// four-letter tokens, and so on. Nothing is left inapplicable, so a piece
    /// keeps merging instead of stalling early — which is what fills the queue.
    /// </summary>
    private static (BpeVocabulary Vocabulary, List<MergePair> Merges) DenseModel(string alphabet, int levels)
    {
        List<string> tokens = [.. alphabet.Select(c => c.ToString())];
        var merges = new List<MergePair>();
        List<string> level = [.. tokens];
        for (int l = 0; l < levels; l++)
        {
            var next = new List<string>();
            foreach (string left in level)
            {
                foreach (string right in level)
                {
                    merges.Add(new MergePair(left, right));
                    next.Add(left + right);
                }
            }
            tokens.AddRange(next);
            level = next;
        }
        return (HandBuiltVocabulary([.. tokens]) with { Merges = merges }, merges);
    }

    /// <summary>
    /// The oracle corpora prove parity on the text they contain, and no more:
    /// whether they ever exercise a merge order where a linked list and a
    /// priority queue could disagree with a repeated scan is not something they
    /// state. So the two are run against each other over generated vocabularies
    /// and generated input, seeded so a failure is reproducible rather than a
    /// story about a run nobody has any more.
    /// </summary>
    [Fact]
    public void Merging_agrees_with_a_naive_scan_over_generated_vocabularies()
    {
        const string Alphabet = "abcd";
        var random = new Random(20260808);
        var failures = new List<string>();

        for (int trial = 0; trial < 40; trial++)
        {
            (BpeVocabulary vocabulary, List<MergePair> merges) = GenerateModel(random, Alphabet, 24);
            var tokenizer = new BpeTokenizer(vocabulary);

            for (int c = 0; c < 25; c++)
            {
                string piece = new([.. Enumerable.Range(0, 3 + random.Next(38)).Select(_ => Alphabet[random.Next(Alphabet.Length)])]);
                List<string> expected = NaiveMerge(piece, merges);
                IReadOnlyList<string> actual = tokenizer.Encode(piece).Tokens;
                if (!expected.SequenceEqual(actual))
                {
                    failures.Add($"{piece}\n  exp: [{string.Join(" | ", expected)}]\n  got: [{string.Join(" | ", actual)}]");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The regime the priority queue was written for, which nothing else in the
    /// suite reaches: one unsplittable piece of 1024 symbols, where the queue is
    /// ten-odd levels deep and takes thousands of entries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every other test here merges a handful of symbols, and a benchmark is not
    /// a gate — it does not run in CI, and it asserts nothing about the tokens.
    /// So the deep-queue regime, the one this whole algorithm exists for, was
    /// proven only by a stopwatch. Here it is proven by the tokens: 1024 symbols
    /// over a two-letter alphabet, against a model where every pair at every
    /// level is applicable so the piece keeps merging instead of stalling. It
    /// ends at 322 tokens, so 702 merges run, each one taking a candidate off a
    /// queue ten levels deep and putting two more back. A sparser, generated
    /// model was tried first and stalled at 552 tokens, which is why this one is
    /// built rather than generated.
    /// </para>
    /// <para>
    /// What this does <em>not</em> pin is <c>QueueCapacity</c>. That was the
    /// intent, and measuring killed it: the queue is bounded by how many entries
    /// are in it at once, not by how many are ever pushed, and each round takes
    /// one before offering at most two. Even <c>count</c> entries — a third of
    /// what is rented — carries this case, verified by shrinking the bound until
    /// something broke and finding that nothing did. So the bound keeps its
    /// slack, and no test claims to be guarding it.
    /// </para>
    /// <para>
    /// Length was chosen against the reference, not the shipped code: the naive
    /// scan is quadratic by construction, which is the whole point of it, so it
    /// is what sets what the suite can afford. At 1024 the whole case runs in
    /// about 40 ms, while sitting 25x past the longest piece any other test
    /// merges.
    /// </para>
    /// </remarks>
    [Fact]
    public void Merging_agrees_with_a_naive_scan_over_a_long_unsplittable_piece()
    {
        const string Alphabet = "ab";
        const int Length = 1024;
        var random = new Random(20260809);
        (BpeVocabulary vocabulary, List<MergePair> merges) = DenseModel(Alphabet, 3);
        var tokenizer = new BpeTokenizer(vocabulary);
        string piece = new([.. Enumerable.Range(0, Length).Select(_ => Alphabet[random.Next(Alphabet.Length)])]);

        IReadOnlyList<string> actual = tokenizer.Encode(piece).Tokens;

        // No whitespace and no punctuation, so the pre-tokenizer hands the whole
        // run to one Merge call rather than splitting it into cheap little pieces.
        Assert.Equal(Length, string.Concat(actual).Length);
        Assert.True(actual.Count * 2 <= Length, $"only {actual.Count} of {Length} symbols merged; the queue barely filled");
        List<string> expected = NaiveMerge(piece, merges);
        Assert.True(
            expected.SequenceEqual(actual),
            $"exp {expected.Count} tokens: [{string.Join(" | ", expected.Take(20))} ...]\n"
            + $"got {actual.Count} tokens: [{string.Join(" | ", actual.Take(20))} ...]");
    }

    /// <summary>
    /// Reads orphan_bpe_model.json directly: this suite tests merging, not loading.
    /// The vocabulary declares no added tokens, unlike <see cref="TinyVocabulary"/>,
    /// so <c>added_tokens</c> is not read here.
    /// </summary>
    internal static BpeVocabulary OrphanVocabulary()
    {
        using JsonDocument doc = OracleLoader.Load("orphan_bpe_model.json");
        JsonElement model = doc.RootElement.GetProperty("model");

        var vocab = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (JsonProperty entry in model.GetProperty("vocab").EnumerateObject())
        {
            vocab[entry.Name] = entry.Value.GetInt32();
        }

        var merges = new List<MergePair>();
        foreach (JsonElement merge in model.GetProperty("merges").EnumerateArray())
        {
            merges.Add(new MergePair(merge[0].GetString()!, merge[1].GetString()!));
        }

        return new BpeVocabulary(vocab, merges);
    }

    /// <summary>
    /// <c>orphan_bpe_model.json</c> declares "abc" as a vocabulary entry without
    /// registering the merge that would let the ordinary merge loop reach it --
    /// "abc" is byte-for-byte what a tiktoken-to-tokenizer.json conversion (Llama-3,
    /// Qwen) produces, and what GPT-2's own natively-trained table structurally
    /// cannot (see <see cref="ByteLevelBpeTests.IgnoreMerges_is_behaviour_neutral_on_a_natively_trained_vocabulary"/>).
    /// With the flag off, encoding "abc" merges greedily to ["ab", "c"], stepping
    /// past the very entry that is sitting in the vocabulary; with it on, the whole
    /// piece is looked up first and emitted as the single "abc" token instead. This
    /// is the test that would still fail if <c>EncodePiece</c> never read
    /// <c>_ignoreMerges</c>.
    /// </summary>
    [Fact]
    public void IgnoreMerges_emits_a_whole_piece_present_in_the_vocabulary()
    {
        using JsonDocument doc = OracleLoader.Load("orphan_bpe.json");
        var normal = new BpeTokenizer(OrphanVocabulary());
        var ignoring = new BpeTokenizer(OrphanVocabulary() with { IgnoreMerges = true });

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            int[] expected = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();
            int[] expectedIgnoring = c.GetProperty("ids_ignore_merges").EnumerateArray().Select(e => e.GetInt32()).ToArray();
            int[] actual = [.. normal.Encode(text).Ids];
            int[] actualIgnoring = [.. ignoring.Encode(text).Ids];
            if (!expected.SequenceEqual(actual))
            {
                failures.Add($"normal {JsonSerializer.Serialize(text)}\n  exp: [{string.Join(", ", expected)}]\n  got: [{string.Join(", ", actual)}]");
            }
            if (!expectedIgnoring.SequenceEqual(actualIgnoring))
            {
                failures.Add($"ignore_merges {JsonSerializer.Serialize(text)}\n  exp: [{string.Join(", ", expectedIgnoring)}]\n  got: [{string.Join(", ", actualIgnoring)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
}
