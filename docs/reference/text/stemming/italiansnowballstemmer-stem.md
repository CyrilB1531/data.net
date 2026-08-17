# ItalianSnowballStemmer.Stem

The Snowball stem of one Italian word.

<!-- docs-declaration -->

```csharp
public static string Stem(string word)
```

**Parameters** — `word` is a single Italian word. It is lowercased and NFC-normalised before the
rules run.

**Returns** — `string`, the stem, always lowercase.

**Exceptions** — `ArgumentNullException` when `word` is `null`. An empty string, or a word of one
character, is returned lowercased and otherwise untouched.

**Example** — a stressed final vowel written both ways, and the divergence worth knowing about.

```csharp
using Lodestar.Text.Stemming;

string accented = ItalianSnowballStemmer.Stem("attività");  // => attiv
string plain = ItalianSnowballStemmer.Stem("attivita");  // => attiv
string diverging = ItalianSnowballStemmer.Stem("esistenza");  // => esistt
```

**Remarks** — `esistt` is not a typo and not a bug. It is what `nltk` returns, and matching `nltk`
is what this package is checked against; the published algorithm would give `esistent`. The
reasoning is in [`decisions/0008`](../../../decisions/0008-italian-enza-nltk-divergence.md), and
the practical consequence is that the stem is still a usable key — every `enza`/`enze` word the
rule reaches is transformed the same way — while being unreadable.

The rule only fires inside R2. `pazienza` keeps its suffix and stems to `pazienz`, which is why
the two words with the same ending come out looking unrelated.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`ItalianSnowballStemmer`](italiansnowballstemmer.md),
[the stemming index](../stemming.md).
