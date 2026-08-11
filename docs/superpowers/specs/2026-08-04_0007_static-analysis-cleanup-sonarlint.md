# Design — #7: the SonarLint backlog

**Date:** 2026-08-04 · **Issue:** #7 · **Branch:** `chore/7-sonarlint-cleanup` ·
**Checkout:** `<repo>`

## Problem

SonarLint has a backlog of findings across the tree and nothing has ever worked
through it. Left alone it grows until nobody reads it, at which point a real
defect hides in the noise.

The complication specific to this repository: **most of the findings sit inside
code whose conformance to a Python reference is the entire product.** A "cleanup"
that changes what `Jaro.Similarity` returns is not a cleanup, it is a silent
regression against rapidfuzz — and the tree has 158 tests that would keep passing
if the corpora did not cover the case.

## Decisions

### D1 — Every fix touching an algorithm is verified against the oracle corpora

Not "the tests pass" — the corpora specifically. That is the only artefact that
knows what the Python reference does. This is the governing rule of the branch and
the reason it is a design decision rather than a chore.

### D2 — Fix the genuine defects

- `S2184` — `Jaro` assigns an `int` division to a `double`.
- `S3218` — `Worker.Stem()` shadows the outer static `Stem(string)` in both
  Snowball stemmers; a record property shadows an outer const in a benchmark.
- `S3241`/`S3626` — `Step1`, `Step2a`, `Step2b` return a `bool` no caller reads,
  leaving dead trailing returns.
- `S3358` — nested ternaries in `Nysiis`, `EnglishSnowballStemmer`,
  `HashingVectorizer`.
- `S6608` — `results.First()` on an indexable collection.
- `S8969` — a null-forgiving operator made redundant by `Assert.NotNull`.
- `S2234` — a symmetry check that reads as swapped arguments.
- `S125` — two prose comments that parse as commented-out code.
- `S1192` — literals repeated across corpora.

### D3 — Three of those need the *unobvious* fix, because the obvious one is wrong

This is the substance of the branch and the reason it cannot be delegated to an
auto-fixer.

- **`S2184` in `Jaro` is not a truncation bug.** The count of mismatched positions
  is always even, so the halving is exact and matches jellyfish and rapidfuzz.
  Casting an operand to `double` — what the rule suggests — would be a *behaviour
  change*. The fix is to make the intent explicit with an `int` and a comment.
- **`S3358` in the stemmer becomes an if/else chain, not a loop over a candidate
  array.** The array version allocates on every call in a per-token path. The rule
  is about readability; the fix must not buy it with an allocation.
- **`S2234` is a false positive.** The symmetry check swaps its arguments
  deliberately and reads as a mistake only because the locals mirror the parameter
  names `a`/`b`. Rename them to `x`/`y`/`z`; do not "fix" the call.

### D4 — Suppress, with a written reason, where the rule is wrong for this code

- **`S3776` (cognitive complexity)** on the phonetic encoders and stemmers.
  Decomposing a published rule-engine into helpers breaks the 1:1 mapping with the
  reference, and that mapping is what makes divergences auditable. The complexity
  is the algorithm's, not the code's.
- **`S3267`, `S4136`, `S127`, `S907`** — the last on the canonical MurmurHash3
  tail, which is written the way the reference is written on purpose.
- **`S2245`** — seeded RNG in benchmarks and the oracle generator, where
  determinism is the requirement.

### D5 — One suppression is load-bearing and is verified, not asserted

`S3267` on `TextAnalyzer.Tokenize` suggests `Select(m => m.Value)`. Applying it
**breaks the build**:

```text
error CS1061: 'MatchCollection' does not contain a definition for 'Select'
  [DataNet.Text.csproj::TargetFramework=netstandard2.0]
```

`MatchCollection` implements only the non-generic `IEnumerable` on
`netstandard2.0`, so LINQ needs a `Cast<Match>()` plus an extra allocation in a
per-document path. The rule is simply wrong for a multi-targeted library here, and
the suppression records that with the error text.

### D6 — Suppressions live in the source, and that is a finding about the tooling

**SonarLint reads neither `.editorconfig` nor a workspace `.vscode/settings.json`.**
`sonarlint.rules` is declared application-scope in the extension manifest, so VS
Code silently drops it from a workspace file.

The pragma works because SonarLint's C# analysis is SonarAnalyzer on Roslyn.
Unknown pragma IDs emit no `CS1691`, so this is safe under the repository-wide
warnings-as-errors #6 just introduced.

Python has no pragma, so the oracle generator uses `# NOSONAR`, which applies only
to the line it terminates.

### D7 — `S3903` needs no work

Multi-targeting (#1) already moved the shared helpers into `DataNet.Internal`. Say
so rather than leaving a reader wondering why it is absent from the list.

## Out of scope

- Any behaviour change. If a fix would move an oracle value, it is not a fix.
- Publishing analysis to SonarCloud (#19) — this is the local backlog only.

## What "done" means

Build clean under warnings-as-errors on both frameworks, 158/158, every oracle
corpus byte-identical, and every remaining finding either fixed or carrying a
written reason.
