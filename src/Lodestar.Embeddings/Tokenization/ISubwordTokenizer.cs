namespace Lodestar.Embeddings.Tokenization;

/// <summary>
/// What the encoding pipeline needs of a tokenizer: turn text into ids, and
/// resolve a special token by name.
/// </summary>
/// <remarks>
/// Matches the surface HuggingFace <c>tokenizers.Tokenizer</c> exposes as
/// <c>encode(text)</c> and <c>token_to_id(token)</c>, implemented by
/// <see cref="WordPieceTokenizer"/>, <see cref="SentencePieceTokenizer"/> and
/// <see cref="BpeTokenizer"/> alike, so <see cref="BatchEncoder"/> works with any.
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
