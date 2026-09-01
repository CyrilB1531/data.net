using System.Text.Json;
using Lodestar.Abstractions;
using Lodestar.Decomposition.Internal;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// <c>NMF(solver="mu")</c> against scikit-learn 1.9.0, from the W₀ and H₀ the corpus freezes.
/// </summary>
public sealed class NmfTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("decomposition_nmf.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("updates").EnumerateArray()];

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

    private static Nmf Fit(JsonElement c) => Nmf.Fit(
        Matrix(c),
        Doubles(c, "initial_w"),
        Doubles(c, "initial_h"),
        new NmfOptions
        {
            BetaLoss = c.GetProperty("beta_loss").GetString() == "kullback-leibler"
                ? NmfBetaLoss.KullbackLeibler
                : NmfBetaLoss.Frobenius,
            MaxIterations = c.GetProperty("max_iterations").GetInt32(),
            Tolerance = c.GetProperty("tolerance").GetDouble(),
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
    public void The_weights_match_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "weights"), Fit(c).Weights);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_components_match_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "components"), Fit(c).Components);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_iteration_count_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        Assert.Equal(c.GetProperty("iterations").GetInt32(), Fit(c).Iterations);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_reconstruction_error_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        Assert.Equal(
            c.GetProperty("reconstruction_error").GetDouble(), Fit(c).ReconstructionError, Tolerance);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Neither_factor_holds_a_negative_number(int index)
    {
        Nmf fitted = Fit(Cases[index]);

        Assert.All(fitted.Weights, value => Assert.True(value >= 0, $"W holds {value}"));
        Assert.All(fitted.Components, value => Assert.True(value >= 0, $"H holds {value}"));
    }

    [Fact]
    public void The_initialising_overload_reaches_the_same_answer()
    {
        // Fit(matrix, k) is Fit(matrix, W0, H0) with NNDSVD in front of it, and nothing else,
        // so the composition is built here and confronted rather than described.
        JsonElement c = Cases[0];
        CsrMatrix matrix = Matrix(c);
        int componentCount = c.GetProperty("component_count").GetInt32();
        NmfOptions options = new()
        {
            Initialization = NmfInitialization.NndSvda,
            MaxIterations = 20,
            Tolerance = 0.0,
            Seed = 20260901,
        };

        (double[] w0, double[] h0) = NndSvd.Initialize(
            matrix, componentCount, options.Initialization, options.Seed, options.RandomMatrix);
        Nmf composed = Nmf.Fit(matrix, componentCount, options);
        Nmf byHand = Nmf.Fit(matrix, w0, h0, options);

        Assert.Equal(20, composed.Iterations);
        Assert.Equal(byHand.Iterations, composed.Iterations);
        AssertSame([.. byHand.Weights], composed.Weights);
        AssertSame([.. byHand.Components], composed.Components);
    }

    [Fact]
    public void A_rank_above_the_row_count_is_refused()
    {
        // Three components of two rows survives the range finder and breaks the truncation
        // once the economic QR narrows the block, so it is refused before either happens.
        CsrMatrix wide = new(2, 5, [1.0, 2.0, 3.0, 4.0], [0, 2, 1, 4], [0, 2, 4]);

        Assert.Throws<ArgumentOutOfRangeException>(() => Nmf.Fit(wide, 3));
    }

    [Fact]
    public void An_omega_of_the_wrong_shape_is_refused()
    {
        JsonElement c = Cases[0];
        CsrMatrix matrix = Matrix(c);
        int k = c.GetProperty("component_count").GetInt32();
        NmfOptions options = new() { RandomMatrix = new double[matrix.ColumnCount * (k + 10) - 1] };

        Assert.Throws<ArgumentException>(() => Nmf.Fit(matrix, k, options));
    }

    [Fact]
    public void An_omega_of_the_right_shape_is_the_initialisation()
    {
        JsonElement c = Cases[0];
        CsrMatrix matrix = Matrix(c);
        int k = c.GetProperty("component_count").GetInt32();
        double[] omega = new double[matrix.ColumnCount * (k + 10)];
        for (int i = 0; i < omega.Length; i++)
        {
            omega[i] = (((i * 7) % 13) - 6) / 6.0;
        }

        Nmf fitted = Nmf.Fit(matrix, k, new NmfOptions { MaxIterations = 5, Tolerance = 0.0, RandomMatrix = omega });

        Assert.Equal(5, fitted.Iterations);
        Assert.Equal(k, fitted.ComponentCount);
        Assert.Equal(matrix.ColumnCount, fitted.FeatureCount);
    }

    [Fact]
    public void An_initialisation_of_the_wrong_shape_is_refused()
    {
        CsrMatrix matrix = Matrix(Cases[0]);

        Assert.Throws<ArgumentException>(
            () => Nmf.Fit(matrix, new double[3], new double[matrix.ColumnCount]));
    }

    [Fact]
    public void A_negative_initialisation_is_refused()
    {
        JsonElement c = Cases[0];
        CsrMatrix matrix = Matrix(c);
        double[] w = Doubles(c, "initial_w");
        w[0] = -1.0;

        Assert.Throws<ArgumentException>(() => Nmf.Fit(matrix, w, Doubles(c, "initial_h")));
    }
}
