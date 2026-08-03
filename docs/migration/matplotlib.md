# matplotlib → .NET

**Verdict: use what exists.** Plotting is well covered in .NET.

| matplotlib need | Recommended .NET |
|---|---|
| Static charts (PNG/SVG), scientific | **ScottPlot** — mature, fast, simple API |
| Interactive charts (HTML/notebook) | **Plotly.NET** |
| WPF/Avalonia integration | OxyPlot, ScottPlot.WPF/.Avalonia |

```bash
dotnet add package ScottPlot
```

```csharp
using ScottPlot;

var plot = new Plot();
plot.Add.Scatter(xs, ys);
plot.Title("Sales");
plot.SavePng("sales.png", 800, 600);
```

## Pitfalls

- **No global `plt.` state**: ScottPlot is object-based (`new Plot()`), which is
  healthier but disorienting at first.
- **Styles / themes** differ from matplotlib; default palettes don't match.
- **Notebooks.** For interactive .NET (Polyglot Notebooks), Plotly.NET renders
  directly to HTML — closer to the Jupyter experience.

_Guide to be expanded as real needs arise._
