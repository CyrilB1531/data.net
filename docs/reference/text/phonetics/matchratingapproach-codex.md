# MatchRatingApproach.Codex

The Match Rating codex of one name.

<!-- docs-declaration -->

```csharp
public static string Codex(string value)
public static string Codex(ReadOnlySpan<char> value)
```

**Parameters** — `value` is a single name: any Unicode letter plus a single space is accepted; a
digit, punctuation or any other whitespace is refused rather than ignored. Case does not matter.
The `string` overload forwards to the span one.

**Returns** — `string`, an uppercase code of 1 to 6 characters, or the empty string when `value` is
empty.

**Exceptions** — `ArgumentNullException` when `value` is `null` (the `string` overload only).
`ArgumentException` when `value` holds a character that is neither a letter nor a space.

**Example** — doubles collapse before non-leading vowels are dropped, and a code over six
characters keeps only its first three and last three.

```csharp
using Lodestar.Text.Phonetics;

string smith = MatchRatingApproach.Codex("Smith");             // => SMTH
string mississippi = MatchRatingApproach.Codex("Mississippi"); // => MSSP
string bhattacharya = MatchRatingApproach.Codex("Bhattacharya");   // => BHTHRY
```

**Remarks** — `Mississippi` reducing to `MSSP` is doubled letters collapsing *before* the
non-leading vowels between them are dropped: the raw sequence `M-I-S-S-I-S-S-I-P-P-I` collapses
its adjacent-letter runs first (`M-I-S-I-S-I-P-I`), and only then loses every vowel but the first
character, whatever it is — a leading vowel is kept, as in `Codex("aeiou")` → `"A"`.

`Bhattacharya` reducing to six characters (`BHTCHRY` would be seven) keeps its first three and its
last three: `BHT` + `HRY`. A name that already fits in six is never touched, however it was spelled.

A character that is neither a letter nor a space throws instead of being dropped —
`Codex("O'Brien")` and `Codex("Anne-Marie")` both raise `ArgumentException`, naming the character
that stopped them. [`Soundex.Encode`](soundex-encode.md), [`Metaphone.Encode`](metaphone-encode.md)
and [`Nysiis.Encode`](nysiis-encode.md) ignore the same input instead — a deliberate difference,
recorded in [the phonetics index](../phonetics.md) and the
[equivalence table](../../../equivalence.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — [`MatchRatingApproach`](matchratingapproach.md),
[`MatchRatingApproach.Compare`](matchratingapproach-compare.md),
[the phonetics index](../phonetics.md).
