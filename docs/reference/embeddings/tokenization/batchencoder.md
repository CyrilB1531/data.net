# BatchEncoder

Strings in, a padded rectangular batch with an attention mask out — what a model actually wants.

<!-- docs-declaration -->

```csharp
public sealed class BatchEncoder
```

**Constructor** — `BatchEncoder(ISubwordTokenizer tokenizer, EncodingOptions? options = null)`.
The template's special tokens are resolved against the tokenizer's vocabulary **here**, so a
mismatch is refused at construction rather than encoded into something the model misreads.

**Properties** — `Options` is the [`EncodingOptions`](encodingoptions.md) it was built with.

**Example** — two texts of different lengths, padded to one rectangle.

```csharp
using Lodestar.Embeddings.Tokenization;

var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
{
    ["[UNK]"] = 0, ["token"] = 1, ["##ize"] = 2, ["text"] = 3,
    ["[CLS]"] = 4, ["[SEP]"] = 5, ["[PAD]"] = 6,
};
var tokenizer = new WordPieceTokenizer(
    vocab, unkToken: "[UNK]", continuationPrefix: "##", maxCharsPerWord: 100, lowercase: true);

var encoder = new BatchEncoder(tokenizer, new EncodingOptions
{
    Template = SpecialTokenTemplate.Bert,
    MaxLength = 8,
});

EncodedBatch batch = encoder.EncodeBatch(["text", "tokenize text"]);

int rows = batch.Count;             // => 2
int width = batch.SequenceLength;   // => 5
```

**Remarks** — the batch is as wide as its **longest** sequence, and the shorter ones are padded to
match. That is what makes it a rectangle a model can consume, and it is why
[`EncodedBatch.Lengths`](encodedbatch.md) exists: the padding has to be ignored downstream, and the
attention mask is how the model is told to.

Asking for a template the vocabulary cannot satisfy throws at construction:

> The tokenizer's vocabulary has no token '[CLS]', which the special-token template requires.

That is deliberate. The alternative — encoding without the token, or with the unknown token in its
place — produces a batch the model accepts and misinterprets, which is the failure that costs a
day.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EncodingOptions`](encodingoptions.md),
[`SpecialTokenTemplate`](specialtokentemplate.md), [`EncodedBatch`](encodedbatch.md).

## Members

| Member | What it does |
| --- | --- |
| [`BatchEncoder.Encode`](batchencoder-encode.md) | One string to ids, template applied. |
| [`BatchEncoder.EncodeBatch`](batchencoder-encodebatch.md) | Many strings to one padded rectangle. |
