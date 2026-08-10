# #25 Bound Regex backtracking — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A crafted document or pattern fed to the public API raises `RegexMatchTimeoutException` instead of stalling the calling thread — with the timeout proven to fire, not assumed.

**Architecture:** `RegexDefaults` joins `Guard` and `StringCompat` in `src/Shared`, compiled into each library, so the policy has one definition. Both `Regex` constructions take their timeout from it. The exception surfaces rather than being swallowed, because a timed-out tokenization must not look like an empty document.

**Tech Stack:** C# (net10.0 + netstandard2.0), `System.Text.RegularExpressions`, xunit.

**Spec:** `2026-08-04_0025_pass-a-timeout-to-regex-to-bound-backtracking.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `fix/25-regex-timeouts`. Never commit to `main`.
- **No tokenization result may change.** `git diff -- tests/oracles/` empty at the
  end, and the vectorizer and tokenizer corpora green.
- **Do not catch the exception.** Swallowing it as a no-match is the failure this
  branch exists to avoid.
- **Do not adopt `[GeneratedRegex]`.** It is net-only and the libraries also
  target `netstandard2.0`.
- Both frameworks build; warnings are errors.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
test_all()  { dotnet test -c Release; }
test_rx()   { dotnet test -c Release --filter "FullyQualifiedName~RegexTimeout"; }

oracles_unchanged() {
  test -z "$(git status --porcelain tests/oracles/)" && echo "ORACLES CLEAN" || echo "ORACLES MOVED — STOP"
}
```

---

### Task 1: See the hang before fixing it

**Files:** none modified.

**Depends on:** nothing.
**Produces:** first-hand knowledge of the failure mode, and the input Task 3's
test will use.

- [ ] **Step 1: Find both construction sites**

```bash
grep -rn "new Regex" src --include='*.cs'
```

Expected: `WordPieceTokenizer.cs` and `TextAnalyzer.cs`, neither passing a
`matchTimeout`.

- [ ] **Step 2: Confirm the pattern is caller-supplied in one of them**

```bash
grep -n "TokenPattern" src/DataNet.Text/Vectorization/CountVectorizer.cs src/DataNet.Text/Vectorization/TextAnalyzer.cs
```

`TextAnalyzer` takes a caller-supplied **pattern** as well as caller-supplied
text. An arbitrary pattern over arbitrary text is the textbook ReDoS pair,
reachable from the public API — which is why Sonar's Minor rating understates it
here.

- [ ] **Step 3: Demonstrate the hang, with a hard stop**

```bash
timeout 10 dotnet run --project /tmp/redos-probe   # (a scratch console app)
echo "exit: $?"   # 124 means it was still running
```

Use `(a+)+$` against `"aaa…a!"` (40 characters). Expected: killed at 10 s. This is
the behaviour a consumer currently gets, and it is why the test in Task 3 proves
something by simply completing.

---

### Task 2: One policy, one definition

**Files:**

- Create: `src/Shared/RegexDefaults.cs`
- Modify: `src/Directory.Build.props`
- Modify: `src/DataNet.Text/Vectorization/TextAnalyzer.cs`
- Modify: `src/DataNet.Embeddings/Tokenization/WordPieceTokenizer.cs`

**Depends on:** Task 1.

- [ ] **Step 1: `RegexDefaults` in `src/Shared`**

Alongside `Guard` and `StringCompat`, in `DataNet.Internal`, compiled into each
library by `src/Directory.Build.props`.

- [ ] **Step 2: One second, with the reasoning in the source**

Generous enough that no realistic document approaches it; small enough that a
catastrophic pattern fails fast. The number is a judgement — write it down once,
here, rather than as a literal at two call sites that will drift.

- [ ] **Step 3: Add it to the `Compile Include` list**

```bash
grep -n "Shared/" src/Directory.Build.props
```

- [ ] **Step 4: Both call sites take the timeout**

- [ ] **Step 5: Do not swallow the exception**

`RegexMatchTimeoutException` propagates. Document it on the public API: a
timed-out tokenization returning "no tokens" would be indistinguishable from a
legitimately empty document, which is the worse of the two failures because it is
silent.

- [ ] **Step 6: Both targets build**

```bash
build_all
```

If `SYSLIB1045` suggests `[GeneratedRegex]`, suppress it with the reason — the
attribute is net-only and this library also targets `netstandard2.0`. Record the
reason so the suggestion is not re-raised in six months.

---

### Task 3: Prove the timeout fires

**Files:**

- Create: `tests/DataNet.Text.Tests/Vectorization/TextAnalyzerRegexTimeoutTests.cs`

**Depends on:** Task 2.

- [ ] **Step 1: The ReDoS test**

```csharp
[Fact]
public void Pathological_pattern_times_out_instead_of_hanging()
{
    string input = new string('a', 40) + "!";
    Assert.Throws<RegexMatchTimeoutException>(() => Analyzer(@"(a+)+$").Analyze(input));
}
```

**Reaching the assertion is the proof.** Unbounded backtracking on that input does
not finish in any reasonable time, so a test that completes has demonstrated the
timeout fired.

- [ ] **Step 2: A second test pinning ordinary behaviour**

An ordinary document tokenizes exactly as before. Without it, a timeout set
absurdly low would pass Step 1 and break everything else.

- [ ] **Step 3: Run them, and read the count**

```bash
test_rx 2>&1 | tail -5
```

Expected: 2 tests, both passing, in well under a second each.

---

### Task 4: Full gate

**Depends on:** Task 3.

- [ ] **Step 1: Everything, and the corpora**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
oracles_unchanged
```

Expected: 160/160 (158 + 2 new), 0 warnings on both frameworks, format clean,
`ORACLES CLEAN`.

- [ ] **Step 2: Record the behavioural change where an upgrader will look**

This is a **contract change**: input that previously hung now throws. It belongs
under *Changed* in the changelog, not filed under *Fixed*.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "Bound Regex backtracking with a match timeout"
```
