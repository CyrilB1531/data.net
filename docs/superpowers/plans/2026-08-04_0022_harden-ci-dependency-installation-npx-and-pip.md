# #22 Harden CI dependency installation — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop CI executing code it fetched from the network moments earlier, without changing a single reference value — markdownlint pinned with lifecycle scripts off, pip restricted to wheels, and the eight oracle dependencies pinned to what they already resolve to.

**Architecture:** Three independent hardenings of `ci.yml` and `tools/requirements.txt`. The second is verified against wheel availability before being applied, because the flag breaks the job if any dependency is sdist-only. The third pins to *currently resolved* versions so this stays a security change, proven by regenerating the corpora byte-identically.

**Tech Stack:** GitHub Actions, npx, pip, the eight Python oracle dependencies.

**Spec:** `2026-08-04_0022_harden-ci-dependency-installation-npx-and-pip.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `chore/22-harden-ci-dependencies`. Never commit to `main`.
- **No corpus may move.** `git diff -- tests/oracles/` empty at the end. If a pin
  changes reference output, that is a separate, deliberate decision — not
  something to bundle into a security fix.
- **Pin to what resolves today**, never to the latest. Upgrading is a behavioural
  change and needs its own branch.
- Stay out of #21's and #24's lanes.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

regen() {
  cd /tmp && PYTHONSAFEPATH=1 /home/cyril/Documents/devs/data.net/.venv-oracles/bin/python \
    /home/cyril/Documents/devs/data.net/tools/generate_oracles.py
  echo "generator exit: $?"
  cd /home/cyril/Documents/devs/data.net
}

oracles_unchanged() {
  test -z "$(git status --porcelain tests/oracles/)" && echo "ORACLES CLEAN" || echo "ORACLES MOVED — STOP"
}
```

---

### Task 1: Pin markdownlint and stop it running install scripts

**Files:**

- Modify: `.github/workflows/ci.yml`

**Depends on:** nothing.
**Produces:** the smallest of the three, done first because it cannot break
anything else.

- [x] **Step 1: Record what is currently resolved**

```bash
npx --yes markdownlint-cli2 --version
```

Pin **this** version, not the latest.

- [x] **Step 2: Pin, and disable lifecycle scripts**

```yaml
run: >
  npx --yes --ignore-scripts markdownlint-cli2@0.23.2
  "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
```

Both halves matter: the pin fixes *what* runs, `--ignore-scripts` fixes *whether
code runs at install time*.

- [x] **Step 3: Comment why, in the workflow**

`npx --yes <name>` resolves the latest version on demand and runs its lifecycle
scripts, so the code executed in CI could change with no commit here. A future
reader will otherwise drop the flags as noise.

- [x] **Step 4: Same lint result as before**

```bash
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
```

Expected: 0 issues, exactly as the unpinned invocation gave.

---

### Task 2: Verify wheel availability *before* restricting pip

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the evidence that `--only-binary :all:` hardens the job rather than
breaking it.

Do this before Task 3 touches the workflow. The flag is the kind of thing copied
from a checklist that lands red.

- [x] **Step 1: Clean virtualenv, wheels only**

```bash
python3 -m venv /tmp/wheelcheck
/tmp/wheelcheck/bin/pip install --only-binary :all: -r tools/requirements.txt
echo "exit: $?"
```

Expected: success for all eight — `rapidfuzz`, `jellyfish`, `textdistance`,
`scikit-learn`, `nltk`, `tokenizers`, `numpy`, `sentencepiece`.

- [x] **Step 2: If any fails, stop and report**

An sdist-only dependency means the flag cannot be applied as-is, and the decision
(vendor a wheel, drop the dependency, accept the risk with a reason) belongs to
the maintainer, not to this branch.

- [x] **Step 3: Record the resolved versions**

```bash
/tmp/wheelcheck/bin/pip freeze | grep -iE "rapidfuzz|jellyfish|textdistance|scikit-learn|nltk|tokenizers|numpy|sentencepiece"
```

These are Task 4's pins.

---

### Task 3: Restrict pip to wheels

**Files:**

- Modify: `.github/workflows/ci.yml`

**Depends on:** Task 2.

- [x] **Step 1: Add `--only-binary :all:`**

- [x] **Step 2: Comment what it prevents**

Without it, pip may build a source distribution, which executes `setup.py` from
the downloaded package.

---

### Task 4: Pin the oracle dependencies to what they already resolve to

**Files:**

- Modify: `tools/requirements.txt`

**Depends on:** Task 3.
**Produces:** an `Oracles are reproducible` job that cannot be moved by someone
else's release.

- [x] **Step 1: Replace the ranges with `==`, using Task 2's output**

- [x] **Step 2: State the reasoning in the file itself**

Not general reproducibility: **these libraries' output is committed under
`tests/oracles/`**, and the drift job diffs it on every pull request. A silent
minor release would land as an unexplained oracle diff on an unrelated change.

- [x] **Step 3: Prove the pins change nothing**

```bash
python3 -m venv /tmp/pinned
/tmp/pinned/bin/pip install --only-binary :all: -r tools/requirements.txt
cd /tmp && PYTHONSAFEPATH=1 /tmp/pinned/bin/python /home/cyril/Documents/devs/data.net/tools/generate_oracles.py
echo "generator exit: $?"
cd /home/cyril/Documents/devs/data.net && oracles_unchanged
```

Expected: `generator exit: 0` and `ORACLES CLEAN`.

Read the **generator's** exit code, not a pipeline's — a failed generation
followed by a clean `git status` proves nothing, because nothing was regenerated.

---

### Task 5: State the gap, then gate

**Depends on:** Task 4.

- [x] **Step 1: Record the known limitation in the pull request**

**Transitive dependencies remain unpinned.** `nltk` pulls `regex`,
`scikit-learn` pulls `scipy`, and neither is fixed by this change. Full
hash-pinning needs `pip-compile` to enumerate the graph, which deserves its own
review.

- [x] **Step 2: Open the follow-up**

"Lock transitive Python dependencies with a hashed requirements file." A partial
hardening described as complete stops anyone from finishing it.

- [x] **Step 3: Full gate**

```bash
dotnet build -c Release && dotnet test -c Release 2>&1 | tail -3
dotnet format --verify-no-changes
oracles_unchanged
for f in .github/workflows/*.yml; do python3 -c "import yaml; yaml.safe_load(open('$f'))"; done
```

Expected: clean, `ORACLES CLEAN`, every workflow parsing.

- [x] **Step 4: Commit**

```bash
git add .github/workflows/ci.yml tools/requirements.txt
git commit -m "Harden CI dependency installation"
```
