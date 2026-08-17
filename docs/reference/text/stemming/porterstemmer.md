# PorterStemmer

English stemming by Martin Porter's original 1980 algorithm.

<!-- docs-declaration -->

```csharp
public static class PorterStemmer
```

**Example** — the textbook plural.

```csharp
using Lodestar.Text.Stemming;

string stem = PorterStemmer.Stem("caresses");  // => caress
```

**Remarks** — reach for this **only to match an index that already exists**. Porter himself
replaced it with Porter2, published as Snowball and available here as
[`EnglishSnowballStemmer`](englishsnowballstemmer.md), which is his own correction of it.
[The index page](../stemming.md) lists all six words the two disagree about.

The algorithm is five steps of suffix rules, gated on a measure of vowel-consonant alternation
rather than on named regions — the mechanism Porter2 replaced with R1 and R2. It knows only ASCII
letters, so an accented word is not something it was built to receive.

Reference behaviour is `nltk.stem.porter.PorterStemmer(mode=ORIGINAL_ALGORITHM)`, matched over
86 words.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EnglishSnowballStemmer`](englishsnowballstemmer.md),
[the stemming index](../stemming.md).

## Members

| Member | What it does |
| --- | --- |
| [`PorterStemmer.Stem`](porterstemmer-stem.md) | The Porter stem of one English word. |
