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

**Exceptions** — none. This method does not throw, and that includes `null`, which is treated as
an empty input and encodes to the empty string.

**Example** — a collision, a word with too few consonants to fill the code, and the `null` case.

```csharp
using Lodestar.Text.Phonetics;

string robert = Soundex.Encode("Robert");  // => R163
string rupert = Soundex.Encode("Rupert");  // => R163
string padded = Soundex.Encode("Lee");  // => L000

string missing = null!;
int empty = Soundex.Encode(missing).Length;  // => 0
```

**Remarks** — `L000` is the padding rule doing its work: `Lee` has one codeable consonant and the
code is still four characters wide, because a fixed width is what lets Soundex be an index key.

That `null` returns `""` rather than throwing is worth knowing, because it is the opposite of the
stemmers in [`Lodestar.Text.Stemming`](../stemming.md), which refuse a `null` word. Here a missing
name silently becomes a code that matches every other missing name — check for it before encoding
if that would be wrong.

The two overloads are the same algorithm; the span one exists so a word already sliced out of a
larger buffer can be encoded without copying it.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Soundex`](soundex.md), [`Nysiis.Encode`](nysiis-encode.md),
[the phonetics index](../phonetics.md).
