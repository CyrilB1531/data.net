# OnnxTextEmbedder

Runs an ONNX sentence-transformer and returns one vector per text.

<!-- docs-declaration -->

```csharp
public sealed class OnnxTextEmbedder : IDisposable
```

**Constructor** — takes the path to an ONNX model. Constructing it loads that model, which is why
this type is the one place in Lodestar that needs a file you supply.

**Properties** — `Dimension` is the width of the vectors the model produces. `MaxSequenceLength`
is the longest input, in tokens, the model accepts; longer inputs are truncated by the encoder
rather than refused here.

**Example** — the shape of a call. It is not executed: see below.

<!-- docs-run: skip - constructing it loads an ONNX model, and model weights are never committed (CONTRIBUTING.md, ADR 0003) -->

```csharp
using Lodestar.Embeddings.Onnx;

using var embedder = new OnnxTextEmbedder("model.onnx");

float[][] vectors = embedder.EmbedBatch(["a first sentence", "a second one"]);
int width = embedder.Dimension;
```

**Remarks** — **every fence on this page and its members is `docs-run: skip`**, and that is not an
oversight. A running example would need a model of tens of megabytes, and weights are never
committed to this repository — [`decisions/0003`](../../../decisions/0003-provenance-and-licensing.md)
is the rule, and the packaging sample declares the same exclusion for the same reason. The fences
are still **compiled** against the packed package, so a renamed member still fails CI; only the
values are unchecked, which is why none of them carries a `// =>`.

ONNX Runtime is referenced by this namespace and nowhere else in Lodestar. A consumer who never
touches this type never pays for that dependency, which is the reason for the isolation.

It is `IDisposable` and holds native resources: the model session outlives garbage collection, so
[`Dispose`](onnxtextembedder-dispose.md) is not optional.

**Applies to** — net10.0, netstandard2.0.

**See also** — `BatchEncoder`, the
[embeddings guide](../../../guides/embeddings.md), the
[Python equivalence table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
| [`OnnxTextEmbedder.Dispose`](onnxtextembedder-dispose.md) | Release the native model session. |
| [`OnnxTextEmbedder.Embed`](onnxtextembedder-embed.md) | One vector, from token ids you already have. |
| [`OnnxTextEmbedder.EmbedBatch`](onnxtextembedder-embedbatch.md) | A vector per text, in one session run. |
