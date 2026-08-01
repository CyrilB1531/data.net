#!/usr/bin/env python3
"""Generate a shared, committed corpus for the cross-language performance bench.

Both the Python harness (rapidfuzz) and the C# harness load *this same file*, so
the comparison measures the algorithms on identical inputs. The corpus is ASCII
only: UTF-16 code units and Unicode code points coincide there, so the C# default
(Utf16Unit) and rapidfuzz (code points) compute the same values — the comparison
is apples to apples.

Deterministic (fixed seed, no timestamps): committing the JSON is part of the
change and diffs stay reviewable.
"""

from __future__ import annotations

import json
import random
from pathlib import Path

SEED = 20260801
LENGTHS = [8, 32, 128, 512]
PAIRS_PER_BUCKET = 1000
ALPHABET = "abcdefghijklmnopqrstuvwxyz "

OUT = Path(__file__).resolve().parent / "pairs.json"


def rand_string(rng: random.Random, length: int) -> str:
    return "".join(rng.choice(ALPHABET) for _ in range(length))


def mutate(rng: random.Random, s: str, edits: int) -> str:
    chars = list(s)
    for _ in range(edits):
        op = rng.choice(("ins", "del", "sub")) if chars else "ins"
        c = rng.choice(ALPHABET)
        if op == "ins":
            chars.insert(rng.randint(0, len(chars)), c)
        elif op == "del":
            del chars[rng.randint(0, len(chars) - 1)]
        else:
            chars[rng.randint(0, len(chars) - 1)] = c
    return "".join(chars)


def main() -> None:
    rng = random.Random(SEED)
    buckets = []
    for length in LENGTHS:
        edits = max(1, length // 10)  # ~10% typo rate: near-duplicate matching
        pairs = []
        for _ in range(PAIRS_PER_BUCKET):
            a = rand_string(rng, length)
            b = mutate(rng, a, edits)
            pairs.append([a, b])
        buckets.append({"length": length, "pairs": pairs})

    payload = {
        "metadata": {
            "seed": SEED,
            "lengths": LENGTHS,
            "pairs_per_bucket": PAIRS_PER_BUCKET,
            "alphabet_size": len(ALPHABET),
            "edit_rate": 0.10,
        },
        "buckets": buckets,
    }
    with OUT.open("w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, indent=1)
        f.write("\n")
    total = sum(len(b["pairs"]) for b in buckets)
    print(f"corpus: {total} pairs across {len(LENGTHS)} buckets -> {OUT}")


if __name__ == "__main__":
    main()
