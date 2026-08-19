# LikelihoodRatios.Compute

Both class likelihood ratios — `sklearn.metrics.class_likelihood_ratios`.

<!-- docs-declaration -->

```csharp
public static LikelihoodRatios Compute(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, int posLabel = 1, double undefinedPositive = NaN, double undefinedNegative = NaN, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted labels, of the same length, over at
most two distinct values. `posLabel` is the label counted as positive, `1` by default.
`undefinedPositive` and `undefinedNegative` are what each ratio answers when it has no value; the
defaults reproduce `replace_undefined_by=nan`. `sampleWeight` is one weight per sample, or empty.

**Returns** — a `LikelihoodRatios` carrying `Positive` and `Negative`.

**Exceptions** — `ArgumentException` when the inputs disagree in length, are empty, the weights do
not match, or **more than two distinct labels occur** — with the reference's own sentence,
"class_likelihood_ratios only supports binary classification problems."

**Example** — six samples, half of them positive.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 1, 1, 0, 1, 0];
int[] predicted = [0, 1, 0, 0, 1, 1];

LikelihoodRatios ratios = LikelihoodRatios.Compute(truth, predicted);
double positive = ratios.Positive;  // => 1.9999…
```

A positive prediction doubles the odds; the negative ratio on the same input is `0.5`, so a negative
prediction halves them.

**Remarks** — two parameters where the reference has one. `replace_undefined_by` takes either a
scalar or a mapping of `{"LR+": …, "LR-": …}`, a union C# has no equivalent of; passing the same
value to both reproduces the scalar form and passing different ones reproduces the mapping.

Neither substitution applies when the truth carries **no positive sample**: there is no sensitivity
to build either ratio from, and the reference returns `nan` whatever was asked for.
[The type page](likelihoodratios.md) has the table of all four undefined shapes.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Precision.Score`](precision-score.md), [`Recall.Score`](recall-score.md),
[`ConfusionMatrix.Compute`](confusionmatrix-compute.md), the
[Python equivalence table](../../../equivalence.md).
