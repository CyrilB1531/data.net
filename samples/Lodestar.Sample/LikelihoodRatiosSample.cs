using Lodestar.Metrics;

namespace Lodestar.Sample;

/// <summary>The one classification pair that does not move with how rare the class is.</summary>
internal static class LikelihoodRatiosSample
{
    public static void Run()
    {
        Console.WriteLine("  likelihood ratios — how far a prediction should move a belief");

        int[] truth = [0, 1, 1, 0, 1, 0];
        int[] predicted = [0, 1, 0, 0, 1, 1];

        LikelihoodRatios ratios = LikelihoodRatios.Compute(truth, predicted);
        Console.WriteLine($"    LR+                 = {Inv.F3(ratios.Positive)} (a positive doubles the odds)");
        Console.WriteLine($"    LR-                 = {Inv.F3(ratios.Negative)} (a negative halves them)");

        // Rarer, but the three added negatives keep this fixture's two-to-one split,
        // so specificity holds: precision falls and the ratios do not move.
        int[] rarer = [0, 1, 1, 0, 1, 0, 0, 0, 0];
        int[] rarerPredicted = [0, 1, 0, 0, 1, 1, 0, 0, 1];
        LikelihoodRatios unchanged = LikelihoodRatios.Compute(rarer, rarerPredicted);
        Console.WriteLine($"    rarer class, LR+    = {Inv.F3(unchanged.Positive)} (unmoved)");
        Console.WriteLine($"    rarer class, prec.  = {Inv.F3(Precision.Score(rarer, rarerPredicted))} "
            + $"(was {Inv.F3(Precision.Score(truth, predicted))})");

        // A truth with no positive sample keeps no value for either ratio, and will
        // not take a replacement -- unlike the other three undefined shapes.
        LikelihoodRatios none = LikelihoodRatios.Compute([0, 0], [0, 1], 1, 1.0, 1.0);
        Console.WriteLine($"    no positive sample  = ({Inv.F3(none.Positive)}, {Inv.F3(none.Negative)}), "
            + "the replacement refused");
        Console.WriteLine();
    }
}
