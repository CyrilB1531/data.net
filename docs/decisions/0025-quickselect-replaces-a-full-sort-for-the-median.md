# 0025 — Quickselect, with an introselect fallback and a branchless partition, replaces a full sort for the unweighted median

**Status:** accepted · **Date:** 2026-08-14

## Context

[Issue #92](https://github.com/CyrilB1531/data.net/issues/92)'s own
performance gate found `MedianAbsoluteError` sorting the whole residual array
where NumPy's `median` selects with introselect in expected `O(n)`, at
0.19×–0.36× of scikit-learn's processor time — the only rows on that gate
below 1×. [Issue #140](https://github.com/CyrilB1531/data.net/issues/140) then
profiled the quickselect that replaced the sort and found 68% of the time at
n = 1 000 000 in `QuickSelect`'s inner loop, traced it to branch
misprediction in the Lomuto partition, and removed the branch. Both
measurements are in `docs/guides/performance.md`: "Regression metrics — mse,
mae, median_ae, r2 (issue #92)" for the first, and "Branchless partitioning
(issue #140)" for the second — the latter is where the 4.39 ns / 2.15 ns pair
and the disassembly below come from; neither is re-derived here.

## Decision

`WeightedPercentile.MedianUnweighted` selects, rather than sorts, the one or
two order statistics `Average` needs:

- **Median-of-three pivoting** (`Partition`) — ordering the first, middle and
  last element of a range so the middle value becomes the pivot — defeats
  already-sorted and reverse-sorted input, which drive a plain first-or-last
  pivot to its `O(n²)` worst case.
- **An introselect budget** (`QuickSelect`) bounds what median-of-three
  cannot: an organ-pipe sequence or many repeats of one value still degrade it
  to `O(n²)`. Past `2·log2(width) + 4` partitioning passes on the current
  range, the range is sorted outright instead — the same guarantee NumPy's own
  introselect-based `median` relies on, turning the worst case into
  `O(n log n)`.
- **An unconditional swap, conditional advance**, inside `Partition`'s inner
  loop: every iteration swaps `values[i]` and `values[storeIndex]`, then
  advances `storeIndex` by `value < pivot ? 1 : 0`, where `value` is read
  *before* the swap. The natural-looking alternative — swap only when
  `value < pivot` — is correct too (when the element does not belong left, the
  store index already points at another element that also does not, so the
  swap is harmless either way) but carries a data-dependent branch that issue
  #140 measured at 4.39 ns per element touched on random residuals against
  2.15 ns on already-sorted ones, isolating misprediction from pass count by
  counting inner-loop iterations rather than timing wall clock. `DOTNET_JitDisasm`
  on a verbatim copy of the loop confirmed RyuJIT on x64 compiles the
  branchless form to `seta`/`movzx`/`add`, no branch; unverified on the other
  runtimes the `netstandard2.0` assembly reaches, where only the gain and not
  the correctness depends on it.

Below a width of 12 elements (`QuickSelect.InsertionCutoff`), the range is
sorted outright rather than selected: a full sort is cheap at that size and
sidesteps the edge cases a three-point pivot has on ranges that barely hold
three positions.

## Consequences

- `Partition`, `QuickSelect` and `MedianUnweighted` each carry a one-line
  pointer to this record at their point of departure from the obvious
  implementation, instead of restating the reasoning inline.
- The processor-time ratios and the branch-misprediction measurement stay in
  `docs/guides/performance.md`, which is where they are kept current; this
  record points at that document rather than repeating numbers that a later
  change could make stale in two places at once.
- `WeightedPercentileMedianTests.cs`'s shape-targeted cases (all equal,
  already sorted, reverse sorted, two distinct values, organ pipe) prove the
  selection is correct on the inputs that defeat median-of-three; they prove
  nothing about the branch-prediction argument, which no committed test
  re-measures.
