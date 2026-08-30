# 0468 — The .npy reader carried a limit its own option exempts it from

**Issue:** [#468](https://github.com/CyrilB1531/lodestar/issues/468) ·
**Status:** accepted — fixed in [`c480c1f`](https://github.com/CyrilB1531/lodestar/commit/c480c1f) ·
**Date:** 2026-08-29 · **Written after the fact**, per `CLAUDE.md`: a spec records measured facts.

## What was wrong

`NpyFile` checked the product of the declared shape against `MaxArrayLength`. A 10 000 × 384
block — the benchmark corpus — is 3 840 000 elements against a default of 1 000 000, so it threw:

```text
The artifact exceeds the maximum length of array 'shape': 3840000
(limit 1000000, from ArtifactLoadOptions.MaxArrayLength).
```

The same vectors loaded from an index artifact one line earlier without complaint.

## Why the two disagreed

They applied the same limit in different units.

| | checked against `MaxArrayLength` | value for the corpus |
| --- | --- | ---: |
| `EmbeddingIndex.Persistence` | the **vector count** | 10 000 |
| `NpyFile` | the **element count** | 3 840 000 |

`ArtifactLoadOptions` documents which is meant, and it is neither for this data:

> Maximum length of any single array in the source, **except an `EmbeddingIndex`'s vector block,
> which `MaxTotalBytes` bounds instead**.

A `.npy` is a vector block and nothing else, so it was carrying the limit its own documentation
exempts it from. The smallest block affected is 2 605 × 384, which is small for embeddings — the
default refused the ordinary case on the path advertised as the raw interchange route.

## What the fix had to preserve

The removed check incidentally prevented an overflow: two declared dimensions can multiply to a
product whose `× sizeof(float)` does not fit a `long`. Its replacement compares by **dividing**
the limit rather than multiplying the element count, so the guard cannot itself overflow.

Nothing weakened. The payload is already read under `MaxTotalBytes`, and the
shape-against-available check still refuses a block that overruns its data.

## What the tests pin, and one that did not

Two, at the boundary — and the first attempt at the second one **passed while testing nothing**.
It set `MaxTotalBytes` under the block's size, so `JsonArtifact.ReadAllBytes` refused the payload
while reading it and the new bound was never reached. SonarCloud caught it as 33.3% coverage on
new code, which is the gate earning its keep.

The bound is only reachable when a header *declares* more elements than the limit allows while
the file holds nothing — which numpy will never write. So the test hand-builds a v1.0 header for
shape `(1000000, 1000)` with no data, the same shape as the structural-garbage cases already in
`NpyFileTests`: a fixture from numpy cannot express a file numpy refuses to produce.

The other test is kept and renamed to what it actually pins — a payload past `MaxTotalBytes` is
refused *while being read*, before any header is parsed — which is the reason the first needs its
own header.

## How it was found

Measuring what a binary sidecar would buy for [#436](https://github.com/CyrilB1531/lodestar/issues/436),
against the `NpyFile` reader [#450](https://github.com/CyrilB1531/lodestar/issues/450) shipped.
The measurement threw before it measured anything.
