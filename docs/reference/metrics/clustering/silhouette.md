# Silhouette

The only metric in this namespace that needs the samples themselves rather than a reference
partition — which is what makes it the one you can use when no truth exists, to choose how many
clusters to ask for.

Each sample is scored on how much closer it sits to its own cluster than to the nearest other one:
`1` means comfortably inside, `0` means on the boundary, and a negative value means it would be
better off elsewhere. The score of a clustering is the mean of those.

Two ways in, one computation. Hand it the samples and it uses the euclidean distance; hand it a
distance matrix you already computed and it uses that. The frozen corpus checks the two agree at
`1e-9` on every case — not bit for bit, because the distances themselves are computed differently
on each side.

## Members

| Member | What it does |
| --- | --- |
| [`Silhouette.PerSample`](silhouette-persample.md) | The score of each sample, from the samples themselves. |
| [`Silhouette.PerSampleFromDistances`](silhouette-persamplefromdistances.md) | The score of each sample, from a distance matrix. |
| [`Silhouette.Score`](silhouette-score.md) | The mean over every sample, from the samples themselves. |
| [`Silhouette.ScoreFromDistances`](silhouette-scorefromdistances.md) | The mean over every sample, from a distance matrix. |
