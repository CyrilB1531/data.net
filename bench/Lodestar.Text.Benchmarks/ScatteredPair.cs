namespace Lodestar.Text.Benchmarks;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

/// <summary>
/// The operand pair the distance benchmarks measure over.
/// </summary>
/// <remarks>
/// Deterministic, and differing in a few scattered positions — representative of
/// typo and near-duplicate matching. Shared rather than copied per benchmark
/// because comparing two distances is only meaningful over identical inputs, seed
/// included (#273).
/// </remarks>
internal static class ScatteredPair
{
    /// <summary>Builds two strings of <paramref name="length"/> differing in about a tenth of their positions.</summary>
    internal static (string A, string B) Build(int length, int seed = 42)
    {
        var rng = new Random(seed);
        const string alphabet = Alphabets.Latin;
        char[] a = new char[length];
        for (int i = 0; i < length; i++)
        {
            a[i] = alphabet[rng.Next(alphabet.Length)];
        }

        char[] b = (char[])a.Clone();
        for (int i = 0; i < Math.Max(1, length / 10); i++)
        {
            b[rng.Next(length)] = alphabet[rng.Next(alphabet.Length)];
        }

        return (new string(a), new string(b));
    }
}
