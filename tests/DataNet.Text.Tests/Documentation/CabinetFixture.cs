namespace DataNet.Tests.Documentation.Fixtures;

/// <summary>A namespace shaped like the two cases D7's refinements carve out.</summary>
/// <remarks>
/// <c>DataNet.Metrics</c> forced both, and <c>DataNet.Text.Distances</c> exports neither a
/// nested type nor a record, so the rules would go unmeasured against a real assembly.
/// This one is real — the test assembly itself — so <c>Check</c> reflects over it exactly
/// as over a shipped one. <see cref="Measure"/> and <see cref="Grid"/> pin the array
/// spellings besides, which no member of the distances page has.
/// </remarks>
public static class Cabinet
{
    /// <summary>One number per drawer, which is a <c>double[]</c> and not a <c>Double[]</c>.</summary>
    public static double[] Measure(int count) => new double[count];

    // CA1814 asks for a jagged array. The shape under test is the one
    // ConfusionMatrix.ToArray really returns, and swapping it for double[][]
    // would leave the rank-2 spelling unmeasured — which is the point of this member.
#pragma warning disable CA1814
    /// <summary>A rectangular reading, which is a <c>double[,]</c>.</summary>
    public static double[,] Grid(int rows) => new double[rows, rows];
#pragma warning restore CA1814

    // CA1034 asks for this not to be nested and publicly visible. Being exactly
    // that is what the rule above it exists to exempt, so un-nesting it would
    // delete the case rather than fix it.
#pragma warning disable CA1034
    /// <summary>Nested and exported, and therefore owed no entry of its own.</summary>
    public sealed record Drawer(int Depth);
#pragma warning restore CA1034
}

/// <summary>A record, whose six synthesised methods are owed no entry either.</summary>
public sealed record Slip(string Name, double Weight);
