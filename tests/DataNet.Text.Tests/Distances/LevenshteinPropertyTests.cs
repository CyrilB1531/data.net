using DataNet.Text.Distances;
using Xunit;

namespace DataNet.Text.Tests.Distances;

/// <summary>
/// Metric-property tests (§4). Levenshtein with unit costs is a true metric, so
/// it must satisfy non-negativity, identity of indiscernibles, symmetry and the
/// triangle inequality. Cases are drawn from a fixed seed for reproducibility.
/// </summary>
public sealed class LevenshteinPropertyTests
{
    private const int Seed = 20260801;
    private const int Trials = 5000;

    private static string RandomString(Random rng, string alphabet, int maxLen)
    {
        int len = rng.Next(0, maxLen + 1);
        return string.Create(len, (rng, alphabet), static (span, state) =>
        {
            (Random r, string alpha) = state;
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = alpha[r.Next(alpha.Length)];
            }
        });
    }

    [Fact]
    public void Satisfies_metric_axioms()
    {
        var rng = new Random(Seed);
        const string alphabet = "abcABC012"; // small alphabet -> frequent collisions

        for (int t = 0; t < Trials; t++)
        {
            string a = RandomString(rng, alphabet, 8);
            string b = RandomString(rng, alphabet, 8);
            string c = RandomString(rng, alphabet, 8);

            int dab = Levenshtein.Distance(a, b);
            int dba = Levenshtein.Distance(b, a);
            int dbc = Levenshtein.Distance(b, c);
            int dac = Levenshtein.Distance(a, c);

            Assert.True(dab >= 0, "non-negativity");
            Assert.Equal(0, Levenshtein.Distance(a, a)); // identity (reflexive)
            Assert.True(dab == 0 == string.Equals(a, b, StringComparison.Ordinal),
                "identity of indiscernibles"); // d==0 iff equal
            Assert.Equal(dab, dba); // symmetry
            Assert.True(dac <= dab + dbc,
                $"triangle inequality: d(a,c)={dac} > d(a,b)={dab} + d(b,c)={dbc}");
        }
    }

    [Fact]
    public void Distance_is_bounded_by_the_longer_length()
    {
        var rng = new Random(Seed + 1);
        const string alphabet = "xyz";

        for (int t = 0; t < Trials; t++)
        {
            string a = RandomString(rng, alphabet, 12);
            string b = RandomString(rng, alphabet, 12);
            int d = Levenshtein.Distance(a, b);

            Assert.True(d <= Math.Max(a.Length, b.Length), "upper bound = max length");
            Assert.True(d >= Math.Abs(a.Length - b.Length), "lower bound = length gap");
        }
    }
}
