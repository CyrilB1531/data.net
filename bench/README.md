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
dotnet run -c Release --project bench/DataNet.Text.Benchmarks        -- --filter '*VectorMath*' --inProcess
dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter '*VectorMath*'
```

### Measured

Intel i7-4770S, .NET 10.0.10, default job. **Two runs per side, and both are
shown**, interleaved net10 → netstandard2.0 → net10 → netstandard2.0 so that any
drift in machine state lands on both columns rather than on whichever one
occupies the second half of the window.

| Method | Dim | net10 | netstandard2.0 | cost |
| --- | --- | --- | --- | --- |
| `Dot` | 384 | 74.8 / 79.3 ns | 330.3 / 334.0 ns | 4.2×–4.4× |
| `Dot` | 768 | 122.9 / 128.7 ns | 648.5 / 661.1 ns | 5.1×–5.3× |
| `Dot` | 1024 | 173.9 / 177.1 ns | 896.7 / 883.8 ns | 5.0×–5.2× |
| `L2Norm` | 384 | 69.6 / 69.6 ns | 322.9 / 319.1 ns | 4.6× |
| `L2Norm` | 768 | 95.5 / 109.9 ns | 650.1 / 651.3 ns | 5.9×–6.8× |
| `L2Norm` | 1024 | 138.8 / 138.3 ns | 887.4 / 875.5 ns | 6.3× |

That is the `Vector<T>` SIMD path against the scalar fallback, which is the one
place the two builds deliberately differ — the span-based `Vector<T>` constructor
is net-only. Everything else compiles to equivalent IL, so a difference elsewhere
means something changed and is worth investigating. `L2Norm` gains more than
`Dot` because it reads one array instead of two, so the vector path is not
sharing load bandwidth with a second stream; on netstandard2.0 the two methods
cost the same, which is what two equivalent scalar loops should do.

**Read the pairs, not the means.** Every figure above was reported by
BenchmarkDotNet with an interval of about ±1 ns, and the two runs of the same
binary still differ by up to 6% (`Dot` at 384) and once by 15% (`L2Norm` at
768, 95.5 ns then 109.9). The interval describes dispersion inside one process
and says nothing about reproducibility across processes. The ratios are far more
stable than either column, which is the argument for quoting them and not the
absolute numbers.

The machine was not idle: the one-minute load average was 4.8–5.5 on 8 logical
cores at the start of all four runs, against the 1.9–2.3 floor this workstation
reaches when only its desktop client, editor and browser are up. That inflates
both columns and is the reason the pairs disagree as much as they do; it does
not favour either side, since the runs alternate.

That load is not an accident of scheduling, and the earlier figures cannot be
reproduced by re-running these commands: the editor's language servers and the
assistant session that drives the run are themselves part of it. So the table
above is internally comparable — one window, alternating sides, both columns
paying the same tax — and **not** comparable to figures taken on this machine in
a quieter state. Compare ratios across such sets, never absolutes.

### Three things keep this honest

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

**And `--inProcess` on the net10 command is the third thing, not decoration.**
The netstandard2.0 project pins `InProcessEmitToolchain` because it has to; the
net10 command would otherwise use the default out-of-process toolchain, and a
ratio taken across those two harnesses mixes the target framework with the
harness that measured it. This section published such ratios until issue #87 —
4.6×, 5.2× and 5.6× — and the flag is what removes the confound.

**What the confound is worth here, measured rather than asserted.** The net10
side was run a third time in the same window, minutes after the four runs above
and on the same loaded machine, with the flag left off:

| Dim | net10, in-process | net10, out-of-process | matched pairing | mixed pairing |
| --- | --- | --- | --- | --- |
| 384 | 74.8 / 79.3 ns | 69.9 ns | 4.2×–4.4× | 4.7×–4.8× |
| 768 | 122.9 / 128.7 ns | 123.0 ns | 5.1×–5.3× | 5.3×–5.4× |
| 1024 | 173.9 / 177.1 ns | 159.6 ns | 5.0×–5.2× | 5.5×–5.6× |

The out-of-process harness reports the *same binary* as up to 10% faster on
`Dot` and 24% faster on `L2Norm` (56.1 ns against 69.6 at 384). Pairing that
column against the in-process netstandard2.0 one — the shape of the old pair of
commands — inflates the gap by up to 0.5× at 384 and 1024, while at 768 the two
pairings agree, which is why the defect was not visible from the numbers alone.

That is the whole of what this control establishes, and it is deliberately not
compared against the figures this section used to publish. Those came off this
same machine, but in a quieter state than a run driven from an editor session
can recreate — the session is part of the load it would have to remove. Two sets
of absolute figures taken under loads that differ by a factor of two support no
inference between them, in either direction. The old ones are withdrawn on the
ground that their two columns came from two harnesses, which is visible in the
commands rather than in the numbers, and not because a later run disagreed with
them.

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
dotnet run -c Release --project bench/DataNet.Text.Benchmarks        -- --filter '*Persistence*' --inProcess
dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter '*Persistence*'
```

