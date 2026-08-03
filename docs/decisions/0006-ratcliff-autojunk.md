# 0006 — Ratcliff-Obershelp: difflib's autojunk heuristic

**Status:** accepted · **Date:** 2026-08-01

## Context

`difflib.SequenceMatcher` applies an **autojunk** filter by default: in a sequence
of more than 200 elements, any element appearing in more than 1% of positions is
treated as "junk" and ignored when finding matching blocks. This can change
`ratio()` on long strings.

## Decision

- **Implement true Ratcliff-Obershelp**, without autojunk: `RatcliffObershelp`
  computes `2·M/T` over the recursive pairing of the longest common sub-block,
  discarding no element.
- **Generate the oracle with `autojunk=False`**, so it is in exact parity with our
  implementation at all lengths.

## Consequences

- For any input ≤ 200 elements, `RatcliffObershelp.Similarity` is identical to
  `difflib` by default (autojunk doesn't trigger).
- Beyond 200 elements, DataNet may differ from `difflib` **by default** (but
  coincides with `difflib(autojunk=False)`). This is a deliberate choice: autojunk
  is a heuristic optimization of difflib, not a property of the Ratcliff-Obershelp
  metric. Documented divergence per §5 of the brief.
