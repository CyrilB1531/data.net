# Semantic search with embeddings

`DataNet.Embeddings` covers the full chain: **tokenize → infer (ONNX) → pool →
index → query**. ONNX Runtime is isolated here so that `DataNet.Text` stays
dependency-free.

```bash
dotnet add package DataNet.Embeddings
```

## Sub-word tokenization

Two tokenizers, depending on the model. **WordPiece** (BERT):

```csharp
using DataNet.Embeddings.Tokenization;

var vocab = new Dictionary<string, int> { ["[UNK]"] = 0, ["play"] = 1, ["##ing"] = 2, /* … */ };
var wp = new WordPieceTokenizer(vocab);
TokenizationResult t = wp.Encode("playing");   // pieces: play ##ing
```

**SentencePiece** (ALBERT, T5, camemBERT, XLM-R) — unigram Viterbi segmentation:

```csharp
// vocab = list of SentencePiece(piece, score, id) from the model
var sp = new SentencePieceTokenizer(vocab);
TokenizationResult t = sp.Encode("the quick brown fox");
```

> The tokenization must match the model's **exactly**, otherwise the embeddings
> are wrong (§5 of the brief). Both tokenizers are validated token-for-token
> against HuggingFace `tokenizers` / the `sentencepiece` library.

## Run an ONNX model + pooling

Weights are **not** shipped: export an encoder (e.g. a sentence-transformers
model) to ONNX and pass its path.

```csharp
using DataNet.Embeddings.Onnx;

using var embedder = new OnnxTextEmbedder("model.onnx");
long[] ids = /* wp.Encode(text).Ids, with [CLS]/[SEP] if the model expects them */;
long[] mask = /* 1 per real token, 0 for padding */;
float[] vector = embedder.Embed(ids, mask);   // mean pooling + L2 built in
```

`OnnxTextEmbedder` feeds `token_type_ids` only if the model declares it, performs
masked mean pooling and L2-normalizes.

## Index a corpus and query it

```csharp
using DataNet.Embeddings.Search;

var index = new EmbeddingIndex(dimension: vector.Length);
foreach (float[] v in corpusVectors) index.Add(v);   // normalized on insertion

IReadOnlyList<SearchResult> hits = index.Search(queryVector, k: 5);
foreach (var h in hits) Console.WriteLine($"#{h.Index}  score={h.Score:F3}");
```

The search is an **exhaustive SIMD-vectorized** cosine (`System.Numerics.Vector`) —
the right default up to a few hundred thousand vectors. An approximate index
(HNSW) is only worth adding once a real need is demonstrated.
