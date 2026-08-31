# From Python to .NET — migration inventory

This page is Lodestar's **migration hub**. It answers a simple question: "I do this
in Python, what do I do in C#?"

The project's guiding principle (see the
[rationale](https://github.com/CyrilB1531/lodestar/blob/main/README.md)) is
**honest**: we don't rewrite Python's data-science ecosystem. Most of it already
exists in .NET, and Python's dense linear algebra relies on Fortran BLAS/LAPACK
kernels there's no point reimplementing. We **use** what exists, and only **write**
native code where .NET has a real gap: **text** (similarity, vectorization).

## The four columns

| Python | Role | .NET recommendation | Verdict |
| --- | --- | --- | --- |
| **PyTorch** | tensors, autograd, training, GPU | [TorchSharp](https://github.com/dotnet/TorchSharp) (= libtorch); [ONNX Runtime](https://onnxruntime.ai/) for inference only | ✅ **Use** |
| **matplotlib** | plotting | [ScottPlot](https://scottplot.net/), [Plotly.NET](https://plotly.net/), OxyPlot | ✅ **Use** |
| **NumPy** | N-dim arrays, dense algebra | [Math.NET Numerics](https://numerics.mathdotnet.com/) (+ native MKL/OpenBLAS provider); `System.Numerics.Tensors` | ✅ **Use** |
| **scikit-learn** | classical ML, pipelines, metrics | [ML.NET](https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet); [SharpLearning](https://github.com/mdabros/SharpLearning) | ✅ **Use** *except* text vectorization → **Lodestar.Text** and classification metrics → **Lodestar.Metrics** |
| **pandas** | DataFrame, groupby, IO | [`Microsoft.Data.Analysis`](https://www.nuget.org/packages/Microsoft.Data.Analysis); [Deedle](https://fslab.org/Deedle/) | 🟡 **Use** (rougher) |
| **statsmodels** | econometric regression, time series, tests | Math.NET (basics); Accord.NET | 🟠 **Decide** — rich econometrics is a gap |
| **seaborn** | tidy statistical viz | ScottPlot / Plotly.NET (charts rebuilt) | 🟠 **Decide** — statistical presets missing |

**Legend.** ✅ a solid equivalent exists, use it as is. 🟡 an equivalent exists but
is less mature than Python; expect some glue. 🟠 the foundation exists but a whole
area is missing: a candidate for native code *if your usage justifies it*.

## What Lodestar writes natively

One area truly justifies native code — **text** — and one more turned out to be
a gap the .NET options do not fill honestly: the **evaluation metrics** every
sklearn user reaches for. That's
[`Lodestar.Text`](https://github.com/CyrilB1531/lodestar/blob/main/src/Lodestar.Text)
and its siblings, delivered as lots (see the brief):

1. **String distances & similarity** — Levenshtein, Damerau-Levenshtein,
   Jaro-Winkler, Jaccard, Ratcliff-Obershelp, phonetics… *(done)*
2. **Tokenization & sparse vectorization** — `CountVectorizer`, `TfidfVectorizer`
   (exact sklearn semantics), home-grown CSR matrix. *(done)*
3. **Embeddings & semantic search** — ONNX Runtime + sub-word tokenizers. *(done)*
   Native for one measured reason: [`Microsoft.ML.Tokenizers`](https://www.nuget.org/packages/Microsoft.ML.Tokenizers)
   builds every tokenizer from a vocabulary, a merges file or a `spiece.model`, and cannot
   read the `tokenizer.json` that Llama-2 and Mistral v0.1 actually ship. Its encode paths
   are faster than ours; the gap is the loader, not the arithmetic —
   [decision 0068](../decisions/0068-the-tokenizer-gap-is-the-loader-not-the-encode-kernel.md).
4. **Applied fuzzy matching** — `rapidfuzz.fuzz` / `process` equivalents. *(done)*
5. **Classification metrics** — sklearn-parity precision, recall, F1, confusion
   matrix, report and ROC-AUC. *(done)*

## Per-library guides

| Guide | Status |
| --- | --- |
| [NumPy → .NET](numpy.md) | draft |
| [pandas → .NET](pandas.md) | draft |
| [scikit-learn → .NET](sklearn.md) | draft |
| [statsmodels → .NET](statsmodels.md) | draft |
| [PyTorch → .NET](pytorch.md) | draft |
| [matplotlib → .NET](matplotlib.md) | draft |
| [seaborn → .NET](seaborn.md) | draft |

The **detailed equivalence table** (Python call → C# call, behavioral differences,
performance notes), filled in as we go, is in [`equivalence.md`](../equivalence.md).

> This document is not legal advice; third-party dependency licenses are recorded
> in [`THIRD-PARTY-NOTICES.md`](https://github.com/CyrilB1531/lodestar/blob/main/THIRD-PARTY-NOTICES.md).
