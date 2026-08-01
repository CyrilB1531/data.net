# pandas → .NET

**Verdict : utiliser, en acceptant plus de rugosité.** L'équivalent existe mais
il est moins mûr et moins ergonomique que pandas ; prévoir de la colle.

| Besoin pandas | .NET recommandé |
|---|---|
| `DataFrame` général, IO CSV, colonnes typées | **`Microsoft.Data.Analysis`** |
| Séries temporelles, indices riches | **Deedle** (origine F#, excellent en time series) |

```bash
dotnet add package Microsoft.Data.Analysis
```

```csharp
using Microsoft.Data.Analysis;

DataFrame df = DataFrame.LoadCsv("data.csv");
df["price"] = df["price"].Multiply(1.2);
DataFrame expensive = df.Filter(df["price"].ElementwiseGreaterThan(100));
```

## Pièges

- **`groupby` / `pivot`.** Moins complet et moins fluide que pandas ; parfois
  plus simple de faire le regroupement en LINQ sur les colonnes.
- **Index.** Pas d'index d'étiquettes façon pandas dans `Microsoft.Data.Analysis`
  (indexation positionnelle). Deedle s'en rapproche davantage.
- **Valeurs manquantes.** Gestion des `null`/NaN différente de pandas ; vérifier
  colonne par colonne.
- **Colle DataNet.** Une passerelle `DataFrame` ↔ matrice creuse
  (`DataNet.Text`) est prévue quand le lot 2 (vectorisation) atterrira.

_Guide à étoffer au fil des besoins réels._
