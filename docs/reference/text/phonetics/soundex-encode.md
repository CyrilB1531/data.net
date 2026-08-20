# Soundex.Encode

The 4-character Soundex code of one word.

<!-- docs-declaration -->

```csharp
public static string Encode(ReadOnlySpan<char> value)
public static string Encode(string value)
```

**Parameters** — `value` is a single word. Non-letters in it are ignored rather than rejected, and
case does not matter. The `string` overload forwards to the span one, so passing a `string`
allocates nothing extra.

**Returns** — `string`: an uppercase letter followed by three digits, always exactly four
characters — or the empty string when `value` holds no letter at all.

**Exceptions** — `ArgumentNullException` when `value` is `null` (the `string` overload only; a
`ReadOnlySpan<char>` cannot be null). An empty string is accepted and encodes to the empty string.

**Example** — a collision, and a word with too few consonants to fill the code.

```csharp
using Lodestar.Text.Phonetics;

string robert = Soundex.Encode("Robert");  // => R163
string rupert = Soundex.Encode("Rupert");  // => R163
string padded = Soundex.Encode("Lee");  // => L000
```

**Remarks** — `L000` is the padding rule doing its work: `Lee` has one codeable consonant and the
code is still four characters wide, because a fixed width is what lets Soundex be an index key.

A `null` word is refused, the same rule the stemmers in
[`Lodestar.Text.Stemming`](../stemming.md) apply — [decision 0042](../../../decisions/0042-phonetic-encoders-refuse-a-null-word.md)
records why the two used to disagree and why refusing won.

The two overloads are the same algorithm; the span one exists so a word already sliced out of a
larger buffer can be encoded without copying it.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Soundex`](soundex.md), [`Nysiis.Encode`](nysiis-encode.md),
[the phonetics index](../phonetics.md).
