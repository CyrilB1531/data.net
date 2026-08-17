# SplitBehavior

What a Split step does with the text it matched.

<!-- docs-declaration -->

```csharp
public enum SplitBehavior { Isolated, Removed, MergedWithPrevious, MergedWithNext, Contiguous }
```

**Members** — `Isolated` makes the match its own piece. `Removed` drops it. `MergedWithPrevious`
attaches it to the piece before, `MergedWithNext` to the piece after. `Contiguous` groups a run of
matches into one piece.

**Example** — the behaviour a Llama-3-shaped Split step declares.

```csharp
using Lodestar.Embeddings.Tokenization;

var step = new BpeSplitStep(BpePatterns.Llama3, SplitBehavior.Isolated, Invert: false);

SplitBehavior behavior = step.Behavior;  // => Isolated
```

**Remarks** — this decides what happens to the **delimiter**, which is the part a reader usually
assumes rather than checks. `Removed` throws the matched text away entirely: correct for a
whitespace splitter, catastrophic for a pattern matching word characters.

`MergedWithNext` is how a leading space stays attached to the word after it, which is what
byte-level BPE needs — the space is part of the token, and separating it changes every id.

The behaviour is read from the model's `tokenizer.json` rather than chosen, and reproducing it is
what makes the ids match HuggingFace's.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeSplitStep`](bpesplitstep.md), [`BpePatterns`](bpepatterns.md).
