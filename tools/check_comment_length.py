#!/usr/bin/env python3
"""Refuse a comment block that runs past eight lines without saying why.

Measured on 2026-08-14 across src/, tests/, tools/, bench/ and samples/: 2056
comment blocks holding 10586 lines, of which 372 run past eight lines and hold
5779 of those lines. About one block in five carries more than half the
prose, and the longest runs 63 lines
(src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs:7).

Long is not banned. A block past the threshold carries `long-comment:` and a
reason as its first line, which is the bargain a #pragma warning disable
strikes: allowed, deliberate, and reviewable. This guard sees only that the
marker exists -- whether the block deserved one is a code review's call, and
CONTRIBUTING.md's "Claims in comments" says so.

A docstring is not a comment block. Python prose belongs in one, and the tools
in this directory open with thirty-line docstrings on purpose.

Usage:  python tools/check_comment_length.py [--report]
        python tools/check_comment_length.py --help

  --report  Print the block, line and marker counts and exit 0 without
            failing. What the sweep uses to see how much is left.
  --help, -h  Print this message to stdout and exit 0.

Exit:   0 clean, 1 findings printed, 2 bad usage
"""

from __future__ import annotations

import pathlib
import subprocess
import sys
from collections import namedtuple

ROOT = pathlib.Path(__file__).resolve().parent.parent

THRESHOLD = 8
MARKER = "long-comment:"

# The file suffixes this guard understands, and the leader(s) that make a
# line of each a comment line. A suffix not listed here is skipped, not
# guessed at -- LEADERS is the whole vocabulary.
LEADERS: dict[str, tuple[str, ...]] = {
    ".cs": ("///", "//"),
    ".py": ("#",),
}

Block = namedtuple("Block", "line length marked")
Finding = namedtuple("Finding", "path line length")


def _leader_for(stripped: str, leaders: tuple[str, ...]) -> str | None:
    """The longest leader `stripped` starts with, or None.

    Longest first so a `///` line is not mistaken for a plain `//` one when
    both are in play -- only `.cs` has more than one leader today, but the
    check costs nothing when there is only one to try.
    """
    for leader in sorted(leaders, key=len, reverse=True):
        if stripped.startswith(leader):
            return leader
    return None


def _is_comment_line(stripped: str, leaders: tuple[str, ...]) -> tuple[bool, str]:
    """Whether `stripped` is a comment line, and its content past the leader.

    A `#!` shebang and a `# -*- coding: ... -*-` declaration both start with
    `.py`'s `#` leader but are not comments -- every tool in tools/ opens with
    both, and counting them would make each file start one line into a block
    it never wrote.
    """
    leader = _leader_for(stripped, leaders)
    if leader is None:
        return False, ""
    if stripped.startswith("#!") or stripped.startswith("# -*-"):
        return False, ""
    return True, stripped[len(leader):].strip()


def blocks_in(lines: list[str], suffix: str) -> list[Block]:
    """Every run of consecutive comment lines in `lines`, as `Block`s.

    A block ends on the first line that is not a comment line -- blank,
    code, or the end of the file. `marked` reports whether the block's own
    first line carries `MARKER` right after its leader, which is what
    `findings_in` below checks against `THRESHOLD`.
    """
    leaders = LEADERS.get(suffix)
    if not leaders:
        return []

    result: list[Block] = []
    start = 0
    length = 0
    marked = False

    for number, raw in enumerate(lines, start=1):
        is_comment, content = _is_comment_line(raw.strip(), leaders)
        if is_comment:
            if length == 0:
                start = number
                marked = content.startswith(MARKER)
            length += 1
        elif length:
            result.append(Block(start, length, marked))
            length = 0

    if length:
        result.append(Block(start, length, marked))

    return result


def findings_in(lines: list[str], suffix: str) -> list[Finding]:
    """The blocks in `lines` past `THRESHOLD` whose first line has no `MARKER`."""
    return [
        Finding("", block.line, block.length)
        for block in blocks_in(lines, suffix)
        if block.length > THRESHOLD and not block.marked
    ]


def tracked_files() -> list[str]:
    """Every path `git ls-files` reports, in repository-relative form.

    Run with cwd pinned to ROOT, as `check_machine_paths.py` does: git
    resolves relative to the process's current directory otherwise, so a
    guard invoked from a subdirectory would silently scan a fraction of the
    repository and still exit 0 -- the bug issue #133 shipped and fixed.
    """
    listing = subprocess.run(
        ["git", "ls-files"], capture_output=True, text=True, check=True, cwd=ROOT)
    return listing.stdout.split("\n")[:-1] if listing.stdout else []


def _parse_arguments(arguments: list[str]) -> int | None:
    """Handle `--help`/`-h` and reject anything but `--report`.

    Returns the exit code main() should return immediately, or None to mean
    "keep going".
    """
    if "--help" in arguments or "-h" in arguments:
        print(__doc__)
        return 0

    for argument in arguments:
        if argument != "--report":
            print(__doc__, file=sys.stderr)
            return 2

    return None


def main(argv: list[str]) -> int:
    arguments = argv[1:]
    early_exit = _parse_arguments(arguments)
    if early_exit is not None:
        return early_exit

    report = "--report" in arguments

    total_blocks = 0
    total_lines = 0
    long_blocks = 0
    long_lines = 0
    marked_blocks = 0
    findings: list[Finding] = []

    for path in tracked_files():
        suffix = pathlib.Path(path).suffix
        if suffix not in LEADERS:
            continue
        try:
            text = (ROOT / path).read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue

        for block in blocks_in(text.split("\n"), suffix):
            total_blocks += 1
            total_lines += block.length
            if block.length > THRESHOLD:
                long_blocks += 1
                long_lines += block.length
                if block.marked:
                    marked_blocks += 1
                else:
                    findings.append(Finding(path, block.line, block.length))

    if report:
        print(f"{total_blocks} comment blocks, {total_lines} comment lines")
        print(f"{long_blocks} run past {THRESHOLD} lines, holding {long_lines} of them")
        print(f"{marked_blocks} of those {long_blocks} carry a {MARKER} marker")
        return 0

    for finding in findings:
        print(f"{finding.path}:{finding.line}: {finding.length} lines, no {MARKER} marker")

    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
