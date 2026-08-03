# scikit-learn → .NET

**Verdict: use** ML.NET (or SharpLearning for a sklearn-like API), **except text
vectorization**, which is the gap filled natively by `DataNet.Text` (exact
`CountVectorizer`/`TfidfVectorizer` semantics).

| sklearn need | Recommended .NET |
|---|---|
| Pipelines, training, deployment | **ML.NET** (`Microsoft.ML`) |
| sklearn-like API (trees, ensembles, metrics) | **SharpLearning** |
| `CountVectorizer` / `TfidfVectorizer` **to the character** | **`DataNet.Text`** |

```bash
dotnet add package Microsoft.ML
```

```csharp
using Microsoft.ML;

var ml = new MLContext(seed: 0);
IDataView data = ml.Data.LoadFromTextFile<Row>("data.csv", hasHeader: true, separatorChar: ',');
var pipeline = ml.Transforms.Concatenate("Features", "f1", "f2")
    .Append(ml.Regression.Trainers.Sdca(labelColumnName: "Label"));
var model = pipeline.Fit(data);
```

## Pitfalls

- **`TfidfVectorizer` is non-standard.** The sklearn formula (`smooth_idf`,
  per-row L2 normalization) must be reproduced to the character — ML.NET's
  `FeaturizeText` does not reproduce it. That is exactly the reason for
  `DataNet.Text`. See [`../equivalence.md`](../equivalence.md).
- **`min_df` / `max_df`, n-gram bounds**: on the DataNet side, not ML.NET.
- **Metrics.** Check the definitions (macro/micro averaging, handling of absent
  classes) before comparing to sklearn.

_Guide to be expanded as real needs arise._
