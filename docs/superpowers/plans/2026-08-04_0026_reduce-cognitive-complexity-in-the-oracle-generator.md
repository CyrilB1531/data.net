# #26 The complexity finding on the oracle generator — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Answer `python:S3776` on `tools/generate_oracles.py:263` correctly — which is to suppress it with a written reason, not to refactor, because the issue's premise that this is "our own glue code" is false.

**Architecture:** Read the function first, establish what it actually is, then suppress with `# NOSONAR` and the reasoning in the docstring — consistent with how the same rule was answered for the same algorithm in C# in #7. Verified by regenerating the corpora with zero drift.

**Tech Stack:** Python 3, SonarQube Cloud, `# NOSONAR`.

**Spec:** `2026-08-04_0026_reduce-cognitive-complexity-in-the-oracle-generator.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `chore/26-suppress-jaro-reference-complexity`. Never commit to `main`.
- **Do not restructure `_jaro_reference`.** Task 1 establishes why; if Task 1's
  finding turns out to be wrong, stop and re-open the design rather than
  proceeding either way.
- **Zero corpus drift.** This is a comment-only change; anything else in
  `git diff -- tests/oracles/` means something went wrong.
- Run the generator from a neutral working directory and **read its own exit
  code**.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

# Neutral working directory, and the exit code read directly — not through a pipe.
regen() {
  cd /tmp
  PYTHONSAFEPATH=1 /home/cyril/Documents/devs/data.net/.venv-oracles/bin/python \
    /home/cyril/Documents/devs/data.net/tools/generate_oracles.py
  local rc=$?
  cd /home/cyril/Documents/devs/data.net
  echo "generator exit: $rc"
  return $rc
}

oracles_unchanged() {
  test -z "$(git status --porcelain tests/oracles/)" && echo "ORACLES CLEAN" || echo "ORACLES MOVED"
}
```

---

### Task 1: Read the function the issue describes, before doing what it asks

**Files:** none modified.

**Depends on:** nothing.
**Produces:** either a confirmation of the issue's premise, or its refutation. The
rest of the plan assumes refutation, because that is what the code shows.

An issue is a hypothesis, including one you wrote yourself.

- [ ] **Step 1: Read it**

```bash
sed -n '250,300p' tools/generate_oracles.py
```

Expected: `_jaro_reference` — a transcription of the published Jaro algorithm:
match window, then transposition count. **Not glue code.**

- [ ] **Step 2: Find its C# counterpart and how the same rule was answered there**

```bash
grep -n -B4 "S3776" src/DataNet.Text/Distances/Jaro.cs
```

Expected: `Jaro.SimilarityCore`, with `S3776` suppressed in #7 and the reason
given — decomposing a published algorithm breaks the one-to-one mapping with the
reference that makes a divergence auditable.

The issue claimed that defence "does not apply" here. It is the same algorithm, in
the other language.

- [ ] **Step 3: Establish why the argument is *stronger* on this side**

This function **generates the reference data every other component is validated
against**. So "the tests still pass" is circular — the tests compare against
exactly this output. A restructuring that silently changed a corpus would be
invisible to the suite designed to catch such changes.

Code that produces the oracle cannot be validated by the oracle. Write that down;
it is the finding.

- [ ] **Step 4: Check whether any other `S3776` in this file *is* glue code**

```bash
grep -n "^def \|^    def " tools/generate_oracles.py | head -40
```

If one exists, it is a real finding and deserves its own issue — do not fold it in
here.

---

### Task 2: Suppress, with the reasoning where it will be read

**Files:**

- Modify: `tools/generate_oracles.py`

**Depends on:** Task 1.

- [ ] **Step 1: `# NOSONAR` on the right line**

Python has no pragma. `# NOSONAR` applies **only to the line it terminates**, so
it goes on the `def` line — not at the top of the block, where it would silently
cover nothing.

- [ ] **Step 2: The reasoning in the docstring, not the commit message**

Name the algorithm, name the C# counterpart and its suppression, and state the
circularity argument from Task 1 Step 3. A commit message is not where the next
reader of this function looks.

- [ ] **Step 3: Nothing else changed**

```bash
git diff --stat tools/generate_oracles.py
```

Expected: a handful of lines, all comment or docstring. Any executable line in the
diff is out of scope.

---

### Task 3: Prove zero drift, and read the exit code

**Depends on:** Task 2.

- [ ] **Step 1: Regenerate**

```bash
regen && oracles_unchanged
```

Expected: `generator exit: 0` then `ORACLES CLEAN`.

- [ ] **Step 2: If the generator fails, do not read anything into a clean diff**

The likely failure, hit while verifying this very change:

```text
ImportError: Blocked import of regex from current working directory for security reasons
```

`nltk` refuses to import its dependencies when they appear to live under the
current directory. Running from `/home/cyril` fails **even with `PYTHONSAFEPATH`
set**; running from `/tmp` with the virtualenv inside the repository works.

A green-looking "no drift" after a failed generator run proves nothing — nothing
was regenerated. This is why the exit code is read directly rather than through a
pipe.

- [ ] **Step 3: Full gate**

```bash
dotnet build -c Release && dotnet test -c Release 2>&1 | tail -3
oracles_unchanged
```

- [ ] **Step 4: Correct the record in the pull request**

State plainly that the issue asked for the wrong thing and why. Quietly doing
something different from what an issue asked leaves the issue's reasoning
standing, and the next person applies it again.

- [ ] **Step 5: Commit**

```bash
git add tools/generate_oracles.py
git commit -m "Suppress the complexity finding on the Jaro reference implementation"
```
