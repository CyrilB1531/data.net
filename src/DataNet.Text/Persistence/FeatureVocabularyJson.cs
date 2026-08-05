using System.Text.Json;
using DataNet.Internal.Persistence;

namespace DataNet.Text.Persistence;

/// <summary>
/// Reads and writes the two arrays a fitted vectorizer carries: the sorted
/// feature names and, for TF-IDF, the idf weights.
/// </summary>
/// <remarks>
/// <c>featureCount</c> is written before either array so a reader can size its
/// buffers from a value it has already checked against
/// <c>ArtifactLoadOptions.MaxVocabularySize</c>, rather than growing a list at the
/// mercy of the file.
/// </remarks>
internal static class FeatureVocabularyJson
{
    public const string FeatureCountProperty = "featureCount";
    public const string VocabularyProperty = "vocabulary";
    public const string IdfProperty = "idf";

    public static void WriteVocabulary(Utf8JsonWriter writer, IReadOnlyList<string> featureNames)
    {
        writer.WriteStartArray(VocabularyProperty);
        for (int i = 0; i < featureNames.Count; i++)
        {
            writer.WriteStringValue(featureNames[i]);
        }
        writer.WriteEndArray();
    }

    public static void WriteIdf(Utf8JsonWriter writer, IReadOnlyList<double> idf)
    {
        writer.WriteStartArray(IdfProperty);
        for (int i = 0; i < idf.Count; i++)
        {
            JsonArtifact.WriteExactDouble(writer, idf[i]);
        }
        writer.WriteEndArray();
    }

    /// <summary>Reads and bounds-checks the declared feature count.</summary>
    public static int ReadFeatureCount(ref Utf8JsonReader reader, string artifact, in ArtifactLimits limits)
    {
        int featureCount = JsonArtifact.ReadInt32(ref reader, artifact, FeatureCountProperty);
        if (featureCount < 0)
        {
            throw JsonArtifact.Inconsistent(artifact, $"{FeatureCountProperty} is negative ({featureCount}).");
        }
        limits.CheckVocabularySize(featureCount);
        return featureCount;
    }

    public static string[] ReadVocabulary(ref Utf8JsonReader reader, string artifact, in ArtifactLimits limits, int declaredCount)
    {
        JsonArtifact.ReadStartArray(ref reader, artifact, VocabularyProperty);

        var names = new List<string>(InitialCapacity(declaredCount));
        while (reader.Read() && reader.TokenType == JsonTokenType.String)
        {
            string name = reader.GetString()!;
            limits.CheckTokenLength(name.Length);
            names.Add(name);
            limits.CheckVocabularySize(names.Count);
        }
        if (reader.TokenType != JsonTokenType.EndArray)
        {
            throw JsonArtifact.UnexpectedToken(artifact, VocabularyProperty, reader.TokenType);
        }

        string[] result = names.ToArray();
        EnsureSortedAndUnique(result, artifact);
        return result;
    }

    public static double[] ReadIdf(ref Utf8JsonReader reader, string artifact, in ArtifactLimits limits, int declaredCount)
    {
        JsonArtifact.ReadStartArray(ref reader, artifact, IdfProperty);

        var values = new List<double>(InitialCapacity(declaredCount));
        while (reader.Read() && reader.TokenType == JsonTokenType.Number)
        {
            if (!reader.TryGetDouble(out double value))
            {
                throw JsonArtifact.UnexpectedToken(artifact, IdfProperty, reader.TokenType);
            }
            values.Add(value);
            limits.CheckArrayLength(values.Count, IdfProperty);
        }
        if (reader.TokenType != JsonTokenType.EndArray)
        {
            throw JsonArtifact.UnexpectedToken(artifact, IdfProperty, reader.TokenType);
        }
        return values.ToArray();
    }

    /// <summary>Checks the declared feature count against what the arrays actually held.</summary>
    public static void EnsureDeclaredCount(string artifact, int declaredCount, int actualCount, string arrayName)
    {
        if (declaredCount != actualCount)
        {
            throw JsonArtifact.Inconsistent(
                artifact,
                $"{FeatureCountProperty} is {declaredCount} but '{arrayName}' holds {actualCount} entries.");
        }
    }

    private static int InitialCapacity(int declaredCount) =>
        declaredCount > 0 ? Math.Min(declaredCount, 4096) : 0;

    private static void EnsureSortedAndUnique(string[] names, string artifact)
    {
        // The vectorizers index features by position in this array, and every
        // lookup assumes it is the ordinal-sorted, duplicate-free list Fit
        // produced. A file that breaks that would transform documents into the
        // wrong columns — silently.
        for (int i = 1; i < names.Length; i++)
        {
            int order = string.CompareOrdinal(names[i - 1], names[i]);
            if (order > 0)
            {
                throw JsonArtifact.Inconsistent(
                    artifact,
                    $"'{VocabularyProperty}' must be sorted in ordinal order, but '{names[i - 1]}' precedes '{names[i]}'.");
            }
            if (order == 0)
            {
                throw JsonArtifact.Inconsistent(artifact, $"'{VocabularyProperty}' contains the duplicate entry '{names[i]}'.");
            }
        }
    }
}
