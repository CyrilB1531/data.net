# #7 SonarLint backlog — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clear the SonarLint backlog with **no behaviour change** — every finding either fixed, or suppressed in the source with a written reason — and prove the "no behaviour change" claim against the oracle corpora rather than against the test count.

**Architecture:** Findings are sorted into three piles and worked in that order: genuine defects, the three whose obvious fix would be wrong, and the ones where the rule does not apply to this code. Suppressions go in the source, because SonarLint ignores both `.editorconfig` and workspace settings.

**Tech Stack:** SonarLint / SonarAnalyzer on Roslyn, C# (net10.0 + netstandard2.0), xunit, `# NOSONAR` for the Python generator.

**Spec:** `2026-08-04_0007_static-analysis-cleanup-sonarlint.md` (in `../specs/`).

## Global Constraints

- **Everything in English** — code comments, suppression reasons, commit message.
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `chore/7-sonarlint-cleanup`. Never commit to `main`.
- **No oracle value may move.** `git diff -- tests/oracles/` must be empty at the
  end. This is the branch's single most important check and it is stronger than
  the test suite: the suite would keep passing on a case the corpora do not cover.
- **Every suppression carries a reason in the source.** No bare `#pragma`.
- Both frameworks build under the warnings-as-errors #6 just landed.
- Stay out of #19's lane: this is the local backlog, not CI analysis.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
test_all()  { dotnet test -c Release; }

