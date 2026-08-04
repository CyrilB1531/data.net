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
