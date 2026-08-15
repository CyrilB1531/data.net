# JaroWinkler

`Jaro` with a thumb on the scale for pairs that already start the same way — the usual default for
matching surnames.

`Jaro`, raised when the two already agree on their first few characters — up to four, at weight
`0.1`, and only once `Jaro` is past `0.7`. Those three constants are Winkler's own. The bonus
assumes a leading agreement is strong evidence, which holds for surnames and not for arbitrary
text.

## Members

| Member | What it does |
| --- | --- |
| [`JaroWinkler.Similarity`](jarowinkler-similarity.md) | Computes `Jaro.Similarity` and then raises it in proportion to how many of the first four |
| [`JaroWinkler.Distance`](jarowinkler-distance.md) | `1 - Similarity`, for code that wants a distance rather than a score. |
