namespace Lodestar.Decomposition.Internal;

/// <summary>Standard normal draws from an <see cref="int"/> seed, reproducible everywhere.</summary>
/// <remarks>
/// <see cref="Random"/> is not the answer: its algorithm changed in .NET 6, so the same seed
/// gives different numbers on .NET Framework and on net10.0 — and this package ships to both.
/// A seed reproduces a run of <em>this</em> library and nothing else, which is why the oracle
/// corpora pass Ω explicitly rather than seeding.
/// </remarks>
internal sealed class GaussianSampler
{
    private ulong _state;

    internal GaussianSampler(int seed) => _state = unchecked((ulong)seed + 0x9E3779B97F4A7C15UL);

    /// <summary>Draws a row-major block of independent standard normals.</summary>
    internal double[] Normal(int rows, int columns)
    {
        if (rows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), rows, "A block has at least one row.");
        }
        if (columns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), columns, "A block has at least one column.");
        }

        double[] block = new double[checked(rows * columns)];
        for (int i = 0; i < block.Length; i += 2)
        {
            // Box–Muller consumes two uniforms and yields two normals; the second is dropped
            // only when the block has an odd length.
            (double first, double second) = NextPair();
            block[i] = first;
            if (i + 1 < block.Length)
            {
                block[i + 1] = second;
            }
        }
        return block;
    }

    private (double First, double Second) NextPair()
    {
        // Radius zero would send Log to -infinity, so the uniform is drawn on (0, 1].
        double radius = Math.Sqrt(-2.0 * Math.Log(NextUnitInterval()));
        double angle = 2.0 * Math.PI * NextUnitInterval();
        return (radius * Math.Cos(angle), radius * Math.Sin(angle));
    }

    /// <summary>A uniform on <c>(0, 1]</c> — the 53 significant bits of a double.</summary>
    private double NextUnitInterval() => ((NextState() >> 11) + 1) * (1.0 / 9007199254740992.0);

    /// <summary>SplitMix64, whose whole state is one addition and three mixes.</summary>
    private ulong NextState()
    {
        unchecked
        {
            _state += 0x9E3779B97F4A7C15UL;
            ulong z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
