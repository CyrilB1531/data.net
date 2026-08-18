#!/usr/bin/env python3
"""Time rapidfuzz's Levenshtein on the shared corpus, per length bucket.

The measurement itself lives in harness.py, which bench_indel.py shares: the two
differ only in which rapidfuzz distance they hand it, and timing them the same
way is the whole point of comparing them.
"""

from __future__ import annotations

import sys
from pathlib import Path

# PYTHONSAFEPATH, which this repository sets elsewhere, stops Python putting the
# script's own directory on the path, and the sibling module lives there.
sys.path.insert(0, str(Path(__file__).resolve().parent))

from rapidfuzz.distance import Levenshtein

from harness import run


def main() -> None:
    run(Levenshtein.distance, "python-levenshtein.json")


if __name__ == "__main__":
    main()
