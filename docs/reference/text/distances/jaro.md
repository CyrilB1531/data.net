# Jaro

A score built for short human names: characters count as matching when they are merely near each
other, not necessarily in the same place.

A score in `[0, 1]` built from how many characters the two share within a window that widens with
their length, and how many of those arrive out of order. Short strings are what it was designed
for — names, codes — and it says nothing about long texts that a reader should trust.
`JaroWinkler` is this score with a prefix bonus on top.

## Members

| Member | What it does |
| --- | --- |
| [`Jaro.Similarity`](jaro-similarity.md) | Scores two strings on how many characters they share within a sliding window, and how many of those |
| [`Jaro.Distance`](jaro-distance.md) | `1 - Similarity`, for code that wants a distance rather than a score. |
