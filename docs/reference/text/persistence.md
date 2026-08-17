# Persistence — `Lodestar.Text`

A fitted vectorizer is worth saving: the vocabulary and the document frequencies came from a
training corpus, and that corpus may not be around later. `Lodestar.Text.Persistence` holds the
one type that governs reading such a file back —
[`ArtifactLoadOptions`](persistence/artifactloadoptions.md), the bounds a load is held to.

## Why a load needs bounds at all

A saved artifact is a file, and a file can come from anywhere. Deserializing one that declares a
hundred million vocabulary entries would allocate them before anything noticed, which is a denial
of service written as a document. Every `Load` and `LoadAsync` in this package takes these bounds,
and the defaults are generous enough that a legitimate model never meets one.

That is the deliberate difference from Python's `pickle.load`, which this package's format
replaces: `pickle` executes arbitrary code by design, so bounding it is not possible.
[`decisions/0011`](../../decisions/0011-persistence-format.md) has that comparison and why the
format is JSON.

An artifact that exceeds a bound is **refused**, not truncated. A model that silently loaded
smaller than it was saved would score differently and say nothing.

## Types

| Type | What it is |
| --- | --- |
| [`ArtifactLoadOptions`](persistence/artifactloadoptions.md) | The five bounds a load is held to. |

## See also

- [`CountVectorizer.Load`](vectorizers/countvectorizer-load.md) and
  [`TfidfVectorizer.Load`](vectorizers/tfidfvectorizer-load.md) — the members that take these.
- [From string to vector](../../guides/vectorization.md) — "Reading a file you did not write".
