# EnglishSnowballStemmer

English stemming by the Snowball algorithm — Porter2, the revision of Porter's original.

<!-- docs-declaration -->

```csharp
public static class EnglishSnowballStemmer
```

**Example** — three inflections of one verb, collapsed onto one key.

```csharp
using Lodestar.Text.Stemming;

string a = EnglishSnowballStemmer.Stem("running");  // => run
string b = EnglishSnowballStemmer.Stem("runs");  // => run
```

**Remarks** — this is the default choice for English. Porter published it himself as the
correction of his 1980 algorithm, and [`PorterStemmer`](porterstemmer.md) is kept only for
indexes already built with the original.

Two things make it more accurate than its predecessor. It defines the regions **R1** and **R2** —
the parts of the word past the first and second vowel-consonant boundary — and refuses to trim a
suffix that reaches outside them, which is what stops `communism` from becoming `commun`. And it
carries a short list of **exceptions** for words the rules get wrong, mapped before any step runs.

Reference behaviour is `nltk.stem.snowball.SnowballStemmer("english")`, matched over 190 words.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`PorterStemmer`](porterstemmer.md), [the stemming index](../stemming.md).

## Members

| Member | What it does |
| --- | --- |
| [`EnglishSnowballStemmer.Stem`](englishsnowballstemmer-stem.md) | The Porter2 stem of one English word. |
