# #36 Ungroup major action updates — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Major action bumps arrive as individual pull requests that can be reviewed and accepted one at a time, while minor and patch updates keep arriving as one weekly group.

**Architecture:** One `update-types` key on the existing group in `.github/dependabot.yml`, verified against the parsed configuration rather than the file text.

**Tech Stack:** Dependabot configuration, GitHub Actions.

**Spec:** `2026-08-04_0036_dependabot-keep-major-action-updates-out-of-the-grouped-pr.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `chore/36-dependabot-major-updates`. Never commit to `main`.
- **Change no action version.** This changes how updates are *delivered*, not what
  is installed.
- Do not act on #32 in this branch.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

show_group() {
  python3 -c "
import yaml
d = yaml.safe_load(open('.github/dependabot.yml'))
for u in d['updates']:
    print(u['package-ecosystem'], u.get('groups'))
"
}
```

---

### Task 1: Establish why grouping majors is wrong here

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the argument the pull request needs, which is stronger than
"ergonomics".

- [x] **Step 1: Look at what #32 actually contained**

```bash
gh pr view 32 --json title,body --jq .body | head -20
```

Expected: six **major** jumps in one pull request. It can only be accepted or
rejected as a block.

- [x] **Step 2: Find the workflows no pull request ever exercises**

```bash
grep -l -E "on:\s*$" -r .github/workflows/ >/dev/null
grep -n -A6 "^on:" .github/workflows/release.yml .github/workflows/release-nuget-org.yml
```

Expected: tag- and dispatch-triggered only. **No pull request runs them.**

That is the real argument: a major bump to `checkout`, `setup-dotnet` or
`upload-artifact` in those files is unverified by CI and surfaces at publish time
— the most expensive and least recoverable moment.

---

### Task 2: Restrict the group

**Files:**

- Modify: `.github/dependabot.yml`

**Depends on:** Task 1.

- [x] **Step 1: Add `update-types`**

```yaml
groups:
  actions:
    patterns: ['*']
    update-types: [minor, patch]
```

- [x] **Step 2: Comment the reason, naming the two unexercised workflows**

Otherwise this reads as a preference and someone will regroup it for the quieter
inbox.

- [x] **Step 3: Verify the parsed configuration, not the text**

```bash
show_group
```

Expected: the group resolving to `['minor', 'patch']`. The schema is easy to get
subtly wrong, and **a mistyped key is ignored silently** — leaving the old
behaviour behind a file that looks fixed.

---

### Task 3: Confirm on the next run

**Depends on:** Task 2.

- [x] **Step 1: Watch the next scheduled Dependabot run**

```bash
gh pr list --author "app/dependabot"
```

Expected: majors as individual pull requests, minor and patch still grouped. If
majors still arrive grouped, the key did not take effect — re-read the parsed
output rather than the file.

- [x] **Step 2: Commit**

```bash
git add .github/dependabot.yml
git commit -m "Keep major action updates out of the grouped Dependabot PR"
```
