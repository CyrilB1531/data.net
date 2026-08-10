# Performance

Performance is the selling point against Python, so it is measured from Lot 1 with
[BenchmarkDotNet](https://benchmarkdotnet.org/), not estimated.

## Reproduce

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*Levenshtein*'
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*VectorizerBenchmarks*' '*FuzzBenchmarks*'
```

## Principles applied

- **`ReadOnlySpan<char>` everywhere.** Inputs are never copied; `string` literals
  convert with no allocation.
- **`ArrayPool<int>` for the dynamic-programming matrices.** The DP row is rented
  then returned: **zero managed allocation per call**, so no GC pressure even
  under heavy load.
- **Rolling DP row on the shorter operand** → `O(min(n, m))` memory.
- **Common prefix/suffix trimming** → collapses the DP band on near-equal inputs
  (the common case in record matching).

## Levenshtein — indicative numbers

Short-job measurement (reduced iterations: means are noisy, but the allocation
column is reliable). Re-run with a full job before quoting.

| Method | Length | Mean | Allocated |
| --- | --- | ---: | ---: |
| `Distance` (UTF-16) | 8 | ~35 ns | **0 B** |
| `Distance` (code point) | 8 | ~208 ns | **0 B** |
| `Distance` (UTF-16) | 64 | ~7.0 µs | **0 B** |
| `Distance` (UTF-16) | 512 | ~21 µs | **0 B** |

**Zero allocation** at every size is the structural result. On very short inputs
the code-point mode costs ~5× the UTF-16 mode (the decode pass dominates when the
computation itself is tiny); from 64+ characters the gap closes. Hence the choice:
**UTF-16 by default**, `CodePoint` on demand.

## Compared to Python (rapidfuzz) — Levenshtein

Cross-language bench with **identical methodology on both sides** (same committed
ASCII corpus, ns/pair throughput, auto-scaling, best-of-5). See
[`bench/README.md`](../../bench/README.md).

Indicative measurement (rapidfuzz 3.14.5 / Python 3.12; DataNet.Text / .NET 10 on
an Intel i7-4770S; dev machine — non-authoritative), **after** adding the blocked
(multi-word) Myers fast path:

| Length | Python (rapidfuzz) | C# (DataNet.Text) | Ratio | C# path |
| ---: | ---: | ---: | --- | --- |
| 8 | 183 ns/pair | **36 ns/pair** | **5.1× C# faster** | DP |
| 32 | **324 ns/pair** | 453 ns/pair | 1.4× Python | Myers (single word) |
| 128 | 2 693 ns/pair | **1 777 ns/pair** | **1.5× C# faster** | Myers (blocked) |
| 512 | 21 688 ns/pair | **20 555 ns/pair** | **1.06× C# faster** | Myers (blocked) |

- **Short strings (≤ ~40)** — the typical name/identifier matching case: C# is
  ahead, largely because rapidfuzz pays per-call interop overhead there.
- **Long strings** — previously 13–31× *behind* rapidfuzz, because patterns over
  64 characters fell back to the DP. Blocked Myers closed that: the 512 bucket
  went from 684 µs to 21 µs, a 33× improvement, and now edges ahead. It was never
  a language problem, only an algorithmic one.
- **The length-32 bucket is the remaining gap**, at 1.4× behind. It already takes
  the single-word path, so this is not the same cause; it wants its own
  measurement rather than a guess.
- **Scope.** The bit-parallel path requires a Latin-1 pattern. Outside that — CJK,
  emoji — `Distance` still uses the DP, so the figures above do not describe those
  inputs. Extending the equality table beyond Latin-1 is unresolved; see
  [`../decisions/0004-levenshtein-myers-backlog.md`](../decisions/0004-levenshtein-myers-backlog.md).

## Vectorizers and fuzzy matching

Short-job measurement, `[MemoryDiagnoser]` (dev machine — indicative).

**Vectorizers**, fit+transform over a synthetic corpus:

| Method | 200 docs | 1000 docs |
| --- | ---: | ---: |
| `CountVectorizer` | ~4.2 ms | ~9.0 ms |
| `TfidfVectorizer` | ~4.3 ms | ~9.1 ms |
| `CountVectorizer` (bigrams) | ~4.8 ms | ~14.7 ms |
| `HashingVectorizer` | ~3.6 ms | ~8.6 ms |

**Fuzzy ratios**, on a ~43-character sentence pair:

| Method | Mean | Allocated |
| --- | ---: | ---: |
| `Fuzz.Ratio` | ~2.5 µs | **0 B** |
| `Fuzz.TokenSortRatio` | ~5.3 µs | 1.3 KB |
| `Fuzz.TokenSetRatio` | ~15 µs | 5.6 KB |
| `Fuzz.WRatio` | ~25 µs | 7.0 KB |
| `Fuzz.PartialRatio` | ~460 µs | 0 B |

> `PartialRatio` is markedly slower: the current sliding-window scan is `O(n·m²)`
> (a full Indel per window). It is correct and zero-alloc, but a bit-parallel or
> block-based optimization is a clear backlog item for long inputs.

## Batched embedding — what the number is, and what it is not

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*BatchEmbedding*' --inProcess
```

**Read this before quoting the ratio.** The model is `tiny_embedder.onnx`: one
Gather node over a 64 × 4 table, because weights are never committed
(`CONTRIBUTING.md`) and a real encoder is a hundred megabytes. Its arithmetic is
free. So what is measured is the per-sequence cost that batching removes — graph
dispatch, thread-pool wake-up, tensor wrapping — and none of the matrix
multiplication a real encoder adds to *both* sides. This is an upper bound on the
speed-up, not the speed-up.

Full job, `[MemoryDiagnoser]`, `InProcessEmitToolchain`, Intel Core i7-4770S
(Haswell, 4 physical cores), Ubuntu 24.04, .NET 10.0.10, X64 RyuJIT AVX2. Corpus
of 1 to 61 words per text, sub-batch 8. `UnitLoop` is the baseline — one `Embed`
call per text, which is what the guide's three lines amounted to before
`EmbedBatch` existed. **Two runs, both shown**, because BenchmarkDotNet's `±`
describes dispersion inside one process and not reproducibility across processes.

| Texts | `UnitLoop` | `EmbedBatch` | ratio | `EmbedBatchBucketed` | ratio | allocated vs baseline |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 9.9 / 11.0 µs | 10.6 / 10.8 µs | 1.06 / 0.99 | 10.0 / 11.1 µs | 1.00 / 1.01 | 1.12 |
| 8 | 168 / 180 µs | 105 / 107 µs | 0.62 / 0.60 | 101 / 106 µs | 0.60 / 0.59 | 0.95 |
| 32 | 628 / 672 µs | 360 / 381 µs | 0.57 / 0.57 | 346 / 349 µs | 0.55 / 0.52 | 0.94 / 0.91 |
| 128 | 2 439 / 2 602 µs | 1 482 / 1 460 µs | 0.61 / 0.56 | 1 370 / 1 356 µs | 0.56 / 0.52 | 0.94 / 0.91 |

**Batching removes about 40 % of the wall clock** from 8 texts upward and stays
there — 0.56–0.62 across every pairing — as the per-call overhead is amortized
over the whole sub-batch. At a single text there is nothing to amortize and the
two paths are a wash: 1.06 in one run, 0.99 in the next, which is a way of
saying this benchmark cannot tell them apart there rather than that either wins.

**Bucketing is a different story, and the honest answer is smaller.** It engages
only when the corpus spans more than one sub-batch, so the rows at 1 and 8 above
run the *identical* code in both columns — they are the control, and what they
differ by is this harness's noise floor: 1–3 % on seven of the eight control
measurements taken here, with one outlier at 5.7 %. At 128 texts bucketing is
ahead by 5.8–7.5 % in all four pairings, and ahead at 32 in all four as well.
The sign is consistent where the magnitude alone would not be decisive. What is
decisive is the allocation column, which is counted rather than sampled:
1 764 KB → 1 697 KB at 128 texts, in every run. That is padding genuinely not
written. On a model doing real work that padding would be matrix multiplication
not performed, and the time would follow; this model cannot show it, so the
claim stops here.

**The two builds.** The same benchmark against the `netstandard2.0` assemblies
does **not** measure a penalty. On `EmbedBatch` the netstandard2.0 side comes in
1.4–5.7 % *ahead* of net10 in all four pairings (355 / 365 µs against 360 / 381
at 32 texts; 1 418 / 1 419 against 1 482 / 1 460 at 128), which is inside this
harness's noise but consistent in direction. This guide previously reported the
opposite — "3–5 % behind" — from a pair of commands that did not share a
BenchmarkDotNet toolchain; that comparison is withdrawn rather than reversed
(issue #87).

Withdrawn, not disproved. The figures above cannot be set against the old ones,
for a reason that has nothing to do with either harness: the old table predates
the bump of `Microsoft.ML.OnnxRuntime` to 1.28.0, and this benchmark is almost
entirely that library's dispatch cost. Add to that a machine carrying twice the
load, and no difference between the two sets is attributable to anything. What
the old pair of commands can be faulted for is visible without measuring — its
two columns came from two toolchains.

Within *this* window the harness is not the explanation either. Running the
net10 side out-of-process here, the same mismatched shape, moves this tier by
2 % at most (`EmbedBatch` 356 µs at 32 texts and 1 441 at 128, against 360 / 381
and 1 482 / 1 460 in-process) and still shows no netstandard2.0 penalty. So
unlike `VectorMath`, where the mismatch moves the ratio by up to 0.5×, on this
path the toolchain barely registers.

**No penalty is the structurally correct answer here**, and the earlier figure
should have been read as suspicious for that reason. `Pooling` guards its
`Vector<T>` branch with `accumulator.Length >= Vector<float>.Count`;
`tiny_embedder.onnx` has a hidden size of 4 (`EMBEDDING_DIM` in
`tools/build_tiny_models.py`) and `Vector<float>.Count` is 8 under AVX2, so on
net10 the guard is false and the code falls into the same scalar tail loop
netstandard2.0 runs unconditionally. The two builds execute identical pooling on
this benchmark. It cannot measure the difference between them, and now reports
that instead of a number. Where the vector path does engage, it is worth 4×–7×
(`VectorMath` over 384–1024 dimensions, section 2 of
[`bench/README.md`](../../bench/README.md)). The one
difference this benchmark does resolve is counted, not timed: the unit-loop path
allocates 0.6 % more on netstandard2.0 (1 887 KB against 1 875 at 128 texts),
identically in both runs, while the two batch paths allocate byte for byte the
same on both targets.

```bash
dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter '*BatchEmbedding*'
```

`--inProcess` on the first command, and not on the second, is the point: the
netstandard2.0 project pins `InProcessEmitToolchain` in its `Program.cs` — it
has to, or BenchmarkDotNet's generated project re-resolves the
`ProjectReference` and silently restores the net10.0 build — so the flag is what
puts the net10 side on the same toolchain. Without it the two commands measure
the same code two different ways.

**Conditions.** The four runs behind the table were taken back to back in one
window, alternating net10 and netstandard2.0, with the one-minute load average
between 5.1 and 5.9 on 8 logical cores — the editor's language servers and the
session driving the runs are part of that load and cannot be excluded from
inside it. Both columns pay it equally, so the table is internally comparable;
it is not comparable to figures taken on this machine in a quieter state, and
the ratios travel between such sets while the absolute microseconds do not.

## Classification metrics (issue #61) — vs scikit-learn

```bash
python bench/corpus/generate_metrics.py           # writes bench/corpus/metrics/, git-ignored
. .venv-oracles/bin/activate && python bench/python/bench_metrics.py
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- compare-metrics
python bench/compare.py metrics
```

Six operations — `confusion_matrix`, `accuracy`, `precision_recall_f1_macro`,
`classification_report`, `roc_auc_binary`, `roc_auc_ovr_macro` — over six shapes
(1 000 / 100 000 / 1 000 000 samples, 2 or 10 classes), on the same corpus files
on both sides. **This is the merge gate for the branch, on processor time**: every
row must be ≥ 1×, and it is.

DataNet.Metrics on .NET 10.0.10 against scikit-learn 1.9.0 / NumPy 2.5.1 on
Python 3.12.3, Intel i7-4770S. Both sides measured back to back, Python first,
on a machine left to settle (one-minute load 1.52 at the Python start — below
this workstation's 1.9–2.3 floor, itself a permanent ~30–40 % background from
the desktop client, an editor and a browser). The C# side started 49 seconds
later, in the Python run's own wake, so its figures are the ones taken on the
busier machine and every ratio below is conservative rather than flattering;
`bench/README.md` records the full conditions.

| Operation | DataNet ms | Python ms | wall | DataNet cpu ms | Python cpu ms | **cpu** |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `confusion_matrix_n1000_k2` | 0.009 | 1.028 | 117.98x | 0.009 | 1.028 | **117.97x** |
| `accuracy_n1000_k2` | 0.001 | 0.546 | 618.32x | 0.001 | 0.546 | **618.33x** |
| `precision_recall_f1_macro_n1000_k2` | 0.008 | 1.793 | 226.58x | 0.008 | 1.793 | **226.58x** |
| `classification_report_n1000_k2` | 0.011 | 6.692 | 623.31x | 0.011 | 6.691 | **623.25x** |
| `roc_auc_binary_n1000_k2` | 0.029 | 2.008 | 70.12x | 0.029 | 2.008 | **70.12x** |
| `confusion_matrix_n1000_k10` | 0.009 | 1.051 | 120.64x | 0.009 | 1.051 | **120.64x** |
| `accuracy_n1000_k10` | 0.001 | 0.541 | 622.03x | 0.001 | 0.541 | **622.08x** |
| `precision_recall_f1_macro_n1000_k10` | 0.010 | 1.855 | 192.49x | 0.010 | 1.855 | **192.48x** |
| `classification_report_n1000_k10` | 0.017 | 7.011 | 422.54x | 0.017 | 7.010 | **422.53x** |
| `roc_auc_ovr_macro_n1000_k10` | 0.550 | 10.526 | 19.13x | 0.550 | 10.525 | **19.13x** |
| `confusion_matrix_n100000_k2` | 0.964 | 15.791 | 16.39x | 0.964 | 15.791 | **16.39x** |
| `accuracy_n100000_k2` | 0.190 | 5.519 | 29.01x | 0.190 | 5.518 | **29.01x** |
| `precision_recall_f1_macro_n100000_k2` | 0.844 | 17.786 | 21.07x | 0.844 | 17.785 | **21.07x** |
| `classification_report_n100000_k2` | 0.848 | 36.233 | 42.75x | 0.847 | 36.231 | **42.75x** |
| `roc_auc_binary_n100000_k2` | 7.977 | 35.024 | 4.39x | 8.092 | 35.023 | **4.33x** |
| `confusion_matrix_n100000_k10` | 1.059 | 16.109 | 15.20x | 1.059 | 16.108 | **15.21x** |
| `accuracy_n100000_k10` | 0.296 | 5.519 | 18.66x | 0.296 | 5.519 | **18.66x** |
| `precision_recall_f1_macro_n100000_k10` | 0.979 | 18.524 | 18.92x | 0.979 | 18.523 | **18.92x** |
| `classification_report_n100000_k10` | 0.979 | 40.139 | 41.00x | 0.979 | 40.137 | **41.00x** |
| `roc_auc_ovr_macro_n100000_k10` | 88.385 | 250.400 | 2.83x | 91.396 | 250.402 | **2.74x** |
| `confusion_matrix_n1000000_k2` | 8.750 | 156.920 | 17.93x | 8.749 | 156.823 | **17.92x** |
| `accuracy_n1000000_k2` | 2.045 | 51.599 | 25.23x | 2.045 | 51.596 | **25.23x** |
| `precision_recall_f1_macro_n1000000_k2` | 8.701 | 164.332 | 18.89x | 8.701 | 164.330 | **18.89x** |
| `classification_report_n1000000_k2` | 8.719 | 314.805 | 36.11x | 8.718 | 314.782 | **36.11x** |
| `roc_auc_binary_n1000000_k2` | 95.219 | 364.420 | 3.83x | 95.684 | 364.384 | **3.81x** |
| `confusion_matrix_n1000000_k10` | 9.916 | 156.707 | 15.80x | 9.915 | 156.699 | **15.80x** |
| `accuracy_n1000000_k10` | 3.122 | 51.877 | 16.61x | 3.122 | 51.874 | **16.61x** |
| `precision_recall_f1_macro_n1000000_k10` | 10.001 | 173.128 | 17.31x | 10.000 | 173.121 | **17.31x** |
| `classification_report_n1000000_k10` | 9.865 | 352.364 | 35.72x | 9.864 | 352.349 | **35.72x** |

**Gate result: 29/29 operations at or above 1× on processor time.** The
narrowest margin is **2.74×**, on `roc_auc_ovr_macro` at n=100 000, k=10 — the
row the design brief flagged as the one most likely to need a radix-sort
rewrite of `BinaryRoc`. It did not: even the heaviest sort-bound row clears the
gate by a comfortable margin, so no algorithmic change was needed on this
branch.

**Read this before quoting a single ratio.** The rows at n=1 000 (70×–620×) are
dominated by CPython's per-call interpreter overhead, not by the computation —
a confusion matrix over 1 000 samples is sub-microsecond work on either side.
The rows that carry the argument are the ones at n=100 000 and n=1 000 000,
where the ratios settle to a more modest but still decisive 2.7×–43×.

Unlike the persistence comparison, wall and processor time agree here to
within about 1% on every row (up to 3.4% on the single heaviest-cpu row): these
metrics allocate little enough per call that .NET's background collector is
never a factor, so there is no gap between the two columns to explain away.

Full breakdown, including the intra-C# and net10-vs-netstandard2.0 tiers and
where the two language sides do not do identical work, in
[`bench/README.md`](../../bench/README.md#5-classification-metrics-issue-61).

### Balanced accuracy, Matthews correlation, Cohen's kappa (issue #93)

Balanced accuracy, Matthews correlation and Cohen's kappa (issue #93, Tasks
3–5) add three operations — `balanced_accuracy`, `matthews`, `cohen_kappa` —
run over all six shapes above, unweighted and with default label handling on
both sides, matching scikit-learn's `balanced_accuracy_score`,
`matthews_corrcoef` and `cohen_kappa_score`. Same corpus files, same harnesses,
same methodology as the table above — **but measured in a separate window
from the original 29 rows, with its own load**: `uptime`'s one-minute average
was **19.70** just before the Python side started and **7.65** by the time
`compare.py` printed the numbers below (fifteen-minute average 13.2–14.9
throughout that window). That is nowhere near the 1.52 one-minute load the
paragraph above states for the original run, so these 18 rows should not be
read as sharing that sentence's conditions — only their own, given here.

| Operation | DataNet ms | Python ms | wall | DataNet cpu ms | Python cpu ms | **cpu** |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `balanced_accuracy_n1000_k2` | 0.016 | 1.194 | 76.44x | 0.011 | 1.194 | **105.24x** |
| `matthews_n1000_k2` | 0.017 | 2.216 | 134.27x | 0.012 | 2.216 | **192.06x** |
| `cohen_kappa_n1000_k2` | 0.018 | 1.240 | 67.89x | 0.012 | 1.240 | **105.23x** |
| `balanced_accuracy_n1000_k10` | 0.008 | 1.225 | 152.93x | 0.008 | 1.225 | **152.96x** |
| `matthews_n1000_k10` | 0.008 | 2.258 | 282.30x | 0.008 | 2.258 | **282.33x** |
| `cohen_kappa_n1000_k10` | 0.009 | 1.399 | 157.84x | 0.009 | 1.399 | **157.84x** |
| `balanced_accuracy_n100000_k2` | 0.887 | 17.287 | 19.49x | 0.887 | 17.282 | **19.48x** |
| `matthews_n100000_k2` | 0.884 | 34.733 | 39.28x | 0.884 | 34.712 | **39.26x** |
| `cohen_kappa_n100000_k2` | 0.880 | 18.133 | 20.61x | 0.880 | 18.103 | **20.58x** |
| `balanced_accuracy_n100000_k10` | 1.001 | 17.326 | 17.31x | 1.001 | 17.320 | **17.31x** |
| `matthews_n100000_k10` | 0.996 | 36.312 | 36.46x | 0.996 | 36.307 | **36.45x** |
| `cohen_kappa_n100000_k10` | 0.980 | 17.130 | 17.49x | 0.979 | 17.129 | **17.49x** |
| `balanced_accuracy_n1000000_k2` | 9.087 | 166.698 | 18.35x | 9.085 | 166.690 | **18.35x** |
| `matthews_n1000000_k2` | 9.003 | 350.953 | 38.98x | 9.003 | 350.762 | **38.96x** |
| `cohen_kappa_n1000000_k2` | 9.032 | 186.455 | 20.64x | 9.032 | 185.697 | **20.56x** |
| `balanced_accuracy_n1000000_k10` | 10.103 | 167.552 | 16.58x | 10.102 | 167.550 | **16.59x** |
| `matthews_n1000000_k10` | 10.262 | 340.992 | 33.23x | 10.261 | 340.854 | **33.22x** |
| `cohen_kappa_n1000000_k10` | 10.352 | 174.623 | 16.87x | 10.352 | 174.619 | **16.87x** |

**18/18 at or above 1× on processor time — the gate holds for these three
metrics too.** The two narrowest are `balanced_accuracy_n1000000_k10` at
**16.59×** and `cohen_kappa_n1000000_k10` at **16.87×**; every other row
clears 17×. As with the original 29, the busier the machine gets, the more
conservative (not flattering) a ratio above 1× is — and this window's load
average was roughly 5–13× the original run's, so these margins are, if
anything, understated relative to a quiet machine.

### Regression metrics — mse, mae, median_ae, r2 (issue #92)

The eleven regression metrics landed for issue #92 add four benchmark
operations — `mse`, `mae`, `median_ae`, `r2` — covering the four distinct cost
shapes among them: a squared mean, an absolute mean, a sort, and a two-pass
centred sum. The other seven metrics are one of those four with a different
arithmetic kernel and are not separately timed. They run over
`y_true_real`/`y_pred_real`, continuous targets drawn by a separate seeded
random generator and attached to each of the six existing corpus shapes,
independent of the classification columns those shapes already carry — the
generator inserting these draws would otherwise have shifted every
classification array after the insertion point, invalidating the 29 and 18
rows above; a before/after comparison of `y_true[:10]` on the regenerated
corpus confirmed it did not. Same corpus files, same harnesses, same
methodology as the tables above — **but measured in yet another separate
window, with its own load**: `uptime`'s one-minute average was **8.05** just
before the Python side started (five/fifteen-minute: 11.95 / 14.25) and
**6.05** by the time `compare.py` printed the numbers below (five/fifteen-minute:
7.15 / 11.07). That is well below the 16–23 one-minute load this session saw
at dispatch and while the code changes were being made, but still noticeably
busier than the 1.52 one-minute load recorded for the original 29 rows, so
these 24 rows should be read only under their own conditions, given here.

| Operation | DataNet ms | Python ms | wall | DataNet cpu ms | Python cpu ms | **cpu** |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `mse_n1000_k2` | 0.005 | 0.486 | 104.89x | 0.005 | 0.458 | **98.88x** |
| `mae_n1000_k2` | 0.005 | 0.358 | 77.79x | 0.005 | 0.358 | **77.70x** |
| `median_ae_n1000_k2` | 0.038 | 0.386 | 10.23x | 0.038 | 0.385 | **10.23x** |
| `r2_n1000_k2` | 0.008 | 0.443 | 57.72x | 0.008 | 0.442 | **57.66x** |
| `mse_n1000_k10` | 0.005 | 1.003 | 219.23x | 0.005 | 0.646 | **141.17x** |
| `mae_n1000_k10` | 0.005 | 0.541 | 119.33x | 0.005 | 0.507 | **111.80x** |
| `median_ae_n1000_k10` | 0.038 | 0.379 | 10.04x | 0.038 | 0.379 | **10.03x** |
| `r2_n1000_k10` | 0.008 | 0.447 | 55.95x | 0.008 | 0.447 | **55.86x** |
| `mse_n100000_k2` | 0.452 | 0.645 | 1.43x | 0.452 | 0.645 | **1.43x** |
| `mae_n100000_k2` | 0.466 | 1.588 | 3.41x | 0.466 | 1.295 | **2.78x** |
| `median_ae_n100000_k2`† | 1.967 | 1.781 | 0.91x | 2.045 | 1.781 | **0.87x** |
| `r2_n100000_k2` | 0.759 | 0.991 | 1.31x | 0.759 | 0.991 | **1.31x** |
| `mse_n100000_k10` | 0.455 | 0.628 | 1.38x | 0.454 | 0.628 | **1.38x** |
| `mae_n100000_k10` | 0.458 | 0.673 | 1.47x | 0.458 | 0.672 | **1.47x** |
| `median_ae_n100000_k10`† | 2.142 | 1.796 | 0.84x | 2.241 | 1.795 | **0.80x** |
| `r2_n100000_k10` | 0.743 | 0.950 | 1.28x | 0.743 | 0.950 | **1.28x** |
| `mse_n1000000_k2` | 5.013 | 5.226 | 1.04x | 5.008 | 5.220 | **1.04x** |
| `mae_n1000000_k2` | 5.054 | 5.635 | 1.12x | 5.036 | 5.633 | **1.12x** |
| `median_ae_n1000000_k2`† | 18.365 | 16.375 | 0.89x | 18.708 | 16.360 | **0.87x** |
| `r2_n1000000_k2` | 8.093 | 9.205 | 1.14x | 8.083 | 9.204 | **1.14x** |
| `mse_n1000000_k10` | 4.983 | 4.989 | 1.00x | 4.982 | 4.983 | **1.00x** |
| `mae_n1000000_k10` | 5.040 | 5.712 | 1.13x | 5.035 | 5.711 | **1.13x** |
| `median_ae_n1000000_k10`† | 18.094 | 16.282 | 0.90x | 18.163 | 16.259 | **0.90x** |
| `r2_n1000000_k10` | 7.807 | 9.687 | 1.24x | 7.807 | 9.686 | **1.24x** |

**20/24 at or above 1× on processor time when this table was first
measured — `median_ae` was the finding, not a fluke to rerun away.** All four
`median_ae` rows at n=100 000 and n=1 000 000 landed below the gate —
**0.36×**, **0.25×**, **0.19×** and **0.19×** — meaning Python was 3× to over
5× *faster* there, the only rows on this page where that was true. The cause
was the algorithm, not the run: scikit-learn's `median_absolute_error` calls
NumPy's `median`, which selects via introselect/quickselect in expected
`O(n)`; DataNet's `MedianAbsoluteError` sorted the whole residual array,
which is `O(n log n)`, and the gap widened with `n` exactly as that
complexity difference predicted (0.36× at 100 000 rows, 0.19× at
1 000 000). `mse_n1000000_k10` was the narrowest *passing* row at **1.00×**
— a squared mean over a million rows, near enough to parity that a busier or
quieter machine could tip it either way; every other passing row cleared
1.12×.

**What changed.** `WeightedPercentile`'s unweighted branch (the follow-up
this branch was created for) no longer sorts the whole array: it selects the
one or two order statistics the median needs with a median-of-three
quickselect, falling back to `Array.Sort` on the remaining range once
partitioning has run more than a budget proportional to `log2(n)` — the same
introselect guarantee NumPy's own `median` relies on, so the worst case
stays `O(n log n)` instead of degrading to `O(n²)` on adversarial input. The
weighted branch, which genuinely needs sorted order for its cumulative-weight
walk, was not touched.

**Re-measured under load deliberately comparable to the original run, not a
quieter one.** The four `median_ae` rows above marked † were re-measured
after that rewrite, with the same corpus and harnesses as the rest of this
section. Re-running on an idle machine would have folded "the machine got
quieter" into "the algorithm got faster," and a reader could not have told
the two apart — so the measurement was deliberately taken while the
one-minute load sat in the same 6–10 band as the original run's 8.05 → 6.05,
rather than waiting for a quieter machine. `uptime`'s one-minute average was
**6.62** just before the Python side started (five/fifteen-minute: 15.34 /
14.79) and **6.52** by the time `compare.py` printed the numbers above
(five/fifteen-minute: 7.37 / 9.96).

**The four rows are faster but still below the gate — that is the finding,
not a reason to keep iterating on the algorithm.** In absolute terms
DataNet's own time dropped by roughly 4×–4.8× (7.358 ms → 1.967 ms at
n=100 000, k=2; 88.792 ms → 18.365 ms at n=1 000 000, k=2), and the
processor-time ratio against scikit-learn rose from **0.36×** to **0.87×**
(n=100 000, k=2), **0.25×** to **0.80×** (n=100 000, k=10), **0.19×** to
**0.87×** (n=1 000 000, k=2) and **0.19×** to **0.90×** (n=1 000 000, k=10).
NumPy's introselect and this quickselect now do the same order of work —
`O(n)` expected, `O(n log n)` worst case — so the remaining gap reads as
constant overhead (managed bounds checks, the Lomuto partition's extra
writes, no SIMD-accelerated comparison loop) rather than an algorithmic
difference, and is recorded here as measured rather than chased further.

