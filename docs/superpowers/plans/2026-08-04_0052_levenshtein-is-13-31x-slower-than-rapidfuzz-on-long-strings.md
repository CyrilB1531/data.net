# #52 Blocked Myers — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `Levenshtein.Distance` stops falling off a cliff above 64 characters — blocked Myers at every length, implemented from the published description, and **proven to actually execute** rather than assumed from a green suite.

**Architecture:** Rule out a badly-written DP by measurement first. Then multi-word Myers with horizontal deltas carried word to word. Then the part that matters most: the corpus does not currently reach the new path at all, so it gains two Latin-1 long families — appended, never inserted, so the RNG stream and the existing 1 241 cases are untouched.

**Tech Stack:** C# (net10.0 + netstandard2.0), BenchmarkDotNet, rapidfuzz 3.14.5 for cross-language comparison, the `levenshtein` oracle corpus.

**Spec:** `2026-08-04_0052_levenshtein-is-13-31x-slower-than-rapidfuzz-on-long-strings.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `perf/52-blocked-myers`. Never commit to `main`.
- **ADR 0003 — provenance.** Implement from Myers (JACM 1999). **Never transcribe
  a copyleft implementation.**
- **The 1 241 existing corpus cases keep their id and value.** New cases are
  appended; nothing is inserted.
- **A green suite is not coverage.** Task 4 exists because the suite passes
  without executing the new code, and it is not optional.
- Both frameworks build; before/after numbers name the machine.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
test_all()  { dotnet test -c Release; }
test_lev()  { dotnet test -c Release --filter "FullyQualifiedName~Levenshtein"; }

bench_lev() { dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*Levenshtein*'; }
```

---

### Task 1: Rule out the cheap explanation, and write the result down

**Files:** none modified (scratch benchmarks only).

**Depends on:** nothing.
**Produces:** the evidence that the gap is algorithmic — which is what licenses the
rest of the branch.

- [x] **Step 1: Reproduce the gap**

```bash
python3 bench/compare.py 2>&1 | tail -20
```

Expected: roughly 13× behind at 128, 31× at 512.

- [x] **Step 2: Try to fix it *without* changing the algorithm**

A char-specialised DP with bounds checks elided through refs. Measure ns/cell.

Expected:

```text
generic  Dp<char> : 3.50 ns/cell
char-specialised  : 3.97 ns/cell
```

**Slower.** A scalar rolling-row DP is already at its floor.

- [x] **Step 3: Do the arithmetic that settles it**

rapidfuzz's effective 0.08 ns/cell is unreachable without computing 64 cells per
word operation. The gap is algorithmic, never micro-architectural.

Record this in the pull request and in ADR 0004. Without it, someone re-runs this
experiment in six months.

---

### Task 2: Blocked Myers

**Files:**

- Modify: `src/DataNet.Text/Distances/Myers.cs`
- Modify: `src/DataNet.Text/Distances/Levenshtein.cs`

**Depends on:** Task 1.

- [x] **Step 1: Remove the 64-character cap**

```bash
grep -n "MyersMaxPatternLength\|MyersMinPatternLength" src/DataNet.Text/Distances/*.cs
```

- [x] **Step 2: Bit vectors spanning `⌈m/64⌉` words**

Horizontal deltas carried word to word, per the published pseudo-code.

- [x] **Step 3: Comment the one subtlety a reader will trip on**

Only the last word's bit at `(m-1) mod 64` moves the score. Bits above it are
never read, so leaving them set is harmless — carries propagate upward only. This
looks like a bug on first reading and will be "fixed" without the comment.

- [x] **Step 4: Both targets build**

```bash
build_all
```

Suppress `S3776` on the kernel if it fires, with the reason the other published
algorithms use — and as a separate commit.

---

### Task 3: Run the suite, and do not believe it

**Depends on:** Task 2.

- [x] **Step 1: Run everything**

```bash
test_all 2>&1 | tail -3
```

Expected: 168/168 green.

- [x] **Step 2: Do not proceed on that basis**

Green here means the existing cases still pass. It says nothing about whether the
code written in Task 2 ran even once. Task 4 is what answers that.

---

### Task 4: Find out whether the new path is executed at all

**Files:** none modified yet.

**Depends on:** Task 3.
**Produces:** the finding that makes this branch trustworthy.

