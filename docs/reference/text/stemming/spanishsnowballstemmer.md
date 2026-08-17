# SpanishSnowballStemmer

Spanish stemming by the Snowball algorithm.

<!-- docs-declaration -->

```csharp
public static class SpanishSnowballStemmer
```

**Example** — a noun's four forms on one key.

```csharp
using Lodestar.Text.Stemming;

string masculine = SpanishSnowballStemmer.Stem("musico");  // => music
string feminine = SpanishSnowballStemmer.Stem("musica");  // => music
string plural = SpanishSnowballStemmer.Stem("musicos");  // => music
```

**Remarks** — Spanish attaches object pronouns to the end of a verb — `dámelo`, `cantándome` —
which would leave a suffix stemmer trimming the pronoun's ending instead of the verb's. The
algorithm therefore opens with a **step 0** that removes those attached pronouns before any
ordinary suffix rule runs. It is the one structural difference from the French and Italian
stemmers.

**Accents are stripped last**, after the rules. `dámelo` and `damelo` both reach `damel`, so a
corpus that omits accents — as plenty of real Spanish text does — indexes to the same keys as one
that keeps them.

Verb conjugation is where the collapse is largest: the present, preterite, imperfect, conditional
and subjunctive forms of `cantar` all reduce to `cant`.

Reference behaviour is `nltk.stem.snowball.SnowballStemmer("spanish")`, matched over 127 words.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`PortugueseSnowballStemmer`](portuguesesnowballstemmer.md), its nearest neighbour,
and [the stemming index](../stemming.md).

## Members

| Member | What it does |
| --- | --- |
| [`SpanishSnowballStemmer.Stem`](spanishsnowballstemmer-stem.md) | The Snowball stem of one Spanish word. |
