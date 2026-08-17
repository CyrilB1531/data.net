# EncodingOptions

Length, truncation, template and batching — everything a [`BatchEncoder`](batchencoder.md) needs
beyond the tokenizer.

<!-- docs-declaration -->

```csharp
public sealed record EncodingOptions
```

**Properties** — `Template` is the [`SpecialTokenTemplate`](specialtokentemplate.md) wrapping each
sequence. `MaxLength` caps a sequence, special tokens **included**, and is nullable — `null` means no cap. `Truncation` is the
[`TruncationStrategy`](truncationstrategy.md) applied when that cap is met. `SortByLength` groups
similar lengths together to waste less padding. `BatchSize` is how many sequences are encoded per
chunk.

**Example** — a BERT-shaped encoder with a cap.

```csharp
using Lodestar.Embeddings.Tokenization;

var options = new EncodingOptions
{
    Template = SpecialTokenTemplate.Bert,
    MaxLength = 128,
    Truncation = TruncationStrategy.Right,
};

int? cap = options.MaxLength;  // => 128
```

**Remarks** — `MaxLength` counts the special tokens, which is what the model's own limit does: a
512-token BERT accepts 510 tokens of text plus `[CLS]` and `[SEP]`. Treating the cap as
text-only is how a sequence ends up two tokens over and is rejected by the model.

`SortByLength` is a throughput setting and not a correctness one — it changes how sequences are
grouped inside the encoder, never the order of the results. Turn it on for large corpora of uneven
length; it does nothing for uniform ones.

Being a `record`, two options with the same settings are equal.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BatchEncoder`](batchencoder.md), [`TruncationStrategy`](truncationstrategy.md),
[`SpecialTokenTemplate`](specialtokentemplate.md).
