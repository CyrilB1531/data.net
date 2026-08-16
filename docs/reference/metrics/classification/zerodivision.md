# ZeroDivision

What a metric returns when its denominator is zero.

<!-- docs-declaration -->

```csharp
public enum ZeroDivision { Zero, One, NaN, Throw }
```

**Members** — `Zero` returns `0.0`, which is scikit-learn's default value. `One` returns `1.0`,
its
`zero_division=1`. `NaN` returns `double.NaN`, its `zero_division=np.nan`. `Throw` raises
`UndefinedMetricException` and has no scikit-learn equivalent.

**Example** — one sample of class 1, and a model that never predicts it.

```csharp
using Lodestar.Metrics;

int[] yTrue = [0, 0, 1];
int[] yPred = [0, 0, 0];

double asZero = Precision.Score(yTrue, yPred);                                  // => 0
double asOne = Precision.Score(yTrue, yPred, zeroDivision: ZeroDivision.One);   // => 1
```

**Remarks** — the choice is about what an unanswerable question should look like downstream, and
there is no universally right answer, which is why it is a parameter. `Zero` is the safe default
and
the one that keeps parity, at the cost of reading in a report as a real, terrible score. `One` is
the optimistic reading — "we were never wrong about a class we never predicted" — and is what
scikit-learn's `zero_division=1` exists for. `NaN` is the honest one when the number is about to
be
averaged: a `NaN` propagates and is visible, where a `0.0` quietly pulls a macro average down by
`1/k`.

The default is not the same everywhere, and that is worth checking rather than assuming. The
precision family defaults to `Zero`; `CohenKappa.Score` and the regression side's `R2` default to
`NaN`, because that is the value scikit-learn returns for *their* undefined cases. Each entry
states
its own.

The trap is `One` in an average. It does not merely hide the problem, it inverts it: a class
nothing
was predicted into contributes the best possible score to a macro average, so adding classes your
model ignores raises the number.

**Applies to** — net10.0, netstandard2.0.

**See also** — `UndefinedMetricException`, `Precision.Score`, `Recall.Score`,
[the regression page](regression.md), the [Python equivalence table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
