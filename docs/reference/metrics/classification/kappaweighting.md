# KappaWeighting

How far apart two classes count as being, when `CohenKappa` charges a disagreement.

<!-- docs-declaration -->

```csharp
public enum KappaWeighting { None, Linear, Quadratic }
```

**Members** — `None` charges every disagreement the same, whatever the two classes were. `Linear`
charges the distance between the two classes' positions. `Quadratic` charges the square of that
distance, so a distant confusion costs disproportionately more than a near one.

**Example** — the same ratings under all three.

```csharp
using DataNet.Metrics;

int[] rater = [1, 1, 2, 2, 3, 3, 1, 3];
int[] model = [1, 3, 2, 1, 3, 2, 1, 3];

double flat = CohenKappa.Score(rater, model, KappaWeighting.None);           // => 0.4285…
double linear = CohenKappa.Score(rater, model, KappaWeighting.Linear);       // => 0.4666…
double quadratic = CohenKappa.Score(rater, model, KappaWeighting.Quadratic); // => 0.5
```

**Remarks** — `None` is the right choice whenever the classes have no order — cat, dog, horse —
because there is no such thing as being nearly right. The other two are for ordinal scales, and
`Quadratic` is the convention in the places kappa is most used, notably medical grading, because
it
punishes a two-grade error four times as hard as a one-grade error rather than twice.

The trap is that the distance is between **positions in the label order**, not between label
values.
On labels `[1, 2, 10]` the gap from `2` to `10` counts as one position, exactly like the gap from
`1` to `2`; and reordering the label set changes every weighted score while leaving `None` alone.
Pass `labels` in the ordinal order whenever the weighting is not `None`.

**Applies to** — net10.0, netstandard2.0.

**See also** — `CohenKappa.Score`,
[decision
0030](../../../decisions/0030-cohen-kappa-keeps-scikit-learns-expected-matrix-orientation.md),
the [Python equivalence table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
