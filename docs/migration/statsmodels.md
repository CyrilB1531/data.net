# statsmodels → .NET

**Verdict: decide.** The foundation (linear regression, distributions, basic
tests) exists; **rich econometrics** (detailed GLMs, ARIMA/SARIMAX, mixed models,
R-style summaries with p-values and confidence intervals) has **no** good .NET
equivalent. It is a candidate for native code *if your usage justifies it*.

| statsmodels need | .NET |
|---|---|
| Linear regression, least squares | **Math.NET Numerics** (`Fit`, `MultipleRegression`) |
| Distributions, basic hypothesis tests | **Math.NET** (`Distributions`), Accord.NET |
| Advanced GLMs, time series, econometric summaries | ⚠️ **gap** — write or work around |

```csharp
using MathNet.Numerics;

// OLS y = a + b·x
(double a, double b) = Fit.Line(xs, ys);
double r2 = GoodnessOfFit.RSquared(xs.Select(x => a + b * x), ys);
```

## Pitfalls

- **No rich `summary()`.** Standard errors, confidence intervals, coefficient
  p-values are not provided out of the box: compute them yourself (the estimator
  covariance matrix) or port them.
- **Time series.** Nothing equivalent to `SARIMAX`/`statespace`: either restrict
  the scope, or make it a native lot of its own.

> Before any native development here, weigh the **real need**: a regression plus a
> few tests is often enough, and Math.NET already covers that.

_Guide to be expanded as real needs arise._
