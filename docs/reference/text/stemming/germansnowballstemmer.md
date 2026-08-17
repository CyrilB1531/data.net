# GermanSnowballStemmer

German stemming by the Snowball algorithm.

<!-- docs-declaration -->

```csharp
public static class GermanSnowballStemmer
```

**Example** — a noun's case endings, collapsed.

```csharp
using Lodestar.Text.Stemming;

string nominative = GermanSnowballStemmer.Stem("kind");  // => kind
string plural = GermanSnowballStemmer.Stem("kinder");  // => kind
string dative = GermanSnowballStemmer.Stem("kindern");  // => kind
```

**Remarks** — German differs from the Romance stemmers here in three ways worth knowing before
reading a stem.

**`ß` becomes `ss` first**, so the two spellings of a word cannot produce two keys: `straße` and
`strasse` both stem to `strass`. **Umlauts are removed at the end**, once the suffix rules have
run, which is why `größe` comes back as `gross` rather than `größ`.

And there is **no RV region** — the algorithm uses R1 and R2 only, with R1 floored at three
characters. That floor is what stops short words from being trimmed to nothing, the job RV does in
the Romance languages.

Compound nouns are not split. `Donaudampfschiff` is one word to this algorithm and comes back as
one stem; splitting German compounds needs a dictionary, and none ships here.

Reference behaviour is `nltk.stem.snowball.SnowballStemmer("german")`, matched over 88 words.

**Applies to** — net10.0, netstandard2.0.

**See also** — [the stemming index](../stemming.md).

## Members

| Member | What it does |
| --- | --- |
| [`GermanSnowballStemmer.Stem`](germansnowballstemmer-stem.md) | The Snowball stem of one German word. |
