# ClassificationReport.ToText

Renders the table the way `sklearn.metrics.classification_report` prints it, to the character.

<!-- docs-declaration -->

```csharp
public string ToText(int digits = 2)
```

**Parameters** — `digits` is how many decimal places the three score columns carry, scikit-learn's
`digits`. Two by default, which is what it prints unasked.

**Returns** — `string`: a header line, a blank line, one line per class, a blank line, the
accuracy
or micro-average row, the two averaged rows, and a trailing newline.

**Exceptions** — `ArgumentOutOfRangeException` when `digits` is negative.

**Example** — the macro-average line of the report above.

```csharp
using System;
using System.Linq;
using Lodestar.Metrics;

int[] yTrue = [0, 0, 1, 1, 2, 2, 2];
int[] yPred = [0, 0, 1, 1, 2, 2, 0];

string table = ClassificationReport.Compute(yTrue, yPred).ToText();
string header = table.Split('\n')[0].Trim();   // => precision    recall  f1-score   support
```

**Remarks** — the reason this renders text at all, rather than leaving formatting to the caller,
is
that a migration is usually checked by putting the two outputs side by side. Column widths, the
blank lines, the right-alignment and the integer-versus-float rendering of the support column are
all scikit-learn's, so a diff of the two files is empty rather than noisy.

Two things are not identical, and both are stated rather than hidden. A report built with
`ZeroDivision.NaN` renders .NET's `NaN` where Python writes `nan` — the numbers match, the eight
characters do not. And the support column switches between integer and float formatting on a rule
that keys off whether **any** sample anywhere was predicted correctly, not off whether accuracy is
zero; the two differ when a label subset is in play, and the reasoning is in
[decision 0031](../../../decisions/0031-nosamplecorrect-mirrors-numpys-float64-upcast.md).

The trap is treating this as a data format. It is aligned for a human eye, columns can run
together
when a target name is long, and nothing here parses it back. Read `Classes` and the average rows
if
you want the numbers.

**Applies to** — net10.0, netstandard2.0.

**See also** — `ClassificationReport.Compute`, `ClassificationReport.ToString`,
[decision 0031](../../../decisions/0031-nosamplecorrect-mirrors-numpys-float64-upcast.md),
the [Python equivalence table](../../../equivalence.md).
