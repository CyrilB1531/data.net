# ExplainedVariance

`R2`'s forgiving cousin: it measures whether the prediction tracks the truth's ups and downs, and
does not charge for being consistently off by a fixed amount.

## Members

| Member | What it does |
| --- | --- |
| [`ExplainedVariance.Score`](explainedvariance-score.md) | One number for the whole prediction: the share of the truth's variance the residuals do not carry. |
| [`ExplainedVariance.PerOutput`](explainedvariance-peroutput.md) | One score per output, unreduced. |
| [`ExplainedVariance.VarianceWeighted`](explainedvariance-varianceweighted.md) | One number, each output counted in proportion to how much its own truth varies. |
