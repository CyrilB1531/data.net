# DaviesBouldin

For each cluster, the worst ratio of "how spread these two are" to "how far apart they sit",
averaged over the clusters. **Lower is better** — the opposite direction to
[`CalinskiHarabasz`](calinskiharabasz.md) and [`Silhouette`](silhouette.md), and `0` is the floor.

That inversion is the one thing worth checking before reading a table of these: a clustering that
improves moves this number down and the other two up, and a reader who takes the three as
interchangeable will read one of them backwards.

Like [`CalinskiHarabasz`](calinskiharabasz.md), it scores a clustering against nothing but the
samples, takes a feature matrix rather than two label vectors, and has **no precomputed-distance
form** — both read cluster centroids, which a distance matrix does not carry.

## Two clusters sharing a centroid contribute nothing

The ratio would divide by zero. The reference replaces a zero centroid distance with infinity before
dividing, so the pair scores `0` and drops out of its cluster's worst case. Measured, four identical
points split into two clusters score `0` — as do two well-separated points each duplicated into a
cluster of its own, which is a perfect clustering. The floor is reached from both directions, and
the number cannot tell them apart.

## Members

| Member | What it does |
| --- | --- |
| [`DaviesBouldin.Score`](daviesbouldin-score.md) | The mean worst-case similarity between a cluster and any other. |
