# Design — #95: print the oracle drift, not just its shape

**Date:** 2026-08-07 · **Issue:** #95 · **Branch:** `fix/95-print-oracle-drift` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

The `Oracles are reproducible` job prints `git diff --stat` and stops. A failure
therefore says **which corpus moved and how many lines**, and never **which
values**.

That gate failed three times in one morning — twice on #94, once on `main`
(`0db78d1`) — always with the same three-line summary, and there was no way to
tell from the log whether the cause was the same each time.

A gate that cannot be diagnosed from its own output is a gate that gets re-run
until it passes.

## Decisions

### D1 — Print the diff itself, not only its shape

```text
::error::Committed oracle corpora differ from a fresh generation. …
 tests/oracles/classification_metrics.json |  6 +++---
 tests/oracles/knn.json                    | 24 ++++++++++++------------
--- first 400 changed lines ---
@@ -7264 +7264 @@
-   "accuracy_count": 413.626,
+   "accuracy_count": 413.6259999999999,
```

The stat names the corpus that moved; **only the values say why**. In this example
they say it immediately: a last-digit float difference, not a behavioural change.

### D2 — `-U0`

The corpora are **one value per line**. Context lines carry nothing here, and the
changed values are the whole message.

### D3 — Capped at 400 lines

So a wholesale regeneration cannot bury the log. That case is covered by D4
instead.

### D4 — Keep the regenerated corpora as an artefact on failure

Retention 14 days.

The runner is thrown away with everything that would let the failure be
reproduced. Drift has already turned out to depend on **which CPU the job landed
on**, which no amount of log-reading settles — so the comparison has to be
possible off the runner.

This is what turns a legible failure into a *reproducible* one, and it is the
half that matters when the cause is environmental.

### D5 — The gate accepts exactly what it accepted before

This branch changes the **reporting**, never the criterion. A diagnostics change
that also relaxes a gate is two changes, and the second one would be invisible.

## Out of scope

- Fixing the drift itself. The values printed here are what makes #97 possible;
  they are not the same issue.
- Any change to the generator.

## What "done" means

A failing run prints the changed values and uploads the regenerated corpora; the
acceptance criterion unchanged; demonstrated on a deliberately drifted corpus
rather than assumed.
