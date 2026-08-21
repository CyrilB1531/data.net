# SwedishSnowballStemmer.Stem

The Snowball stem of one Swedish word.

<!-- docs-declaration -->

```csharp
public static string Stem(string word)
```

**Parameters** — `word` is a single Swedish word, lowercased before the rules run. `å`, `ä` and
`ö` are vowels of the alphabet and are never folded to `a` or `o`; folding them would merge words
the language keeps apart.

**Returns** — `string`, the stem, always lowercase.

**Exceptions** — `ArgumentNullException` when `word` is `null`. A word of one or two characters is
returned lowercased and otherwise untouched, having no room for a region.

**Example** — the two rules that rewrite rather than delete, and a word the region floor protects.

```csharp
using Lodestar.Text.Stemming;

string hopeless = SwedishSnowballStemmer.Stem("hopplöst");  // => hopplös
string adjective = SwedishSnowballStemmer.Stem("vänligt");  // => vän
string brief = SwedishSnowballStemmer.Stem("nytt");  // => nytt
```

**Remarks** — `vänligt` losing five characters and `nytt` losing none is the region floor doing
its job. `vänligt` is long enough that `gt` and then `lig` both fall inside R1; `nytt` is not, so
its `tt` is left where it is.

A stem is a key, not a word. `böckerna` comes back as `böck`, which is not Swedish; what matters
is that `bok`'s inflected forms that reach this algorithm land on the same key, and that no other
root lands there too.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SwedishSnowballStemmer`](swedishsnowballstemmer.md),
[the stemming index](../stemming.md).
