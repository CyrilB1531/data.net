# 0434 — The vocabulary dictionary is already pre-sized

**Issue:** [#434](https://github.com/CyrilB1531/lodestar/issues/434) ·
**Status:** proposed — recommends closing the issue as already done · **Date:** 2026-08-29

## Problem, as the issue states it

`tfidf_load` sits at ~7 ms and was untouched by all four lots on this
path (#323, #324, #336 and #377). The issue proposes the one lever it believes remains: build the vocabulary → index
`Dictionary` with a capacity taken from `featureCount`, which
[ADR 0011](../../decisions/0011-persistence-format.md) guarantees is written **before** the array it
describes. Every resize and rehash on the way to 30 000 entries would then disappear.

## The lever is already pulled

`TfidfVectorizer.Load` reaches the dictionary through exactly one path:

```text
TfidfVectorizer.Load(...)
  → FeatureVocabularyJson.ReadVocabulary(ref reader, ArtifactName, limits, featureCount)
  → CountVectorizer.RestoreVocabulary(string[] sortedFeatureNames)
```

and `RestoreVocabulary` — `src/Lodestar.Text/Vectorization/CountVectorizer.Persistence.cs:112` —
reads:

```csharp
var vocabulary = new Dictionary<string, int>(sortedFeatureNames.Length, StringComparer.Ordinal);
```

**The capacity is already the final count.** `git log -S RestoreVocabulary` puts it in `aa283eb`,
*Persist fitted vectorizers as versioned JSON artifacts* — the commit that introduced the artifact
format. It has never not been pre-sized.

The array's length is what supplies the capacity rather than `featureCount` directly, which is
strictly better than the issue asks for: `EnsureDeclaredCount` confronts the two before
`RestoreVocabulary` runs, so the capacity is a length the reader has already validated rather than
a number the file declared. The issue's own safety note — *"the check must stay ahead of the
capacity, not behind it"* — is satisfied, and by construction rather than by care.

## What this means for the issue

**#434 closes as already done, not as refused.** The distinction matters on a roadmap: nobody
should re-propose this, and nobody should look for a regression that was never there.

`tfidf_load` staying at ~7 ms across four lots is therefore explained by what the issue already
suspected — its buffers are below the large-object-heap threshold, so none of the allocation work
those lots did applies — and **not** by a missed dictionary lever.

## What would still be worth knowing, and is not this issue

Nothing here measured where `tfidf_load`'s 7 ms goes. #324 profiled the *index* load phase by
phase and that is what made its lot decidable; no equivalent profile exists for the tf-idf artifact,
and the guide records it reading anywhere from 6.9 to 7.9 ms, a spread wide enough to hide any
lever smaller than about 15%.

If that profile is wanted it is a new issue, and it is the step-0 shape ADR 0051 argues for:
measure the phases first, then decide whether there is a lot at all. Opening it is not this spec's
call.

## Acceptance

- The reading above confirmed by someone other than its author — one `grep` for `new Dictionary`
  under `src/Lodestar.Text/Vectorization/` and one read of `RestoreVocabulary` is the whole check.
- #434 closed with the file, the line and the commit named, so the finding outlives the issue.
- No code change. **A no-op commit "implementing" a pre-size that already exists would be worse
  than nothing**, because the next reader would take it for a fix and look for the regression.
