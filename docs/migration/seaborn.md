# seaborn → .NET

**Verdict : à trancher.** La *capacité* de tracer existe (ScottPlot / Plotly.NET),
mais les **abstractions statistiques** de seaborn (API tidy, `histplot`/`kdeplot`,
`regplot`, `heatmap`, `pairplot`, palettes) n'ont pas d'équivalent clé en main :
on reconstruit chaque graphe. Une fine couche de presets pourrait être écrite si
le besoin se confirme.

| Besoin seaborn | .NET |
|---|---|
| Histogramme / densité | ScottPlot (`Add.Histogram`) + calcul KDE à la main |
| Nuage + régression (`regplot`) | ScottPlot : scatter + droite ajustée (Math.NET `Fit.Line`) |
| Heatmap de corrélation | ScottPlot (`Add.Heatmap`) sur une matrice calculée |
| `pairplot` | ⚠️ manque — grille de sous-graphes à assembler soi-même |

```csharp
using ScottPlot;

var plot = new Plot();
plot.Add.Histogram(values, binCount: 30);   // pas de KDE automatique
plot.SavePng("dist.png", 700, 500);
```

## Pièges

- **API tidy absente.** seaborn consomme un `DataFrame` « long » et infère
  couleurs/facettes ; côté .NET on prépare les séries explicitement.
- **Presets statistiques manquants** (`kdeplot`, bandes de confiance,
  `pairplot`) : c'est le vrai delta. Candidat à une petite lib de helpers
  au-dessus de ScottPlot, *si l'usage réel le demande* — pas avant.

_Guide à étoffer au fil des besoins réels._
