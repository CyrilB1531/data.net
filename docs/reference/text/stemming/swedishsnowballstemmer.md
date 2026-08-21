# SwedishSnowballStemmer

Swedish stemming by the Snowball algorithm.

<!-- docs-declaration -->

```csharp
public static class SwedishSnowballStemmer
```

**Example** — a definite plural, a chain of suffixes, and a compound that keeps its head.

```csharp
using Lodestar.Text.Stemming;

string plural = SwedishSnowballStemmer.Stem("flickorna");  // => flick
string chained = SwedishSnowballStemmer.Stem("verksamheterna");  // => verksam
string compound = SwedishSnowballStemmer.Stem("kraftfullt");  // => kraftfull
```

**Remarks** — Swedish is the simplest of the algorithms here, and its shape is worth knowing
because two more share it.

**There is only R1.** No R2, no RV: every rule tests the same single region, floored at three
characters. That floor is the whole of the protection short words get, which is why `hus` and
`nytt` come back untouched.

**The suffix list is long and the steps are three.** The first deletes the longest listed suffix
lying in R1, or a final `s` when the letter before it is one the algorithm allows. The second
drops the last letter of a hard ending such as `gt` or `tt`. The third removes `lig`, `ig` or
`els`, and rewrites `fullt` to `full` and `löst` to `lös`.

**A suffix outside R1 is not a candidate at all.** `rolig` ends in both `lig` and `ig`; only `ig`
lies inside R1, so `ig` wins and the stem is `rol`. Taking the longer match first and testing it
afterwards leaves the word unstemmed — a difference that shows up only when a short suffix is
inside the region and its extension is not.

Reference behaviour is `nltk.stem.snowball.SnowballStemmer("swedish")`, matched over 94 words.

**Applies to** — net10.0, netstandard2.0.

**See also** — [the stemming index](../stemming.md).

## Members

| Member | What it does |
| --- | --- |
| [`SwedishSnowballStemmer.Stem`](swedishsnowballstemmer-stem.md) | The Snowball stem of one Swedish word. |
