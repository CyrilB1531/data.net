using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>The two internal-validity metrics against the frozen corpus.</summary>
public sealed class InternalValidityTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("internal_validity.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("cases").EnumerateArray()];

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_sklearn_on_every_case(int index)
    {
        JsonElement c = Cases[index];
        int[] labels = MetricsCorpus.Ints(c, "labels");
        double[] features = MetricsCorpus.Doubles(c, "features");
        int featureCount = c.GetProperty("feature_count").GetInt32();

        Assert.Equal(c.GetProperty("calinski_harabasz").GetDouble(),
                     CalinskiHarabasz.Score(labels, features, featureCount),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("davies_bouldin").GetDouble(),
                     DaviesBouldin.Score(labels, features, featureCount),
                     MetricsCorpus.Tolerance);
    }

    // Neither returns a non-finite value on any input the reference accepts, which is
    // what #192 asked and no metric in this package does today.
    [Theory]
    [MemberData(nameof(Indices))]
    public void Never_answers_a_non_finite_value(int index)
    {
        JsonElement c = Cases[index];
        int[] labels = MetricsCorpus.Ints(c, "labels");
        double[] features = MetricsCorpus.Doubles(c, "features");
        int featureCount = c.GetProperty("feature_count").GetInt32();

        Assert.True(double.IsFinite(CalinskiHarabasz.Score(labels, features, featureCount)));
        Assert.True(double.IsFinite(DaviesBouldin.Score(labels, features, featureCount)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void Refuses_a_label_count_outside_the_scorable_range(int clusters)
    {
        double[] features = [1.0, 2.0, 1.5, 1.8, 5.0, 8.0, 8.0, 8.0, 1.0, 0.6, 9.0, 11.0];
        int[] labels = clusters == 1 ? [0, 0, 0, 0, 0, 0] : [0, 1, 2, 3, 4, 5];

        foreach (var score in new Func<double>[]
        {
            () => CalinskiHarabasz.Score(labels, features, 2),
            () => DaviesBouldin.Score(labels, features, 2),
            () => Silhouette.Score(labels, features, 2),
        })
        {
            var error = Assert.Throws<ArgumentException>(() => score());
            Assert.Contains("Valid values are 2 to n_samples - 1", error.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Refuses_a_feature_block_that_is_not_a_whole_number_of_samples()
    {
        int[] labels = [0, 0, 1];
        double[] features = [1.0, 2.0, 3.0, 4.0];

        Assert.Throws<ArgumentException>(() => CalinskiHarabasz.Score(labels, features, 2));
        Assert.Throws<ArgumentException>(() => DaviesBouldin.Score(labels, features, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => DaviesBouldin.Score(labels, features, 0));
    }

    // The two disagree by construction, and a reader who takes them for interchangeable
    // will read one of them backwards.
    [Fact]
    public void Reads_in_opposite_directions()
    {
        double[] features = [0.0, 0.0, 0.1, 0.1, 10.0, 10.0, 10.1, 10.1];
        int[] tight = [0, 0, 1, 1];
        int[] mixed = [0, 1, 0, 1];

        Assert.True(CalinskiHarabasz.Score(tight, features, 2) > CalinskiHarabasz.Score(mixed, features, 2));
        Assert.True(DaviesBouldin.Score(tight, features, 2) < DaviesBouldin.Score(mixed, features, 2));
    }
}
