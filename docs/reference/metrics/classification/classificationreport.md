# ClassificationReport

The table people actually paste into a pull request: precision, recall, F1 and support, per class,
plus the averages — available both as objects and as scikit-learn's own text, character for
character.

## Members

| Member | What it does |
| --- | --- |
| [`ClassificationReport.Compute`](classificationreport-compute.md) | Builds the report: one row per class, the accuracy, and two or three averaged rows. |
| [`ClassificationReport.ToText`](classificationreport-totext.md) | Renders the table the way `sklearn.metrics.classification_report` prints it, to the character. |
| [`ClassificationReport.ToString`](classificationreport-tostring.md) | The two-digit table, so that printing a report does something useful. |
