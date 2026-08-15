# 0005 — Accepted divergence from jellyfish on combining marks (Hamming, Jaro)

**Status:** accepted · **Date:** 2026-08-01

## Context

The brief maps Hamming to `jellyfish.hamming_distance` and Jaro/Jaro-Winkler to
`jellyfish.jaro*`. While validating, we found that jellyfish 1.2.1 **diverges from
the standard definition on ≈ 5% of the corpus** (Hamming: 62/1241), always on
degenerate strings containing combining marks / emoji / mixed scripts. The same
phenomenon affects `jaro_similarity` and `jaro_winkler_similarity` on those same
inputs (the exact count is recorded in each oracle's `jellyfish_divergences`
metadata).

Investigation:

- It is **not** a byte-level (UTF-8) comparison: `hamming('é','e')` is 1 in
  jellyfish, 2 in bytes.
- It is **not** NFC normalization: normalizing before comparison doesn't change
  the agreement rate (1179/1241 either way).
- For all "normal" inputs (ASCII, simple accents, equal-length codes — the real
  use of Hamming), jellyfish **coincides** with the standard definition.

The exact cause (behavior of jellyfish's Rust core on combining marks) was not
pinned down, and reproducing it would mean copying an unspecified quirk.

## Decision

- **Implement the standard definition** (Hamming: differing positions + length
  difference; Jaro/Jaro-Winkler: the classic algorithm with a 0.7 boost threshold
  for Winkler), over code points or UTF-16 units per `TextElement`.
- **Generate each oracle from an explicit reference** of that definition
  (`_hamming_reference`, `_jaro_reference`, `_jaro_winkler_reference` in
  `tools/generate_oracles.py`), not from jellyfish's output. The generator
  **counts and records** the number of jellyfish divergences
  (`jellyfish_divergences`) for traceability.
- **Anchor jellyfish parity on sane inputs** via hand-written test cases
  (`[InlineData]`) whose values are exactly jellyfish's (real names:
  MARTHA/MARHTA, DWAYNE/DUANE, DIXON/DICKSONX…).

## Consequences

- [`Hamming.Distance`](../reference/text/distances/hamming-distance.md) (and Jaro/Jaro-Winkler) are correct per the definition and
  coincide with jellyfish everywhere jellyfish computes a standard result.
- The divergence is explicit, measured and versioned, in line with §5 of the brief
  ("either replicate, or document the divergence").
