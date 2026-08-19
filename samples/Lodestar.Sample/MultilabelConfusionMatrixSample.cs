using Lodestar.Metrics;

namespace Lodestar.Sample;

/// <summary>One 2×2 matrix per label, or per sample — and each one an ordinary matrix.</summary>
internal static class MultilabelConfusionMatrixSample
{
    public static void Run()
    {
        Console.WriteLine("  multilabel confusion — a stack of matrices, not a new type");

        bool[] truth = [true, false, true, false, true, true];
        bool[] predicted = [true, false, false, true, true, true];

        ConfusionMatrix[] perLabel = MultilabelConfusionMatrix.Compute(truth, predicted, 3);
        ConfusionMatrix[] perSample = MultilabelConfusionMatrix.Compute(truth, predicted, 3, samplewise: true);
        Console.WriteLine($"    per label           = {perLabel.Length} matrices");
        Console.WriteLine($"    samplewise          = {perSample.Length} matrices (one per row)");

        // Each entry is a ConfusionMatrix, so every reader of one reads these.
        double[,] first = perLabel[0].ToArray();
        Console.WriteLine($"    label 0             = [[{Inv.F0(first[0, 0])}, {Inv.F0(first[0, 1])}], "
            + $"[{Inv.F0(first[1, 0])}, {Inv.F0(first[1, 1])}]] (tn, fp / fn, tp)");

        // Single-label input gives one matrix per class, one against all the rest.
        int[] classes = [0, 1, 2, 1];
        int[] guessed = [0, 2, 2, 1];
        ConfusionMatrix[] perClass = MultilabelConfusionMatrix.Compute(classes, guessed);
        Console.WriteLine($"    per class           = {perClass.Length} matrices, "
            + $"class 1 recall = {Inv.F3(Recall.Score(perClass[1], Averaging.Binary))}");
        Console.WriteLine();
    }
}
