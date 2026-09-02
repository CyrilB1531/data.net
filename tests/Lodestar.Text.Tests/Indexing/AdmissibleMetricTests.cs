using Lodestar.Text.Distances;
using Xunit;

namespace Lodestar.Text.Tests.Indexing;

/// <summary>
/// The correctness precondition every <c>BkTree</c> factory rests on: the triangle
/// inequality, checked exhaustively -- not sampled. Two sweeps, both exhaustive over
/// every triple: the 121 words up to length 4 on the three-letter alphabet <c>"abc"</c>
/// (1 771 561 triples per metric), and the 127 words up to length 6 on the two-letter
/// alphabet <c>"ab"</c> (2 048 383 triples per metric) -- both counts matching the
/// reference page's own claim. <c>Osa</c> is checked too, for the opposite reason:
/// excluded because it fails this same property.
/// </summary>
public sealed class AdmissibleMetricTests
{
    private const string ThreeLetterAlphabet = "abc";
    private const int ThreeLetterMaxLength = 4;

    private const string TwoLetterAlphabet = "ab";
    private const int TwoLetterMaxLength = 6;

    private static readonly List<string> ThreeLetterWords = AllWordsUpTo(ThreeLetterAlphabet, ThreeLetterMaxLength);
    private static readonly List<string> TwoLetterWords = AllWordsUpTo(TwoLetterAlphabet, TwoLetterMaxLength);

    private static List<string> AllWordsUpTo(string alphabet, int maxLength)
    {
        var words = new List<string> { string.Empty };
        List<string> frontier = words;

        for (int length = 1; length <= maxLength; length++)
        {
            var next = new List<string>(frontier.Count * alphabet.Length);
            foreach (string prefix in frontier)
            {
                foreach (char c in alphabet)
                {
                    next.Add(prefix + c);
                }
            }

            words.AddRange(next);
            frontier = next;
        }

        return words;
    }

    private static void AssertTriangleInequalityExhaustively(Func<string, string, int> metric, List<string> words)
    {
        int n = words.Count;
        var distance = new int[n][];
        for (int i = 0; i < n; i++)
        {
            distance[i] = new int[n];
            for (int j = 0; j < n; j++)
            {
                distance[i][j] = metric(words[i], words[j]);
            }
        }

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                for (int k = 0; k < n; k++)
                {
                    int dac = distance[i][k];
                    int dab = distance[i][j];
                    int dbc = distance[j][k];

                    // Built only on failure: eager evaluation as an Assert.True argument would
                    // cost several million wasted allocations across the four metrics and two sweeps.
                    if (dac > dab + dbc)
                    {
                        Assert.Fail(
                            $"triangle inequality: d(\"{words[i]}\",\"{words[k]}\")={dac} > " +
                            $"d(\"{words[i]}\",\"{words[j]}\")={dab} + d(\"{words[j]}\",\"{words[k]}\")={dbc}");
                    }
                }
            }
        }
    }

    [Fact]
    public void Levenshtein_satisfies_the_triangle_inequality_exhaustively_three_letter_alphabet() =>
        AssertTriangleInequalityExhaustively(static (a, b) => Levenshtein.Distance(a, b), ThreeLetterWords);

    [Fact]
    public void DamerauLevenshtein_satisfies_the_triangle_inequality_exhaustively_three_letter_alphabet() =>
        AssertTriangleInequalityExhaustively(static (a, b) => DamerauLevenshtein.Distance(a, b), ThreeLetterWords);

    [Fact]
    public void Indel_satisfies_the_triangle_inequality_exhaustively_three_letter_alphabet() =>
        AssertTriangleInequalityExhaustively(static (a, b) => Indel.Distance(a, b), ThreeLetterWords);

    [Fact]
    public void Hamming_satisfies_the_triangle_inequality_exhaustively_three_letter_alphabet() =>
        AssertTriangleInequalityExhaustively(static (a, b) => Hamming.Distance(a, b), ThreeLetterWords);

    [Fact]
    public void Levenshtein_satisfies_the_triangle_inequality_exhaustively_two_letter_alphabet() =>
        AssertTriangleInequalityExhaustively(static (a, b) => Levenshtein.Distance(a, b), TwoLetterWords);

    [Fact]
    public void DamerauLevenshtein_satisfies_the_triangle_inequality_exhaustively_two_letter_alphabet() =>
        AssertTriangleInequalityExhaustively(static (a, b) => DamerauLevenshtein.Distance(a, b), TwoLetterWords);

    [Fact]
    public void Indel_satisfies_the_triangle_inequality_exhaustively_two_letter_alphabet() =>
        AssertTriangleInequalityExhaustively(static (a, b) => Indel.Distance(a, b), TwoLetterWords);

    [Fact]
    public void Hamming_satisfies_the_triangle_inequality_exhaustively_two_letter_alphabet() =>
        AssertTriangleInequalityExhaustively(static (a, b) => Hamming.Distance(a, b), TwoLetterWords);

    [Fact]
    public void Osa_violates_the_triangle_inequality()
    {
        int dac = Osa.Distance("ab", "bca");
        int dab = Osa.Distance("ab", "ba");
        int dbc = Osa.Distance("ba", "bca");

        Assert.True(dac > dab + dbc,
            $"expected a violation: d(\"ab\",\"bca\")={dac} should exceed " +
            $"d(\"ab\",\"ba\")={dab} + d(\"ba\",\"bca\")={dbc}");
        Assert.Equal(3, dac);
        Assert.Equal(2, dab + dbc);
    }
}
