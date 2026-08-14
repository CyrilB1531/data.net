#!/usr/bin/env python3
"""Count comment blocks that name a reference library, and how many cite evidence.

Issue #151's sweep reported "162 claims, 0 cited" and it was wrong twice: it
counted LINES and looked for the citation on the same line, which a block almost
never satisfies, and it swept in obj/. The corrected figure was then quoted as
prose in a commit message and did not reproduce for a reviewer, because nothing
said what counts as a citation.

So the rule lives here rather than in a sentence. A block claims if it names a
reference library. It cites if it points at something a reader can open: a
corpus file, an oracle case, an ADR, an issue, a test class, or a stated
measurement.

A <see cref> does not count. It is a cross-reference to another member of this
library, not a pointer to what checks the claim -- counting it reads 45% where
the honest figure is 12%.

Usage:  python3 tools/count_cited_claims.py [path-prefix ...]
        python3 tools/count_cited_claims.py --help

Exit:   0. This reports; #155 wires the gate that blocks.
"""

from __future__ import annotations

import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

import check_comment_length as guard  # noqa: E402

LIBRARY = re.compile(
    r"scikit-learn|sklearn|numpy|HuggingFace|rapidfuzz|jellyfish|textdistance"
    r"|tokenizers|sentencepiece|difflib|nltk")
EVIDENCE = re.compile(
    r"tests/oracles|\.json\b|docs/decisions/\d{4}|ADR \d{4}|#\d{2,}"
    r"|measured|[A-Za-z]+Tests\b"
    # A line in the reference's own source is something a reader can open, and
    # leaving it out undercounted a sweep that had cited four of them.
    r"|[\w/]+\.py:\d+")
CROSS_REFERENCE = re.compile(r"<see\s+cref=\"[^\"]*\"\s*/?>")


def _claiming_blocks(path: str):
    """Every block in one file that names a reference library, with its body."""
    suffix = pathlib.Path(path).suffix
    if suffix not in guard.LEADERS:
        return
    try:
        lines = (guard.ROOT / path).read_text(encoding="utf-8").split("\n")
    except (OSError, UnicodeDecodeError):
        return
    for block in guard.blocks_in(lines, suffix):
        body = "\n".join(lines[block.line - 1:block.line - 1 + block.length])
        if LIBRARY.search(body):
            yield block, body


def survey(prefixes: tuple[str, ...]) -> tuple[int, int, list[str]]:
    """Blocks naming a library, how many cite, and where the uncited ones are."""
    claimed = cited = 0
    uncited: list[str] = []
    for path in guard.tracked_files():
        if prefixes and not path.startswith(prefixes):
            continue
        for block, body in _claiming_blocks(path):
            claimed += 1
            if EVIDENCE.search(CROSS_REFERENCE.sub("", body)):
                cited += 1
            else:
                uncited.append(f"{path}:{block.line}")
    return claimed, cited, uncited


def main(argv: list[str]) -> None:
    """Prints the survey. There is no verdict here, so there is none to return."""
    if len(argv) > 1 and argv[1] in ("--help", "-h"):
        print(__doc__)
        return
    claimed, cited, uncited = survey(tuple(argv[1:]))
    scope = " ".join(argv[1:]) or "the whole tree"
    share = f"{100 * cited // claimed}%" if claimed else "n/a"
    print(f"{claimed} blocks name a reference library in {scope}")
    print(f"{cited} of them cite something a reader can open ({share})")
    for place in uncited[:20]:
        print(f"    uncited: {place}")
    if len(uncited) > 20:
        print(f"    ... and {len(uncited) - 20} more")


if __name__ == "__main__":
    main(sys.argv)
