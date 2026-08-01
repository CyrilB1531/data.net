# Benchmarks

Two complementary tools.

## 1. Intra-C# micro-benchmarks (BenchmarkDotNet)

Rigorous per-method measurement, for optimizing the C# implementation itself:

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*Levenshtein*'
```

## 2. Cross-language comparison vs Python

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
