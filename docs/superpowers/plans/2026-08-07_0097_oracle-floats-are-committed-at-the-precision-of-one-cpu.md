# #97 Round the oracle floats — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop the corpora recording which BLAS kernel the runner had — twelve significant digits through one helper, proven stable across three kernels rather than across one green run.

**Architecture:** A single `stable()` in `tools/generate_oracles.py` that every float goes through. Three corpora regenerate. The digit count is derived from the observed spread and the existing test tolerances, not chosen.

**Tech Stack:** Python 3, NumPy / OpenBLAS (`OPENBLAS_CORETYPE`), scikit-learn.

**Spec:** `2026-08-07_0097_oracle-floats-are-committed-at-the-precision-of-one-cpu.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `fix/97-round-oracle-floats`. Never commit to `main`.
- **Nothing about what the metrics compute changes.** Only how many digits are
  written.
- **The rounding must not be able to move an assertion.** Prove that against the
  tolerances already in the tests.
- Run the generator from a neutral working directory and read its own exit code.

### Reusable verification commands

```bash
cd <repo>

regen_with_kernel() {   # $1 = OPENBLAS_CORETYPE
  cd /tmp
  OPENBLAS_CORETYPE="$1" PYTHONSAFEPATH=1 \
    <repo>/.venv-oracles/bin/python \
    <repo>/tools/generate_oracles.py
  local rc=$?
  cd <repo>
  echo "kernel=$1 generator exit: $rc"
  return $rc
}
```

---

### Task 1: Measure the spread before choosing a digit count

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the arithmetic that makes "twelve" a derivation rather than a
preference.

- [x] **Step 1: Regenerate under two different kernels and diff**

```bash
regen_with_kernel Haswell && cp -r tests/oracles /tmp/or-haswell
git checkout tests/oracles
regen_with_kernel Nehalem && diff -r /tmp/or-haswell tests/oracles | head -20
git checkout tests/oracles
```

- [x] **Step 2: Characterise the differences**

Expected: always the **last bit**, so the absolute spread scales with the value —
~1e-13 on `accuracy_count` near 413, ~1e-16 on knn scores near 0.4.

That is the same sixteenth digit in both. **Only a significant-digit rule catches
both with one threshold**; a fixed decimal count over-rounds one and under-rounds
the other.

- [x] **Step 3: Read the tolerances the tests already use**

```bash
grep -rn "Tolerance" tests/ --include='*.cs' | head
```

Expected: `MetricsCorpus.Tolerance = 1e-9`, `EmbeddingIndexTests.Tolerance = 1e-4f`.

- [x] **Step 4: Do the arithmetic**

Twelve significant digits leaves **four orders above the observed spread** and
costs at most **5e-13** against a `1e-9` tolerance. The rounding cannot move an
assertion, and that is a computation rather than a hope.

---

### Task 2: One helper, every float through it

**Files:**

- Modify: `tools/generate_oracles.py`

**Depends on:** Task 1.

- [x] **Step 1: Add `stable()`**

Twelve significant digits, one implementation.

- [x] **Step 2: Route all twelve bare `float(...)` sites through it**

```bash
grep -n "float(" tools/generate_oracles.py | wc -l
```

Miss one and the corpus it writes keeps failing, intermittently, on a different
day.

- [x] **Step 3: Include `roc_auc.json`, which has not drifted yet**

It is the same scikit-learn reduction written the same way. **Leaving it out is
choosing the date of the next red rather than avoiding it.**

- [x] **Step 4: Comment the reasoning at the helper**

Significant digits and not decimals, and why twelve. Both are the kind of number
someone will later "tidy".

---

### Task 3: Regenerate, and be explicit about the diff

**Files:**

- Modify: `tests/oracles/classification_metrics.json`, `knn.json`, `roc_auc.json`

**Depends on:** Task 2.

- [x] **Step 1: Regenerate**

```bash
regen_with_kernel Haswell
git diff --stat -- tests/oracles/
```

Expected: three corpora, a large line count.

- [x] **Step 2: Confirm the diff is empty in meaning**

```bash
git diff -U0 -- tests/oracles/ | grep -E "^[+-]\s+\"" | head -20
```

Every changed line must drop digits and nothing else. **A reviewer seeing
thousands of changed lines in an oracle corpus is right to be alarmed by
default**, so say this in the pull request and show the sample.

- [x] **Step 3: The suite still passes**

```bash
dotnet test -c Release 2>&1 | tail -3
```

---

### Task 4: Prove stability across kernels, which is the actual criterion

**Depends on:** Task 3.

- [x] **Step 1: Regenerate under three `OPENBLAS_CORETYPE` values**

```bash
for k in Haswell Nehalem Prescott; do
  git checkout tests/oracles
  regen_with_kernel "$k" && cp -r tests/oracles "/tmp/or-$k"
done
diff -r /tmp/or-Haswell /tmp/or-Nehalem && diff -r /tmp/or-Haswell /tmp/or-Prescott && echo "IDENTICAL ACROSS KERNELS"
```

Expected: byte-identical.

**The acceptance criterion is stability across kernels, not a single green run** —
one green run proves only that the current runner agrees with itself.

- [x] **Step 2: Commit**

```bash
git add tools/generate_oracles.py tests/oracles/
git commit -m "Commit the metric, not the machine that computed it"
```
