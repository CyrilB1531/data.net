# Regression metrics — `Lodestar.Metrics`

Your model predicted a number and the truth was a different number. How wrong is that? Every type on
this page answers it, and they disagree about what "wrong" should cost. Squaring the miss makes one
bad prediction dominate; taking the median makes it disappear entirely; dividing by the truth makes
being 1 out on 10 as bad as being 100 out on 1000. None of these is the correct answer — the right
one is the one that matches what a mistake actually costs you — and reporting the wrong one is the
usual reason a model that looks good on a metric behaves badly in use.

Two families sit here, and it is worth knowing which one you are reading.

- An **error** — everything with `Error` or `Loss` in its name — is `0` when the prediction is
  perfect and grows without bound. It is in the units of your target, or their square, so `0.5` means
  nothing until you know what the target was measured in.
- A **score** — `R2` and `ExplainedVariance` — is `1` when the prediction is perfect, `0` for a model
  no better than always predicting the mean, and **negative** for one that is worse than that. It is
  unitless, so it can be compared across problems, which is precisely what an error cannot do.

## How the parameters fit together

Everything here except `MaxError` shares one shape, and reading it once saves reading it eleven
times.

```mermaid
flowchart TD
    A["<b>yTrue</b>, <b>yPred</b> — one flat span each.<br/>With more than one output they are row-major:<br/>sample 0's outputs, then sample 1's."] --> B["<b>outputCount</b> says where one row ends"]
    B --> C["per sample and per output, a residual,<br/>charged the way this metric charges it"]
    C --> D["<b>sampleWeight</b> weights the <i>rows</i><br/>— one weight per sample, not per value"]
    D --> E["one number per output"]
    E --> F["<b>PerOutput</b><br/>returns that array as it is"]
    E --> G["<b>Score</b><br/>reduces it: a plain mean, or a mean<br/>weighted by <b>outputWeights</b>"]
    E --> H["<b>VarianceWeighted</b><br/>reduces it by each output's own variance<br/>— <i>R2 and ExplainedVariance only</i>"]
```

`outputCount` defaults to `1`, which is the ordinary case: one target per sample, one number out.
There is no two-dimensional overload because a `ReadOnlySpan<T>` cannot carry one, and `PerOutput`
is a method rather than an enum member because it changes the return type —
[decision 0021](../../decisions/0021-multioutput-is-a-method-not-an-enum.md).

Two refusals every metric here shares, both reproducing the message their Python layer prints. A
`sampleWeight` that is zero **throughout** is refused — the rule is every weight, not the sum, so
`[-1, -2, -3]` still scores — and `outputWeights` whose **sum** is zero are refused, so `[1, -1]` is
refused and `[-1, -1]` scores. Both arrive as `ArgumentException`. Non-finite values in `yTrue` or
`yPred` are refused too.

Classification metrics — how often a label was right — are on the
[classification page](classification.md), not here. `ZeroDivision`, which `R2` takes, is documented
there.

## Which one do I report?

```mermaid
flowchart TD
    A["What are you reporting?"] --> B{"An error in the target's units,<br/>or a unitless score?"}
    B -->|a score, to compare across problems| C{"Should a constant bias<br/>count against the model?"}
    C -->|yes, it is a real error| D["R2"]
    C -->|no, only the spread matters| E["ExplainedVariance"]
    B -->|an error| F{"How should one very<br/>bad prediction count?"}
    F -->|more than its share| G{"In the target's units?"}
    G -->|yes| H["RootMeanSquaredError"]
    G -->|no, squared is fine| I["MeanSquaredError"]
    F -->|exactly its share| J["MeanAbsoluteError"]
    F -->|not at all| K["MedianAbsoluteError"]
    F -->|it is the only thing that matters| L["MaxError"]
    A --> M{"Is the target a count or a<br/>quantity spanning orders of magnitude?"}
    M -->|yes, and relative error is what hurts| N["MeanAbsolutePercentageError"]
    M -->|yes, and under-prediction hurts more| O["MeanSquaredLogError,<br/>RootMeanSquaredLogError"]
    A --> P{"Are you predicting a quantile<br/>rather than a mean?"}
    P -->|yes| Q["PinballLoss"]
```

| Type | What it measures |
| --- | --- |
| [`ExplainedVariance`](regression/explainedvariance.md) | The share of the truth's spread the prediction accounts for, ignoring a constant bias. |
| [`MaxError`](regression/maxerror.md) | The single worst prediction, and nothing else. |
| [`MeanAbsoluteError`](regression/meanabsoluteerror.md) | The average miss, in the target's own units. |
| [`MeanAbsolutePercentageError`](regression/meanabsolutepercentageerror.md) | The average miss as a fraction of the truth. |
| [`MeanSquaredError`](regression/meansquarederror.md) | The average squared miss — the one big errors dominate. |
| [`MeanSquaredLogError`](regression/meansquaredlogerror.md) | The same, on `log(1 + y)`, so a ratio matters more than a difference. |
| [`MedianAbsoluteError`](regression/medianabsoluteerror.md) | The typical miss, immune to any number of outliers. |
| [`PinballLoss`](regression/pinballloss.md) | The loss for a quantile prediction, charging over- and under-shooting differently. |
| [`R2`](regression/r2.md) | How much better than always predicting the mean, as a unitless score. |
| [`RootMeanSquaredError`](regression/rootmeansquarederror.md) | `MeanSquaredError` back in the target's units. |
| [`RootMeanSquaredLogError`](regression/rootmeansquaredlogerror.md) | `MeanSquaredLogError` back in log units. |
