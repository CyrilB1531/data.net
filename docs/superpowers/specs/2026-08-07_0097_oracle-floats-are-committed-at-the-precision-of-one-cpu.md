# Design — #97: commit the metric, not the machine that computed it

**Date:** 2026-08-07 · **Issue:** #97 · **Branch:** `fix/97-round-oracle-floats` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

`tools/generate_oracles.py` writes twelve float values as bare `float(...)`, at
full float64 repr. The last bits of a reduction depend on the BLAS kernel the
runner happened to have, so the committed corpora record **the machine as well as
the metric** — and the `Oracles are reproducible` job fails on a difference that
means nothing:

```text
-   "accuracy_count": 413.626,
+   "accuracy_count": 413.6259999999999,
```

That gate failed three times in one morning for this reason, which is what #95 made
visible.

## Decisions

### D1 — One `stable()` helper, twelve significant digits

Every float written by the generator goes through it.

### D2 — Significant digits, not decimals

**The spread is always at the last bit, so it scales with the value:** ~1e-13 on
`accuracy_count` near 413, ~1e-16 on knn scores near 0.4.

That is the same sixteenth digit in both, and **only a significant-digit rule
catches both with one threshold**. A fixed number of decimals would over-round one
and under-round the other.

### D3 — Twelve, with the margin computed rather than chosen

Twelve leaves **four orders of magnitude above the observed spread**, and costs at
most 5e-13 against the tolerances already in the tests: `MetricsCorpus.Tolerance`
is `1e-9`, `EmbeddingIndexTests.Tolerance` is `1e-4f`.

So the rounding cannot move any assertion, and the reason is arithmetic rather
than taste.

### D4 — Include `roc_auc.json`, which has not drifted yet

It is the same scikit-learn reduction written the same way. **Leaving it out would
be choosing the date of the next red rather than avoiding it.**

### D5 — The diff is large in line count and empty in meaning

Three corpora move — `classification_metrics.json`, `knn.json`, `roc_auc.json` —
and every changed line drops digits that were never a property of the metric. Say
so in the pull request, because a reviewer seeing thousands of changed lines in an
oracle corpus is right to be alarmed by default.

### D6 — The acceptance criterion is stability across kernels, not one green run

Generate under three `OPENBLAS_CORETYPE` values and require byte-identical output.
A single green run proves only that the current runner agrees with itself.

## Out of scope

- Any change to what the metrics compute.
- The reporting improvements from #95, which made this diagnosable.

## What "done" means

All twelve values through `stable()`; the three corpora regenerated;
byte-identical output under three kernels; the tolerance arithmetic recorded.
