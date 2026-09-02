#!/usr/bin/env python3
"""Generate the dictionary BkTreeBenchmarks indexes. Git-ignored, like the vocabularies.

Two shapes, because they prune differently: uniform random words, and words clustered
around roots the way a natural dictionary is. The clustered corpus is the honest one --
near neighbours are what a BK-tree has to work through.

    python bench/corpus/generate_dictionary.py
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

# Standalone script, not a package: puts the repository root on sys.path so the
# import below resolves the way every static analyser expects it to.
sys.path.append(str(Path(__file__).resolve().parents[2]))

from tools.python_floor import require_supported_python  # noqa: E402

# Before the seeded_random import, never after: that module is PEP 695, so an
# interpreter below the floor fails parsing it first (issue #486).
require_supported_python("bench/corpus/generate_dictionary.py")

from tools.seeded_random import SeededRandom  # noqa: E402

ALPHABET = "abcdefghijklmnopqrstuvwxyz"
SEED = 20260902
SIZE = 20000
OUT = Path(__file__).resolve().parent / "dictionary.json"


def _word(rng: SeededRandom) -> str:
    return "".join(rng.choice(ALPHABET) for _ in range(rng.randint(4, 10)))


def _uniform(rng: SeededRandom) -> list[str]:
    words: set[str] = set()
    while len(words) < SIZE:
        words.add(_word(rng))
    return sorted(words)


def _clustered(rng: SeededRandom) -> list[str]:
    roots = [_word(rng) for _ in range(SIZE // 8)]
    words: set[str] = set(roots)
    while len(words) < SIZE:
        letters = list(rng.choice(roots))
        for _ in range(rng.randint(1, 2)):
            op = rng.randint(0, 2)
            if op == 0 and letters:
                letters[rng.randrange(len(letters))] = rng.choice(ALPHABET)
            elif op == 1:
                letters.insert(rng.randrange(len(letters) + 1), rng.choice(ALPHABET))
            elif op == 2 and len(letters) > 1:
                letters.pop(rng.randrange(len(letters)))
        words.add("".join(letters))
    return sorted(words)


def main() -> None:
    rng = SeededRandom(SEED)
    payload = {"seed": SEED, "size": SIZE,
               "uniform": _uniform(rng), "clustered": _clustered(rng)}
    with OUT.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, allow_nan=False)
    print(f"dictionary.json: {SIZE} uniform + {SIZE} clustered -> {OUT}")


if __name__ == "__main__":
    main()
