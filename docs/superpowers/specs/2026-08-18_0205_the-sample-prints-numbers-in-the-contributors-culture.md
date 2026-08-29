# 0205 — The sample prints 85 numbers in the contributor's culture

**Issue:** [#0205](https://github.com/CyrilB1531/lodestar/issues/0205) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

`samples/` printed 85 numbers with the ambient culture's separator, so the same sample read differently in France and in the United States. `CA1305` could not reach one of them, which is why an analyser rule was not the answer.

## What was decided

A guard rather than a rule: `tools/check_sample_culture.py` refuses a tracked sample source that formats a number without an invariant culture. It is offline and instant, which is what lets it run in a pre-commit hook as well as in CI.

## What shipped

The guard, the 85 call sites converted, and `Inv` — a tiny helper in `samples/` so a sample formats a number in one short call rather than repeating `CultureInfo.InvariantCulture` at every use.
