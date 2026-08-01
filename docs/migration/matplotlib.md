# matplotlib → .NET

**Verdict : utiliser l'existant.** Le tracé est bien couvert en .NET.

| Besoin matplotlib | .NET recommandé |
|---|---|
| Graphes statiques (PNG/SVG), scientifique | **ScottPlot** — mûr, rapide, API simple |
| Graphes interactifs (HTML/notebook) | **Plotly.NET** |
| Intégration WPF/Avalonia | OxyPlot, ScottPlot.WPF/.Avalonia |

```bash
dotnet add package ScottPlot
```

```csharp
using ScottPlot;

var plot = new Plot();
plot.Add.Scatter(xs, ys);
plot.Title("Ventes");
plot.SavePng("ventes.png", 800, 600);
```

## Pièges

- **Pas d'état global façon `plt.`** : ScottPlot est objet (`new Plot()`), ce qui
  est plus sain mais dépayse au début.
- **Styles / thèmes** diffèrent de matplotlib ; les palettes par défaut ne
  coïncident pas.
- **Notebooks.** Pour du .NET interactif (Polyglot Notebooks), Plotly.NET rend
  directement en HTML — plus proche de l'expérience Jupyter.

_Guide à étoffer au fil des besoins réels._
