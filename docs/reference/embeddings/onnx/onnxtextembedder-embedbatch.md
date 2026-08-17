# OnnxTextEmbedder.EmbedBatch

A vector per text, in one session run.

<!-- docs-declaration -->

```csharp
public float[][] EmbedBatch(IEnumerable<string> texts, EncodingOptions options = null, CancellationToken cancellationToken = default)
public float[][] EmbedBatch(IEnumerable<string> texts, BatchEncoder encoder, CancellationToken cancellationToken = default)
public float[][] EmbedBatch(EncodedBatch batch, CancellationToken cancellationToken = default)
```

**Parameters** — `texts` are the strings to embed. `options` tunes the encoding — padding,
truncation, the maximum length. `encoder` is a `BatchEncoder`
you have already configured, for when the model's tokenizer is not the default. `batch` is an
`EncodedBatch` you encoded yourself. `cancellationToken`
abandons the run.

**Returns** — `float[][]`, one vector of `Dimension` per input text, in input order.

**Exceptions** — `ObjectDisposedException` after [`Dispose`](onnxtextembedder-dispose.md).
`OperationCanceledException` when cancelled.

**Example** — the shortest path from strings to vectors.

<!-- docs-run: skip - constructing it loads an ONNX model, and model weights are never committed -->

```csharp
using Lodestar.Embeddings.Onnx;

using var embedder = new OnnxTextEmbedder("model.onnx");

float[][] vectors = embedder.EmbedBatch(
[
    "the cat sat on the mat",
    "a dog lay on the rug",
]);
```

**Remarks** — three overloads for one operation, and the choice is about **who owns the
tokenizer**. The first owns it for you and is right when the model ships a standard vocabulary.
The second takes an encoder you built, for a model whose tokenizer differs. The third takes an
already-encoded batch, for when encoding happened elsewhere — on another thread, or once for
several models.

Batching is not merely convenient. A session run has a fixed cost, so embedding a hundred texts
one at a time costs a hundred of those; the vectors are identical either way, and the time is not.

Order is preserved, so the *n*th vector belongs to the *n*th text however the batch was padded
internally.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`OnnxTextEmbedder.Embed`](onnxtextembedder-embed.md),
`BatchEncoder`,
`EncodedBatch`.
