# DutchSnowballStemmer

Dutch stemming by the Snowball algorithm.

<!-- docs-declaration -->

```csharp
public static class DutchSnowballStemmer
```

**Example** — a plural, a derivation and the doubled vowel a deletion leaves behind.

```csharp
using Lodestar.Text.Stemming;

string plural = DutchSnowballStemmer.Stem("fietsen");  // => fiets
string derived = DutchSnowballStemmer.Stem("mogelijkheden");  // => mogelijk
string doubled = DutchSnowballStemmer.Stem("manen");  // => man
```

**Remarks** — three things distinguish Dutch from the other Germanic stemmer here.

**Accents are folded before anything else.** `ë` and `é` both become `e`, so `financiën` and a
hypothetical `financien` cannot produce two keys. German does the opposite, folding its umlauts
last; the difference is deliberate in both, because German's suffix rules read the umlaut while
Dutch's never do.

**A `y` at the start or after a vowel, and an `i` between two vowels, count as consonants.** They
are held in upper case while the rules run and lowered again at the end, so nothing of that
marking reaches the caller.

**A stem can end in a shortened vowel.** `maan` comes back as `man` and `brood` as `brod`: when a
deletion leaves a word ending consonant–vowel–vowel–consonant, one of the doubled vowels goes.
That is a spelling rule of the language, not a truncation, and it is what makes `maan` and `manen`
share a key.

R1 is floored at three characters as in German, but R2 is measured *before* that floor is applied.
Measuring it after moves R2 rightward on a short word and refuses a suffix that belongs inside it —
`overheid` would stem to itself instead of to `over`.

Reference behaviour is `nltk.stem.snowball.SnowballStemmer("dutch")`, matched over 109 words.

**Applies to** — net10.0, netstandard2.0.

**See also** — [the stemming index](../stemming.md).

## Members

| Member | What it does |
| --- | --- |
| [`DutchSnowballStemmer.Stem`](dutchsnowballstemmer-stem.md) | The Snowball stem of one Dutch word. |
