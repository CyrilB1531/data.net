# OnnxTextEmbedder.Dispose

Release the native model session.

<!-- docs-declaration -->

```csharp
public void Dispose()
```

**Example** — `using` is the whole of it.

<!-- docs-run: skip - constructing it loads an ONNX model, and model weights are never committed -->

```csharp
using Lodestar.Embeddings.Onnx;

using var embedder = new OnnxTextEmbedder("model.onnx");
float[] vector = embedder.Embed([101, 2054, 102], [1, 1, 1]);
// Dispose runs at the end of the scope.
```

**Remarks** — the session is **native memory**, not managed, so it is not reclaimed by a garbage
collection and a forgotten embedder holds the model until the process ends. Loading several models
in a long-running service without disposing is how that becomes visible.

Disposing twice is safe. Calling [`Embed`](onnxtextembedder-embed.md) or
[`EmbedBatch`](onnxtextembedder-embedbatch.md) afterwards is not, and throws.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`OnnxTextEmbedder`](onnxtextembedder.md).
