# 0438 — `Lodestar.Text` against ML.NET's `FeaturizeText`

**Issue:** [#438](https://github.com/CyrilB1531/lodestar/issues/438) ·
**Status:** accepted · **Date:** 2026-08-31

## Problem

The third of [#438](https://github.com/CyrilB1531/lodestar/issues/438)'s four boxes, and the one
that box itself flags as awkward:

> **`Lodestar.Text`** — ML.NET `FeaturizeText`, and Lucene.NET where it overlaps. Framed to show
> the comparison is **not** like-for-like: `FeaturizeText` is `IDataView`-coupled and does a
> different job, and a row treating them as equals would contradict our own argument.

The two lots before this one could open on an agreement check: identical distances, identical
ratios, identical ids. Here there is no agreement to check, because the two sides do not compute
the same thing. That changes what the lot has to deliver — the framing is the deliverable, and the
ratio is the part most likely to mislead.

## What the two sides actually produce

Measured on `bench/corpus/vocabs/documents.json`, not inferred:

| documents | `TfidfVectorizer` | `FeaturizeText` |
| ---: | --- | --- |
| 200 | 200 × 7 018 sparse, 7 996 stored values | 200 × 31 112 dense, 70 307 non-zero, 6 222 400 floats materialized |
| 1 000 | 1 000 × 21 867 sparse, 39 974 stored values | 1 000 × 81 384 dense, 351 217 non-zero, 81 384 000 floats materialized |

`FeaturizeText` adds character n-grams to the word n-grams and L2-normalizes. Its feature space is
about four times wider and it produces roughly **8.8× more non-zero features**. It is doing more
work, and any wall-clock ratio that does not say so is unusable.

## Decision — publish the ratio only with its divisor

The container run measures 45.6 ms against 445.0 ms at 1 000 documents, a 9.75× advantage. Left
there, that reads as "our TF-IDF is ten times faster", which the table above shows is not what was
measured.

Divided by what each side produced: **1.14 µs and 1.27 µs per non-zero feature — within about
11 %.** The arithmetic is not the difference. What is:

- a sparse matrix stores 39 974 values where the dense pipeline materializes 81 million floats for
  the same thousand documents;
- ours returns a `CsrMatrix` from a method call, with no `IDataView`, no schema and no pipeline
  object between the caller and the result.

That is the argument the README already makes, and it is exactly what this measurement supports.
No more — and `bench/README.md` section 15 says so in those terms so a reader meeting the 9.75×
first cannot take it for a claim about the kernel.

## Lucene.NET is out of overlap, and that is the finding

Issue #438 says "where it overlaps". On the vectorizer it does not. Lucene.NET's TF-IDF lives inside an
index, reached through an `IndexSearcher`: obtaining a document-term matrix means indexing the
corpus and then reading term vectors back. Comparing a library call to an indexing engine would
repeat the category error this lot exists to avoid.

Its analysis chain does overlap our tokenizers. That is a different measurement against a
different type, and a different lot's decision.

## Scope

`VectorizerIncumbentBenchmarks`, `[Params]` over 200 and 1 000 documents of the same corpus the
tokenizer class reads. ML.NET 5.0.0, pinned exactly, referenced by `bench/` and by nothing under
`src/`.

The ML.NET side materializes its rows rather than stopping at `Fit`: its transforms are lazy, so
timing the plan instead of the work would flatter it against a sparse side that does everything
eagerly.

One suppression, with its reason on the line: `CA1819` on the `float[]` output property, because
ML.NET binds its output column by reflecting over exactly that shape.

## What does not change

No number reaches `docs/guides/performance.md` — the run is a shared container, and
[ADR 0051](../../decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md) settled what
that is worth. The class is in `bench-map.json`, so the nightly publishes its ratios; the
name-the-machine page takes the rest.

## Testing

- `tools/check_bench_map.py` refuses a `[Benchmark]` class the map does not name; the class is
  mapped and the check passes.
- The build is clean at `AnalysisMode=All` with warnings as errors.
- The class runs to completion under `--job short`.
- The feature-space table above is a measurement of the same corpus at the same two sizes, taken
  before the ratio was written down rather than after.
