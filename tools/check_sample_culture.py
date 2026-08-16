#!/usr/bin/env python3
"""Fail when the sample can print a number in the contributor's culture.

The sample is the packaging gate: CI runs it on every pull request and a
contributor reads its output to see that a package works. String interpolation
formats through CurrentCulture, so `{value:F3}` printed 0,807 on a French
console and 0.807 on CI -- the same commit, two outputs, and nothing failing,
because the gate checks that every public type is reachable rather than what
the run said (#205, and docs/decisions/0019 which left it open).

CA1305 cannot catch this. It fires on an explicit ToString(string) and never on
an interpolated hole, at any AnalysisMode -- the gap is in the rule, not in the
configuration, so raising AnalysisLevel would not surface one of them.

Two things are checked, because neither covers the other:

  1. No interpolated hole carries a numeric format specifier. Those are the
     rewritable ones, and Inv.F3(...) is what they become.
  2. Program.cs still pins the thread culture. That covers what a syntactic
     scan cannot -- a bare {value} hole whose expression is a double, which
     reads exactly like a bare {count} hole whose expression is an int.

Exit codes: 0 clean, 1 a finding, 2 bad usage or a tree that is not shaped as
expected.
"""
from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SAMPLE = "samples/Lodestar.Sample"
ENTRY_POINT = f"{SAMPLE}/Program.cs"
HELPER = f"{SAMPLE}/Inv.cs"

# A hole's closing half, walked backwards to its brace by _opening_brace below.
# Letter then digits only -- the sample embeds JSON, where ":10}" is data.
FORMAT_SUFFIX = re.compile(r":[A-Za-z]\d*\}")

# {expr,10:F3}. Reported like the rest, and named, because the rewrite to
# Inv.F3(expr) has to keep the alignment rather than swallow it.
ALIGNED_HOLE = re.compile(r"^\{.*,\s*-?\d+\s*:[A-Za-z]\d*\}$")

# What Program.cs must still do. Matched on the assignment rather than on the
# comment above it, so rewording the comment does not fail the build.
PINS_CULTURE = re.compile(
    r"CultureInfo\.DefaultThreadCurrentCulture\s*=\s*CultureInfo\.InvariantCulture")

USAGE = "usage: check_sample_culture.py"


def tracked_sample_sources() -> list[str]:
    """The sample's tracked .cs files, repository-relative, and never bin/ or obj/.

    git ls-files rather than a glob: the build output holds copies of these same
    sources, and a sweep that reached them would report findings nobody can fix
    while a green run proved nothing about the files that ship.
    """
    listed = subprocess.run(
        ["git", "-C", str(ROOT), "ls-files", f"{SAMPLE}/*.cs"],
        capture_output=True, text=True, check=True)
    return listed.stdout.split()


def _opening_brace(line: str, close: int) -> int:
    """The index of the brace that opens the hole closing at <paramref>close</paramref>, or -1.

    Walked backwards with a depth counter rather than matched: the expression can
    hold balanced braces of its own, and an object initializer in an argument list
    routinely does.
    """
    depth = 0
    for index in range(close, -1, -1):
        if line[index] == "}":
            depth += 1
        elif line[index] == "{":
            depth -= 1
            if depth == 0:
                return index
    return -1


def formatted_holes(line: str) -> list[str]:
    """Every interpolated hole on one line that carries a format specifier."""
    holes = []
    for suffix in FORMAT_SUFFIX.finditer(line):
        close = suffix.end() - 1
        start = _opening_brace(line, close)
        if start >= 0:
            holes.append(line[start:close + 1])
    return holes


def _holes_in(relative: str) -> list[str]:
    """Every formatted hole in one file, already rendered as a finding line."""
    findings = []
    text = (ROOT / relative).read_text(encoding="utf-8")
    for number, line in enumerate(text.splitlines(), start=1):
        for hole in formatted_holes(line):
            kind = "aligned " if ALIGNED_HOLE.match(hole) else ""
            findings.append(
                f"{relative}:{number}: {kind}interpolated hole formats with the "
                f"current culture: {hole}")
    return findings


def main(argv: list[str]) -> int:
    if argv[1:]:
        print(USAGE, file=sys.stderr)
        return 2

    sources = tracked_sample_sources()
    if not sources:
        print(f"error: no tracked .cs files under {SAMPLE}", file=sys.stderr)
        return 2

    findings = []
    for relative in sources:
        findings.extend(_holes_in(relative))

    entry = ROOT / ENTRY_POINT
    if not entry.exists():
        print(f"error: {ENTRY_POINT} is missing", file=sys.stderr)
        return 2
    if not PINS_CULTURE.search(entry.read_text(encoding="utf-8")):
        findings.append(
            f"{ENTRY_POINT}: no longer pins CultureInfo.DefaultThreadCurrentCulture "
            "to InvariantCulture, which is what covers a hole carrying no format "
            "specifier")

    if findings:
        for finding in findings:
            print(finding)
        print(f"\n{len(findings)} finding(s). Print numbers through Inv.F3 and its "
              f"siblings in {HELPER}; see issue #205.")
        return 1

    print(f"ok  {len(sources)} sample sources print no number in the contributor's culture")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
