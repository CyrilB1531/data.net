# 0138 — Windows development is neither documented nor verified

**Issue:** [#138](https://github.com/CyrilB1531/data.net/issues/138) · **Date:** 2026-08-13 ·
**Branch:** `chore/138-windows-development` · **Supersedes:** [#137](https://github.com/CyrilB1531/data.net/issues/137)

## Context

The repository ships `net10.0;netstandard2.0` in one package so that .NET Framework, Mono and Unity
consumers are served, and nothing tells a Windows contributor they are unwelcome. But every documented
command assumes POSIX, parts of the tooling assume it too, and **all nine CI jobs run `ubuntu-latest`**, so
nothing would notice if a change broke Windows entirely.

Verified on `main` at `aa25fce`: `grep -rh "runs-on:" .github/workflows/` gives nine `ubuntu-latest` and
nothing else; `CLAUDE.md` carries five POSIX-only constructs and `CONTRIBUTING.md` six; and
`tools/check_machine_paths.py:189` derives its strongest probes from `os.environ.get("HOME")` alone, with a
`home.rstrip("/")` that does not know the other separator. On native Windows `HOME` is typically unset, so
the half of that guard which catches shapes nobody enumerated is inert **on the machine where the paths are
created** — which is the one place it was written for.

## The constraint that shapes this lot

Almost none of this can be settled from Linux. The issue lists six facts that have to be measured, and
guessing at them is how the machine-path guard acquired a false claim in its own comment before it shipped.

**The measuring instrument is a CI job.** A throwaway workflow on `windows-latest`, pushed on this branch,
can ask the six questions and print their answers; the log is the evidence, and it costs nothing —
this repository is public, so GitHub's standard runners are free and unmetered on every platform, Windows
included. The 2× multiplier applies to private repositories' included minutes and does not apply here.

**And the runner is not representative for one of the six.** GitHub's Actions environment sets `HOME` on
Windows runners. A contributor's machine does not: `cmd` and PowerShell have `USERPROFILE`, and Git Bash
sets `HOME` itself. Measuring the `HOME` question on a runner measures the runner. That one is flagged in
D2 and answered conservatively rather than from a reading that cannot see the case that matters.

## Decisions

### D1 — a throwaway probe job answers the six questions before anything is written

`.github/workflows/windows-probe.yml`, on this branch only, deleted before the pull request. One job,
`windows-latest`, whose steps print rather than assert:

1. **The oracle generator's neutral directory.** Does `nltk` refuse to import its dependencies when the
   working directory is the repository — the refusal `CLAUDE.md` documents `/tmp` for — and if so, what is
   the Windows equivalent that satisfies it?
2. **The venv layout**, and whether `python -m <tool>` makes the `bin` / `Scripts` split moot for every
   tool this repository documents.
3. **`HOME`, `USERPROFILE` and `TEMP`** — which are set, to what, and whether `TEMP` sits *inside* the
   user profile, which would put a scratch directory inside the home directory rather than beside it and
   change which probe catches it.
4. **Line endings.** With `core.autocrlf=true` at checkout, does `dotnet format --verify-no-changes` still
   pass, and does any file under `tests/oracles/` compare differently?
5. **The interpreter's name** — whether `python3` resolves at all, and what `python --version` reports.
6. **The wall-clock** of `dotnet build` and `dotnet test` there, which decides whether the permanent job of
   D5 becomes the slowest check on every pull request.

Every step prints its answer and none fails the job: a probe that goes red tells you less than one that
reports.

### D2 — the guard reads both variables, and shared profiles are not home directories

`environment_probes` takes the first of `HOME` and `USERPROFILE` that is set, and the separator handling
learns `\` beside `/`. Both, rather than one, because the two coexist: Git Bash sets `HOME` on Windows and
`cmd` does not, so a guard that reads only one is inert in half the shells a contributor uses — and the
probe job of D1 cannot settle this, since the runner sets `HOME` where a real machine would not.

`C:\Users\Public`, `Default` and `All Users` are **not** treated as home directories. They are shared
profile folders, no contributor's path runs through them, and a probe derived from one would fire on
ordinary prose containing the word "public". This is a decision, not an oversight, and it is recorded here
so a reviewer asking "why not those" finds the answer.

### D3 — one canonical command where one exists, two forms where it does not

The issue prefers a single form that works on both, and for most of the surface one exists: `python -m …`
rather than `python3 …`, `python tools/…` rather than an interpreter path, and forward slashes, which
Windows accepts throughout.

Three constructs resist, and get both forms side by side rather than a unified one that would be wrong on
both platforms:

- the oracle generator's **neutral working directory**, whose Windows equivalent D1 measures;
- **environment-variable assignment** — `export NAME=value` against `$env:NAME = 'value'`;
- the **wait loop** for the local SonarQube container, which is a POSIX shell construct.

### D4 — the tooling stops assuming POSIX where it does not have to

`tools/check_machine_paths.py` is the one that must change, because its subject *is* filesystem paths (D2).
The others are audited rather than rewritten: a script that already uses `pathlib` and `os.path` needs
nothing, and one that hardcodes `/` in a path it builds does. What the audit finds is reported even where
it changes nothing, because "we looked and there was nothing" is a result and "we assumed" is not.

### D5 — one permanent `windows-latest` job, covering what fails platform-specifically

Build, the eight test assemblies across both target frameworks, and the checks whose failure mode is
platform-specific: `tools/check_machine_paths.py`, `tools/check_version_floor.py`, and the tool tests under
`tools/tests/`. Not the nine jobs duplicated: markdownlint, the oracle drift gate and the SonarCloud gate
do not depend on the platform, and duplicating them buys nothing but wall-clock on every pull request.

Cost is not the criterion — the runners are free here — but **return time is**. The job runs in parallel
with the others, so it lengthens a pull request only if it becomes the slowest, which D1's sixth question
measures before this is committed to.

### D6 — a Windows virtual machine is a complement, not a prerequisite

The user has qemu. A Windows VM would answer two things the runner cannot: what `HOME` looks like outside
an Actions environment (D2's subject), and whether the documented setup works from a bare machine, where
the runner arrives with .NET, Python and git already installed and configured.

Neither blocks this lot. If the VM happens, its findings amend D2 and D3; if it does not, D2's decision to
read both variables is the conservative answer that holds either way. This is written here so that a later
reader knows the question was asked rather than missed.

## Evidence

- **The probe job's log**, quoted into the implementation's report, and its findings carried into whatever
  they change. It is deleted before the pull request; the log survives in the run history and in the
  report.
- **The permanent job**, green, on a pull request — which is the only proof that the Windows path works,
  as opposed to being believed to.
- **`tools/tests/`** gains cases for the Windows shapes, in the assembled-literal style the existing tests
  use so the test file does not itself trip the guard it tests.
- The documented commands are **run** on the probe job where they can be, rather than transcribed and
  hoped for.

## Documentation

- `CLAUDE.md` and `CONTRIBUTING.md` per D3.
- `tools/README.md` if the audit of D4 changes a script's invocation.
- No ADR. Nothing here diverges from a reference implementation; it is a platform the repository already
  claimed to support and now proves.

## Out of scope

macOS, which is a third answer to every question above. The runtime behaviour of the shipped packages on
Windows, which the `netstandard2.0` target and its own test run already cover — this is about developing
the repository, not consuming it. And the local SonarQube container of #109, whose Windows story is a
different question from the one this lot answers.

## Risks

- **The probe job measures the runner, not a machine.** Explicit in D2, and the reason its conclusion is
  the conservative one. The risk is that another of the six turns out to be runner-specific in a way
  nobody flagged; the mitigation is that every one of them is *printed*, so a later reader can see what
  was actually observed rather than what was concluded.
- **Line endings could turn out to be a larger problem than a checkbox.** If `core.autocrlf=true` moves
  `dotnet format` or a corpus, the answer is a `.gitattributes` decision affecting every contributor, which
  is bigger than this lot and would be split out rather than absorbed.
- **The permanent job could become the slowest check.** Measured in D1 before it is committed to; if it
  is, the scope in D5 is what gets cut, and the cut is recorded.
