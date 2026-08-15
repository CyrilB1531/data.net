# Hamming

Position-by-position comparison: the measure for fixed-width codes, where nothing ever shifts
along.

Only for inputs that line up position by position: fixed-width codes, identifiers, checksums. A
difference in length is added to the count rather than aligned away, so a single inserted
character makes every position after it differ — which is exactly what `Levenshtein` exists to
forgive.

## Members

| Member | What it does |
| --- | --- |
| [`Hamming.Distance`](hamming-distance.md) | Counts the positions at which the two differ, then adds the difference in their lengths. |
| [`Hamming.NormalizedSimilarity`](hamming-normalizedsimilarity.md) | Turns the distance into a score in `[0, 1]`: `1 - distance / max(len(a), len(b))`. |
