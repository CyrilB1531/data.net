# 0003 — Code provenance and license

**Status:** accepted · **Date:** 2026-08-01

## Context

The project will be published on GitHub — a distribution under the meaning of free
licenses. The obligations apply fully from the first commit (§10).

## Decision

- **Project license: Apache-2.0.** As permissive as MIT, plus an explicit patent
  grant and a contribution clause — the usual default for a library aimed at
  enterprise adoption. `LICENSE` file present from initialization;
  `PackageLicenseExpression=Apache-2.0` in the metadata.
- **Provenance rule: no transcription of copyleft code.** Translating from one
  language to another is a derivative work. `python-Levenshtein` (GPL) is
  therefore **excluded** — neither transcribed nor even used to generate test data
  (for hygiene, though the GPL claims nothing over a program's outputs).
- **Allowed sources**, in order of preference: published papers/pseudo-code
  (algorithms are not protectable); textbooks and documentation; permissively
  licensed implementations as a *behavior reference* only — rapidfuzz (MIT),
  jellyfish (MIT), textdistance (MIT), scikit-learn (BSD-3). We reproduce
  inputs/outputs and analogous naming, never the source.
- **Test-data generation.** rapidfuzz/jellyfish are run by
  `tools/generate_oracles.py` to produce the oracle JSON. They are **development**
  dependencies, never runtime ones; they are not redistributed.

## Consequences

- Each implementation whose inspiration source is worth tracing gets a note in
  this `decisions/` folder.
- `NOTICE` and `THIRD-PARTY-NOTICES.md` record attributions; they are updated at
  the same time as any third-party dependency or resource is added.
- **ONNX model weights** (Lot 3) are not redistributed in the repository:
  downloaded at runtime + license documented case by case.
