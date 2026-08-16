# ClassRow

One class's line of a report: the label, an optional readable name, and the four columns.

<!-- docs-declaration -->

```csharp
public sealed record ClassRow(int Label, string? Name, double Precision, double Recall, double F1, double Support)
```

**Properties** — `Label` is the label value this line scores and `Name` the readable name supplied
through `targetNames`, or null when none was. `Precision`, `Recall` and `F1` are that class's own
scores, and `Support` is the weight of the samples whose true label is this class.

**Example** — reading one class off the report rather than off the text.

```csharp
using Lodestar.Metrics;

int[] yTrue = [0, 0, 1, 1, 2, 2, 2];
int[] yPred = [0, 1, 1, 1, 2, 2, 0];

ClassRow spam = ClassificationReport.Compute(yTrue, yPred, ["urgent", "normal", "spam"]).Classes[2];
string name = spam.Name!;      // => spam
double precision = spam.Precision;   // => 1
double support = spam.Support;       // => 3
```

**Remarks** — this exists so that a report can be asserted on, filtered and sorted without going
through the rendered table. `Classes` is in the matrix's label order, which is the sorted union of
both inputs unless `labels` said otherwise, so `Classes[i].Label` is the label and `i` is not.

The trap is reading `Support` as a sample count. It is a **weight**, and with `sampleWeight` in
play
it is a `double` that need not be a whole number — which is exactly why the property is typed the
way it is rather than as `int`.

**Applies to** — net10.0, netstandard2.0.

**See also** — `ClassificationReport.Compute`, `AverageRow`,
the [Python equivalence table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
