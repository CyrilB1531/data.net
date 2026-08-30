#!/usr/bin/env python3
"""Refuse a Console call in a shipped package, and an unexplained one in a bench.

`src/` is absolute: nothing under it prints. A library that writes to a console
its caller did not open is a library deciding for an application it cannot see,
and the packages here have never done it -- this guard exists so that stays true
by construction rather than by everyone remembering. There is no marker for
`src/`, because there is no reason that would survive review.

`bench/` is different, and the difference is what this guard is really about.
Ten Console calls there were a run narrating itself: banners, one line per
measured row, `-> path` after a file was written. None of them was in a timed
region and none changed a number, which is exactly why they accumulated -- each
was harmless, and nobody was counting. Four remain because each carries
something no file does: that the benchmark loaded the wrong build and its
results are meaningless, which assembly it did load, why a cell is missing from
a table, and the two group sizes without which a diagnostic class's own rows
cannot be read.

So a bench Console call needs a marker naming its reason, on its own line above
it or trailing the call:

    // console-print: says the wrong build was measured, or the run is a lie

An exemption list in this file would rot the way `check_machine_paths.py` says
they rot -- switched off a file at a time, by someone who is not the reviewer.
A marker rots in the diff that adds it, in front of the person who can refuse it.
Decision 0045 has the argument and what the four marked calls carry.

Usage:  python tools/check_no_console_writeline.py [--report]

  --report  Print the marked calls and exit 0 without judging them.
"""
from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MARKER = "console-print:"

# Console.Write, .WriteLine, .Error.Write, .Out.Write, .OpenStandardOutput -- the
# call, not the word, so a comment or an identifier that contains it is not a hit.
CONSOLE = re.compile(r"\bConsole\s*\.\s*(?:Error\s*\.|Out\s*\.)?(?:Write|Open)")

# This module and its test carry the pattern they search for, and neither is C#.
SCANNED = ("src/", "bench/")


def tracked_sources() -> list[str]:
    """Every tracked `.cs` file under the scanned roots, repository-relative."""
    listing = subprocess.run(
        ["git", "ls-files", "--cached", "--others", "--exclude-standard", "--", "src", "bench"],
        capture_output=True, text=True, check=True, cwd=ROOT)
    paths = listing.stdout.split("\n")[:-1] if listing.stdout else []

    # Untracked sources included: a new file is where an unmarked call arrives,
    # and the index cannot see one until it has already been committed.
    return [
        p for p in paths
        if p.endswith(".cs") and p.startswith(SCANNED) and (ROOT / p).is_file()
    ]


def _marked(lines: list[str], index: int) -> bool:
    """Whether the call on `index` carries a marker with a reason.

    Trailing on the call or on the line above it: a `Console.WriteLine` whose
    arguments span several lines has nowhere to put a trailing comment, and
    forcing one would push the reason away from the call it excuses.
    """
    candidates = [lines[index]]
    if index > 0:
        candidates.append(lines[index - 1])
    for line in candidates:
        position = line.lower().find(MARKER)
        if position >= 0 and line[position + len(MARKER):].strip():
            return True
    return False


def findings_in(path: str) -> list[tuple[int, str, bool]]:
    """Each Console call in `path`: its line, the line's text, and whether it is marked."""
    lines = (ROOT / path).read_text(encoding="utf-8").splitlines()
    found = []
    for index, line in enumerate(lines):
        if CONSOLE.search(line) and not line.lstrip().startswith(("//", "///", "*")):
            found.append((index + 1, line.strip(), _marked(lines, index)))
    return found


def _parse_arguments(arguments: list[str]) -> int | None:
    """Handle `--help`/`-h` and reject anything but `--report`.

    Returns the exit code main() should return immediately, or None to mean "keep
    going" -- pulled out of main() to keep its cognitive complexity under the limit
    the rest of the repository holds itself to, the same way
    `check_machine_paths.py` does.
    """
    if "--help" in arguments or "-h" in arguments:
        print(__doc__)
        return 0

    for argument in arguments:
        if argument != "--report":
            print(__doc__, file=sys.stderr)
            return 2

    return None


def _partition() -> tuple[list[str], list[str]]:
    """Every call found, split into the refused and the marked.

    `src/` never reaches the marked side: a shipped package does not print, and the
    guard reads no marker there, so a reason written under `src/` is refused with the
    call it was meant to excuse.
    """
    refused, marked = [], []
    for path in tracked_sources():
        for line, text, is_marked in findings_in(path):
            entry = f"{path}:{line}: {text}"
            if is_marked and path.startswith("bench/"):
                marked.append(entry)
            else:
                refused.append(entry)
    return refused, marked


def _refuse(refused: list[str]) -> int:
    """Print what nothing explains, and how to explain it."""
    for entry in refused:
        print(entry, file=sys.stderr)

    print(
        f"\n{len(refused)} Console call(s) that nothing explains. Under src/ a "
        "shipped package must not print, and there is no marker for it. Under "
        f"bench/ add `// {MARKER} <reason>` on the call or the line above it, "
        "and expect a reviewer to disagree with the reason.",
        file=sys.stderr)
    return 1


def main(argv: list[str]) -> int:
    arguments = argv[1:]
    early = _parse_arguments(arguments)
    if early is not None:
        return early

    refused, marked = _partition()

    if "--report" in arguments:
        for entry in marked:
            print(entry)
        print(f"{len(marked)} marked, {len(refused)} unmarked")
        return 0

    if refused:
        return _refuse(refused)

    print(f"ok  no unexplained Console call under {', '.join(SCANNED)}"
          f" ({len(marked)} marked in bench)")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
