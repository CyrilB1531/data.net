#!/usr/bin/env python3
"""Generate the benchmark corpus for DataNet.Metrics (issue #61).

Written rather than committed, like bench/corpus/vocabs: both language sides
read these same files, which is what makes the comparison mean anything, and the
bytes do not need to be reproducible across machines.

    python bench/corpus/generate_metrics.py
"""

from __future__ import annotations

import json
import math
import sys
from pathlib import Path

# Standalone script, not a package: puts the repository root on sys.path so the
# import below resolves the way every static analyser expects it to.
sys.path.append(str(Path(__file__).resolve().parents[2]))

from tools.seeded_random import SeededRandom  # noqa: E402

SEED = 20260806
OUT = Path(__file__).resolve().parent / "metrics"

# (samples, classes). 10-class scores stop at 100_000 rows: a million rows by
# ten classes is 200 MB of JSON, measuring the disk rather than the metric.
SHAPES = [(1_000, 2), (1_000, 10), (100_000, 2), (100_000, 10), (1_000_000, 2), (1_000_000, 10)]
SCORE_LIMIT = 100_000


def softmax(row: list[float]) -> list[float]:
    top = max(row)
    exps = [math.exp(v - top) for v in row]
    total = sum(exps)
    return [v / total for v in exps]


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    rng = SeededRandom(SEED)

    for n, k in SHAPES:
        y_true = [rng.randrange(k) for _ in range(n)]
        y_pred = [t if rng.random() < 0.7 else rng.randrange(k) for t in y_true]
        payload = {
            "samples": n,
            "classes": k,
            "y_true": y_true,
            "y_pred": y_pred,
            "sample_weight": [round(rng.uniform(0.1, 3.0), 3) for _ in range(n)],
            "binary_scores": [round(rng.random() * 0.6 + (0.4 if t == 1 else 0.0), 9)
                              for t in y_true] if k == 2 else None,
            "scores": None,
        }
        if k > 2 and n <= SCORE_LIMIT:
            rows = []
            for t in y_true:
                logits = [rng.gauss(0.0, 1.0) for _ in range(k)]
                logits[t] += 1.5
                rows.append([round(v, 9) for v in softmax(logits)])
            payload["scores"] = rows

        # A separate SeededRandom, not more `rng` draws, so #61/#93's already-measured
        # arrays don't shift; seeded by `n` alone — see docs/guides/performance.md.
        real_rng = SeededRandom(SEED + 1_000 + n)
        truth = [round(real_rng.uniform(0.5, 100.0), 9) for _ in range(n)]
        payload["y_true_real"] = truth
        payload["y_pred_real"] = [round(t + real_rng.gauss(0.0, 5.0), 9) for t in truth]

        path = OUT / f"metrics_n{n}_k{k}.json"
        with path.open("w", encoding="utf-8") as f:
            json.dump(payload, f)
        print(f"{path.name}: {n} samples, {k} classes")


if __name__ == "__main__":
    main()
