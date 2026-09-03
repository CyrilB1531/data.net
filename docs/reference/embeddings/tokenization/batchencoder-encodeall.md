# BatchEncoder.EncodeAll

Every text as its own row of ids, unpadded.

<!-- docs-declaration -->

```csharp
public IReadOnlyList<long[]> EncodeAll(IEnumerable<string> texts, CancellationToken cancellationToken = default)
```

**Parameters** — `texts` are the strings to encode. `cancellationToken` is observed between texts,
because tokenizing a large corpus is not instant.

**Returns** — one `long[]` per text, in the order given, each already carrying its template tokens
and already truncated. No padding and no rectangle.

**Exceptions** — `OperationCanceledException` when cancelled. `ArgumentNullException` when `texts`
is null.

**Example** — two texts of different lengths stay different lengths.

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

int shorter = sequences[0].Length;   // => 3
int longer = sequences[1].Length;    // => 5
```

**Remarks** — this is the front half of [`EncodeBatch`](batchencoder-encodebatch.md), and
[`Pad`](batchencoder-pad.md) is the back half. `EncodeBatch` is the two of them in one call, over
the whole corpus at once; the pair exists for a caller that has to decide how to group the rows
before they are laid out — running a corpus through an inference session a few rows at a time, say,
where the widest row in a group is what the group costs.

Knowing the lengths *before* padding is the point. A group of similar lengths wastes little; a
group that mixes 3 and 512 pays 512 for every row in it.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BatchEncoder.Pad`](batchencoder-pad.md),
[`BatchEncoder.EncodeBatch`](batchencoder-encodebatch.md), [`EncodedBatch`](encodedbatch.md).
