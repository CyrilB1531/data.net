# 0438 — `Lodestar.Metrics` against ML.NET's evaluators

**Issue:** [#438](https://github.com/CyrilB1531/lodestar/issues/438) ·
**Status:** accepted · **Date:** 2026-08-31

## Problem

The last of [#438](https://github.com/CyrilB1531/lodestar/issues/438)'s four boxes, and the one
with an expectation attached:

> **`Lodestar.Metrics`** — ML.NET metrics. Expected to favour us on coverage rather than speed; the
> table should say which of the two it measures.

So the lot owes two numbers, not one, and has to keep them apart.

## The harness was wrong first, and nothing said so

ML.NET's binary evaluator thresholds the **`Score` column at zero** and ignores `PredictedLabel`.
Fed probabilities in `[0, 1]` — the obvious content for a column named `Score` — it classified
every row positive:

```text
PREDICTED || positive | negative | Recall
 positive ||   10,007 |        0 | 1.0000
 negative ||    9,993 |        0 | 0.0000
```

Accuracy 0.5003 against our 0.7496. A benchmark written on that harness would have compared a real
classifier to a degenerate one and reported a number.

**AUC agreed throughout**, at 2.5e-8, because it is rank-based and the scores were monotone. The
one metric a careless check looks at is the one that could not catch this. Passing the margin
(`score - 0.5`) makes accuracy and the confusion matrix agree exactly and AUC to 5e-9 — the
residual being ML.NET taking the score as a `float`.

Same shape as the beginning-of-sentence piece in
[the tokenizer lot](2026-08-31_0438_lodestar-embeddings-against-ml-tokenizers.md): a convention on
one side only, silent, fatal to the comparison. Two of the three ML.NET pairs have now had one.

## Coverage, counted

By reflection over both assemblies:

| | types | metric entry points |
| --- | ---: | ---: |
| `Lodestar.Metrics` | 54 static metric classes | 81 distinct public method names |
| ML.NET | 6 result types | 28 distinct properties |

ML.NET's six are `BinaryClassificationMetrics`, `CalibratedBinaryClassificationMetrics`,
`MulticlassClassificationMetrics`, `RegressionMetrics`, `ClusteringMetrics` and `RankingMetrics`.

## Scope — the request is the parameter

`MetricsIncumbentBenchmarks`, `[Params]` over 100 000 and 1 000 000 scored predictions and over
what is asked for:

- **`Bundle`** — the six numbers ML.NET's binary evaluator returns (accuracy, ROC-AUC, average
  precision, F1, precision, recall), from both sides.
- **`AccuracyAlone`** — one number.

ML.NET runs the same `EvaluateNonCalibrated` call in both rows. That is not an unfair setup: it has
no call returning a single metric, so accuracy alone costs a caller the whole bundle. The coverage
difference, made measurable rather than argued.

`Microsoft.ML.Data` is aliased rather than imported — it also has a `ConfusionMatrix`, and
importing the namespace makes every mention of ours ambiguous.

## What the container run showed

| samples | request | Lodestar | ML.NET | ratio | Lodestar alloc. | ML.NET alloc. |
| ---: | --- | ---: | ---: | ---: | ---: | ---: |
| 100 000 | Bundle | 10.13 ms | 38.59 ms | 3.81 | 1 016 B | 5.09 MB |
| 100 000 | AccuracyAlone | 0.31 ms | 39.81 ms | 127.69 | — | 5.09 MB |
| 1 000 000 | Bundle | 162.07 ms | 238.37 ms | 1.47 | — | 23.23 MB |
| 1 000 000 | AccuracyAlone | 3.45 ms | 242.85 ms | 70.30 | — | 23.23 MB |

Issue #438's expectation half holds. On the bundle the advantage narrows with size — 3.81× at 100 000,
1.47× at a million — which is the honest reading: at scale both are doing the same arithmetic over
the same array. What does not narrow is the allocation, and what does not narrow is the shape: one
metric costs a caller of ML.NET the same 240 ms as all six.

So the answer to "coverage or speed" is coverage, and the `AccuracyAlone` row is where coverage
stops being a list and becomes a cost.

## What does not change

No number reaches `docs/guides/performance.md` — shared container,
[ADR 0051](../../decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md). The class is
in `bench-map.json`, selected by any change under `src/Lodestar.Metrics/`.

## Testing

- `tools/check_bench_map.py` refuses a `[Benchmark]` class the map does not name; the class is
  mapped and the check passes.
- The build is clean at `AnalysisMode=All` with warnings as errors.
- The class runs to completion under `--job short`.
- The agreement check is a precondition of the lot, and the correction it forced is recorded above
  rather than quietly applied.
