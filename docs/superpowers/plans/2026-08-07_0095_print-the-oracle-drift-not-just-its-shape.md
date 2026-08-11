# #95 Print the oracle drift — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make a drift failure diagnosable from its own output — the changed values in the log, the regenerated corpora kept as an artefact — without changing what the gate accepts.

**Architecture:** Two additions to the `Oracles are reproducible` job: a capped `git diff -U0` after the stat, and an `upload-artifact` step conditioned on failure.

**Tech Stack:** GitHub Actions, `git diff`, `actions/upload-artifact`.

**Spec:** `2026-08-07_0095_print-the-oracle-drift-not-just-its-shape.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `fix/95-print-oracle-drift`. Never commit to `main`.
- **The gate must accept exactly what it accepted before.** A diagnostics change
  that also relaxes a criterion is two changes, and the second would be invisible.
- **Do not fix the drift here.** That is a separate issue, made possible by this
  one.
- Demonstrate on a deliberately drifted corpus. A reporting change verified only
  on a green run is unverified.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

parse() { python3 -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml'))" && echo OK; }
```

---

### Task 1: Confirm what the failure currently tells you

**Files:** none modified.

**Depends on:** nothing.

- [x] **Step 1: Read one of the three recent failures**

```bash
gh run list --workflow ci.yml --limit 20 --json databaseId,conclusion,headBranch \
  --jq '.[] | select(.conclusion=="failure") | "\(.databaseId) \(.headBranch)"'
gh run view <id> --log-failed | grep -A10 "Oracles"
```

Expected: a three-line `--stat` summary, and nothing about the values.

- [x] **Step 2: Note that it failed three times in one morning**

Twice on #94, once on `main` (`0db78d1`), always with the same summary — with **no
way to tell whether the cause was the same each time**.

- [x] **Step 3: Read the current step**

```bash
grep -n -A20 "Regenerate oracles" .github/workflows/ci.yml
```

---

### Task 2: Print the values

**Files:**

- Modify: `.github/workflows/ci.yml`

**Depends on:** Task 1.

- [x] **Step 1: Keep the stat, and add the diff after it**

The stat names the corpus that moved; only the values say why.

- [x] **Step 2: `-U0`, with the reason in a comment**

The corpora are **one value per line**. Context lines carry nothing and the
changed values are the whole message.

- [x] **Step 3: Cap at 400 lines**

So a wholesale regeneration cannot bury the log. Say in the comment that the
artefact covers that case.

- [x] **Step 4: Do not touch the criterion**

```bash
git diff .github/workflows/ci.yml | grep -E "^[+-].*(git diff --quiet|exit 1)"
```

The condition that decides pass or fail must be byte-identical.

---

### Task 3: Keep the evidence off the runner

**Files:**

- Modify: `.github/workflows/ci.yml`

**Depends on:** Task 2.

- [x] **Step 1: Upload `tests/oracles/` on failure, 14-day retention**

- [x] **Step 2: Say why in a comment**

The runner is thrown away with everything that would let the failure be
reproduced. **Drift has already turned out to depend on which CPU the job landed
on**, which no amount of log-reading settles.

This is what turns a legible failure into a reproducible one, and it is the half
that matters when the cause is environmental.

---

### Task 4: Demonstrate it on a real failure

**Depends on:** Task 3.

- [x] **Step 1: Drift a corpus on purpose**

```bash
python3 - <<'EOF'
import json
p = 'tests/oracles/classification_metrics.json'
d = json.load(open(p))
# perturb one value in the last digit
EOF
git commit -am "TEMPORARY: drift one value"
git push
```

- [x] **Step 2: Read the failing run**

Expected: the `::error::`, the stat, then the changed values under
`--- first 400 changed lines ---`, and the `oracles-as-regenerated` artefact
present.

- [x] **Step 3: Download the artefact and confirm it is usable**

```bash
gh run download <id> -n oracles-as-regenerated -D /tmp/drift
diff -u tests/oracles/classification_metrics.json /tmp/drift/classification_metrics.json | head
```

- [x] **Step 4: Revert the deliberate drift and confirm green**

- [x] **Step 5: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "Print the oracle drift, not just its shape"
```

- [x] **Step 6: Open the follow-up the output now makes possible**

The example values — `413.626` against `413.6259999999999` — are a last-digit
float difference, not a behavioural change. That is a separate issue, and this
branch is what makes it diagnosable.
