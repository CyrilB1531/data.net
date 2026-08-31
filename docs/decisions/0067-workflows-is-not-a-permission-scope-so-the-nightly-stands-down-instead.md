# 0067 — `workflows` is not a permission scope, so the nightly stands down instead

**Status:** accepted · **Date:** 2026-08-31 ·
**Amends** [`0066`](0066-the-nightlys-publish-holds-the-elevated-token-and-runs-no-branch-code.md)

## Context

[Decision 0066](0066-the-nightlys-publish-holds-the-elevated-token-and-runs-no-branch-code.md)
answered [#464](https://github.com/CyrilB1531/lodestar/issues/464) by moving the nightly's publish
into a job that holds `workflows: write` and runs no repository code. Its reasoning about *where*
the permission should live stands. Its premise — that the permission can be declared at all — does
not.

GitHub refused the merged file outright:

```text
Invalid workflow file: .github/workflows/bench-nightly.yml#L1
(Line: 310, Col: 7): Unexpected value 'workflows'
```

`workflows` is **not** one of GitHub's `permissions:` scopes. The run-43 rejection that opened
issue #464 — *"refusing to allow a GitHub App to create or update workflow `bench-ondemand.yml` without
`workflows` permission"* — names a **GitHub App** permission, granted where the App is installed,
not a token scope a workflow can ask for. 0066 read one as the other.

The cost was worse than the bug it fixed. An unknown key is a parse error, so it does not fail one
job: it takes the whole workflow out of service, scheduled runs included, and every other job in
the file with it.

## Decision

**`GITHUB_TOKEN` cannot create a ref whose `.github/workflows/` differs from the default branch's,
and no `permissions:` block can change that. The nightly detects the case before the push and
publishes to the job summary instead.**

The guard is one `git diff` against `origin/main`, placed before anything is staged. On `main` it
is always false and the behaviour is exactly what it was. On a ref that touches
`.github/workflows/` the run writes both pages into `$GITHUB_STEP_SUMMARY`, emits a `::notice::`
saying why there is no pull request, and exits 0 — measured, readable, and not a red cross.

The `nightly-pages` artifact 0066 introduced carries the same two pages, so nothing is only in a
summary.

0066's split survives, on its own smaller merit: `publish` runs pinned actions, `git` and `gh` and
nothing from the measured branch, and `measure` holds `contents: write` alone. That is worth
keeping whether or not an elevated token was ever available.

`tools/tests/test_bench_nightly_workflow.py` checks **every** `permissions:` block in
`.github/workflows/` against GitHub's scope list. That test fails on the exact file that merged,
which is what it exists for: a key outside the set is not a narrower grant, it is a workflow that
does not run.

## Options refused

**A PAT with `workflow` scope (#464's option 3).** Still the only way to make that push succeed,
and still refused for its own reasons: a secret to rotate, and a permission moved out of a reviewed
file into repository settings. Standing down costs a pull request on a minority of runs; a PAT
costs a credential forever.

**Reverting 0066 and deleting it.** `docs/decisions/README.md` is explicit — a decision record is
not edited, and an amendment is its own record. This is that record. 0066 stays on `main` saying
what it said, and its row points here.

**Skipping the nightly on such refs.** The measurement is the point; it is the *publish* that
cannot happen. Refusing to measure would throw away the run this decision exists to save.

## Consequences

- A ref touching `.github/workflows/` gets its numbers in the job summary and the artifact, and no
  pull request. That is the one thing lost, and it is lost to a platform rule rather than a choice.
- No workflow in this repository can merge an unknown permission key again.
- `main`'s `bench-nightly.yml` is valid again; between 0066 merging and this landing, every job in
  that file was disabled, scheduled runs included.
