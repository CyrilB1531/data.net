# FrenchSnowballStemmer.Stem

The Snowball stem of one French word.

<!-- docs-declaration -->

```csharp
public static string Stem(string word)
```

**Parameters** — `word` is a single French word. It is lowercased and NFC-normalised before the
rules run.

**Returns** — `string`, the stem, always lowercase. Accented characters survive when the stem
keeps them.

**Exceptions** — `ArgumentNullException` when `word` is `null`. An empty string, or a word of one
character, is returned lowercased and otherwise untouched.

**Example** — an adverb reduced to its root, and two forms of one verb meeting.

```csharp
using Lodestar.Text.Stemming;

string adverb = FrenchSnowballStemmer.Stem("amoureusement");  // => amour
string infinitive = FrenchSnowballStemmer.Stem("finir");  // => fin
string participle = FrenchSnowballStemmer.Stem("finissant");  // => fin
```

**Remarks** — `amoureusement` losing both `-ement` and `-eux` in one pass shows the step ordering
doing its work: the adverbial suffix goes first, and what it uncovers is then eligible for the
next rule. This is why the steps are conditional on each other rather than independent.

Aggressive is the right word for the result. `continuellement` reduces to `continuel`, and verbs
of the second group lose most of their length. That is appropriate for an index and wrong for
anything shown to a reader.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`FrenchSnowballStemmer`](frenchsnowballstemmer.md),
[the stemming index](../stemming.md).
