# Benchmarks

Three complementary tools.

## 1. Intra-C# micro-benchmarks (BenchmarkDotNet)

Rigorous per-method measurement, for optimizing the C# implementation itself:

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*Levenshtein*'
```

## 2. net10 vs netstandard2.0 — what the broad-reach target costs

`netstandard2.0` is a contract, not a runtime: nothing executes *on* it. What can
be measured is the **netstandard2.0-compiled assembly against the net10-compiled
one, both hosted on .NET 10** — same JIT, same GC, so any difference comes from
the libraries' own conditional code paths.

`DataNet.NetStandard.Benchmarks` links the *same* benchmark sources as the suite
above (`<Compile Include="../DataNet.Text.Benchmarks/…" />`, never a copy) and
only swaps which assemblies it references.

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks        -- --filter '*VectorMath*'
dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter '*VectorMath*'
```

### Measured

`VectorMath.Dot`, Intel i7-4770S, .NET 10.0.110:

| Dimension | net10 | netstandard2.0 | cost |
| --- | --- | --- | --- |
| 384 | 73.2 ns | 338.5 ns | 4.6× |
| 768 | 130.9 ns | 679.8 ns | 5.2× |
| 1024 | 163.6 ns | 912.1 ns | 5.6× |

That is the `Vector<T>` SIMD path against the scalar fallback, which is the one
place the two builds deliberately differ — the span-based `Vector<T>` constructor
is net-only. Everything else compiles to equivalent IL, so a difference elsewhere
means something changed and is worth investigating.

### Two things keep this honest

`SetTargetFramework` on the `ProjectReference` is **not sufficient on its own**.
BenchmarkDotNet's default toolchain generates and builds its own project per run,
which re-resolves the reference and restores the net10 build — both suites then
measure the same assemblies while reporting plausible numbers. An earlier version
of this comparison showed a 4% difference for exactly that reason.

So the suite pins the in-process toolchain, removing the generated project, and
asserts what it actually loaded before running anything:

```text
// DataNet.Text: .NETStandard,Version=v2.0
// DataNet.Embeddings: .NETStandard,Version=v2.0
```

A mismatch exits non-zero rather than producing numbers. Sanity-check any result
against physics too: a scalar dot product over 1024 floats is latency-bound near
1000 ns, so a result close to the SIMD figure means the isolation broke again.

## 3. Cross-language comparison vs Python

Same committed corpus (`bench/corpus/pairs.json`), same throughput metric
(ns/pair), same auto-scaling best-of-N methodology on both sides — so the numbers
are actually comparable.

```bash
# Python side (rapidfuzz)
. .venv-oracles/bin/activate      # needs: pip install -r tools/requirements.txt
python bench/python/bench_levenshtein.py

# C# side (DataNet.Text) — matched Stopwatch harness, not BenchmarkDotNet
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- compare
#   add --codepoint to measure the code-point mode instead of UTF-16

# side-by-side table
python bench/compare.py
```

Results land in `bench/results/` (git-ignored: they are machine-specific and not
authoritative). The corpus is ASCII, so UTF-16 units and code points coincide and
both sides compute identical distances.

### Reading the numbers

The comparison is deliberately honest about methodology: the Python side times the
**realistic per-call loop** a user writes. rapidfuzz also exposes batch APIs
(`process.cdist`) that amortise the Python→C boundary; those are faster than the
loop measured here.

Current headline (see `docs/guides/performance.md` for a captured table): C# wins
on short strings (no interpreter overhead), but rapidfuzz's **bit-parallel Myers**
core scales far better on long strings than our naive O(nm) DP. Closing that gap
is tracked in `docs/decisions/0004-levenshtein-myers-backlog.md`.

## 4. The persistence layer (issue #58)

Three vocabulary loaders and the TF-IDF round trip, on the same three axes as
above, plus one the other sections do not need: **processor time**.

The corpus is generated rather than committed — about 3 MB of vocabulary files:

```bash
python bench/corpus/generate_vocabs.py     # writes bench/corpus/vocabs/, git-ignored
```

Both language sides read those same files, which is what makes the comparison
mean anything; the bytes are not reproducible across machines and do not need to
be. A harness that cannot find the corpus exits non-zero naming the generator
rather than reporting numbers.

