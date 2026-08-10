# #70 Pin `sonar.projectBaseDir` — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make #68's exclusions unable to fail open on a future scanner upgrade, by pinning the base directory they resolve against — **without re-keying a single issue**.

**Architecture:** One scanner property, whose value is read from the log of an actual run rather than reasoned about, so the change is provably a no-op today.

**Tech Stack:** GitHub Actions, `dotnet-sonarscanner`, SonarQube Cloud.

**Spec:** `2026-08-06_0070_pin-sonar-projectbasedir-so-the-relative-exclusions-cannot-fail-open.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `chore/70-pin-sonar-project-base-dir`. Never commit to `main`.
- **The pinned value must equal what the scanner already resolves.**
  `sonar.projectBaseDir` determines file keys; changing it closes and reopens
  every issue as new and disturbs blame attribution.
- **Read the value from a log. Do not infer it.** This is the whole verification.
- No change to the exclusion patterns.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

parse() { python3 -c "import yaml; yaml.safe_load(open('.github/workflows/sonarcloud.yml'))" && echo OK; }
```

---

### Task 1: Read the base directory the scanner is already using

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the value to pin, and the evidence that pinning it changes nothing.

- [ ] **Step 1: Find the most recent successful analysis run**

```bash
gh run list --workflow sonarcloud.yml --limit 5
```

- [ ] **Step 2: Extract the base directory from its log**

```bash
gh run view <id> --log | grep -i "Base dir"
```

Expected:

```text
Base dir: /home/runner/work/data.net/data.net
```

- [ ] **Step 3: Confirm that equals `$GITHUB_WORKSPACE`**

```bash
gh run view <id> --log | grep -i "GITHUB_WORKSPACE\|working-directory" | head
```

If the two differ, **stop**. Pinning a different value re-keys the project, and
that is a much larger decision than this branch.

- [ ] **Step 4: Read the scanner's own warning, and quote it in the PR**

> Starting with Scanner for .NET v8 the way the `sonar.projectBaseDir` property is
> automatically detected has changed and this has an impact on the files that are
> analyzed and other properties that are resolved relative to it like
> `sonar.exclusions` and `sonar.test.exclusions`.

The scanner is telling us this has already moved once. That is the argument for
this branch, in the tool's own words.

---

### Task 2: Pin it

**Files:**

- Modify: `.github/workflows/sonarcloud.yml`

**Depends on:** Task 1.

- [ ] **Step 1: Add the property**

```yaml
/d:sonar.projectBaseDir="$GITHUB_WORKSPACE" \
```

- [ ] **Step 2: `$GITHUB_WORKSPACE`, not a literal and not `.`**

The runner guarantees it, and it does not depend on the shell's working directory
when the scanner starts. A literal path breaks on a self-hosted runner; `.`
reintroduces the ambiguity being removed.

- [ ] **Step 3: Comment what it anchors**

In the style of the two comments already in that step. Name the failure it
prevents: without it, a scanner upgrade that moves the base makes
`tests/oracles/**` match nothing, the exclusions **fail open**, and 4.5 MB of
corpora re-enter analysis behind a green job.

- [ ] **Step 4: Parse check**

```bash
parse
```

---

### Task 3: Confirm it is a no-op

**Depends on:** Task 2.

- [ ] **Step 1: Compare the base directory in the new run's log**

```bash
gh run view <new-id> --log | grep -i "Base dir"
```

Expected: byte-identical to Task 1's value.

- [ ] **Step 2: Confirm nothing was re-keyed**

The issue count and their creation dates must be unchanged on the dashboard. A
wave of "new" issues dated today means the base moved, and the change must be
reverted rather than accepted.

- [ ] **Step 3: Confirm the exclusions still apply**

The `Invalid character encountered` warning must stay absent and the indexed file
count unchanged. That is what proves the patterns still resolve.

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/sonarcloud.yml
git commit -m "Anchor the Sonar exclusions to an explicit base directory"
```
