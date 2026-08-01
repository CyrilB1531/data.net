# scikit-learn → .NET

**Verdict : utiliser** ML.NET (ou SharpLearning pour une API proche de sklearn),
**sauf la vectorisation de texte**, qui est le trou comblé nativement par
`DataNet.Text` (sémantique `CountVectorizer`/`TfidfVectorizer` exacte).

| Besoin sklearn | .NET recommandé |
|---|---|
| Pipelines, entraînement, déploiement | **ML.NET** (`Microsoft.ML`) |
| API proche sklearn (arbres, ensembles, métriques) | **SharpLearning** |
| `CountVectorizer` / `TfidfVectorizer` **au caractère près** | **`DataNet.Text`** (lot 2) |

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

## Pièges

- **`TfidfVectorizer` n'est pas standard.** La formule sklearn
  (`smooth_idf`, normalisation L2 par ligne) doit être reproduite au caractère
  près — `FeaturizeText` de ML.NET ne la reproduit pas. C'est précisément la
  raison d'être de `DataNet.Text`. Voir [`../equivalence.md`](../equivalence.md).
- **`min_df` / `max_df`, bornes de n-grammes** : côté DataNet, pas ML.NET.
- **Métriques.** Vérifier les définitions (moyennage macro/micro, gestion des
  classes absentes) avant de comparer à sklearn.

_Guide à étoffer au fil des besoins réels._