### net10 vs netstandard2.0

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks        -- --filter '*Persistence*'
dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter '*Persistence*'
```

Intel i7-4770S, .NET 10.0.110. Loaders parse from memory here, so the figures are
parsing cost rather than disk latency.

| Operation | net10 | netstandard2.0 | net10 alloc | ns2.0 alloc |
| --- | --- | --- | --- | --- |
| `VocabTxt` | 4.201 ms | 4.238 ms | 4.25 MB | 4.25 MB |
| `TokenizerJsonWordPiece` | 11.780 ms | 12.189 ms | 7.24 MB | 7.25 MB |
| `TokenizerJsonUnigram` | 16.130 ms | 17.001 ms | 9.64 MB | 9.64 MB |
| `SpieceModel` | 4.369 ms | **10.505 ms** | 4.61 MB | **6.56 MB** |
| `TfidfSave` | 1.781 ms | 1.915 ms | 2.09 MB | 2.09 MB |
| `TfidfLoad` | 5.033 ms | 5.349 ms | 4.34 MB | 4.34 MB |

One caveat on the `TfidfSave` allocation figure: the benchmark pre-sizes its
destination `MemoryStream` to the artifact's exact final length, which a caller
writing to `new MemoryStream()` cannot do. That removes the buffer-doubling
garbage a realistic save would pay, so the number is friendlier than the typical
case. It is deliberate — the question on this axis is what the two *targets* cost
each other, not what a caller pays — but it is not a general "cost of Save".

Five rows sit inside their own Error/StdDev — noise, which is what equivalent IL
should produce. `SpieceModel` is the exception and it is real: netstandard2.0
allocates twice per piece where net10 allocates nothing, once for the `byte[4]`
scratch buffer in `ProtobufReader.ReadFloat` and once for the array copy in
`DecodeUtf8` that `netstandard2.0` needs because it has no span overload. Across
29 861 pieces that is the whole 1.95 MB difference, and it costs 2.4× the time.

### vs Python

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- compare-persistence
python bench/python/bench_persistence.py
python bench/compare.py persistence
```

DataNet on .NET 10.0.10 against Python 3.12.3 with `tokenizers` 0.23.1,
`sentencepiece` 0.2.2 and `scikit-learn` 1.9.0. Ratios above 1 mean DataNet is
faster.

| Operation | DataNet | Python | wall | DataNet cpu | Python cpu | **cpu** |
| --- | --- | --- | --- | --- | --- | --- |
| `spiece_model` | 4.604 ms | 40.049 ms | 8.70× | 5.496 ms | 40.049 ms | **7.29×** |
| `tokenizer_json_unigram` | 17.265 ms | 65.289 ms | 3.78× | 19.106 ms | 65.288 ms | **3.42×** |
| `vocab_txt` | 4.165 ms | 16.860 ms | 4.05× | 5.230 ms | 16.859 ms | **3.22×** |
| `tokenizer_json_wordpiece` | 11.417 ms | 23.782 ms | 2.08× | 14.375 ms | 23.781 ms | **1.65×** |
| `tfidf_save` | 2.028 ms | 4.734 ms | 2.33× | 2.343 ms | 4.734 ms | **2.02×** |
| `tfidf_load` | 5.305 ms | 5.043 ms | 0.95× | 6.568 ms | 5.028 ms | **0.77×** |

Both sides were run back to back on an otherwise idle machine, and the pair above
comes from one such run rather than the best figure of each operation across
several — picking per-row winners from different runs would flatter whichever
side happened to be measured last. Run-to-run spread on this harness is a few
percent, so treat differences smaller than that as noise; the loader ratios are
far larger than it, and `tfidf_load` sits inside it on the wall column.

### Why processor time is reported too

Elapsed time alone flatters this runtime. .NET's background collector does its
work on other threads, so an allocation-heavy operation finishes in less elapsed
time than it costs: every DataNet row above burns 1.12–1.21 processor-seconds per
elapsed second, while CPython is strictly single-threaded and measures 1.00 on
every row. Reading only the wall column would report a parity on `tfidf_load`
that disappears the moment two models load at once.

`tfidf_load` is the one row where Python still wins, by 22% of processor time for
the same elapsed time. That residue is background collection, which is a property
of the host runtime rather than something a library chooses — an application that
wants it gone sets `ConcurrentGarbageCollection=false` in its own runtimeconfig.

### Reading the numbers

The two sides do not do the same amount of work on the loader rows, and the
comparison says so rather than hiding it. `tokenizers` and `sentencepiece` build
a whole tokenizer: the normalizer and pre-tokenizer graph, and the Rust or C++
matcher they will encode with. DataNet's loaders build a validated dictionary and
stop — the guides tell readers to construct a tokenizer from it as a second step.
A margin in DataNet's favour therefore reflects, in part, work it does not do.

Both are also native code behind a thin binding, not interpreted Python.

The `tfidf_save` / `tfidf_load` pair is the one comparison that is close to like
for like, and it is the one that moved most. It started at 0.60× and 0.44× — both
losses — and reached the table above through five changes, each of which was kept
only because it measured: shortest-round-trippable doubles, the idf vector as
base64, the relaxed JSON encoder, folding the ordering check into the read loop,
and sizing the vocabulary buffer from the declared count. Two further changes
were measured and **discarded** for showing no gain: disabling writer validation,
and an earlier version of that last buffer change, which paid nothing until the
idf vector stopped dominating the profile. The reasoning is in
[`docs/decisions/0011`](../docs/decisions/0011-persistence-format.md).

