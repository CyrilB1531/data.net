using Lodestar.Abstractions;
using Lodestar.Decomposition;

namespace Lodestar.Sample;

/// <summary>
/// Latent semantic analysis over a tiny term-document matrix: five documents, six terms, and
/// nothing centred — which is the whole reason a sparse matrix can be factorized at all.
/// </summary>
internal static class TruncatedSvdSample
{
    // Five documents over six terms, built by hand so the shape is readable: term 0-2 belong to
    // one subject and term 3-5 to another, with document 2 straddling both.
    private static readonly double[] Values =
        [2.0, 1.0, 3.0, 1.0, 2.0, 1.0, 1.0, 1.0, 2.0, 1.0, 3.0, 2.0, 1.0, 2.0];
    private static readonly int[] Columns = [0, 1, 0, 2, 1, 2, 3, 4, 3, 5, 0, 2, 4, 5];
    private static readonly int[] Rows = [0, 2, 4, 8, 10, 14];

    public static void Run()
    {
        CsrMatrix corpus = new(5, 6, Values, Columns, Rows);
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
