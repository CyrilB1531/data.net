namespace DataNet.Embeddings.Tokenization;

/// <summary>What to do with a sequence longer than the model accepts.</summary>
/// <remarks>
/// The names follow HuggingFace's <c>truncation</c> argument. DataNet encodes one
/// sequence at a time (never a pair), so <see cref="LongestFirst"/> and
/// <see cref="Right"/> both drop tokens from the end — the distinction only bites
/// on a pair, which this pipeline does not build.
/// </remarks>
public enum TruncationStrategy
{
    /// <summary>Refuse: a sequence over the limit raises <see cref="ArgumentException"/>.</summary>
    /// <remarks>
    /// Matches <c>truncation=False</c>. Silently dropping the tail of a document is
    /// a decision, so this makes it possible to insist on being told instead.
    /// </remarks>
    None = 0,

    /// <summary>Drop tokens from the end until the sequence fits. Matches <c>truncation="longest_first"</c>.</summary>
    LongestFirst = 1,

    /// <summary>Drop tokens from the end until the sequence fits. Matches <c>truncation="only_first"</c>.</summary>
    Right = 2,
}

/// <summary>
/// How a batch of text becomes model input: special tokens, truncation, padding
/// and bucketing.
/// </summary>
/// <remarks>
/// The C# form of a HuggingFace <c>tokenizer(texts, padding=True, truncation=True,
/// max_length=…)</c> call, plus two knobs that matter once batched. Padding is to
/// the batch's own longest sequence, never <c>padding="max_length"</c>; see
/// <c>docs/guides/embeddings.md</c>'s "Embed a batch".
/// </remarks>
public sealed record EncodingOptions
{
    /// <summary>The special tokens to wrap each sequence in. Defaults to <see cref="SpecialTokenTemplate.Bert"/>.</summary>
    public SpecialTokenTemplate Template { get; init; } = SpecialTokenTemplate.Bert;

    /// <summary>
    /// The maximum total length of an encoded sequence, special tokens included.
    /// <see langword="null"/> asks the model for its declared maximum.
    /// </summary>
    /// <remarks>
    /// Matches <c>max_length</c>; the budget covers the special tokens, so with
    /// <see cref="SpecialTokenTemplate.Bert"/> and <c>MaxLength = 8</c> at most six
    /// survive. Left <see langword="null"/>, see <c>docs/guides/embeddings.md</c>'s
    /// "Embed a batch" for what <c>OnnxTextEmbedder</c> substitutes.
    /// </remarks>
    public int? MaxLength { get; init; }

    /// <summary>What to do with a sequence over <see cref="MaxLength"/>. Defaults to <see cref="TruncationStrategy.LongestFirst"/>.</summary>
    public TruncationStrategy Truncation { get; init; } = TruncationStrategy.LongestFirst;

    /// <summary>
    /// Group sequences of similar length together before inference, restoring the
    /// caller's order on output. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// A performance switch, never an observable one — the output order is always
    /// the input order. See <c>docs/guides/embeddings.md</c>'s "Embed a batch" for
    /// why sorting helps and when it does nothing (a batch no larger than
    /// <see cref="BatchSize"/>).
    /// </remarks>
    public bool SortByLength { get; init; } = true;

    /// <summary>How many sequences go into one ONNX Runtime call. Defaults to 32.</summary>
    /// <remarks>
    /// The equivalent of sentence-transformers' <c>batch_size</c>. Larger amortizes
    /// the per-call dispatch over more sequences; too large makes the padded
    /// activation tensor the constraint instead.
    /// </remarks>
    public int BatchSize { get; init; } = 32;
}
