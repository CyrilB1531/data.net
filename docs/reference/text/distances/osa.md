# Osa

`DamerauLevenshtein` under one restriction: no stretch of text may be edited twice. Cheaper, and
the variant rapidfuzz calls OSA.

Optimal String Alignment: a swap costs one edit, but no stretch of text may be edited twice. That
restriction is what makes it cheaper than `DamerauLevenshtein` and what breaks the triangle
inequality, so it is a comparison to sort by rather than a distance to index with.

## Members

| Member | What it does |
| --- | --- |
| [`Osa.Distance`](osa-distance.md) | Counts the fewest insertions, deletions, substitutions and swaps of neighbouring characters, with |
| [`Osa.NormalizedDistance`](osa-normalizeddistance.md) | Scales the distance into `[0, 1]` by dividing it by the length of the longer input. |
| [`Osa.NormalizedSimilarity`](osa-normalizedsimilarity.md) | `1 - NormalizedDistance`: `1` when the two are identical, `0` when nothing survives. |
