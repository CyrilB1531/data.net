# 0140 — The median's partitioning branches on every element

**Issue:** [#140](https://github.com/CyrilB1531/data.net/issues/140) · **Date:** 2026-08-13 ·
**Branch:** `perf/140-branchless-partition` · **Follow-up to:** [#127](https://github.com/CyrilB1531/data.net/issues/127)

## Context

`MedianAbsoluteError` is the most expensive regression metric by a factor of three — 15.4 ms at
n = 1 000 000 against `mse`'s 5.0 — and this issue was opened believing that a sort was the reason and
that threads were the answer. Two spikes settled both questions, and neither the way the issue expected.

**Where the time goes.** `MedianAbsoluteError.Compute` instrumented per phase, n = 1 000 000, unweighted —
which is the shape the cross-language harness measures, since none of its operations passes a sample
weight:

| phase | time | share |
| --- | ---: | ---: |
| allocating the 8 MB column | 0.6 ms | 4% |
| filling it with \|y − ŷ\| | 2.6 ms | 16% |
| **`QuickSelect`** | **10.8 ms** | **65%** |
| validation, reduction, harness | ~2 ms | 15% |

Two hypotheses died there. `ArrayPool` would have bought 0.6 ms, not a third. And the fill is *cheaper*
than `mse`'s loop, because `mse` adds a compensated sum per element where this only subtracts and takes an
absolute value.

**And the parallelism this issue first proposed is not the lever.** `Outputs.WeightedMean` patched to
partition over fixed ranges, one `CompensatedSum` each, merged in index order, measured interleaved with
`median_ae` as a control: **1.56× at four workers, 1.65× at eight**. That is a ceiling taken with pinned
pointers and no copy — and `ReadOnlySpan<double>` cannot be captured in a lambda, so a shipped version
needs either `unsafe`, which this repository sets to `false` explicitly in `DataNet.Text`, or the 16 MB
copy `MultiClassRoc.CopyForWorkers` uses, which costs more than the 2 ms the parallelism saves. Recorded on
the issue as measured and not taken.

## The claim this lot tests

`QuickSelect` spends 10.8 ms on a million doubles, about **5 ns per element touched**, on sequential
access. That is not bandwidth. `Partition` is a Lomuto scheme whose inner loop is

```csharp
for (int i = from; i < to; i++)
{
    if (values[i] < pivot)
    {
        Swap(values, i, storeIndex);
        storeIndex++;
    }
}
```

— one comparison against the pivot per element, taken about half the time on random data, which is the
worst case for a branch predictor. At 1 000 000 elements, half a million mispredictions at roughly fifteen
cycles is about 2.4 ms on this machine, and the partition runs over shrinking ranges several times.

## Decisions

### D1 — the diagnosis is measured before the fix is written, and the obvious experiment is wrong

Comparing `QuickSelect` on random data against sorted data is the natural test and it is **confounded**:
on sorted input, median-of-three lands on the true median, so a single partition pass suffices. The time
would collapse because the number of passes collapsed, and the branch would take credit it had not earned.

So the experiment counts **element touches** and compares **nanoseconds per touch**, not total time. If
the per-touch cost falls sharply on predictable data, the branch is the cost. If it does not, the
hypothesis is wrong, this lot stops, and the issue closes on that finding rather than on a change nobody
needed.

### D2 — branchless Lomuto, and nothing else moves

If D1 holds, `Partition`'s inner loop becomes:

```csharp
for (int i = from; i < to; i++)
{
    double value = values[i];
    values[i] = values[storeIndex];
    values[storeIndex] = value;
    storeIndex += value < pivot ? 1 : 0;
}
```

The swap is unconditional and the index advances by the comparison. It is correct for the reason the
branchy version is: when `value` does not belong left, `storeIndex` points at the first element that is
also not less than the pivot, so the two are interchangeable and the swap is harmless. `storeIndex += … ? 1
: 0` compiles to a `setcc`, with no branch.

Nothing around it changes: the introselect budget, the median-of-three pivot, the `Array.Sort` fallback
below the insertion cutoff and on budget exhaustion, and the weighted path — which sorts through
`Array.Sort` and is the framework's code, not this repository's.

No `#if`. This is ordinary arithmetic and both target frameworks get the same source, which keeps
`DataNet.Metrics`'s two targets bit-identical where #127 already had to write down that they are not for
the vectorized paths.

### D3 — the bar, written before the number

**20% on `median_ae` at n = 1 000 000, or the change is reverted.** That is about 30% on the phase that
carries 65% of the metric, and it is an order of magnitude above the 2.4% the control moved across four
campaigns in the spike that motivated this.

The cost being bought is readability: an unconditional swap that looks wrong until the invariant is
explained. A change that buys less than that is not worth the reader's second glance, and this repository
has spent the day proving that a plausible optimisation can be a regression — #127 measured its branchless
2Sum lever slower and reverted it.

### D4 — the evidence is that not one byte moves

`QuickSelect` selects the same element whichever way it partitions, so **every regression corpus must be
unchanged and every test must pass untouched**. That is a stronger check than a new test: the median of the
existing fixtures is already pinned against scikit-learn, and a partition that no longer partitions would
move it.

What a new test must add is the property the current suite does not state — that selection is correct on
the shapes a partition scheme gets wrong: all elements equal, already sorted, reverse sorted, two distinct
values, and an organ-pipe sequence. Those are the inputs where an off-by-one in the index arithmetic
survives random testing.

## Documentation

- `docs/guides/performance.md` — the before and after, with the phase decomposition above, because it is
  what explains why this lever and not another. Same protocol as #127: interleaved campaigns in one window,
  `uptime` at both ends, and an operation nobody touched as a control.
- No ADR. This diverges from nothing; it is the same algorithm with the same output, arranged so a
  predictor cannot lose on it.

## Out of scope

The parallelism this issue originally proposed, recorded on the issue with its measurement. The weighted
median's `Array.Sort`. `ArrayPool` for the column, measured at 0.6 ms and not worth its complexity. And
anything about `mse`/`mae`, which are already at or ahead of numpy after #127.

## Risks

- **The diagnosis may not hold.** D1 is written so that a negative result ends the lot cleanly. The risk is
  answering it with the confounded experiment instead, which is why the confound is named here rather than
  discovered later.
- **The branchless form is subtler.** Its correctness rests on an invariant that the code must state in a
  comment a reader can check, not assert.
- **The measurement window is noisy on this machine** — a desktop session runs throughout. The control and
  the interleaving are the answer, and a round whose control moves more than a few percent is void.
