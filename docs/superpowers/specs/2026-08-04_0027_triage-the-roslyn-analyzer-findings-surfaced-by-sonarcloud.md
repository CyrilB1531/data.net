# Design — #27: triage the Roslyn findings SonarQube Cloud surfaced

**Date:** 2026-08-04 · **Issue:** #27 · **Branch:** `chore/27-roslyn-findings` ·
**Checkout:** `<repo>`

## Problem

Issue #19 made SonarQube Cloud import the Roslyn analyzer output (`external_roslyn`),
which surfaced seventeen findings that were never visible locally.

They split into two groups, and **the split is by whether the rule is right about
this codebase — not by how easy it is to silence**. That distinction is the whole
design: a list of seventeen analyzer findings invites a batch auto-fix, and a
batch auto-fix here breaks the portable build.

## Decisions

### D1 — Fix where the rule is right

- **`CA1512` ×2** — `ArgumentOutOfRangeException.ThrowIfLessThan` in
  `EmbeddingIndex`. It is net8+, so it goes behind a new `Guard.NotLessThan`
  beside `Guard.NotNull`: **one `#if`, not one per call site**, the same shape #1
  established.
- **`CA1865`** — `Nysiis` compares a single-character string. The char overload is
  in-box on net10 and polyfilled by `StringCompat` on `netstandard2.0`, so this
  one is portable and worth taking.
- **`CA1869`** — the cross-language benchmark builds a `JsonSerializerOptions` per
  serialization, defeating its metadata cache.
- **`CA1861` ×2** — constant array arguments in the WordPiece tests allocate on
  every call.

### D2 — Suppress where obeying the rule would break the `netstandard2.0` build

Each of these suggests an API that does not exist there. "Fixing" them undoes #1.

- **`CA1845` ×5** — span-based `string.Concat` and `AsSpan` instead of
  `Substring`, in both Snowball stemmers. The span overload is net-only; the
  `Substring` form is **precisely what was written to make netstandard2.0
  compile**.
- **`CA2249`** — `string.Contains` instead of `string.IndexOf`, in
  `StringCompat.cs`. Circular: that file *is* the polyfill for `Contains(char)`,
  so it cannot be implemented in terms of itself.
- **`SYSLIB1045`** — `[GeneratedRegex]`, net-only.

### D3 — `CA1822` is verified, not argued

The rule says the benchmark methods can be `static`. That sounds obviously right.

Making them static and running BenchmarkDotNet gives:

```text
* Benchmarked method `Ratio` is static.
  Benchmarks MUST be instance methods, static methods are not supported.
```

**Every benchmark was rejected — and the build succeeded.** Obeying this rule
breaks the benchmark suite at run time, silently, behind a green build.

This is the finding that justifies the whole triage. An argument about why a rule
is wrong can be mistaken; a run that rejects five benchmarks cannot.

### D4 — Prefer per-target scoping to a blanket suppression, where possible

Check whether the netstandard-only findings can be scoped per target framework
rather than suppressed outright. A suppression that also hides the rule on the
net10 leg gives up real coverage.

### D5 — Every suppression carries its reason at the suppression

Not in the pull request, not in the issue. The next person to read
`FrenchSnowballStemmer.cs` needs it there.

## Out of scope

- Any change to what the analysers are configured to run (later #84, #107).
- Behaviour. The corpora must not move.

## What "done" means

Each of the seventeen either fixed or suppressed with a written justification;
nothing in the first group "fixed" in a way that breaks the `netstandard2.0`
build; BenchmarkDotNet still discovering and running all five benchmarks.
