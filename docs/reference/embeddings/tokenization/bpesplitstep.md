# BpeSplitStep

A Split step ahead of ByteLevel, as a Llama-3 or Qwen2 file declares one.

<!-- docs-declaration -->

```csharp
public sealed record BpeSplitStep
```

**Properties** — `Pattern` is the regex. `Behavior` is the [`SplitBehavior`](splitbehavior.md)
applied to what it matches. `Invert` splits on what the pattern does **not** match instead.

**Example** — the step a Llama-3 file puts before ByteLevel.

```csharp
using Lodestar.Embeddings.Tokenization;

var step = new BpeSplitStep(BpePatterns.Llama3, SplitBehavior.Isolated, Invert: false);

bool inverted = step.Invert;  // => False
```

**Remarks** — stock GPT-2 declares a bare ByteLevel pre-tokenizer with no Split in front of it. A
Llama-3 or Qwen2 file declares a `Sequence` of Split then ByteLevel, and the Split's behaviour
decides what happens to the text **between** matches — which is the whole reason this type exists
rather than a bare pattern string.

`Invert` reads oddly and is what several shipped files use: it is easier to write a pattern for
the text you want to keep than for the delimiters you want to cut on.

Where a model declares none, [`BpeVocabulary.PreSplit`](bpevocabulary.md) is `null`, and that is
different from declaring one that matches nothing.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SplitBehavior`](splitbehavior.md), [`BpeVocabulary`](bpevocabulary.md).
