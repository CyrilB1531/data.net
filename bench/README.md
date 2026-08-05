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
