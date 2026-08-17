# SpecialTokenTemplate

Which tokens wrap a sequence, and what pads it — per model family.

<!-- docs-declaration -->

```csharp
public sealed record SpecialTokenTemplate
```

**Properties** — `PrefixTokens` go before the text and `SuffixTokens` after. `PadToken` fills the
rectangle. `SpecialTokenCount` is how many of the budget they consume. `Bert`, `Roberta`, `T5` and
`None` are the four ready-made templates.

**Example** — BERT costs two tokens of the budget; none costs none.

```csharp
using Lodestar.Embeddings.Tokenization;

int bert = SpecialTokenTemplate.Bert.SpecialTokenCount;  // => 2
int none = SpecialTokenTemplate.None.SpecialTokenCount;  // => 0
```

**Remarks** — the template must match the **model**, not the tokenizer family: a RoBERTa model and
a BERT model can share a WordPiece-shaped vocabulary and still expect different wrappers. Getting
it wrong produces a batch the model accepts and reads differently, which is the failure mode this
whole namespace exists to make hard.

It must also match the **vocabulary**, and that is checked: constructing a
[`BatchEncoder`](batchencoder.md) whose vocabulary lacks `[CLS]` throws rather than substituting
the unknown token.

`None` is for models that want none — many sentence-transformer exports do their own wrapping — and
for measuring what the text alone encodes to.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BatchEncoder`](batchencoder.md), [`EncodingOptions`](encodingoptions.md).

## Members

| Member | What it does |
| --- | --- |
| [`SpecialTokenTemplate.Equals`](specialtokentemplate-equals.md) | Value equality over the tokens. |
| [`SpecialTokenTemplate.GetHashCode`](specialtokentemplate-gethashcode.md) | A hash consistent with it. |
