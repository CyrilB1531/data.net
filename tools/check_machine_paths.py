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
        python tools/check_machine_paths.py --help

  --no-environment  Skip the probes derived from this machine's $HOME (its
                    home directory, its account name, and the dashed form a
                    session scratch directory is named after). Use this when
                    an ordinary account name -- src, build, net, docs, main,
                    dev and the like -- collides with prose already in the
                    tree and turns the guard into noise; the named shapes
                    above still run either way.
  --help, -h        Print this message to stdout and exit 0.

Exit:   0 clean, 1 findings printed, 2 bad usage
"""

from __future__ import annotations

import os
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent

# Windows accepts either separator; a backslash in a JSON/C# string literal
# comes out doubled. Matches single or doubled backslash, or a single slash.
_WINDOWS_SEP = r"(?:\\{1,2}|/)"

# A directory named after a person, under the place each platform keeps them.
# Trailing separator distinguishes a path from a prose mention.
NAMED_SHAPES: tuple[tuple[str, re.Pattern[str]], ...] = (
    ("a home directory under /home", re.compile(r"/home/[A-Za-z0-9._-]+/")),
    ("a home directory under /Users", re.compile(r"/Users/[A-Za-z0-9._-]+/")),
    ("a Windows home directory",
     re.compile(r"[A-Za-z]:" + _WINDOWS_SEP + r"Users" + _WINDOWS_SEP
                + r"[A-Za-z0-9._-]+" + _WINDOWS_SEP)),
    # A UNC path to a profile on a network share -- no drive letter, so the
    # pattern above cannot see it. "Users" required, same reason as there.
    ("a Windows home directory on a network share",
     re.compile(r"\\{2,4}[A-Za-z0-9._-]+\\{1,2}Users\\{1,2}[A-Za-z0-9._-]+\\{1,2}")),
    # A following path character is required so a prose mention of /root
    # isn't flagged, and so this line does not match its own pattern.
    ("the root user's home directory", re.compile(r"/root/[A-Za-z0-9._-]")),
    # Named after the checkout's absolute path, dash-separated -- the shape
    # #133's spec found in four plans, invisible to a slash pattern.

    # S5443 false positive: this /tmp literal is a search pattern over file
    # contents, never opened, joined into a path, or resolved.
    ("a session scratch directory", re.compile(r"/tmp/claude-\d+/")),  # NOSONAR S5443
)

EXEMPT = frozenset({
    "tools/check_machine_paths.py",
    "tools/tests/test_check_machine_paths.py",
})


# S8495 false positive: the return type is a variable-length tuple, not a
# fixed 3-tuple -- every caller iterates, concatenates or compares it below, never unpacks by position.
def environment_probes(home: str | None) -> tuple[tuple[str, re.Pattern[str]], ...]:  # NOSONAR S8495
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

    The caller passes whichever of $HOME or %USERPROFILE% is set, checking
    $HOME first: a probe job on windows-latest measured $HOME unset in both
    PowerShell and cmd (with %USERPROFILE% holding the runner's profile),
    while Git Bash on Windows does set $HOME, so a guard reading only one of
    the two is inert in half the shells a contributor on Windows might use.

    Known blind spot, left open rather than half-fixed: that same probe job
    found %TEMP% resolved to the 8.3 short name
    C:\\Users\\RUNNER~1\\AppData\\Local\\Temp against that %USERPROFILE%, so a
    path written from %TEMP% sits inside the profile while matching none of
    these three probes -- resolving short names back to their long form is
    not portable enough to attempt here.
    """
    if not home:
        return ()

    # Stripped first: an unstripped trailing separator would survive into the
    # rsplit below and leave account == "", dropping all three probes.
    home = home.rstrip("/\\")
    if not home:
        return ()

    account = re.split(r"[/\\]", home)[-1]
    if not account:
        return ()

    return (
        ("this machine's home directory", re.compile(re.escape(home) + r"[/\\]")),
        ("this machine's account name", re.compile(r"[/\\-]" + re.escape(account) + r"[/\\-]")),
        ("this machine's home directory, dash-separated",
         re.compile(re.escape(home.replace("/", "-")) + r"[-/\\]")),
    )


def scan_text(text: str, probes) -> list[tuple[int, str, str]]:
    """Every (1-based line number, probe description, matched text) `probes` finds in `text`."""
    findings = []
    for description, pattern in probes:
        for match in pattern.finditer(text):
            findings.append((text.count("\n", 0, match.start()) + 1, description, match.group(0)))
    return sorted(findings)


def tracked_files() -> list[str]:
    """Every path `git ls-files` reports, in repository-relative form.

    Run with cwd pinned to ROOT: git resolves relative to the process's
    current directory otherwise, and so does every read in main() below, so a
    guard invoked from a subdirectory would silently scan a fraction of the
    repository and still exit 0.
    """
    listing = subprocess.run(
        ["git", "ls-files"], capture_output=True, text=True, check=True, cwd=ROOT)
    return listing.stdout.split("\n")[:-1] if listing.stdout else []


def _parse_arguments(arguments: list[str]) -> int | None:
    """Handle `--help`/`-h` and reject anything but `--no-environment`.

    Returns the exit code main() should return immediately, or None to mean
    "keep going" -- pulled out of main() to keep its cognitive complexity
    under the limit the rest of the repository holds itself to.
    """
    if "--help" in arguments or "-h" in arguments:
        print(__doc__)
        return 0

    for argument in arguments:
        if argument != "--no-environment":
            print(__doc__, file=sys.stderr)
            return 2

    return None


def _failure_message(findings: int, derived_matched: bool) -> str:
    """The stderr summary after a scan that found something."""
    message = (
        f"\n{findings} machine path(s) in tracked files. "
        "Replace them with $SCRATCH, $(mktemp -d), or a description of what the path held."
    )
    if derived_matched:
        message += (
            " At least one of those came from a probe derived from this "
            "machine's $HOME, which can be a false positive for an ordinary "
            "account name -- rerun with --no-environment to check against "
            "the named shapes alone."
        )
    return message


def main(argv: list[str]) -> int:
    arguments = argv[1:]
    early_exit = _parse_arguments(arguments)
    if early_exit is not None:
        return early_exit

    use_environment = "--no-environment" not in arguments
    derived = environment_probes(
        os.environ.get("HOME") or os.environ.get("USERPROFILE")) if use_environment else ()
    probes = NAMED_SHAPES + derived
    derived_descriptions = frozenset(description for description, _ in derived)

    findings = 0
    derived_matched = False
    for path in tracked_files():
        if path in EXEMPT:
            continue
        try:
            text = (ROOT / path).read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue
        for line, description, matched in scan_text(text, probes):
            print(f"{path}:{line}: {description}: {matched}")
            findings += 1
            if description in derived_descriptions:
                derived_matched = True

    if findings:
        print(_failure_message(findings, derived_matched), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
