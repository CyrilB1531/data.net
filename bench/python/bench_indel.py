#!/usr/bin/env python3
"""Time rapidfuzz's Indel on the shared corpus, per length bucket.

The mirror of bench_levenshtein.py, over the same committed corpus and with the
same methodology, for the distance behind fuzz.ratio: Indel is
len(a) + len(b) - 2*LCS, so this measures the longest-common-subsequence kernel
on both sides rather than an edit distance with substitution.

We time the realistic loop a Python user writes (one call per pair). rapidfuzz
exposes a batch API that is faster; timing it would compare a batch C loop with
a per-call C# one, which is not the comparison this file exists to make.
"""

from __future__ import annotations

import json
import platform
from importlib.metadata import version
from pathlib import Path
from time import perf_counter

from rapidfuzz.distance import Indel

MIN_TIME = 0.5   # seconds per measurement
REPEATS = 5      # best-of

ROOT = Path(__file__).resolve().parent.parent
CORPUS = ROOT / "corpus" / "pairs.json"
OUT = ROOT / "results" / "python-indel.json"


def time_bucket(pairs) -> float:
    """Return nanoseconds per pair (best of REPEATS)."""
    distance = Indel.distance
    n = len(pairs)
    best = float("inf")
    for _ in range(REPEATS):
        iters = 1
        while True:
            t0 = perf_counter()
            for _ in range(iters):
                for a, b in pairs:
                    distance(a, b)
            dt = perf_counter() - t0
            if dt >= MIN_TIME:
                break
            iters *= 2
        best = min(best, dt / (iters * n) * 1e9)
    return best


def main() -> None:
    corpus = json.loads(CORPUS.read_text(encoding="utf-8"))
    results = []
    for bucket in corpus["buckets"]:
        length = bucket["length"]
        pairs = bucket["pairs"]
        ns = time_bucket(pairs)
        results.append({"length": length, "pairs": len(pairs), "ns_per_pair": ns})
        print(f"  len={length:>4}  {ns:10.1f} ns/pair")

    payload = {
        "metadata": {
            "side": "python",
            "library": "rapidfuzz",
            "library_version": version("rapidfuzz"),
            "python": platform.python_version(),
            "machine": platform.machine(),
            "min_time_s": MIN_TIME,
            "repeats": REPEATS,
        },
        "results": results,
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    print(f"-> {OUT}")


if __name__ == "__main__":
    main()
