namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// What the encoding pipeline needs of a tokenizer: turn text into ids, and
/// resolve a special token by name.
/// </summary>
/// <remarks>
/// <para>
/// Matches the surface HuggingFace <c>tokenizers.Tokenizer</c> exposes as
/// <c>encode(text)</c> and <c>token_to_id(token)</c>. Both
/// <see cref="WordPieceTokenizer"/> and <see cref="SentencePieceTokenizer"/>
/// implement it, so <see cref="BatchEncoder"/> — and therefore
/// <c>OnnxTextEmbedder.EmbedBatch</c> — works with either without knowing which.
/// </para>
/// <para>
/// <see cref="TryGetId"/> is what makes the special-token template *data*: the
/// template names <c>[CLS]</c> or <c>&lt;s&gt;</c>, and the tokenizer's own
/// vocabulary decides what id that is. Nothing is hardcoded.
/// </para>
/// </remarks>
public interface ISubwordTokenizer
{
    /// <summary>Tokenizes <paramref name="text"/> into sub-word tokens and their ids.</summary>
    /// <remarks>Matches <c>tokenizers.Tokenizer.encode(text)</c>, without the post-processor.</remarks>
    /// <param name="text">The text to tokenize.</param>
    TokenizationResult Encode(string text);

    /// <summary>Looks up the id of a literal vocabulary entry, special tokens included.</summary>
    /// <remarks>Matches <c>tokenizers.Tokenizer.token_to_id(token)</c>.</remarks>
    /// <param name="token">The token string, e.g. <c>[CLS]</c>.</param>
    /// <param name="id">Receives the id when the token is present.</param>
    /// <returns><see langword="true"/> when the vocabulary contains <paramref name="token"/>.</returns>
    bool TryGetId(string token, out int id);
}
