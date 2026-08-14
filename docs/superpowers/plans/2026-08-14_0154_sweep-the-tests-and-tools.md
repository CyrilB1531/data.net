# #154 — Sweeping the tests and tools Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Goal:** Bring the 316 over-budget comment blocks in `tests/`, `tools/`, `bench/` and `samples/` inside the
rule — without deleting the reason a test case exists, and without moving a byte of any corpus.

**Architecture:** One task per zone, largest first. Each block gets one of three outcomes — cite what
answers it, run it once and cite the output, or cut it as the opinion it is — and what outgrows its budget
moves to the consuming function's docstring or to `tools/README.md`, never into a corpus.

**Tech Stack:** C# inline comments and XML documentation, Python comments and docstrings,
`tools/check_comment_length.py`, `tools/count_cited_claims.py`.

**Spec:** `docs/superpowers/specs/2026-08-14_0154_sweep-the-tests-and-tools.md`

## Global Constraints

- Branch `docs/154-sweep-tests-and-tools`, rebased on `main` at `166b935`. Do not push, do not open a pull
  request without asking.
- **No behaviour changes, and no corpus moves.** `git status --porcelain tests/oracles/` stays empty after
  every task, and the suite stays at **3 185 passing, 0 failed** across eight assemblies. A moved corpus byte
  means the lot changed evidence rather than prose, which is the one thing a sweep of the test zone must
  never do.
