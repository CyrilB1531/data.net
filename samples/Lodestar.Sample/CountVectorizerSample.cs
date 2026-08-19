using Lodestar.Text.Vectorization;

namespace Lodestar.Sample;

/// <summary>Counts, and the vocabulary every later stage indexes by.</summary>
internal static class CountVectorizerSample
{
    public static void Run()
    {
        var counts = new CountVectorizer(TextCorpus.Counting());
        CsrMatrix matrix = counts.FitTransform(TextCorpus.Documents);

        Console.WriteLine($"  CountVectorizer  : {matrix.RowCount} x {matrix.ColumnCount}, {matrix.NonZeroCount} non-zeros");
        Console.WriteLine($"  first features   : {string.Join(", ", counts.GetFeatureNames().Take(4))}");

        // Fit alone, then Transform: the order a training corpus and a test one need.
        CsrMatrix learned = new CountVectorizer(TextCorpus.Counting()).Fit(TextCorpus.Documents).Transform(TextCorpus.Documents);
        Console.WriteLine($"  Fit then Transform: {learned.RowCount} x {learned.ColumnCount}");
    }
}
