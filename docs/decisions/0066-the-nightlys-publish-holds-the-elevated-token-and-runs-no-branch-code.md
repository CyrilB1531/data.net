# 0066 — The nightly's publish holds the elevated token, and runs no branch code

**Status:** accepted · **Date:** 2026-08-31

## Context

`bench-nightly.yml` builds its results branch with `git checkout -B` from the measured ref's own
tip, so the new ref carries that ref's whole tree. GitHub compares a newly created ref's
`.github/workflows/` against the default branch's, so a measured branch that touched a workflow
file makes the push a workflow write — which `GITHUB_TOKEN` cannot do without `workflows: write`.
Run 43 discovered that after two hours of measurement:

```text
! [remote rejected] bench/nightly-perf-433-warm-heap-measurement-2026-08-29
  (refusing to allow a GitHub App to create or update workflow
   `.github/workflows/bench-ondemand.yml` without `workflows` permission)
```

[#464](https://github.com/CyrilB1531/lodestar/issues/464) listed three fixes and asked which. The
permission itself is not in question — the publish genuinely performs a workflow write. What is in
question is where it lives.

## Decision

**The publish moves into a second job that holds `workflows: write` and runs nothing from the
repository.**

`bench-nightly.yml` was one job, and that job runs the measured branch's own code: `dotnet run` on
`bench/`, `python bench/corpus/generate_vocabs.py`, `python bench/compare.py`. Granting it
`workflows: write` would put a CI-rewriting token in a job whose purpose is to execute code a
branch author wrote. Splitting costs about forty lines and an artifact hand-off, and buys a
`publish` job whose every step is a pinned action, `git` or `gh`.

`permissions:` therefore moves off the workflow and onto each job — `measure` keeps
`contents: write` for the wiki push and loses `pull-requests: write`, which only the publish used.

`tools/tests/test_bench_nightly_permissions.py` holds the property rather than the diff: exactly
one job may declare `workflows: write`, it must invoke no `python`, `dotnet`, `pip`, `node` or
`npx`, its `uses:` must be pinned to a commit, and the workflow must declare no permissions block
of its own — a workflow-level grant would reach `measure` again, which is the whole escalation.

## Options refused

**`workflows: write` on the workflow (#464's option 1).** One line, and it hands the token to the
job running branch code. That is the escalation the split exists to avoid, and it is invisible
afterwards: nothing in the file would say why the permission is there.

**Exclude `.github/**` from the results branch (#464's option 2).** It does not survive the
mechanism. The rejection is not about the commit the nightly makes — that commit adds two Markdown
files — but about the new ref's workflow tree differing from main's. Excluding therefore means
restoring main's version on the results branch, and the pull request would show the measured
branch's workflow changes being reverted, then revert them on merge.

**A PAT (#464's option 3).** It works, adds a secret to rotate, and moves the permission out of a
reviewed file into repository settings, where it is neither visible in review nor scoped to one
job.

## Consequences

- A nightly on a branch touching `.github/workflows/` publishes instead of dying after the
  expensive part.
- The pull request now opens after the wiki push rather than before. Both read the same tree and
  neither depends on the other; the wiki publish is a side-channel gated to `main` (#367).
- A failed push says so in its own words, naming the permission and stating that the measurement
  succeeded — #464's closing complaint, that a permissions failure reads like a benchmark failure.
- `pyyaml` becomes a declared dependency in `tools/requirements.txt` rather than one inherited
  through `huggingface-hub`. The test above parses YAML, and a test that skips itself when its
  parser is absent is a check that goes green on silence.
