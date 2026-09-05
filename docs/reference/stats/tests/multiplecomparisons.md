# MultipleComparisons

Adjusting a family of p-values for the number of tests in it.

Twenty tests at the five-percent level produce one significant result by chance alone. These
three rules answer that, and they answer different questions: Bonferroni controls the chance of
*any* false positive, while the Benjamini rules control the expected *proportion* of false
positives among the results called significant.

Each returns adjusted p-values in the input's own order, so an adjusted value can be compared
against the level the caller already had in mind.

## Members

| Member | What it does |
| --- | --- |
| [`MultipleComparisons.Bonferroni`](multiplecomparisons-bonferroni.md) | Multiplies each p-value by the family size, clamped at one. |
| [`MultipleComparisons.BenjaminiHochberg`](multiplecomparisons-benjaminihochberg.md) | The Benjamini-Hochberg step-up procedure. |
| [`MultipleComparisons.BenjaminiYekutieli`](multiplecomparisons-benjaminiyekutieli.md) | The Benjamini-Yekutieli procedure, valid under any dependence. |
