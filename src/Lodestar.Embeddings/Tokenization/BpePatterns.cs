namespace Lodestar.Embeddings.Tokenization;

/// <summary>
/// The pre-tokenization patterns a model splits on.
/// </summary>
/// <remarks>
/// Each is the <c>Split</c> pattern from that model's own <c>tokenizer.json</c> —
/// the split decides where a token can begin, so the wrong one produces plausible
/// tokens and wrong embeddings. Exposed as properties, not <c>const</c> fields: a
/// <c>const</c> is a compile-time constant, so a consumer referencing one emits no
/// member reference, invisible to the sample's packaging gate.
/// </remarks>
public static class BpePatterns
{
    /// <summary>The classic lineage's pattern. Matches <c>pre_tokenizers.Whitespace()</c>.</summary>
    /// <remarks>
    /// Splits on word boundaries and isolates punctuation, unlike
    /// <c>WhitespaceSplit</c> (<c>\S+</c>, not implemented here). This is the
    /// pattern <see cref="BpeTokenizer"/> used to supply when a vocabulary
    /// declared none, which it now refuses instead — see
    /// <see cref="BpeVocabulary.NoPreTokenizer"/>.
    /// </remarks>
    public static string Whitespace { get; } = @"\w+|[^\w\s]+";

    /// <summary>GPT-2's pattern. Matches <c>pre_tokenizers.ByteLevel(use_regex=True)</c>.</summary>
    public static string Gpt2 { get; } =
        @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+";

    /// <summary>Llama-3's pattern, from its <c>tokenizer.json</c>.</summary>
    public static string Llama3 { get; } =
        @"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+";

    /// <summary>Qwen2's pattern, from its <c>tokenizer.json</c>.</summary>
    public static string Qwen2 { get; } =
        @"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+";
}
