# PorterStemmer.Stem

The Porter stem of one English word.

<!-- docs-declaration -->

```csharp
public static string Stem(string word)
```

**Parameters** — `word` is a single word, not a sentence. It is lowercased before the first step
runs.

**Returns** — `string`, the stem, always lowercase.

**Exceptions** — `ArgumentNullException` when `word` is `null`. An empty string is returned
unchanged, as is any word of two characters or fewer.

**Example** — the same word under both English stemmers, which is the only reason to prefer this
one.

```csharp
using Lodestar.Text.Stemming;

string original = PorterStemmer.Stem("ties");  // => ti
string revised = EnglishSnowballStemmer.Stem("ties");  // => tie
```

**Remarks** — `ti` is not a mistake in this implementation; it is what the 1980 algorithm
specifies, and `nltk` returns it too. Preserving that is the point of shipping the original at
all. If the answer looks wrong, the fix is
[`EnglishSnowballStemmer.Stem`](englishsnowballstemmer-stem.md), not a patch here.

Only ASCII letters are treated as letters. Accented input is neither rejected nor folded — it is
simply not what the rules were written for, and another language's stemmer is the right answer for
another language's text.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`PorterStemmer`](porterstemmer.md),
[`EnglishSnowballStemmer.Stem`](englishsnowballstemmer-stem.md),
[the stemming index](../stemming.md).
