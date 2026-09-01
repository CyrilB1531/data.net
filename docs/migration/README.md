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
| **MAPIE** | conformal prediction: intervals and prediction sets with a coverage guarantee | none — no C# implementation exists | 🔴 **Write** — split conformal is **Lodestar.Conformal** |
| **pandas** | DataFrame, groupby, IO | [`Microsoft.Data.Analysis`](https://www.nuget.org/packages/Microsoft.Data.Analysis); [Deedle](https://fslab.org/Deedle/) | 🟡 **Use** (rougher) |
| **statsmodels** | econometric regression, time series, tests | Math.NET (basics) — *not* Accord.NET, see below | 🟠 **Decide** — rich econometrics is a gap |
| **seaborn** | tidy statistical viz | ScottPlot / Plotly.NET (charts rebuilt) | 🟠 **Decide** — statistical presets missing |

**Legend.** ✅ a solid equivalent exists, use it as is. 🟡 an equivalent exists but
is less mature than Python; expect some glue. 🟠 the foundation exists but a whole
area is missing: a candidate for native code *if your usage justifies it*.
🔴 **nothing exists** and this project wrote it.
⛔ **unmaintained** — do not reach for it, and the row says since when.

## Unmaintained, and why that is stated with dates

Pointing a reader at a library nobody has touched in years is worse than saying
nothing, so every ⛔ here carries **the last published package and the last commit,
both dated**. A verdict without them is a claim that rots quietly; with them, a
reader can check whether it is still true and this page can be corrected rather
than argued about.

| library | last package | last commit | reach for |
| --- | --- | --- | --- |
| **Accord.NET** | `Accord` 3.8.0, **19 October 2017** (3.8.2-alpha, November 2017, is the last of any kind) | **18 November 2020** | Math.NET Numerics for distributions and regression; [`Lodestar.Metrics`](https://github.com/CyrilB1531/lodestar/blob/main/src/Lodestar.Metrics) for evaluation |
| **Pandas.NET** | `Pandas.NET` 0.6.0, **6 November 2023** | **4 August 2024**, deleting its CI workflow | `Microsoft.Data.Analysis`, or Deedle for time series |

Accord.NET is the one that matters, because it is still the first result for half
of these searches: the package a reader would install predates .NET Core 3.0.

## When calling Python is still the right answer

This page recommends .NET libraries because the project's thesis is that most of
the ecosystem already exists there. It is not that Python is never the answer. A
model that only exists as a Python package, a notebook workflow with a person in
it, a one-off analysis — for those, calling Python is right, and a migration guide
that pretended otherwise would not be trusted on the cases where it is not.

| option | shape | last release |
| --- | --- | --- |
| [**CSnakes**](https://github.com/tonybaloney/CSnakes) | source-generates typed C# from Python type hints; embeds CPython in the process | `CSnakes.Runtime` 1.2.1, August 2025 |
| [**Python.NET**](https://github.com/pythonnet/pythonnet) | dynamic interop, the long-standing option | `pythonnet` 3.1.0, May 2026 |

Both were active within the last month at the time of writing. What they cost is
what `Lodestar` exists to avoid where it can: a Python runtime to deploy and
version alongside the application, no ahead-of-time compilation to a single
artifact, and the GIL between your threads and theirs.

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
6. **Split conformal prediction** — MAPIE-parity intervals and prediction sets, with
   the finite-sample coverage guarantee and the exchangeability assumption it rests
   on. *(done)* The survey behind
   [#441](https://github.com/CyrilB1531/lodestar/issues/441) found **no C#
   implementation at all**, which is why this one is written rather than delegated;
   the guarantee's assumption leads
   [its guide](https://github.com/CyrilB1531/lodestar/blob/main/docs/guides/conformal.md)
   rather than closing it.

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
