# seaborn → .NET

**Verdict: decide.** The *capability* to plot exists (ScottPlot / Plotly.NET), but
seaborn's **statistical abstractions** (tidy API, `histplot`/`kdeplot`, `regplot`,
`heatmap`, `pairplot`, palettes) have no out-of-the-box equivalent: each chart is
rebuilt. A thin layer of presets could be written if the need is confirmed.

| seaborn need | .NET |
| --- | --- |
| Histogram / density | ScottPlot (`Add.Histogram`) + KDE computed by hand |
| Scatter + regression (`regplot`) | ScottPlot: scatter + fitted line (Math.NET `Fit.Line`) |
| Correlation heatmap | ScottPlot (`Add.Heatmap`) over a computed matrix |
| `pairplot` | ⚠️ gap — assemble the subplot grid yourself |

```csharp
using ScottPlot;

var plot = new Plot();
plot.Add.Histogram(values, binCount: 30);   // no automatic KDE
plot.SavePng("dist.png", 700, 500);
```

## Pitfalls

- **No tidy API.** seaborn consumes a "long" `DataFrame` and infers
  colors/facets; on the .NET side you prepare the series explicitly.
- **Missing statistical presets** (`kdeplot`, confidence bands, `pairplot`): that
  is the real delta. A candidate for a small helper library on top of ScottPlot,
  *if real usage demands it* — not before.

*Guide to be expanded as real needs arise.*
