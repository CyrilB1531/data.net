# 0048 — The bit-parallel gate depends on the kernel *and* the alphabet

**Status:** accepted · **Date:** 2026-08-21 · **Amends:** [`0047`](0047-one-gate-per-kernel-not-one-per-alphabet.md)

Reverses 0047's central finding on evidence 0047 could not reach. That record's
reasoning about *shape* stands and is not restated here; what it concluded about the
alphabet does not.

## Context

Decision 0047 read a sweep over the committed corpus and concluded that no alphabet
wants a different gate: every curve rose with the gate and the wide regime was the less
sensitive one. It said so from bucket 32, because that was the only bucket whose
patterns straddled any candidate value — and its median trimmed pattern is 16, with 70%
of its pairs at or above 12.

**That is above the range where the curves separate.** Issue #409 gave the corpus twenty
banded buckets, whose pattern after `Affixes.Trim` is exactly the band named, spanning 2
to 16 in both alphabets. With them the question stops needing a sweep at all: at a gate
of 2 every band takes the kernel and at 17 every band takes the dynamic program, so one
pair of runs prices both routes over the same pairs and the crossing is where the ratio
reaches 1.

## What the measurement says

Four crossings, not one, and both passes agree on each. Numbers in
[`../guides/performance.md`](../guides/performance.md).

| | Latin | CJK | shipped gate |
| --- | ---: | ---: | ---: |
| `Lcs` (`fuzz.ratio`'s path) | ≤ 2 | 6 | 8 |
| `Myers` | 5 | 10 | 8 |

**Two dimensions, independent and both real.** The LCS kernel crosses about three bands
before Myers, its recurrence being four operations per text character against Myers'
dozen — the same asymmetry 0043 measured when only one of the two kernels was worth
holding a table for. And CJK crosses about four bands after Latin in both, the side
table raising the kernel's floor while leaving the dynamic program's cost untouched.

## Decision

**One shared constant cannot serve those four crossings, and 8 serves none of them
well.** It is three bands too high for Myers on Latin, at least six too high for LCS on
Latin, two too high for LCS on CJK, and two too *low* for Myers on CJK.

The largest error is on the hottest path in the repository: `Lcs.SubsequenceLengthChars`
is what `fuzz.ratio`, `process.extract` and blocking deduplication run, and at band 8 its
kernel is already 2.6× cheaper than the dynamic program the gate sends those pairs to
(91.6 ns against 236.2). Bands 2 to 7 are refused the kernel while it is 24% to 56%
cheaper there.

**What replaces the shared constant is deliberately not decided here.** Four constants,
or a gate reading the pattern's width — which the dispatch can know, since building the
equality table walks the pattern anyway — are both open, and choosing between them is a
change to the hot path that owes its own before/after over the scattered buckets. This
record settles only that the shared 8 is wrong and why.

## Consequences

- **0047's error is instructive rather than embarrassing, and is recorded as such.** It
  measured honestly, in a place where the effect it was looking for does not appear.
  A conclusion drawn from the only bucket available is bounded by that bucket, and
  saying so in the record is what let #409 overturn it in a day rather than a year.
- **The corpus is now able to answer a question it could not**, and #208's standing
  refusal — "stays at 8 until a corpus that can answer it exists" — is discharged. 0043
  called the value "likely conservative" and asked for a sweep below 8 rather than a
  guess; this is that sweep.
- **The ratio is taken across two processes, not within one.** Unlike the gate
  benchmarks, whose DP baseline runs in the same process, the kernel and DP readings
  here come from separate builds, so machine drift enters the ratio — about 10% between
  the two passes. The separation between alphabets is four bands, an order above that,
  but a boundary band is not pinned by this measurement.
