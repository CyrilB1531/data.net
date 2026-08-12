# Machine-path guard — implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fail the `Lint` job when a tracked file contains a path under someone's home directory, so the
next one is caught by a check rather than by a reader.

**Architecture:** One standard-library script, `tools/check_machine_paths.py`, on the pattern
`check_version_floor.py` establishes. It carries two probe sets — named shapes, and probes derived at run
time from `$HOME` including its dashed form — and exempts only its own source and test module. `pytest`
already runs over `tools/tests`, so the guard's tests ride there, holding the strings that actually
occurred.

**Tech Stack:** Python 3.12, standard library only, `pytest` for the tests, one step in the GitHub Actions
`Lint` job.

**Spec:** `docs/superpowers/specs/2026-08-12_0133_machine-path-guard.md`

**Issue:** [#133](https://github.com/CyrilB1531/data.net/issues/133) ·
**Branch:** `chore/133-machine-path-guard`, off `main` (already created; the spec commit is on it)

## Global Constraints

- **Everything in English** — code, comments, commit messages, and the script's own output.
- **Standard library only.** `tools/requirements.lock.txt` exists for the oracle generator; this script
  must not need it, because the `Lint` job installs nothing but `pytest`.
- **Since #109 the build enforces nine Sonar rules locally**, and Python rules remain visible only to the
  quality gate and to SonarLint. `python:S1192` fires at **three** occurrences of a literal — name a
  constant rather than repeating one, and prefer reusing an existing constant over defining a second
  holding the same value.
- **A wrong claim in a comment is a defect** here. For every comment you write, the question is not whether
  it reads plausibly but **what you would run to check it, and whether you ran it**.
- **Do not paste a matching literal into any document.** The spec's D4b decision: the script and its test
  module may contain the patterns because they must, and everything else describes the shapes in prose.
  A plan that pasted one would fail the guard it specifies.
- **`dotnet format` and markdownlint do not run per task on this branch.** They run once, after the last
  task, and again after any review fixes. Per task, the gate is:

  ```bash
  python -m pytest tools/tests -q
  ```

  **Read the test count, not the colour** — a filter that matches nothing exits zero and reports success.

## What the guard is for, and the trap it has to avoid

The obvious pattern is not enough, and the measurement says so. Run against `main` before the sweep, over
every tracked text file, patterns matching a named directory under `/home`, under `/Users`, under
`C:\Users`, and the root user's home caught the **two** runner paths in #70's documents and **none** of the
eight scratchpad paths in four plans.

The scratchpad names itself after the absolute path of the checkout it belongs to, with the separators
replaced by dashes. Nothing searching for a slash-separated home directory sees it. **A guard that shipped
with only the named shapes would have reported clean on the eight occurrences that carried a real home
directory** — which is why the environment-derived probes exist and why Task 2 comes before Task 3.

## The plan and the spec are scanned too

The guard exempts its own source and its own test module, and nothing else — including these documents.
That is not an oversight to work around but a constraint on how they are written, and it shaped two
details below:

- the root-directory pattern requires a following path character, so the line defining it does not match
  itself, and a mention of the directory in prose is not a finding;
- every path-shaped literal in the tests is assembled from pieces, so no source line holds a whole one.

Both were found by running the guard's own patterns over this plan, which flagged it twice before they
were applied.

## File Structure

| File | Responsibility |
| --- | --- |
| `tools/check_machine_paths.py` | **New.** The whole guard: probes, scan, report, exit code. |
| `tools/tests/test_check_machine_paths.py` | **New.** Its tests, holding the strings that actually occurred. |
| `.github/workflows/ci.yml` | One step in the `Lint` job. |
| `CONTRIBUTING.md` | One sentence under the existing local-checks list. |

---

### Task 1: The script, with the named shapes only

Deliberately incomplete: this task ships a guard that catches two of the ten paths that occurred. Task 2
adds what catches the other eight. Splitting them is what makes Task 2's test meaningful — it fails against
Task 1's script for the reason the spec exists.

**Files:**

- Create: `tools/check_machine_paths.py`
- Create: `tools/tests/test_check_machine_paths.py`

**Interfaces:**

- Consumes: nothing.
- Produces, for Tasks 2 and 3:
  - `NAMED_SHAPES` — `tuple[tuple[str, re.Pattern[str]], ...]`, each `(description, pattern)`
  - `EXEMPT` — `frozenset[str]`, repository-relative paths the scan skips
  - `scan_text(text: str, probes) -> list[tuple[int, str]]` — `(line number, matched text)`, 1-based
  - `tracked_files() -> list[str]` — repository-relative paths from `git ls-files`
  - `main(argv: list[str]) -> int` — `0` clean, `1` findings, `2` bad usage

- [ ] **Step 1: Write the failing tests**

`tools/tests/test_check_machine_paths.py`:

```python
"""The guard's own tests, holding the strings that actually reached this repository.

Issue #133 exists because ten absolute paths were committed and nothing caught
them. Six of the strings below are those, recovered from the commits that
removed them, rather than examples someone invented -- which is the only way
these prove the guard catches what happened instead of what was imagined.

The module under test contains the patterns it searches for, so it exempts
itself and this file. Everything else in the repository is scanned.
"""
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import check_machine_paths as guard  # noqa: E402


# The two CI runner paths, as they appeared in issue #70's spec and plan.
RUNNER_PATH = "Base dir: " + "/home/" + "runner/work/data.net/data.net"

# A home-directory path in the shape a contributor's terminal produces.
POSIX_HOME = "/home/" + "someone/Documents/devs/data.net"
MAC_HOME = "/Users/" + "someone/src/data.net"
WINDOWS_HOME = "C:\\\\Users\\\\someone\\\\src"

# Load-bearing paths that must never be flagged. The oracle generator has to
# run from a neutral directory -- nltk refuses to import its dependencies when
# they appear to live under the current one -- so /tmp appears in CLAUDE.md,
# in CONTRIBUTING.md and in several plans.
NEUTRAL = "cd /tmp && python tools/generate_oracles.py"
SYSTEM = "/usr/bin/env python3"
TILDE = "~/.nuget/packages"


def scan(text):
    return guard.scan_text(text, guard.NAMED_SHAPES)


def test_a_runner_checkout_path_is_flagged():
    assert scan(RUNNER_PATH)


def test_a_posix_home_directory_is_flagged():
    assert scan(POSIX_HOME)


def test_a_mac_home_directory_is_flagged():
    assert scan(MAC_HOME)


def test_a_windows_home_directory_is_flagged():
    assert scan(WINDOWS_HOME)


def test_the_neutral_working_directory_is_not_flagged():
    # /tmp is load-bearing here; a guard that refused it would break the
    # documented way to run the oracle generator.
    assert not scan(NEUTRAL)


def test_system_paths_and_tilde_are_not_flagged():
    assert not scan(SYSTEM)
    assert not scan(TILDE)


def test_the_report_names_the_line():
    text = "clean line\n" + POSIX_HOME + "\n"
    findings = scan(text)

    assert findings[0][0] == 2


def test_the_guard_exempts_only_itself_and_its_tests():
    assert guard.EXEMPT == frozenset({
        "tools/check_machine_paths.py",
        "tools/tests/test_check_machine_paths.py",
    })
```

The literals are assembled by concatenation on purpose: written whole they would make this file match the
patterns it tests, and the exemption covers the file rather than making it harmless to write them.

- [ ] **Step 2: Run them and watch them fail**

```bash
python -m pytest tools/tests/test_check_machine_paths.py -q
```

Expected: collection error — no module named `check_machine_paths`.

- [ ] **Step 3: Write the script**

`tools/check_machine_paths.py`:

```python
#!/usr/bin/env python3
"""Refuse a tracked file that contains a path under someone's home directory.

Ten such paths reached this public repository before anything looked for them,
and both sweeps that removed them started from a reader noticing a line rather
than from a check. They arrive by being pasted from a terminal, which is
exactly when nobody is thinking about what the string contains.

What this does *not* refuse is an absolute path. /tmp is load-bearing here --
nltk refuses to import its dependencies when they appear to live under the
current directory, so the oracle generator has to run from somewhere neutral,
and that instruction is in CLAUDE.md, in CONTRIBUTING.md and in several plans.
/usr, /etc and ~/.nuget likewise appear legitimately. The question asked here
is narrower: is this a path under a home directory.

This module and its test module contain the patterns they search for, so both
are exempt. Nothing else is, deliberately -- an exemption list that grows is a
guard being switched off one file at a time.

Usage:  python tools/check_machine_paths.py [--no-environment]
Exit:   0 clean, 1 findings printed, 2 bad usage
"""

from __future__ import annotations

import re
import subprocess
import sys

# A directory named after a person, under the place each platform keeps them.
# The trailing separator is required: it is what distinguishes a path from a
# mention of the directory itself in prose.
NAMED_SHAPES: tuple[tuple[str, re.Pattern[str]], ...] = (
    ("a home directory under /home", re.compile(r"/home/[A-Za-z0-9._-]+/")),
    ("a home directory under /Users", re.compile(r"/Users/[A-Za-z0-9._-]+/")),
    ("a Windows home directory", re.compile(r"[A-Za-z]:\\{1,2}Users\\{1,2}[A-Za-z0-9._-]+")),
    # A following path character is required, so that a mention of the
    # directory in prose is not a finding and, more usefully, so that this
    # very line does not match the pattern it defines.
    ("the root user's home directory", re.compile(r"/root/[A-Za-z0-9._-]")),
    # The session scratch directory, which names itself after the absolute
    # path of the checkout it belongs to. This is the shape the four plans
    # carried, and the one no slash-separated pattern sees.
    ("a session scratch directory", re.compile(r"/tmp/claude-\d+/")),
)

EXEMPT = frozenset({
    "tools/check_machine_paths.py",
    "tools/tests/test_check_machine_paths.py",
})


def scan_text(text: str, probes) -> list[tuple[int, str]]:
    """Every (1-based line number, matched text) `probes` finds in `text`."""
    findings = []
    for _, pattern in probes:
        for match in pattern.finditer(text):
            findings.append((text.count("\n", 0, match.start()) + 1, match.group(0)))
    return sorted(findings)


def tracked_files() -> list[str]:
    """Every path `git ls-files` reports, in repository-relative form."""
    listing = subprocess.run(
        ["git", "ls-files"], capture_output=True, text=True, check=True)
    return listing.stdout.split("\n")[:-1] if listing.stdout else []


def main(argv: list[str]) -> int:
    for argument in argv[1:]:
        if argument != "--no-environment":
            print(__doc__, file=sys.stderr)
            return 2

    probes = NAMED_SHAPES
    findings = 0
    for path in tracked_files():
        if path in EXEMPT:
            continue
        try:
            text = pathlib.Path(path).read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue
        for line, matched in scan_text(text, probes):
            print(f"{path}:{line}: {matched}")
            findings += 1

    if findings:
        print(
            f"\n{findings} machine path(s) in tracked files. "
            "Replace them with $SCRATCH, $(mktemp -d), or a description of what the path held.",
            file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
```

`pathlib` is used and not imported above — add `import pathlib` with the other imports. This is stated
rather than left silent because the code block is otherwise complete and an implementer transcribing it
would meet the error at run time instead of at reading time.

- [ ] **Step 4: Run the tests, then the guard over the repository**

```bash
python -m pytest tools/tests/test_check_machine_paths.py -q
python tools/check_machine_paths.py; echo "exit=$?"
```

Expected: the tests pass, and the guard prints nothing and exits 0 — `main` was swept clean by #135, and
the spec measured 519 tracked text files with zero hits.

**If it reports a finding, do not exempt the file.** Read what it found: either the sweep missed something,
which is worth knowing, or a pattern is too broad, which is worth fixing.

- [ ] **Step 5: Commit**

```bash
git add tools/check_machine_paths.py tools/tests/test_check_machine_paths.py
git commit -m "$(cat <<'EOF'
Refuse a tracked file holding a path under someone's home directory

Ten of them reached this public repository before anything looked, and both
sweeps that removed them started from a reader noticing a line. They arrive
by being pasted from a terminal, which is exactly when nobody is thinking
about what the string contains.

The guard asks whether a path is under a home directory, never whether it is
absolute. /tmp is load-bearing here -- nltk refuses to import its
dependencies from the working directory, so the oracle generator runs from
somewhere neutral, and that instruction is in CLAUDE.md, in CONTRIBUTING.md
and in several plans.

It exempts its own source and its own test module, which have to contain the
patterns to exist, and nothing else: an exemption list that grows is a guard
being switched off one file at a time.

This catches two of the ten paths that occurred. The other eight are the
next commit's, and they are the ones that mattered.

Issue #133
EOF
)"
```

---

### Task 2: The probes derived from `$HOME`, which catch what the shapes miss

**Files:**

- Modify: `tools/check_machine_paths.py`
- Modify: `tools/tests/test_check_machine_paths.py`

**Interfaces:**

- Consumes: `NAMED_SHAPES`, `scan_text`, `main` from Task 1.
- Produces, for Task 3: `environment_probes(home: str | None) -> tuple[tuple[str, re.Pattern[str]], ...]`

- [ ] **Step 1: Write the failing tests**

Append to `tools/tests/test_check_machine_paths.py`:

```python
# The scratchpad path as it appeared in four plans, with the name redacted the
# way the spec redacts it -- the shape is what matters, and a whole one here
# would put a home directory back into a tracked file.
SCRATCH = "/tmp/claude-" + "49201103/" + "-home-" + "someone-Documents-devs-data-net2/x/scratchpad"


def test_the_named_shapes_alone_miss_the_dashed_form():
    # The finding that shaped this guard: the scratchpad encodes the home
    # directory with dashes, so nothing searching for a slash-separated one
    # sees it. Only the /tmp/claude- prefix catches this string by shape.
    dashed_only = "-home-" + "someone-Documents-devs-data-net2"

    assert not guard.scan_text(dashed_only, guard.NAMED_SHAPES)


def test_an_environment_probe_catches_the_dashed_form():
    probes = guard.environment_probes("/home/" + "someone")
    dashed_only = "-home-" + "someone-Documents-devs-data-net2"

    assert guard.scan_text(dashed_only, probes)


def test_an_environment_probe_catches_the_home_path_itself():
    probes = guard.environment_probes("/home/" + "someone")

    assert guard.scan_text(POSIX_HOME, probes)


def test_an_environment_probe_needs_a_boundary_around_the_name():
    # A username that appears inside an unrelated word is not a path, and a
    # guard that said otherwise would fire on prose for any contributor
    # unlucky enough to be called something ordinary.
    probes = guard.environment_probes("/home/" + "ed")

    assert not guard.scan_text("the edited plan", probes)
    assert guard.scan_text("/home/" + "ed/src", probes)


def test_no_home_means_no_environment_probes():
    assert guard.environment_probes(None) == ()
```

- [ ] **Step 2: Run them and watch them fail**

```bash
python -m pytest tools/tests/test_check_machine_paths.py -q
```

Expected: four failures — `environment_probes` does not exist — and
`test_the_named_shapes_alone_miss_the_dashed_form` **passes**. That passing test is the point: it records
that Task 1's guard is blind here, which is why this task exists.

- [ ] **Step 3: Add the derived probes**

In `tools/check_machine_paths.py`, after `EXEMPT`:

```python
def environment_probes(home: str | None) -> tuple[tuple[str, re.Pattern[str]], ...]:
    """Probes for *this* machine's home directory, in the forms it gets written.

    The named shapes above are a list, and a list is never complete. These are
    derived instead, so they catch shapes nobody enumerated -- on the machine
    where the string is created, and on CI, where $HOME is the runner's own
    home and one of the two paths this guard exists because of had that shape.

    Three forms, because a home directory reaches a file in three ways: the
    path itself; the account name inside some longer path; and the path with
    its separators replaced by dashes, which is what a session scratch
    directory is named after. The dashed form is the one the named shapes
    miss, and it carried eight of the ten paths that occurred.

    The account name is matched only when a separator or a dash bounds it, so
    a contributor called `ed` does not turn every "edited" into a finding.
    """
    if not home:
        return ()

    account = home.rsplit("/", 1)[-1]
    if not account:
        return ()

    return (
        ("this machine's home directory", re.compile(re.escape(home) + r"[/\\]")),
        ("this machine's account name", re.compile(r"[/\\-]" + re.escape(account) + r"[/\\-]")),
        ("this machine's home directory, dash-separated",
         re.compile(re.escape(home.replace("/", "-")) + r"[-/\\]")),
    )
```

and use them in `main`, replacing `probes = NAMED_SHAPES`:

```python
    probes = NAMED_SHAPES
    if "--no-environment" not in argv[1:]:
        probes += environment_probes(os.environ.get("HOME"))
```

Add `import os` with the other imports.

- [ ] **Step 4: Run the tests, then the guard over the repository**

```bash
python -m pytest tools/tests/test_check_machine_paths.py -q
python tools/check_machine_paths.py; echo "exit=$?"
```

Expected: all tests pass, and the guard still exits 0 over the repository — now with the derived probes
active, which on this machine search for the account name as well.

**If the guard now reports findings, read them before doing anything else.** A derived probe firing on a
tracked file means either a real path the sweep missed, or an account name common enough to collide, which
is what `--no-environment` exists for and what the spec's D3 anticipated.

- [ ] **Step 5: Commit**

```bash
git add tools/check_machine_paths.py tools/tests/test_check_machine_paths.py
git commit -m "$(cat <<'EOF'
Derive probes from $HOME, because the named shapes are a list

Measured against main before the sweep, patterns for a named directory under
/home, /Users, C:\Users and the root user's home caught the two runner paths
in issue #70's documents and none of the eight scratchpad paths in four
plans. The scratchpad names itself after the absolute path of the checkout it
belongs to, with the separators replaced by dashes, so nothing searching for
a slash-separated home directory sees it.

A guard shipping with the named shapes alone would therefore have reported
clean on the eight occurrences that carried a real home directory, which is
worse than no guard. A test now records that blindness rather than leaving it
to be rediscovered.

The derived probes take $HOME and search for it three ways: the path, the
account name bounded by a separator or a dash, and the path with its
separators replaced by dashes. They work on both sides -- on CI $HOME is the
runner's home, which is the shape of one of the two paths this guard exists
because of.

Issue #133
EOF
)"
```

---

### Task 3: The gate, and the sentence that tells a contributor about it

**Files:**

- Modify: `.github/workflows/ci.yml`, the `Lint` job
- Modify: `CONTRIBUTING.md`

**Interfaces:**

- Consumes: `tools/check_machine_paths.py` from Tasks 1 and 2.
- Produces: nothing.

- [ ] **Step 1: Read the job you are adding to**

```bash
sed -n '10,50p' .github/workflows/ci.yml
```

The `Lint` job checks out, sets up .NET, runs markdownlint pinned to a version with `--ignore-scripts`,
runs `dotnet format --verify-no-changes`, sets up Python, installs from the hashed lock file, and runs
`python -m pytest tools/tests -q`. Your step goes after the `pytest` step, since both are Python and the
guard needs no install of its own.

- [ ] **Step 2: Add the step**

After the `python -m pytest tools/tests -q` step in the `Lint` job:

```yaml
      # An absolute path under a home directory reaches a file by being pasted
      # from a terminal, which is exactly when nobody is thinking about what the
      # string contains. Ten of them were committed to this public repository
      # before anything looked for them.
      - name: No machine paths in tracked files
        run: python tools/check_machine_paths.py
```

`$HOME` is set on the runner, so the derived probes are active there too and search for `runner`.

- [ ] **Step 3: Add the sentence to `CONTRIBUTING.md`**

Find the list of checks a contributor runs locally — the one naming markdownlint and
`dotnet format --verify-no-changes` — and add the guard beside them, in the same shape the neighbours use:

```markdown
python tools/check_machine_paths.py
```

with one sentence of prose above or beside it, matching how the neighbouring entries are introduced, saying
that it refuses a tracked file holding a path under someone's home directory and that `/tmp` and system
paths are deliberately allowed. **Do not paste an example that matches** — describe the shapes, as the spec
and this plan do, or the guard will fail on `CONTRIBUTING.md`.

- [ ] **Step 4: Verify the workflow parses and the guard still passes**

```bash
python -c "import yaml,sys; yaml.safe_load(open('.github/workflows/ci.yml')); print('ci.yml parses')"
python tools/check_machine_paths.py; echo "exit=$?"
python -m pytest tools/tests -q
```

Expected: `ci.yml parses`, `exit=0`, and the whole `tools/tests` suite green — not just this guard's file,
since the step you added runs the whole directory.

If `yaml` is not importable, skip that check and say so in your report; `actionlint` is not installed here
and the workflow's syntax is otherwise verified by CI itself.

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/ci.yml CONTRIBUTING.md
git commit -m "$(cat <<'EOF'
Run the machine-path guard in the job that gates the merge

One step in Lint, beside markdownlint and the format check, after the
existing pytest step -- both are Python and the guard installs nothing of its
own. $HOME is set on the runner, so the derived probes are active there too.

CONTRIBUTING gains the command beside the other local checks. Its sentence
describes the shapes rather than showing one, because an example that matched
would make the guard fail on the document explaining it.

Closes #133
EOF
)"
```

---

## Self-review

**Spec coverage.** D1's two probe sets are Tasks 1 and 2 — named shapes first, derived second, in that
order so that Task 2's test can record what Task 1 misses. D2, the guard being about home directories
rather than absolute paths, is Task 1's `NEUTRAL`/`SYSTEM`/`TILDE` tests and the docstring's paragraph.
D3, the boundary around a generic account name, is
`test_an_environment_probe_needs_a_boundary_around_the_name` and `--no-environment`. D4, the `Lint` step
and no hook, is Task 3 — no task creates a hook. D4b, the two-file exemption, is
`test_the_guard_exempts_only_itself_and_its_tests`, which asserts the set is exactly those two so that a
third addition fails a test rather than passing quietly. D5, tests carrying the strings that occurred, is
Task 1's `RUNNER_PATH` and Task 2's `SCRATCH`.

**Placeholders.** None. One step deliberately points out a missing import in its own code block rather than
silently correcting it, because an implementer transcribing the block should meet that at reading time.

**Type consistency.** `scan_text(text, probes)` takes the probe tuple in both tasks and returns
`list[tuple[int, str]]`; Task 1's tests index `[0][0]` for a line number and Task 2's use it as a truth
value, both consistent with that. `NAMED_SHAPES` and the return of `environment_probes` are the same shape
— `tuple[tuple[str, re.Pattern[str]], ...]` — which is what lets `main` concatenate them with `+=`.
`EXEMPT` is a `frozenset[str]` in the module and is compared against a `frozenset` in the test.
`main(argv)` returns `0`/`1`/`2`, matching `check_version_floor.py`'s convention.

**What this plan cannot prove.** That the named shapes are complete — nothing can, and the spec says so.
The derived probes narrow it on the machine where a path is created; the tests stop the named shapes from
regressing. Neither closes the gap, and Task 2's commit message says which of the two matters more.
