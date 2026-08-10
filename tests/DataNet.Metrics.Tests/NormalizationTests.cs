using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class NormalizationTests
{
    [Fact]
    public void The_loader_decodes_a_non_finite_oracle_value()
    {
        // The corpus is strict JSON, so NaN travels as a string. Nothing else in
        // this repository's oracles has ever needed that, which is why the
        // decoding lives in one place instead of at each call site.
        using JsonDocument doc = JsonDocument.Parse("""{"a": "NaN", "b": 0.5, "c": "-Infinity"}""");
        JsonElement root = doc.RootElement;

        Assert.True(double.IsNaN(OracleLoader.Number(root.GetProperty("a"))));
        Assert.Equal(0.5, OracleLoader.Number(root.GetProperty("b")));
        Assert.True(double.IsNegativeInfinity(OracleLoader.Number(root.GetProperty("c"))));
    }
}