## Multiclass ROC-AUC, sequential against parallel (issue #86)

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- roc-parallel
```

### Read the axis before reading the table

**The axis here is elapsed time, and processor time rises.** That is what
spending cores means rather than a fault in the measurement, so both columns are
printed side by side for every cell and neither is dropped. Concretely, on
`ovr_n100000_k10` at eight workers: processor time goes from 75.993 ms to
142.224 ms while elapsed time falls from 75.991 ms to 26.648 ms. The work got
larger and the wait got shorter. A caller who is paying for CPU seconds should
read the second column and may well conclude the default is right for them.

**The table immediately above this one is on a different axis, and the two must
not be paired.** Issue #61's comparison against scikit-learn is a *processor
time* gate — every row at or above 1×, which is the property that survives being
run on a busy machine — and it stays one. This table asks a different question of
the same code: how long the caller waits. Setting a processor-time ratio beside
an elapsed-time ratio and reading down the page is the easiest available way to
mislead a reader, so the axes are named at both tables rather than inferred from
the column headers.

**One pair of numbers is on the same axis, and it is the one to watch.** Issue #61's
table reports `roc_auc_ovr_macro_n100000_k10` at **88.385 ms wall** (91.396 ms
processor); the table further down this page reports the same operation at `dop=1`
as **75.991 / 75.779 ms elapsed**. Both are elapsed milliseconds, so the axis
warning just given does not cover them — and they are still not the same
measurement. Three things differ, and all three matter:

- **Different input.** Issue #61 scores the committed corpus file
  `bench/corpus/metrics/metrics_n100000_k10.json`; this bench generates a seeded
  separable problem in process (`Random(86)`, rows normalised). How separable the
  problem is decides how many equal scores `BinaryRoc` groups into one threshold,
  which changes the work per curve.
- **Different machine load.** Issue #61's pass was taken at a one-minute load of
  1.52. These figures were taken between 2.31 and 4.01 — the Conditions section
  below gives the three readings.
- **Different code on the sequential path.** The sequential multiclass drivers were
  rewritten for this issue: the per-curve buffers now come from one pooled
  `BinaryRoc.Scratch` reused across every class, where Issue #61's build allocated
  two fresh arrays per class inside `BinaryRoc.Score`. Issue #61's 88.385 ms and
  this table's 75.991 ms were not produced by the same implementation, and nothing
  here measures how much of the gap that accounts for.

So the arithmetic a reader is most likely to reach for is the one nobody should
publish: dividing this table's best `ovr_n100000_k10` cell (26.648 ms elapsed) into
Issue #61's Python column for that row (250.400 ms wall) yields a tidy-looking
"9.4× faster than scikit-learn" that no single run measured — two windows, two
inputs, two builds. A cross-language figure for the parallel path would have to be
measured as one pass with both sides on the same input, and it has not been.

### Conditions

Intel i7-4770S, **4 physical cores / 8 logical threads**, Ubuntu 24.04,
.NET 10, one sitting. Inputs generated in-process from a fixed seed (`Random(86)`,
rows normalised to sum to 1) — no shared corpus, because both sides of this
comparison are C#. Each cell is the best of five repeats over a 0.5 s minimum,
with wall and processor time taken from the same repeat.

**Two back-to-back passes over all 24 cells, both published.** One-minute load
average, from `uptime`: **2.31 before the first pass, 3.32 between them, 4.01
after the second.** The rise across the window is the runs themselves — this
workstation does not reach that on its own — and it is why both passes are shown
instead of a mean, following `bench/README.md`'s "read the pairs, not the means"
rule. The two passes are unusually close here: the sequential baselines of the
two heaviest cells agree to 0.28% and 0.45%, against the up-to-15% dispersion
that same section documents for this machine. The widest disagreement anywhere in
the 24 pairs is 6.45%, on `ovo_n1000_k10` at four workers, where the whole cell
is under 0.3 ms.

Because the load climbed from 2.31 to 4.01 while these figures were taken, the
table is comparable **to itself** — one window, all four worker counts of a shape
measured next to each other — and **not** to the scikit-learn table above, which
was taken at a one-minute load of 1.52. Those absolute milliseconds and these
were not measured on the same machine state.

### The 24 cells

Elapsed and processor milliseconds per operation, as `pass 1 / pass 2`. The
`cpu ÷ elapsed` column is what the harness prints as `(…x cores)` — how many
cores' worth of work the call consumed. The speed-up column is elapsed time
divided into the same pass's own `dop=1` row.

| Operation | dop | elapsed ms | processor ms | cpu ÷ elapsed | speed-up vs dop=1 |
| --- | ---: | ---: | ---: | ---: | ---: |
| `ovr_n1000_k10` | 1 | 0.466 / 0.470 | 0.467 / 0.470 | 1.00 / 1.00 | 1.00 / 1.00 |
| `ovr_n1000_k10` | 2 | 0.294 / 0.301 | 0.647 / 0.672 | 2.20 / 2.23 | 1.59 / 1.56 |
| `ovr_n1000_k10` | 4 | **0.240 / 0.238** | 0.958 / 0.944 | 3.98 / 3.96 | **1.94 / 1.97** |
| `ovr_n1000_k10` | 8 | 0.249 / 0.244 | 1.066 / 1.036 | 4.28 / 4.24 | 1.87 / 1.93 |
| `ovo_n1000_k10` | 1 | 0.745 / 0.741 | 0.745 / 0.741 | 1.00 / 1.00 | 1.00 / 1.00 |
| `ovo_n1000_k10` | 2 | 0.438 / 0.439 | 0.957 / 0.943 | 2.19 / 2.15 | 1.70 / 1.69 |
| `ovo_n1000_k10` | 4 | **0.297 / 0.279** | 1.215 / 1.169 | 4.09 / 4.19 | **2.51 / 2.66** |
| `ovo_n1000_k10` | 8 | 0.423 / 0.421 | 1.627 / 1.603 | 3.84 / 3.81 | 1.76 / 1.76 |
| `ovr_n100000_k5` | 1 | 36.770 / 36.633 | 36.770 / 36.635 | 1.00 / 1.00 | 1.00 / 1.00 |
| `ovr_n100000_k5` | 2 | 23.535 / 23.444 | 38.852 / 38.905 | 1.65 / 1.66 | 1.56 / 1.56 |
| `ovr_n100000_k5` | 4 | 17.777 / 17.656 | 45.709 / 46.358 | 2.57 / 2.63 | 2.07 / 2.07 |
| `ovr_n100000_k5` | 8 | **14.380 / 14.405** | 56.961 / 57.354 | 3.96 / 3.98 | **2.56 / 2.54** |
| `ovo_n100000_k5` | 1 | 55.121 / 54.865 | 55.117 / 54.860 | 1.00 / 1.00 | 1.00 / 1.00 |
| `ovo_n100000_k5` | 2 | 29.305 / 29.674 | 56.743 / 57.214 | 1.94 / 1.93 | 1.88 / 1.85 |
| `ovo_n100000_k5` | 4 | 24.177 / 24.151 | 60.830 / 60.723 | 2.52 / 2.51 | 2.28 / 2.27 |
| `ovo_n100000_k5` | 8 | **17.331 / 17.535** | 91.430 / 91.555 | 5.28 / 5.22 | **3.18 / 3.13** |
| `ovr_n100000_k10` | 1 | 75.991 / 75.779 | 75.993 / 75.785 | 1.00 / 1.00 | 1.00 / 1.00 |
| `ovr_n100000_k10` | 2 | 40.242 / 40.163 | 77.532 / 77.425 | 1.93 / 1.93 | 1.89 / 1.89 |
| `ovr_n100000_k10` | 4 | 34.997 / 35.644 | 94.273 / 91.616 | 2.69 / 2.57 | 2.17 / 2.13 |
| `ovr_n100000_k10` | 8 | **26.648 / 26.379** | 142.224 / 140.712 | 5.34 / 5.33 | **2.85 / 2.87** |
| `ovo_n100000_k10` | 1 | 127.375 / 126.810 | 127.377 / 126.801 | 1.00 / 1.00 | 1.00 / 1.00 |
| `ovo_n100000_k10` | 2 | 64.317 / 64.250 | 123.433 / 123.341 | 1.92 / 1.92 | 1.98 / 1.97 |
| `ovo_n100000_k10` | 4 | **37.162 / 36.875** | 133.183 / 131.681 | 3.58 / 3.57 | **3.43 / 3.44** |
| `ovo_n100000_k10` | 8 | 37.284 / 37.457 | 187.662 / 184.325 | 5.03 / 4.92 | 3.42 / 3.39 |

Bold marks the fastest worker count for each shape in elapsed time. Nothing was
skipped: one-vs-one at n=100 000, k=10 is 45 pairs and 90 curves, the heaviest
cell in the matrix, and a single sequential call is about 127 ms — well inside the
bench's 60-second patience for that cell.

### What the numbers say

**At k=10 and n=100 000 the opt-in is worth 2.85×–2.87× on one-vs-rest and
3.43×–3.44× on one-vs-one**, on four physical cores. One-vs-rest wants all eight
logical threads to get there (26.6 / 26.4 ms); one-vs-one gets there at four
(37.2 / 36.9 ms) and eight buys it nothing.

**At k=5 the ceiling is lower, and part of it is arithmetic.** One-vs-rest tops
out at 2.56× / 2.54×: five classes are five independent units of work, so the
per-index loop is clamped to five workers however many the caller asks for, and
five pieces cannot be spread evenly over four cores — one core does two while
three do one. One-vs-one at the same shape has ten pairs to hand out and reaches
3.18× / 3.13×, the best ratio in the table at n=100 000.

**At n=1000 the opt-in is a gain, not a cost — which is not what was expected.**
The design brief assumed the small-input row would be a dispatch overhead to
justify. It is a doubling: one-vs-rest 0.466 / 0.470 ms → 0.240 / 0.238 at four
workers (1.94× / 1.97×), one-vs-one 0.745 / 0.741 → 0.297 / 0.279 (2.51× /
2.66×). Ten classes are ten independent sorts even when each sorts only a
thousand values, and the copy the parallel path pays for is 1000 × 10 doubles —
80 KB, which fits in L2. This is why the option has no internal size threshold:
a crossover constant calibrated for "small inputs" would have thrown this away.

**`dop=8` loses to `dop=4` on three of the six shapes, in both passes.**
`ovr_n1000_k10` (0.249 / 0.244 against 0.240 / 0.238), `ovo_n1000_k10` (0.423 /
0.421 against 0.297 / 0.279 — 42% / 51% slower) and `ovo_n100000_k10` (37.284 /
37.457 against 37.162 / 36.875). This machine has **4 physical cores and 8
logical threads**: past four workers the extra ones share execution units with a
sibling rather than adding any, while still adding scheduling and cache
pressure. The `ovo_n1000_k10` row is the clearest case, and it is a large
regression, not a rounding error.

The practical consequence is worth stating plainly, because
`MaxDegreeOfParallelism`'s own documentation offers `Environment.ProcessorCount`
as the way to ask for every core: **on a hyperthreaded machine that property can
be the wrong number, and slower than half of it.** `ProcessorCount` is 8 here.
Four is the better setting on half these shapes and never much worse on the rest.
There is no recommended value in the library because there is no value that is
right on every machine — measure your shape on your hardware, which is what this
bench mode exists for.

**Where the ceiling comes from.** Nothing here approaches 4× on four cores, and
three things account for it, in decreasing order of confidence. `ValidateRowSums`
walks all `samples × classes` scores on the calling thread before any dispatch —
it stays sequential so its message can name the *first* bad row — and no worker
count shortens it. The parallel path copies its inputs, `samples × classes × 8`
bytes for the transposed score matrix, about 8 MB at n=100 000, k=10. And the
per-index loop can only be as parallel as it has indices, which is what caps
one-vs-rest at k=5. This run does not time those three separately, so the split
between them is not quantified here; what is measured is the total, and it is in
the table.

The reasoning behind the opt-in default, the absent `-1` sentinel and the absent
threshold is in
[`../decisions/0018-multiclass-roc-auc-parallelism-is-opt-in.md`](../decisions/0018-multiclass-roc-auc-parallelism-is-opt-in.md).
