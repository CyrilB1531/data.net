# #30 Declare the Python version to SonarQube Cloud — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Python analysis targets 3.12 explicitly, so version-dependent rules are evaluated instead of suppressed — with the value tied to the version CI actually installs.

**Architecture:** One scanner argument in `sonarcloud.yml`, with a comment naming `ci.yml` as the value it must track.

**Tech Stack:** GitHub Actions, `dotnet-sonarscanner`.

**Spec:** `2026-08-04_0030_set-sonar-python-version-so-python-analysis-is-version-precise.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `chore/30-sonar-python-version`. Never commit to `main`.
- **Declare what runs, not what is newest.** A version the job does not install is
  worse than none — it is confidently wrong.
- No change to any Python code or to the installed version.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

parse() { python3 -c "import yaml; yaml.safe_load(open('.github/workflows/sonarcloud.yml'))" && echo OK; }
```

---

### Task 1: Confirm which version actually runs

**Files:** none modified.

**Depends on:** nothing.

- [ ] **Step 1: Read it from the workflow rather than assuming**

```bash
grep -n -A2 "setup-python" .github/workflows/ci.yml
```

Expected: `python-version: '3.12'` in the `Oracles are reproducible` job.

- [ ] **Step 2: Check no other job installs a different one**

```bash
grep -rn "python-version" .github/workflows/
```

If two jobs disagree, the scanner argument cannot be right for both, and that
disagreement is the real finding.

---

### Task 2: Pass the parameter

**Files:**

- Modify: `.github/workflows/sonarcloud.yml`

**Depends on:** Task 1.

- [ ] **Step 1: Add it to the scanner `begin` step**

```text
/d:sonar.python.version="3.12"
```

- [ ] **Step 2: Comment the coupling**

The value tracks `python-version` in `ci.yml`; if that moves, this moves with it.
Without the comment the two drift silently — and the failure mode is invisible,
because a version *is* set and the warning stays gone while the analysis is wrong.

- [ ] **Step 3: Parse check**

```bash
parse
```

---

### Task 3: Gate, and be honest about what is unverified

**Depends on:** Task 2.

- [ ] **Step 1: Say in the pull request what was verified locally**

The YAML parses. That is all that can be checked here.

- [ ] **Step 2: Say what cannot be**

**Whether the warning clears only shows on the next analysis.** Do not claim the
outcome; confirm it on the dashboard once merged.

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/sonarcloud.yml
git commit -m "Tell SonarQube Cloud which Python version the code targets"
```
