# AverageRow

One averaged line of a report: `macro avg`, `weighted avg` or `micro avg`, with the same four
columns a class row has.

<!-- docs-declaration -->

```csharp
public sealed record AverageRow(string Name, double Precision, double Recall, double F1, double Support)
```

**Properties** — `Name` is the label scikit-learn prints for the row. `Precision`, `Recall` and
`F1`
are the averaged scores, reduced the way the row's name says. `Support` is the total weight the
average covers, which is the same for all three rows of one report.

**Example** — the macro row of the three-class report below.

```csharp
using DataNet.Metrics;

int[] yTrue = [0, 0, 1, 1, 2, 2, 2];
int[] yPred = [0, 1, 1, 1, 2, 2, 0];

ClassificationReport report = ClassificationReport.Compute(yTrue, yPred);
AverageRow macro = report.MacroAverage;
double f1 = macro.F1;         // => 0.7000…
double support = macro.Support;   // => 7
```

**Remarks** — a record, so it is compared by value and prints its own contents, which makes it
useful in a test assertion without any ceremony. The three rows a report can hold are
`MacroAverage`, `WeightedAverage` and — only when an explicit label subset dropped samples —
`MicroAverage`.

The trap is `Support` on this row versus on a `ClassRow`. A class row's support is that class's
own
weight; every average row carries the **total**, so summing the supports of a report's rows double
counts. Read `TotalSupport` off the report if that is what you want.

**Applies to** — net10.0, netstandard2.0.

**See also** — `ClassificationReport.Compute`, the [Python equivalence
table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
