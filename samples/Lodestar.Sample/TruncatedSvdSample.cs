using Lodestar.Abstractions;
using Lodestar.Decomposition;

namespace Lodestar.Sample;

/// <summary>
/// Latent semantic analysis over a tiny term-document matrix: five documents, six terms, and
/// nothing centred — which is the whole reason a sparse matrix can be factorized at all.
/// </summary>
internal static class TruncatedSvdSample
{
    public static void Run()
    {
        CsrMatrix corpus = DecompositionCorpus.Documents();
        TruncatedSvd fitted = TruncatedSvd.Fit(corpus, componentCount: 2);

        Console.WriteLine($"  rank kept             = {fitted.ComponentCount} of {fitted.FeatureCount} terms");
        Console.WriteLine($"  singular values       = {Inv.List(fitted.SingularValues)}");
        Console.WriteLine($"  explained variance    = {Inv.List(fitted.ExplainedVariance)}");
        Console.WriteLine($"  ... as a ratio        = {Inv.List(fitted.ExplainedVarianceRatio)}");

        // The sum, not the order, is what says whether the rank is enough: an uncentred
        // factorization's first component carries the mean direction, whose variance is small.
        Console.WriteLine($"  variance covered      = {Inv.F3(fitted.ExplainedVarianceRatio.Sum())}");

        // One row of Components per component, FeatureCount long: what each term contributes.
        // The signs are pinned — largest-magnitude entry positive — not left to the solver.
        Console.WriteLine($"  first component       = {Inv.List(fitted.Components.Take(fitted.FeatureCount))}");

        double[] projected = fitted.Transform(corpus);
        Console.WriteLine($"  document 0 in 2-D     = {Inv.List(projected.Take(2))}");
        Console.WriteLine($"  document 2 in 2-D     = {Inv.List(projected.Skip(4).Take(2))}");
        Console.WriteLine();
    }
}
