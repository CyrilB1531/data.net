# 0037 — The guards run before the commit, through `core.hooksPath` and no framework

**Status:** accepted · **Date:** 2026-08-18

## Context

[0015](0015-sonar-rules-in-the-build.md) moved the Sonar rules from *after the
push* into the build, and gave the reason: a check whose only reporter is CI
costs a round trip for every finding. The guards under `tools/` were left where
0015 found the analysers.

There are four of them that need nothing but Python and the repository —
`check_machine_paths.py`, `check_comment_length.py`, `check_version_floor.py`
and `check_sample_culture.py` — and each is a separate CI step on its own exit
code. Every one of them is first heard from after a push. #133 said the hook
that would fix that was "its own issue, with its own decision about installation
and opt-out"; this is that decision.

Two facts constrain it, and neither is a matter of taste.

**Three of the four read `git ls-files`.** Run by hand, they see the tracked
tree; run from a pre-commit hook they see the index, which is the state CI will
build. A newly `git add`ed file is invisible to the first and visible to the
second, so the hook is not merely earlier than a manual run — on exactly the
files most likely to trip a guard, it is *more correct*.

**Neither `python` nor `python3` resolves on both supported platforms.** Ubuntu
24.04 ships `/usr/bin/python3` and no `python`; python.org's Windows installer
ships `python.exe` and no `python3.exe`, and a `python3` that does resolve there
is often the Microsoft Store alias, which opens the Store instead of running
anything. A hook that names one of them breaks commits on a machine where the
guards would have run fine.

## Decision

A tracked `.githooks/pre-commit`, a POSIX `sh` script, installed by pointing git
at the directory:

```bash
git config core.hooksPath .githooks
```

It resolves `python3` then `python`, runs the four guards in sequence, reports
every one that failed rather than the first, and exits non-zero if any did.
`git commit --no-verify` skips it, and the failure message says so.

## Why `core.hooksPath` and not `pre-commit`

`pre-commit` is the obvious alternative and it is genuinely better at the
problem it solves: hooks in several languages, pinned by revision, installed
per-repository. This repository does not have that problem. It has four scripts
in one language that is already a development dependency.

| | `core.hooksPath` | `pre-commit` |
| --- | --- | --- |
| To install | one `git config` | `pip install pre-commit`, then `pre-commit install` |
| New dependency for every contributor | none | one, plus its own pinned config |
| Where the guard list lives | the hook, next to the guards | `.pre-commit-config.yaml`, in its own vocabulary |
| What keeps it honest | a test, below | the same test would still be needed |

The repository already refuses to add a runtime dependency lightly, and a hook
framework is a development one for everybody who clones. The asymmetry is what
decides it: `pre-commit` buys isolation and pinning for a set of hooks that is
four invocations of `python tools/check_*.py`, and charges an install step to
each contributor for it.

## Why the guards and not the gates

The scope is what runs offline in about a second. Measured on this machine
(Linux 7.0.0-28-generic, CPython 3.12), one run each over the tree at the time
of writing:

| guard | |
| --- | --- |
| `check_machine_paths.py` | 0.97 s |
| `check_comment_length.py` | 0.12 s |
| `check_version_floor.py` | 0.06 s |
| `check_sample_culture.py` | 0.03 s |
| **the four in sequence** | **1.18 s** |

> **#11 update: a fifth joined, and the budget still holds.**
> `check_bench_map.py` refuses a benchmark class that `bench/bench-map.json` does not name,
> because the nightly run selects what to re-measure from that map and a class missing from it
> stops being measured in silence. Measured the same way, on the same machine: **0.04 s**, the
> five in sequence **1.35 s**. It reads the benchmark sources and the map and touches neither
> the network nor a build, so it belongs in the hook rather than in the exclusions above.
>
> `tools/tests/test_pre_commit_hook.py` is what made this an obligation rather than a courtesy:
> adding the guard to CI and not to the hook failed that test immediately, with a message
> naming the guard and the two places it could go.
>
> **#280 update: a sixth, same shape.** `check_sample_coverage.py` refuses a public class
> with no sample named after it, for the packages decision 0041 has already split — the
> rest still carry their `Lot*` files and would fail every run. Measured the same way:
> **0.04 s**, the six in sequence **1.36 s**. It reads `src/` and `samples/` and touches
> neither the network nor a build.

Issue #207 estimated "well under a second" for the four; the real figure is 1.18 s and
`check_machine_paths.py` is 82% of it. That is still the right side of the line
this decision draws, and the line is drawn where it is because a hook that
reached `dotnet build` — 35 s here, cold — is a hook that gets uninstalled in a
week, at which point the guards run after the push again and nothing has been
gained.

Two guards CI runs are deliberately **not** in the hook:

- `check_nuspec_dependencies.py` reads the `.nuspec` files inside a packed
  `./artifacts`. It is a post-pack assertion, not an offline one; putting it in
  the hook would mean packing four projects before every commit.
- `check_version_floor.py --check-feed` reaches nuget.org. The guard runs
  without the flag, so the two offline rules still hold; the feed rule is CI's,
  because a commit should not wait on the network and a hook that fails on a
  train is a hook people delete.

## Why the opt-out is documented rather than quiet

`--no-verify` exists whether or not this repository mentions it, so the decision
is only whether a contributor learns it from the project or from a search engine
at the moment they are most annoyed. It is named in `CONTRIBUTING.md` and in the
hook's own failure message. A hook that presents itself as unskippable is a hook
that gets deleted rather than skipped, and a deleted hook is silent next time.

## Consequences

- **CI is unchanged, and stays the authority.** The hook adds no check and
  removes none; every guard still runs on the push, and the three gates, the
  build, the tests and Sonar are reachable only there. A green commit is not a
  green pull request and this decision does not let anyone believe otherwise.
- **Installation is opt-in, and silence is the failure mode.** `core.hooksPath`
  is local configuration, so a clone has no hook until someone runs the command;
  a contributor who never does is exactly where they were before. That is the
  price of not shipping a framework, and it is the reason CI keeps the guards
  rather than delegating them.
- **The hook can drift from CI, so a test holds them together.**
  `tools/tests/test_pre_commit_hook.py` reads both the hook and `ci.yml` and
  fails when CI grows a guard the hook does not run — with the two exclusions
  above written down as the only allowed difference. A fifth guard added to CI
  and not to the hook is a test failure, not a discovery six months later.
- **The hook reads the worktree, not the index.** `git ls-files` reports the
  index, which is the gain described above, but the guards then read each file
  from disk. A file staged in one state and edited in another is checked in its
  worktree state — so the hook can pass on content the commit does not contain,
  and fail on content it does. Reproducing the index exactly would mean a stash
  dance around every commit, which trades a rare wrong answer for a common way
  to lose work. CI reads the commit and is not affected.
- **`.githooks/**` is pinned to LF in `.gitattributes`.** This is the first
  shell script in the repository, and Git for Windows checks text out as CRLF by
  default; `#!/bin/sh\r` is not a program any kernel finds, and the error names
  a file that plainly exists.
