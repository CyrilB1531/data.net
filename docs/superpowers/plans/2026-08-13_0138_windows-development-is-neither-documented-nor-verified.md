# #138 — Windows development, documented and verified Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Goal:** Make Windows a platform this repository proves rather than assumes — the commands documented for
both, the tooling not assuming POSIX where it need not, and a `windows-latest` job that would notice a
regression.

**Architecture:** A throwaway probe workflow on `windows-latest` answers six questions nobody can settle
from Linux, and its log is read before anything else is written. What it finds shapes the documentation,
the guard's environment probes, and the scope of the permanent job. The probe is deleted before the pull
request; the run log and the report keep the evidence.

**Tech Stack:** GitHub Actions (`windows-latest`, `ubuntu-latest`), PowerShell and `cmd`, Python 3.12,
.NET SDK 10.

**Spec:** `docs/superpowers/specs/2026-08-13_0138_windows-development-is-neither-documented-nor-verified.md`

## Global Constraints

- Everything in English — code, comments, commit messages, PR body. Commit messages carry no
  `feat:`/`fix:` prefix and no process prefix such as `Fix round 1:`.
- Branch `chore/138-windows-development`, based on `main` at `aa25fce`. Never commit to `main`.
  **This lot is the exception to "do not push": Task 1 cannot run without pushing the branch.** The
  controller pushes after asking; no task pushes on its own initiative, and none opens a pull request.
- **No absolute machine path in anything committed** — `tools/check_machine_paths.py` enforces it, and this
  branch's whole subject is paths, so run it after every task that touches a document.
