using Lodestar.Abstractions;
using Lodestar.Decomposition;

namespace Lodestar.Sample;

/// <summary>
/// A parts-based decomposition of the same tiny corpus the SVD sample factorizes: two
/// components, neither of which is allowed to subtract a word from a document.
/// </summary>
internal static class NmfSample
{
    public static void Run()
    {
        CsrMatrix corpus = DecompositionCorpus.Documents();
        Nmf fitted = Nmf.Fit(corpus, componentCount: 2);

        Console.WriteLine($"  rank kept             = {fitted.ComponentCount} of {fitted.FeatureCount} terms");
        Console.WriteLine($"  updates run           = {fitted.Iterations}");
        Console.WriteLine($"  reconstruction error  = {Inv.F3(fitted.ReconstructionError)}");

        // One row of Components per component, FeatureCount long. Nothing subtracts, so a
        // component reads as the set of terms that carry weight in it.
        Console.WriteLine($"  component 0 over terms= {Inv.List(fitted.Components.Take(fitted.FeatureCount))}");
        Console.WriteLine($"  component 1 over terms= {Inv.List(fitted.Components.Skip(fitted.FeatureCount))}");

        // One row of Weights per document: how much of each component it is made of.
        Console.WriteLine($"  document 0 as a mix   = {Inv.List(fitted.Weights.Take(2))}");
        Console.WriteLine($"  document 2 as a mix   = {Inv.List(fitted.Weights.Skip(4).Take(2))}");

        // The other overload runs the same loop on an initialisation written down instead of
        // computed, which is what makes a run comparable against another implementation's.
        double[] initialWeights = [1.0, 0.5, 0.5, 1.0, 0.8, 0.8, 1.0, 0.2, 0.4, 0.9];
        double[] initialComponents = [1.0, 0.5, 1.0, 0.5, 1.0, 1.0, 0.5, 1.0, 0.5, 1.0, 1.0, 0.5];
        Nmf written = Nmf.Fit(
            corpus, initialWeights, initialComponents,
            new NmfOptions { MaxIterations = 100, Tolerance = 0.0 });

        Console.WriteLine($"  from a written W0, H0 = {Inv.F3(written.ReconstructionError)} after {written.Iterations}");
        Console.WriteLine();
    }
}
