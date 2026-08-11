# Design — #68: stop Sonar reading the binary fixtures as text

**Date:** 2026-08-06 · **Issue:** #68 · **Branch:** `chore/68-exclude-oracle-fixtures-from-sonar` ·
**Checkout:** `<repo>`

## Problem

```text
WARN: Invalid character encountered in file tests/oracles/tiny_sp.model
at line 10 for encoding UTF-8. Please fix file content or configure the
encoding to be used using property 'sonar.sourceEncoding'.
```

Pre-existing, and unrelated to whichever pull request happens to surface it —
`tiny_sp.model` landed in `d9e5b7f`, `tiny_encoder.onnx` in `7bddc59`.

**Cause.** The scanner indexes every unexcluded file and decodes it with
`sonar.sourceEncoding`. Two fixtures are binary; `file(1)` reports both as `data`.

| File | First undecodable byte |
| --- | ---: |
| `tests/oracles/tiny_sp.model` | 54 |
| `tests/oracles/tiny_encoder.onnx` | 3 |

At the reported line the bytes are `03 e2 96 81 15 4f bf 3b c0 0a`. `e2 96 81` is
valid — it is `▁` (U+2581), the SentencePiece meta symbol. The `bf` after it is a
continuation byte with no lead byte.

The configuration set `sonar.coverage.exclusions` but **no `sonar.exclusions`**, so
nothing was excluded from *indexing*.

## Decisions

### D1 — Exclude `tests/oracles/**` from indexing and from test indexing

```text
/d:sonar.exclusions="tests/oracles/**"
/d:sonar.test.exclusions="tests/oracles/**"
```

Both, because the tree holds fixtures the scanner would otherwise classify either
way.

### D2 — Exclude the whole directory, not the two binaries

It is generated data. Beyond the two binary fixtures it holds several megabytes of
JSON corpora that are machine-written, reviewed as diffs and analysed by nothing.
Naming two files would leave the next fixture to reintroduce the warning.

### D3 — The reason goes in a comment beside the exclusion

In the style of the adjacent one. An exclusion with no reason is the kind of line
someone deletes while tidying, and the warning returns months later attached to an
unrelated change.

## Diagnosis worth keeping

The decoded bytes are in the record because "invalid UTF-8" is the sort of finding
that gets guessed at. It is not a corrupt fixture and not a wrong
`sourceEncoding`: it is a **binary file being read as text**, and the byte
sequence proves which.

## Out of scope

- `sonar.projectBaseDir`, which this change makes load-bearing and which becomes
  #70 immediately after.
- Any change to the fixtures.

## What "done" means

The warning gone from the next analysis; the exclusion carrying its reason; the
corpora no longer indexed.
