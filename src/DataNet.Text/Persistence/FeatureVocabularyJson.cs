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

    /// <summary>
    /// Writes the idf vector as one base64 string of raw little-endian IEEE-754 bits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The vocabulary stays plain text, because that is the half of an artifact a
    /// human reads. The idf vector is thirty thousand floats that nobody inspects by
    /// eye, and writing it as JSON numbers was measurably the most expensive part of
    /// the format: parsing them cost four times what materialising the whole
    /// vocabulary cost, and they made the file a quarter larger.
    /// </para>
    /// <para>
    /// Raw bits also make the round trip exact <em>by construction</em> rather than
    /// by trusting a decimal formatter — no shortest-round-trippable versus
    /// <c>"G17"</c> question arises, on any framework.
    /// </para>
    /// </remarks>
    public static void WriteIdf(Utf8JsonWriter writer, IReadOnlyList<double> idf)
    {
        for (int i = 0; i < idf.Count; i++)
        {
            double value = idf[i];
            // Raw bits would carry these happily, where a JSON number could not. The
            // format's promise is that what it holds is a usable model, so the refusal
            // that WriteExactDouble applies to every other double applies here too.
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
            // Ordering is checked here rather than in a second pass over the finished
            // array: the predecessor is still in cache, and 30k strings are not worth
            // walking twice.
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
            // Raw bits carry NaN and infinity perfectly well, where JSON numbers could
            // not. Left through, they turn every later Transform into NaN scores —
            // silently, and a long way from the file that caused it.
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
    /// <para>
    /// Sized from the count the file declares, so the common case is one allocation
    /// of exactly the right length and no copy at all — where growing from a small
    /// clamp cost four reallocations plus a final <c>ToArray</c> for a 30k-feature
    /// artifact, all of it garbage on a path whose remaining cost is collection.
    /// </para>
    /// <para>
    /// The ceiling is what keeps a declared count from sizing the allocation on its
    /// own: a file claiming a million features gets 64k entries and has to actually
    /// deliver the rest before the buffer grows. <c>CheckVocabularySize</c> still
    /// bounds the total.
    /// </para>
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
