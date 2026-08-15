# Levenshtein

The edit distance most people mean when they say "how close are these two strings".

Unit costs, so every insertion, deletion and substitution counts one. Reach for `Distance` when
you want a count you can threshold, and for `NormalizedSimilarity` when you want a score
comparable across pairs of different lengths. A swap of two neighbouring characters costs **two**
edits here; when that is the mistake you are chasing, `DamerauLevenshtein` is the one that charges
one.

## Members

| Member | What it does |
| --- | --- |
| [`Levenshtein.Distance`](levenshtein-distance.md) | Counts the fewest insertions, deletions and substitutions that turn one string into the other. |
| [`Levenshtein.NormalizedDistance`](levenshtein-normalizeddistance.md) | Scales the distance into `[0, 1]` by dividing it by the length of the longer input. |
| [`Levenshtein.NormalizedSimilarity`](levenshtein-normalizedsimilarity.md) | `1 - NormalizedDistance`: `1` when the two are identical, `0` when nothing survives. |
