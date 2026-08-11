# #34 Skip Sonar analysis for Dependabot — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dependabot pull requests stop arriving with a failing `Build and analyze` check, without weakening the fork guard or changing what is analysed on ordinary pull requests.

**Architecture:** One condition on one job. The existing fork guard is **extended**, not replaced — the two cases have different causes and both need covering.

**Tech Stack:** GitHub Actions job conditions, SonarQube Cloud.

**Spec:** `2026-08-04_0034_sonarqube-job-fails-on-every-dependabot-pull-request.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `fix/34-skip-sonar-for-dependabot`. Never commit to `main`.
- **Extend the condition, do not replace it.** Dropping the fork clause reopens
  the hole #19 closed.
- No change to what ordinary pull requests analyse.

### Reusable verification commands

```bash
cd <repo>

parse() { python3 -c "import yaml; yaml.safe_load(open('.github/workflows/sonarcloud.yml'))" && echo OK; }
show_if() { grep -n -A4 "if:" .github/workflows/sonarcloud.yml; }
```

---

### Task 1: Confirm the cause before changing the condition

**Files:** none modified.

**Depends on:** nothing.
**Produces:** certainty that this is a secrets-availability problem and not a
misconfigured token.

- [x] **Step 1: Read the failing run on #32**

```bash
gh run list --workflow sonarcloud.yml --limit 10
gh run view <id> --log-failed | head -20
```

Expected:

```text
The format of the analysis property sonar.token= is invalid
```

An **empty** token, not a wrong one. That distinction decides the fix: a wrong
token would mean rotating a secret.

- [x] **Step 2: Confirm the existing guard passes for Dependabot**

```bash
show_if
```

The guard tests whether the head repository is this one. **Dependabot branches
live in this repository**, so it passes — the guard was written for forks and
Dependabot is a different case with the same symptom.

- [x] **Step 3: Note the consequence, because it shapes the urgency**

Issue #24 added Dependabot so the action SHA pins stay maintained. Every one of those
pull requests now fails a check, and once the Sonar gate becomes required (#12)
they become unmergeable — the automation added to keep the pins safe blocked by
the analysis added two pull requests earlier.

---

### Task 2: Extend the condition

**Files:**

- Modify: `.github/workflows/sonarcloud.yml`

**Depends on:** Task 1.

- [x] **Step 1: Add the actor clause**

```yaml
if: >-
  github.event_name == 'push' ||
  (github.event.pull_request.head.repo.full_name == github.repository &&
   github.actor != 'dependabot[bot]')
```

- [x] **Step 2: Comment why analysis is skipped rather than enabled**

A dependency bump changes no source, so there is nothing for the analysis to say.
Record the rejected alternative too — adding `SONAR_TOKEN` to the repository's
**Dependabot secrets** would let it run, and is only worth doing if analysing
bumps is genuinely wanted.

The fix looks like avoidance. Writing down that it is a choice lets someone
reverse it knowingly.

- [x] **Step 3: Parse check**

```bash
parse && show_if
```

---

### Task 3: Verify the three paths

**Depends on:** Task 2.

- [x] **Step 1: Push to a branch and open an ordinary pull request — analysis runs**

- [x] **Step 2: Confirm the next Dependabot pull request skips, and is not left
      pending**

```bash
gh pr list --author "app/dependabot"
gh pr checks <n>
```

Expected: `Build and analyze` **skipped**, and the pull request mergeable. GitHub
counts a skipped check as satisfied — which is the property that makes this safe
once #12 lands.

- [x] **Step 3: Confirm the fork clause survived**

```bash
show_if
```

Both conditions present. Replacing rather than extending is the likely mistake
here and it is silent until a fork pull request arrives.

- [x] **Step 4: Commit**

```bash
git add .github/workflows/sonarcloud.yml
git commit -m "Skip SonarQube analysis for Dependabot pull requests"
```
