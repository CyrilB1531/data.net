# Averaging

A per-class metric gives one number per class. This says how those numbers become one.

<!-- docs-declaration -->

```csharp
public enum Averaging { Binary, Micro, Macro, Weighted }
```

**Members** — `Binary` reports the positive class only, and is the default because it is
scikit-learn's; it is valid only when there are two classes. `Micro` pools the true positives,
false positives and false negatives over every class and divides once. `Macro` takes the plain
unweighted mean of the per-class scores. `Weighted` takes the mean weighted by each class's
support.

**Example** — the same predictions read three ways.

```csharp
using DataNet.Metrics;

int[] yTrue = [0, 0, 1, 1, 2, 2, 2];
int[] yPred = [0, 1, 1, 1, 2, 2, 0];

double macro = Precision.Score(yTrue, yPred, Averaging.Macro);         // => 0.7222…
double weighted = Precision.Score(yTrue, yPred, Averaging.Weighted);   // => 0.7619…
double micro = Precision.Score(yTrue, yPred, Averaging.Micro);         // => 0.7142…
```

**Remarks** — the choice between `Macro` and `Weighted` is a choice about what a class is worth.
`Macro` says every class counts once, so a class with three samples moves the score as much as one
with three thousand — which is what you want when the rare classes are the interesting ones, and
misleading when they are noise. `Weighted` says every *sample* counts once, which keeps the score
close to what a user experiences and lets a rare class be ignored entirely.

`Micro` is the odd one. Pooling the counts before dividing makes micro-precision, micro-recall and
micro-F1 all equal to each other and, when every class is included, all equal to accuracy — the
`0.7142…` above is exactly `Accuracy.Score` on the same data. It is worth computing only when an
explicit label subset has left some samples out, which is the case it exists for.

Two traps. `Binary` is the **default**, so a call written for two classes and later fed three
throws
rather than silently averaging; that is deliberate, and the fix is to name the averaging you
meant.
And scikit-learn's `average=None` has no member here: it changes the return type rather than the
value, so it is a separate method — `Precision.PerClass` and its siblings.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Precision.Score`, `Precision.PerClass`, `BalancedAccuracy.Score`,
the [Python equivalence table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
