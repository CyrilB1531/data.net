# Soundex

American Soundex: an initial letter followed by three digits.

<!-- docs-declaration -->

```csharp
public static class Soundex
```

**Example** — the collision the algorithm is famous for.

```csharp
using Lodestar.Text.Phonetics;

string robert = Soundex.Encode("Robert");  // => R163
string rupert = Soundex.Encode("Rupert");  // => R163
```

**Remarks** — the oldest of the three and the one most likely to be what an existing system
already uses. It keeps the first letter, then maps the rest to digits by articulation —
`bfpv`→`1`, `cgjkqsxz`→`2`, `dt`→`3`, `l`→`4`, `mn`→`5`, `r`→`6` — dropping vowels, and pads or
truncates to exactly four characters.

Four characters is why it merges so much: over the 402-word corpus it pins,
[101 words share a code](../phonetics.md) with at least one other. That is the point of it —
recall first — and the reason not to reach for it when a wrong match is expensive.

Reference behaviour is `jellyfish.soundex`, matched over 402 words.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Nysiis`](nysiis.md), [`Metaphone`](metaphone.md),
[the phonetics index](../phonetics.md).

## Members

| Member | What it does |
| --- | --- |
| [`Soundex.Encode`](soundex-encode.md) | The 4-character Soundex code of one word. |
