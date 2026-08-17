# EnglishSnowballStemmer.Stem

The Porter2 stem of one English word.

<!-- docs-declaration -->

```csharp
public static string Stem(string word)
```

**Parameters** — `word` is a single word, not a sentence. It is lowercased before anything else
runs, so the caller need not do it.

**Returns** — `string`, the stem, always lowercase. It is a key rather than a word: `poni` and
`agre` are both correct answers.

**Exceptions** — `ArgumentNullException` when `word` is `null`. An empty string is returned
unchanged, as is any word of two characters or fewer.

**Example** — a plural, a participle and a word the rules would over-trim.

```csharp
using Lodestar.Text.Stemming;

string plural = EnglishSnowballStemmer.Stem("ponies");  // => poni
string participle = EnglishSnowballStemmer.Stem("agreed");  // => agre
string kept = EnglishSnowballStemmer.Stem("communism");  // => communism
```

**Remarks** — `communism` is the one worth reading twice. The `-ism` suffix is real, and the
algorithm still declines to remove it, because doing so would cut into R1. That restraint is the
difference from `PorterStemmer.Stem`, which returns `commun` — a fragment short enough to be
reached from several unrelated words.

Words of two characters or fewer skip the algorithm entirely and come back lowercased. Nothing
sensible can be trimmed from `an`, and the rules would find a suffix anyway.

Call it once per token, after tokenizing — it has no notion of whitespace, and a whole sentence
passed in is treated as one long word.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`EnglishSnowballStemmer`](englishsnowballstemmer.md),
[`PorterStemmer.Stem`](porterstemmer-stem.md), [the stemming index](../stemming.md).
