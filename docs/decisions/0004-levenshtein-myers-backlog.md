# 0004 — Levenshtein: bit-parallel (Myers) optimization

**Status:** single-word shipped; multi-word backlogged · **Date:** 2026-08-01

## Context

The cross-language bench (`bench/`) shows that the initial implementation of
`Levenshtein.Distance` — a rolling-row DP `O(n·m)` — is markedly slower than
rapidfuzz on long strings (≈ 37× at 512 characters), while being faster on short
strings (no call overhead). rapidfuzz owes that advantage to the **bit-parallel
Myers algorithm** (1999), in `O(n·⌈m/w⌉)` with `w = 64` bits per machine word.

Since performance is the project's central argument, this algorithmic gap must be
closed for medium/long strings.

## Done

- **Single-word Myers shipped** (`src/DataNet.Text/Distances/Myers.cs`), wired as
  the fast path of `Distance` on the `char` path for a pattern of length 16–64 in
  Latin-1; falls back to the DP otherwise. Zero allocation (`Peq` table in
  `stackalloc`). Validated with no extra test code by the BMP oracle cases
  (`Distance_default_utf16_matches_rapidfuzz_for_bmp_cases`).

## To do

- **Multi-word (block) Myers** for patterns > 64: this is what's missing to catch
  up with rapidfuzz on long strings (the 128/512 buckets of the bench).
- Extend the fast path to the code-point mode (`Distance<int>`) and to `Indel`.

## Notes

- Myers manipulates per-alphabet-character bit masks; implementing it cleanly for
  an arbitrary Unicode alphabet needs a `char -> bitmask` table (dictionary or
  array depending on range). Start with the common ASCII/BMP path.
- Allowed inspiration source: Myers' published paper, "A Fast Bit-Vector Algorithm
  for Approximate String Matching Based on Dynamic Programming" (JACM 1999) —
  published pseudo-code, no transcription of copyleft source (cf.
  [`0003-provenance-and-licensing.md`](0003-provenance-and-licensing.md)).
