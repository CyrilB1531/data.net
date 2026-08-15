# ExplainedVariance.VarianceWeighted

One number, each output counted in proportion to how much its own truth varies.

<!-- docs-declaration -->

```csharp
public static double VarianceWeighted(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount, ReadOnlySpan<double> sampleWeight = default, bool forceFinite = true)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major. `outputCount`
is
how many outputs each row holds, and unlike the other two members it has no default — asking for a
variance-weighted average of one output is a mistake worth catching at the call site.
`sampleWeight` weights the rows, and `forceFinite` clamps the zero-variance case.

**Returns** — `double` at most `1`.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one.

**Example** — the same two outputs, the busier one counting for more.

```csharp
using DataNet.Metrics;

double[] yTrue = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
double[] yPred = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

double weighted = ExplainedVariance.VarianceWeighted(yTrue, yPred, outputCount: 2);   // => 0.9830…
```

**Remarks** — a plain mean over outputs treats a target that barely moves as equal in importance
to
one that swings widely, which is rarely what anyone means. This weights each output by the
variance
of its own truth, so the outputs that carry the information carry the score.

It is a method rather than an `outputWeights` value you could pass to `Score`, because the weights
are this computation's own per-output variances: they come out of the same pass that produced the
scores and cannot be recovered from the scores alone —
[decision 0021](../../../decisions/0021-multioutput-is-a-method-not-an-enum.md).

The trap is that it is not comparable with `Score` across datasets. Two models on the same data
can
be ranked by either, but a variance-weighted number and a uniform-average number are different
summaries, and swapping one for the other between two reports invents a change that is not there.

**Applies to** — net10.0, netstandard2.0.

**See also** — `ExplainedVariance.Score`, `ExplainedVariance.PerOutput`, `R2.VarianceWeighted`,
[decision 0021](../../../decisions/0021-multioutput-is-a-method-not-an-enum.md),
the [Python equivalence table](../../../equivalence.md).
