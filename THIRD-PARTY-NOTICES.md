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

## Redistributed resources (shipped inside the assemblies)

| Component | License | Shipped by | Source |
| --- | --- | --- | --- |
| Snowball stop-word lists (fr, de, it, pt, es) | BSD-3-Clause | `DataNet.Text` | `https://snowballstem.org/algorithms/<language>/stop.txt` |

These are data, not code: the five lists are compiled into
`DataNet.Text.Vectorization.StopWords` by `tools/fetch_stopwords.py`, which pins a
SHA-256 per file. Unlike the libraries below, they *are* redistributed, so the
licence travels with them. The English list is scikit-learn's (BSD-3-Clause), not
Snowball's; the nltk stop-word corpus is deliberately not used — see
[`docs/decisions/0010-stop-word-list-provenance.md`](docs/decisions/0010-stop-word-list-provenance.md).

```
Copyright (c) 2001, Dr Martin Porter,
Copyright (c) 2002, Richard Boulton.
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice, this
   list of conditions and the following disclaimer in the documentation and/or
   other materials provided with the distribution.
3. Neither the name of the copyright holder nor the names of its contributors may
   be used to endorse or promote products derived from this software without
   specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS
OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN
IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```

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

> The Apache-2.0 above covers the `nltk` *code* we execute. It does not extend to
> the `nltk_data` corpora, which are licensed individually — the `stopwords`
> corpus among them has no stated licence, which is why the shipped lists come
> from Snowball instead. See
> [`docs/decisions/0010-stop-word-list-provenance.md`](docs/decisions/0010-stop-word-list-provenance.md).

> `python-Levenshtein` (GPL) is deliberately **not** used, as a matter of both
> transcription hygiene and generated-data hygiene. See
> [`docs/decisions/0003-provenance-and-licensing.md`](docs/decisions/0003-provenance-and-licensing.md).
