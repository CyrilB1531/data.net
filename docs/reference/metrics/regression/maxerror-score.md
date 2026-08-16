# MaxError.Score

The largest absolute difference between a true value and its prediction.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, one per sample and the
same
length. There is nothing else: no weights and no multioutput.

**Returns** — `double`, never negative, `0` only when every prediction is exact. In the target's
own
units.

**Exceptions** — `ArgumentException` when the spans disagree in length, are empty, or hold a
non-finite value.

**Example** — three predictions are perfect and one is out by 96.

```csharp
using Lodestar.Metrics;

double[] yTrue = [1.0, 2.0, 3.0, 100.0];
double[] yPred = [1.0, 2.0, 3.0, 4.0];

double worst = MaxError.Score(yTrue, yPred);   // => 96
```

**Remarks** — this is the metric for a guarantee rather than for a summary: "no dose is ever off
by
more than this", "no invoice is ever wrong by more than this". If your requirement is a bound,
this
is the only number on the page that measures it, because every other one can be excellent while a
single catastrophic prediction hides inside it — on the data above, `MedianAbsoluteError.Score` is
`0`.

The trap is the mirror image, and it is why nobody optimises this: it is decided by **one
sample**,
so it is as noisy as your worst label. One mistyped value in a dataset moves this number and no
other. Report it beside an average, never instead of one.

The missing parameters are fidelity, not an oversight. `max_error` accepts no `sample_weight` and
refuses a two-dimensional target outright, and the reason is real: a worst case is not an average,
so there is nothing for a weight to scale.

**Applies to** — net10.0, netstandard2.0.

**See also** — `MedianAbsoluteError.Score`, `MeanAbsoluteError.Score`, `MeanSquaredError.Score`,
the [Python equivalence table](../../../equivalence.md).
