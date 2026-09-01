# SplitConformal.Interval

The prediction interval around a point prediction: `[ŷ − q, ŷ + q]`.

<!-- docs-declaration -->

```csharp
public static (double Lower, double Upper) Interval(double prediction, double quantile)
```

**Parameters** — `prediction` is the model's point prediction for one new sample. `quantile` is the
calibrated quantile from [`Quantile`](splitconformal-quantile.md).

**Returns** — a `(double Lower, double Upper)` tuple. `Lower` is `prediction − quantile` and `Upper`
is `prediction + quantile`, in the target's own units.

**Exceptions** — `ArgumentOutOfRangeException` when `quantile` is negative or `NaN`.

**Example** — a prediction of 11.0 with a calibrated quantile of 0.4.

```csharp
using Lodestar.Conformal;

(double Lower, double Upper) interval = SplitConformal.Interval(11.0, 0.4);
double lower = interval.Lower;   // => 10.6
double upper = interval.Upper;   // => 11.4
```

**Remarks** — the arithmetic is the least interesting part of split conformal prediction, and that
is the point: everything that carries the guarantee already happened in `Quantile`. Calling this
with a quantile you computed some other way produces an interval with no guarantee at all and no
way to tell from the output.

An infinite `quantile` yields the whole line, which is the trivial prediction the calibration size
forced — see [`Quantile`](splitconformal-quantile.md) and
[decision 0070](../../../decisions/0070-k-greater-than-n-returns-an-infinite-interval.md). A zero
quantile yields the point back, which happens when every calibration prediction was exact and is
almost always a leaking split rather than a perfect model.

**The guarantee assumes exchangeability** — see the guide's
[*Exchangeability*](../../../guides/conformal.md#exchangeability) section.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SplitConformal.AbsoluteResiduals`](splitconformal-absoluteresiduals.md),
[`SplitConformal.Quantile`](splitconformal-quantile.md), the
[Python equivalence table](../../../equivalence.md).
