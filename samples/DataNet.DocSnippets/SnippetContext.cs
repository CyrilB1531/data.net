using DataNet.Embeddings.Tokenization;

namespace DataNet.DocSnippets;

// The symbols the guides introduce in prose rather than in a fence: a page that
// says "…later, in another process" is not going to re-declare its corpus, and
// making it do so would be worse documentation.
//
// This is scaffolding, not a copy of any snippet — no line here appears in a
// guide. The names are camelCase because they are the names the guides use, and
// the guides are the side that must not move. The values are irrelevant: this
// project is compiled and never run, because what the check catches is an API
// that moved, not a wrong number.

internal sealed partial class Vectorization
{
    /// <summary>The corpus the first fence builds and later ones reuse.</summary>
    public readonly string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];

    /// <summary>The corpus a vectorizer is fitted on before being saved.</summary>
    public readonly string[] trainingDocuments = ["the cat eats", "the dog eats"];

    /// <summary>Documents scored by an already-fitted vectorizer.</summary>
    public readonly string[] newDocuments = ["the cat sleeps"];
}

internal sealed partial class Embeddings
{
    /// <summary>One embedding, as <c>OnnxTextEmbedder.Embed</c> returns it.</summary>
    public readonly float[] vector = new float[384];

    /// <summary>The embedded corpus an index is filled from.</summary>
    public readonly float[][] corpusVectors = [new float[384]];

    /// <summary>The embedding a search is run for.</summary>
    public readonly float[] queryVector = new float[384];

    /// <summary>The tokenizer an embedder is built with; the guide builds one several fences earlier.</summary>
    public readonly WordPieceTokenizer wp = new(
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["[UNK]"] = 0,
            ["[CLS]"] = 1,
            ["[SEP]"] = 2,
            ["[PAD]"] = 3,
        },
        "[UNK]");

    /// <summary>The corpus a batch call embeds.</summary>
    public readonly string[] texts = ["the cat sat", "the dog ran"];

    /// <summary>Token ids the single-sequence entry point is handed.</summary>
    public readonly long[] ids = [1, 4, 2];

    /// <summary>The mask that goes with them.</summary>
    public readonly long[] mask = [1, 1, 1];
}
