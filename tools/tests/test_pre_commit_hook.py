"""What holds the pre-commit hook and CI together as guards are added.

The hook exists to run CI's offline guards earlier (decision 0037). Nothing in
git makes that true tomorrow: a fifth guard lands as a step in ci.yml, the hook
keeps running four, and the divergence is silent -- the hook still passes, the
push still fails, and the round trip the hook removed is back for one check.

So the relationship is asserted rather than remembered. CI is read as the source
of truth for which guards exist, the hook for which of them run before a commit,
and the difference between the two sets has to be exactly the exclusion decision
0037 wrote down. Adding a guard to CI without a decision about the hook fails
here, which is the point.

`OFFLINE_EXCLUSIONS` is the one guard CI runs that the hook deliberately does
not: `check_nuspec_dependencies.py` reads the `.nuspec` files inside a packed
`./artifacts`, so running it before a commit would mean packing four projects
first. It is named there, in 0037, and nowhere else.

The floor guard needs no exclusion. CI passes it `--check-feed` and the hook does
not, but that is a flag rather than a guard, and its two offline rules run in
both places -- so the set stays about what cannot run offline at all.

Both files are read as text rather than parsed. ci.yml would need a YAML
dependency this repository does not have for its tool tests, and the hook is
shell; a regular expression over `tools/check_*.py` is what the two spellings
have in common, which is why the hook spells its guards as paths.
"""
import re
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
HOOK = REPO / ".githooks" / "pre-commit"
WORKFLOWS = REPO / ".github" / "workflows"

OFFLINE_EXCLUSIONS = {"check_nuspec_dependencies"}

GUARD = re.compile(r"tools/(check_\w+)\.py")


def guards_in(text):
    """Every `tools/check_*.py` a file invokes, as bare guard names."""
    return set(GUARD.findall(text))


def test_the_hook_runs_every_offline_guard_ci_runs():
    in_ci = set()
    for workflow in WORKFLOWS.glob("*.yml"):
        in_ci |= guards_in(workflow.read_text(encoding="utf-8"))

    missing = in_ci - OFFLINE_EXCLUSIONS - guards_in(HOOK.read_text(encoding="utf-8"))

    assert not missing, (
        f"CI runs {sorted(missing)} and .githooks/pre-commit does not. Add the guard "
        "to the hook, or -- if it needs the network, a pack or a build -- to "
        "OFFLINE_EXCLUSIONS here and to decision 0037's list, with the reason."
    )


def test_the_hook_runs_no_guard_that_does_not_exist():
    # A renamed script leaves the hook failing every commit on an interpreter
    # error rather than on a finding.
    for guard in guards_in(HOOK.read_text(encoding="utf-8")):
        assert (REPO / "tools" / f"{guard}.py").is_file(), f"the hook runs a missing {guard}.py"


def test_the_excluded_guards_are_still_real():
    # An exclusion outliving the guard it excuses is how a list like this rots.
    for guard in OFFLINE_EXCLUSIONS:
        assert (REPO / "tools" / f"{guard}.py").is_file(), f"{guard}.py is gone; drop the exclusion"


def test_the_hook_names_neither_interpreter_unconditionally():
    # Hard-coding either name breaks commits on the platform shipping the other,
    # which is a fault the guards themselves do not have.
    text = HOOK.read_text(encoding="utf-8")
    assert "command -v python3" in text
    assert "command -v python " in text


def test_the_hook_is_checked_out_with_unix_line_endings():
    # `#!/bin/sh\r` is not a program any kernel finds, and Git for Windows
    # checks text out as CRLF unless .gitattributes says otherwise.
    assert b"\r\n" not in HOOK.read_bytes()
    attributes = (REPO / ".gitattributes").read_text(encoding="utf-8")
    assert ".githooks/** text eol=lf" in attributes
