# Chi2ContingencyResult

A contingency-table chi-square result.

<!-- docs-declaration -->

```csharp
public sealed record Chi2ContingencyResult(double Statistic, double PValue, int Dof, double[][] ExpectedFrequencies)
```

**Properties** — `Statistic` is the chi-square statistic. `PValue` is the upper-tail p-value.
`Dof` is the degrees of freedom, `(rows - 1) * (columns - 1)`. `ExpectedFrequencies` is the table
expected under independence, row-major, the same shape as the input table.

**Example** — the table [`ChiSquare.Contingency`](chisquare-contingency.md) tests for
independence.

```csharp
using Lodestar.Stats;

double[][] table =
[
    [30.0, 20.0],
    [15.0, 35.0],
];

Chi2ContingencyResult result = ChiSquare.Contingency(table);

int dof = result.Dof;                                  // => 1
double expected01 = result.ExpectedFrequencies[0][1];   // => 27.5
```

**Remarks** — `ExpectedFrequencies` is what the table would look like if the two factors were
independent, each cell `rowTotal * columnTotal / grandTotal`; comparing it against `table`
cell by cell is what the statistic itself does. It shares `table`'s exact shape rather than a
flattened form, so `result.ExpectedFrequencies[i][j]` lines up directly with `table[i][j]`.

Being a `record`, and `double[][]` comparing by reference rather than by value, two
`Chi2ContingencyResult`s are equal only when they share the very same expected-frequencies array
— a detail that matters for a unit test comparing two results, and not otherwise.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`ChiSquare.Contingency`](chisquare-contingency.md), [`TestResult`](testresult.md),
the [Python equivalence table](../../../equivalence.md).
