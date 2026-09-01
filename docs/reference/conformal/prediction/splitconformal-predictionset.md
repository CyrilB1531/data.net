# SplitConformal.PredictionSet

The prediction set: every class whose probability clears `1 − q`. Sometimes that is none of them.

<!-- docs-declaration -->

```csharp
public static bool[] PredictionSet(ReadOnlySpan<double> probabilities, double quantile)
```

**Parameters** — `probabilities` is one sample's predicted probabilities, in the same class order
[`LeastAmbiguousScores`](splitconformal-leastambiguousscores.md) was given. `quantile` is the
calibrated quantile from [`Quantile`](splitconformal-quantile.md).

**Returns** — a fresh `bool[]` of the same length, `true` where that class is in the set.

**Exceptions** — `ArgumentOutOfRangeException` when `quantile` is negative or `NaN`.

**Example** — a calibrated quantile of `0.5`, so the threshold is `0.5`. One confident row keeps
one class; one undecided row keeps none.

```csharp
using Lodestar.Conformal;

bool[] confident = SplitConformal.PredictionSet([0.75, 0.15, 0.10], 0.5);
bool firstIn = confident[0];    // => True
bool secondIn = confident[1];   // => False

bool[] undecided = SplitConformal.PredictionSet([0.40, 0.35, 0.25], 0.5);
bool anyIn = Array.Exists(undecided, included => included);   // => False
```

**Remarks** — **the empty set is a real answer, and it is not repaired here.** When no class clears
the threshold, LAC says so, and that is information: the model is less sure about this sample than
it was about `1 − alpha` of the calibration set. Substituting the most likely class would return
something with no coverage guarantee under a name that promises one — the same mistake as clamping
the quantile, which
[decision 0070](../../../decisions/0070-k-greater-than-n-returns-an-infinite-interval.md) refuses
for the same reason. If your call site must produce a class, take the arg-max yourself, knowingly.

A set with two or more classes is the other half of the same signal, and it is the usual reason to
reach for conformal classification at all: the model is telling you which alternatives it could not
rule out at this level. An infinite `quantile` returns every class, which is the trivial prediction
the calibration size forced.

Coverage is a statement about the *calibration set as a whole*, not about this row: `1 − alpha` of
exchangeable samples have their true class in the set. Nothing says which ones.

**The guarantee assumes exchangeability** — see the guide's
[*Exchangeability*](../../../guides/conformal.md#exchangeability) section.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SplitConformal.LeastAmbiguousScores`](splitconformal-leastambiguousscores.md),
[`SplitConformal.Quantile`](splitconformal-quantile.md), the
[Python equivalence table](../../../equivalence.md).
