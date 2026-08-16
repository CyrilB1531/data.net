using System.Text.Json;
using Lodestar.Internal.Persistence;

namespace Lodestar.Text.Persistence;

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

    /// <summary>
    /// Writes the idf vector as one base64 string of raw little-endian IEEE-754 bits.
    /// </summary>
    /// <remarks>
    /// The vocabulary stays plain text, because that is the half of an artifact a
    /// human reads; the idf vector does not, because nobody reads thirty thousand
    /// floats by eye. See <c>docs/decisions/0011-persistence-format.md</c>, "The
    /// idf vector is base64, and the vocabulary is not", for the measurements and
    /// the exactness argument for raw bits over a decimal formatter.
    /// </remarks>
    public static void WriteIdf(Utf8JsonWriter writer, IReadOnlyList<double> idf)
    {
        for (int i = 0; i < idf.Count; i++)
        {
            double value = idf[i];
            // Refused before write: JSON has no NaN/infinity, and a model carrying
            // one is broken already (0011-persistence-format.md, "Doubles").
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new InvalidDataException(
                    $"Cannot persist a non-finite idf weight at index {i}: the model is broken before it reaches the file.");
            }
        }
        Base64Numbers.WriteDoubles(writer, IdfProperty, idf);
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

        string[] names = new string[InitialCapacity(declaredCount)];
        int count = 0;
        string? previous = null;
        while (reader.Read() && reader.TokenType == JsonTokenType.String)
        {
            string name = reader.GetString()!;
            limits.CheckTokenLength(name.Length);
            // Checked inline, not in a second pass: the predecessor is already in
            // cache, and 30k strings do not need walking twice.
            if (previous is not null && string.CompareOrdinal(previous, name) >= 0)
            {
                throw OutOfOrder(artifact, previous, name);
            }
            if (count == names.Length)
            {
                // SonarLint S2583: the zero-length case is reachable and covered by
                // A_vocabulary_written_before_the_feature_count_still_loads. The reader
                // accepts reordered properties, so 'vocabulary' can arrive before the
                // 'featureCount' that would have sized this buffer, leaving it empty.
                // The analyser cannot see that declaredCount may be -1 there.
#pragma warning disable S2583
                Array.Resize(ref names, names.Length == 0 ? 4 : names.Length * 2);
#pragma warning restore S2583
            }
            names[count++] = name;
            previous = name;
            limits.CheckVocabularySize(count);
        }
        if (reader.TokenType != JsonTokenType.EndArray)
        {
            throw JsonArtifact.UnexpectedToken(artifact, VocabularyProperty, reader.TokenType);
        }

        if (count != names.Length)
        {
            Array.Resize(ref names, count);
        }
        return names;
    }

    /// <summary>Reads the base64 idf vector written by <see cref="WriteIdf"/>.</summary>
    public static double[] ReadIdf(ref Utf8JsonReader reader, string artifact, in ArtifactLimits limits)
    {
        double[] values = Base64Numbers.ReadDoubles(ref reader, artifact, IdfProperty, limits);
        for (int i = 0; i < values.Length; i++)
        {
            // Checked on read too, matching the write-side refusal (see WriteIdf,
            // and 0011-persistence-format.md's "Doubles" section, for why).
            if (double.IsNaN(values[i]) || double.IsInfinity(values[i]))
            {
                throw JsonArtifact.Inconsistent(
                    artifact,
                    $"'{IdfProperty}' holds a value that is not finite, at index {i}.");
            }
        }
        return values;
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

    /// <summary>
    /// How large to make the vocabulary buffer before reading it.
    /// </summary>
    /// <remarks>
    /// Sized from the declared count, so the common case allocates once at the
    /// right length instead of growing and copying as items arrive. The 64k
    /// ceiling then keeps a declared count from sizing the allocation on its own;
    /// <c>CheckVocabularySize</c> still bounds the total actually accepted.
    /// </remarks>
    private static int InitialCapacity(int declaredCount) =>
        declaredCount > 0 ? Math.Min(declaredCount, MaxPreallocatedEntries) : 0;

    private const int MaxPreallocatedEntries = 65_536;

    /// <summary>
    /// Names the way two consecutive vocabulary entries break the ordering contract.
    /// </summary>
    /// <remarks>
    /// The vectorizers index features by position in this array, and every lookup
    /// assumes it is the ordinal-sorted, duplicate-free list Fit produced. A file
    /// that breaks that would transform documents into the wrong columns — silently.
    /// </remarks>
    private static InvalidDataException OutOfOrder(string artifact, string previous, string current) =>
        string.Equals(previous, current, StringComparison.Ordinal)
            ? JsonArtifact.Inconsistent(artifact, $"'{VocabularyProperty}' contains the duplicate entry '{current}'.")
            : JsonArtifact.Inconsistent(
                artifact,
                $"'{VocabularyProperty}' must be sorted in ordinal order, but '{previous}' precedes '{current}'.");
}
