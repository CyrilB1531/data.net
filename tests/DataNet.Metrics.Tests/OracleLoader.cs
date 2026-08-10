using System.Text.Json;

namespace DataNet.Metrics.Tests;

/// <summary>Minimal loader for the committed oracle JSON files.</summary>
internal static class OracleLoader
{
    public static JsonDocument Load(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "oracles", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Oracle '{fileName}' not found at '{path}'. Run tools/generate_oracles.py.", path);
        }
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    /// <summary>
    /// Reads an oracle number, decoding the strings the generator writes for
    /// values JSON cannot express.
    /// </summary>
    /// <remarks>
    /// The generator passes <c>allow_nan=False</c> so that a non-finite it did not
    /// encode on purpose fails generation rather than producing a file this loader
    /// would reject. The three names below are the only ones it writes.
    /// </remarks>
    public static double Number(JsonElement element) =>
        element.ValueKind == JsonValueKind.String
            ? element.GetString() switch
            {
                "NaN" => double.NaN,
                "Infinity" => double.PositiveInfinity,
                "-Infinity" => double.NegativeInfinity,
                // A discard, not a `string other` arm with a `null` one beside it:
                // GetString() returns null only for a Null element, which this
                // branch has already excluded, so that second arm was unreachable
                // and the analyser said so. The raw text names the offending value
                // whatever its kind, which is what a failing test needs to read.
                _ => throw new InvalidOperationException(
                    $"The oracle holds {element.GetRawText()} where a number belongs."),
            }
            : element.GetDouble();
}
