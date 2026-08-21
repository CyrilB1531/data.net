namespace Lodestar.Text.Benchmarks;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

/// <summary>The operand pair whose differing middle is a parameter rather than an accident.</summary>
/// <remarks>
/// What the gate benchmarks need and <see cref="ScatteredPair"/> cannot give them: a pair
/// whose band after <c>Affixes.Trim</c> is exactly the length asked for, so a row named for
/// one band measures that band. Shared for the reason <see cref="ScatteredPair"/> gives —
/// two kernels compared across two files are only comparable over one corpus, seed included.
/// </remarks>
internal static class BandedPair
{
    /// <summary>The shared affix length on each side, long enough that trimming is the common case.</summary>
    private const int Shared = 24;

    /// <summary>The seed both gate benchmarks pass, so their bands hold the same characters.</summary>
    internal const int GateSeed = 20260818;

    /// <summary>Two strings sharing a prefix and suffix around independent middles of <paramref name="band"/>.</summary>
    /// <remarks>
    /// The middles' first and last characters are forced apart: drawn independently they
    /// collide once in 27, and trimming then eats into the band and moves it below the gate.
    /// They are taken from <paramref name="alphabet"/> rather than written as literals, so the
    /// band holds one alphabet and not three Latin characters inside a CJK one (#383). The four
    /// it takes are <c>a</c>, <c>b</c>, <c>c</c> and <c>d</c> under the Latin default, which is
    /// what it forced before — so the figures #273 published still reproduce.
    /// </remarks>
    internal static (string A, string B) Build(int band, int seed = GateSeed, string? alphabet = null)
    {
        alphabet ??= Alphabets.Latin;
        var rng = new Random(seed);
        string prefix = Random(rng, Shared, alphabet);
        string suffix = Random(rng, Shared, alphabet);
        return (prefix + WithEnds(rng, band, alphabet, alphabet[0], alphabet[1]) + suffix,
                prefix + WithEnds(rng, band, alphabet, alphabet[2], alphabet[3]) + suffix);
    }

    /// <summary>A random middle whose first and last characters are imposed.</summary>
    private static string WithEnds(Random rng, int length, string alphabet, char first, char last)
    {
        char[] text = Random(rng, length, alphabet).ToCharArray();
        text[0] = first;
        text[^1] = last;
        return new string(text);
    }

    private static string Random(Random rng, int length, string alphabet)
    {
        char[] text = new char[length];
        for (int i = 0; i < length; i++)
        {
            text[i] = alphabet[rng.Next(alphabet.Length)];
        }

        return new string(text);
    }
}
