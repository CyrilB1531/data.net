using System.Text.Json;

namespace Lodestar.Stats.Tests.Oracles;

/// <summary>Loads a frozen stats corpus committed under <c>tests/oracles/</c>.</summary>
internal static class StatsCorpus
{
    internal static JsonDocument Load(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "oracles", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Oracle '{fileName}' not found at '{path}'. Run tools/generate_oracles.py.", path);
        }

        return JsonDocument.Parse(File.ReadAllText(path));
    }

    /// <summary>One corpus number, decoding the three non-finite spellings.</summary>
    /// <remarks>
    /// The generator writes with <c>allow_nan=False</c>, so a one-sided
    /// confidence bound and an infinite odds ratio arrive as the strings
    /// <c>"Infinity"</c>, <c>"-Infinity"</c> and <c>"NaN"</c> rather than as
    /// tokens no strict JSON reader accepts.
    /// </remarks>
    internal static double Number(JsonElement element) =>
        element.ValueKind == JsonValueKind.String
            ? element.GetString() switch
            {
                "Infinity" => double.PositiveInfinity,
                "-Infinity" => double.NegativeInfinity,
                "NaN" => double.NaN,
                var other => throw new InvalidDataException($"Unknown corpus number '{other}'."),
            }
            : element.GetDouble();

    internal static double[] Doubles(JsonElement element) =>
        [.. element.EnumerateArray().Select(Number)];

    internal static double[][] Table(JsonElement element) =>
        [.. element.EnumerateArray().Select(Doubles)];

    /// <summary>The <c>alternative</c> a case was generated with.</summary>
    /// <remarks>
    /// Here rather than in each replay: five of the ten families read the same
    /// three spellings, and a switch written five times is a switch that drifts
    /// four ways.
    /// </remarks>
    internal static Alternative Alternative(JsonElement args) =>
        args.GetProperty("alternative").GetString() switch
        {
            "two-sided" => Lodestar.Stats.Alternative.TwoSided,
            "less" => Lodestar.Stats.Alternative.Less,
            "greater" => Lodestar.Stats.Alternative.Greater,
            var other => throw new InvalidDataException($"Unknown alternative '{other}'."),
        };

    /// <summary>The <c>method</c> a case was generated with. scipy spells the KS one "asymp".</summary>
    internal static ExactMethod Method(JsonElement args) =>
        args.GetProperty("method").GetString() switch
        {
            "auto" => ExactMethod.Auto,
            "exact" => ExactMethod.Exact,
            "asymptotic" or "asymp" => ExactMethod.Asymptotic,
            var other => throw new InvalidDataException($"Unknown method '{other}'."),
        };
}
