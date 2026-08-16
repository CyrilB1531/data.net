# ClassificationReport.ToString

The two-digit table, so that printing a report does something useful.

<!-- docs-declaration -->

```csharp
public string ToString()
```

**Returns** — `string`, exactly what `ToText(2)` returns.

**Example** — the two are the same call.

```csharp
using System;
using Lodestar.Metrics;

int[] yTrue = [0, 0, 1, 1, 2, 2, 2];
int[] yPred = [0, 1, 1, 1, 2, 2, 0];

ClassificationReport report = ClassificationReport.Compute(yTrue, yPred);
bool same = string.Equals(report.ToString(), report.ToText(2), StringComparison.Ordinal);   // => True
```

**Remarks** — an override rather than the default `Lodestar.Metrics.ClassificationReport`, because
a
report in a debugger watch window or an interpolated string is nearly always something a human is
about to read. It carries no information `ToText` does not.

The trap is the one every `ToString` override has: it is not a serialization format and it is not
stable across a `digits` you did not choose. Log `ToText(digits)` if the number of decimal places
matters to whatever reads the log.

**Applies to** — net10.0, netstandard2.0.

**See also** — `ClassificationReport.ToText`, `ClassificationReport.Compute`,
the [Python equivalence table](../../../equivalence.md).
