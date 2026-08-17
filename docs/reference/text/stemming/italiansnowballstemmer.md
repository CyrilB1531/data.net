# ItalianSnowballStemmer

Italian stemming by the Snowball algorithm.

<!-- docs-declaration -->

```csharp
public static class ItalianSnowballStemmer
```

**Example** — the four endings of one noun.

```csharp
using Lodestar.Text.Stemming;

string masculine = ItalianSnowballStemmer.Stem("musico");  // => music
string feminine = ItalianSnowballStemmer.Stem("musica");  // => music
string plural = ItalianSnowballStemmer.Stem("musiche");  // => music
```

**Remarks** — two preparations run before the suffix rules, and both change what matches.
**Acute accents are folded to grave** (`á` → `à` and so on), so the two ways Italian text writes a
stressed vowel cannot produce two keys — `attività` and `attivita` both reach `attiv`. Then `u`
and `i` **between two vowels are marked** as consonants, which is what they are in Italian
phonology and what several suffix rules depend on.

This stemmer carries the one deliberate divergence in the namespace. The published Snowball
description replaces the suffix `enza`/`enze` with `ente`; `nltk` replaces it with `te`, and this
implementation follows `nltk`, so `esistenza` stems to `esistt` rather than `esistent`.
[`decisions/0008`](../../../decisions/0008-italian-enza-nltk-divergence.md) records why matching
the library everyone actually compares against won over matching the text.

Reference behaviour is `nltk.stem.snowball.SnowballStemmer("italian")`, matched over 96 words.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`FrenchSnowballStemmer`](frenchsnowballstemmer.md) and
[`SpanishSnowballStemmer`](spanishsnowballstemmer.md), which share the region scheme, and
[the stemming index](../stemming.md).

## Members

| Member | What it does |
| --- | --- |
| [`ItalianSnowballStemmer.Stem`](italiansnowballstemmer-stem.md) | The Snowball stem of one Italian word. |
