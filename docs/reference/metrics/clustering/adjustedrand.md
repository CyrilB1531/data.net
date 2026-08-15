# AdjustedRand

The one metric here that is corrected for chance, which is what makes it the safe default when you do not know how many clusters to expect. A clustering that carries no information scores `0` however many clusters it invents, and a clustering worse than chance scores below it.

## Members

| Member | What it does |
| --- | --- |
| [`AdjustedRand.Score`](adjustedrand-score.md) | How many pairs of samples the two partitions agree about, minus what chance would give. |
