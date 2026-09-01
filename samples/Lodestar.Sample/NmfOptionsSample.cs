using Lodestar.Abstractions;
using Lodestar.Decomposition;

namespace Lodestar.Sample;

/// <summary>
/// What the factorization is allowed to vary, and what each knob buys: the loss decides which
/// errors matter, the initialisation decides which entries can ever be non-zero, and the
/// tolerance decides whether the iteration count is a cap or an input.
/// </summary>
internal static class NmfOptionsSample
{
    public static void Run()
    {
        CsrMatrix corpus = DecompositionCorpus.Documents();

        // The two losses are not two routes to one answer, and the two numbers are not on one
        // scale: each is the divergence it minimised, so compare a loss against itself.
        foreach (NmfBetaLoss loss in new[] { NmfBetaLoss.Frobenius, NmfBetaLoss.KullbackLeibler })
        {
            Nmf fitted = Nmf.Fit(corpus, 2, new NmfOptions { BetaLoss = loss });
            Console.WriteLine(
                $"  {loss,-15}       = {Inv.F3(fitted.ReconstructionError)} after {fitted.Iterations} updates");
        }

        // NndSvd leaves the zeros NNDSVD produced, and a multiplicative update can never revive
        // one — so this counts what the initialisation, not the corpus, decided to leave out.
        foreach (NmfInitialization start in new[] { NmfInitialization.NndSvd, NmfInitialization.NndSvda })
        {
            Nmf fitted = Nmf.Fit(corpus, 2, new NmfOptions { Initialization = start });
            int zeros = Zeros(fitted.Weights) + Zeros(fitted.Components);
            Console.WriteLine(
                $"  {start,-15}       = {Inv.F3(fitted.ReconstructionError)}, {zeros} zeros in W and H");
        }

        // Tolerance = 0 turns MaxIterations from a cap into the exact number of updates, which
        // is the setting that makes two implementations comparable step for step.
        NmfOptions exact = new() { MaxIterations = 25, Tolerance = 0.0, Seed = 20260901 };
        Nmf pinned = Nmf.Fit(corpus, 2, exact);
        Console.WriteLine($"  tol = 0, 25 updates   = {Inv.F3(pinned.ReconstructionError)} after {pinned.Iterations}");

        // Ω as an input rather than a seed: the initialisation's random block, written down, so
        // the fit is reproducible across implementations and not merely across runs.
        NmfOptions frozen = new() { RandomMatrix = Omega(6, 2 + 10), MaxIterations = 25, Tolerance = 0.0 };
        Console.WriteLine($"  over a frozen omega   = {Inv.F3(Nmf.Fit(corpus, 2, frozen).ReconstructionError)}");
        Console.WriteLine();
    }

    /// <summary>How many entries are exactly zero, which is the number the initialisation decided.</summary>
    // S1244: exact is what is meant. A multiplicative update scales every entry, so a zero
    // is the one value it can never leave, and a near-zero is an ordinary small weight.
#pragma warning disable S1244
    private static int Zeros(IEnumerable<double> block) => block.Count(value => value == 0);
#pragma warning restore S1244

    /// <summary>A block written down rather than drawn, so the run is portable as well as repeatable.</summary>
    private static double[] Omega(int features, int width)
    {
        double[] omega = new double[features * width];
        for (int i = 0; i < omega.Length; i++)
        {
            omega[i] = (((i * 7) % 13) - 6) / 6.0;
        }
        return omega;
    }
}
