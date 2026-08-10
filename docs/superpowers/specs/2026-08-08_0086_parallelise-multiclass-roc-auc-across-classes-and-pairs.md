# Parallelising multiclass ROC-AUC — design

**Issue:** [#86](https://github.com/CyrilB1531/data.net/issues/86) ·
**Date:** 2026-08-08 · **Package:** `DataNet.Metrics`

## Problem

Issue #61 shipped classification metrics at scikit-learn parity and gated the
merge on processor time against scikit-learn at every size. All 29 passed, and
one row passed by the narrowest margin: `roc_auc_ovr_macro` at n=100 000, k=10,
at **2.74×**, costing 88.4 ms wall where a confusion matrix over ten times the
samples costs 9.9 ms. ROC-AUC is an order of magnitude more expensive than
everything else in the package and holds the smallest lead, because it is
sort-bound and numpy's `argsort` is well-optimised C.

At k=10, `roc_auc_ovr_macro` is ten independent binary ROC computations run one
after another on one thread, on a machine with eight. This work spends those
cores.

## Why one-vs-rest and one-vs-one, and nothing else

Because they parallelise without changing a single bit of the result. Each class
(one-vs-rest) and each pair (one-vs-one) computes its AUC from its own column,
writes it into its own slot, and the averaging happens afterwards in array order.
Which thread produced which slot cannot reach the sum.

The same is not true elsewhere, and the reason is on the record: during #61, a
change of floating-point summation order — nothing more — broke the
character-exact parity of `classification_report`'s text output. Parallelising a
floating-point accumulation is that hazard by construction.

Out of scope, therefore: the confusion-matrix pass (memory-bound, 8.5 ms for a
million samples, nothing to win), the binary curve's trapezoid accumulation, and
the sort inside the binary curve — that last one is parallelisable safely and is
worth its own issue after this lands.

## The decision the issue asks for

**Parallelism is opt-in, and the default stays sequential.** A library that
silently spawns threads is hostile inside a server already running one request
per core, and scikit-learn does not parallelise `roc_auc_score` either. The
caller names the worker count; nothing about the current default path changes.

Two consequences follow, both deliberate:

- There is **no `-1` sentinel** meaning "all cores". A caller who wants all
  cores writes `Environment.ProcessorCount`, so the number is visible at the
  call site rather than resolved inside the library.
- The setting is **honoured always**, with no internal size threshold. The knob
  means what it says; nobody turns it by accident, because the default is
  sequential. What the opt-in costs at n=1000 is measured and published rather
  than papered over by a threshold calibrated on one workstation.

## Public API

The four trailing optional parameters of `RocAuc.MultiClass` are replaced by one
options value. `DataNet.Metrics` is at 0.1.0 and has never shipped, so the
surface that changes is an unreleased surface, and the package keeps one way to
express a call rather than gaining a second.

```csharp
public readonly ref struct MultiClassRocOptions
{
    /// <summary>One-vs-rest or one-vs-one. Defaults to OneVsRest.</summary>
    public MultiClassStrategy Strategy { get; init; }

    /// <summary>Macro or Weighted. Null — the default — means Macro.</summary>
    public Averaging? Average { get; init; }

    /// <summary>The classes the columns stand for, sorted ascending and unique.</summary>
    public ReadOnlySpan<int> Labels { get; init; }

    /// <summary>A weight per sample. Refused with OneVsOne, as scikit-learn refuses it.</summary>
    public ReadOnlySpan<double> SampleWeight { get; init; }

    /// <summary>
    /// Workers over the per-class or per-pair loop. 0 and 1 — the default — are
    /// sequential. Above 1, the parallel path copies the inputs; see below.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; }
}

public static double MultiClass(
    ReadOnlySpan<int> yTrue,
    ReadOnlySpan<double> yScore,
    int classCount,
    MultiClassRocOptions options = default);
```

### Three encodings that need defending

**A `ref struct`, not a class or a plain struct.** `Labels` and `SampleWeight`
are `ReadOnlySpan<T>`, which cannot be a field of anything but a `ref struct`.
Any other shape would turn them into arrays and impose an allocation on every
caller. The file already has this precedent: `MultiClassRoc.PairContext` is a
`readonly ref struct` created for the same reason.

**`Averaging?`, not `Averaging`.** `default(Averaging)` is `Binary`, which
multiclass ROC-AUC refuses — so `default(MultiClassRocOptions)` would throw
instead of reproducing today's defaults. `null → Macro` is the honest encoding:
writing `Average = Averaging.Binary` still throws, so the existing validation
survives intact. `Strategy` has no such problem, `OneVsRest` being 0 already.
Renumbering `Averaging` was rejected — `Binary` is scikit-learn's default for
`Precision`, `Recall`, `F1` and `FBeta`, which have nothing to do with this.

**0 means sequential, not invalid.** `default` has to mean sequential, so 0 and 1
are both sequential and only a negative value throws
`ArgumentOutOfRangeException`.

### Rejected alternatives

- **An eighth optional parameter** on the existing signature. Smaller diff, but
  it leaves a method of eight parameters and no room for the next knob.
- **A sibling `MultiClassParallel` method.** Parallelism visible at the call site
  without reading argument names, at the price of seven duplicated parameters and
  their XML documentation, and two methods to keep in step forever.
- **A `ReadOnlyMemory<T>` overload**, which would need no copy at all. Rejected:
  `MultiClass(int[], double[], 3)` is ambiguous between the span and memory
  overloads, which is exactly the trap `RocAuc`'s own class remark already
  documents for the binary and multiclass entry points.

## Internal design

### How spans reach worker threads

A `ReadOnlySpan<T>` cannot be captured by the body of a `Parallel.For`: the
caller's span may point at the stack, and nothing in the language allows handing
it to another thread. `fixed` pointers would do it at zero cost and are refused —
`DataNet.Text` sets `AllowUnsafeBlocks=false` deliberately, and a perf issue does
not reverse a posture.

So on the parallel path, and only there, the inputs are copied: `yTrue` into a
rented `int[]`, and `yScore` **transposed** into a column-major rented
`double[]`. Transposing costs the same single pass as a straight copy and makes
each column contiguous for the worker that reads it, instead of reads spaced `k`
apart — the copy recovers part of its own price.

One extraction-and-score kernel, written once over `ReadOnlySpan`, parameterised
by `(offset, stride)`:

| Path | Source | Offset | Stride | Copy |
| --- | --- | ---: | ---: | --- |
| sequential (default) | the caller's span, row-major | `c` | `k` | none |
| parallel (opt-in) | rented array, column-major | `c × n` | `1` | `n` ints + `n×k` doubles |

Only the driver differs. The default path rents nothing and copies nothing: it
remains the code that shipped in #61, reading the caller's memory in place.

**The opt-in costs a copy of the input.** At n=100 000, k=10 that is 8.4 MB
rented and returned. This goes in the XML documentation and in the ADR, not in a
footnote.

### One set of buffers per worker, not per iteration

`Parallel.For`'s `localInit`/`localFinally` give each participating thread one
rental of `binary`, `column`, `keys` and `points`, returned when that thread is
done. The sequential path rents the same set once outside its loop.

This forces the buffers into `BinaryRoc.Score`, which today allocates `keys` and
`points` on every call. That is not tidiness: at n=100 000 those arrays are
800 KB and 1.6 MB — **large-object heap**, whose allocation takes a lock. Eight
workers allocating 2.4 MB of LOH per class would serialise much of the gain this
issue exists to collect. The sequential path allocates less than it does today as
a side effect, and produces the same numbers.

Consequences inside `BinaryRoc`:

- `Point` becomes visible to the scratch type that holds the arrays.
- `BuildPoints` writes `[0, size)` of buffers that may be longer.
- The sort becomes `Array.Sort(keys, points, 0, size)`.
- `Accumulate` takes `size` instead of reading `keys.Length`.

### What stays sequential, deliberately

- **`ValidateRowSums`.** Its message names the first offending row; parallelising
  it would change which row is reported. It is one `O(n×k)` pass against `k`
  sorts of `n log n`, so it is a small share of the total — the measurement will
  say how small rather than this document guessing.
- **`Mean` and `WeightedMean`.** They are the floating-point accumulation this
  issue is forbidden to reorder.

### One-vs-one indexing

The nested `(a, b)` loops become a flat `Parallel.For` over `0..pairCount` with a
precomputed `(a, b)` table — `pairCount` entries, built once — rather than
decoding a triangular index arithmetically. Each iteration computes its own
`size`, its own two ordering scores and writes its own `pairScores[pair]` and
`prevalence[pair]`.

## Errors

Today a bad input throws from the first offending class. In parallel, which
worker throws first is a scheduling detail, and `Parallel.For` wraps everything
in an `AggregateException` — two observable regressions.

Each worker therefore catches into its own slot, **the loop does not stop early**,
and after the loop the exception from the lowest index is rethrown through
`ExceptionDispatchInfo`. Same type, same message, same `ParamName` as sequential,
and no `AggregateException` crosses the public API.

Not stopping early is the subtle part. `ParallelLoopState.Stop` could cancel
class 1 before it ran, and class 3's exception would be reported where sequential
reports class 1's. The error path therefore does all the work; it has no budget
to defend.

## Testing

`tests/oracles/roc_auc.json` is not regenerated and does not move by a byte, so
the "Oracles are reproducible" CI job stays green without doing anything.

| Test | What it prevents |
| --- | --- |
| Bit-identical replay of the frozen corpus: every multiclass case, every `values` key, `dop=1` against `dop=2,3,8`, compared through `BitConverter.DoubleToInt64Bits` | a parallelisation that moves a bit |
| The existing scikit-learn tolerance test, replayed with a non-trivial `MaxDegreeOfParallelism` | a corpus validated on one code path only |
| Two offending classes, sequential against parallel: identical type, message and `ParamName` | a leaked `AggregateException`, or the wrong class's exception |
| A negative `MaxDegreeOfParallelism` throws `ArgumentOutOfRangeException`; 0 and 1 behave identically | `default` ceasing to mean sequential |

The netstandard2.0 suite replays all of it — `Parallel` is in the ns2.0 contract.

## Measurement

BenchmarkDotNet reports elapsed time only, and the issue requires both axes. The
vehicle is therefore `bench/DataNet.Text.Benchmarks/CrossLang/Harness.cs`, which
already records wall and processor time from the same run, behind a new
`roc-parallel` mode. The name is deliberately not `compare-*`: those are the
face-offs against Python, and this is C# against C#.

**The k=5 data does not exist.** The bench corpus knows k=2 and k=10 only, and
its ten-class score matrix stops at 100 000 rows. The harness generates its own
input from a fixed seed instead of extending `generate_metrics.py`: for a C#
against C# before-and-after, the only property that matters is the same data on
both sides, and that is guaranteed more firmly in memory than by a committed
file. `bench/corpus/` is untouched and #61's published table stays intact.

Every cell at `dop=1, 2, 4, 8`, wall and processor time:

| Shape | What it establishes |
| --- | --- |
| OvR n=10⁵, k=10 | the speed-up the issue targets, 10 independent units |
| OvR n=10⁵, k=5 | the second required point, 5 units over 8 threads — parallelism in excess |
| OvO n=10⁵, k=5 and k=10 | 10 then 45 pairs, two curves each: the heaviest load in the package |
| OvR n=1000, k=10 | what the opt-in costs on a small input, published even if negative |

Cores named: **Intel i7-4770S, 4 physical cores / 8 logical threads**, with load
conditions recorded as #61's table already does.

The numbers land in a new subsection of `docs/guides/performance.md` under the
metrics section, stating in one explicit sentence that the axis is **elapsed
time**, that processor time rises, and that this is expected — the issue requires
that written down rather than an axis quietly switched.

## Acceptance

- [ ] Every value in `tests/oracles/roc_auc.json` replays bit-identically
      between the sequential and parallel paths, compared as raw IEEE-754 bits.
- [ ] Before and after measured on the same machine in one sitting, wall and
      processor time both reported, elapsed time named as the axis.
- [ ] A stated speed-up at k=10 and k=5 against the sequential baseline, with the
      core count named.
- [ ] The threading decision recorded in
      `docs/decisions/0017-multiclass-roc-auc-parallelism-is-opt-in.md`.
- [ ] The n=1000 path does not regress: the default path is untouched by
      construction, and the opted-in cost at that size is measured and published.
- [ ] `dotnet build` clean (warnings are errors), `dotnet test` green on net10 and
      netstandard2.0, `dotnet format --verify-no-changes` and markdownlint clean.

## Plumbing that would fail the build if forgotten

- `samples/DataNet.Sample/PackagingGate.cs` requires a **member reference** to
  every exported public type. `MultiClassRocOptions` is a new public type, so
  `Lot5Metrics` must set at least one of its properties, not merely name it.
- `DataNet.Metrics` stays at **0.1.0** — never published, so the changed surface
  is an unreleased one. The CHANGELOG entry goes under the existing
  `### DataNet.Metrics — 0.1.0` heading, not into a new version.
- The 11 call sites in `tests/DataNet.Metrics.Tests/RocAucMultiClassTests.cs`,
  3 in `samples/DataNet.Sample/Lot5Metrics.cs`, 1 in
  `bench/DataNet.Text.Benchmarks/CrossLang/MetricsCrossLang.cs` and the
  `RocAuc.MultiClass` row of `docs/equivalence.md` all move to the options form.

## Out of scope

The confusion-matrix path. The binary curve's accumulation loop. The sort inside
the binary curve, which is safely parallelisable and earns its own issue. Any
change to `RocAuc.Score`, the binary entry point.
