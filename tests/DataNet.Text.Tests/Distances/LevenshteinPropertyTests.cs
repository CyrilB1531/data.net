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
            // Named x/y/z rather than a/b/c: the symmetry check deliberately swaps the
            // arguments, which reads as a mistake when the locals mirror the parameters.
            string x = RandomString(rng, alphabet, 8);
            string y = RandomString(rng, alphabet, 8);
            string z = RandomString(rng, alphabet, 8);

            int dab = Levenshtein.Distance(x, y);
            int dba = Levenshtein.Distance(y, x);
            int dbc = Levenshtein.Distance(y, z);
            int dac = Levenshtein.Distance(x, z);

            Assert.True(dab >= 0, "non-negativity");
            Assert.Equal(0, Levenshtein.Distance(x, x)); // identity (reflexive)
            Assert.True(dab == 0 == string.Equals(x, y, StringComparison.Ordinal),
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