- [x] **Step 1: Count what the corpus actually contains**

```bash
python3 -c "
import json
d = json.load(open('tests/oracles/levenshtein.json'))
cases = d['cases']
long_ = [c for c in cases if max(len(c['a']), len(c['b'])) > 64]
latin = [c for c in long_ if all(ord(ch) < 256 for ch in c['a'] + c['b'])]
print('total cases :', len(cases))
print('pattern > 64:', len(long_))
print('  of which Latin-1 (i.e. actually reaching blocked Myers):', len(latin))
"
```

Expected:

```text
total cases : 1241
pattern > 64: 85
  of which Latin-1 (i.e. actually reaching blocked Myers): 0
```

**Zero.** The corpus's `long` family draws from BMP ranges, so every long case
contains CJK, fails the Latin-1 check, and falls back to the DP. The new path was
never executed, and the suite was green throughout.

- [x] **Step 2: Confirm it independently, not only by arithmetic**

Add a temporary counter or breakpoint in the blocked path and run the suite. It
must never be hit. Then remove it — the corpus fix in Task 5 is the permanent
answer, not instrumentation.

---

### Task 5: Extend the corpus so the path is covered

**Files:**

- Modify: `tools/generate_oracles.py`
- Modify: `tests/oracles/levenshtein.json` (and the other distance corpora it
  shares `build_pairs` with)

**Depends on:** Task 4.

- [x] **Step 1: Append `long_ascii` and `long_latin` families to `build_pairs`**

**Append, never insert.** Appending leaves the RNG stream intact; inserting
renumbers every subsequent case and makes the diff unreadable.

- [x] **Step 2: Regenerate**

```bash
cd /tmp && PYTHONSAFEPATH=1 /home/cyril/Documents/devs/data.net/.venv-oracles/bin/python \
  /home/cyril/Documents/devs/data.net/tools/generate_oracles.py
echo "generator exit: $?"
cd /home/cyril/Documents/devs/data.net
```

- [x] **Step 3: Prove the pre-existing cases did not move**

```bash
python3 -c "
import json, subprocess
old = json.loads(subprocess.check_output(['git','show','HEAD:tests/oracles/levenshtein.json']))
new = json.load(open('tests/oracles/levenshtein.json'))
o, n = old['cases'], new['cases']
print('old:', len(o), 'new:', len(n))
print('prefix identical:', o == n[:len(o)])
"
```

Expected: `prefix identical: True`. **All 1 241 keep their id and value**, so the
added cases are the entire corpus diff.

- [x] **Step 4: Re-count coverage**

```bash
# Task 4 Step 1's command again.
```

Expected: **89 cases** now genuinely reaching blocked Myers.

- [x] **Step 5: They agree with rapidfuzz**

```bash
test_lev 2>&1 | tail -3
```

---

### Task 6: Measure, and state the limits

**Files:**

- Modify: `docs/decisions/0004-levenshtein-myers-backlog.md`
- Modify: `docs/guides/performance.md`

**Depends on:** Task 5.

- [x] **Step 1: Re-run the cross-language comparison**

```bash
python3 bench/compare.py 2>&1 | tail -20
bench_lev 2>&1 | tail -20
```

Expected shape:

| Length | rapidfuzz | before | after | |
| ---: | ---: | ---: | ---: | --- |
| 8 | 183 ns | 38.5 ns | 35.8 ns | 5.1× C# faster |
| 32 | 324 ns | 451 ns | 453 ns | unchanged |
| 128 | 2 693 ns | 36 178 ns | 1 777 ns | **20×**, now 1.5× C# faster |
| 512 | 21 688 ns | 683 581 ns | 20 555 ns | **33×**, now 1.06× C# faster |

- [x] **Step 2: Record the Latin-1 limit with the numbers, not below them**

The equality table is 256 entries, so **CJK and emoji patterns still take the DP
and these figures do not describe them**. A speedup quoted without this is
accurate and misleading at the same time.

- [x] **Step 3: Record the length-32 bucket honestly**

Still 1.4× behind on the single-word path. Different cause; wants its own
measurement rather than a guess.

- [x] **Step 4: Full gate**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
```

- [x] **Step 5: Commit**

```bash
git commit -m "Add blocked Myers so long strings stop losing to rapidfuzz"
git commit -m "Suppress S3776 on the blocked Myers kernel"
```
