#!/usr/bin/env python3
"""Time rapidfuzz's Indel on the shared corpus, per length bucket.

The distance behind fuzz.ratio: Indel is len(a) + len(b) - 2*LCS, so this
measures the longest-common-subsequence kernel on both sides rather than an edit
distance with substitution. The measurement is harness.py's, shared with
bench_levenshtein.py so the two numbers can be read against each other.
"""

from __future__ import annotations

import sys
from pathlib import Path

# PYTHONSAFEPATH, which this repository sets elsewhere, stops Python putting the
# script's own directory on the path, and the sibling module lives there.
sys.path.insert(0, str(Path(__file__).resolve().parent))

from rapidfuzz.distance import Indel

from harness import run


def main() -> None:
    run(Indel.distance, "python-indel.json")


if __name__ == "__main__":
    main()
