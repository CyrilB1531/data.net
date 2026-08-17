# PortugueseSnowballStemmer

Portuguese stemming by the Snowball algorithm.

<!-- docs-declaration -->

```csharp
public static class PortugueseSnowballStemmer
```

**Example** — a noun's four forms on one key.

```csharp
using Lodestar.Text.Stemming;

string masculine = PortugueseSnowballStemmer.Stem("musico");  // => music
string feminine = PortugueseSnowballStemmer.Stem("musica");  // => music
string plural = PortugueseSnowballStemmer.Stem("musicas");  // => music
```

**Remarks** — Portuguese and Spanish look alike and their stemmers are not interchangeable. Two
differences decide it.

**Nasal vowels are expanded before the rules run**: `ã` becomes `a~` and `õ` becomes `o~`, two
characters where there was one. This keeps the nasal from being read as a plain vowel by a suffix
rule, and it is undone before the stem is returned.

**Accents are kept**, where the Spanish algorithm strips them at the end — 19 of the 105 pinned
words stem to something still carrying one. So a corpus that drops accents will not index to the
same keys as one that keeps them, the opposite of the Spanish situation and the thing to check
before mixing sources. It also means a nasal can outlive the rules that would have merged two
forms: [`Stem`](portuguesesnowballstemmer-stem.md) has the `geração`/`gerações` case, where a
singular and its plural end on different keys.

Reference behaviour is `nltk.stem.snowball.SnowballStemmer("portuguese")`, matched over 105 words.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SpanishSnowballStemmer`](spanishsnowballstemmer.md), the neighbour it is easy to
reach for by mistake, and [the stemming index](../stemming.md).

## Members

| Member | What it does |
| --- | --- |
| [`PortugueseSnowballStemmer.Stem`](portuguesesnowballstemmer-stem.md) | The Snowball stem of one Portuguese word. |
