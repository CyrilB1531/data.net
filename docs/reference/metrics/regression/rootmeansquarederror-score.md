# RootMeanSquaredError.Score

The square root of the mean squared error, taken per output before the outputs are reduced.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ReadOnlySpan<double> outputWeights = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major when there is
more
than one output. `outputCount` is how many outputs each row holds, `sampleWeight` weights the
rows,
and `outputWeights` weights the outputs in the reduction.

**Returns** — `double`, never negative, `0` only for an exact prediction. In the target's own
units.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one.

**Example** — two outputs, showing that the root is taken **per output**: taking it after the
reduction instead gives a different number.

```csharp
using System;
using DataNet.Metrics;

double[] yTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
double[] yPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

double rootThenMean = RootMeanSquaredError.Score(yTrue, yPred, outputCount: 2);          // => 0.8227…
double meanThenRoot = Math.Sqrt(MeanSquaredError.Score(yTrue, yPred, outputCount: 2));   // => 0.8416…
```

**Remarks** — this is the number to report. It is in the same units as the target, so "the model
is
out by about 0.6 metres" is a sentence, and it is still dominated by large errors the way squared
error is — which is usually what you want a headline number to be. Beside
`MeanAbsoluteError.Score`
it also carries information: the gap between the two grows with the spread of the errors, so
`RootMeanSquaredError.Score` much larger than mean absolute error says the errors are uneven
rather
than uniformly middling.

A type of its own rather than a flag on `MeanSquaredError`, because scikit-learn deprecated
`mean_squared_error(squared=False)` in 1.4 and removed it in 1.6 in favour of a second function; a
`squared` parameter here would transcribe an API that no longer exists.

The trap is the order of operations on more than one output, which the example measures: the root
is
taken **per output** and the reduction runs on the roots. That is scikit-learn's order, and it is
not
the same number as the root of the reduced mean squared error whenever the outputs differ. On one
output the two coincide, which is why the difference goes unnoticed until a multioutput target
appears.

**Applies to** — net10.0, netstandard2.0.

**See also** — `RootMeanSquaredError.PerOutput`, `MeanSquaredError.Score`,
`MeanAbsoluteError.Score`,
the [Python equivalence table](../../../equivalence.md).
