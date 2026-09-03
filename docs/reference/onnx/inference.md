# ONNX inference — `Lodestar.Onnx`

One type, [`OnnxTextEmbedder`](inference/onnxtextembedder.md): it runs a sentence-transformer model
and gives you a vector per text. It is the only place in Lodestar where a **model file** is
required, and the only place ONNX Runtime is referenced — that dependency is what this package is
for. A caller who tokenizes, pools or searches without inferring never restores a native runtime.

## Why every example here is unexecuted

Weights are never committed to this repository. A running example would need a model of tens of
megabytes, and [`decisions/0003`](../../decisions/0003-provenance-and-licensing.md) rules that
out; `tools/fetch_*.py` pulls vocabularies against a pinned SHA-256 when they are needed, and
weights are not among them.

So the fences on these pages **compile** against the packed package and are marked
`docs-run: skip`, which is what that marker is for. The same exclusion is declared in the
packaging sample, where `OnnxTextEmbedder` is one of its two documented exclusions.

## Where the vectors come from, and where they go

This package produces vectors, and does nothing else. Turning text into the token ids it wants is
[`Lodestar.Embeddings.Tokenization`](../embeddings/tokenization.md); reducing a sequence of
vectors to one is [`Lodestar.Embeddings.Pooling`](../embeddings/pooling.md) — which this type
also does internally; searching a set of them is
[`Lodestar.Embeddings.Search`](../embeddings/search.md). This page is the middle step of four, and
the only one that needs a file from outside, which is why it is the only step that ships apart.

## Types

| Type | What it is |
| --- | --- |
| [`OnnxTextEmbedder`](inference/onnxtextembedder.md) | Runs an ONNX sentence-transformer and returns vectors. |

## See also

- [ONNX inference](../../guides/onnx.md) — the guide for this package.
- [Semantic search with embeddings](../../guides/embeddings.md) — the chain it sits in.
- [Python → C# equivalence](../../equivalence.md).
