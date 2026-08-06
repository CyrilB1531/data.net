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
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*BatchEmbedding*'
```

**Read this before quoting the ratio.** The model is `tiny_embedder.onnx`: one
Gather node over a 64 × 4 table, because weights are never committed
(`CONTRIBUTING.md`) and a real encoder is a hundred megabytes. Its arithmetic is
free. So what is measured is the per-sequence cost that batching removes — graph
dispatch, thread-pool wake-up, tensor wrapping — and none of the matrix
multiplication a real encoder adds to *both* sides. This is an upper bound on the
speed-up, not the speed-up.

Full job, `[MemoryDiagnoser]`, Intel Core i7-4770S (Haswell, 4 physical cores),
Ubuntu 24.04, .NET 10.0.10, X64 RyuJIT AVX2. Corpus of 1 to 61 words per text,
sub-batch 8. `UnitLoop` is the baseline — one `Embed` call per text, which is
what the guide's three lines amounted to before `EmbedBatch` existed.

| Texts | `UnitLoop` | `EmbedBatch` | ratio | `EmbedBatchBucketed` | ratio | allocated vs baseline |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 17.8 µs | 14.2 µs | 0.80 | 14.8 µs | 0.83 | 1.11 |
| 8 | 220 µs | 114 µs | 0.52 | 117 µs | 0.53 | 0.94 |
| 32 | 795 µs | 388 µs | 0.49 | 372 µs | 0.47 | 0.93 / 0.91 |
| 128 | 3 135 µs | 1 582 µs | 0.50 | 1 516 µs | 0.48 | 0.93 / 0.90 |

**Batching halves the wall clock** from 8 texts upward and stays there: the
per-call overhead is amortized over the whole sub-batch. At a single text there
is nothing to amortize, and the batch path is marginally cheaper only because the
caller no longer builds a mask by hand.

**Bucketing is a different story, and the honest answer is smaller.** It engages
only when the corpus spans more than one sub-batch, so the rows at 1 and 8 above
run the *identical* code in both columns — they are the control, and what they
differ by, 2–4 %, is this harness's noise floor. The 4 % wall-clock gain at 32
and 128 does not clear it. What does clear it is the allocation column, which is
counted rather than sampled: 1 766 KB → 1 699 KB at 128 texts. That is padding
genuinely not written. On a model doing real work that padding would be matrix
multiplication not performed, and the time would follow; this model cannot show
it, so the claim stops here.

**The two builds.** The same benchmark against the `netstandard2.0` assemblies —
scalar pooling, since `Vector<T>` has no span constructor there — runs 3–5 %
behind: 405 µs against 388 at 32 texts, 1 635 against 1 582 at 128. That is what
the broad-reach target costs on this path, measured rather than assumed.

```bash
dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter '*BatchEmbedding*'
```

## Classification metrics (issue #61) — vs scikit-learn

```bash
python bench/corpus/generate_metrics.py           # writes bench/corpus/metrics/, git-ignored
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- compare-metrics
. .venv-oracles/bin/activate && python bench/python/bench_metrics.py
python bench/compare.py metrics
```

Six operations — `confusion_matrix`, `accuracy`, `precision_recall_f1_macro`,
`classification_report`, `roc_auc_binary`, `roc_auc_ovr_macro` — over six shapes
(1 000 / 100 000 / 1 000 000 samples, 2 or 10 classes), on the same corpus files
on both sides. **This is the merge gate for the branch, on processor time**: every
row must be ≥ 1×, and it is.

DataNet.Metrics on .NET 10.0.10 against scikit-learn 1.9.0 / NumPy 2.5.1 on
Python 3.12.3, Intel i7-4770S. Both sides measured back to back on an otherwise
idle machine (load average 1.9–2.3 at the one-minute mark — this workstation's
floor, a permanent ~30–40 % background from the desktop client, an editor and a
browser).

| Operation | DataNet ms | Python ms | wall | DataNet cpu ms | Python cpu ms | **cpu** |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `confusion_matrix_n1000_k2` | 0.009 | 1.001 | 117.41x | 0.009 | 1.000 | **117.41x** |
| `accuracy_n1000_k2` | 0.001 | 0.520 | 585.69x | 0.001 | 0.520 | **585.70x** |
| `precision_recall_f1_macro_n1000_k2` | 0.008 | 1.739 | 213.89x | 0.008 | 1.734 | **213.74x** |
| `classification_report_n1000_k2` | 0.011 | 6.492 | 593.20x | 0.011 | 6.490 | **594.87x** |
| `roc_auc_binary_n1000_k2` | 0.028 | 1.961 | 70.24x | 0.028 | 1.961 | **70.24x** |
| `confusion_matrix_n1000_k10` | 0.009 | 1.037 | 119.39x | 0.009 | 1.035 | **119.18x** |
| `accuracy_n1000_k10` | 0.001 | 0.538 | 622.80x | 0.001 | 0.538 | **622.82x** |
| `precision_recall_f1_macro_n1000_k10` | 0.009 | 1.840 | 194.98x | 0.009 | 1.837 | **194.68x** |
| `classification_report_n1000_k10` | 0.016 | 7.132 | 445.01x | 0.016 | 7.132 | **445.75x** |
| `roc_auc_ovr_macro_n1000_k10` | 0.546 | 10.673 | 19.55x | 0.546 | 10.673 | **19.56x** |
| `confusion_matrix_n100000_k2` | 0.949 | 16.330 | 17.20x | 0.949 | 16.329 | **17.21x** |
| `accuracy_n100000_k2` | 0.189 | 5.530 | 29.29x | 0.189 | 5.529 | **29.29x** |
| `precision_recall_f1_macro_n100000_k2` | 0.836 | 17.742 | 21.23x | 0.836 | 17.742 | **21.23x** |
| `classification_report_n100000_k2` | 0.840 | 36.103 | 43.00x | 0.839 | 36.100 | **43.00x** |
| `roc_auc_binary_n100000_k2` | 7.856 | 33.973 | 4.32x | 7.956 | 33.967 | **4.27x** |
| `confusion_matrix_n100000_k10` | 1.064 | 16.208 | 15.23x | 1.064 | 16.200 | **15.22x** |
| `accuracy_n100000_k10` | 0.291 | 5.634 | 19.35x | 0.291 | 5.632 | **19.35x** |
| `precision_recall_f1_macro_n100000_k10` | 0.972 | 18.754 | 19.30x | 0.972 | 18.754 | **19.30x** |
| `classification_report_n100000_k10` | 0.951 | 40.807 | 42.90x | 0.951 | 40.804 | **42.90x** |
| `roc_auc_ovr_macro_n100000_k10` | 86.067 | 249.623 | 2.90x | 88.869 | 249.134 | **2.80x** |
| `confusion_matrix_n1000000_k2` | 8.486 | 154.095 | 18.16x | 8.485 | 153.985 | **18.15x** |
| `accuracy_n1000000_k2` | 2.006 | 50.824 | 25.34x | 2.006 | 50.808 | **25.33x** |
| `precision_recall_f1_macro_n1000000_k2` | 8.538 | 161.865 | 18.96x | 8.540 | 161.709 | **18.94x** |
| `classification_report_n1000000_k2` | 8.485 | 307.363 | 36.22x | 8.469 | 307.345 | **36.29x** |
| `roc_auc_binary_n1000000_k2` | 94.338 | 377.152 | 4.00x | 94.841 | 377.043 | **3.98x** |
| `confusion_matrix_n1000000_k10` | 9.621 | 152.707 | 15.87x | 9.620 | 152.700 | **15.87x** |
| `accuracy_n1000000_k10` | 3.069 | 50.999 | 16.62x | 3.068 | 50.903 | **16.59x** |
| `precision_recall_f1_macro_n1000000_k10` | 9.870 | 170.950 | 17.32x | 9.854 | 170.931 | **17.35x** |
| `classification_report_n1000000_k10` | 9.636 | 342.540 | 35.55x | 9.633 | 342.267 | **35.53x** |

**Gate result: 29/29 operations at or above 1× on processor time.** The
narrowest margin is **2.80×**, on `roc_auc_ovr_macro` at n=100 000, k=10 — the
row the design brief flagged as the one most likely to need a radix-sort
rewrite of `BinaryRoc`. It did not: even the heaviest sort-bound row clears the
gate by a comfortable margin, so no algorithmic change was needed on this
branch.

**Read this before quoting a single ratio.** The rows at n=1 000 (70×–620×) are
dominated by CPython's per-call interpreter overhead, not by the computation —
a confusion matrix over 1 000 samples is sub-microsecond work on either side.
The rows that carry the argument are the ones at n=100 000 and n=1 000 000,
where the ratios settle to a more modest but still decisive 2.8×–43×.

Unlike the persistence comparison, wall and processor time agree here to
within about 1% on every row (up to 3.5% on the single heaviest-cpu row): these
metrics allocate little enough per call that .NET's background collector is
never a factor, so there is no gap between the two columns to explain away.

Full breakdown, including the intra-C# and net10-vs-netstandard2.0 tiers and
where the two language sides do not do identical work, in
[`bench/README.md`](../../bench/README.md#5-classification-metrics-issue-61).
