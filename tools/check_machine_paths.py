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

import pathlib
import re
import subprocess
import sys

# A directory named after a person, under the place each platform keeps them.
# The trailing separator is required: it is what distinguishes a path from a
# mention of the directory itself in prose.
NAMED_SHAPES: tuple[tuple[str, re.Pattern[str]], ...] = (
    ("a home directory under /home", re.compile(r"/home/[A-Za-z0-9._-]+/")),
    ("a home directory under /Users", re.compile(r"/Users/[A-Za-z0-9._-]+/")),
    ("a Windows home directory",
     re.compile(r"[A-Za-z]:\\{1,2}Users\\{1,2}[A-Za-z0-9._-]+\\{1,2}")),
    # A following path character is required, so that a mention of the
    # directory in prose is not a finding and, more usefully, so that this
    # very line does not match the pattern it defines.
    ("the root user's home directory", re.compile(r"/root/[A-Za-z0-9._-]")),
    # The session scratch directory, which names itself after the absolute
    # path of the checkout it belongs to. This is the shape the four plans
    # carried, and the one no slash-separated pattern sees.
    #
    # S5443 flags this literal as a world-writable directory used unsafely --
    # the rule is about code that opens, writes to or resolves a path under
    # /tmp, where a symlink planted by another user can redirect the write.
    # This string is never used that way: it is a search pattern matched
    # against file *contents*, never passed to open(), joined into a path, or
    # resolved against the filesystem. /tmp appears here only because that is
    # where the session scratch directory this pattern hunts for actually
    # lives.
    ("a session scratch directory", re.compile(r"/tmp/claude-\d+/")),  # NOSONAR S5443
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
