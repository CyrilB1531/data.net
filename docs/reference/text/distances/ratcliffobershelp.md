# RatcliffObershelp

The measure behind Python's `difflib`: find the longest matching block, then do the same on what
is
left either side of it, and report how much of the two texts got covered.

Gestalt pattern matching: find the longest matching block, then recurse into what is left on
either side, and report how much of the two texts the blocks cover. It rewards material that
arrives in a few long passages where `Indel` rewards material that is shared at all, scattered or
not. difflib's `autojunk` heuristic is deliberately not reproduced ([decision
0006](../../../decisions/0006-ratcliff-autojunk.md)).

## Members

| Member | What it does |
| --- | --- |
| [`RatcliffObershelp.Similarity`](ratcliffobershelp-similarity.md) | Scores two strings as twice the total length of their recursively matched blocks, divided by the |
| [`RatcliffObershelp.Distance`](ratcliffobershelp-distance.md) | `1 - Similarity`, for code that wants a distance rather than a score. |
