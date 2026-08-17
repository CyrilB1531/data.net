# BatchEncoder.Encode

One string to ids, with the template applied.

<!-- docs-declaration -->

```csharp
public long[] Encode(string text)
```

**Parameters** — `text` is the string to encode.

**Returns** — `long[]`, the ids with the template's prefix and suffix tokens in place. `long`
because that is what ONNX models take.

**Example** — `[CLS]`, the text, `[SEP]`.

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
});

long[] ids = encoder.Encode("text");

int length = ids.Length;  // => 3
long first = ids[0];      // => 4
```

**Remarks** — three ids for a one-token text: `[CLS]` is `4`, `text` is `3`, `[SEP]` is `5`. The
template is the difference between this and
[`ISubwordTokenizer.Encode`](isubwordtokenizer-encode.md), which gives what the text *is* rather
than what the model expects around it.

No padding happens here — there is nothing to pad to. Truncation does, when `MaxLength` is set and
the text is longer, and the special tokens are counted against that budget rather than added on
top of it.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BatchEncoder.EncodeBatch`](batchencoder-encodebatch.md),
[`SpecialTokenTemplate`](specialtokentemplate.md).