## 5. Classification metrics (issue #61)

`ConfusionMatrix`, `Accuracy`, `Precision`/`Recall`/`F1`, `ClassificationReport`
and `RocAuc`, against scikit-learn's equivalents, on the same three axes as
persistence — wall time, processor time, and net10 vs netstandard2.0 — plus a
fourth this branch adds: **processor time is the merge gate**, not a footnote.
Every row in the cross-language table below must be ≥ 1×, or the branch does
not merge.

The corpus is generated rather than committed, like `bench/corpus/vocabs/` —
six JSON files, about 54 MB total:

```bash
python bench/corpus/generate_metrics.py     # writes bench/corpus/metrics/, git-ignored
```

Six shapes — (1 000, 2), (1 000, 10), (100 000, 2), (100 000, 10),
(1 000 000, 2), (1 000 000, 10) samples × classes — each with `y_true`,
`y_pred`, a sample-weight column (generated but unused by the six benchmarked
operations, on both sides), and scores for the ROC-AUC rows. The 10-class score
matrix stops at 100 000 rows: a million rows by ten classes is 200 MB of JSON,
which would measure the parser rather than the metric.

### Intra-C#, and net10 vs netstandard2.0

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks        -- --filter '*MetricsBenchmarks*' --job short
dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter '*MetricsBenchmarks*' --job short
```

**`--job short` (3 iterations, 3 warmup), not the default job used elsewhere in
this file.** The full parameter matrix — 3 sizes × 2 class counts × 5 methods =
30 benchmarks — at default statistical precision runs to the better part of an
hour across both targets; short jobs answer the questions these two tiers
actually ask (the shape of per-metric cost across sizes, and what the
netstandard2.0 build costs against net10) without that cost. Do not compare
these figures against the higher-precision tables elsewhere in this file — a
short job's confidence interval is wide, sometimes most of the mean itself at
the smallest size (`AccuracyScore` at n=1 000 carries a ±77% margin in the raw
BenchmarkDotNet output).

**The two commands above do not produce the same row count.** The first runs
30 benchmarks — `--job short` is the only job configured for that project. The
second runs **60**: `DataNet.NetStandard.Benchmarks/Program.cs` already
configures its own job (`Job.Default.WithToolchain(InProcessEmitToolchain.Instance)`,
needed for the in-process isolation this tier depends on), and BenchmarkDotNet's
CLI `--job short` *adds* a second job to that list rather than replacing it. Of
the 60 rows the second command produces, only the 30 labelled `ShortRun` are
comparable to the first command's output — those are what the table below uses.
A reader who copies both commands and reads all 60 rows from the second will be
looking at two different jobs' worth of numbers without a column that says so.

Same isolation discipline as the VectorMath/BatchEmbedding comparisons above:
the in-process toolchain, and an assertion that the netstandard2.0 run actually
loaded the netstandard2.0 build before trusting any number from it.

```text
// DataNet.Metrics: .NETStandard,Version=v2.0
```

Intel i7-4770S, .NET 10.0.10. `Matrix` — the base op every other benchmark's
average is close to, `MatrixWeighted`/`AccuracyScore`/`F1Macro`/`Report`
excepted at the smallest size where noise dominates — at all six shapes:

| Samples | Classes | net10 (short) | netstandard2.0 (short) | ratio |
| ---: | ---: | ---: | ---: | ---: |
| 1 000 | 2 | 7.460 µs | 7.334 µs | 0.98× |
| 1 000 | 10 | 7.392 µs | 7.578 µs | 1.03× |
| 100 000 | 2 | 841.4 µs | 841.5 µs | 1.00× |
| 100 000 | 10 | 922.4 µs | 957.7 µs | 1.04× |
| 1 000 000 | 2 | 8.710 ms | 8.832 ms | 1.01× |
| 1 000 000 | 10 | 9.566 ms | 9.566 ms | 1.00× |

`MatrixWeighted`, `AccuracyScore`, `F1Macro` and `Report` land within the same
few percent of parity at n=100 000 and n=1 000 000 (0.95×–1.06× across all 24
remaining rows), with one exception: `AccuracyScore` at n=1 000, k=10 reads
2.59× — net10 878 ns against netstandard2.0 2 276 ns — which is exactly the
short-job noise floor at work, not a real regression: both figures sit inside
a confidence interval wider than the mean itself at that size, and every
larger-n row for the same method agrees to within 2%.

This is **not** the `VectorMath.Dot` story from section 2. `DataNet.Metrics`
has no `Vector<T>` SIMD path or other target-conditional code — every
benchmark here is a scalar loop over `int[]`/`double[]` — so net10 and
netstandard2.0 compile to equivalent IL and land at parity, not the 4.6×–5.6×
gap that section documents. The near-1.00× ratios above are the expected
result, not a surprise.

### vs Python

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- compare-metrics
. .venv-oracles/bin/activate && python bench/python/bench_metrics.py
python bench/compare.py metrics
```

