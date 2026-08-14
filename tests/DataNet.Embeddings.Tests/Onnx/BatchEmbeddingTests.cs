using DataNet.Embeddings.Onnx;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// The whole pipeline, end to end: text in, one normalized vector per text out. Runs against
/// <c>tiny_embedder.onnx</c> (a single-node Gather over a 64x4 table, <c>tools/build_tiny_models.py</c>),
/// not <c>tiny_encoder.onnx</c>: that model maps every token to a multiple of one direction, so its
/// pooled vector is the same for any input and is blind to what batching can get wrong. Here two
/// different token sets pool to two different directions, and <c>[PAD]</c> gathers a non-zero row, so
/// a padding leak moves the result. The replay of reference vectors is the only assertion using a
/// tolerance, not 1e-9, since ONNX Runtime returns and normalizes in float32. Everything else exact
/// (ids and mask, batched-vs-single-sequence equality, batching/bucketing invariance) is asserted exactly.
/// </summary>
public sealed class BatchEmbeddingTests
{
    /// <summary>
    /// The float32 pipeline's honest bound, measured rather than guessed.
    /// </summary>
    /// <remarks>
    /// The largest disagreement with the float64 reference across the whole corpus
    /// is 3.5e-8: 1e-7 passes, 3e-8 fails on six cases, 1e-9 on seven. The constant
    /// sits an order of magnitude above the measurement, and the margin is for a
    /// machine whose ONNX Runtime contracts the gather and the sum differently —
    /// not for a defect, which at this scale would move a component by far more.
    /// </remarks>
    private const double Tolerance = 1e-6;

    private static readonly string EmbedderPath =
        Path.Combine(AppContext.BaseDirectory, "oracles", "tiny_embedder.onnx");

    private static readonly string EncoderPath =
        Path.Combine(AppContext.BaseDirectory, "oracles", "tiny_encoder.onnx");

    private static OnnxTextEmbedder Embedder() => new(EmbedderPath, BatchCorpus.Tokenizer());

    public static TheoryData<string> CaseNames()
    {
        var names = new TheoryData<string>();
        foreach (BatchCase batch in BatchCorpus.Oracle.Cases)
        {
            names.Add(batch.Name);
        }
        return names;
    }

