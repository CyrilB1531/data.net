# NormalizedMutualInformation

Information-theoretic rather than pair-counting, and equal to `VMeasure` by construction: scikit-learn's default normalizer is the arithmetic mean of the two entropies, which is what the harmonic mean of homogeneity and completeness reduces to. The frozen corpus shows the two agreeing on every case.

## Members

| Member | What it does |
| --- | --- |
| [`NormalizedMutualInformation.Score`](normalizedmutualinformation-score.md) | How much knowing one labelling tells you about the other, scaled into `[0, 1]`. |
