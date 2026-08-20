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
    /// <summary>Latin, one byte a character: the alphabet every band used before #383.</summary>
    internal const string Latin = "abcdefghijklmnopqrstuvwxyz ";

    /// <summary>CJK, above Latin-1 and inside the BMP — one UTF-16 unit, and off the dense table.</summary>
    /// <remarks>
    /// 27 symbols, matching <see cref="Latin"/>, so a band built from either differs only in where
    /// its characters sit: the equality table for one, the side table for the other. Comparing the
    /// two bands is only meaningful if that is the single difference.
    /// </remarks>
    internal const string Cjk = "一二三四五六七八九十百千万上下左右前後東西南北中大小山";

    /// <summary>Builds two strings of <paramref name="length"/> differing in about a tenth of their positions.</summary>
    internal static (string A, string B) Build(int length, int seed = 42, string? alphabet = null)
    {
        var rng = new Random(seed);
        alphabet ??= Latin;
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
