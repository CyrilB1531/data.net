# EncodedBatch.Sequence

One sequence's ids, without the padding.

<!-- docs-declaration -->

```csharp
public ReadOnlySpan<long> Sequence(int index)
```

**Parameters** — `index` is the zero-based sequence, in the order the texts were given.

**Returns** — `ReadOnlySpan<long>` of that sequence's **true** length — `Lengths[index]`, not
`SequenceLength`. A view into `InputIds`, copying nothing.

**Example** — the shorter of two sequences, three ids rather than five.

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

int width = batch.SequenceLength;          // => 5
int actual = batch.Sequence(0).Length;     // => 3
```

**Remarks** — it stops at the true length, which is the whole reason to use it rather than slicing
`InputIds` yourself: the arithmetic is easy to get right and easy to get *subtly* wrong, and a
slice that includes padding reads as real tokens.

It is a view, not a copy, so it is only valid while the batch is.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EncodedBatch`](encodedbatch.md).
