# GermanSnowballStemmer.Stem

The Snowball stem of one German word.

<!-- docs-declaration -->

```csharp
public static string Stem(string word)
```

**Parameters** — `word` is a single German word. It is lowercased and NFC-normalised before the
rules run, which matters more here than elsewhere: German nouns are capitalised in ordinary text,
so most input arrives needing it.

**Returns** — `string`, the stem, always lowercase; the final step folds umlauts to their base
vowel.

**Exceptions** — `ArgumentNullException` when `word` is `null`. An empty string, or a word of one
character, is returned lowercased and otherwise untouched.

**Example** — the two spellings of `ß` meeting, and an umlaut removed on the way out.

```csharp
using Lodestar.Text.Stemming;

string sharp = GermanSnowballStemmer.Stem("straße");  // => strass
string spelled = GermanSnowballStemmer.Stem("strasse");  // => strass
string umlaut = GermanSnowballStemmer.Stem("größe");  // => gross
```

**Remarks** — `größe` → `gross` is the result most likely to look like a bug. It is two
substitutions, both deliberate: `ß` → `ss` before the rules, `ö` → `o` after them. The stem is a
key, and it is the same key `grosse` produces, which is the whole intent.

Because umlauts are stripped last rather than first, a word whose suffix rule depends on `ä` or
`ü` still sees it while the decision is being made. Folding them up front would change which
suffixes match, and give different stems.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`GermanSnowballStemmer`](germansnowballstemmer.md),
[the stemming index](../stemming.md).
