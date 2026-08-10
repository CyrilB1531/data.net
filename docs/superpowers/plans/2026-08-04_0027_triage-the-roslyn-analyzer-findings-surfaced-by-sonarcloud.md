# #27 Triage the Roslyn findings — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Answer all seventeen `external_roslyn` findings — each either fixed or suppressed with a written reason — without breaking the `netstandard2.0` build or the benchmark suite.

**Architecture:** Sort by whether the rule is right *about this codebase*, then work the two groups separately. The fixes go first because they are unambiguous; the suppressions second, each carrying its reason at the suppression. `CA1822` is settled by running BenchmarkDotNet rather than by argument.

**Tech Stack:** Roslyn analyzers (CA rules), C# (net10.0 + netstandard2.0), BenchmarkDotNet, xunit.

**Spec:** `2026-08-04_0027_triage-the-roslyn-analyzer-findings-surfaced-by-sonarcloud.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `chore/27-roslyn-findings`. Never commit to `main`.
- **Never apply a fix that removes an API the `netstandard2.0` leg needs.** Build
  both targets after every change, not at the end.
- **No batch auto-fix.** Seventeen findings is exactly the size where a bulk apply
  feels efficient and silently undoes #1.
- **No corpus may move.**
- Every suppression carries its reason **at the suppression**.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
build_ns()  { dotnet build -c Release -f netstandard2.0; }
test_all()  { dotnet test -c Release; }

bdn_discovers() { dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --list flat; }

oracles_unchanged() {
  test -z "$(git status --porcelain tests/oracles/)" && echo "ORACLES CLEAN" || echo "ORACLES MOVED"
}
```

---

### Task 1: Sort the seventeen, by whether the rule is right

**Files:** none modified.

**Depends on:** nothing.
**Produces:** two lists — and the discipline that the sort is by correctness, not
by effort.

- [ ] **Step 1: List them from the SonarQube Cloud dashboard, by rule and file**

- [ ] **Step 2: For each, ask one question — does the suggested API exist on
      `netstandard2.0`?**

```bash
grep -n "NET5_0_OR_GREATER\|#if NET" src/Shared/*.cs
```

`CA1845`, `CA2249` and `SYSLIB1045` all suggest net-only APIs. `CA2249` is
circular in particular: `StringCompat` **is** the `Contains(char)` polyfill.

- [ ] **Step 3: Mark `CA1822` as undecided**

It looks like an obvious fix. Task 4 settles it by experiment, and it must not be
grouped with the other fixes before that.

---

### Task 2: The fixes

**Files:**

- Modify: `src/Shared/Guard.cs`
- Modify: `src/DataNet.Embeddings/Search/EmbeddingIndex.cs`
- Modify: `src/DataNet.Text/Phonetics/Nysiis.cs`
- Modify: `bench/DataNet.Text.Benchmarks/CrossLang/LevenshteinCrossLang.cs`
- Modify: `tests/DataNet.Embeddings.Tests/WordPieceTokenizerTests.cs`

**Depends on:** Task 1.

- [ ] **Step 1: `CA1512` — `Guard.NotLessThan`, not the API directly**

`ArgumentOutOfRangeException.ThrowIfLessThan` is net8+. It goes behind a new
`Guard.NotLessThan` beside `Guard.NotNull`: **one `#if` for the repository**, the
shape #1 established. Using the API directly at two call sites would put a
directive back in `EmbeddingIndex`.

- [ ] **Step 2: `CA1865` — `Nysiis` compares a single-character string**

The char overload is in-box on net10 and polyfilled by `StringCompat` on
`netstandard2.0`, so this fix is portable. Verify by building both.

- [ ] **Step 3: `CA1869` — cache the `JsonSerializerOptions`**

Built per serialization in the cross-language benchmark, which defeats its
metadata cache — a real cost in a benchmark, of all places.

- [ ] **Step 4: `CA1861` ×2 — hoist the constant arrays in the WordPiece tests**

- [ ] **Step 5: Both targets, and the corpora**

```bash
build_all && test_all 2>&1 | tail -3 && oracles_unchanged
```

---

### Task 3: The suppressions that protect the portable build

**Files:**

- Modify: `src/DataNet.Text/Stemming/EnglishSnowballStemmer.cs`
- Modify: `src/DataNet.Text/Stemming/FrenchSnowballStemmer.cs`
- Modify: `src/Shared/StringCompat.cs`

**Depends on:** Task 2.

- [ ] **Step 1: Try per-target scoping before suppressing outright**

A blanket suppression also hides the rule on the net10 leg, giving up real
coverage. Check whether the finding can be scoped to the `netstandard2.0` target
first.

- [ ] **Step 2: `CA1845` ×5 in both Snowball stemmers**

Reason: the span-based `string.Concat` overload does not exist on
`netstandard2.0`, and the `Substring` form the rule objects to is precisely what
makes these files compile there.

- [ ] **Step 3: `CA2249` in `StringCompat`**

Reason: circular. This file *is* the polyfill for `Contains(char)`.

- [ ] **Step 4: `SYSLIB1045`**

Net-only attribute. Same reason recorded in #25.

- [ ] **Step 5: Prove the portable build still compiles**

```bash
build_ns
```

A suppression that quietly accompanied a "fix" would show up here.

---

### Task 4: `CA1822` — settle it by running the benchmarks

**Files:**

- Modify: `bench/DataNet.Text.Benchmarks/FuzzBenchmarks.cs`

**Depends on:** Task 3.
**Produces:** the finding that justifies triaging rather than auto-fixing.

- [ ] **Step 1: Apply the rule — make the five benchmark methods `static`**

- [ ] **Step 2: Build, and note that it succeeds**

```bash
build_all
```

Expected: green. That is the trap.

- [ ] **Step 3: Run BenchmarkDotNet**

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*Fuzz*' 2>&1 | head -20
```

Expected:

```text
* Benchmarked method `Ratio` is static.
  Benchmarks MUST be instance methods, static methods are not supported.
```

**Every benchmark rejected, behind a green build.** Obeying this rule breaks the
suite silently at run time.

- [ ] **Step 4: Revert and suppress, with that output as the reason**

- [ ] **Step 5: Confirm all five are discovered again**

```bash
bdn_discovers | grep -c "Fuzz"
```

Expected: five. Reverting is not enough — prove the suite is whole.

---

### Task 5: Full gate

**Depends on:** Task 4.

- [ ] **Step 1: Everything**

```bash
dotnet clean -c Release && build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
oracles_unchanged
```

- [ ] **Step 2: Every finding accounted for**

Walk Task 1's list. Each entry must map to a fix in the diff or a suppression with
a reason. An unaccounted finding means the dashboard list was incomplete.

- [ ] **Step 3: Read SonarQube Cloud on the pushed branch**

A green build is not a clean Sonar.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Triage the Roslyn analyzer findings surfaced by SonarQube Cloud"
```
