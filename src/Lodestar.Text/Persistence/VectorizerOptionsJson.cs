using System.Text.Json;
using DataNet.Internal.Persistence;
using Lodestar.Text.Vectorization;

namespace Lodestar.Text.Persistence;

/// <summary>
/// Reads and writes the option records of the vectorizers.
/// </summary>
/// <remarks>
/// Enumerations persist under their scikit-learn spelling — <c>word</c>/
/// <c>char</c>/<c>char_wb</c>, <c>l1</c>/<c>l2</c> — so an artifact reads like
/// the constructor call it mirrors. Stop words are written in ordinal order:
/// the option is a set, so only sorting makes the artifact byte-reproducible.
/// </remarks>
internal static class VectorizerOptionsJson
{
    public static void Write(Utf8JsonWriter writer, string propertyName, CountVectorizerOptions options)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteBoolean("lowercase", options.Lowercase);
        writer.WriteBoolean("stripAccents", options.StripAccents);
        writer.WriteString("analyzer", AnalyzerName(options.Analyzer));
        writer.WriteNumber("ngramMin", options.NgramRange.Min);
        writer.WriteNumber("ngramMax", options.NgramRange.Max);
        JsonArtifact.WriteExactDouble(writer, "minDf", options.MinDf);
        JsonArtifact.WriteExactDouble(writer, "maxDf", options.MaxDf);
        writer.WriteBoolean("binary", options.Binary);
        writer.WriteString("tokenPattern", options.TokenPattern);
        WriteStopWords(writer, options.StopWords);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, string propertyName, TfidfOptions options)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteBoolean("useIdf", options.UseIdf);
        writer.WriteBoolean("smoothIdf", options.SmoothIdf);
        writer.WriteBoolean("sublinearTf", options.SublinearTf);
        WriteNorm(writer, "norm", options.Norm);
        writer.WriteEndObject();
    }

    public static CountVectorizerOptions ReadCount(ref Utf8JsonReader reader, string artifact, in ArtifactLimits limits)
    {
        JsonArtifact.ReadStartObject(ref reader, artifact, "options");

        var result = new CountVectorizerOptions();
        int ngramMin = result.NgramRange.Min;
        int ngramMax = result.NgramRange.Max;

        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            string name = reader.GetString()!;
            switch (name)
            {
                case "lowercase":
                    result = result with { Lowercase = JsonArtifact.ReadBoolean(ref reader, artifact, name) };
                    break;
                case "stripAccents":
                    result = result with { StripAccents = JsonArtifact.ReadBoolean(ref reader, artifact, name) };
                    break;
                case "analyzer":
                    result = result with { Analyzer = ParseAnalyzer(JsonArtifact.ReadString(ref reader, artifact, name)) };
                    break;
                case "ngramMin":
                    ngramMin = JsonArtifact.ReadInt32(ref reader, artifact, name);
                    break;
                case "ngramMax":
                    ngramMax = JsonArtifact.ReadInt32(ref reader, artifact, name);
                    break;
                case "minDf":
                    result = result with { MinDf = JsonArtifact.ReadDouble(ref reader, artifact, name) };
                    break;
                case "maxDf":
                    result = result with { MaxDf = JsonArtifact.ReadDouble(ref reader, artifact, name) };
                    break;
                case "binary":
                    result = result with { Binary = JsonArtifact.ReadBoolean(ref reader, artifact, name) };
                    break;
                case "tokenPattern":
                    result = result with { TokenPattern = JsonArtifact.ReadString(ref reader, artifact, name) };
                    break;
                case "stopWords":
                    result = result with { StopWords = ReadStopWords(ref reader, artifact, limits) };
                    break;
                default:
                    throw JsonArtifact.UnknownProperty(artifact, "options." + name);
            }
        }

        EnsureEndOfObject(ref reader, artifact);
        if (ngramMin < 1 || ngramMax < ngramMin)
        {
            throw JsonArtifact.Inconsistent(artifact, $"n-gram range ({ngramMin}, {ngramMax}) is not a valid ascending range starting at 1 or more.");
        }
        return result with { NgramRange = (ngramMin, ngramMax) };
    }

    public static TfidfOptions ReadTfidf(ref Utf8JsonReader reader, string artifact)
    {
        JsonArtifact.ReadStartObject(ref reader, artifact, "tfidf");

        var result = new TfidfOptions();
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            string name = reader.GetString()!;
            switch (name)
            {
                case "useIdf":
                    result = result with { UseIdf = JsonArtifact.ReadBoolean(ref reader, artifact, name) };
                    break;
                case "smoothIdf":
                    result = result with { SmoothIdf = JsonArtifact.ReadBoolean(ref reader, artifact, name) };
                    break;
                case "sublinearTf":
                    result = result with { SublinearTf = JsonArtifact.ReadBoolean(ref reader, artifact, name) };
                    break;
                case "norm":
                    result = result with { Norm = ParseNorm(JsonArtifact.ReadNullableString(ref reader, artifact, name), artifact) };
                    break;
                default:
                    throw JsonArtifact.UnknownProperty(artifact, "tfidf." + name);
            }
        }

        EnsureEndOfObject(ref reader, artifact);
        return result;
    }

    public static void WriteNorm(Utf8JsonWriter writer, string propertyName, SparseNorm? norm)
    {
        if (norm is null)
        {
            writer.WriteNull(propertyName);
        }
        else
        {
            writer.WriteString(propertyName, norm == SparseNorm.L1 ? "l1" : "l2");
        }
    }

    public static SparseNorm? ParseNorm(string? value, string artifact) => value switch
    {
        null => null,
        "l1" => SparseNorm.L1,
        "l2" => SparseNorm.L2,
        _ => throw new InvalidDataException($"Unknown norm '{value}' in a '{artifact}' artifact; expected \"l1\", \"l2\" or null."),
    };

    internal static void EnsureEndOfObject(ref Utf8JsonReader reader, string artifact)
    {
        if (reader.TokenType != JsonTokenType.EndObject)
        {
            throw JsonArtifact.Truncated(artifact);
        }
    }

    private static void WriteStopWords(Utf8JsonWriter writer, IReadOnlyCollection<string>? stopWords)
    {
        if (stopWords is null)
        {
            writer.WriteNull("stopWords");
            return;
        }

        var sorted = stopWords.ToArray();
        Array.Sort(sorted, StringComparer.Ordinal);
        writer.WriteStartArray("stopWords");
        foreach (string word in sorted)
        {
            writer.WriteStringValue(word);
        }
        writer.WriteEndArray();
    }

    private static List<string>? ReadStopWords(ref Utf8JsonReader reader, string artifact, in ArtifactLimits limits)
    {
        if (!reader.Read())
        {
            throw JsonArtifact.Truncated(artifact);
        }
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw JsonArtifact.UnexpectedToken(artifact, "options.stopWords", reader.TokenType);
        }

        var words = new List<string>();
        while (reader.Read() && reader.TokenType == JsonTokenType.String)
        {
            string word = reader.GetString()!;
            limits.CheckTokenLength(word.Length);
            words.Add(word);
            limits.CheckArrayLength(words.Count, "options.stopWords");
        }
        if (reader.TokenType != JsonTokenType.EndArray)
        {
            throw JsonArtifact.UnexpectedToken(artifact, "options.stopWords", reader.TokenType);
        }
        return words;
    }

    private static string AnalyzerName(AnalyzerKind kind) => kind switch
    {
        AnalyzerKind.Word => "word",
        AnalyzerKind.Char => "char",
        AnalyzerKind.CharWordBoundary => "char_wb",
        _ => throw new InvalidDataException($"Cannot persist the unknown analyzer kind {kind}."),
    };

    private static AnalyzerKind ParseAnalyzer(string value) => value switch
    {
        "word" => AnalyzerKind.Word,
        "char" => AnalyzerKind.Char,
        "char_wb" => AnalyzerKind.CharWordBoundary,
        _ => throw new InvalidDataException($"Unknown analyzer '{value}'; expected \"word\", \"char\" or \"char_wb\"."),
    };
}
