# DamerauLevenshtein

The edit distance for text where two characters get typed in the wrong order: a swap costs one
edit, not two.

At unit costs this **is** a true metric — the triangle inequality holds — which is what makes it
the one to index with. `Osa` computes the restricted variant faster and is not a metric; the two
disagree whenever a stretch of text would have to be edited twice, `"CA"` against `"ABC"` being
the smallest case.

## Members

| Member | What it does |
| --- | --- |
| [`DamerauLevenshtein.Distance`](dameraulevenshtein-distance.md) | Counts the fewest insertions, deletions, substitutions and swaps of neighbouring characters that |
| [`DamerauLevenshtein.NormalizedDistance`](dameraulevenshtein-normalizeddistance.md) | Scales the distance into `[0, 1]` by dividing it by the length of the longer input. |
| [`DamerauLevenshtein.NormalizedSimilarity`](dameraulevenshtein-normalizedsimilarity.md) | `1 - NormalizedDistance`: `1` when the two are identical, `0` when nothing is shared. |