# The check that actually matters on this branch.
oracles_unchanged() {
  git diff --stat -- tests/oracles/ | tail -1
  test -z "$(git diff --name-only -- tests/oracles/)" && echo "ORACLES CLEAN" || echo "ORACLES MOVED — STOP"
}
```

---

### Task 1: Inventory before touching anything

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the three piles, and a baseline the end of the branch is compared to.

- [x] **Step 1: Record the current findings, per rule and per file**

From the SonarLint panel or a solution-wide analysis. Group by rule id. A count
per rule is what makes Task 4's suppressions defensible — a rule suppressed
without knowing how often it fires is a rule suppressed on a hunch.

- [x] **Step 2: Record the baseline test count and the oracle hashes**

```bash
test_all 2>&1 | tail -3
git rev-parse HEAD:tests/oracles
```

- [x] **Step 3: Sort every finding into one of three piles**

- **Defect** — the rule is right and the code is wrong.
- **Right rule, wrong fix** — obeying the suggestion changes behaviour or costs an
  allocation on a hot path.
- **Rule does not apply** — the shape is deliberate.

Do not skip this and work the list top to bottom. The three piles get different
treatment and mixing them is how a behaviour change gets committed as a cleanup.

---

### Task 2: The genuine defects

**Files:**

- Modify: `src/DataNet.Text/Distances/Jaro.cs`, `Lcs.cs`, `Levenshtein.cs`,
  `Osa.cs`, `Indel.cs`, `Hamming.cs`, `DamerauLevenshtein.cs`
- Modify: `src/DataNet.Text/Phonetics/Nysiis.cs`, `Metaphone.cs`, `Soundex.cs`
- Modify: `src/DataNet.Text/Stemming/EnglishSnowballStemmer.cs`,
  `FrenchSnowballStemmer.cs`, `PorterStemmer.cs`
- Modify: `src/DataNet.Text/Vectorization/HashingVectorizer.cs`,
  `CountVectorizer.cs`
- Modify: `src/DataNet.Fuzzy/Deduplicator.cs`
- Modify: `src/DataNet.Embeddings/Onnx/OnnxTextEmbedder.cs`,
  `Tokenization/SentencePieceTokenizer.cs`
- Modify: `tests/DataNet.Fuzzy.Tests/ProcessOracleTests.cs`,
  `tests/DataNet.Text.Tests/Distances/LevenshteinPropertyTests.cs`
- Modify: `bench/DataNet.Text.Benchmarks/*.cs`

**Depends on:** Task 1.

Work **one rule at a time**, and run the corpora after each. A batch of fixes that
moves an oracle gives you no information about which fix did it.

- [x] **Step 1: `S3218` — shadowed members**

`Worker.Stem()` shadows the outer static `Stem(string)` in both Snowball stemmers;
a record property shadows an outer const in a benchmark. Rename the inner ones.

- [x] **Step 2: `S3241` / `S3626` — return values nobody reads**

`Step1`, `Step2a`, `Step2b` return a `bool` no caller reads, which leaves dead
trailing returns. Make them `void`.

- [x] **Step 3: `S3358` — nested ternaries in `Nysiis` and `HashingVectorizer`**

Straightforward here. The stemmer case is **not**, and is Task 3.

- [x] **Step 4: `S6608`, `S8969`, `S125`, `S1192`**

`results.First()` → indexer; drop the null-forgiving operator `Assert.NotNull`
already makes redundant; rewrite the two prose comments that parse as code; hoist
the literals repeated across corpora.

- [x] **Step 5: Corpora after every rule**

```bash
build_all && test_all 2>&1 | tail -3 && oracles_unchanged
```

Expected: 158/158, `ORACLES CLEAN`.

---

### Task 3: The three where the obvious fix is wrong

**Files:**

- Modify: `src/DataNet.Text/Distances/Jaro.cs`
- Modify: `src/DataNet.Text/Stemming/EnglishSnowballStemmer.cs`
- Modify: `src/DataNet.Fuzzy/Deduplicator.cs` (or wherever `S2234` lands)

**Depends on:** Task 2.
**Produces:** the part of this branch a reviewer should read closely.

- [x] **Step 1: `S2184` in `Jaro` — make the intent explicit, do not cast**

The rule sees an `int` division assigned to a `double` and suggests casting an
operand. **That would be a behaviour change.** The count of mismatched positions
is always even, so the halving is exact, and it is exact in jellyfish and
rapidfuzz too.

Fix by typing the intermediate as `int` and adding a comment saying the count is
even by construction. Verify by the corpus, not by reasoning:

```bash
dotnet test -c Release --filter "FullyQualifiedName~Jaro" 2>&1 | tail -3
```

- [x] **Step 2: `S3358` in the stemmer — if/else, not a candidate array**

The tidy fix is a loop over an array of candidates. That allocates on every call,
in a per-token path. Use an if/else chain and say why in a comment; the next
person to read the rule will otherwise re-apply the tidy version.

- [x] **Step 3: `S2234` — rename the locals, do not touch the call**

The symmetry check swaps its arguments **deliberately**. It reads as a mistake
only because the locals mirror the parameter names `a`/`b`. Rename to `x`/`y`/`z`.

If you find yourself editing the call, stop: you are about to delete the assertion
the test exists for.

- [x] **Step 4: Corpora**

```bash
test_all 2>&1 | tail -3 && oracles_unchanged
```

---

### Task 4: Suppressions, each with its reason

**Files:**

- Modify: the phonetic encoders, the stemmers, `MurmurHash3.cs`,
  `TextAnalyzer.cs`, the benchmarks
- Modify: `tools/generate_oracles.py`

**Depends on:** Task 3.

- [x] **Step 1: `S3776` on the rule-engines**

Phonetic encoders and stemmers. Reason: decomposing a published rule-engine into
helpers breaks the 1:1 mapping with the reference, and that mapping is what makes
a divergence auditable. The complexity belongs to the algorithm.

- [x] **Step 2: `S3267` on `TextAnalyzer.Tokenize` — verify before suppressing**

Do not take the spec's word for it. Apply the suggestion and build:

```bash
# temporarily rewrite Tokenize to use Select(m => m.Value), then:
dotnet build -c Release -f netstandard2.0 2>&1 | grep "CS1061"
```

Expected:

```text
error CS1061: 'MatchCollection' does not contain a definition for 'Select'
```

Revert, and put that error text in the suppression comment. A suppression whose
justification is checkable is worth five that are not.

- [x] **Step 3: `S4136`, `S127`, `S907`, `S2245`**

`S907` on the canonical MurmurHash3 tail — written the way the reference is
written on purpose. `S2245` on the seeded RNG in benchmarks and the generator,
where determinism is the requirement, not a risk.

- [x] **Step 4: The Python side uses `# NOSONAR`**

Python has no pragma. `# NOSONAR` applies only to the line it terminates, so place
it precisely rather than at the top of a block.

- [x] **Step 5: Confirm the pragmas do not themselves warn**

```bash
build_all 2>&1 | grep -c "CS1691"
```

Expected: `0`. Unknown pragma ids emit no `CS1691`, which is what makes this safe
under repository-wide warnings-as-errors — but check rather than assume, because
the whole branch rests on it.

---

### Task 5: Full gate

**Depends on:** Task 4.

- [x] **Step 1: Everything**

```bash
dotnet clean -c Release && build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
oracles_unchanged
```

Expected: 0 warnings, 0 errors on both frameworks; 158/158; format clean;
`ORACLES CLEAN`.

- [x] **Step 2: Confirm the SonarLint panel is empty or accounted for**

Every remaining finding must map to a suppression added in Task 4. A finding with
no entry means the inventory in Task 1 was incomplete.

- [x] **Step 3: Note what needed no work**

`S3903` is absent from the list because multi-targeting (#1) already moved the
shared helpers into `DataNet.Internal`. Say so in the PR body — a reader comparing
the backlog to the diff will otherwise look for it.

- [x] **Step 4: Commit**

```bash
git add -A
git commit -m "Static-analysis cleanup (SonarLint), no behavior change"
```

The message claims no behaviour change. `ORACLES CLEAN` is what backs the claim.
