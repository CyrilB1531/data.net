# OnnxTextEmbedder.Embed

One vector, from token ids you already have.

<!-- docs-declaration -->

```csharp
public float[] Embed(ReadOnlySpan<long> inputIds, ReadOnlySpan<long> attentionMask)
```

**Parameters** — `inputIds` are the token ids for one text, as the model's own tokenizer produces
them. `attentionMask` is the same length, `1` for a real token and `0` for padding.

**Returns** — `float[]` of length `Dimension`, the pooled vector for that text.

**Exceptions** — `ArgumentException` when the two spans differ in length.
`ObjectDisposedException` after [`Dispose`](onnxtextembedder-dispose.md) — the type
checks a flag of its own, because reaching a disposed ONNX Runtime session surfaces as a
null dereference from inside it, naming neither the object nor the mistake.

**Example** — ids from a tokenizer, one text at a time.

<!-- docs-run: skip - constructing it loads an ONNX model, and model weights are never committed -->

```csharp
using Lodestar.Embeddings.Onnx;

using var embedder = new OnnxTextEmbedder("model.onnx");

long[] ids = [101, 2054, 2003, 102];
long[] mask = [1, 1, 1, 1];

float[] vector = embedder.Embed(ids, mask);
```

**Remarks** — this is the low-level entry: it takes ids rather than text, so the tokenizer is
yours to choose and yours to match to the model. Mismatching them produces vectors that are
confidently wrong rather than an error, which is the failure worth guarding against — use the
vocabulary that shipped with the model.

For text rather than ids, [`EmbedBatch`](onnxtextembedder-embedbatch.md) takes strings and does the
encoding. It is also the faster path for more than one text: a session run has a fixed cost that a
batch amortises.

The `attentionMask` matters even for a single unpadded text, where it is all ones — the model
reads it, and pooling uses it to ignore padding.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`OnnxTextEmbedder.EmbedBatch`](onnxtextembedder-embedbatch.md),
`BatchEncoder`.
