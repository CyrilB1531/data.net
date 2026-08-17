# ONNX inference — `Lodestar.Embeddings`

One type, [`OnnxTextEmbedder`](onnx/onnxtextembedder.md): it runs a sentence-transformer model and
gives you a vector per text. It is the only place in Lodestar where a **model file** is required,
and the only place ONNX Runtime is referenced — that dependency is deliberately confined to this
namespace so the rest of the package has none.

## Why every example here is unexecuted

Weights are never committed to this repository. A running example would need a model of tens of
megabytes, and [`decisions/0003`](../../decisions/0003-provenance-and-licensing.md) rules that
out; `tools/fetch_*.py` pulls vocabularies against a pinned SHA-256 when they are needed, and
weights are not among them.

So the fences on these pages **compile** against the packed package and are marked
`docs-run: skip`, which is what that marker is for. The same exclusion is declared in the
packaging sample, where `OnnxTextEmbedder` is one of its two documented exclusions.

## Where the vectors come from, and where they go

This namespace produces vectors. Turning text into the token ids it wants is
`Lodestar.Embeddings.Tokenization`; reducing a sequence of vectors to one is
`Lodestar.Embeddings.Pooling`; searching a set of them is
`Lodestar.Embeddings.Search`. This page is the middle step of four, and the only one
that needs a file from outside. The other three are named without links here because their pages
do not exist yet — they arrive with #226, #233 and #231, and a link written ahead of its target is
a link that is broken until then.

## Types

| Type | What it is |
| --- | --- |
| [`OnnxTextEmbedder`](onnx/onnxtextembedder.md) | Runs an ONNX sentence-transformer and returns vectors. |

## See also

- [Semantic search with embeddings](../../guides/embeddings.md) — the guide, end to end.
- [Python → C# equivalence](../../equivalence.md).