DataNet on .NET 10.0.10 against scikit-learn 1.9.0 / NumPy 2.5.1 on Python
3.12.3. Ratios above 1 mean DataNet is faster.

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

**Merge gate: 29/29 operations at or above 1× on processor time.** The
narrowest margin is 2.80× (`roc_auc_ovr_macro` at n=100 000, k=10). The design
brief named `roc_auc_binary` at a million samples — where the cost is a sort —
as the likely candidate for falling below 1×, with a radix pass over the
`double` bit patterns as the fallback if it did. It came in at 3.98×; no row
needed that change on this branch.

### Measurement conditions

Both files were produced back to back, in the order shown, after discarding
one earlier pairing: a first C# run completed at 17:01 while the one-minute
load average was still 1.8–2.3 but the five- and fifteen-minute figures
(4.7–8.8) showed the machine still shedding the tail of a preceding full test
suite. That run is not in the table above. The pair that is:

| Side | Written | Load (1 / 5 / 15 min) |
| --- | --- | --- |
| Python (`python-metrics.json`) | 17:05:10 | 1.92 / 3.51 / 7.58 |
| C# (`csharp-metrics.json`) | 17:09:29 | 2.29 / 3.43 / 7.42 (start) |

The machine carries a permanent 30–40% background load from the desktop
client, an editor and a browser; a one-minute average of 1.9–2.3 is this
workstation's floor, not a transient to wait out further.

### Why processor time barely differs from wall time here

Unlike the persistence comparison in section 4 — where DataNet burns 1.12–1.21
processor-seconds per elapsed second, so the wall and cpu columns diverge and
`tfidf_load` even flips winner between them — the two columns above agree to
within about 1% on every row (up to 3.5% on the single heaviest row,
`roc_auc_ovr_macro` at n=100 000). These operations allocate little enough
(a few kilobytes at most; see `MetricsBenchmarks`'s `[MemoryDiagnoser]` output)
that .NET's background collector never gets involved, so there is no gap
between the two columns for the cpu one to correct.

### Reading the numbers

The rows at n=1 000 (70×–620×) are not telling you DataNet is two orders of
magnitude faster at the arithmetic — a confusion matrix over 1 000 samples is
sub-microsecond work either way. They are measuring CPython's per-call
interpreter overhead, which the auto-scaling loop cannot amortise away because
each `skm.*` call re-enters the interpreter regardless of how little work is
inside it. The rows that carry the argument about the underlying computation
are the ones at n=100 000 and n=1 000 000, where the ratios settle to a still
decisive but far more modest 2.8×–43×.

None of the six operations passes `sample_weight` on either side — the corpus
carries that column, but the brief's own six calls do not use it, so this
comparison does not exercise `ConfusionMatrix`'s weighted path at all; see
`MetricsBenchmarks.MatrixWeighted` in the intra-C# tier above for that.

`precision_recall_f1_macro` is the one operation where the two sides do not do
quite the same amount of work, though both compute the same three numbers over
the same matrix. scikit-learn's `precision_recall_fscore_support` builds the
per-class true-positive/predicted/support sums once and reads precision,
recall and F1 off that single pass. The DataNet side builds the confusion
matrix once too, but `Precision.Score`/`Recall.Score`/`F1.Score` each walk
those sums again independently — three redundant `O(classes)` passes instead
of Python's one. At 2 or 10 classes that redundancy is a handful of additions,
several orders of magnitude below the `O(samples)` matrix construction both
sides pay first, so it is invisible in the ratios above; it is called out here
because it is real, not because it matters at this scale. `classification_report`
carries the same pattern more deeply (each of its per-class arrays, and each of
its macro/weighted averages, re-derives those sums again) for the same reason
and to the same effect: negligible at these class counts.

Everything else — `confusion_matrix`, `accuracy`, `roc_auc_binary`,
`roc_auc_ovr_macro` — is like for like: same algorithm (`BinaryRoc` mirrors
scikit-learn's `_binary_clf_curve` sort-and-accumulate exactly, and
`MultiClassRoc`'s one-vs-rest reduction is scikit-learn's own), same data,
parsed once outside the timed loop on both sides so neither pays JSON-parsing
cost inside the measurement.
