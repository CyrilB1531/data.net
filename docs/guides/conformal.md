# Prediction with a guarantee — `Lodestar.Conformal`

Your model predicts `11.4`. How wrong is that? Most answers to this question are either a
distributional assumption you cannot check, or a validation RMSE that describes the average case
and says nothing about this one.

**Split conformal prediction** answers it differently. Hold out a calibration set the model did not
train on, measure how wrong the model was on each of those points, and read off one number. Apply
that number to a new prediction and you get an interval that contains the truth at least 90 % of
the time — or 80 %, or whatever you asked for. The guarantee is finite-sample: it holds for a
calibration set of thirty points, not only in the limit. It does not assume the errors are normal,
it does not assume the model is any good, and it does not need to know what the model is. A bad
model gets wide intervals, which is the correct answer.

There is one price, and the whole of this guide's second half is about it.

## Regression: a point becomes an interval

Three calls. Score the calibration set, turn the scores into one number, apply it.

```csharp
using Lodestar.Conformal;

// Nine held-out points: what actually happened, and what the model had said.
double[] observed = [12.0, 9.5, 15.0, 11.25, 8.75, 14.0, 10.5, 14.25, 10.0];
double[] predicted = [11.75, 10.0, 14.25, 12.0, 8.25, 13.25, 11.0, 13.5, 10.75];

double[] scores = SplitConformal.AbsoluteResiduals(observed, predicted);
double q = SplitConformal.Quantile(scores, alpha: 0.2);        // 0.75

(double Lower, double Upper) band = SplitConformal.Interval(11.0, q);
// band is (10.25, 11.75): the model said 11.0, and it is within 0.75 at least 80 % of the time.
```

`alpha` is the **mis**coverage: `0.2` asks for 80 % coverage, `0.1` for 90 %. The quantile is
[`SplitConformal.Quantile`](../reference/conformal/prediction/splitconformal-quantile.md)'s
return value rather than something held inside an object, which is deliberate — it is the number
that carries the guarantee, so you can print it, log it, watch it drift, and compare it against the
one you had last month.
[`SplitConformal.Interval`](../reference/conformal/prediction/splitconformal-interval.md) then does
the only arithmetic left, and doing it with a quantile obtained some other way produces a band with
no guarantee and no way to tell from the output.

The interval has the same width for every prediction, because
[`SplitConformal.AbsoluteResiduals`](../reference/conformal/prediction/splitconformal-absoluteresiduals.md)
scores every calibration point the same way. That is a real limitation, not a simplification: a
model whose error grows with the target gets intervals too wide where it is confident and too
narrow where it is not, while still covering at the rate asked for **overall**. The fix is a
normalised conformity score, which divides the residual by a second model's estimate of the local
spread; this package does not ship one yet.

## Classification: a class becomes a set

The same three calls, with a different score.
[`SplitConformal.LeastAmbiguousScores`](../reference/conformal/prediction/splitconformal-leastambiguousscores.md)
implements LAC — *least ambiguous set-valued classifier* — which scores a calibration sample by how
much probability the model withheld from the class that turned out to be right.

```csharp
using Lodestar.Conformal;

// Four calibration samples over three classes, row-major, with their true classes.
double[] calibration =
[
    0.75, 0.15, 0.10,
    0.10, 0.50, 0.40,
    0.25, 0.25, 0.50,
    0.50, 0.25, 0.25,
];
int[] labels = [0, 1, 2, 0];

double[] scores = SplitConformal.LeastAmbiguousScores(calibration, labels, classCount: 3);
double q = SplitConformal.Quantile(scores, alpha: 0.25);       // 0.5

bool[] clear = SplitConformal.PredictionSet([0.75, 0.15, 0.10], q);      // { 0 }
bool[] undecided = SplitConformal.PredictionSet([0.40, 0.35, 0.25], q);  // { } — empty
```

Two things in that last pair are the point of the whole exercise.

A set with **more than one** class is the model telling you which alternatives it could not rule
out at this level. That is usually why people reach for conformal classification: not to get a
better single answer, but to get an honest short list.