`--inProcess` puts the net10 side on the toolchain the netstandard2.0 project
pins, for the reasons section 2 sets out; this tier published a mismatched pair
until issue #88.

Intel i7-4770S, .NET 10.0.10. Loaders parse from memory here, so the figures are
parsing cost rather than disk latency. **Two runs per side, both shown**,
interleaved net10 → netstandard2.0 → net10 → netstandard2.0.

| Operation | net10 | netstandard2.0 | ratio | net10 alloc | ns2.0 alloc |
| --- | --- | --- | --- | --- | --- |
| `VocabTxt` | 4.251 / 4.308 ms | 4.206 / 4.347 ms | 0.99 / 1.01 | 4.25 MB | 4.25 MB |
| `TokenizerJsonWordPiece` | 12.024 / 12.222 ms | 11.955 / 11.862 ms | 0.99 / 0.97 | 7.25 MB | 7.25 MB |
| `TokenizerJsonUnigram` | 16.206 / 16.035 ms | 16.632 / 15.630 ms | 1.03 / 0.97 | 9.64 MB | 9.64 MB |
| `SpieceModel` | 4.235 / 4.222 ms | **10.602 / 10.032 ms** | **2.50 / 2.38** | 4.61 MB | **6.56 MB** |
| `TfidfSave` | 1.834 / 1.939 ms | 1.789 / 1.782 ms | 0.98 / 0.92 | 2.09 MB | 2.09 MB |
| `TfidfLoad` | 5.047 / 4.920 ms | 5.085 / 5.130 ms | 1.01 / 1.04 | 4.34 MB | 4.34 MB |

One caveat on the `TfidfSave` allocation figure: the benchmark pre-sizes its
destination `MemoryStream` to the artifact's exact final length, which a caller
writing to `new MemoryStream()` cannot do. That removes the buffer-doubling
garbage a realistic save would pay, so the number is friendlier than the typical
case. It is deliberate — the question on this axis is what the two *targets* cost
each other, not what a caller pays — but it is not a general "cost of Save".

Five rows are noise, which is what equivalent IL should produce, and two runs per
side say so more convincingly than one: those five scatter between 0.92× and
1.04× and **change sign between rounds** — `TokenizerJsonUnigram` reads 1.03×
then 0.97×, `TfidfSave` 0.98× then 0.92×. A single run per side cannot
distinguish that from a small consistent penalty, which is what this table used
to report.

`SpieceModel` is the exception and it is real: netstandard2.0 allocates twice per
piece where net10 allocates nothing, once for the `byte[4]` scratch buffer in
`ProtobufReader.ReadFloat` and once for the array copy in `DecodeUtf8` that
`netstandard2.0` needs because it has no span overload. Across 29 861 pieces that
is the whole 1.95 MB difference, and it costs 2.38×–2.50× the time. The
allocation column is counted rather than sampled and does not move between runs
at all.

**What the toolchain mismatch was worth here.** Running the net10 side
out-of-process in the same window — the shape of the command pair this section
used to publish — makes the same binary read up to 8% faster (`TfidfLoad`
4.625 ms against 4.920 / 5.047 in-process; `VocabTxt` 4.092 against 4.251 /
4.308). Pair that column against the in-process netstandard2.0 one and
`TfidfLoad` reports 1.10×–1.11× and `VocabTxt` 1.03×–1.06×, where the matched
pairing gives 1.01×–1.04× and 0.99×–1.01×. The mismatch is the same size as the
differences the five rows were being read for, which is why nothing could be
concluded from them either way. It is far too small to touch `SpieceModel`.

