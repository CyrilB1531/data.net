using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

namespace Lodestar.Sample;

/// <summary>The bucket count, and the sign trick that keeps collisions from only adding.</summary>
internal static class HashingVectorizerOptionsSample
{
    public static void Run()
    {
        var options = new HashingVectorizerOptions
        {
            NumFeatures = 1 << 10,
            AlternateSign = true,
            Norm = SparseNorm.L2,
            Count = TextCorpus.Counting(),
        };

        Console.WriteLine($"  HashingVectorizerOptions: buckets={options.NumFeatures}, alternateSign={options.AlternateSign}, norm={options.Norm}");
    }
}
