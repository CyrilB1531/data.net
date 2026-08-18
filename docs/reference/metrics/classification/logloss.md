# LogLoss

The cross-entropy of a probabilistic prediction: how surprised the model should be by what actually
happened. `0` is perfect and it is **unbounded above** — one confident mistake dominates the whole
average, which is the property the metric exists for.

Three questions, three metrics. [`Accuracy`](accuracy.md) asks whether the prediction was *right*.
[`RocAuc`](rocauc.md) asks whether the *ranking* was right. This asks whether the *confidence* was
honest, and it is the one to read before choosing a threshold.

## The clip is the specification

A predicted `0` for the class that actually occurred would make the logarithm infinite, so the
reference clips every probability into `[eps, 1 - eps]`. **That bound is machine epsilon,
`2.220446049250313e-16`** — measured against scikit-learn 1.9.0 rather than assumed, because it has
changed across versions, and it is what decides the number in exactly the cases a caller cares about.

Three consequences worth knowing:

- A predicted `0` for the true class contributes `-log(eps)`, about `36.04`. Two samples of
  `[0.0, 0.5]` against labels `[1, 0]` score `18.36840028483855`.
- **Anything below the clip scores the same.** `1e-20` and `0.0` are one number; `1e-15` is above it
  and scores `17.6159…` instead.
- A perfect prediction is not `0` but `2.2204460492503136e-16`, because the upper end is clipped too.

## A row that does not sum to one is scored, not fixed

The reference warns and computes with the values as given. C# has no warning channel, so this is
silent — and the number is what carries the behaviour. Measured, halving every row of a four-sample
matrix takes the loss from `0.5017…` to `1.1948…` rather than leaving it alone. If rows may not be
normalised, normalise them.

## Members

| Member | What it does |
| --- | --- |
| [`LogLoss.Score`](logloss-score.md) | The binary case, from one probability per sample. |
| [`LogLoss.MultiClass`](logloss-multiclass.md) | The same over a probability matrix. |
