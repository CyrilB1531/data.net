# 0064 — `check_repeated_literals.py` runs in CI only, not the pre-commit hook

**Status:** accepted · **Date:** 2026-08-31

## Context

[Decision 0037](0037-the-guards-run-before-the-commit.md) put CI's offline guards in the
pre-commit hook, and `tools/tests/test_pre_commit_hook.py` holds the two spellings to each other:
every `tools/check_*.py` the workflows run must appear in `.githooks/pre-commit` too, unless it is
named in that test's `OFFLINE_EXCLUSIONS` with a reason.

Two guards are excluded today. `check_nuspec_dependencies.py` reads the `.nuspec` files inside a
packed `./artifacts`, so running it before a commit would mean packing four projects first — 0037
named it. `check_adr_immutable.py` needs `--base`, and a commit made before a pull request exists
has no base to name; [decision 0046](0046-check-adr-immutable-runs-in-ci-only.md) recorded that as
a decision of its own rather than an edit to 0037, which is what 0046 exists to enforce.

`check_repeated_literals.py`, added for [#489](https://github.com/CyrilB1531/lodestar/issues/489),
is the third. It asks before the push the question SonarCloud's S1192 asks after it, and it takes
`--base` for the same reason 0046's guard does.

## Decision

**It is excluded from the hook, for `check_adr_immutable.py`'s reason and one of its own.**

The shared reason is `--base`. A pre-commit hook runs with no pull request and therefore no base
commit; passing it `origin/main` instead would compare against whatever that ref happens to point
at locally, which on a stale clone is neither the branch point nor the merge target.

The reason of its own is what the check reports. `tools/generate_oracles.py` already holds some
140 literals at or over S1192's threshold, mostly JSON keys like `"metadata"` and `"count"`. The
quality gate tolerates them because only new code counts, and this guard is only useful for the
same reason: it reports what a change *adds*. Without a base there is no change to compare, and a
hook that printed 140 standing findings on every commit would be turned off within a day — which
is the outcome 0037 exists to prevent.

Recorded here rather than as an edit to 0037 or 0046, both accepted and immutable, per the rule
0046 itself was written to enforce.

## Consequences

`OFFLINE_EXCLUSIONS` gains a third name, and `test_the_hook_runs_every_offline_guard_ci_runs`
keeps holding the two spellings together for every other guard.

A contributor who wants the answer before pushing runs it by hand —
`python3 tools/check_repeated_literals.py --base origin/main`, which `CLAUDE.md`'s command list
carries — and `--report` prints the standing backlog without failing, for whoever wants to see it
rather than be blocked by it.

This does not weaken 0037. The rule there is that a guard which *can* run offline before a commit
does; this one cannot answer its question without a base, and answering it wrongly is worse than
leaving it to the pull request.
