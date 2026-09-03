# BatchEncoder.Pad

A window of encoded rows, laid out as one rectangle.

<!-- docs-declaration -->

```csharp
public EncodedBatch Pad(IReadOnlyList<long[]> sequences, int start, int count, int[] order = null)
```

**Parameters** — `sequences` are unpadded encodings, as
[`EncodeAll`](batchencoder-encodeall.md) returns them. `start` and `count` select the window to lay
out. `order` is an optional indirection: with it, row *i* of the batch is
`sequences[order[start + i]]`, which is how length grouping is expressed without reordering
`sequences` itself.

**Returns** — [`EncodedBatch`](encodedbatch.md), padded to the longest row *in this window* — not
to `MaxLength`, and not to the longest row in `sequences`.

**Exceptions** — `ArgumentNullException` when `sequences` is null.
`ArgumentOutOfRangeException` when `start` or `count` is negative, or when the window reaches past
the end of `sequences` (or of `order`, when one is given).

**Example** — the same two texts, first together, then the long one alone.

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

IReadOnlyList<long[]> sequences = encoder.EncodeAll(["text", "tokenize text"]);

EncodedBatch both = encoder.Pad(sequences, 0, 2);
int width = both.SequenceLength;         // => 5

EncodedBatch shortOnly = encoder.Pad(sequences, 0, 1);
int narrower = shortOnly.SequenceLength; // => 3
```

**Remarks** — the second call is the whole reason this is public. Padding per window rather than
per corpus is what makes grouping worth doing: three rows of length 3 cost a 3-wide rectangle, and
only the window that actually contains a 512-token row pays for one.

A window of zero rows still gets one column, masked off. An empty dimension is a tensor shape an
inference runtime will refuse, and "no tokens" has to be expressible.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BatchEncoder.EncodeAll`](batchencoder-encodeall.md),
[`BatchEncoder.EncodeBatch`](batchencoder-encodebatch.md), [`EncodedBatch`](encodedbatch.md).
