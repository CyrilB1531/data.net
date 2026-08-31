using Xunit;
using Lodestar.Embeddings.Tokenization;

namespace Lodestar.Embeddings.Tests;

/// <summary>
/// The span-keyed lookup the tokenizers use in place of a
/// <see cref="Dictionary{TKey, TValue}"/> they had to build a substring for (#498).
/// </summary>
public class CharSpanMapTests
{
    private static CharSpanMap<int> Map(params string[] keys)
    {
        var entries = new KeyValuePair<string, int>[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            entries[i] = new KeyValuePair<string, int>(keys[i], i);
        }
        return new CharSpanMap<int>(entries);
    }

    [Fact]
    public void A_present_key_is_found_by_span()
    {
        CharSpanMap<int> map = Map("ab", "abc", "b");
        Assert.True(map.TryGetValue("abc".AsSpan(), out int value));
        Assert.Equal(1, value);
    }

    [Fact]
    public void An_absent_key_is_not_found()
    {
        CharSpanMap<int> map = Map("ab", "abc");
        Assert.False(map.TryGetValue("abd".AsSpan(), out int value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void A_slice_of_a_longer_string_is_found_without_copying_it()
    {
        CharSpanMap<int> map = Map("cat", "dog");
        Assert.True(map.TryGetValue("thecatsat".AsSpan(3, 3), out int value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void A_prefix_is_matched_without_concatenating_it()
    {
        CharSpanMap<int> map = Map("##ing", "ing");
        Assert.True(map.TryGetValue("##".AsSpan(), "ing".AsSpan(), out int value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void An_empty_prefix_matches_the_bare_key()
    {
        CharSpanMap<int> map = Map("##ing", "ing");
        Assert.True(map.TryGetValue([], "ing".AsSpan(), out int value));
        Assert.Equal(1, value);
    }

    [Fact]
    public void The_empty_key_is_a_key_like_any_other()
    {
        CharSpanMap<int> map = Map(string.Empty, "a");
        Assert.True(map.TryGetValue([], out int value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void An_empty_map_finds_nothing()
    {
        CharSpanMap<int> map = Map();
        Assert.Equal(0, map.Count);
        Assert.False(map.TryGetValue("a".AsSpan(), out _));
    }

    [Fact]
    public void A_repeated_key_keeps_the_last_value_as_an_indexer_assignment_would()
    {
        var entries = new[]
        {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("a", 2),
        };
        var map = new CharSpanMap<int>(entries);
        Assert.Equal(1, map.Count);
        Assert.True(map.TryGetValue("a".AsSpan(), out int value));
        Assert.Equal(2, value);
    }

    [Fact]
    public void Every_key_of_a_large_map_is_found_and_no_absent_one_is()
    {
        // Past any plausible probe-sequence bug, and past the point where a table
        // that never grew would still answer correctly.
        const int Count = 5000;
        var entries = new KeyValuePair<string, int>[Count];
        for (int i = 0; i < Count; i++)
        {
            entries[i] = new KeyValuePair<string, int>($"piece{i}", i);
        }
        var map = new CharSpanMap<int>(entries);

        Assert.Equal(Count, map.Count);
        for (int i = 0; i < Count; i++)
        {
            Assert.True(map.TryGetValue($"piece{i}".AsSpan(), out int value), $"piece{i}");
            Assert.Equal(i, value);
            Assert.False(map.TryGetValue($"absent{i}".AsSpan(), out _), $"absent{i}");
        }
    }

    [Fact]
    public void A_null_entry_collection_is_refused()
    {
        Assert.Throws<ArgumentNullException>(() => new CharSpanMap<int>(null!));
    }

    [Fact]
    public void A_null_key_is_refused()
    {
        var entries = new[] { new KeyValuePair<string, int>(null!, 1) };
        Assert.Throws<ArgumentNullException>(() => new CharSpanMap<int>(entries));
    }
}
