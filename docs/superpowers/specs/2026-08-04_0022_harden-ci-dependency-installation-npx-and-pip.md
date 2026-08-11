# Design — #22: harden CI dependency installation

**Date:** 2026-08-04 · **Issue:** #22 · **Branch:** `chore/22-harden-ci-dependencies` ·
**Checkout:** `<repo>`

## Problem

Three SonarQube Cloud findings on `ci.yml`, all the same class: **CI executes
arbitrary code fetched from the network at build time.**

- **`S6505` — `npx --yes markdownlint-cli2`** resolves the *latest* version on
  demand and runs its lifecycle scripts. The code executed in CI can change with
  no commit here: a compromised release or a typosquat runs with the job's
  permissions.
- **`S8541` — `pip install -r tools/requirements.txt`** without
  `--only-binary :all:` may build a source distribution, executing `setup.py` from
  the downloaded package.
- **`S8544` — `tools/requirements.txt` uses ranges**, so CI does not install the
  same thing twice.

## Why the third one matters more here than elsewhere

In a typical project, unpinned dependencies are a reproducibility annoyance. Here
they are a **correctness** problem, because these libraries are not merely build
inputs: **the committed corpora under `tests/oracles/` are their output**, and the
`Oracles are reproducible` job regenerates and diffs them on every pull request.

Under a range, a silent minor release of `nltk` or `rapidfuzz` lands as an
unexplained oracle diff on an unrelated pull request — blaming the wrong commit,
and costing whoever is on that branch an afternoon.

## Decisions

### D1 — Pin markdownlint and disable lifecycle scripts

`npx --yes --ignore-scripts markdownlint-cli2@0.23.2`. Both halves are needed: the
pin fixes *what* runs, `--ignore-scripts` fixes *whether it runs code on install*.

### D2 — `--only-binary :all:`, after verifying it does not break the job

Verified in a clean virtualenv that **all eight dependencies publish wheels** for
the CI platform: `rapidfuzz`, `jellyfish`, `textdistance`, `scikit-learn`, `nltk`,
`tokenizers`, `numpy`, `sentencepiece`.

Had any been sdist-only, the flag would have *broken* the job rather than hardened
it. This is exactly the kind of change that gets copied from a checklist and lands
red, so it is tested rather than assumed.

### D3 — Pin to the versions currently resolved

Deliberately. This keeps the change a **security** one and not a behavioural one —
the same reasoning as pinning the actions in #24.

```text
rapidfuzz==3.14.5   jellyfish==1.2.1     textdistance==4.6.3  scikit-learn==1.9.0
nltk==3.10.1        tokenizers==0.23.1   numpy==2.5.1         sentencepiece==0.2.2
```

Proven rather than assumed: a clean virtualenv built from the pinned file must
regenerate the corpora **byte-identically**, so no corpus update is bundled in and
the `oracles-fresh` job stays green.

### D4 — The remaining gap is stated, not quietly left

**Transitive dependencies are still unpinned.** Full hash-pinning
(`--require-hashes`) requires `pip-compile` to enumerate the whole resolved graph,
which is a larger change deserving its own review.

Say so in the pull request and open the follow-up. A partial hardening described
as complete is worse than a partial hardening described as partial — it stops
anyone from finishing it.

## Out of scope

- `--require-hashes` and the generated lock file (the follow-up D4 creates, later
  #38).
- Action SHA pinning (#24) and workflow injection (#21).

## What "done" means

markdownlint pinned with lifecycle scripts disabled; `pip install` using
`--only-binary :all:` with wheel availability verified; exact version pins;
corpora byte-identical; the transitive-dependency gap recorded and tracked.
