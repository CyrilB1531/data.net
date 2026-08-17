# ISubwordTokenizer

What the three tokenizers have in common: encode a string, and look a token up.

<!-- docs-declaration -->

```csharp
public interface ISubwordTokenizer
```

**Example** — the same code against whichever tokenizer the model came with.

```csharp
using Lodestar.Embeddings.Tokenization;

var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
{
    ["[UNK]"] = 0, ["token"] = 1, ["##ize"] = 2, ["text"] = 3,
};

ISubwordTokenizer tokenizer = new WordPieceTokenizer(
    vocab, unkToken: "[UNK]", continuationPrefix: "##", maxCharsPerWord: 100, lowercase: true);

TokenizationResult encoded = tokenizer.Encode("tokenize text");
int count = encoded.Ids.Count;  // => 3
```

**Remarks** — deliberately narrow. Decoding is **not** here, because only
[`BpeTokenizer`](bpetokenizer.md) can do it losslessly: byte-level BPE round-trips any input
exactly, while WordPiece has already thrown away the information about where words were split.
Putting `Decode` on the interface would promise something two of the three cannot keep.

[`BatchEncoder`](batchencoder.md) takes this interface rather than a concrete tokenizer, which is
what lets one batching path serve all three families.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`WordPieceTokenizer`](wordpiecetokenizer.md),
[`SentencePieceTokenizer`](sentencepiecetokenizer.md), [`BpeTokenizer`](bpetokenizer.md).

## Members

| Member | What it does |
| --- | --- |
| [`ISubwordTokenizer.Encode`](isubwordtokenizer-encode.md) | One string to tokens and ids. |
| [`ISubwordTokenizer.TryGetId`](isubwordtokenizer-trygetid.md) | The id of a token, if the vocabulary holds it. |
