#!/usr/bin/env python3
"""Refuse a pull request that touches an already-accepted ADR at all.

docs/decisions/README.md states the rule directly: "An ADR's body is never
rewritten to agree with a later one." The convention it used to point to --
appending a `> **#NNN update:**` blockquote next to a stale claim, as
docs/decisions/0022-added-token-matching-flags.md's section 10 and the
0015/0019 amendment both still show -- has itself been superseded: "Amend 0004
in a decision of its own instead of editing it" pulled three such blockquotes
back out of decision 0004 and put what they said in a new decision, 0043,
because "a decision record is not edited; an amendment is its own record."
That is now the whole rule, addition included, not just removal -- and nothing
enforced either version of it before this script. An edit to an already-merged
ADR is still valid markdown, still passes every other gate, and says nothing
about itself.

Only files that already existed at --base are covered: a brand-new ADR in the
same pull request is unrestricted, and so is docs/decisions/README.md, which is
the index rather than a decision and is expected to gain a row on every ADR.

Usage:  python tools/check_adr_immutable.py --base <commit>
        python tools/check_adr_immutable.py --help

  --base <commit>  What the pull request is compared against. Required: an
                    implicit default would silently compare against the wrong
                    thing on a rebased or force-pushed branch.
  --help, -h       Print this message to stdout and exit 0.

Exit:   0 clean, 1 findings printed, 2 bad usage
"""

from __future__ import annotations

import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent

# The index (docs/decisions/README.md) is deliberately unmatched: it is not an
# ADR body, and every new decision adds a row to it by design.
ADR_PATH = re.compile(r"^docs/decisions/\d{4}-.*\.md$")

# A leading '-' would read to git as an option rather than a revision -- the
# same guard tools/select_benchmarks.py's REVISION applies to its own --since.
REVISION = re.compile(r"^[0-9A-Za-z][0-9A-Za-z._/~^-]{0,254}$")


def existed_at(base: str, path: str) -> bool:
    """Whether `path` was already a file at `base`, not introduced by this PR."""
    result = subprocess.run(
        ["git", "cat-file", "-e", f"{base}:{path}"],
        cwd=ROOT, capture_output=True, check=False)
    return result.returncode == 0


def changed_adr_files(base: str) -> list[str]:
    """Every docs/decisions/ ADR the diff against `base` touches, README.md excluded."""
    out = subprocess.run(
        ["git", "diff", "--name-only", base, "--", "docs/decisions/"],
        cwd=ROOT, capture_output=True, text=True, check=True)
    return [line for line in out.stdout.splitlines() if ADR_PATH.match(line)]


def line_counts(base: str, path: str) -> tuple[int, int]:
    """(added, removed) lines the diff against `base` reports for `path`."""
    out = subprocess.run(
        ["git", "diff", "--numstat", base, "--", path],
        cwd=ROOT, capture_output=True, text=True, check=True)
    added, removed, _ = out.stdout.strip().split("\t", 2)
    return int(added), int(removed)


def main(argv: list[str]) -> int:
    arguments = argv[1:]
    if "--help" in arguments or "-h" in arguments:
        print(__doc__)
        return 0
    if len(arguments) != 2 or arguments[0] != "--base":
        print(__doc__, file=sys.stderr)
        return 2
    base = arguments[1]
    if not REVISION.match(base):
        print(f"--base {base!r} is not a usable revision", file=sys.stderr)
        return 2

    findings = []
    for path in changed_adr_files(base):
        if not existed_at(base, path):
            continue
        added, removed = line_counts(base, path)
        findings.append((path, added, removed))

    for path, added, removed in findings:
        print(f"{path}: {added} line(s) added, {removed} removed -- already accepted.")

    if findings:
        print(
            f"\n{len(findings)} ADR file(s) touched past what an accepted decision "
            "allows -- not even a `> **#<issue> update:**` blockquote, per "
            "\"Amend 0004 in a decision of its own instead of editing it\". Revert "
            "the change and record it as a new ADR instead, indexed in "
            "docs/decisions/README.md.",
            file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
