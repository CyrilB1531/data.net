using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

namespace Lodestar.Sample;

/// <summary>The IDF weighting, applied to counts someone else produced.</summary>
internal static class TfidfTransformerSample
{
    public static void Run()
    {
        var counts = new CountVectorizer(TextCorpus.Counting());
        counts.Fit(TextCorpus.Documents);

        var transformer = new TfidfTransformer(new TfidfOptions
        {
            Norm = SparseNorm.L2,
            SmoothIdf = true,
            SublinearTf = false,
            UseIdf = true,
        });
        CsrMatrix weighted = transformer.Fit(counts.Transform(TextCorpus.Documents))
            .Transform(counts.Transform(TextCorpus.Documents));

        Console.WriteLine($"  TfidfTransformer : {weighted.NonZeroCount} non-zeros, idf[0]={Inv.F4(transformer.Idf[0])}");

        CsrMatrix reweighted = new TfidfTransformer(new TfidfOptions { Norm = SparseNorm.L2 })
            .FitTransform(counts.Transform(TextCorpus.Documents));
        Console.WriteLine($"  FitTransform     : row 0 length {Inv.F4(reweighted.RowL2Norm(0))}");
    }
}
