#!/usr/bin/env python3
"""Time the Python counterparts of the DataNet.Metrics work (issue #61),
extended with balanced accuracy, Matthews correlation and Cohen's kappa
(issue #93), and with four regression operations (issue #92).

Methodology is mirrored by the C# harness (bench/DataNet.Text.Benchmarks,
`compare-metrics` mode) so the two are comparable:

  * same corpus files (bench/corpus/metrics/, from generate_metrics.py),
  * same thirteen operations, in the same order, over the same shapes,
  * metric: milliseconds per operation,
  * auto-scaling: repeat until a measurement lasts >= MIN_TIME,
  * report the best (minimum) of REPEATS measurements,
  * elapsed time (perf_counter) and processor time (process_time) together.

None of the calls below passes sample_weight, on either side of the
comparison -- the corpus carries a weight column for potential future use, but
none of the named operations read it. roc_auc_binary only runs over the
two-class files; roc_auc_ovr_macro only over the ten-class files whose
`scores` matrix the generator actually wrote (it stops at 100 000 rows).
balanced_accuracy, matthews, cohen_kappa and the four regression operations
(mse, mae, median_ae, r2) run over every shape, unlike the two roc_auc rows --
the regression pair (y_true_real/y_pred_real) is continuous data drawn by a
separate SeededRandom, independent of the classification shape it is
attached to.
"""

from __future__ import annotations

import json
import platform
from importlib.metadata import version
from pathlib import Path
from time import perf_counter, process_time

import numpy as np
import sklearn.metrics as skm

MIN_TIME = 0.5
REPEATS = 5

ROOT = Path(__file__).resolve().parent.parent
CORPUS = ROOT / "corpus" / "metrics"
OUT = ROOT / "results" / "python-metrics.json"

SHAPES = [(1_000, 2), (1_000, 10), (100_000, 2), (100_000, 10), (1_000_000, 2), (1_000_000, 10)]


def check_corpus() -> None:
    missing = [f"metrics_n{n}_k{k}.json" for n, k in SHAPES
               if not (CORPUS / f"metrics_n{n}_k{k}.json").exists()]
    if missing:
        raise SystemExit(
            f"benchmark corpus incomplete, missing {missing} in {CORPUS}\n"
            "generate it first: python bench/corpus/generate_metrics.py"
        )


def measure(operation: str, action) -> dict:
    """Time one operation, recording both elapsed time and processor time.

    The C# harness (bench/DataNet.Text.Benchmarks/CrossLang/Harness.cs) records
    the same pair for the same reason: .NET's background collector does its work
    on other threads, so elapsed time understates what an allocating operation
    actually costs. CPython is strictly single-threaded, so cpu/wall lands at
    1.00 here regardless -- which is exactly the point of reporting both.
    """
    best_wall, cpu_of_best = float("inf"), float("nan")
    for _ in range(REPEATS):
        iters = 1
        while True:
            c0, w0 = process_time(), perf_counter()
            for _ in range(iters):
                action()
            dt = perf_counter() - w0
            cpu = process_time() - c0
            if dt >= MIN_TIME:
                break
            iters *= 2
        wall_ms = dt / iters * 1e3
        if wall_ms < best_wall:
            best_wall, cpu_of_best = wall_ms, cpu / iters * 1e3
    print(f"  {operation:<28} {best_wall:10.3f} ms/op  cpu {cpu_of_best:8.3f} ms/op  ({cpu_of_best / best_wall:.2f}x cores)")
    return {"operation": operation, "ms_per_op": best_wall, "cpu_ms_per_op": cpu_of_best}


def load_shape(n: int, k: int) -> dict:
    path = CORPUS / f"metrics_n{n}_k{k}.json"
    payload = json.loads(path.read_text(encoding="utf-8"))
    y_true = np.asarray(payload["y_true"], dtype=np.int64)
    y_pred = np.asarray(payload["y_pred"], dtype=np.int64)
    binary_scores = (np.asarray(payload["binary_scores"], dtype=np.float64)
                      if payload["binary_scores"] is not None else None)
    scores = (np.asarray(payload["scores"], dtype=np.float64)
              if payload["scores"] is not None else None)
    y_true_real = np.asarray(payload["y_true_real"], dtype=np.float64)
    y_pred_real = np.asarray(payload["y_pred_real"], dtype=np.float64)
    return {
        "y_true": y_true,
        "y_pred": y_pred,
        "binary_scores": binary_scores,
        "scores": scores,
        "y_true_real": y_true_real,
        "y_pred_real": y_pred_real,
    }


def measure_shape(n: int, k: int) -> list[dict]:
    data = load_shape(n, k)
    y_true, y_pred = data["y_true"], data["y_pred"]
    suffix = f"n{n}_k{k}"

    results = [
        measure(f"confusion_matrix_{suffix}", lambda: skm.confusion_matrix(y_true, y_pred)),
        measure(f"accuracy_{suffix}", lambda: skm.accuracy_score(y_true, y_pred)),
        measure(f"precision_recall_f1_macro_{suffix}",
                lambda: skm.precision_recall_fscore_support(y_true, y_pred, average="macro", zero_division=0)),
        measure(f"classification_report_{suffix}",
                lambda: skm.classification_report(y_true, y_pred, zero_division=0)),
    ]

    if k == 2 and data["binary_scores"] is not None:
        binary_scores = data["binary_scores"]
        results.append(measure(f"roc_auc_binary_{suffix}", lambda: skm.roc_auc_score(y_true, binary_scores)))

    if k > 2 and data["scores"] is not None:
        scores = data["scores"]
        results.append(measure(
            f"roc_auc_ovr_macro_{suffix}",
            lambda: skm.roc_auc_score(y_true, scores, multi_class="ovr", average="macro"),
        ))

    results.append(measure(f"balanced_accuracy_{suffix}", lambda: skm.balanced_accuracy_score(y_true, y_pred)))
    results.append(measure(f"matthews_{suffix}", lambda: skm.matthews_corrcoef(y_true, y_pred)))
    results.append(measure(f"cohen_kappa_{suffix}", lambda: skm.cohen_kappa_score(y_true, y_pred)))

    yt_real = np.asarray(data["y_true_real"], dtype=np.float64)
    yp_real = np.asarray(data["y_pred_real"], dtype=np.float64)
    results.append(measure(f"mse_{suffix}", lambda: skm.mean_squared_error(yt_real, yp_real)))
    results.append(measure(f"mae_{suffix}", lambda: skm.mean_absolute_error(yt_real, yp_real)))
    results.append(measure(f"median_ae_{suffix}", lambda: skm.median_absolute_error(yt_real, yp_real)))
    results.append(measure(f"r2_{suffix}", lambda: skm.r2_score(yt_real, yp_real)))

    return results


def main() -> None:
    check_corpus()

    print("Python metrics bench")
    results: list[dict] = []
    for n, k in SHAPES:
        results.extend(measure_shape(n, k))

    payload = {
        "metadata": {
            "side": "python",
            "libraries": {
                "scikit-learn": version("scikit-learn"),
                "numpy": version("numpy"),
            },
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
