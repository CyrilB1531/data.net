# BatchEncoder.EncodeBatch

Many strings to one padded rectangle.

<!-- docs-declaration -->

```csharp
public EncodedBatch EncodeBatch(IEnumerable<string> texts, CancellationToken cancellationToken = default)
```

**Parameters** — `texts` are the strings to encode. `cancellationToken` abandons the work.

**Returns** — [`EncodedBatch`](encodedbatch.md): the ids, the attention mask, and each sequence's
true length before padding.

**Exceptions** — `OperationCanceledException` when cancelled.

**Example** — a short text and a longer one, in one rectangle.

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

int width = batch.SequenceLength;   // => 5
int shorter = batch.Lengths[0];     // => 3
int longer = batch.Lengths[1];      // => 5
```

**Remarks** — the rectangle is five wide because the longer text needs five slots; the shorter one
occupies three and is padded. `Lengths` records that, and the attention mask carries it to the
model.

Order is preserved: row *n* is text *n*, whatever `SortByLength` does internally. That option
groups similar lengths together to reduce padding waste, and it does **not** change the order of
the results — a batch that reordered its output would be a trap rather than an optimization.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EncodedBatch`](encodedbatch.md), [`EncodingOptions`](encodingoptions.md).
