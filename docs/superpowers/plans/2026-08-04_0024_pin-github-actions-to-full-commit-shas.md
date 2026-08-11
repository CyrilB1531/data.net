# #24 Pin GitHub Actions to commit SHAs — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every third-party action pinned to a 40-character commit SHA with its version in a trailing comment, pinned to **what the tag resolves to today**, plus Dependabot to keep the pins from rotting.

**Architecture:** A mechanical pass over four workflows and 16 references, with two non-mechanical cases — an annotated tag that must be dereferenced, and an action already pinned twice to different SHAs. Verified by an audit rather than by reading. `.github/dependabot.yml` lands in the same branch, because a pin nobody updates is a frozen vulnerability.

**Tech Stack:** GitHub Actions, `gh api`, Dependabot.

**Spec:** `2026-08-04_0024_pin-github-actions-to-full-commit-shas.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `chore/24-pin-actions-to-sha`. Never commit to `main`.
- **Pin, do not upgrade.** Every SHA is what that reference's *current tag*
  resolves to. If a pin corresponds to a newer major version than the tag in the
  file, it is wrong.
- **No behavioural change.** The workflows must do exactly what they did before.
- Stay out of #21's and #22's lanes.

### Reusable verification commands

```bash
cd <repo>

# Every uses: reference and how it is pinned.
list_uses() { grep -rn "uses:" .github/workflows/ | sed 's/^\s*//'; }

# The audit this branch must make return nothing.
audit_pins() {
  grep -rhoE "uses: [^ ]+" .github/workflows/ | sed 's/uses: //' | grep -vE "@[0-9a-f]{40}$" || echo "ALL PINNED"
}

parse_all() {
  for f in .github/workflows/*.yml .github/dependabot.yml; do
    [ -e "$f" ] && python3 -c "import yaml; yaml.safe_load(open('$f'))" && echo "OK $f"
  done
}
```

---

### Task 1: Inventory, and find the two that are not mechanical

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the list, and early warning of the two special cases.

- [x] **Step 1: List every reference**

```bash
list_uses
```

Expected: 16 across four workflows.

- [x] **Step 2: Find the references already pinned**

```bash
audit_pins
```

`sonarcloud.yml` already pins `setup-java`, `checkout` and `cache`. Note the
`checkout` SHA — Task 3 has to reconcile it.

- [x] **Step 3: Resolve every tag to a commit**

```bash
for ref in actions/checkout@v4 actions/setup-dotnet@v4 actions/upload-artifact@v4 NuGet/login@v1; do
  echo -n "$ref -> "
  gh api "repos/${ref%@*}/git/ref/tags/${ref#*@}" --jq '.object.type + " " + .object.sha'
done
```

**Read the `type` field.** A `commit` can be pinned directly; a `tag` is an
annotated tag and must be dereferenced in Task 2.

---

### Task 2: The annotated tag

**Files:** none modified yet.

**Depends on:** Task 1.
**Produces:** the correct SHA for `NuGet/login@v1`, which is the one most likely
to be wrong and the one in the job that mints a publishing key.

- [x] **Step 1: Dereference it**

```bash
TAGSHA=$(gh api repos/NuGet/login/git/ref/tags/v1 --jq '.object.sha')
gh api "repos/NuGet/login/git/tags/$TAGSHA" --jq '.object.type + " " + .object.sha'
```

Expected: `commit <sha>`. **That** is the SHA to pin — not `$TAGSHA`, which is the
tag object's own hash.

- [x] **Step 2: Confirm the commit exists on the action repository**

```bash
gh api repos/NuGet/login/commits/<sha> --jq '.sha'
```

Pinning a tag object's SHA fails in a way that is easy to misread as a network
problem.

---

### Task 3: Pin all 16, and unify the duplicate

**Files:**

- Modify: `.github/workflows/ci.yml`
- Modify: `.github/workflows/release.yml`
- Modify: `.github/workflows/release-nuget-org.yml`
- Modify: `.github/workflows/sonarcloud.yml`

**Depends on:** Task 2.

- [x] **Step 1: Replace each tag with its SHA, version in a trailing comment**

```yaml
- uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
```

The comment is not decoration: it is what makes the file readable and what
Dependabot rewrites.

- [x] **Step 2: Reconcile the two `actions/checkout` pins**

`sonarcloud.yml` pins a different SHA than the tag resolves to today. Two answers
to "which checkout do we run" is the maintenance trap pinning exists to avoid.
Pick one and use it everywhere.

- [x] **Step 3: Confirm no reference moved a major version**

```bash
git diff main -- .github/workflows/ | grep -E "^\+.*uses:" | grep -oE "# v[0-9]+" | sort -u
git diff main -- .github/workflows/ | grep -E "^-.*uses:" | grep -oE "@v[0-9]+" | sort -u
```

The major numbers must match. The tags in use resolve to the v4/v5 series while
the latest releases are v6 and v7 — pinning to the latest would silently make this
an upgrade.

---

### Task 4: Keep the pins honest

**Files:**

- Create: `.github/dependabot.yml`

**Depends on:** Task 3.

- [x] **Step 1: Dependabot for the `github-actions` ecosystem**

Weekly, **grouped into one pull request** rather than one per action.

- [x] **Step 2: Say why in the file**

A pin that is never updated freezes a known-vulnerable version — a real regression
against a mutable tag, which at least receives patches. Dependabot understands SHA
pins and refreshes both the SHA and the version comment.

Without this file, the change trades one risk for another and calls it security.

---

### Task 5: Audit, then gate

**Depends on:** Task 4.

- [x] **Step 1: The audit returns nothing**

```bash
audit_pins
```

Expected: `ALL PINNED`. Sixteen references across four files is exactly the size
where reading them all feels sufficient and is not.

- [x] **Step 2: Everything parses**

```bash
parse_all
```

Expected: five files, all `OK`.

- [x] **Step 3: Commit**

```bash
git add .github/
git commit -m "Pin GitHub Actions to full commit SHAs"
```

- [x] **Step 4: Watch the first Dependabot run**

If it opens one pull request per action instead of a group, the configuration is
wrong and the noise will train everyone to ignore it.
