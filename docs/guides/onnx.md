# ONNX inference

`Lodestar.Onnx` is the one package in Lodestar with an external dependency: ONNX
Runtime. It holds a single type, `OnnxTextEmbedder`, which runs a transformer
encoder you export yourself and pools its token outputs into a sentence vector.

It sits in the middle of the chain the
[semantic search guide](embeddings.md) walks end to end — **tokenize → infer →
pool → index → query** — and ships apart so that the other four steps reach a
caller who does not want a native runtime.

```bash
dotnet add package Lodestar.Onnx
```

That pulls `Lodestar.Embeddings` with it, for the tokenizers and the encoding
options below; the reverse is not true.

## Embed a batch

Weights are **not** shipped: export an encoder (e.g. a sentence-transformers
model) to ONNX and pass its path, together with the tokenizer it was trained
with.

```csharp
using Lodestar.Onnx;
using Lodestar.Embeddings.Tokenization;

using var embedder = new OnnxTextEmbedder("model.onnx", wp);

float[][] vectors = embedder.EmbedBatch(texts, new EncodingOptions
{
    Template = SpecialTokenTemplate.Bert,          // [CLS] … [SEP]
    MaxLength = 256,                               // special tokens included, as in HuggingFace
    Truncation = TruncationStrategy.LongestFirst,
    BatchSize = 32,
});
```

**The library inserts the special tokens.** `SpecialTokenTemplate` carries them
as data — `Bert` is `[CLS] … [SEP]`, `Roberta` is `<s> … </s>`, `T5` appends
`</s>` and nothing else, and a model that wraps its input differently takes a
template you write out. The tokens are named, never numbered: the id comes from
the model's own vocabulary, so a vocabulary that places `[CLS]` anywhere works,
and one that lacks it fails at construction instead of embedding a plausible
wrong id.

It also builds the attention mask, which is the part a caller most often gets
wrong. Each sub-batch is padded to **its own longest sequence**, never to
`MaxLength` — padding every batch to 512 when the median length is 30 wastes
most of the compute — and the padded positions are masked to 0 so they cannot
reach the pooled vector. That last property is asserted directly: a text
embedded in a batch gets the same vector, bit for bit, as the same text embedded
alone.

`SortByLength` groups sequences of similar length into the same call so the long
ones stop dictating the width of every row they share it with. The caller's
order is restored before returning, so it is a performance switch and never an
observable one. `EmbedBatch` takes a `CancellationToken`, observed while
tokenizing and between sub-batches.

`MaxLength` left null asks the model for its declared maximum — which most
exports do not have, since `torch.onnx.export` with `dynamic_axes` writes a
symbolic sequence dimension. The real positional limit lives in the model's
`config.json`, not in the graph, so for a real encoder pass it explicitly.

The single-sequence entry point is still there for a caller who owns the
tokenization:

```csharp
using Lodestar.Onnx;

using var embedder = new OnnxTextEmbedder("model.onnx");
float[] single = embedder.Embed(ids, mask);   // mean pooling + L2 built in
```

`OnnxTextEmbedder` feeds `token_type_ids` only if the model declares it, performs
masked mean pooling and L2-normalizes. It takes the token-embeddings output —
the only output when the model has one, else the first of `last_hidden_state`,
`token_embeddings`, `sentence_embedding` and `output` that it declares, unless
you name one. It refuses an output whose rank is neither
`[batch, sequence, dim]` nor the `[batch, dim]` of a model that pools
internally.
