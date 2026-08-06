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
