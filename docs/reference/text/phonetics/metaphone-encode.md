# Metaphone.Encode

The Metaphone code of one word.

<!-- docs-declaration -->

```csharp
public static string Encode(ReadOnlySpan<char> value)
public static string Encode(string value)
```

**Parameters** — `value` is a single word. Non-letters in it are ignored rather than rejected, and
case does not matter. The `string` overload forwards to the span one.

**Returns** — `string`, a variable-length code drawn from `B X S K J T F H L M N P R 0 W Y`, or
the empty string when `value` holds no letter.

**Exceptions** — none. This method does not throw, and `null` encodes to the empty string.

**Example** — the digraphs that make the alphabet look strange.

```csharp
using Lodestar.Text.Phonetics;

string th = Metaphone.Encode("Thomas");  // => 0MS
string sh = Metaphone.Encode("Christina");  // => XRSTN
string silent = Metaphone.Encode("Knight");  // => NT
```

**Remarks** — `0` and `X` are not placeholders. `0` is the "th" sound and `X` the "sh" sound, both
written as single characters so a code stays one character per sound. This is why a Metaphone code
must never be shown to a user: `0MS` is correct and unreadable.

`Knight` → `NT` is the behaviour that distinguishes this from the other two encoders, and it is
also the reason to prefer it for ordinary English words over `Soundex.Encode`. Silent `k`, `w`,
`g` and `b` are all handled, along with `GH`, `DGE`, `-TION` and the rest of the spellings English
uses for sounds it does not write plainly.

**Parity is claimed on real words only.** The shared 402-word corpus contains random letter
sequences, and on those jellyfish behaves in ways specific to its C implementation;
[decision 0007](../../../decisions/0007-metaphone-scope.md) is the reasoning, and the practical
form of it is that a code for `xhdzhumzj` is not something this package promises to match.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Metaphone`](metaphone.md), [`Soundex.Encode`](soundex-encode.md),
[the phonetics index](../phonetics.md).