- **Every `dotnet` invocation goes through `./.dotnet-guarded`**, never bare `dotnet`.
- `dotnet build` gives no analyzer diagnostics without `--no-incremental`. Warnings are errors.
- Budgets: **two lines** inline, **eight lines of prose** in XML documentation. Two things are already
  exempt and must not be tidied into scope: **the reason above a `#pragma`** (since #151) and **a Python
  docstring**, which the counter does not treat as a comment block at all.
- **Never explain a corpus case inside the corpus.** Adding a field regenerates it. The explanation goes to
  the generator function's docstring.
- **Write no ADR and take no ADR number.** A block holding a real undocumented decision is reported.
- `dotnet format DataNet.slnx --verify-no-changes` runs **once**, in the final task.
- Run `python3 tools/extract_doc_snippets.py` if `samples/` or a guide changes, and markdownlint if any
  Markdown changes.
- English everywhere. Commit messages carry no `feat:`/`fix:` prefix and no process prefix.

## How to triage one block

1. **Read what it claims** — what the reference does, what the corpus proves, or what the code does.
2. **Ask what would check it.** In this zone the cheap tier is usually the corpus the test already replays:
   name the file and the case. Next is an ADR, then a `file:line`.
3. **If it is executable and nothing frozen answers it**, run it once and cite the output.
4. **If nothing reasonable checks it**, it is an opinion: cut it, or rewrite it as one.
5. **Then fit the budget.** What survives and does not fit goes to the consuming function's docstring or to
   `tools/README.md`, and the block keeps one line naming where it went.

**The exception this zone turns on.** A comment explaining **why a case exists** — the shape the test was
written to catch — is not a restatement of the assertion below it. It shortens and keeps the corpus case
named. A test whose reason for existing is deleted is a test the next person deletes as redundant.

## Per-task shape

1. **List your blocks**: `python3 tools/check_comment_length.py | grep '<your prefix>'`.
2. **Triage and edit** by the five rules above.
3. **Verify**: `./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental` (0 warnings), then
   `./.dotnet-guarded dotnet test DataNet.slnx -c Release` — **3 185 passing** — and
   `git status --porcelain tests/oracles/` empty.
4. **Confirm** the same `grep` prints nothing for your files.
5. **Commit**, naming what moved and where, and any claim found false.

---

### Task 1: `tools/generate_oracles.py` — 57 blocks

**Files:** `tools/generate_oracles.py`, `tools/README.md`.

**Depends on:** nothing. First because it is the file every corpus comes from, and its comments explain why
each corpus holds what it holds.

- [ ] **Step 1: The docstring route, where it applies**

Measured: **none of the 57 sits above a `def` or a `class`**, so the issue's proposed escape does not apply
directly. They sit above module constants (23), above statements (11), inside literals (9), on section
banners (5) and above assertions (5).

For the 23 constants: each feeds one or two generators, and this file already explains its corpora in those
generators' docstrings. Move the rationale there and leave the constant a one-line comment naming what it is.

- [ ] **Step 2: The nine inside literals are the per-case explanations, and they are the trap**

They say what a single corpus case is for. **Do not move them into the corpus** — that means a new field,
which means regenerating, which means bytes move. Shorten them, or lift them into the generator's docstring
as a sentence per case, whichever keeps the reason findable.

- [ ] **Step 3: `tools/README.md` already has a `## generate_oracles.py` section**

Conventions and traps belong there — the map #156 published, applied to this file. Read the section first;
some of what these blocks say may already be in it, in which case the block cites rather than repeats.

- [ ] **Step 4: Regenerate and prove nothing moved**

```bash
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py > /tmp/154-gen.log 2>&1
echo "generate=$?"
cd <repo> && git status --porcelain tests/oracles/
```

Empty is the requirement. A comment edit that moves a corpus byte is a bug in the edit.

- [ ] **Step 5: Verify and commit** per the per-task shape.

```bash
git commit -m "Sweep the oracle generator, and move what explains a corpus into the docstring that owns it"
```

---

### Task 2: `tests/DataNet.Metrics.Tests` — 78 blocks

**Files:** the test files the counter names under that directory.

**Depends on:** Task 1.

- [ ] **Step 1: Expect the common shape to be a block restating its own assertion**

That is what this zone's size is made of. A comment that repeats what `Assert.Equal(expected, actual)` says
is the tier-3 case: cut it.

- [ ] **Step 2: Keep what says why the case exists**

`WeightedPercentileMedianTests` (7 blocks) is the type case: its shapes — all equal, sorted, reverse sorted,
two distinct values, organ pipe — exist because a partition scheme gets them wrong, and its comment records
which of them can detect a wrong rank and which only catch a hang. **That is not a restatement.** Shorten it,
keep the distinction, name the corpus or the issue.

- [ ] **Step 3: `RocAucParallelTests` (9 blocks) claims concurrency behaviour**

Those are executable claims about worker counts and determinism. Where nothing frozen answers one, run it
once and cite the output; where ADR 0018 answers it, cite the ADR after opening it.

- [ ] **Step 4-5: Verify and commit** per the per-task shape.

```bash
git commit -m "Sweep the metrics tests, keeping what says why each case exists"
```

---

### Task 3: `tests/DataNet.Embeddings.Tests` — 67 blocks

**Files:** the test files the counter names, `TokenizerJsonLoaderTests.cs` first at 15 blocks.

**Depends on:** Tasks 1-2.

- [ ] **Step 1: This is the zone six lots have edited this week**

Issue #118, #119, #120, #121, #122, #130, #143, #145 and #149 all landed tests here. Expect claims that were true
of an earlier design — #152 found three in the matching `src/` files, and #153 found two more.
**A claim found false is fixed and named in the report**, not reformatted.

- [ ] **Step 2: The corpora are thick here, so the cheap tier is nearly always available**

`tests/oracles/` holds a corpus per behaviour. A comment asserting what HuggingFace does should cite the
corpus file and the case number rather than restating the measurement.

- [ ] **Step 3-5: Verify and commit** per the per-task shape.

```bash
git commit -m "Sweep the embeddings tests onto the corpora they already replay"
```

---

### Task 4: `bench/` and `samples/` — 48 blocks

**Files:** `bench/` (28) and `samples/` (20).

**Depends on:** Tasks 1-3.

- [ ] **Step 1: These answer to a different reader, so tier 3 applies more often**

`samples/` is read by someone learning the API and compiled against the published packages (ADR 0009);
`bench/` by someone reproducing a measurement. Their comments explain the example and the protocol, not the
library's behaviour. A block that philosophises about the library belongs to neither.

- [ ] **Step 2: `bench/`'s methodology claims point at the guide**

Issue #156 made `docs/guides/performance.md` the single home for measurements, and moved `bench/README.md`'s
result table there. A `bench/` comment asserting a measured number cites the guide; one explaining the
harness stays.

- [ ] **Step 3-5: Verify and commit** per the per-task shape. `samples/` changes mean
      `python3 tools/extract_doc_snippets.py` must still pass.

```bash
git commit -m "Sweep the benchmarks and the sample, whose readers are not the library's"
```

---

### Task 5: `tools/`'s other scripts and `tools/tests/` — 48 blocks

**Files:** `tools/` scripts other than `generate_oracles.py` (26), `tools/tests/` (22).

**Depends on:** Tasks 1-4.

- [ ] **Step 1: These are Python, so the budget is two lines and the docstring is free**

Most of these scripts open with a thirty-line docstring on purpose — that is the module's documentation and
the counter does not touch it. A `#` block that has outgrown two lines usually belongs in that docstring,
one scroll above it.

- [ ] **Step 2: `tools/tests/` asserts what the guard under test does**

Its comments should cite the guard's own docstring or the issue that set the rule, not restate the
assertion. `test_check_machine_paths.py` alone holds 13 blocks.

- [ ] **Step 3-5: Verify and commit** per the per-task shape, plus
      `.venv-oracles/bin/python -m pytest tools/tests -q`.

```bash
git commit -m "Sweep the tools and their tests into the docstrings above them"
```

---

### Task 6: `tests/DataNet.Text.Tests` and the last block — 18 blocks

**Files:** `tests/DataNet.Text.Tests` (17), `tests/DataNet.Fuzzy.NetStandard.Tests` (1).

**Depends on:** Tasks 1-5.

- [ ] **Step 1: The stemmer and phonetic tests carry provenance too**

ADR 0003 makes a comment tracing a rule to the published algorithm description into evidence, and #153 kept
eight such traces in `src/DataNet.Text`. The tests that exercise those rules can carry the same kind of
comment: **shorten, keep the reference to the published description, never rewrite it as "matches nltk".**

- [ ] **Step 2: Confirm the whole zone is empty before committing**

```bash
python3 tools/check_comment_length.py | grep -E '^(tests|tools|samples|bench)/'   # prints nothing
```

If it prints a file that is not yours, say so rather than fixing it silently.

- [ ] **Step 3-5: Verify and commit** per the per-task shape.

```bash
git commit -m "Sweep the text tests, and the last block in the zone"
```

---

### Task 7: Final verification

**Depends on:** Tasks 1-6.

- [ ] **Step 1: The issue's "done when", and the claims counter**

```bash
cd <repo>
python3 tools/check_comment_length.py | grep -E '^(tests|tools|samples|bench)/'   # nothing
python3 tools/check_comment_length.py | wc -l                                      # 331 - 316 = 15
python3 tools/count_cited_claims.py tests tools bench samples                      # was 112 blocks, 33 cited (29%)
```

The second is the number that says whether the sweep cited or merely shortened. Report it either way.

- [ ] **Step 2: Every gate**

```bash
git status --porcelain                                                                     # empty
./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental > /tmp/154-fv-b.log 2>&1; echo "build=$?"; grep -E "Avertissement\(s\)|Erreur\(s\)" /tmp/154-fv-b.log
./.dotnet-guarded dotnet format DataNet.slnx --verify-no-changes > /tmp/154-fv-f.log 2>&1;  echo "format=$?"
./.dotnet-guarded dotnet test DataNet.slnx -c Release > /tmp/154-fv-t.log 2>&1;             echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/154-fv-t.log
python3 tools/check_version_floor.py; python3 tools/check_machine_paths.py
.venv-oracles/bin/python -m pytest tools/tests -q | tail -1
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md" > /dev/null; echo "markdownlint=$?"
python3 tools/extract_doc_snippets.py | tail -2
```

Then pack and build both samples under an isolated `NUGET_PACKAGES`, and regenerate the oracles from a
neutral directory: `git status --porcelain tests/oracles/` must be empty.

- [ ] **Step 3: The evidence**

Every fact that moved with its new home; every block cut; every claim found false; every block marked
`long-comment:` with its reason. **And, specific to this zone: every comment that explained why a test case
exists, with what became of it** — that is the one this lot could destroy without any gate noticing.

- [ ] **Step 4: Stop and report.** Do not push, do not open a pull request.

---

## Self-Review

**Spec coverage.** D1 → the six zone tasks, largest first. D2 → Task 1 Steps 1-3 and the standing "never
explain a case inside the corpus" constraint. D3 → the triage's exception, restated in Task 2 Step 2.
D4 → Task 4. D5 → the Global Constraints. D6 → step 3 of every task and Task 7 Step 2.

**Placeholders.** Each task names what is specific to it — the file whose escape route had to be found, the
zone six lots have edited, the readers who are not the library's. `<repo>` stands for a path that must not be
written into a committed file.

**Type consistency.** No code changes. The counts come from `check_comment_length.py` on `main` at `166b935`
and sum to 316: 57 + 78 + 67 + 48 + 48 + 18.
