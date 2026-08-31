# 0464 — The nightly publishes from a job that runs no branch code

**Issue:** [#464](https://github.com/CyrilB1531/lodestar/issues/464) ·
**Status:** accepted · **Date:** 2026-08-31

## Problem

Nightly run 43, dispatched on `perf/433-warm-heap-measurement`, measured the whole suite and then
failed on its last step:

```text
! [remote rejected] bench/nightly-perf-433-warm-heap-measurement-2026-08-29
  (refusing to allow a GitHub App to create or update workflow
   `.github/workflows/bench-ondemand.yml` without `workflows` permission)
```

`bench-nightly.yml` builds its results branch with `git checkout -B "$branch"` from the measured
ref's own tip, so the new ref carries that ref's whole tree — `.github/workflows/` included.
GitHub compares a newly created ref's workflow files against the default branch's, so a measured
branch that touched a workflow file makes the push a workflow write, and `GITHUB_TOKEN` cannot do
that without `workflows: write`.

It costs a whole nightly window each time, because the rejection lands after the measurement.
**Any** branch touching `.github/workflows/` has the same fate; #461 was merely the first to put
one on a measured branch.

## What the fix has to protect

Issue #464 lists three options and calls option 1 — `workflows: write` on the workflow — "the smallest
diff, and the worst blast radius". That reading is right, and it is right for a reason worth
stating precisely, because it is what decides the shape of the fix.

`bench-nightly.yml` is one job today, and that job **runs the measured branch's own code**:
`dotnet run` on `bench/`, `python bench/corpus/generate_vocabs.py`, `python bench/compare.py`, all
from the ref that was dispatched. A `workflows: write` token on that job is a token any branch
author can reach, in a job whose purpose is to run their code. That is the escalation — not the
permission itself, which the publish genuinely needs.

Option 2 ("exclude `.github/**` from the results branch") does not survive contact with the
mechanism above. The rejection is not about the *commit* the nightly makes — that commit adds two
Markdown files. It is about the new ref's workflow tree differing from main's. Excluding
`.github/**` therefore means restoring main's version of it on the results branch, and the pull
request would then show the measured branch's workflow changes being reverted — and would revert
them on merge.

Option 3 (a PAT) works, and moves the permission from a reviewed file into repository settings,
where it is neither visible in review nor scoped to one job.

## Decision

**The publish moves into a second job that holds `workflows: write` and runs no repository code.**

| job | permissions | what it runs |
| --- | --- | --- |
| `measure` | `contents: write` | the branch's benchmarks, the page renderers, the wiki publish |
| `publish` | `contents: write`, `pull-requests: write`, `workflows: write` | `actions/checkout`, `actions/download-artifact`, `git`, `gh` — nothing from the repository |

`measure` uploads the two rendered pages and the selection list as an artifact and keeps the
permissions it has today. `publish` needs the elevated token, and the only things it executes are
pinned actions and two binaries the runner ships. A branch author cannot reach it.

That is the whole argument: the permission is not made smaller, it is put somewhere the measured
branch's code cannot get at it.

## What changes in the file

1. `permissions:` moves off the workflow and onto each job. `measure` loses
   `pull-requests: write`, which only the publish ever used.
2. `measure` gains a final step uploading `docs/guides/nightly_run.md` and
   `docs/guides/benchmark_latest.md` — or their `docs/guides/branch/` counterparts — plus
   `selected.txt`, and exposes `steps.select.outputs.selected` as a job output for the pull
   request body.
3. The `Open the pull request` step becomes the `publish` job: `needs: measure`, a checkout of the
   dispatched ref, `actions/download-artifact` over it, then the same staging, commit, push and
   `gh pr create` the step does today, unchanged.
4. The push reports which failure it was. #464's closing paragraph asks for that: today a
   permissions rejection reads in the run list like a benchmark failure. A failed push now prints
   a line naming `workflows: write` and the results branch, and the job's own name says the
   measurement itself succeeded.

**One ordering changes.** Today the pull request opens before the wiki push; afterwards it opens
after. Both read the same tree, and neither depends on the other — the wiki publish is a
side-channel gated to `main` (#367), and the pull request is the record. Noted here so a reader of
the run log is not surprised by it.

## What does not change

- The results branch is still built from the measured ref's tip, so the pull request still shows
  two added files and nothing else.
- `--branch` still routes a non-main run to `docs/guides/branch/`, and the baseline marker still
  re-states the merge-base rather than advancing (#379).
- `.gitattributes`' `-merge` on the generated pages (#446/#447) is untouched.
- No secret is added.

## Testing

A workflow cannot be unit-tested here, and this repository has no workflow-linting job. What can
be checked, and is:

- `tools/tests/test_bench_nightly_permissions.py` parses `.github/workflows/bench-nightly.yml` and
  asserts the property the fix exists for: `workflows: write` appears on exactly one job, that job
  declares no `run:` step invoking `python`, `dotnet` or any path under the repository, and the job
  that runs the benchmarks does not hold it. A future edit that moves a `python` call into
  `publish`, or the permission back onto `measure`, fails.
- The YAML parses, and every `uses:` in that job is pinned to a 40-character SHA — already the
  file's convention, extended to the new `actions/download-artifact` pin.
- `pyyaml` is declared in `tools/requirements.txt` rather than inherited through
  `huggingface-hub`, so the test above cannot skip itself for want of a parser. The lock is
  regenerated; its only change is that one package's provenance line, so no pin moves and no
  corpus can.

The end-to-end proof is a dispatch on a branch that touches `.github/workflows/`; this branch is
one, so the run that measures it is the test.
