# 0046 — `check_adr_immutable.py` runs in CI only, not the pre-commit hook

**Status:** accepted · **Date:** 2026-08-20

## Context

[#399](https://github.com/CyrilB1531/lodestar/issues/399) is what this records. Nothing
enforced that an accepted ADR stays untouched — [decision 0037](0037-the-guards-run-before-the-commit.md)
lists the guards `.githooks/pre-commit` and CI share, and this is a new one:
`check_adr_immutable.py` refuses a pull request that changes a `docs/decisions/NNNN-*.md`
file already present at the pull request's own base commit, addition included.

`tools/tests/test_pre_commit_hook.py` holds the hook and CI to the same guard list, with
two named exceptions — `check_nuspec_dependencies.py` (needs a packed `./artifacts`) and
`check_version_floor.py --check-feed` (reaches nuget.org). Both read something a commit
should not have to wait on. `check_adr_immutable.py` needs neither the network nor a
build, so on that axis alone it would join the hook the way `check_bench_map.py` and
`check_sample_coverage.py` did.

## Decision

**`check_adr_immutable.py` is CI-only, for a different reason than the two guards already
excluded: it needs `--base`, the pull request's own base commit, and a commit made before
a pull request exists has none to name.** `git merge-base HEAD origin/main` is the nearest
local substitute, but it answers "where did this branch diverge from `main`", not "what is
this pull request's base" — the two differ the moment `main` moves after the branch
started, which is routine rather than rare on a repository with several branches in flight
at once (`#379`'s own finding). A guard that passes locally and fails in CI because the two
questions disagreed would cost the round trip decision 0037 exists to avoid, on the guard
built to prevent exactly that kind of silent disagreement.

## Consequences

- `tools/tests/test_pre_commit_hook.py`'s `OFFLINE_EXCLUSIONS` gains `check_adr_immutable`,
  with this decision as the reason a reviewer can check.
- `tools/README.md`'s guard list notes the exclusion inline, next to the guard's own entry.
- A future local check against `git merge-base HEAD origin/main` is not ruled out by this
  decision — it answers a real, related question, just not the one CI asks. It would be its
  own guard, advisory rather than a substitute for this one.
