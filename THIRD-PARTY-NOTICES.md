# Third-party notices

This file records attributions for third-party components. Per §10.3 of the
project brief, every embedded dependency and data resource is listed here with
its license. Development-only tools (used to *generate* test data but never
shipped or linked into the distributed library) are recorded separately at the
bottom for traceability.

## Runtime / shipped dependencies

_None yet._ `TextSimilarity.Core` has no external runtime dependencies by design
(§3). ONNX Runtime will be added here when Lot 3 (`TextSimilarity.Embeddings`)
lands.

## Build / test / benchmark dependencies

| Component | License | Usage |
|---|---|---|
| xUnit | Apache-2.0 | Test framework (`tests/`) |
| BenchmarkDotNet | MIT | Micro-benchmarks (`bench/`) |

## Development-only oracle generation (not distributed)

These libraries are executed by `tools/generate_oracles.py` to produce the
reference JSON committed under `tests/oracles/`. They are **not** dependencies of
the shipped library, and are **not** transcribed — only their observable
input/output behavior is reproduced (permitted per §10.2). Running a program to
generate test data creates no license claim over the output.

| Component | License | Usage |
|---|---|---|
| rapidfuzz | MIT | Reference values for edit-distance / fuzzy metrics |
| jellyfish | MIT | Reference values for phonetic / Jaro-family metrics |
| textdistance | MIT | Reference values for set/token similarity metrics |
| scikit-learn | BSD-3-Clause | Reference values for vectorizers (Lot 2) |

> `python-Levenshtein` (GPL) is deliberately **not** used, as a matter of both
> transcription hygiene and generated-data hygiene. See
> `docs/decisions/0003-provenance-and-licensing.md`.
