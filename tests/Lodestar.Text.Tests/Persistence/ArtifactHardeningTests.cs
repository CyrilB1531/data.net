using System.Buffers.Binary;
using System.Text;
using Lodestar.Text.Persistence;
using Lodestar.Text.Vectorization;
using Xunit;

namespace Lodestar.Text.Tests.Persistence;

/// <summary>
/// A persisted model is a file, and a file can come from anywhere. Every case
/// here is an input that must fail with <see cref="InvalidDataException"/> and a
/// message a caller can act on — never an unhandled parser error, an
/// out-of-memory, or worse, a silent misread.
/// </summary>
public sealed class ArtifactHardeningTests
{
    private static readonly string[] TinyCorpus = ["alpha beta", "beta gamma", "gamma delta"];

    [Fact]
    public void Input_that_is_not_json_is_rejected()
    {
        InvalidDataException error = LoadCount("this is not json");
        Assert.Contains("not well-formed JSON", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Input_that_is_not_an_object_is_rejected()
    {
        InvalidDataException error = LoadCount("[1, 2, 3]");
        Assert.Contains("must be a JSON object", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_truncated_artifact_is_rejected()
    {
        string json = Baseline();
        InvalidDataException error = LoadCount(json.Substring(0, json.Length / 2));
        Assert.Contains("not well-formed JSON", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_after_the_artifact_is_rejected()
    {
        InvalidDataException error = LoadCount(Baseline() + "{}");
        Assert.Contains("not well-formed JSON", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_schema_is_rejected()
    {
        InvalidDataException error = LoadCount(
            Baseline().Replace("\"$schema\":\"datanet/count-vectorizer\",", string.Empty, StringComparison.Ordinal));

        Assert.Contains("missing the required property '$schema'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_renamed_schema_property_is_rejected_as_unknown()
    {
        InvalidDataException error = LoadCount(Baseline().Replace("\"$schema\"", "\"schema\"", StringComparison.Ordinal));
        Assert.Contains("Unknown property 'schema'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_version_is_rejected()
    {
        InvalidDataException error = LoadCount(Baseline().Replace(",\"version\":1", string.Empty, StringComparison.Ordinal));
        Assert.Contains("missing the required property 'version'", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(9999)]
    public void An_unsupported_version_is_rejected(int version)
    {
        InvalidDataException error = LoadCount(
            Baseline().Replace("\"version\":1", $"\"version\":{version}", StringComparison.Ordinal));

        Assert.Contains($"version {version}", error.Message, StringComparison.Ordinal);
        Assert.Contains("reads versions 1 to 1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_version_of_the_wrong_json_type_is_rejected()
    {
        InvalidDataException error = LoadCount(
            Baseline().Replace("\"version\":1", "\"version\":\"1\"", StringComparison.Ordinal));

        Assert.Contains("version", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_top_level_property_is_rejected()
    {
        InvalidDataException error = LoadCount(
            Baseline().Replace("\"featureCount\"", "\"weights\":[1],\"featureCount\"", StringComparison.Ordinal));

        Assert.Contains("Unknown property 'weights'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_option_is_rejected()
    {
        InvalidDataException error = LoadCount(
            Baseline().Replace("\"lowercase\"", "\"lowercaseAll\":true,\"lowercase\"", StringComparison.Ordinal));

        Assert.Contains("options.lowercaseAll", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_feature_count_that_disagrees_with_the_vocabulary_is_rejected()
    {
        InvalidDataException error = LoadCount(
            Baseline().Replace("\"featureCount\":4", "\"featureCount\":3", StringComparison.Ordinal));

        Assert.Contains("featureCount is 3", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_negative_feature_count_is_rejected()
    {
        InvalidDataException error = LoadCount(
            Baseline().Replace("\"featureCount\":4", "\"featureCount\":-1", StringComparison.Ordinal));

        Assert.Contains("negative", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unsorted_vocabulary_is_rejected()
    {
        InvalidDataException error = LoadCount(
            Baseline().Replace("[\"alpha\",\"beta\"", "[\"beta\",\"alpha\"", StringComparison.Ordinal));

        Assert.Contains("must be sorted in ordinal order", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_duplicated_vocabulary_entry_is_rejected()
    {
        InvalidDataException error = LoadCount(
            Baseline().Replace("[\"alpha\",\"beta\"", "[\"alpha\",\"alpha\"", StringComparison.Ordinal));

        Assert.Contains("duplicate entry 'alpha'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_analyzer_is_rejected()
    {
        InvalidDataException error = LoadCount(
            Baseline().Replace("\"analyzer\":\"word\"", "\"analyzer\":\"sentence\"", StringComparison.Ordinal));

        Assert.Contains("Unknown analyzer 'sentence'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_norm_is_rejected()
    {
        var original = new TfidfVectorizer().Fit(TinyCorpus);
        using var saved = new MemoryStream();
        original.Save(saved);
        string json = Encoding.UTF8.GetString(saved.ToArray()).Replace("\"norm\":\"l2\"", "\"norm\":\"l3\"", StringComparison.Ordinal);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        InvalidDataException error = Assert.Throws<InvalidDataException>(() => TfidfVectorizer.Load(stream));

        Assert.Contains("Unknown norm 'l3'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_vocabulary_written_before_the_feature_count_still_loads()
    {
        // Reordered properties put the vocabulary before the count that would size
        // its buffer -- the only path where the buffer grows from nothing.
        var original = new CountVectorizer().Fit(TinyCorpus);
        using var saved = new MemoryStream();
        original.Save(saved);
        string json = Encoding.UTF8.GetString(saved.ToArray());

        int countStart = json.IndexOf(",\"featureCount\":", StringComparison.Ordinal);
        int countEnd = json.IndexOf(",\"vocabulary\":", StringComparison.Ordinal);
        string countProperty = json.Substring(countStart, countEnd - countStart);
        string reordered = string.Concat(
            json.AsSpan(0, countStart),
            json.AsSpan(countEnd, json.Length - countEnd - 1),
            countProperty.AsSpan(),
            "}".AsSpan());

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(reordered));
        CountVectorizer reloaded = CountVectorizer.Load(stream);

        Assert.Equal(original.GetFeatureNames(), reloaded.GetFeatureNames());
    }

    [Fact]
    public void An_idf_vector_of_the_wrong_length_is_rejected()
    {
        // One double's worth of bits, where the vocabulary declares more.
        string oneValue = Convert.ToBase64String(BitConverter.GetBytes(1.0));

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadTfidfWithIdf(oneValue));

        Assert.Contains("'idf' holds 1 entries", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_idf_that_is_not_valid_base64_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadTfidfWithIdf("not base64 at all!!"));

        Assert.Contains("not valid base64", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Saving_an_unfitted_vectorizer_to_a_path_leaves_the_existing_file_intact()
    {
        // OpenWrite truncates before the fitted check fires, so a failed Save would
        // destroy a good artifact. The MemoryStream overload cannot see this.
        string path = Path.Combine(Path.GetTempPath(), $"datanet-save-{Guid.NewGuid():N}.json");
        try
        {
            new TfidfVectorizer().Fit(TinyCorpus).Save(path);
            byte[] before = File.ReadAllBytes(path);

            Assert.Throws<InvalidOperationException>(() => new TfidfVectorizer().Save(path));

            Assert.Equal(before, File.ReadAllBytes(path));
            // And the surviving file must still be loadable, not merely the same size.
            using FileStream reopened = File.OpenRead(path);
            Assert.NotNull(TfidfVectorizer.Load(reopened));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void An_idf_holding_a_non_finite_value_is_rejected(double poison)
    {
        // Raw bits carry NaN, so moving idf out of JSON numbers moved it out of the
        // write path's non-finite check -- and every later Transform scores NaN.
        var original = new TfidfVectorizer().Fit(TinyCorpus);
        using var saved = new MemoryStream();
        original.Save(saved);
        string json = Encoding.UTF8.GetString(saved.ToArray());

        int start = json.IndexOf("\"idf\":\"", StringComparison.Ordinal) + "\"idf\":\"".Length;
        int end = json.IndexOf('"', start);
        byte[] raw = Convert.FromBase64String(json.Substring(start, end - start));
        BinaryPrimitives.WriteInt64LittleEndian(raw.AsSpan(0), BitConverter.DoubleToInt64Bits(poison));
        string poisoned = string.Concat(
            json.AsSpan(0, start), Convert.ToBase64String(raw), json.AsSpan(end));

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(poisoned));
        InvalidDataException error = Assert.Throws<InvalidDataException>(() => TfidfVectorizer.Load(stream));

        Assert.Contains("not finite", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_idf_whose_length_is_not_a_whole_number_of_doubles_is_rejected()
    {
        // 12 bytes: one double and a half.
        string ragged = Convert.ToBase64String(new byte[12]);

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadTfidfWithIdf(ragged));

        Assert.Contains("whole number of 64-bit values", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_idf_over_the_array_limit_is_rejected()
    {
        string tooMany = Convert.ToBase64String(new byte[64]);   // 8 doubles

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadTfidfWithIdf(tooMany, new ArtifactLoadOptions { MaxArrayLength = 4 }));

        Assert.Contains("MaxArrayLength", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Saves a fitted vectorizer, swaps its <c>idf</c> for <paramref name="base64"/>,
    /// and loads the result.
    /// </summary>
    private static TfidfVectorizer LoadTfidfWithIdf(string base64, ArtifactLoadOptions? options = null)
    {
        var original = new TfidfVectorizer().Fit(TinyCorpus);
        using var saved = new MemoryStream();
        original.Save(saved);
        string json = Encoding.UTF8.GetString(saved.ToArray());
        int idfStart = json.IndexOf("\"idf\":", StringComparison.Ordinal);
        json = string.Concat(json.AsSpan(0, idfStart), $"\"idf\":\"{base64}\"}}");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return TfidfVectorizer.Load(stream, options);
    }

    [Fact]
    public void A_hashing_artifact_with_no_features_is_rejected()
    {
        var original = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 16 });
        using var saved = new MemoryStream();
        original.Save(saved);
        string json = Encoding.UTF8.GetString(saved.ToArray()).Replace("\"numFeatures\":16", "\"numFeatures\":0", StringComparison.Ordinal);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        InvalidDataException error = Assert.Throws<InvalidDataException>(() => HashingVectorizer.Load(stream));

        Assert.Contains("numFeatures must be at least 1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_artifact_over_the_byte_limit_is_rejected_before_it_is_parsed()
    {
        InvalidDataException error = LoadCount(Baseline(), new ArtifactLoadOptions { MaxTotalBytes = 16 });

        Assert.Contains("artifact size in bytes", error.Message, StringComparison.Ordinal);
        Assert.Contains("MaxTotalBytes", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_artifact_over_the_vocabulary_limit_is_rejected()
    {
        InvalidDataException error = LoadCount(Baseline(), new ArtifactLoadOptions { MaxVocabularySize = 2 });

        Assert.Contains("vocabulary size", error.Message, StringComparison.Ordinal);
        Assert.Contains("MaxVocabularySize", error.Message, StringComparison.Ordinal);
        Assert.Contains("limit 2", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_token_over_the_length_limit_is_rejected()
    {
        InvalidDataException error = LoadCount(Baseline(), new ArtifactLoadOptions { MaxTokenLength = 3 });

        Assert.Contains("token length", error.Message, StringComparison.Ordinal);
        Assert.Contains("MaxTokenLength", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_stop_word_list_over_the_array_limit_is_rejected()
    {
        var original = new CountVectorizer(new CountVectorizerOptions { StopWords = ["one", "two", "three"] }).Fit(TinyCorpus);
        using var saved = new MemoryStream();
        original.Save(saved);

        using var stream = new MemoryStream(saved.ToArray());
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => CountVectorizer.Load(stream, new ArtifactLoadOptions { MaxArrayLength = 2 }));

        Assert.Contains("options.stopWords", error.Message, StringComparison.Ordinal);
        Assert.Contains("MaxArrayLength", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_artifact_deeper_than_the_depth_limit_is_rejected()
    {
        // The artifact itself is shallow; the limit is what makes it too deep, so
        // this exercises the plumbing rather than a contrived nesting bomb.
        InvalidDataException error = LoadCount(Baseline(), new ArtifactLoadOptions { MaxJsonDepth = 1 });

        Assert.Contains("not well-formed JSON", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_limits_default_to_the_documented_values()
    {
        var options = new ArtifactLoadOptions();

        Assert.Equal(1_000_000, options.MaxVocabularySize);
        Assert.Equal(1024, options.MaxTokenLength);
        Assert.Equal(32, options.MaxJsonDepth);
        Assert.Equal(256L * 1024 * 1024, options.MaxTotalBytes);
        Assert.Equal(1_000_000, options.MaxArrayLength);
    }

    /// <summary>A valid, freshly written artifact: <c>alpha</c>, <c>beta</c>, <c>delta</c>, <c>gamma</c>.</summary>
    private static string Baseline()
    {
        var vectorizer = new CountVectorizer().Fit(TinyCorpus);
        using var stream = new MemoryStream();
        vectorizer.Save(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static InvalidDataException LoadCount(string json, ArtifactLoadOptions? options = null)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return Assert.Throws<InvalidDataException>(() => CountVectorizer.Load(stream, options));
    }
}
