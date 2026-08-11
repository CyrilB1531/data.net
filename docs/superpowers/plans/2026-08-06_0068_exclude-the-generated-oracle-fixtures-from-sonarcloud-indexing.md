# #68 Exclude the oracle fixtures from Sonar indexing — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop the scanner decoding binary fixtures as UTF-8, by excluding `tests/oracles/**` from indexing — with the cause diagnosed from the bytes rather than guessed.

**Architecture:** Two scanner properties in `sonarcloud.yml`, each carrying its reason in a comment.

**Tech Stack:** GitHub Actions, `dotnet-sonarscanner`.

**Spec:** `2026-08-06_0068_exclude-the-generated-oracle-fixtures-from-sonarcloud-indexing.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `chore/68-exclude-oracle-fixtures-from-sonar`. Never commit to `main`.
- **Do not touch the fixtures.** They are correct; the scanner's treatment of them
  is not.
- **Do not change `sonar.sourceEncoding`.** That would be treating a binary-read-as-text
  problem as an encoding problem.

### Reusable verification commands

```bash
cd <repo>

parse() { python3 -c "import yaml; yaml.safe_load(open('.github/workflows/sonarcloud.yml'))" && echo OK; }
```

---

### Task 1: Diagnose from the bytes

**Files:** none modified.

**Depends on:** nothing.
**Produces:** certainty about which of three plausible causes it is.

- [x] **Step 1: Confirm the files are binary**

```bash
file tests/oracles/tiny_sp.model tests/oracles/tiny_encoder.onnx
```

Expected: `data` for both.

- [x] **Step 2: Find the first undecodable byte in each**

```bash
python3 -c "
for f in ('tests/oracles/tiny_sp.model','tests/oracles/tiny_encoder.onnx'):
    b = open(f,'rb').read()
    for i in range(len(b)):
        try: b[:i+1].decode('utf-8')
        except UnicodeDecodeError as e:
            if e.start == i: print(f, 'first undecodable byte:', i); break
"
```

Expected: 54 and 3.

- [x] **Step 3: Read the bytes at the reported line**

Expected: `03 e2 96 81 15 4f bf 3b c0 0a`. `e2 96 81` is valid — it is `▁`
(U+2581), the SentencePiece meta symbol. The `bf` after it is a continuation byte
with no lead byte.

**This is not a corrupt fixture and not a wrong `sourceEncoding`.** It is a binary
file being read as text, and the byte sequence is what proves it.

- [x] **Step 4: Confirm nothing excludes them from indexing**

```bash
grep -n "sonar\." .github/workflows/sonarcloud.yml
```

Expected: `sonar.coverage.exclusions` present, `sonar.exclusions` **absent**.
Coverage exclusion does not stop indexing.

- [x] **Step 5: Establish that it is pre-existing**

```bash
git log --oneline --diff-filter=A -- tests/oracles/tiny_sp.model tests/oracles/tiny_encoder.onnx
```

Worth stating in the pull request: the warning is unrelated to whichever branch
happens to surface it.

---

### Task 2: Exclude the directory

**Files:**

- Modify: `.github/workflows/sonarcloud.yml`

**Depends on:** Task 1.

- [x] **Step 1: Both properties**

```text
/d:sonar.exclusions="tests/oracles/**"
/d:sonar.test.exclusions="tests/oracles/**"
```

- [x] **Step 2: The whole directory, not the two files**

Beyond the binaries it holds megabytes of generated JSON corpora, machine-written
and reviewed as diffs. Naming two files leaves the next fixture to reintroduce the
warning.

- [x] **Step 3: A comment giving the reason, in the style of the adjacent one**

An exclusion with no reason gets deleted while tidying, and the warning comes back
attached to an unrelated change months later.

- [x] **Step 4: Parse check**

```bash
parse
```

---

### Task 3: Confirm, and notice what this makes load-bearing

**Depends on:** Task 2.

- [x] **Step 1: Read the next analysis log**

Expected: the `Invalid character encountered` warnings gone, and the indexed file
count down by the size of `tests/oracles/`.

A `WARN` never fails a job, so this can only be confirmed by reading the log — not
by a green tick.

- [x] **Step 2: Note the new dependency, and open the follow-up**

These patterns are resolved **relative to whatever the scanner decided is the base
directory**, which is now load-bearing and is not pinned. That becomes its own
issue immediately.

- [x] **Step 3: Commit**

```bash
git add .github/workflows/sonarcloud.yml
git commit -m "Stop Sonar reading the binary fixtures as text"
```
