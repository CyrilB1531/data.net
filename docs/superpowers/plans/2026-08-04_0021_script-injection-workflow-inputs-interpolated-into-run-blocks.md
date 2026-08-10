# #21 Workflow script injection — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove every `${{ }}` interpolation of user- or ref-controlled data from inside a `run:` block, so a crafted version string or tag name can no longer execute commands in a job that holds `id-token: write`.

**Architecture:** Values move from the script body into the step's `env:`, where the runner expands them into environment variables and the shell receives quoted data instead of source. Applied to both release workflows, plus the two secrets that are not part of the vulnerability but should not sit on a command line either.

**Tech Stack:** GitHub Actions, `workflow_dispatch` inputs, OIDC / Trusted Publishing.

**Spec:** `2026-08-04_0021_script-injection-workflow-inputs-interpolated-into-run-blocks.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. **Do not tag** — a tag on
  this branch triggers the very workflows being edited.
- Branch `fix/21-workflow-script-injection`. Never commit to `main`.
- **Do not change what the workflows do.** Same packages, same versions, same
  publishing behaviour. A security fix that also alters release behaviour cannot
  be reviewed as either.
- **Do not "fix" this by validating the input.** Validation leaves the class
  intact for the next person who writes an interpolation.
- Stay out of #22's and #24's lanes: no dependency pinning, no action SHAs.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

# Every workflow parses.
parse_all() {
  for f in .github/workflows/*.yml; do
    python3 -c "import yaml,sys; yaml.safe_load(open('$f'))" && echo "OK $f" || echo "FAIL $f"
  done
}

# The audit this branch has to make return nothing.
audit_run_blocks() {
  python3 - <<'EOF'
import re, glob
for f in glob.glob('.github/workflows/*.yml'):
    src = open(f).read()
    for m in re.finditer(r'run:\s*(?:\||>)?[^\n]*(?:\n(?:[ \t]+[^\n]*)?)*', src):
        for e in re.findall(r'\$\{\{[^}]+\}\}', m.group(0)):
            print(f, e.strip())
EOF
}
```

---

### Task 1: Find every occurrence before fixing any

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the complete list — the issue names two, and "two" is an assumption
until checked.

- [ ] **Step 1: Audit every `run:` block in every workflow**

```bash
audit_run_blocks
```

Record every hit and classify each: user-controlled (`inputs.*`),
ref-controlled (`github.ref_name`, `GITHUB_REF_NAME`), secret, or constant.

- [ ] **Step 2: Note which jobs hold dangerous permissions**

```bash
grep -n -B5 "id-token: write" .github/workflows/*.yml
```

This is what turns a Minor into a Blocker: the job exchanges an OIDC token for a
temporary nuget.org publishing key. A hit in a job with `id-token: write` is the
priority; the rest are still fixed.

---

### Task 2: `release-nuget-org.yml`

**Files:**

- Modify: `.github/workflows/release-nuget-org.yml`

**Depends on:** Task 1.

- [ ] **Step 1: Move `inputs.version` into the step environment**

```yaml
- name: Pack
  env:
    VERSION: ${{ inputs.version }}
  run: |
    dotnet pack "$proj" --configuration Release -o artifacts -p:Version="$VERSION"
```

Quote the variable in the shell. An unquoted `$VERSION` reintroduces word
splitting, which is a smaller hole than injection but a hole.

- [ ] **Step 2: Same treatment for `steps.login.outputs.NUGET_API_KEY`**

Not user-controlled, so not part of the vulnerability — but it keeps the secret
off the command line and out of process listings. Label it as hygiene in the pull
request so the threat model stays legible.

- [ ] **Step 3: Parse check**

```bash
parse_all
```

---

### Task 3: `release.yml`

**Files:**

- Modify: `.github/workflows/release.yml`

**Depends on:** Task 2.

- [ ] **Step 1: The same pattern at line 39, from `GITHUB_REF_NAME`**

It looks safer and is not. Tag names are attacker-influenceable in the general
case, and GitHub's own guidance treats `ref_name` as untrusted.

- [ ] **Step 2: `secrets.GITHUB_TOKEN` into `env:` as well**

- [ ] **Step 3: Why both files must change together**

Leaving one unfixed leaves two examples in the repository, one safe and one not.
The unsafe one is the one that gets copied.

---

### Task 4: Prove the class is gone, not just the two instances

**Depends on:** Task 3.

- [ ] **Step 1: The audit returns nothing**

```bash
audit_run_blocks
```

Expected: **no output** for anything user- or ref-controlled. A remaining hit on a
constant is fine; note it so the next reader knows it was considered.

- [ ] **Step 2: Every workflow still parses**

```bash
parse_all
```

- [ ] **Step 3: Confirm the diff changes no behaviour**

```bash
git diff main -- .github/workflows/ | grep -E "^[+-]" | grep -vE "^[+-]{3}" | grep -viE "env:|VERSION|NUGET_API_KEY|GITHUB_TOKEN|\\\$\{\{"
```

Expected: nothing substantive. Same packages, same versions, same publishing
behaviour.

- [ ] **Step 4: Read SonarQube Cloud on the pushed branch**

A green build is not a clean Sonar. The injection finding must be gone before this
is called done.

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/
git commit -m "Pass workflow inputs through the environment, not into the script"
```
