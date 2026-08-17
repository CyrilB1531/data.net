# PortugueseSnowballStemmer.Stem

The Snowball stem of one Portuguese word.

<!-- docs-declaration -->

```csharp
public static string Stem(string word)
```

**Parameters** — `word` is a single Portuguese word. It is lowercased and NFC-normalised before
the rules run.

**Returns** — `string`, the stem, always lowercase. Unlike the Spanish stemmer, this one does not
strip accents: 19 of the 105 pinned words stem to something still carrying one.

**Exceptions** — `ArgumentNullException` when `word` is `null`. An empty string, or a word of one
character, is returned lowercased and otherwise untouched.

**Example** — a derivation collapsing onto its root, and a plural that does not collapse at all.

```csharp
using Lodestar.Text.Stemming;

string noun = PortugueseSnowballStemmer.Stem("atividade");  // => ativ
string adverb = PortugueseSnowballStemmer.Stem("ativamente");  // => ativ
string singular = PortugueseSnowballStemmer.Stem("geração");  // => geraçã
string plural = PortugueseSnowballStemmer.Stem("gerações");  // => geraçõ
```

**Remarks** — `atividade` and `ativamente` meeting at `ativ` is the behaviour that makes a
stemmer worth running: a noun and an adverb built from one adjective are one term to a reader and
would be two to an index without it.

`geração` and `gerações` are the counter-example, and the one to know about. They are the singular
and plural of the same noun and they stem to **different keys** — `geraçã` and `geraçõ` — because
the nasal `ã`/`õ` distinction survives the rules that would otherwise have merged them. A search
for one will not find the other. `amável` and `amáveis` split the same way.

This is faithful to Snowball and to `nltk`, not a defect here, but it means Portuguese recall is
not uniform across a vocabulary. Where it matters, folding `ã`/`õ` together before indexing is the
caller's decision to make, and it changes the keys on both sides of the search.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`PortugueseSnowballStemmer`](portuguesesnowballstemmer.md),
[`SpanishSnowballStemmer.Stem`](spanishsnowballstemmer-stem.md),
[the stemming index](../stemming.md).
