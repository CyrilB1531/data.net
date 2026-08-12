# 0133 — Nothing stops a machine path from being committed

**Issue:** [#133](https://github.com/CyrilB1531/data.net/issues/133) · **Date:** 2026-08-12

## Context

Ten absolute filesystem paths reached this public repository across six documents, and nothing caught any
of them. Both sweeps that removed them — [#135](https://github.com/CyrilB1531/data.net/pull/135) — started
from a reader noticing a line, not from a check.

- **Eight session scratch-directory paths**, in four plans. These are the ones that matter: the scratchpad
  is named after the absolute path of the checkout it belongs to, so each occurrence encoded a home
  directory.
- **Two CI runner checkout paths**, quoted verbatim from a log in #70's spec and plan.

The paths reach documents by being pasted from a terminal, which is exactly when nobody is thinking about
what the string contains.

## The measurement that decides the design

Four obvious patterns — a named directory under `/home`, under `/Users`, under `C:\Users`, and the root
user's home — were run against
`main` **before** the sweep, over every tracked text file:

| What was there | Caught |
| --- | --- |
| The 2 runner paths in #70's documents | yes |
| **The 8 scratchpad paths in four plans** | **no** |

The scratchpad encodes the home directory with **dashes**, not slashes — shown here with the name
redacted, because a spec that pasted the real one would be the very thing this guard exists to refuse:

```text
/tmp/claude-<id>/-home-<user>-<path>-to-<checkout>/<session>/scratchpad
```

Nothing searching for `/home/` sees it. A guard built on the obvious pattern would have caught the two
least sensitive occurrences — a runner path identical for every public repository on the platform — and
missed the eight that carried a real home directory. **That is worse than no guard**, because it would
report clean.

## Design

### D1 — two probe sets, one script

`tools/check_machine_paths.py`, on the pattern `check_version_floor.py` establishes: a docstring naming
the drift it catches, standard library only, invoked from CI.

**Named shapes**, portable and active everywhere: a home directory under `/home` or `/Users`, its Windows
equivalent under `C:\Users`, the root user's own home directory, and the session scratch-directory prefix
`/tmp/claude-<digits>/`. Each carries a comment saying
what it catches and, where one exists, the occurrence that motivated it.

**Environment-derived probes**, computed at run time from `$HOME`: the path itself, its basename, and its
dashed form — `$HOME` with `/` replaced by `-`. The dashed form is the mechanical transformation the
scratchpad applies, and it is what the named shapes miss.

The second set is what makes the guard stronger than a fixed list, and it works on both sides rather than
only locally: on a contributor's machine `$HOME` is `/home/<them>`, and on GitHub Actions it is
`/home/runner` — which is exactly the shape of one of the two paths this issue exists because of.

### D2 — the guard is about home directories, not about absolute paths

`/tmp` is **load-bearing** in this repository: `nltk` refuses to import its dependencies when they appear
to live under the current directory, so `tools/generate_oracles.py` must be run from a neutral directory,
and that instruction appears in `CLAUDE.md`, in `CONTRIBUTING.md` and in several plans. `/usr`, `/etc` and
`~/.nuget` likewise appear legitimately.

So the guard never asks "is this an absolute path". It asks "is this a path under someone's home
directory". `/tmp/claude-…` is caught by its own named shape rather than by a rule about `/tmp`.

### D3 — a generic username must not turn the guard into noise

An environment-derived probe for a username like `dev` or `build` would match half the repository. The
derived probes therefore match the basename only when bounded by a path separator or a dash, and never as
a bare substring.

A repository whose contributor is called something that still collides needs an escape rather than a
disabled guard, so `--no-environment` skips the derived set and leaves the named shapes enforcing. The
script says so in its `--help`.

### D4 — it runs in the `Lint` job, and there is no git hook

One step beside markdownlint and `dotnet format`, which is what gates the merge.

**No git hook.** This repository ships none today; adding one touches the contribution flow of everyone
who clones, and the script runs by hand in one command regardless. If a pre-commit hook is wanted it is
its own issue, with its own decision about installation and opt-out.

### D4b — the guard exempts its own source and tests, and nothing else

A guard's implementation must contain the patterns it searches for, and so must its tests. The exemption is
forward-looking rather than something today's tree already needs: the literals in both files are assembled
from pieces (concatenated strings, a regex written as a pattern rather than a matching literal), so running
the design's own patterns over them today finds nothing. It would become load-bearing the day a contributor
whose account name is `home` or `tmp` runs the guard — a derived probe built from that `$HOME` would turn one
of those same pieces into a hit, in the one file that must not be asked to exempt itself out of existence.

Two files are exempt: `tools/check_machine_paths.py` and its test module. The justification does not
generalise, which is the point — an exemption list that grows is a guard being switched off one file at a
time, so the script hard-codes those two rather than reading a list from anywhere.

Documents about the guard — this spec, its plan, the eventual `CONTRIBUTING.md` sentence — take the other
route and describe the shapes in prose rather than pasting a literal that matches. That is why the design
above says "the root user's own home directory" instead of writing the path.

### D5 — the tests carry the strings that actually occurred

`tools/tests/` already exists, and CI already runs `python -m pytest tools/tests -q`.

The guard's tests use the **real strings from both sweeps**, recovered from git history, rather than
invented examples. That is the only way to prove it catches what happened rather than what someone
imagined — and specifically the dashed scratchpad form, which is the case the obvious pattern missed.

Alongside them, tests that the load-bearing paths are *not* flagged: a bare `/tmp/…`, `/usr/bin`,
`~/.nuget`, and the `$GITHUB_WORKSPACE` prose that replaced the runner paths.

## Evidence

- The guard reports clean on `main` as it stands: 519 tracked text files, zero hits under all five named
  shapes. Measured before this spec was written.
- The guard flags the eight scratchpad paths and the two runner paths. This is asserted by unit tests
  holding those exact strings as fixtures — recovered from git history — rather than by running the script
  over an old checkout, so the assertion survives any later change to history.

## Out of scope

**Rewriting history.** The removed strings survive in the commits that introduced them. A `filter-repo`
pass would change every SHA, break forks and cross-references, and need a force push to a protected
branch. #135 recorded the judgement that neither the runner path nor a directory name warrants that cost;
this issue is about stopping the next one.

**Tokens, hostnames and other accidental secrets.** The same class of mistake, and a different set of
patterns with a different false-positive profile. Naming them here would make this lot's scope a
negotiation.

**A pre-commit hook**, per D4.

## Risks

- **A guard that reports clean while missing a shape** is the failure this issue exists because of, and no
  finite list closes it. D1's environment-derived set is the mitigation — it catches shapes nobody
  enumerated, on the machine where they are created — and D5's tests are what stop the named shapes from
  regressing. Neither makes the list complete, and the spec should not pretend otherwise.
- **A false positive blocks a merge on a document that is fine.** D3 bounds the derived probes and gives an
  escape. The named shapes have no escape by design: a path under a home directory is never wanted in a
  committed file.
