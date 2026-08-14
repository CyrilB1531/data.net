#!/usr/bin/env python3
"""Refuse a comment block that runs past eight lines without saying why.

What set the two budgets, measured on 2026-08-14 over C# files: 1837 comment
blocks holding 9803 lines, of which 354 ran past eight and held 5532 -- and
316 of those 354 were XML documentation, which CLAUDE.md requires on every
public member. A single eight-line cap would have been a cap on documenting
the API. Run --report for what the tree holds now; a total pinned in this
docstring would be false at the next commit. About one block in five carries more than half the
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

import re
import pathlib
import subprocess
import sys
from collections import namedtuple

ROOT = pathlib.Path(__file__).resolve().parent.parent

# long-comment: the measurement that set both budgets, which a reader
# changing either one needs
# Two budgets, because the two kinds of prose sit in different places. An
# inline comment stands between a reader and the code, so it is a sentence:
# measured, 446 blocks run past two lines today, holding 2391 of them. XML
# documentation is the member's own interface, read by a caller who does not
# have the source, and CLAUDE.md requires every public member to carry it --
# so it gets eight, counted over prose only.
THRESHOLD_INLINE = 2
THRESHOLD_DOC = 8
MARKER = "long-comment:"

# long-comment: why structural elements are exempt, measured
# An XML documentation element a well-formed member must carry anyway: a
# <param> per parameter, an <exception> per throw, a <summary> that the
# analyzers require. Measured: counting these against the budget puts 316 of
# the 354 over-length C# blocks inside public API documentation, which
# CLAUDE.md separately requires every public member to have -- so the cap
# would have been a cap on documenting the API rather than on prose. Counting
# only the prose leaves 228, and a well-formed 12-line block with a four-line
# <remarks> passes while a three-paragraph essay does not.
STRUCTURAL = re.compile(
    r"</?(summary|param|returns|exception|typeparam|value|inheritdoc|seealso)")

# The whole vocabulary: a suffix not listed here is skipped, not guessed at.
LEADERS: dict[str, tuple[str, ...]] = {
    ".cs": ("///", "//"),
    ".py": ("#",),
}

Block = namedtuple("Block", "line length prose doc marked suppression")
Finding = namedtuple("Finding", "path line length")


def _is_marker(content: str) -> bool:
    """A marker is the prefix AND a reason: an empty one is the cheapest rubber stamp."""
    if not content.lower().startswith(MARKER):
        return False
    return bool(content[len(MARKER):].strip())


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
    if stripped.startswith(("#!", "# -*-")):
        return False, ""
    return True, stripped[len(leader):].strip()


def _justifies_a_suppression(lines: list[str], after: int) -> bool:
    """Whether the block ending here is the reason above a #pragma.

    CONTRIBUTING.md requires a suppression to carry one and CLAUDE.md refuses
    a reason "a reviewer cannot disagree with", which is a stricter demand
    than brevity and not one two lines usually meet. Such a block is governed
    by that rule rather than by this budget. Measured: 94 blocks across the tree
    are a suppression's reason, and 63 of those would be reported if this budget
    governed them.
    """
    return after < len(lines) and "#pragma warning disable" in lines[after]


def _doc_prefix(lines: list[str], start: int, length: int) -> tuple[int, int]:
    """The block's opening XML documentation lines, and how many are prose.

    A /// run and a // run touching each other are one block to the scanner but
    two things to the rules: only the // part can be a suppression's reason, and
    the /// part keeps its own budget however the block ends.

    The prose count is returned alongside because the split path below cannot
    reuse the whole block's: STRUCTURAL is exempt wherever it appears, and a
    member whose /// run is nothing but a <summary> and eight <param> lines --
    FBeta.Score, whose eight parameters are what its S107 suppression is about
    -- spends none of the budget on them.
    """
    count = 0
    prose = 0
    for offset in range(length):
        stripped = lines[start - 1 + offset].strip()
        if not stripped.startswith("///"):
            break
        count += 1
        if not STRUCTURAL.search(stripped[len("///"):].strip()):
            prose += 1
    return count, prose


def _close(lines: list[str], after: int, start: int, length: int,
           prose: int, doc: bool, marked: bool) -> Block:
    """The block that just ended, with the suppression rule applied to it.

    A /// run touching a // run is one run to the scanner and two things to the
    rules: only the // part can be a suppression's reason, so the documentation
    above it keeps its own budget rather than riding the exemption out.
    """
    suppression = _justifies_a_suppression(lines, after)
    documented, documented_prose = _doc_prefix(lines, start, length)
    if suppression and 0 < documented < length:
        return Block(start, documented, documented_prose, True, marked, False)
    return Block(start, length, prose, doc, marked, suppression)


def blocks_in(lines: list[str], suffix: str) -> list[Block]:
    """Every run of consecutive comment lines in `lines`, as `Block`s.

    A block ends on the first line that is not a comment line -- blank,
    code, or the end of the file. `marked` reports whether the block's own
    first line carries `MARKER` right after its leader, which is what
    `findings_in` below checks against the budget for its kind.
    """
    leaders = LEADERS.get(suffix)
    if not leaders:
        return []

    result: list[Block] = []
    start = 0
    length = 0
    prose = 0
    doc = False
    marked = False

    for number, raw in enumerate(lines, start=1):
        is_comment, content = _is_comment_line(raw.strip(), leaders)
        if is_comment:
            if length == 0:
                start = number
                marked = _is_marker(content)
                prose = 0
                doc = raw.strip().startswith("///")
            length += 1
            if not STRUCTURAL.search(content):
                prose += 1
        elif length:
            result.append(_close(lines, number - 1, start, length, prose, doc, marked))
            length = 0

    if length:
        result.append(Block(start, length, prose, doc, marked, False))

    return result


def _budget(block) -> int:
    """Two lines for an inline block, eight for XML documentation."""
    return THRESHOLD_DOC if block.doc else THRESHOLD_INLINE


def findings_in(lines: list[str], suffix: str) -> list[Finding]:
    """The blocks whose PROSE runs past their budget with no `MARKER`.

    Structural XML elements do not spend the budget: see STRUCTURAL above for
    the measurement that decided it.
    """
    return [
        Finding("", block.line, block.prose)
        for block in blocks_in(lines, suffix)
        if block.prose > _budget(block) and not block.marked and not block.suppression
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


Tally = namedtuple("Tally", "blocks lines long_blocks long_lines marked findings")


def _walk() -> Tally:
    """Every tracked file the guard understands, counted once."""
    blocks = lines = long_blocks = long_lines = marked = 0
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
            blocks += 1
            lines += block.length
            if block.prose <= _budget(block) or block.suppression:
                continue
            long_blocks += 1
            long_lines += block.prose
            if block.marked:
                marked += 1
            else:
                findings.append(Finding(path, block.line, block.prose))

    return Tally(blocks, lines, long_blocks, long_lines, marked, findings)


def _print_report(tally: Tally) -> None:
    """The counts, for a sweep that wants to know how much is left."""
    print(f"{tally.blocks} comment blocks, {tally.lines} comment lines")
    print(f"{tally.long_blocks} run past their budget ({THRESHOLD_INLINE} inline, "
          f"{THRESHOLD_DOC} documentation), holding {tally.long_lines} prose lines")
    print(f"{tally.marked} of those {tally.long_blocks} carry a {MARKER} marker")


def main(argv: list[str]) -> int:
    arguments = argv[1:]
    early_exit = _parse_arguments(arguments)
    if early_exit is not None:
        return early_exit

    tally = _walk()

    if "--report" in arguments:
        _print_report(tally)
        return 0

    findings = tally.findings
    for finding in findings:
        print(f"{finding.path}:{finding.line}: {finding.length} lines, no {MARKER} marker")

    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
