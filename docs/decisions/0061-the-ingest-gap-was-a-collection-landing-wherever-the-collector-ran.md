# 0061 — The ingest gap was a collection, landing wherever the collector ran

**Status:** accepted · **Date:** 2026-08-30 · **Amends:** [`0058`](0058-the-npy-ingest-is-memcpy-bound-and-the-allocation-is-not-the-cost.md)

## Context

[0058](0058-the-npy-ingest-is-memcpy-bound-and-the-allocation-is-not-the-cost.md) answered
[#480](https://github.com/CyrilB1531/lodestar/issues/480) and left one thing open:

> `ingest_total` measures 2.17–2.26 ms where its own parts sum to 0.97–1.00 and the canonical
> harness measures the same chain at 1.109–1.134.

It named **position within the round** as the likeliest candidate — `ingest_total` is always first,
so it absorbs whatever the previous round left the collector — labelled it a candidate, and said a
phase-reordering run is what would settle it. This is that run.

## What was measured

`ingest-phases` on `3d4fa2d`, a hosted runner, .NET 10.0.11, 4 cores, workstation GC, three rounds
of nine interleaved runs, load average 3.46 / 3.46 / 3.08. `ingest_total_last` calls the same
`Ingest` method as `ingest_total`, last in the round instead of first, so a difference between them
is position and nothing else.

| | round 1 | round 2 | round 3 |
| --- | ---: | ---: | ---: |
| `ingest_total`, first | 0.936 ms | 0.949 ms | 0.910 ms |
| its collections, gen0/1/2 | 0 / 0 / 0 | **3 / 3 / 3** | 0 / 0 / 0 |
| `ingest_total_last`, last | 1.308 ms | 0.943 ms | 1.255 ms |
| its collections, gen0/1/2 | **3 / 3 / 3** | 0 / 0 / 0 | **2 / 2 / 2** |

Canonical harness, same runs: 1.163 / 1.136 / 1.154 ms wall.

## Decision

**The gap is a garbage collection, and it lands on whichever phase the collector happens to run in.
It is not position.** 0058's candidate is refused by its own test.

Round 2 is what settles it: the two rows are **0.949 against 0.943** — indistinguishable — and it is
the **first** that carries the three collections. Rounds 1 and 3 have the last one slower by
0.35–0.37 ms, and there the collections are on the last. **The cost follows the collections, not
the rank.** A position explanation would have put the same row ahead in all three rounds.

**And the row was never stable in the first place.** Adding a second ingest to the round moved
`ingest_total` from 0058's **2.17–2.26 ms** to **0.910–0.949**, without touching the ingest. With
one ingest per round, that single row absorbed every collection the round's 15.36 MB allocations
provoked; with two, the debt is split and neither carries all of it.

**So 0058's unexplained gap is explained, and the number that raised it was an artefact of the
table's own shape.** At 0.910–0.949 ms the first-position ingest now agrees with both things 0058
said it disagreed with: the sum of its parts (0.97–1.00) and the canonical harness (1.136–1.163).

Nothing about 0058's substance changes. The ingest is still `memcpy`-bound, the reader's allocation
still costs about 0.02 ms, and adopting is still worth exactly one copy. **What changes is that the
one row which did not fit that account now does.**

## What was refused

**Shuffling the phases.** Every ratio in this table rests on the phases being interleaved in a
fixed order, so reordering per round would have made each round incomparable to the last in order
to answer a question about one row. Duplicating one row at the other end of the same round is the
same experiment with one variable instead of two.

**Reading round 1 or round 3 alone.** Either would have confirmed position handsomely — 1.308
against 0.936, 1.255 against 0.910 — and been wrong. Round 2 is the round that decides, and it
decides against the hypothesis the run was built to test.

**Chasing the collection out of the diagnostic.** A `GC.Collect` between phases, or a larger
allocation budget, would flatten the table and hide the thing this lot just learned: on a 15.36 MB
block the collector's timing is worth 0.35 ms to whichever phase it lands in, which is a third of
the ingest.

## Consequences

- **#480 is closed.** Both halves are answered: 0058 for where the time goes, this for the row that
  did not add up.
- **A single-call median on a 15 MB allocation is not a stable measurement here**, and the phase
  table now shows why rather than only its result. Any future phase in this mode that allocates a
  block of this size should expect its median to carry a collection it did not cause.
- **The canonical harness was right all along.** Best-of-five over scaled iterations read
  1.109–1.163 ms across both runs while the single-call median moved from 2.2 to 0.9 — which is an
  argument for what the published rows already use.
- **What would change this decision** is a run where the collections and the slower row come apart:
  a round with the collections on the first and the last still slower, or the reverse. Three rounds
  is what this rests on, and two of them agree by putting both together.
