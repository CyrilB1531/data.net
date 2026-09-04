# 0078 — keybert is declared `--no-deps`, not compiled into the lock

**Status:** accepted · **Date:** 2026-09-04

## Context

[#525](https://github.com/CyrilB1531/lodestar/issues/525)'s `Mmr` oracle replays
`keybert._mmr.mmr` — the MMR selection function alone, one call among the several `keybert` the
package ships. `tools/requirements.txt` is the human-edited input `pip-compile` resolves into
`tools/requirements.lock.txt`, the hash-pinned graph every CI job installs from
(`CONTRIBUTING.md`'s *Dependencies*), and the plan this issue started from assumed keybert would
join it the same way rake-nltk and summa did for Tasks 2 and 6.

Measured before writing the generator: `keybert` 0.9.0 declares `sentence-transformers` as a
dependency, and through it `torch` and `transformers` — several hundred megabytes, native wheels
included, pulled in for one function. `keybert._mmr.mmr` itself imports nothing but `numpy` and
`scikit-learn`, both already pinned in `requirements.lock.txt` for the metrics oracles. `pip-compile`
has no per-package `--no-deps`: declaring `keybert` in `requirements.txt` resolves and pins its
whole dependency closure into the one lock file five CI jobs install from, two of them benchmark
workflows (`bench/README.md`) that call no Python at all.

A second, unrelated package landed in the same file for a different reason, caught by the
whole-branch review of [#525](https://github.com/CyrilB1531/lodestar/issues/525) before it was
ever pushed: `summa` 1.2.0 — the TextRank oracle's reference — publishes **only an
sdist** on PyPI, no wheel at all. Measured: `pip install --dry-run --only-binary :all:
summa==1.2.0` finds no candidate. All five of this repository's install sites pass
`--only-binary :all:`, so `summa==1.2.0` in `requirements.txt` — compiled into the lock like every
other oracle dependency — would fail the install at all five, on the first push. `summa`'s own
`install_requires` is `scipy >= 0.19` alone, and its source imports nothing beyond that and `numpy`
(checked against the sdist), both already pinned in `requirements.lock.txt` through `scikit-learn`
and directly, respectively — the same closure test `keybert._mmr` passes below.

## Decision

**`keybert` is declared in `tools/requirements-nodeps.txt`, hash-pinned, and installed with
`pip install --no-deps --require-hashes` — outside `requirements.lock.txt`, by the *Oracles are
reproducible* job alone, immediately after the lock is installed.** `--no-deps` skips dependency
resolution entirely, so `sentence-transformers` is never resolved, downloaded, or pinned anywhere;
`keybert._mmr.mmr`'s own imports are satisfied because `numpy` and `scikit-learn` are already on the
lock's own graph.

The exception this carves is bounded, not "add oracle dependencies where they're annoying to
compile": **a package may live in `requirements-nodeps.txt` only when everything it imports is
already pinned in `requirements.lock.txt`.** `keybert._mmr` qualifies on exactly that test. `keybert`
as a whole would not — `keybert.KeyBERT`, the class most callers reach for, imports
`sentence-transformers` directly, so declaring the package rather than naming the one module this
oracle calls would reopen the same closure `--no-deps` exists to avoid pulling in, the moment
anything imports the top-level package instead of the submodule.

**`summa` is declared in the same file, for a different reason: not a dependency-closure problem
but the total absence of a wheel.** It is hash-pinned to its sdist, and the *Oracles are
reproducible* job's install of this file gains one more flag, `--no-binary summa`, alongside the
`--only-binary :all:` every install site already carries — pip resolves per-package binary/source
overrides, and the more specific one wins, so `summa` alone is allowed to build from source while
every other package in both requirements files, at every install site including this one, is still
wheels-only. This is the one place in the repository's whole dependency graph where a `setup.py`
executes: `summa`'s own (checked, not assumed — see Context) `install_requires` is `scipy >= 0.19`
alone, satisfied entirely by what `scikit-learn` and this file's own `numpy` pin already resolve, so
the code that setup.py hands off to is not fetching or building anything the lock does not already
account for.

## What enforces it

Nothing mechanical — this is a convention a reviewer checks, not a script. A second entry in
`requirements-nodeps.txt` that imports something outside `requirements.lock.txt`'s graph would
install successfully and fail only when that import actually runs, inside the one CI job that
installs the file. A package with no wheel, like `summa`, would fail louder — at every other install
site's `--only-binary :all:` — the moment it landed in `requirements.txt` instead of here, which is
exactly what the whole-branch review of #525 caught before the branch was ever pushed.
`CONTRIBUTING.md`'s *Dependencies* section states the invariant and links here.

## Options considered

**Compile `keybert==0.9.0` into `requirements.lock.txt` like every other oracle dependency** —
the uniform path, and the one the plan assumed. Refused: it pins `sentence-transformers`, `torch`
and `transformers` at every one of the lock's five install sites, two of them benchmark workflows
(`bench/README.md`) with no Python step at all, for a dependency graph one function of one oracle
generator needs.

**Vendor a minimal reimplementation of `keybert._mmr.mmr`** — no install at all, any version. Refused
for the same reason [`Mmr.Select`](../reference/embeddings/search/mmr-select.md) itself is not
derived from reading `keybert`'s source under
[ADR 0003](0003-provenance-and-licensing.md): the oracle has to call the actual reference
implementation, not a re-derivation of it, or a bug shared between the two would read as agreement.

**A separate virtual environment for `keybert` alone, outside `pip-compile` entirely** — sidesteps
the lock, but reopens exactly what the lock exists to close: an unpinned, unhashed install that a
transitive bump can change without anyone noticing, for the one generator every other oracle in this
repository already trusts the lock to protect.

**Compile `summa==1.2.0` into `requirements.lock.txt` like every other oracle dependency** —
refused for a reason distinct from keybert's: it is not that `summa` pulls in an oversized
dependency graph, it is that no wheel exists to compile hashes for. Every one of the lock's five
install sites passes `--only-binary :all:`, so this would fail all five outright, on the first push
of the branch that added it — which is exactly what happened, caught only by review because the
branch had never been pushed.

**Weaken `--only-binary :all:` at all five install sites to serve `summa` alone** — refused: the
other four sites keep the invariant "no source distribution, so no `setup.py` executes" for every
package they install, `summa` included where they encounter it via the shared lock (they do not —
`summa` moved out of the lock precisely so they never see it), and weakening a repository-wide
guarantee to accommodate one package in one file is the wrong direction: the fix scopes the
exception to the one job and the one package that need it, not the other way around.
