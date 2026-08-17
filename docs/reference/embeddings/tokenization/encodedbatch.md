# EncodedBatch

The rectangular result: ids, attention mask, and the true lengths the padding hides.

<!-- docs-declaration -->

```csharp
public sealed class EncodedBatch
```

**Properties** — `Count` is how many sequences. `SequenceLength` is the width every one was padded
to. `InputIds` holds all the ids, row-major, `Count × SequenceLength` of them. `AttentionMask` is
the same shape, `1` for a real token and `0` for padding. `Lengths` is each sequence's true length
before padding.

**Example** — two texts, and what the rectangle says about them.

```csharp
using Lodestar.Embeddings.Tokenization;

var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
{
    ["[UNK]"] = 0, ["token"] = 1, ["##ize"] = 2, ["text"] = 3,
    ["[CLS]"] = 4, ["[SEP]"] = 5, ["[PAD]"] = 6,
};
var tokenizer = new WordPieceTokenizer(
    vocab, unkToken: "[UNK]", continuationPrefix: "##", maxCharsPerWord: 100, lowercase: true);
var encoder = new BatchEncoder(tokenizer, new EncodingOptions { Template = SpecialTokenTemplate.Bert });

EncodedBatch batch = encoder.EncodeBatch(["text", "tokenize text"]);

int cells = batch.InputIds.Length;  // => 10
int first = batch.Lengths[0];       // => 3
```

**Remarks** — ten cells for two sequences of width five, three of which are padding. The
**attention mask is not optional**: a model given padding without being told it is padding will
attend to it and produce a different vector. Passing `InputIds` alone is the most common way to
get plausible, wrong embeddings.

`InputIds` is row-major and flat rather than jagged, because that is the layout ONNX Runtime wants
and building it here avoids a copy at the boundary.

[`Sequence`](encodedbatch-sequence.md) is how to read one row without arithmetic.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BatchEncoder.EncodeBatch`](batchencoder-encodebatch.md),
[`OnnxTextEmbedder.EmbedBatch`](../onnx/onnxtextembedder-embedbatch.md).

## Members

| Member | What it does |
| --- | --- |
| [`EncodedBatch.Sequence`](encodedbatch-sequence.md) | One sequence's ids, without the padding. |
