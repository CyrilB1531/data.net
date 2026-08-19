using Lodestar.Metrics;

namespace Lodestar.Sample;

/// <summary>The one metric that reads a decision function rather than a label or a probability.</summary>
internal static class HingeLossSample
{
    public static void Run()
    {
        Console.WriteLine("  hinge loss — a decision function, and a margin of one");

        int[] truth = [-1, 1, 1, -1];
        double[] decisions = [-0.5, 1.2, 0.3, 0.8];

        Console.WriteLine($"    binary              = {Inv.F3(HingeLoss.Score(truth, decisions))}");

        // Only the sign is compared against the label, so relabelling moves nothing.
        Console.WriteLine($"    labels 0 and 1      = {Inv.F3(HingeLoss.Score([0, 1, 1, 0], decisions))} (the same)");

        // Right, but inside the margin: still charged, where an error count is free.
        int[] both = [1, 1];
        double[] barely = [0.2, 0.2];
        Console.WriteLine($"    right but barely    = {Inv.F3(HingeLoss.Score(both, barely))} hinge, "
            + $"{Inv.F3(ZeroOneLoss.Score(both, [1, 1]))} zero-one");

        // One decision per class: the margin is the true class against its best rival.
        int[] classes = [0, 1, 2, 1];
        double[] perClass =
        [
            1.2, 0.3, -0.5,
            0.1, 0.9, 0.2,
            0.4, 0.2, 0.7,
            0.3, 0.1, 0.6,
        ];
        Console.WriteLine($"    multiclass          = {Inv.F3(HingeLoss.MultiClass(classes, perClass, 3))}");
        Console.WriteLine();
    }
}
