namespace Lodestar.Sample;

/// <summary>Every Lodestar.Decomposition sample, in the order a reader meets the package.</summary>
/// <remarks>
/// Not named <c>*Sample.cs</c> on purpose: <c>tools/check_sample_coverage.py</c> reads that
/// suffix as the example of a class of the same name, and this file demonstrates none.
/// </remarks>
internal static class DecompositionSamples
{
    public static void Run()
    {
        Console.WriteLine("Lodestar.Decomposition");
        Console.WriteLine("  truncated SVD — latent semantic analysis, nothing centred");
        TruncatedSvdSample.Run();
        Console.WriteLine("  what the randomized solver is allowed to vary");
        TruncatedSvdOptionsSample.Run();
        Console.WriteLine("  non-negative matrix factorization — parts, not directions");
        NmfSample.Run();
        Console.WriteLine("  what the factorization is allowed to vary");
        NmfOptionsSample.Run();
    }
}
