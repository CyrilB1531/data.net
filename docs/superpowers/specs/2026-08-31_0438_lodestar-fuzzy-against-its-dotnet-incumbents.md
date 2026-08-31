# 0438 — `Lodestar.Fuzzy` against its .NET incumbents

**Issue:** [#438](https://github.com/CyrilB1531/lodestar/issues/438) ·
**Status:** accepted · **Date:** 2026-08-31

## Problem

[#427](https://github.com/CyrilB1531/lodestar/issues/427)'s package rule is one sentence with two
clauses: every package ships a Python-oracle test suite **and one benchmark against a named .NET
incumbent.** [#438](https://github.com/CyrilB1531/lodestar/issues/438) established that the first
holds and the second is unmet for all four packages — searching `bench/` for Fastenshtein,
Quickenshtein, F23, Raffinert.FuzzySharp, `TensorPrimitives`, Lucene.NET or
`Microsoft.ML.Tokenizers` returned nothing, and everything in `docs/guides/performance.md` compares
against Python.

That answers *"should you leave Python"*, not *"why this over the .NET library that already
exists"* — the question a .NET reader asks first.

This lot takes the first of #438's four boxes, `Lodestar.Fuzzy`, on that issue's own reasoning:
the most contested package with the least excuse, because its incumbents are single-purpose and
directly comparable.

## Scope

Two BenchmarkDotNet classes in `bench/Lodestar.Text.Benchmarks`, and the four incumbents pinned
exactly in its project file — referenced by `bench/` and by nothing under `src/`, which
`tools/check_nuspec_dependencies.py` already asserts.

| class | baseline | incumbents |
| --- | --- | --- |
| `LevenshteinIncumbentBenchmarks` | `Levenshtein.Distance` | Fastenshtein 1.0.12, Quickenshtein 1.5.1, F23.StringSimilarity 7.0.1 |
| `FuzzIncumbentBenchmarks` | `Fuzz.*` | Raffinert.FuzzySharp 6.0.0 |

Levenshtein is in `Lodestar.Text` rather than `Lodestar.Fuzzy`, and belongs to this lot anyway:
Issue #438 lists Fastenshtein under Fuzzy because `Fuzz.Ratio` is built on `Indel`, and a table without
it would not answer the claim the roadmap says nobody will believe without it.

## Three things the shape had to get right

**1. The values agree before the clocks run.** A speed table over two functions returning different
answers means nothing. Checked over `kitten`/`sitting`, `flaw`/`lawn`, the sentence pair, an empty
operand and an identical pair: all four Levenshtein implementations return the same distance on all
five, and Lodestar and FuzzySharp the same ratio on all four operations, to the last digit of the
double. Section 14 of `bench/README.md` made agreement a precondition; this follows it.

**2. The unit has to match.** All four Levenshtein implementations compare UTF-16 code units, so
`Levenshtein.Distance`'s default overload is the baseline. The `TextElement.CodePoint` overload
would be measuring something none of the incumbents offers, and `LevenshteinBenchmarks` already
times it.

**3. Each pair needs its own baseline.** Written as eight methods, the fuzzy class would give
BenchmarkDotNet one baseline for all of them, and the ratio column would compare `PartialRatio`
against `Ratio` rather than against its counterpart. The operation is therefore a `[Params]` enum
and there are two methods, so each operation is its own row-group. The switch costs both sides the
same jump.

## Where the numbers may be published — not here

This lot deliberately adds **no number to `docs/guides/performance.md`**.

The run available to the session that wrote this is a shared container, and the repository has
already ruled on that twice:
[ADR 0051](../../decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md) withdrew a
1.61× taken that way, and `bench/README.md` section 14 records the container **inverting** every
`TensorPrimitives` ratio it was asked for. A container run of these classes proves the harness
works and nothing else.

Two channels take it from here, both already built:

- `docs/guides/performance.md` takes the tables from a named machine, under its existing
  name-the-machine rule.
- The nightly publishes their ratios to `docs/guides/nightly_run.md` on its own: both classes are
  in `bench-map.json`, selected by any change under `src/Lodestar.Fuzzy/` or
  `src/Lodestar.Text/Distances/`.

## What the container run showed, and what was done with it

Reported as a lead rather than a figure. Levenshtein favours us on every length against all three
incumbents. The fuzzy ratios do not: `Ratio` is ours and allocates nothing, while `PartialRatio`,
`TokenSetRatio` and `WRatio` each lose on time, allocation, or both — `TokenSetRatio` at 5,824 B
against 1,944 B for one 43-character pair.

Issue #438 said this measurement "can come back negative", and on three rows of four it did. The
allocation is the least likely part to move with the host, because bytes per operation are a
property of the code path. It is filed as
[#494](https://github.com/CyrilB1531/lodestar/issues/494), whose first step is confirming the
reading on a named machine — a refutation closes it, and that is a real outcome.

Fixing it is not this lot. The lot's job was to find out.

## Testing

- `tools/check_bench_map.py` refuses a `[Benchmark]` class the map does not name; both new classes
  are mapped, and the check passes.
- The build is clean at `AnalysisMode=All` with warnings as errors, `samples/` unaffected.
- Both classes run to completion under `--job short`, which is what proves the harness rather than
  the numbers.