A set with **no** classes at all is
[`SplitConformal.PredictionSet`](../reference/conformal/prediction/splitconformal-predictionset.md)
saying that this sample is less like the calibration set than `1 − alpha` of it was. It is
information, and this package does not repair it. Substituting the most likely class there would
hand back something with no coverage guarantee under a name that promises one. If your call site
must produce a class, take the arg-max yourself — knowingly, and outside the guarantee.

Coverage is a statement about the calibration set as a whole, never about one row: `1 − alpha` of
exchangeable samples have their true class in the set. Nothing says which ones.

## Exchangeability

Everything above rests on one assumption, and it is not the one people expect.

The guarantee holds when the calibration data and the data you predict on are **exchangeable** —
loosely, when their joint distribution does not care about the order they arrived in. It does not
require independence, it does not require normality, and it does not require a good model. It does
require that a test point is, statistically, just another calibration point.

When that fails, the intervals still come out. They are the same shape, the same width, and the
same type. They simply do not cover at the rate they claim, and **nothing in the output says so**.
That is what makes this worth a section rather than a footnote: every other failure in this library
is loud, and this one is silent.

Three ways it fails in practice, and what to do instead.

**Calibrating on data the model trained on.** The residuals come out small because the model has
seen those points, `q` comes out small, and every interval is too narrow. This is the common one,
and it is easy to do by accident — a pipeline that refits on the full dataset after selecting
hyperparameters has already leaked. *Instead:* split before anything is fitted, and keep the
calibration split out of every fit, including the feature scaler's.

**Splitting a time-ordered dataset at random.** Random rows from the past and future of the same
series are not exchangeable with a future you have not seen; the model is being calibrated partly
on data from after the period it will predict. *Instead:* calibrate on the most recent block before
the prediction window, accept that even that is an approximation, and re-calibrate on a schedule.
There are conformal methods designed for time series — this package does not implement them, and
using this one on a series is a choice to be made explicitly.

**Calibrating before something changed.** A deployment, a new sensor, a new customer segment, a
supplier who reformulated a product. The calibration set now describes a world the test points are
not from. *Instead:* monitor `q` by re-computing it on fresh labelled data and comparing; a
calibrated quantile that has moved is the cheapest drift signal this library can give you, and it
is the reason the number is returned rather than hidden.

## When the calibration set is too small

Ask for 95 % coverage with nine calibration points and the rule asks for the tenth smallest of nine
scores. `Quantile` returns `double.PositiveInfinity`, `Interval` returns the whole line, and
`PredictionSet` returns every class. That is not an error path: it is the only answer at that level
whose coverage is what you asked for.

The rule is `k = ceil((n + 1)(1 − alpha))`, so the smallest calibration set that can answer at level
`alpha` has `n ≥ 1 / alpha − 1` points: 19 for 95 %, 99 for 99 %. Test `double.IsInfinity(q)` if an
infinite interval is unacceptable at your call site, and collect more calibration data. There is no
third answer —
[decision 0070](../decisions/0070-k-greater-than-n-returns-an-infinite-interval.md) records why
this does not clamp to the widest score the way MAPIE does, and why it does not throw.

## What this is not

**Not a calibrated probability.** Conformal prediction says nothing about whether the model's
`0.75` means 75 %. Reach for `BrierScore` and `LogLoss` in `Lodestar.Metrics` for that question,
and for the reliability curve that goes with them.

**Not adaptivity.** Every interval here is the same width, for the reason the regression section
gives.

**Not a replacement for evaluation.** An interval that covers 90 % of the time can still be twenty
units wide because the model is poor. Coverage and sharpness are two different numbers, and this
package computes the first one's guarantee, not the second one's value.

## Parity

Every value in this guide is replayed from a frozen corpus generated by MAPIE 1.5.0 —
`SplitConformalRegressor` and `SplitConformalClassifier` at `conformity_score="lac"`, both
`prefit`. The one deliberate divergence is the `k > n` edge above.
[`docs/equivalence.md`](../equivalence.md) maps each call.
