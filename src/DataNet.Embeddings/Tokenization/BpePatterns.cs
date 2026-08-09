namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// The pre-tokenization patterns the byte-level models split on.
/// </summary>
/// <remarks>
/// <para>
/// Each is the <c>Split</c> pattern from that model's own <c>tokenizer.json</c>.
/// They matter more than they look: the split decides where a token can begin,
/// so a model tokenized with the wrong one produces plausible tokens and wrong
/// embeddings.
/// </para>
/// <para>
/// Exposed as properties rather than <c>const</c> fields on purpose. A
/// <c>const</c> is a compile-time constant, so a consumer referencing it emits
/// no member reference — and the sample's packaging gate, which proves the
/// public surface is reachable, would be structurally unable to see it.
/// </para>
/// </remarks>
public static class BpePatterns
{
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
