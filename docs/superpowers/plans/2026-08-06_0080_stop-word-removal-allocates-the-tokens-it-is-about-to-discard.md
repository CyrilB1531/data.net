# #80 Stop-word lookup without allocating — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop paying, on every document, for the tokens that are about to be discarded — without changing which words are removed.

**Architecture:** `StopWordSet` centralises the lookup and the one `#if` between the two targets. On net10 the token is tested as a span through `FrozenSet`'s `AlternateLookup`, so a stop word never becomes a string. Each shipped list gets its own nested holder so one list is built per first use. A shipped list is adopted rather than re-hashed; a caller's set is still copied.

**Tech Stack:** C# (net10.0 + netstandard2.0), `System.Collections.Frozen`, BenchmarkDotNet `[MemoryDiagnoser]`, xunit.

**Spec:** `2026-08-06_0080_stop-word-removal-allocates-the-tokens-it-is-about-to-discard.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `perf/80-stop-word-lookup`. Never commit to `main`.
- **Which words are removed must not change.** Corpora untouched.
- **One `#if`, in `StopWordSet`.** Not one per call site — the shape
  `src/Shared/Guard.cs` established.
- A `perf/` pull request carries before/after numbers and names the machine.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
test_sw()   { dotnet test -c Release --filter "FullyQualifiedName~StopWord"; }
test_all()  { dotnet test -c Release; }
bench_sw()  { dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*StopWord*'; }

oracles_unchanged() {
  test -z "$(git status --porcelain tests/oracles/)" && echo "ORACLES CLEAN" || echo "ORACLES MOVED"
}
```

---

### Task 1: Measure the three costs

**Files:**

- Create: `bench/DataNet.Text.Benchmarks/StopWordBenchmarks.cs`

**Depends on:** nothing.
**Produces:** the baseline; a `perf/` branch without one is an opinion.

- [x] **Step 1: Benchmark the per-token path with `[MemoryDiagnoser]`**

```bash
bench_sw 2>&1 | tail -20
```

Record allocations per document, not only time. The claim in this branch is
primarily about allocation.

- [x] **Step 2: Measure the initialiser cost**

```bash
# Time and allocation of first touching StopWords.English alone.
```

Expected: the six lists share one static constructor, so `StopWords.English`
hashes **1 493 words to hand back 318**.

- [x] **Step 3: Confirm the shipped list is re-hashed by the vectorizer**

The third cost: a `CountVectorizer` configured with `StopWords.English` copies a
set that is already frozen with the right comparer.

---

### Task 2: `StopWordSet` — one place for the lookup and the one `#if`

**Files:**

- Create: `src/DataNet.Text/Vectorization/StopWordSet.cs`
- Modify: `src/DataNet.Text/Vectorization/TextAnalyzer.cs`

**Depends on:** Task 1.

- [x] **Step 1: The net10 path — span lookup, no allocation**

`s.AsSpan(m.Index, m.Length)` against `FrozenSet<string>` through
`AlternateLookup<ReadOnlySpan<char>>`. **Only survivors reach `m.Value`.**

- [x] **Step 2: The `netstandard2.0` path — unchanged**

It has neither type. Keep what it always had.

- [x] **Step 3: One `#if`, here**

In the shape of `src/Shared/Guard.cs`. A directive at each call site is how the
two targets drift.

- [x] **Step 4: Adopt a shipped list; copy a caller's**

`ToFrozenSet(StringComparer.Ordinal)` returns its argument when the set is already
frozen with that comparer. **Verify that** — including that it *copies* when the
comparer differs — rather than relying on documented behaviour.

A caller's `HashSet<string>` is still copied: it is theirs to keep mutating, and a
fitted vectorizer that followed along would remove words its options never
declared. Write a test for exactly that.

---

### Task 3: One list per first use

**Files:**

- Modify: `src/DataNet.Text/Vectorization/StopWords.cs`,
  `StopWords.Snowball.cs`

**Depends on:** Task 2.

- [x] **Step 1: A nested holder type per language**

So touching `StopWords.English` builds one list, not six.

- [x] **Step 2: `=> FrenchList.Value`, not `{ get; }`**

An auto-property puts a static field back on `StopWords` and restores the shared
initialiser. Comment it, because it looks like a style choice and is not.

- [x] **Step 3: A laziness test that catches the regression**

Touch one list and assert the others were not built. **This test is what makes the
decision durable** — without it the next tidy-up silently undoes the work.

Confirm it fails when the property is written as an auto-property.

---

### Task 4: Prove nothing changed, and measure what did

**Depends on:** Task 3.

- [x] **Step 1: Corpora and suite**

```bash
build_all && test_all 2>&1 | tail -3 && oracles_unchanged
```

Expected: green on both frameworks, `ORACLES CLEAN`. Which words are removed is
not part of this change.

- [x] **Step 2: Re-benchmark, both targets**

```bash
bench_sw 2>&1 | tail -20
```

Report allocations before and after, and name the machine.

- [x] **Step 3: Update the guide and the changelog**

`docs/guides/vectorization.md` describes the stop-word path; the changelog records
a performance change with no behavioural component.

- [x] **Step 4: Commit**

```bash
git add -A
git commit -m "Discard a stop word without allocating it first"
```
