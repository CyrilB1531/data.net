# 0018 — Multiclass ROC-AUC parallelism is opt-in, and the caller names the worker count

**Status:** accepted · **Date:** 2026-08-10

## Context

[Issue #86](https://github.com/CyrilB1531/data.net/issues/86) asks for multiclass
ROC-AUC to use more than one core. The shape of the computation invites it:
one-vs-rest scores `k` independent binary problems, one-vs-one scores `k(k-1)/2`
independent pairs, and nothing in either loop reads what another iteration wrote.
Issue #61's own gate measured the cost — `roc_auc_ovr_macro` at n=100 000, k=10
was the narrowest row in the whole scikit-learn comparison at 2.74× on processor
time, and it is sort-bound rather than overhead-bound.

"Use more cores" is not one decision, though. It is a decision about who chooses,
a decision about when it engages, and a decision about what may not change while
it does. Each of those is hard to reverse: the first is public API, the second is
behaviour callers will come to depend on, and the third is a number a downstream
test asserts on. The measurement behind the figures quoted below is in
[`../guides/performance.md`](../guides/performance.md).

## Decision

### Parallelism is opt-in, and the default stays sequential

`MultiClassRocOptions.MaxDegreeOfParallelism` defaults to 0, and 0 and 1 both
mean sequential. The sequential drivers still read the caller's spans directly and
take no private copy of the inputs for workers to share, so a caller who upgrades
and changes no code gets the same one core and the same result as before — the
same bits, which `tests/DataNet.Metrics.Tests/RocAucFrozenBitsTests.cs` now pins
absolutely rather than against a second code path.

What that caller does *not* get is the same allocations. The sequential drivers
were rewritten on this branch, not left alone: the per-curve buffers moved into a
pooled `BinaryRoc.Scratch` that every class and every pair reuses. The direction
is favourable on the multiclass path and mildly unfavourable on the binary one,
and the Consequences below give both with numbers.

The alternative worth taking seriously is not "always parallel" but "parallel by
default, with an opt-out". It was rejected because of where this library runs. A
metrics call is most often made inside a request handler on a server already
running one request per core; a library that quietly claims all eight logical
threads for one scoring call in that setting does not make the process faster,
it makes the other seven requests wait, and it does so without any line of the
caller's code saying so. The cost of guessing wrong is asymmetric: a caller who
wanted threads and did not get them reads one property name in the
documentation, while a caller who got threads they did not want debugs a latency
regression under load with no local evidence of the cause.

scikit-learn's own answer is the same one. `roc_auc_score` has no `n_jobs`
parameter at all — the caller who wants their cores used reaches for
`joblib.Parallel` around it — so this default also keeps the parity story
straight: `default(MultiClassRocOptions)` reproduces scikit-learn's defaults in
every field, including this one.

A reader who disagrees is disagreeing with the claim about where the calls come
from. If the dominant caller were a batch script on an otherwise idle machine,
"parallel by default" would be the better default and this record would be
wrong.

### No `-1` sentinel for "all cores"

`Parallel.For` spells "as many as you like" as `MaxDegreeOfParallelism = -1`, and
mirroring that on the options struct would have been the smaller diff.
`ArgumentOutOfRangeException` is thrown for anything negative instead, and a
caller who wants every core writes `Environment.ProcessorCount` at the call
site.

The reason is that a sentinel hides the number where it is chosen. `-1` in a
config file, or in an options object built three layers from the call, tells a
reader nothing about how many threads will run; `MaxDegreeOfParallelism =
Environment.ProcessorCount` tells them exactly, in the one place where somebody
could reasonably change it.

The measurement has since supplied a second reason the decision did not have
when it was taken: **`ProcessorCount` is not always the right number.** On the
4-physical-core, 8-logical-thread workstation the guide's table was taken on,
`dop=8` is *slower* than `dop=4` on three of the six shapes, in both passes —
one-vs-rest at n=1000, and one-vs-one at n=1000 and at n=100 000, all with
k=10. `Environment.ProcessorCount` returns 8 there. Had `-1` been the idiom, the
best setting for half these shapes would have had no way to be written at all
short of the caller computing it, and the obvious spelling would have been the
losing one.

### The setting is honoured at any input size, with no internal threshold

There is no "below n samples, ignore the request" rule. Whatever number the
caller writes is what runs, at n=1000 and at n=1 000 000 alike.

A threshold is the conventional move here, and it is tempting because it looks
protective: dispatch and the input copy cost something, so on a small enough
input the opt-in should lose. The problem is that the crossover point is a
property of one machine — core count, memory bandwidth, thread-pool state — and
a constant compiled into this library would be calibrated on the one workstation
that happened to measure it. A caller on a 64-core server would then be silently
overruled by a number chosen for a 2013 desktop, and would have no way to see
it, because a threshold that silently downgrades a request cannot be observed
from the call site.

What replaces the threshold is publication. The n=1000, k=10 row is in the
performance guide, and it does not say what the design brief expected it to say:
**the opt-in is a gain there, not a cost.** One-vs-rest goes from 0.466 / 0.470
ms sequential to 0.240 / 0.238 ms at `dop=4` — 1.94× / 1.97× — and one-vs-one
from 0.745 / 0.741 ms to 0.297 / 0.279 ms, 2.51× / 2.66×. Ten classes are ten
independent sorts even when each is short, and the copy at that size is 1000 ×
10 doubles. The prediction that small inputs would need defending was wrong, and
a threshold written to defend them would have removed a doubling.

### The arguments moved into `MultiClassRocOptions`, a `ref struct`

[`RocAuc.MultiClass`](../reference/metrics/classification.md#rocaucmulticlass)
used to take strategy, averaging, labels and sample weights as trailing
parameters. `MaxDegreeOfParallelism` would have been a fifth, and an
options value carries the whole set instead.

It is a `ref struct` and could not have been anything else: `Labels` is a
`ReadOnlySpan<int>` and `SampleWeight` a `ReadOnlySpan<double>`, and no ordinary
class or struct can hold a span as a field. A `class` would have forced both onto
arrays, imposing an allocation on every caller — including the ones who pass
neither — to add a settings object. The price is that the type cannot be stored
in a field, captured by a lambda, or held across an `await`; it has to be built
at the call site. For a value whose whole job is to describe one call, that is
where it belongs anyway.

`Average` is `Averaging?` rather than `Averaging` for a reason that has nothing
to do with style: `default(Averaging)` is `Averaging.Binary`, which multiclass
ROC-AUC refuses outright. A non-nullable property would make
`default(MultiClassRocOptions)` — and `new MultiClassRocOptions()`, and every
initializer that omits `Average` — throw instead of meaning "the default".
`null` means `Macro`, which is what scikit-learn's `average="macro"` default is.

### The parallel path copies its inputs

Above one worker, `CopyForWorkers` rents and fills three arrays: `yTrue`, the
sample weights when the caller supplied any, and a **transposed** copy of the
score matrix. That last one is the size worth naming: about
`samples × classes × 8` bytes — 8 MB at n=100 000, k=10 — rented from
`ArrayPool<double>.Shared` and returned on the way out.

This is not an optimisation, it is the only legal option. A `ReadOnlySpan<T>`
cannot be captured by the body of a `Parallel.For`: the caller's span may point
at the stack, and nothing in the language lets it travel to another thread. The
way round is `unsafe` — pin the memory, capture a raw pointer, index it
unchecked in every worker — and no project under `src/` enables unsafe blocks. A
performance change is not the occasion to reverse that, particularly not to hand
several threads an unchecked pointer into a caller's buffer.

The transpose is free relative to the copy that had to happen anyway: it is the
same single pass, and it leaves each class's column contiguous for the worker
that reads it instead of strided `classCount` apart. So the copy pays for itself
partly in cache behaviour, which is part of why the small-input row gains rather
than loses.

The sequential path does none of this, which is the concrete content of "the
default does not pay for the opt-in".

### `ScoreSource` takes the sample count as a parameter, not `yTrue.Length`

`ScoreSource`'s constructor is handed `sampleCount` explicitly and checks both
`yTrue` and `scores` against it, rather than trusting `yTrue.Length` and
checking only that `scores.Length == yTrue.Length * classCount`. The stricter
form exists because the looser one cannot detect the one failure it exists to
catch: two spans sliced to a **rented array's length** rather than the sample
count, which `CopyForWorkers`'s callers must get right on every parallel call.

`ArrayPool<T>.Shared`'s buckets are powers of two, so `Rent(n).Length * k`
equals `Rent(n * k).Length` far more often than intuition suggests — verified
with `ArrayPool<int>.Shared.Rent`/`ArrayPool<double>.Shared.Rent` (the bucket
size does not depend on the element type) for every `n` in `[2, 4096]` against
`k` in `{2, 4, 8}`: 4088 of the 4095 values of `n` collide for at least one
`k`. (An inline version of this claim previously read "4079 of 4095", which
this sweep re-measured and corrected; the source disagreed with the code it
was explaining.) Two unsliced rented spans would then agree with each other on
length and disagree with reality, `Offset(column)` would multiply by the
bucket size instead of the sample count, and every column after the first
would be read from the wrong place — silently, since both checks the looser
form performs would still pass. Naming the sample count makes both spans
answer to a fact neither of them supplies on its own.

### Only one-vs-rest and one-vs-one are parallelised

The per-class loop and the per-pair loop are spread over workers. Nothing else
is, and specifically no floating-point accumulation is touched.

The property that makes these two safe is narrow and worth stating precisely:
class `c` writes `scores[c]` and `weights[c]`, pair `p` writes `pairScores[p]`
and `prevalence[p]`, and no iteration reads any slot but its own. The averaging
then runs afterwards, on the calling thread, over the array in index order. So
no thread's timing can reach a sum, and the result is bit-identical to the
sequential path's — identical bits, not agreement within a tolerance, which is
what the committed tests compare.

The reason that line is drawn at accumulation rather than somewhere more
generous is Issue #61's own scar. `classification_report`'s parity against
scikit-learn broke there from a change in the **order of floating-point
summation alone** — no algorithm changed, no input changed, and the printed
digits moved. Addition of doubles is not associative, so any reduction whose
order depends on how work was scheduled is a reduction that produces different
output run to run. A parallel `Mean` over the per-class scores would be exactly
that. The speed-up it could offer is `O(classes)` work against `O(samples log
samples)`; the correctness it would cost is the guarantee the whole feature is
sold on.

### `ValidateRowSums` and the final average stay sequential

`ValidateRowSums` walks every row before any dispatch happens. It stays on the
calling thread because its failure message names the **first** offending row —
it interpolates the row index and the sum it found — and "first" is only a fact
if the scan is ordered. Parallelised, the row a caller is told about would depend
on which worker got there first, turning a reproducible diagnostic into a flaky
one.
It is `O(samples × classes)` and it is part of what limits the speed-up; that is
a price paid knowingly for an error message that points at the right row.

`Mean` and `WeightedMean` stay sequential for the summation-order reason above.

### Exceptions are rethrown from the lowest index, and the loop does not stop early

Each iteration returns the `ArgumentException` it caught into its own slot of a
`failures` array. When the loop is done, `RethrowFirst` scans that array in
ascending index order and rethrows the first non-null entry through
`ExceptionDispatchInfo`, so the original instance crosses the API — type,
message and `ParamName` intact, and no `AggregateException` wrapping something
the caller documented a `catch` for.

The loop deliberately does **not** call `ParallelLoopState.Stop`. Stopping looks
like the efficient thing to do — why score class 7 when class 2 has already
failed? — but `Stop` cancels iterations that have not started yet, and an
iteration that never runs cannot report its failure. With eight workers over ten
classes, class 2 failing can leave class 1 unstarted and cancelled, and the
caller is then told about class 2 where the sequential path would have told them
about class 1. The exception a caller sees would depend on thread scheduling.

So every index is attempted even when an earlier one has already failed. This
spends work on an input that is going to throw, which is the one path in the
library with no performance budget to defend.

## Consequences

- [`RocAuc.MultiClass`](../reference/metrics/classification.md#rocaucmulticlass)'s
  parameter list changed shape before the package's first release.
  `DataNet.Metrics` has never shipped, so this is a change to an
  unreleased surface; after 0.1.0 the same change would have been breaking, and
  gathering the arguments now is the cheap moment.
- `MultiClassRocOptions` is a `ref struct` forever, with everything that
  implies: no field, no lambda capture, no `await` across it, and no `async`
  method holding one. Callers construct it at the call site. Every documentation
  snippet must too.
- Two code paths now exist per strategy, sequential and parallel, and they must
  agree bit for bit. That is a permanent test obligation rather than a one-time
  check: `tests/DataNet.Metrics.Tests/RocAucParallelTests.cs` compares raw
  IEEE-754 bits across worker counts, and any future change to either path has
  to keep passing it. That comparison is *relative*, and on its own it is not
  enough: it moves with any change to arithmetic the two paths share, so
  reassociating the division in `Mean` and `WeightedMean` passed the entire suite
  while moving the last bit of three corpus values. The absolute pin is
  `RocAucFrozenBitsTests.cs`, which asserts the bits of all twelve multiclass
  corpus values against constants committed in the file. The frozen oracle cannot
  do that job: it stores scikit-learn's answers to 12 decimals, so the digits
  that move are not in it.
- Callers can make this slower. `dop=8` on a 4-core machine loses to `dop=4` on
  three of the six measured shapes, and the guide says so with numbers. That is
  the accepted cost of honouring the request as given rather than second-guessing
  it, and it is why the option's documentation points at a measurement instead of
  recommending a value.
- **The sequential multiclass path allocates far less than it did.** That is an
  unadvertised win, and it belongs in the record because "the default is
  unchanged" was written here before it was true. Before this branch, one-vs-rest
  allocated a fresh `int[n]` and `double[n]` once per call, plus a fresh
  `double[n]` and `Point[n]` inside `BinaryRoc.Score` *per class* — `2k + 2` new
  heap arrays for one call — and one-vs-one `4 × pairs + 2`, two curves per pair.
  Both now take four pooled rentals in total — one `Scratch`, reused across every
  class and every pair — on top of the small `k`-length or `pairs`-length result
  arrays that both versions allocate either way. At n=100 000, k=10 that is 22
  fresh arrays, most of them on the large-object heap at that size, replaced by
  four rentals from `ArrayPool<T>.Shared`.
- **The binary entry point pays a small unadvertised cost.**
  [`RocAuc.Score`](../reference/metrics/classification.md#rocaucscore) used to
  allocate two fresh arrays and now rents four, because it shares
  `BinaryRoc.Scratch` with the multiclass drivers and two of the four —
  `Scratch.Binary` and `Scratch.Column`, where a driver compacts one class's
  samples before scoring — are never read on the binary path. Two idle rentals
  per call, returned on the way out, against two large-object allocations saved.
  Worth naming rather than hiding; not worth a second `Scratch` shape to avoid.
- The parallel path allocates on top of all that, again from the shared pool. At
  n=100 000, k=10 it is about 8 MB for the transposed score matrix, rented and
  returned per call. A caller in a tight loop who has not measured should leave
  the default alone.
- The published table is a comparison of this code against itself on elapsed
  time. It is not comparable to Issue #61's scikit-learn table, which is
  processor time and was taken at a different machine load. Keeping those two
  apart is now a documentation obligation the guide states in as many words.
