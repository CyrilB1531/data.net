# R2.VarianceWeighted

One score, each output counted in proportion to how much its own truth varies.

<!-- docs-declaration -->

```csharp
public static double VarianceWeighted(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount, ReadOnlySpan<double> sampleWeight = default, bool forceFinite = true, ZeroDivision zeroDivision = ZeroDivision.NaN)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major. `outputCount`
is
how many outputs each row holds and has no default, unlike the other two members. `sampleWeight`
weights the rows, `forceFinite` answers a truth of zero variance, and `zeroDivision` answers fewer
than two samples.

**Returns** — `double` at most `1`.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one;
`UndefinedMetricException` when there are fewer than two samples and `zeroDivision` is
`ZeroDivision.Throw`.

**Example** — the same two outputs, with the busier one counting for more.

```csharp
using DataNet.Metrics;

double[] yTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
double[] yPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

double weighted = R2.VarianceWeighted(yTrue, yPred, outputCount: 2);   // => 0.9382…
```

**Remarks** — scikit-learn's `multioutput="variance_weighted"`. The case it exists for is a model
whose outputs differ wildly in how much they move: an output that is nearly constant is easy to
predict well and would otherwise pull a plain mean upward for no reason, and this weights it down
to
almost nothing.

It is a method rather than a value you could pass as `outputWeights`, because the weights are this
computation's own per-output variances — produced by the same pass as the scores, and not
recoverable
from them —
[decision 0021](../../../decisions/0021-multioutput-is-a-method-not-an-enum.md).

The trap is that this quietly hides a failing output. An output the model is terrible at, whose
truth
happens not to vary much, contributes almost nothing to the number. If you want to know whether
every
output is predicted acceptably, read `R2.PerOutput`; this answers a different question, which is
how
much of the total variance in the data the model accounted for.

**Applies to** — net10.0, netstandard2.0.

**See also** — `R2.Score`, `R2.PerOutput`, `ExplainedVariance.VarianceWeighted`,
[decision 0021](../../../decisions/0021-multioutput-is-a-method-not-an-enum.md),
the [Python equivalence table](../../../equivalence.md).
