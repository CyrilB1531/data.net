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
- **Lodestar glue.** There is no `DataFrame` ↔ sparse-matrix bridge and none is
  planned; the join is a LINQ expression, shown below.

## Feeding a DataFrame column to a vectorizer

The vectorizers take `IEnumerable<string>`, so a column reaches them without anything
in between. Both of these work — the typed column already enumerates as strings, and
the untyped indexer needs a cast:

```csharp
using Microsoft.Data.Analysis;
using Lodestar.Text.Vectorization;

DataFrame df = DataFrame.LoadCsv("reviews.csv");

// The column is typed: enumerate it directly.
IEnumerable<string> documents =
    ((StringDataFrameColumn)df["review"]).Select(v => v ?? string.Empty);

// Or from the untyped indexer, which enumerates as object.
IEnumerable<string> viaCast =
    df["review"].Cast<string?>().Select(v => v ?? string.Empty);

CsrMatrix counts = new TfidfVectorizer().FitTransform(documents);
```

**Decide what an empty cell means before you write `?? string.Empty`.** pandas keeps
`NaN` distinct from `""`; a vectorizer cannot, and a missing review scored as an empty
document is a row of zeros that looks like a legitimate answer. Filter the nulls out if
that is what you mean.

Going back is reading, not converting: [`CsrMatrix`](../reference/text/vectorizers/csrmatrix.md)
exposes `Values`, `ColumnIndices` and `RowPointers`, and `ToDense()` if the shape is
small enough to want a rectangle. **Materialising a document-term matrix into a
`DataFrame` is usually the wrong move** — a vocabulary of thirty thousand terms is
thirty thousand columns, almost all zero, which is exactly the density the sparse
format exists to avoid.

### Why there is no bridge package

The doctrine in `CLAUDE.md` is native code only where .NET has a real gap, and this is
not one: both sides already exist and the join above is one expression. A bridge would
also cost `Lodestar.Text` its no-third-party-dependency property, or need a fifth
package to hold a `Select`.

_Guide to be expanded as real needs arise._
