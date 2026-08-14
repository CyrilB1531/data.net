using DataNet.Text.Vectorization;
using Xunit;

namespace DataNet.Text.Tests.Vectorization;

/// <summary>
/// <see cref="CountVectorizerOptions"/> is a record, so it advertises value
/// equality — and comparing two configurations is a far more plausible thing to do
/// than comparing two fitted vocabularies. But <c>StopWords</c> is a collection,
/// which the generated equality compares by reference, and
/// <see cref="TfidfVectorizerOptions"/> embeds the whole thing.
/// </summary>
public sealed class OptionsEqualityTests
{
    [Fact]
    public void Options_with_the_same_stop_words_are_equal()
    {
        var a = new CountVectorizerOptions { StopWords = ["the", "a", "over"] };
        var b = new CountVectorizerOptions { StopWords = ["the", "a", "over"] };

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Stop_word_order_does_not_change_the_configuration()
    {
        // Stop words are looked up, never enumerated in order, so two lists holding
        // the same words describe the same vectorizer.
        var a = new CountVectorizerOptions { StopWords = ["the", "a"] };
        var b = new CountVectorizerOptions { StopWords = ["a", "the"] };

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void A_repeated_stop_word_does_not_break_the_hash_contract()
    {
        // Equals compares as a set, so these are equal -- and a Dictionary keyed on
        // options would miss the entry it holds if GetHashCode disagreed.
        var a = new CountVectorizerOptions { StopWords = ["the", "the"] };
        var b = new CountVectorizerOptions { StopWords = ["the"] };

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Options_with_different_stop_words_are_not_equal()
    {
        var a = new CountVectorizerOptions { StopWords = ["the"] };
        var b = new CountVectorizerOptions { StopWords = ["a"] };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Options_with_and_without_stop_words_are_not_equal()
    {
        var a = new CountVectorizerOptions { StopWords = ["the"] };
        var b = new CountVectorizerOptions();

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Options_differing_in_a_scalar_are_not_equal()
    {
        var a = new CountVectorizerOptions { StopWords = ["the"], MinDf = 0.5 };
        var b = new CountVectorizerOptions { StopWords = ["the"], MinDf = 0.2 };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Tfidf_options_inherit_the_fixed_comparison()
    {
        // The embedded CountVectorizerOptions is why this matters in practice.
        var a = new TfidfVectorizerOptions { Count = new CountVectorizerOptions { StopWords = ["the", "a"] } };
        var b = new TfidfVectorizerOptions { Count = new CountVectorizerOptions { StopWords = ["the", "a"] } };

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
