#!/usr/bin/env python3
"""Generate a shared, committed corpus for the cross-language performance bench.

Both the Python harness (rapidfuzz) and the C# harness load *this same file*, so
the comparison measures the algorithms on identical inputs. Every bucket stays
inside the Basic Multilingual Plane, where UTF-16 code units and Unicode code
points coincide, so the C# default (Utf16Unit) and rapidfuzz (code points)
compute the same values — the comparison is apples to apples. That is why the
wide buckets are CJK and not emoji: a supplementary character is one code point
and two UTF-16 units, and the two sides would stop measuring the same quantity.

Deterministic (fixed seed, no timestamps): committing the JSON is part of the
change and diffs stay reviewable.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

# Standalone script, not a package: puts the repository root on sys.path so the
# import below resolves the way every static analyser expects it to.
sys.path.append(str(Path(__file__).resolve().parents[2]))

from tools.seeded_random import SeededRandom  # noqa: E402

SEED = 20260801
LENGTHS = [8, 32, 128, 512]
PAIRS_PER_BUCKET = 1000

LATIN = "latin"
CJK = "cjk"

# Two alphabets of 27 symbols, so a bucket differs from its twin only in where its
# characters sit: Latin in the kernels' dense table, CJK in the side one (#406).
ALPHABETS = {
    LATIN: "abcdefghijklmnopqrstuvwxyz ",
    CJK: "一二三四五六七八九十百千万上下左右前後東西南北中大小山",
}

OUT = Path(__file__).resolve().parent / "pairs.json"


def rand_string(rng: SeededRandom, length: int, alphabet: str) -> str:
    return "".join(rng.choice(alphabet) for _ in range(length))


def mutate(rng: SeededRandom, s: str, edits: int, alphabet: str) -> str:
    chars = list(s)
    for _ in range(edits):
        op = rng.choice(("ins", "del", "sub")) if chars else "ins"
        c = rng.choice(alphabet)
        if op == "ins":
            chars.insert(rng.randint(0, len(chars)), c)
        elif op == "del":
            del chars[rng.randint(0, len(chars) - 1)]
        else:
            chars[rng.randint(0, len(chars) - 1)] = c
    return "".join(chars)


def build(rng: SeededRandom, name: str) -> list[dict]:
    """One bucket per length, all drawn from the alphabet `name` selects."""
    alphabet = ALPHABETS[name]
    buckets = []
    for length in LENGTHS:
        edits = max(1, length // 10)  # ~10% typo rate: near-duplicate matching
        pairs = []
        for _ in range(PAIRS_PER_BUCKET):
            a = rand_string(rng, length, alphabet)
            b = mutate(rng, a, edits, alphabet)
            pairs.append([a, b])
        buckets.append({"length": length, "alphabet": name, "pairs": pairs})
    return buckets


def main() -> None:
    rng = SeededRandom(SEED)
    # Latin first, CJK strictly after: one stream feeds both, and drawing the wide
    # buckets any earlier would shift every Latin pair already published (#406).
    buckets = build(rng, LATIN) + build(rng, CJK)

    payload = {
        "metadata": {
            "seed": SEED,
            "lengths": LENGTHS,
            "pairs_per_bucket": PAIRS_PER_BUCKET,
            "alphabets": {name: len(a) for name, a in ALPHABETS.items()},
            "edit_rate": 0.10,
        },
        "buckets": buckets,
    }
    with OUT.open("w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, indent=1)
        f.write("\n")
    total = sum(len(b["pairs"]) for b in buckets)
    print(f"corpus: {total} pairs across {len(buckets)} buckets -> {OUT}")


if __name__ == "__main__":
    main()
