#!/usr/bin/env python3
"""The measurement both rapidfuzz benchmarks make, written once.

Methodology is deliberately mirrored by the C# harness
(bench/Lodestar.Text.Benchmarks, `compare` mode) so the two are comparable:

  * same committed corpus (bench/corpus/pairs.json),
  * throughput metric: nanoseconds per pair over the whole bucket,
  * auto-scaling: repeat the bucket until a measurement lasts >= MIN_TIME,
  * report the best (minimum) of REPEATS measurements.

We time the realistic loop a Python user writes (one call per pair). rapidfuzz
also offers batch APIs (process.cdist) that amortise the Python->C boundary; the
comparison note in docs/guides/performance.md mentions this.
"""

from __future__ import annotations

import json
import platform
from importlib.metadata import version
from pathlib import Path
from time import perf_counter

MIN_TIME = 0.5   # seconds per measurement
REPEATS = 5      # best-of

ROOT = Path(__file__).resolve().parent.parent
CORPUS = ROOT / "corpus" / "pairs.json"


def time_bucket(pairs, distance) -> float:
    """Return nanoseconds per pair (best of REPEATS)."""
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


def run(distance, out_name: str) -> None:
    """Times `distance` over every bucket of the shared corpus and writes the result."""
    out = ROOT / "results" / out_name
    corpus = json.loads(CORPUS.read_text(encoding="utf-8"))
    results = []
    for bucket in corpus["buckets"]:
        length = bucket["length"]
        # Two buckets answer to every length since #406; the alphabet is what tells
        # them apart, here and in the join bench/compare.py makes downstream.
        alphabet = bucket["alphabet"]
        kind = bucket["kind"]
        band = bucket.get("band")
        pairs = bucket["pairs"]
        ns = time_bucket(pairs, distance)
        results.append({
            "length": length, "alphabet": alphabet, "kind": kind, "band": band,
            "pairs": len(pairs), "ns_per_pair": ns,
        })
        label = f"band={band:>3}" if band is not None else f"len={length:>4}"
        print(f"  {alphabet:>5} {label}  {ns:10.1f} ns/pair")

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
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    print(f"-> {out}")
