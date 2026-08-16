# MultiClassStrategy

ROC-AUC is defined for two classes. This says how a problem with more gets reduced to problems
with
two.

<!-- docs-declaration -->

```csharp
public enum MultiClassStrategy { OneVsRest, OneVsOne }
```

**Members** — `OneVsRest` scores each class against everything else and averages the results.
`OneVsOne` scores every pair of classes against each other and averages those, the Hand and Till
formulation.

**Example** — the two on the same scores; see `MultiClassRocOptions` for the data.

```csharp
using Lodestar.Metrics;

int[] yTrue = [0, 1, 2, 2, 2, 1];
double[] yScore =
[
    0.6, 0.3, 0.1,
    0.3, 0.5, 0.2,
    0.2, 0.5, 0.3,
    0.1, 0.2, 0.7,
    0.4, 0.4, 0.2,
    0.2, 0.3, 0.5,
];

MultiClassRocOptions oneVsRest = new() { Strategy = MultiClassStrategy.OneVsRest };
MultiClassRocOptions oneVsOne = new() { Strategy = MultiClassStrategy.OneVsOne };

double rest = RocAuc.MultiClass(yTrue, yScore, 3, oneVsRest);   // => 0.7824…
double pairs = RocAuc.MultiClass(yTrue, yScore, 3, oneVsOne);   // => 0.8194…
```

**Remarks** — `OneVsRest` is the default and the cheaper of the two: it runs one binary problem
per
class, so `k` of them, and it is the one whose per-class numbers you can also look at
individually.
`OneVsOne` runs `k(k-1)/2` binary problems, and its selling point is that each pair is judged
without the other classes' samples in the way, which makes it insensitive to how common the
classes
are.

The trap is comparing the two numbers, as above: `0.7824…` and `0.8194…` are the same model on the
same scores, and neither is more correct. Pick one, and say which one the number is.

`SampleWeight` is refused with `OneVsOne` — scikit-learn refuses it too, because a pairwise
average
has no agreed weighting.

**Applies to** — net10.0, netstandard2.0.

**See also** — `RocAuc.MultiClass`, `MultiClassRocOptions`,
the [Python equivalence table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
