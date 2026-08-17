# SpanishSnowballStemmer.Stem

The Snowball stem of one Spanish word.

<!-- docs-declaration -->

```csharp
public static string Stem(string word)
```

**Parameters** — `word` is a single Spanish word. It is lowercased and NFC-normalised before the
rules run.

**Returns** — `string`, the stem, always lowercase.

**Exceptions** — `ArgumentNullException` when `word` is `null`. An empty string, or a word of one
character, is returned lowercased and otherwise untouched.

**Example** — an attached pronoun removed, and the same word with and without its accent.

```csharp
using Lodestar.Text.Stemming;

string gerund = SpanishSnowballStemmer.Stem("cantándome");  // => cant
string infinitive = SpanishSnowballStemmer.Stem("cantar");  // => cant
string accented = SpanishSnowballStemmer.Stem("dámelo");  // => damel
string plain = SpanishSnowballStemmer.Stem("damelo");  // => damel
```

**Remarks** — `cantándome` reaching the same `cant` as `cantar` is step 0 and the verb rules in
sequence: `-me` goes first as an attached pronoun, then `-ándo` as a gerund ending. Neither would
have fired without the other, which is why the pronoun step has to come first.

`dámelo` and `damelo` agreeing matters for real corpora, where accents are dropped often enough
that treating the two spellings as different terms would split a term's postings in half.

The one thing this method does not do is disambiguate: the gerund forms of `hacer` all reach
`hac`, which names nothing on its own. Stems are compared, not read.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SpanishSnowballStemmer`](spanishsnowballstemmer.md),
[the stemming index](../stemming.md).
