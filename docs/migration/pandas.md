# pandas → .NET

**Verdict: use it, accepting more roughness.** The equivalent exists but is less
mature and less ergonomic than pandas; expect some glue.

| pandas need | Recommended .NET |
| --- | --- |
| General `DataFrame`, CSV IO, typed columns | **`Microsoft.Data.Analysis`** |
| Time series, rich indices | **Deedle** (F# origin, excellent at time series) |

```bash
dotnet add package Microsoft.Data.Analysis
```

```csharp
using Microsoft.Data.Analysis;

DataFrame df = DataFrame.LoadCsv("data.csv");
df["price"] = df["price"].Multiply(1.2);
DataFrame expensive = df.Filter(df["price"].ElementwiseGreaterThan(100));
```

## Pitfalls

- **`groupby` / `pivot`.** Less complete and less fluent than pandas; sometimes
  it's simpler to group with LINQ over the columns.
- **Index.** No label index like pandas in `Microsoft.Data.Analysis` (positional
  indexing). Deedle is closer.
- **Missing values.** `null`/NaN handling differs from pandas; check column by
  column.
- **DataNet glue.** A `DataFrame` ↔ sparse-matrix bridge (`DataNet.Text`) is
  planned to connect vectorization with tabular data.

_Guide to be expanded as real needs arise._
