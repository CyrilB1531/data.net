# Design — #38: lock the resolved Python dependency graph with hashes

**Date:** 2026-08-04 · **Issue:** #38 · **Branch:** `chore/38-hashed-python-lock` ·
**Checkout:** `<repo>`

## Problem

`githubactions:S8544` on `ci.yml:85` **survived #22**, which pinned every direct
dependency to an exact version.

That is correct behaviour from the rule, not a false positive. `==` pins fix *what
we asked for*; they say nothing about what actually installs underneath.
`nltk==3.10.1` still pulls `regex`, `joblib`, `click` and `tqdm` at whatever
resolves on the day, and `scikit-learn` pulls `scipy` and `threadpoolctl`.

| | before | after |
| --- | --- | --- |
| pinned | 8 direct | **29** — all direct + transitive |
| integrity | none | per-artefact hashes, `--require-hashes` |

Twenty-one packages floating behind eight pinned ones.

## Why this matters here specifically

The same reason the direct pins did, only sharper: **the committed corpora under
`tests/oracles/` are the output of these libraries**, and CI regenerates and diffs
them on every pull request. A transitive bump moves that output, and the failure
lands on an unrelated change.

`regex` is the sharp case — it is a transitive dependency of `nltk`, and **nltk's
tokenization is exactly what several corpora capture**.

## Decisions

### D1 — A generated lock file, with `requirements.txt` kept as the human input

```bash
pip-compile --generate-hashes --strip-extras \
  --output-file tools/requirements.lock.txt tools/requirements.txt
```

The lock is generated and **never hand-edited**. Two files with two jobs: one
states intent, one records the resolution.

### D2 — CI installs from the lock, with hashes enforced

```yaml
pip install --only-binary :all: --require-hashes -r tools/requirements.lock.txt
```

`--require-hashes` refuses anything not matching a recorded hash, closing the
unlocked-transitive hole and the substituted-artefact hole together. It composes
with `--only-binary :all:` from #22 — hashes are per-artefact.

### D3 — Verified byte-identically, or it is not a security change

A clean virtualenv installed from the lock with hashes enforced must regenerate
the corpora **byte-identically**. If they drift, the lock has captured different
transitive versions than the ones that produced the committed JSON — and that must
be resolved deliberately rather than committed as noise.

Same standard as #22 and #24: a security change with no behavioural component.

### D4 — Two traps found while verifying, both documented rather than remembered

Both cost real time and both are **silent** failures:

- **`nltk` refuses to import its own dependencies when they appear to live under
  the current directory**, so the generator fails whenever the working directory
  is an *ancestor* of the virtualenv — even with `PYTHONSAFEPATH` set. `/tmp` with
  the venv in the repository works; the repository root or `~` does not.
- **`python … | tail` reports `tail`'s exit code.** A failed generation then reads
  as success, and the drift check that follows proves nothing, because nothing was
  regenerated. This was hit while verifying this very change, and a verification
  that had not happened was nearly recorded.

Both go in `CONTRIBUTING.md`, next to the oracle procedure.

### D5 — If the lock is not worth its maintenance, say so instead of pretending

The honest alternative is to mark the finding "Won't fix" in SonarQube Cloud with
the reasoning recorded. What is **not** acceptable is leaving the direct pins and
claiming the finding is addressed — they do not close it.

## Out of scope

- Upgrading any dependency. The lock captures what resolves today.

## What "done" means

`tools/requirements.lock.txt` generated with hashes; CI installing from it with
`--require-hashes`; corpora byte-identical from a clean virtualenv; the
regeneration command and both traps documented in `CONTRIBUTING.md`.