- GitHub Actions are pinned to full commit SHAs (#24). Copy the pinned lines from `.github/workflows/ci.yml`
  verbatim: `actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1`,
  `actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0`,
  `actions/setup-python@5fda3b95a4ea91299a34e894583c3862153e4b97 # v7.0.0`.
- **A claim about what Windows, `nltk`, or a shell does is a claim, and a false one is a defect** (#134).
  Nothing in this lot may be written from a reading of documentation: if the probe did not measure it, the
  text says so or does not say it.
- `dotnet format DataNet.slnx --verify-no-changes` runs **once**, in the final task, and is run bare with no
  `env -u DOTNET_ROOT` wrapper.
- Read the pass/fail **counts** of every test run. Baseline on this branch: **2 995 passing, 0 failed**
  across eight assemblies, and **40 passing** under `tools/tests`.
- Never write `echo "exit=$?"` after a pipeline — redirect to a file and check separately.
- `docs/**`, `CONTRIBUTING.md`, `README.md`, `tools/README.md` and `bench/README.md` are inside CI's
  markdownlint glob; `CLAUDE.md` is **not**, so a broken table there fails nothing — check it by eye.

## The constructs this lot rewrites, located

Measured on this branch. Every one is a line a Windows contributor cannot run:

| File | Line | Construct |
| --- | ---: | --- |
| `CLAUDE.md` | 21, 22, 44 | `python3 tools/…` |
| `CLAUDE.md` | 38 | `cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python …` |
| `CLAUDE.md` | 97 | `export DataNetUseProjectRefs=true` |
| `CONTRIBUTING.md` | 94, 95, 96, 103 | `python3 tools/…` |
| `CONTRIBUTING.md` | 127 | `until curl … \| grep -q …; do sleep 5; done` |
| `CONTRIBUTING.md` | 200 | `export DataNetUseProjectRefs=true` |
| `CONTRIBUTING.md` | 238 | the same `cd /tmp && …` generator invocation |

## File Structure

| File | Responsibility |
| --- | --- |
| `.github/workflows/windows-probe.yml` *(new, deleted in Task 6)* | Asks the six questions on `windows-latest` and prints the answers. |
| `tools/check_machine_paths.py` | Reads `HOME` **or** `USERPROFILE`; the separator handling learns `\`. |
| `tools/tests/test_check_machine_paths.py` | The Windows shapes, in the assembled-literal style already there. |
| `CLAUDE.md`, `CONTRIBUTING.md` | One canonical command where one exists, two forms where it does not. |
| `.github/workflows/ci.yml` | One permanent `windows-latest` job. |
| `tools/README.md` | Only if the audit changes a documented invocation. |

---

### Task 1: Ask Windows the six questions

**Files:**

- Create: `.github/workflows/windows-probe.yml`

**Depends on:** nothing.

**Produces:** the answers Tasks 2, 3 and 5 are written against. Nothing else in this plan may be written
before this task's log has been read.

**This task's job is to report, not to pass.** Every step prints; none asserts. A probe that goes red tells
you less than one that answers.

- [ ] **Step 1: Write the probe workflow**

```yaml
name: Windows probe (temporary, issue #138)

# Pushed on this branch only and deleted before the pull request. It answers
# questions nobody can settle from Linux; the run log is the evidence and it
# survives the file.
on:
  push:
    branches: [chore/138-windows-development]

jobs:
  probe:
    name: What Windows actually does
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
      - uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0
        with:
          dotnet-version: '10.0.x'
      - uses: actions/setup-python@5fda3b95a4ea91299a34e894583c3862153e4b97 # v7.0.0
        with:
          python-version: '3.12'

      - name: Q5 — what the interpreter is called
        continue-on-error: true
        run: |
          Write-Host "python  : $(python --version 2>&1)"
          Write-Host "python3 : $(python3 --version 2>&1)"
          Write-Host "py      : $(py --version 2>&1)"

      - name: Q3 — the environment variables the guard depends on
        run: |
          Write-Host "HOME        = '$env:HOME'"
          Write-Host "USERPROFILE = '$env:USERPROFILE'"
          Write-Host "TEMP        = '$env:TEMP'"
          Write-Host "TMP         = '$env:TMP'"
          $inProfile = $env:TEMP -and $env:USERPROFILE -and $env:TEMP.StartsWith($env:USERPROFILE)
          Write-Host "TEMP is inside USERPROFILE: $inProfile"
          Write-Host "--- and what cmd sees, which is not what Actions sets for PowerShell ---"
          cmd /c "echo HOME=%HOME% USERPROFILE=%USERPROFILE% TEMP=%TEMP%"

      - name: Q4 — line endings
        run: |
          git config core.autocrlf
          Write-Host "--- dotnet format, on a checkout as the runner made it ---"
          dotnet format DataNet.slnx --verify-no-changes
          Write-Host "format exit: $LASTEXITCODE"
          Write-Host "--- and whether any tracked file differs from its committed bytes ---"
          git status --porcelain
          git diff --stat

      - name: Q2 and Q1 — the venv layout, and nltk's working directory
        continue-on-error: true
        run: |
          python -m venv .venv-probe
          Write-Host "--- what the venv contains ---"
          Get-ChildItem .venv-probe -Name
          if (Test-Path .venv-probe\Scripts\python.exe) { Write-Host "interpreter: Scripts\python.exe" }
          if (Test-Path .venv-probe/bin/python)          { Write-Host "interpreter: bin/python" }
          .venv-probe\Scripts\python.exe -m pip install --quiet --only-binary :all: --require-hashes -r tools/requirements.lock.txt
          Write-Host "--- importing nltk from the repository root, which is what fails on Linux ---"
          .venv-probe\Scripts\python.exe -c "import nltk; print('import from repo root: OK', nltk.__version__)"
          Write-Host "nltk-from-root exit: $LASTEXITCODE"
          Write-Host "--- and from a neutral directory, whatever that is here ---"
          Push-Location $env:TEMP
          & "$env:GITHUB_WORKSPACE\.venv-probe\Scripts\python.exe" -c "import nltk; print('import from TEMP: OK', nltk.__version__)"
          Write-Host "nltk-from-temp exit: $LASTEXITCODE"
          Pop-Location

      - name: Q6 — what the permanent job would cost
        run: |
          $build = Measure-Command { dotnet build DataNet.slnx -c Release --no-incremental }
          Write-Host "build   : $($build.TotalSeconds) s"
          $test = Measure-Command { dotnet test DataNet.slnx -c Release }
          Write-Host "test    : $($test.TotalSeconds) s"

      - name: The Python checks, which the permanent job is meant to carry
        continue-on-error: true
        run: |
          python tools/check_version_floor.py
          Write-Host "version-floor exit: $LASTEXITCODE"
          python tools/check_machine_paths.py
          Write-Host "machine-paths exit: $LASTEXITCODE"
          .venv-probe\Scripts\python.exe -m pytest tools/tests -q
          Write-Host "tool-tests exit: $LASTEXITCODE"
```

`continue-on-error: true` sits on the steps whose *failure is the answer* — the interpreter that may not
exist, the import that may be refused, the checks that may not run. It is deliberately absent from the two
steps that must simply work.

- [ ] **Step 2: Commit, and ask the controller to push**

```bash
git add .github/workflows/windows-probe.yml
git commit -m "Ask Windows the six questions #138 cannot answer from here"
```

Then **stop and tell the controller the branch needs pushing**. Do not push. The workflow triggers on push
to this branch, so nothing runs until it does.

- [ ] **Step 3: Read the log, and write down every answer**

```bash
gh run list --branch chore/138-windows-development --limit 3
gh run view <id> --log > /tmp/138-probe.log
```

Quote each of the six answers **verbatim** into your report, then state what each one means for the tasks
that follow:

1. does `nltk` refuse to import from the repository root on Windows, and if so what satisfies it;
2. where the venv puts its interpreter, and whether `python -m` makes it moot;
3. `HOME`, `USERPROFILE`, `TEMP`, whether `TEMP` is inside the profile, and **what `cmd` sees** — the
   spec expects PowerShell under Actions to have `HOME` set where a contributor's shell would not, and
   that difference is the reason D2 reads both;
4. whether `dotnet format` and the tracked files survive the runner's checkout;
5. whether `python3` resolves at all;
6. the build and test wall-clock, which decides Task 5's scope.

If an answer contradicts the spec, say so plainly — that is the probe working, not failing.

---

### Task 1b: The hashed lock file, which does not install on Windows at all

**Files:**

- Modify: `tools/requirements.txt` and/or `tools/requirements.lock.txt`

**Depends on:** Task 1, which found this.

**What the probe found, and why it blocks more than it looks like.**

```text
ERROR: In --require-hashes mode, all requirements must have their versions pinned with ==. These do not:
    colorama … (from click==8.4.2->-r tools/requirements.lock.txt (line 17))
```

`colorama` is `click`'s Windows-only conditional dependency, and `click` is `nltk`'s. A lock resolved on
Linux never needed to pin it, so `--require-hashes` — which demands the whole closure — refuses the entire
install. Nothing downstream runs: not the oracle generator, not the tool tests, and not the `nltk` question
the probe existed to answer, which came back "never tested" for this reason alone.

- [ ] **Step 1: Establish what this repository's pip-tools can do**

```bash
pip-compile --version
pip-compile --help | grep -n "universal"
```

`--universal` produces a lock carrying every platform's closure with environment markers, and it exists
from pip-tools 7.4. If this version has it, that is the fix. If it does not, the alternatives are naming
the Windows-only packages in `tools/requirements.txt` so the resolver pins them, or generating a second
lock — and the second is worse, because two hashed graphs drift apart exactly the way
`check_version_floor.py` exists to catch elsewhere. **Report which route the version left open.**

- [ ] **Step 2: Regenerate, and diff what moved**

The command `CONTRIBUTING.md` records is
`pip-compile --generate-hashes --strip-extras --output-file tools/requirements.lock.txt tools/requirements.txt`;
add `--universal` to it if Step 1 says so, and update that documented line in the same commit — a recorded
command that no longer produces the committed file is worse than none.

```bash
git diff --stat tools/requirements.lock.txt
git diff tools/requirements.lock.txt | grep -E "^\+[a-z]" | head -20
```

Expect `colorama` to appear with a marker, and expect **nothing else to move**. A version bump smuggled in
by a regeneration is a separate change: if one appears, say so and stop rather than carrying it.

- [ ] **Step 3: Prove it still installs where it already worked**

```bash
python3 -m venv /tmp/138-lockcheck
/tmp/138-lockcheck/bin/pip install --only-binary :all: --require-hashes -r tools/requirements.lock.txt > /tmp/138-lock.log 2>&1
echo "install=$?"
tail -3 /tmp/138-lock.log
rm -rf /tmp/138-lockcheck
```

Linux is where it already worked and where a regeneration could break it. Windows is proven by the
permanent job of Task 5, not here — say so rather than claiming what you cannot run.

- [ ] **Step 4: Commit**

```bash
git add tools/requirements.txt tools/requirements.lock.txt CONTRIBUTING.md
git commit -m "Lock the dependencies Windows needs and Linux never asked for"
```

---

### Task 2: The guard learns the other home directory

**Files:**

- Modify: `tools/check_machine_paths.py` (`environment_probes`, and its single caller at the bottom)
- Modify: `tools/tests/test_check_machine_paths.py`

**Depends on:** Task 1 (question 3), whose answer contradicted the spec and is quoted here so this task
is not written against the old assumption: on `windows-latest`, **`HOME` is unset in PowerShell and in
`cmd` alike** (`HOME=''`, and `%HOME%` left unsubstituted), while `USERPROFILE` held
`C:\Users\runneradmin`. A guard reading only `HOME` is therefore inert on every Windows path, not merely
in some shells.

The probe also found a shape nobody had listed: `TEMP` came back as the 8.3 short name
`C:\Users\RUNNER~1\AppData\Local\Temp` against that `USERPROFILE`, so a path written from `%TEMP%` sits
inside the profile while matching no probe derived from it. The guard cannot resolve short names portably;
record it as a known blind spot in the code comment rather than attempting to close it.

**Interfaces:**

- Produces: `environment_probes(home)` keeps its signature — one optional string, returning the same tuple
  of `(description, pattern)` pairs. The caller decides which variable supplies it.

- [ ] **Step 1: Write the failing tests**

Read the file first: its cases assemble path literals from fragments precisely so the test file does not
trip the guard it tests. Follow that style exactly — a test containing a literal Windows home path would
make `check_machine_paths.py` fail on its own test suite.

```python
def test_a_windows_home_directory_yields_the_same_three_probes():
    home = "C:" + chr(92) + "Users" + chr(92) + "someone"

    probes = gen.environment_probes(home)

    assert len(probes) == 3


def test_a_windows_path_is_caught_by_the_derived_probes():
    home = "C:" + chr(92) + "Users" + chr(92) + "someone"
    text = "the file lives at " + home + chr(92) + "src" + chr(92) + "thing.cs"

    assert any(pattern.search(text) for _, pattern in gen.environment_probes(home))


def test_a_trailing_backslash_does_not_swallow_the_account_name():
    # The POSIX branch strips a trailing "/" for this reason: without it the
    # account name comes out empty and all three probes are silently dropped.
    bare = "C:" + chr(92) + "Users" + chr(92) + "someone"

    assert gen.environment_probes(bare + chr(92)) == gen.environment_probes(bare)
```

Name the module alias the test file already uses rather than `gen` if it differs.

- [ ] **Step 2: Run them and watch them fail**

```bash
.venv-oracles/bin/python -m pytest tools/tests/test_check_machine_paths.py -q > /tmp/138-t2-red.log 2>&1
echo "pytest=$?"
tail -6 /tmp/138-t2-red.log
```

Expected: the trailing-separator case fails, and possibly the others depending on how the existing regexes
treat `\`. **Read which ones failed and why** — a case that already passes is telling you the code was more
general than the spec assumed, and that belongs in the report.

- [ ] **Step 3: Teach it the other separator and the other variable**

In `environment_probes`, strip either separator and split on either. In the caller at the bottom of the
file, read `HOME` first and fall back to `USERPROFILE`:

```python
    derived = environment_probes(os.environ.get("HOME") or os.environ.get("USERPROFILE")) if use_environment else ()
```

The comment above `environment_probes` explains why the probes are derived at all; extend it with why two
variables are read, in one sentence, citing what Task 1 measured rather than what MSDN says: Git Bash sets
`HOME` on Windows and `cmd` does not, so a guard reading one is inert in half the shells a contributor uses.

`C:\Users\Public`, `Default` and `All Users` are **not** home directories here — that is D2's decision.
Add it as a comment only if the code would otherwise look like an oversight; do not add a test for a
behaviour the code does not have.

- [ ] **Step 4: Green, and prove the guard still guards**

```bash
.venv-oracles/bin/python -m pytest tools/tests -q > /tmp/138-t2-green.log 2>&1
echo "pytest=$?"; tail -3 /tmp/138-t2-green.log
python3 tools/check_machine_paths.py > /tmp/138-t2-guard.log 2>&1
echo "guard=$?"
```

Expected: **43 passing** (40 + 3), and the guard exits 0 on the repository — including on the test file you
just edited.

- [ ] **Step 5: Commit**

```bash
git add tools/check_machine_paths.py tools/tests/test_check_machine_paths.py
git commit -m "Derive the guard's probes from whichever variable holds the home directory"
```

---

### Task 3: Commands a Windows contributor can run

**Files:**

- Modify: `CLAUDE.md` (lines 21, 22, 38, 44, 97)
- Modify: `CONTRIBUTING.md` (lines 94, 95, 96, 103, 127, 200, 238)

**Depends on:** Tasks 1 and 2.

- [ ] **Step 1: Take the canonical form wherever one exists**

`python3 tools/…` becomes `python tools/…` everywhere — **if** Task 1's question 5 confirms `python`
resolves on Windows and `python3` does not, and question 5's answer also tells you whether Linux needs a
note that `python` may be Python 2 on older distributions. Forward slashes stay: Windows accepts them.

Do not touch a command that already works on both.

- [ ] **Step 2: Give the three resistant constructs both forms**

The generator invocation, the environment-variable assignment, and the SonarQube wait loop. Side by side,
in the shape the document already uses for alternatives, with the Windows form taken from Task 1's answers
— particularly the neutral directory, which question 1 measured rather than guessed.

The environment variable, for instance, is `export DataNetUseProjectRefs=true` on POSIX and
`$env:DataNetUseProjectRefs = 'true'` in PowerShell; give both, and say which shell each belongs to rather
than leaving a reader to infer it.

- [ ] **Step 3: Say what is not proven**

If Task 1 left a question half-answered — the spec expects the `HOME` question to be one, since the runner
is not a contributor's machine — the document says so where it matters, in one sentence. A guide that
promises a platform it has only half measured is the failure this issue exists to end.

- [ ] **Step 4: Check both documents**

```bash
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" \
  "tools/README.md" "bench/README.md" > /tmp/138-t3-md.log 2>&1
echo "markdownlint=$?"
python3 tools/check_machine_paths.py > /tmp/138-t3-paths.log 2>&1
echo "machine-paths=$?"
```

`CLAUDE.md` is outside that glob, so read your changes to it by eye: a broken table there fails nothing.

- [ ] **Step 5: Commit**

```bash
git add CLAUDE.md CONTRIBUTING.md
git commit -m "Give both platforms the commands, and one form where one suffices"
```

---

### Task 4: Audit the tooling, and report even where nothing changes

**Files:**

- Modify: whichever of `tools/*.py` assumes POSIX where it need not
- Modify: `tools/README.md` *(only if an invocation changes)*

**Depends on:** Task 1.

`tools/check_machine_paths.py` was Task 2. This task looks at the rest.

- [ ] **Step 1: Find the assumptions**

```bash
grep -nE "'/'|\"/\"|/tmp|os\.sep|\.split\('/'\)|startswith\('/'\)" tools/*.py | grep -v "^tools/check_machine_paths.py"
grep -n "subprocess\|shell=True" tools/*.py
```

A script using `pathlib` or `os.path` needs nothing. One that hardcodes `/` in a path it builds, or shells
out to a POSIX command, needs an answer. **List everything you looked at in your report, including the
scripts that turned out to be fine** — "we looked and there was nothing" is a result; "we assumed" is not.

- [ ] **Step 2: Fix what genuinely breaks, and leave the rest**

Change only what would fail on Windows. Do not rewrite a working script into a more portable shape for its
own sake; this lot has a subject and that is not it.

- [ ] **Step 3: Green**

```bash
.venv-oracles/bin/python -m pytest tools/tests -q > /tmp/138-t4-t.log 2>&1; echo "pytest=$?"; tail -2 /tmp/138-t4-t.log
python3 tools/check_version_floor.py > /tmp/138-t4-v.log 2>&1; echo "floor=$?"
python3 tools/check_machine_paths.py > /tmp/138-t4-p.log 2>&1; echo "paths=$?"
```

- [ ] **Step 4: Commit, or report that nothing needed changing**

If nothing changed, commit nothing and say so — with the list from Step 1, so the next person does not
repeat the search.

---

### Task 5: The permanent job

**Files:**

- Modify: `.github/workflows/ci.yml`

**Depends on:** Tasks 1-4. Task 1's question 6 decides the scope.

- [ ] **Step 1: Add the job**

Beside the existing ones, not inside them. It carries what D5 names: the build, the eight test assemblies
across both target frameworks, and the three checks whose failure mode is platform-specific —
`tools/check_machine_paths.py`, `tools/check_version_floor.py`, and the tool tests.

Copy the three pinned `uses:` lines from the jobs above it, SHA and version comment included. The comment
above the job says what it is for and what it deliberately does not duplicate: markdownlint, the oracle
drift gate and the SonarCloud gate do not depend on the platform.

- [ ] **Step 2: The timing is already in, and it says keep the scope**

Task 1 measured `dotnet build` at **39.7 s** and `dotnet test` at **24.3 s** on `windows-latest`, against a
`Build, test, pack` job that takes minutes on Linux. The Windows job will not be the slowest check, so the
scope stays as D5 defines it. Confirm those numbers against the probe's log rather than taking them from
here, then move on — this step exists to be closed, not to be re-litigated.

- [ ] **Step 2b: If your own reading of the timings disagrees, cut and say so**

The scope is a decision, not a default. If the build and test wall-clock on `windows-latest` exceeds what
`Build, test, pack` takes on Linux by enough to lengthen every pull request, cut to the build and the test
suite and record the cut — in the workflow's own comment, where the next person will ask why.

- [ ] **Step 3: Validate the file, and push for a real run**

```bash
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml')); print('yaml ok')"
```

YAML parsing is not evidence that the job works. Ask the controller to push, then read the run: the job
going green **on a pull request** is the only proof the Windows path works rather than being believed to.

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "Run the build, the suite and the platform-specific checks on Windows"
```

---

### Task 6: Delete the probe, and verify everything

**Files:**

- Delete: `.github/workflows/windows-probe.yml`

**Depends on:** Tasks 1-5.

- [ ] **Step 1: Delete the probe**

```bash
git rm .github/workflows/windows-probe.yml
git commit -m "Delete the probe, whose answers are now in the documents it shaped"
```

Its log stays in the run history, and its answers are in the report and in whatever they changed. Nothing
is lost by removing the file, and leaving a temporary workflow behind is how a repository acquires a job
nobody can explain.

- [ ] **Step 2: Every gate, with real exit codes**

```bash
git status --porcelain                                                       # empty
dotnet build DataNet.slnx -c Release --no-incremental > /tmp/138-fv-b.log 2>&1; echo "build=$?"; tail -3 /tmp/138-fv-b.log
dotnet format DataNet.slnx --verify-no-changes > /tmp/138-fv-f.log 2>&1;      echo "format=$?"
dotnet test DataNet.slnx -c Release > /tmp/138-fv-t.log 2>&1;                 echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/138-fv-t.log
python3 tools/check_version_floor.py > /tmp/138-fv-v.log 2>&1;                echo "floor=$?"
python3 tools/check_machine_paths.py > /tmp/138-fv-p.log 2>&1;                echo "paths=$?"
.venv-oracles/bin/python -m pytest tools/tests -q > /tmp/138-fv-py.log 2>&1;  echo "pytest=$?"; tail -2 /tmp/138-fv-py.log
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" \
  "tools/README.md" "bench/README.md" > /tmp/138-fv-md.log 2>&1;              echo "markdownlint=$?"
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml')); print('yaml ok')"
```

All 0, 0 warnings, **2 995 passing** across eight assemblies, and the pytest count stated — 43 if Task 2
added three and Task 4 added none.

- [ ] **Step 3: The oracle drift gate**

```bash
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py > /tmp/138-fv-gen.log 2>&1
echo "generate=$?"
cd <repo> && git status --porcelain tests/oracles/
```

Expected: empty. This branch touches no corpus, so anything here is the known flakiness — regenerate once
more before reporting it.

- [ ] **Step 4: Stop and report**

Do not push and do not open a pull request. Report the state, the six answers from Task 1, and what each
one changed, and let the user decide both.

---

## Self-Review

**Spec coverage.** D1 → Task 1. D2 → Task 2. D3 → Task 3. D4 → Task 4. D5 → Task 5, whose scope Task 1
Step 3 question 6 decides. D6 → no task, deliberately: the virtual machine is a complement the spec makes
optional, and D2's answer is written to hold whether or not it happens. Evidence section → Task 1's log,
Task 5's green run, Task 2's tests. Documentation section → Task 3 and Task 4 Step 4. Out of scope →
nothing here touches macOS or the shipped packages' runtime behaviour.

**Placeholders.** Task 3 and Task 5 branch on answers Task 1 measures, and both say what to do in each
direction rather than leaving a blank. Task 4 may legitimately change nothing, and says what to do then.
`<repo>` in Task 6 is a path only the executing session knows — and writing the real one into a file is
what `tools/check_machine_paths.py` exists to refuse.

**Type consistency.** `environment_probes(home)` keeps one parameter and its return shape across Task 2's
tests and its implementation. The workflow file is `.github/workflows/windows-probe.yml` in Tasks 1 and 6
and in the file table. The three pinned action SHAs are quoted once in the Global Constraints and referred
to, not retyped, in Tasks 1 and 5.
