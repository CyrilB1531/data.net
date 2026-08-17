# Nysiis

NYSIIS — the New York State Identification and Intelligence System encoding, built for names.

<!-- docs-declaration -->

```csharp
public static class Nysiis
```

**Example** — the pair Soundex cannot separate, separated.

```csharp
using Lodestar.Text.Phonetics;

string robert = Nysiis.Encode("Robert");  // => RABAD
string rupert = Nysiis.Encode("Rupert");  // => RAPAD
```

**Remarks** — the finest of the three. Over the 402-word corpus all three are pinned to, only
[13 words share a code](../phonetics.md) with another — against 101 under
[`Soundex`](soundex.md). It keeps letters rather than digits and keeps most of the word, so two
names have to be genuinely close before it calls them equal.

That makes it the right default when a **wrong** match costs more than a missed one: deduplicating
a customer list, say, where merging two real people is worse than leaving a duplicate.

This is the **modern, non-truncated variant** — the original NYSIIS capped the code at six
characters, and that cap is not applied here, which is why a code can run to eleven.

Reference behaviour is `jellyfish.nysiis`, matched over 402 words.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Soundex`](soundex.md), [`Metaphone`](metaphone.md),
[the phonetics index](../phonetics.md).

## Members

| Member | What it does |
| --- | --- |
| [`Nysiis.Encode`](nysiis-encode.md) | The NYSIIS code of one name. |