**Conditions.** The one-minute load average was 4.7–8.1 on 8 logical cores across
the five runs; the editor's language servers and the session driving them are
part of that and cannot be excluded from inside it. Both columns pay it equally
and the runs alternate, so the table is internally comparable — but it is not
comparable to figures taken on this machine in a quieter state, and the earlier
ones are withdrawn for their mismatched pairing rather than because these
disagree with them.

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
dotnet run -c Release --project bench/DataNet.Text.Benchmarks        -- --filter '*MetricsBenchmarks*' --inProcess
dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter '*MetricsBenchmarks*'
```

Default job on both sides. The full matrix — 3 sizes × 2 class counts ×
5 methods = 30 benchmarks — takes about ten minutes per run.

**`--inProcess` on the first command is not decoration.** Without it the two
commands do not measure the same way: `DataNet.NetStandard.Benchmarks` pins
`InProcessEmitToolchain` (its `Program.cs` needs it, or BenchmarkDotNet's
generated project re-resolves the `ProjectReference` and silently restores the
net10.0 build), while the first command would use the default out-of-process
toolchain. Any figure compared across those two harnesses mixes the target
framework with the harness that measured it. This tier was published that way
before, and the flag is what removes the confound. Sections 2 and 4 above and the
batched-embedding comparison in `docs/guides/performance.md` carried the same
defect and were re-measured with the flag in place (issues #87 and #88). Every
net10-versus-netstandard2.0 figure in this repository now comes from a pair of
commands that share a toolchain, and from two runs per side rather than one.

Same isolation assertion as those sections: the netstandard2.0 run proves it
loaded the netstandard2.0 build before any number from it is trusted.

```text
// DataNet.Metrics: .NETStandard,Version=v2.0
```

**Every figure below is two runs, not one, and the two are shown.** The runs
were interleaved (net10, netstandard2.0, net10, netstandard2.0) so that any
drift in machine load spreads across both targets instead of landing on
whichever one occupies the second half of the window. Intel i7-4770S,
.NET 10.0.10. `Matrix`, at all six shapes:

| Samples | Classes | net10 (run 1 / run 2) | netstandard2.0 (run 1 / run 2) | ratio |
| ---: | ---: | ---: | ---: | ---: |
| 1 000 | 2 | 7.167 / 7.792 µs | 7.090 / 7.003 µs | 0.90–0.99× |
| 1 000 | 10 | 7.010 / 8.620 µs | 6.951 / 7.017 µs | 0.81–0.99× |
| 100 000 | 2 | 805.1 / 800.9 µs | 795.4 / 799.2 µs | 0.99–1.00× |
| 100 000 | 10 | 916.5 / 899.5 µs | 892.1 / 894.4 µs | 0.97–0.99× |
| 1 000 000 | 2 | 8.396 / 8.181 ms | 8.158 / 8.357 ms | 0.97–1.02× |
| 1 000 000 | 10 | 9.421 / 9.139 ms | 9.118 / 9.145 ms | 0.97–1.00× |

**At n=100 000 and n=1 000 000 the two targets are at parity**, and now with
something behind the claim: all 40 measurements there — 5 methods × 4 shapes ×
2 pairings — land between 0.97× and 1.05×, while each target's own spread
between its two runs stays inside 1.05× (net10) and 1.02× (netstandard2.0).
The difference between the targets is the same size as the noise, which is the
honest way to say parity.

**At n=1 000 the ratio column is not measuring the target framework.** The
net10 side moves by up to **2.64×** between two runs of the same binary over
the same corpus — `AccuracyScore` at k=10 reads 836 ns in one run and 2 212 ns
in the next — while netstandard2.0 stays inside 1.02× across every method. The
inflation is cleanly inverse to the work per operation: `AccuracyScore` (~0.84 µs
per op) moves 2.64×, `Matrix`, `MatrixWeighted` and `F1Macro` (~7–8 µs) move
1.1×–1.3×, and `Report` (~10–16 µs) moves by 1–2%. That is the shape of a fixed
per-invocation overhead appearing in one process and not another, not of a
difference in the metric code — the same computation is happening either way.

The consequence is worth stating plainly, because this section got it wrong
before: **BenchmarkDotNet's ± margin describes dispersion *within* one process
and says nothing about reproducibility *across* processes.** An earlier version
published `AccuracyScore` at n=1 000, k=10 reading 2.59× and explained it as
the short job's noise floor — implying a longer job would settle it. A longer
job did not: the 2.59× vanished, and k=2 came back at 0.64× with a ±0.3%
margin on the net10 side, which is to say a tight interval around a figure
that a re-run then contradicted outright (1 331 ns, then 833 ns). A
`MatrixWeighted` gap of 1.06×–1.18×, apparently systematic across all six
shapes, evaporated the same way once the toolchain confound was lifted and the
runs repeated. Anything at n=1 000 in this tier needs repeated processes
before it means anything.

One small-n row *is* stable on both sides, and it says something real:
`Report` runs **1.06×–1.11× slower on netstandard2.0 at n=1 000**, consistently
across both pairings and both class counts, fading to 1.00×–1.05× at the larger
sizes. A fixed per-call cost the netstandard2.0 build pays and net10 does not,
drowned once O(samples) work dominates.

This is still **not** the `VectorMath.Dot` story from section 2, where the gap
is 4.2×–5.3×. `DataNet.Metrics` has no `Vector<T>` SIMD path, and every
benchmark here is a scalar loop over `int[]`/`double[]`. The one piece of
target-conditional code in its dependencies is `DataNet.Internal.Guard`, which
picks `ArgumentNullException.ThrowIfNull` on net10 and a hand-written check on
netstandard2.0 — a null test outside every loop, which cannot produce a
per-sample difference and does not.

### vs Python

```bash
. .venv-oracles/bin/activate && python bench/python/bench_metrics.py
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- compare-metrics
python bench/compare.py metrics
```

DataNet on .NET 10.0.10 against scikit-learn 1.9.0 / NumPy 2.5.1 on Python
3.12.3. Ratios above 1 mean DataNet is faster.

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

**Merge gate: 29/29 operations at or above 1× on processor time.** The
narrowest margin is 2.74× (`roc_auc_ovr_macro` at n=100 000, k=10). The design
brief named `roc_auc_binary` at a million samples — where the cost is a sort —
as the likely candidate for falling below 1×, with a radix pass over the
`double` bit patterns as the fallback if it did. It came in at 3.81×; no row
needed that change on this branch.

### Measurement conditions

Both files were produced back to back, Python first, on a machine left to
settle first — an earlier pairing was discarded outright for having started
while the five- and fifteen-minute averages were still shedding the tail of a
preceding test suite. The pair in the table above:

| Side | Started | Written | Load at start (1 / 5 / 15 min) |
| --- | --- | --- | --- |
| Python (`python-metrics.json`) | 19:09:25 | 19:12:30 | 1.52 / 3.71 / 3.94 |
| C# (`csharp-metrics.json`) | 19:13:19 | 19:16:41 | 4.20 / 4.10 / 4.05 |

The machine carries a permanent 30–40% background load from the desktop
client, an editor and a browser; a one-minute average of 1.9–2.3 is this
workstation's floor, and the Python side started below it.

**The C# side started at 4.20, and that is its predecessor's own wake** — 49
seconds after the Python run finished saturating a core. The second side of any
back-to-back pair pays this; the alternative, waiting for the tail to decay,
buys a quieter machine at the cost of the two sides no longer being back to
back. The bias it introduces runs *against* DataNet — the C# figures are the
ones measured on the busier machine — so every ratio in the table above, and
the merge gate that reads them, is conservative rather than flattering.

Memory was never the constraint these runs looked like they might have: the
working set at n=1 000 000 is about 16 MB, and the machine had 22 GB free with
3 GB in swap. CPU contention is the only thing worth waiting out here.

### Why processor time barely differs from wall time here

Unlike the persistence comparison in section 4 — where DataNet burns 1.12–1.21
processor-seconds per elapsed second, so the wall and cpu columns diverge and
`tfidf_load` even flips winner between them — the two columns above agree to
within about 1% on every row (up to 3.4% on the single heaviest row,
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
decisive but far more modest 2.7×–43×.

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
