# UndefinedMetricException

Thrown when a metric has no value and you asked to be told rather than handed a number.

<!-- docs-declaration -->

```csharp
public sealed class UndefinedMetricException : InvalidOperationException
```

**Constructors** — the parameterless one carries a default message; the others take a message, and
a
message with an inner exception.

**Example** — asking to be told instead of scoring `0`.

```csharp
using DataNet.Metrics;

int[] yTrue = [0, 0, 1];
int[] yPred = [0, 0, 0];

string what = "nothing was thrown";
try
{
    _ = Precision.Score(yTrue, yPred, zeroDivision: ZeroDivision.Throw);
}
catch (UndefinedMetricException error)
{
    what = error.Message;
}

string message = what;   // => Precision is undefined here: no sample contributes…
```

**Remarks** — this is the counterpart of scikit-learn's `UndefinedMetricWarning`, which it does
not
reproduce and deliberately improves on. A warning in Python is easy to miss and easy to filter,
and
the value that comes back with it — `0.0` — is indistinguishable in a report from a genuinely
terrible score. Selecting `ZeroDivision.Throw` turns that silence into a stack trace naming the
metric.

The trap is reaching for it as the default. It is not, and should not be: parity with scikit-learn
requires the value, so `ZeroDivision.Zero` is what every precision-family metric starts from.
Throw
is the setting for a pipeline that would rather fail than publish a number nobody can interpret —
which is a reasonable thing to want in CI and a bad thing to want in a dashboard.

**Applies to** — net10.0, netstandard2.0.

**See also** — `ZeroDivision`, `Precision.Score`, `CohenKappa.Score`,
the [Python equivalence table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
