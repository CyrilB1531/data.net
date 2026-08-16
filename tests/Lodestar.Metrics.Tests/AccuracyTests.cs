using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

public sealed class AccuracyTests
{
    [Theory]
    [MemberData(nameof(MetricsCorpus.Indices), MemberType = typeof(MetricsCorpus))]
    public void Matches_sklearn_accuracy_score(int index)
    {
        JsonElement c = MetricsCorpus.Cases[index];
        int[] yTrue = MetricsCorpus.Ints(c, "y_true");
        int[] yPred = MetricsCorpus.Ints(c, "y_pred");
        double[] weight = MetricsCorpus.OptionalDoubles(c, "sample_weight");

        Assert.Equal(c.GetProperty("accuracy").GetDouble(),
                     Accuracy.Score(yTrue, yPred, sampleWeight: weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("accuracy_count").GetDouble(),
                     Accuracy.Score(yTrue, yPred, normalize: false, sampleWeight: weight),
                     MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Reads_the_same_number_off_a_matrix_that_dropped_nothing()
    {
        int[] yTrue = [0, 1, 2, 2];
        int[] yPred = [0, 1, 2, 1];

        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred);

        Assert.Equal(Accuracy.Score(yTrue, yPred), Accuracy.Score(cm), MetricsCorpus.Tolerance);
    }
}
