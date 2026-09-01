using Lodestar.Abstractions;
using Lodestar.Decomposition;

namespace Lodestar.Sample;

/// <summary>
/// What the randomized solver is allowed to vary, and what each knob actually buys: oversampling
/// and power iterations move the answer, the normalizer guards it, and Ω decides whether a run is
/// reproducible only here or reproducible against scikit-learn too.
/// </summary>
internal static class TruncatedSvdOptionsSample
{
    // Twelve documents over ten terms, bigger than the corpus next door on purpose: a matrix
    // the probe block covers entirely is fitted exactly, and no setting below would move a digit.
    private static readonly double[] Values =
    [
        3.0, 2.0, 1.0, 2.0, 3.0, 1.0, 1.0, 4.0, 2.0, 2.0, 1.0, 3.0,
        1.0, 3.0, 2.0, 2.0, 2.0, 1.0, 3.0, 1.0, 1.0, 1.0, 2.0, 4.0,
        2.0, 1.0, 3.0, 1.0, 1.0, 2.0, 4.0, 2.0, 1.0, 2.0, 3.0, 1.0,
    ];
    private static readonly int[] Columns =
    [
        0, 1, 2, 0, 2, 3, 1, 2, 4, 3, 4, 5,
        4, 5, 6, 5, 6, 7, 6, 7, 8, 7, 8, 9,
        0, 4, 8, 1, 5, 9, 2, 6, 7, 0, 3, 9,
    ];
    private static readonly int[] Rows = [0, 3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36];

    public static void Run()
    {
        CsrMatrix corpus = new(12, 10, Values, Columns, Rows);

        Console.WriteLine($"  defaults              = {Fit(corpus, new TruncatedSvdOptions { Seed = 20260901 })}");

        // The two knobs that move the answer: a narrow probe block with one power iteration
        // reaches a visibly smaller third singular value than the defaults do.
        TruncatedSvdOptions coarse = new() { Seed = 20260901, Oversampling = 0, PowerIterations = 1 };
        Console.WriteLine($"  p = 0, one iteration  = {Fit(corpus, coarse)}");

        TruncatedSvdOptions patient = new() { Seed = 20260901, Oversampling = 2, PowerIterations = 8 };
        Console.WriteLine($"  p = 2, eight of them  = {Fit(corpus, patient)}");

        // All four agree here, which is the point: they guard one subspace against collapsing
        // rather than computing different ones — and scikit-learn's answer depends on which.
        foreach (PowerIterationNormalizer normalizer in new[]
                 {
                     PowerIterationNormalizer.Auto,
                     PowerIterationNormalizer.None,
                     PowerIterationNormalizer.Qr,
                     PowerIterationNormalizer.Lu,
                 })
        {
            TruncatedSvdOptions options = new() { Seed = 20260901, Normalizer = normalizer };
            Console.WriteLine($"  normalizer {normalizer,-6}     = {Fit(corpus, options)}");
        }

        // Ω as an input rather than a seed: the same block gives the same answer in any language,
        // which is how this package's components are compared to scikit-learn's entry by entry.
        Console.WriteLine($"  over a frozen omega   = {Fit(corpus, new TruncatedSvdOptions { Oversampling = 4, RandomMatrix = Omega(10, 3 + 4) })}");
        Console.WriteLine();
    }

    private static string Fit(CsrMatrix corpus, TruncatedSvdOptions options) =>
        Inv.List(TruncatedSvd.Fit(corpus, componentCount: 3, options).SingularValues);

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
