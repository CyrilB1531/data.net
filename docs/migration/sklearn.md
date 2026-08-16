# scikit-learn → .NET

**Verdict: use** ML.NET (or SharpLearning for a sklearn-like API), **except text
vectorization**, which is the gap filled natively by `Lodestar.Text` (exact
`CountVectorizer`/`TfidfVectorizer` semantics).

| sklearn need | Recommended .NET |
| --- | --- |
| Pipelines, training, deployment | **ML.NET** (`Microsoft.ML`) |
| sklearn-like API (trees, ensembles) | **SharpLearning** |
| `CountVectorizer` / `TfidfVectorizer` **to the character** | **`Lodestar.Text`** |
| `classification_report`, `roc_auc_score`, the averaging modes | **`Lodestar.Metrics`** |

```bash
dotnet add package Microsoft.ML
```

```csharp
using Microsoft.ML;

var ml = new MLContext(seed: 0);
IDataView data = ml.Data.LoadFromTextFile<Row>("data.csv", hasHeader: true, separatorChar: ',');
var pipeline = ml.Transforms.Concatenate("Features", "f1", "f2")
    .Append(ml.Regression.Trainers.Sdca(labelColumnName: "Label"));
var model = pipeline.Fit(data);
```

## Pitfalls

- **`TfidfVectorizer` is non-standard.** The sklearn formula (`smooth_idf`,
  per-row L2 normalization) must be reproduced to the character — ML.NET's
  `FeaturizeText` does not reproduce it. That is exactly the reason for
  `Lodestar.Text`. See [`../equivalence.md`](../equivalence.md).
- **`min_df` / `max_df`, n-gram bounds**: on the Lodestar side, not ML.NET.

## Metrics: the averaging mode is not a formatting choice

This is the pitfall that used to read "check the definitions before comparing to
sklearn", which names the trap without getting anyone out of it.

`precision_score(y_true, y_pred, average=…)` returns a different **number**, not
a different presentation, for each mode. On an imbalanced problem the modes do
not disagree slightly — they disagree by a factor of two, and every one of them
is arithmetically correct.

A worked example, taken from this repository's own oracle corpus
(`binary_imbalanced`: 190 samples of class 0, 10 of class 1, a classifier with
30 % label noise). Its confusion matrix is `[[133, 57], [4, 6]]`, so the model
finds 6 of the 10 positives and calls 57 negatives positive:

| Class | Precision | Recall | F1 | Support |
| --- | ---: | ---: | ---: | ---: |
| 0 | 0.971 | 0.700 | 0.813 | 190 |
| 1 | 0.095 | 0.600 | 0.164 | 10 |

| `average=` | Precision | Recall | F1 | What it means |
| --- | ---: | ---: | ---: | --- |
| `"micro"` | 0.695 | 0.695 | 0.695 | Pool every sample, then score once. On a full label set this **is** accuracy. |
| `"macro"` | 0.533 | 0.650 | **0.489** | Mean of the per-class scores. The 10-sample class weighs exactly as much as the 190-sample one. |
| `"weighted"` | 0.927 | 0.695 | **0.781** | Mean of the per-class scores weighted by support. The majority class dominates. |
| `"binary"` | 0.095 | 0.600 | 0.164 | Not an average: class `posLabel` alone, ignoring the other. sklearn's default. |

Macro F1 says 0.489, weighted F1 says 0.781, for one model on one dataset. Report
either without naming the mode and the reader learns nothing. The two are
answering different questions: macro asks how the model does on a class picked at
random, weighted asks how it does on a *sample* picked at random.

In C#, the mode is an enum rather than a string, so a typo is a compile error
instead of a `ValueError` at the end of a run. One
[`ConfusionMatrix.Compute`](../reference/metrics/classification/confusionmatrix-compute.md)
pass feeds both
[`F1.Score`](../reference/metrics/classification/f1-score.md) and
[`ClassificationReport.Compute`](../reference/metrics/classification/classificationreport-compute.md):

```csharp
using Lodestar.Metrics;

ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred);   // one O(samples) pass
double macro    = F1.Score(cm, Averaging.Macro);              // 0.489
double weighted = F1.Score(cm, Averaging.Weighted);           // 0.781
double[] perClass = F1.PerClass(cm);                          // [0.813, 0.164]

Console.WriteLine(ClassificationReport.Compute(cm).ToText()); // what sklearn prints
```

Two differences from the Python spelling are deliberate. `average=None` becomes
[`F1.PerClass`](../reference/metrics/classification/f1-perclass.md), a method,
because it returns one value per class rather than a scalar — an enum member
cannot change its method's return type. And
`Averaging.Binary` throws on a target with more than two classes instead of
guessing which class was meant. Both are recorded in
[`../decisions/0016`](../decisions/0016-metrics-package-placement.md).

**Absent classes.** A class with no predictions gives 0/0. sklearn returns 0 and
emits an `UndefinedMetricWarning`; a warning is easy to miss in a log and has no
natural .NET equivalent. `Lodestar.Metrics` makes the choice explicit —
`ZeroDivision.Zero` (sklearn's value), `One`, `NaN`, or `Throw`, which raises
`UndefinedMetricException` rather than letting a silent 0 flow into a report.

Every function, with its sklearn call and its deliberate divergences, is in
[`../equivalence.md`](../equivalence.md).

```bash
dotnet add package Lodestar.Metrics
```

*Guide to be expanded as real needs arise.*
