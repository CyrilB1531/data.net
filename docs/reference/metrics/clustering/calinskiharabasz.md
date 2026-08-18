# CalinskiHarabasz

The variance ratio criterion: how far the clusters sit from each other, against how spread they are
inside, each corrected for the degrees of freedom it uses. **Higher is better**, and there is no
upper bound — which is what makes it a way to compare clusterings of one dataset rather than a
quality you can read on its own.

Where the five agreement metrics compare a clustering against a **reference partition**, this and
[`DaviesBouldin`](daviesbouldin.md) score it against nothing but the samples. That is what makes them
useful for choosing how many clusters to ask for, and it is why they take a feature matrix where the
others take two label vectors.

## It reads centroids, so there is no precomputed-distance form

[`Silhouette`](silhouette.md) offers [`ScoreFromDistances`](silhouette-scorefromdistances.md) for a
caller who has a distance matrix and wants another metric than the euclidean. Neither of these two
can: both read the mean position of each cluster, and a distance matrix does not carry one. A reader
arriving from `silhouette_score(metric='precomputed')` will look for the equivalent and there is
none — the reference has none either.

## What it answers where the arithmetic runs out

**Clusters with no spread at all score `1`**, not an infinity: the reference tests the within-cluster
dispersion against exact zero and returns `1` rather than dividing. Measured, four identical points
split into two clusters score `1`, and so do two distinct points each duplicated into a cluster of
its own — the second is a perfect clustering and reads the same as the degenerate one, which is the
number's own limit rather than something this library chose.

**Neither this nor `DaviesBouldin` ever returns a non-finite value** on an input the reference
accepts. That was worth measuring rather than assuming, because no other metric in this package
answers with an infinity or a `NaN` and one that did would be a surprise.

## Members

| Member | What it does |
| --- | --- |
| [`CalinskiHarabasz.Score`](calinskiharabasz-score.md) | The between-cluster dispersion over the within-cluster one. |
