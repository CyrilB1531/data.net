# RatcliffObershelp.Similarity

Scores two strings as twice the total length of their recursively matched blocks, divided by the
sum of their lengths.

<!-- docs-declaration -->

```csharp
public static double Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `a` and `b` are the two strings to compare. `element` says what counts as one
character; `difflib` works on code points, so `TextElement.CodePoint` is what reproduces its
numbers
on supplementary-plane text.

**Returns** — `double` in `[0, 1]`, larger meaning more alike. `1` for equal inputs, and `1` when
both are empty.

**Example** — the matched blocks are `st` and `e`: three characters, counted twice, over ten.

```csharp
using DataNet.Text.Distances;

double s = RatcliffObershelp.Similarity("state", "taste");   // => 0.6
```

**Remarks** — this is the measure for longer text whose overlap comes in **passages**: it rewards
long unbroken runs and does not care how much unmatched material sits between them. It is exactly
`difflib.SequenceMatcher(None, a, b).ratio()`, so it is the port for anything written against
Python's standard library rather than against rapidfuzz.

The page's other recommendation for longer text is `Indel`, and the two are not interchangeable
even though they agree on plenty of pairs. The difference is contiguity: `Indel` credits every
character the two share in order however scattered, while this credits only characters inside a
shared unbroken run, and it commits greedily to the longest run before looking at what is left. On
`("state", "taste")` — the example above — that is `0.6` here against `0.8` from
`Indel.NormalizedSimilarity`, and on `("conversation", "voicesranton")` it is `0.25` against
`0.5833…`. Reach for this when a long verbatim passage should count for more than the same number
of
characters sprinkled about, and for `Indel` when it should not.

Two things to know, and the first is the one that catches people. This measure is **not
symmetric**:
swapping the arguments can change the answer, sometimes by a lot. `Similarity("bbcabba", "bacaa")`
is `0.6666…` and `Similarity("bacaa", "bbcabba")` is `0.3333…`, because the recursion anchors on
the
longest matching block and difflib's tie-break — earliest start in `a`, then earliest in `b` — is
reproduced here, so a tie broken one way for `(a, b)` breaks the other way for `(b, a)`. Fix an
argument order and keep it, or you will get two different scores for the same pair of records.

And on inputs longer than 200 elements it deliberately diverges from `difflib`'s default. difflib
applies an `autojunk` heuristic there, ignoring any element that appears in more than 1% of
positions; this implementation does not, matching `difflib(autojunk=False)` at every length. The
reasoning is in [decision 0006](../../../decisions/0006-ratcliff-autojunk.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — `RatcliffObershelp.Distance`, `Lcs.SubstringLength`, `Indel.NormalizedSimilarity`,
[decision 0006](../../../decisions/0006-ratcliff-autojunk.md),
the [Python equivalence table](../../../equivalence.md).
