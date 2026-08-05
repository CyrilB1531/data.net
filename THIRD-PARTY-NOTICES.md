# Third-party notices

This file records attributions for third-party components. Per §10.3 of the
project brief, every shipped dependency is listed here with its license.
Build-time and development-only tools — used to compile or to *generate* test
data, but never linked into the distributed libraries — are recorded separately
for traceability.

## Runtime / shipped dependencies

`DataNet.Text` and `DataNet.Fuzzy` have **no** runtime dependencies on `net10.0`,
by design (§3).

| Component | License | Shipped by | Target |
| --- | --- | --- | --- |
| Microsoft.ML.OnnxRuntime | MIT | `DataNet.Embeddings` | both |
| System.Memory | MIT | all three packages | `netstandard2.0` only |
| System.Numerics.Vectors | MIT | all three packages | `netstandard2.0` only |

`System.Memory` and `System.Numerics.Vectors` supply `Span`, `Memory`,
`ArrayPool` and `Vector<T>`, which are in-box on `net10.0`. They appear only in
the `netstandard2.0` dependency group of each package.

ONNX Runtime is deliberately isolated to `DataNet.Embeddings`, so consumers of
the distance, vectorization and fuzzy-matching packages take no native
dependency.

## Build-time dependencies (not shipped)

| Component | License | Usage |
| --- | --- | --- |
| PolySharp | MIT | Compile-time polyfills for the `netstandard2.0` target. `PrivateAssets="all"`, so it emits source and adds no package dependency. |

## Test and benchmark dependencies (not shipped)

| Component | License | Usage |
| --- | --- | --- |
| xUnit | Apache-2.0 | Test framework (`tests/`) |
| coverlet.collector | MIT | Code coverage collection (`tests/`) |
| BenchmarkDotNet | MIT | Micro-benchmarks (`bench/`) |

## Development-only oracle generation (not distributed)

These libraries are executed by `tools/generate_oracles.py` to produce the
reference JSON committed under `tests/oracles/`. They are **not** dependencies of
the shipped libraries, and are **not** transcribed — only their observable
input/output behavior is reproduced (permitted per §10.2). Running a program to
generate test data creates no license claim over the output.

| Component | License | Usage |
| --- | --- | --- |
| rapidfuzz | MIT | Reference values for edit-distance / fuzzy metrics |
| jellyfish | MIT | Reference values for phonetic / Jaro-family metrics |
| textdistance | MIT | Reference values for set/token similarity metrics |
| scikit-learn | BSD-3-Clause | Reference values for the vectorizers |
| nltk | Apache-2.0 | Reference values for Porter and the six Snowball stemmers |
| tokenizers | Apache-2.0 | Reference values for WordPiece |
| sentencepiece | Apache-2.0 | Reference values for the unigram tokenizer |
| numpy | BSD-3-Clause | Reference values for pooling and kNN |

> `python-Levenshtein` (GPL) is deliberately **not** used, as a matter of both
> transcription hygiene and generated-data hygiene. See
> [`docs/decisions/0003-provenance-and-licensing.md`](docs/decisions/0003-provenance-and-licensing.md).
