# ShapiroWilk.Test

Tests whether a sample could have come from a normal distribution.

<!-- docs-declaration -->

```csharp
public static TestResult Test(ReadOnlySpan<double> sample)
```

**Parameters** — `sample` is the data, between 3 and 5000 values, not all equal; the span is
read, never modified.

**Returns** — `TestResult`: Royston's W statistic, and its p-value.

**Exceptions** — `ArgumentException` when `sample` holds fewer than 3 or more than 5000 values,
or every value is identical.

**Example** — a symmetric sample against a heavily right-skewed one of the same size.

```csharp
using Lodestar.Stats;

double[] symmetric = [-1.62, -1.10, -0.74, -0.47, -0.23, 0.0, 0.23, 0.47, 0.74, 1.10, 1.62];
double[] skewed = [0.1, 0.2, 0.3, 0.4, 0.6, 0.9, 1.4, 2.2, 3.6, 6.1, 12.0];

TestResult normalLooking = ShapiroWilk.Test(symmetric);
TestResult notNormal = ShapiroWilk.Test(skewed);

double symmetricW = Math.Round(normalLooking.Statistic, 4);   // => 0.9958
double skewedW = Math.Round(notNormal.Statistic, 4);          // => 0.7088
double skewedP = Math.Round(notNormal.PValue, 6);             // => 0.000608
```

**Remarks — `W` is close to `1` for a shape close to normal, and the p-value follows from how far
below `1` it falls.** `symmetric`'s `W` of `0.9958` carries a p-value over `0.999` — nothing here
argues against normality; `skewed`'s `0.7088` is a p-value of `0.0006`, decisively against it.

**The 3-to-5000 bound is a property of the fitted transform, not an arbitrary guard.** Royston's
polynomials that turn `W` into a p-value are fitted over that range; outside it there is no
p-value the fit actually covers, so this refuses instead of extrapolating a number that would
look precise and mean nothing:

```csharp
using Lodestar.Stats;

string message = "nothing was thrown";
try
{
    ShapiroWilk.Test([1.0, 2.0]);
}
catch (ArgumentException error)
{
    message = error.Message;
}

string what = message;   // => Royston's approximation covers 3 to 5000 values…
```

scipy warns on the same input and answers anyway; this package treats the fitted range as a hard
boundary instead.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`KolmogorovSmirnov.TwoSample`](kolmogorovsmirnov-twosample.md), the
[Python equivalence table](../../../equivalence.md).