    /// <summary>
    /// The table frozen in the corpus is the table the model gathers from. <c>build_tiny_models.py</c>
    /// and <c>generate_oracles.py</c> each spell it out separately -- they run in virtualenvs that
    /// cannot import each other -- so this is what stops the two copies drifting apart unnoticed. What
    /// is compared is each row's direction, not its magnitude: the public surface returns normalized
    /// vectors, so a row that differed by anything but a positive scale -- a different entry, a
    /// transposed table, an off-by-one id -- changes the direction and fails here.
    /// </summary>
    [Fact]
    public void The_model_gathers_from_the_table_the_corpus_froze()
    {
        double[][] table = BatchCorpus.Oracle.EmbeddingTable;
        using var embedder = new OnnxTextEmbedder(EmbedderPath);

        for (int id = 0; id < table.Length; id++)
        {
            // A batch of one token with a full mask pools to that token's row, so
            // what comes back is the row scaled to unit length.
            float[] gathered = embedder.Embed([id], [1L]);
            double[] expected = Normalized(table[id]);

            for (int d = 0; d < expected.Length; d++)
            {
                Assert.True(Math.Abs(expected[d] - gathered[d]) < Tolerance,
                    $"row {id} dim {d}: the model gathers {gathered[d]:R}, the corpus froze {expected[d]:R}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void EmbedBatch_matches_the_reference_vectors(string name)
    {
        BatchCase expected = BatchCorpus.Oracle.Named(name);
        using OnnxTextEmbedder embedder = Embedder();

        float[][] actual = embedder.EmbedBatch(expected.Texts, expected.Options);

        Assert.Equal(expected.Texts.Length, actual.Length);
        for (int row = 0; row < actual.Length; row++)
        {
            for (int d = 0; d < actual[row].Length; d++)
            {
                double difference = Math.Abs(expected.PooledNormalized[row][d] - actual[row][d]);
                Assert.True(difference < Tolerance,
                    $"{expected} row {row} dim {d}: expected {expected.PooledNormalized[row][d]:R}, "
                    + $"got {actual[row][d]:R}, off by {difference:R}");
            }
        }
    }

    /// <summary>
    /// A text embedded in a batch gets the same vector as the same text embedded alone, bit for bit --
    /// the property that catches a wrong attention mask. In the batch it is padded to the longest row
    /// of <c>mixed_lengths</c> (eleven tokens); alone it is not padded at all. <c>[PAD]</c> gathers a
    /// non-zero row, so a leaked padded position would show up as a visible difference, not a last-bit one.
    /// </summary>
    [Fact]
    public void A_batched_vector_equals_the_single_sequence_vector()
    {
        BatchCase mixed = BatchCorpus.Oracle.Named("mixed_lengths");
        var encoder = new BatchEncoder(BatchCorpus.Tokenizer(), mixed.Options);
        using OnnxTextEmbedder embedder = Embedder();

        float[][] batched = embedder.EmbedBatch(mixed.Texts, mixed.Options);

        for (int i = 0; i < mixed.Texts.Length; i++)
        {
            long[] ids = encoder.Encode(mixed.Texts[i]);
            var mask = new long[ids.Length];
            Array.Fill(mask, 1L);

            float[] alone = embedder.Embed(ids, mask);

            Assert.Equal(alone, batched[i]);
        }
    }

    /// <summary>
    /// Sub-batching and length bucketing change how the work is padded and in what
    /// order it is run. Neither may change a single returned value.
    /// </summary>
    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(64, true)]
    public void Batching_and_bucketing_are_invisible_in_the_output(int batchSize, bool sortByLength)
    {
        BatchCase mixed = BatchCorpus.Oracle.Named("mixed_lengths");
        using OnnxTextEmbedder embedder = Embedder();

        float[][] reference = embedder.EmbedBatch(mixed.Texts, mixed.Options);
        float[][] actual = embedder.EmbedBatch(
            mixed.Texts,
            mixed.Options with { BatchSize = batchSize, SortByLength = sortByLength });

        Assert.Equal(reference.Length, actual.Length);
        for (int i = 0; i < reference.Length; i++)
        {
            Assert.Equal(reference[i], actual[i]);
        }
    }

    /// <summary>
    /// Bucketing reorders the work, so the restored order has to be checked
    /// against something that tells the rows apart.
    /// </summary>
    /// <remarks>
    /// The invariance fact above would pass on an implementation that returned the
    /// bucketed order in both calls. This one embeds distinct texts, of lengths
    /// deliberately out of order, and checks each vector against the one that text
    /// gets on its own.
    /// </remarks>
    [Fact]
    public void Bucketing_restores_the_caller_order()
    {
        string[] texts = ["the cat sat on the mat", "the", "hello world!", "", "the dog runs and the cat plays"];
        var options = new EncodingOptions { BatchSize = 2, SortByLength = true };
        using OnnxTextEmbedder embedder = Embedder();

        float[][] bucketed = embedder.EmbedBatch(texts, options);

        for (int i = 0; i < texts.Length; i++)
        {
            float[] alone = embedder.EmbedBatch([texts[i]], options)[0];
            Assert.Equal(alone, bucketed[i]);
        }
    }

    [Fact]
    public void An_empty_corpus_returns_no_vectors()
    {
        using OnnxTextEmbedder embedder = Embedder();
        Assert.Empty(embedder.EmbedBatch([]));
    }

    [Fact]
    public void Cancellation_is_observed()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        using OnnxTextEmbedder embedder = Embedder();

        Assert.Throws<OperationCanceledException>(
            () => embedder.EmbedBatch(["the cat"], new EncodingOptions(), source.Token));
    }

    [Fact]
    public void An_embedder_without_a_tokenizer_says_so_instead_of_dereferencing_null()
    {
        using var embedder = new OnnxTextEmbedder(EmbedderPath);

        InvalidOperationException error =
            Assert.Throws<InvalidOperationException>(() => embedder.EmbedBatch(["the cat"]));
        Assert.Contains("tokenizer", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// An input name the model does not declare is an argument error naming what
    /// it does declare, not an opaque failure inside the runtime.
    /// </summary>
    [Fact]
    public void An_input_the_model_does_not_declare_is_refused_by_name()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new OnnxTextEmbedder(EmbedderPath, inputIdsName: "tokens"));

        Assert.Contains("tokens", error.Message, StringComparison.Ordinal);
        Assert.Contains("input_ids", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_output_the_model_does_not_declare_is_refused_by_name()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new OnnxTextEmbedder(EmbedderPath, outputName: "pooler_output"));

        Assert.Contains("pooler_output", error.Message, StringComparison.Ordinal);
        Assert.Contains("last_hidden_state", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Both models export a symbolic sequence axis, as every `dynamic_axes` export
    /// does, so neither declares a maximum length to fall back on.
    /// </summary>
    /// <remarks>
    /// The distinction matters: a model whose sequence axis is a fixed number does
    /// constrain the encoder, and one whose axis is symbolic does not. Reporting a
    /// symbolic axis — a negative dimension — as a length would truncate every
    /// input to a negative budget.
    /// </remarks>
    [Fact]
    public void A_dynamic_sequence_axis_is_not_a_declared_maximum()
    {
        using var embedder = new OnnxTextEmbedder(EmbedderPath);
        using var encoder = new OnnxTextEmbedder(EncoderPath);

        Assert.Null(embedder.MaxSequenceLength);
        Assert.Null(encoder.MaxSequenceLength);
        Assert.Equal(4, embedder.Dimension);
    }

    /// <summary>
    /// The batch path works on a model that does not declare `token_type_ids`,
    /// and on one that does.
    /// </summary>
    /// <remarks>
    /// `tiny_encoder.onnx` declares two inputs and `tiny_embedder.onnx` three, so
    /// the branch that feeds the zero buffer is covered on one side and skipped on
    /// the other. The encoder maps every token to a multiple of `W`, so whatever
    /// the texts, every vector it returns is `W` normalized.
    /// </remarks>
    [Fact]
    public void A_model_without_token_type_ids_is_batched_too()
    {
        using var embedder = new OnnxTextEmbedder(EncoderPath, BatchCorpus.Tokenizer());

        float[][] vectors = embedder.EmbedBatch(["the cat", "the dog runs and the cat plays", ""]);

        double norm = Math.Sqrt(0.01 + 0.04 + 0.09 + 0.16);
        Assert.Equal(3, vectors.Length);
        foreach (float[] vector in vectors)
        {
            for (int d = 0; d < 4; d++)
            {
                Assert.Equal((d + 1) * 0.1 / norm, vector[d], 4);
            }
        }
    }

    /// <summary>Scales a reference row to unit length, the way the pipeline ends.</summary>
    private static double[] Normalized(double[] row)
    {
        double magnitude = Math.Sqrt(row.Sum(v => v * v));

        // No table entry is ever zero, by construction (tools/build_tiny_models.py); this
        // guard is about the assertion staying honest if the table's generator ever changes.
        Assert.True(magnitude > 0, "a zero row carries no direction to compare");
        return row.Select(v => v / magnitude).ToArray();
    }
}
