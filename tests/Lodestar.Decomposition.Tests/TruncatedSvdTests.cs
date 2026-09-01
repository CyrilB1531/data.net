using System.Text.Json;
using Lodestar.Abstractions;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// Randomized SVD against scikit-learn 1.9.0, over the Ω the corpus freezes. Ω is an input on
/// both sides, so this is an ordinary parity comparison and not a subspace one.
/// </summary>
public sealed class TruncatedSvdTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("decomposition_svd.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("randomized").EnumerateArray()];

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }
        return data;
    }

    private static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    private static int[] Ints(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetInt32())];

    private static CsrMatrix Matrix(JsonElement c) => new(
        c.GetProperty("rows").GetInt32(),
        c.GetProperty("columns").GetInt32(),
        Doubles(c, "values"),
        Ints(c, "column_indices"),
        Ints(c, "row_pointers"));

    private static PowerIterationNormalizer Normalizer(JsonElement c) =>
        c.GetProperty("normalizer").GetString() switch
        {
            "QR" => PowerIterationNormalizer.Qr,
            "LU" => PowerIterationNormalizer.Lu,
            "none" => PowerIterationNormalizer.None,
            _ => PowerIterationNormalizer.Auto,
        };

    private static TruncatedSvd Fit(JsonElement c) => TruncatedSvd.Fit(
        Matrix(c),
        c.GetProperty("component_count").GetInt32(),
        new TruncatedSvdOptions
        {
            Oversampling = c.GetProperty("oversampling").GetInt32(),
            PowerIterations = c.GetProperty("power_iterations").GetInt32(),
            Normalizer = Normalizer(c),
            RandomMatrix = Doubles(c, "omega"),
        });

    private static void AssertSame(double[] expected, IReadOnlyList<double> actual)
    {
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_singular_values_match_scikit_learn(int index)
    {
        JsonElement c = Cases[index];
        int k = c.GetProperty("component_count").GetInt32();

        AssertSame([.. Doubles(c, "singular_values").Take(k)], Fit(c).SingularValues);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_components_match_scikit_learn(int index)
    {
        JsonElement c = Cases[index];
        int k = c.GetProperty("component_count").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();

        AssertSame([.. Doubles(c, "components").Take(k * columns)], Fit(c).Components);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_explained_variance_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "explained_variance"), Fit(c).ExplainedVariance);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_explained_variance_ratio_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "explained_variance_ratio"), Fit(c).ExplainedVarianceRatio);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_transform_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "transform"), Fit(c).Transform(Matrix(c)));
    }

    /// <summary>The corpus's case 0, refitted at a chosen iteration count and normalizer.</summary>
    private static TruncatedSvd Refit(int powerIterations, PowerIterationNormalizer normalizer)
    {
        JsonElement c = Cases[0];
        return TruncatedSvd.Fit(
            Matrix(c),
            c.GetProperty("component_count").GetInt32(),
            new TruncatedSvdOptions
            {
                Oversampling = c.GetProperty("oversampling").GetInt32(),
                PowerIterations = powerIterations,
                Normalizer = normalizer,
                RandomMatrix = Doubles(c, "omega"),
            });
    }

    // Bit equality, not 1e-9: the rule says which code path runs, and the two paths agree to
    // 1e-14 anyway, so a tolerance would pass whichever branch Auto took.
    [Fact]
    public void Auto_applies_no_normalizer_below_three_power_iterations()
    {
        TruncatedSvd auto = Refit(2, PowerIterationNormalizer.Auto);

        Assert.Equal(Refit(2, PowerIterationNormalizer.None).SingularValues, auto.SingularValues);
        Assert.Equal(Refit(2, PowerIterationNormalizer.None).Components, auto.Components);
        Assert.NotEqual(Refit(2, PowerIterationNormalizer.Lu).SingularValues, auto.SingularValues);
    }

    [Fact]
    public void Auto_applies_the_lu_normalizer_from_three_power_iterations_up()
    {
        TruncatedSvd auto = Refit(3, PowerIterationNormalizer.Auto);

        Assert.Equal(Refit(3, PowerIterationNormalizer.Lu).SingularValues, auto.SingularValues);
        Assert.Equal(Refit(3, PowerIterationNormalizer.Lu).Components, auto.Components);
        Assert.NotEqual(Refit(3, PowerIterationNormalizer.None).SingularValues, auto.SingularValues);
    }

    [Fact]
    public void A_seed_draws_the_matrix_when_none_is_given()
    {
        JsonElement c = Cases[0];
        TruncatedSvdOptions options = new() { Seed = 20260901 };

        TruncatedSvd first = TruncatedSvd.Fit(Matrix(c), 3, options);
        TruncatedSvd second = TruncatedSvd.Fit(Matrix(c), 3, options);

        Assert.Equal(first.SingularValues, second.SingularValues);
    }

    [Fact]
    public void A_component_count_at_or_above_the_feature_count_is_refused()
    {
        CsrMatrix matrix = Matrix(Cases[0]);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TruncatedSvd.Fit(matrix, matrix.ColumnCount));
    }

    [Fact]
    public void A_rank_above_the_row_count_is_refused()
    {
        // scikit-learn accepts it; here the range finder narrows the basis below k and the
        // truncation would throw out of Array.Copy, so it is refused where a caller can read it.
        CsrMatrix wide = new(2, 5, [1.0, 2.0, 3.0, 4.0], [0, 2, 1, 4], [0, 2, 4]);

        Assert.Throws<ArgumentOutOfRangeException>(() => TruncatedSvd.Fit(wide, 3));
    }

    [Fact]
    public void A_random_matrix_of_the_wrong_shape_is_refused()
    {
        CsrMatrix matrix = Matrix(Cases[0]);

        Assert.Throws<ArgumentException>(() => TruncatedSvd.Fit(
            matrix, 4, new TruncatedSvdOptions { RandomMatrix = new double[7] }));
    }

    [Fact]
    public void Transforming_a_matrix_of_another_width_is_refused()
    {
        JsonElement c = Cases[0];
        TruncatedSvd fitted = Fit(c);
        CsrMatrix narrower = new(2, 3, [1.0], [0], [0, 1, 1]);

        Assert.Throws<ArgumentException>(() => fitted.Transform(narrower));
    }

    [Fact]
    public void A_negative_oversampling_is_refused()
    {
        CsrMatrix matrix = Matrix(Cases[0]);

        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(
            () => TruncatedSvd.Fit(matrix, 4, new TruncatedSvdOptions { Oversampling = -1 }));

        // The name a caller reading ParamName can find in Fit's signature, not the private one.
        Assert.Equal("options", thrown.ParamName);
    }

    [Fact]
    public void An_oversampling_that_will_not_add_to_the_rank_is_refused()
    {
        CsrMatrix matrix = Matrix(Cases[0]);

        Assert.Throws<ArgumentOutOfRangeException>(() => TruncatedSvd.Fit(
            matrix, 4, new TruncatedSvdOptions { Oversampling = int.MaxValue }));
    }

    [Fact]
    public void A_negative_power_iteration_count_is_refused()
    {
        CsrMatrix matrix = Matrix(Cases[0]);

        ArgumentOutOfRangeException thrown = Assert.Throws<ArgumentOutOfRangeException>(
            () => TruncatedSvd.Fit(matrix, 4, new TruncatedSvdOptions { PowerIterations = -1 }));

        Assert.Equal("options", thrown.ParamName);
    }

    [Fact]
    public void Fitting_a_null_matrix_is_refused()
    {
        Assert.Throws<ArgumentNullException>(() => TruncatedSvd.Fit(null!, 2));
    }

    [Fact]
    public void Transforming_a_null_matrix_is_refused()
    {
        TruncatedSvd fitted = Fit(Cases[0]);

        Assert.Throws<ArgumentNullException>(() => fitted.Transform(null!));
    }
}
