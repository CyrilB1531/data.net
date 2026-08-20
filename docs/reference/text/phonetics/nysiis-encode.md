# Nysiis.Encode

The NYSIIS code of one name.

<!-- docs-declaration -->

```csharp
public static string Encode(ReadOnlySpan<char> value)
public static string Encode(string value)
```

**Parameters** — `value` is a single name. Non-letters in it are ignored rather than rejected, and
case does not matter. The `string` overload forwards to the span one.

**Returns** — `string`, an uppercase letter code of variable length — 1 to 11 characters over the
corpus it is pinned to — or the empty string when `value` holds no letter.

**Exceptions** — `ArgumentNullException` when `value` is `null` (the `string` overload only; a
`ReadOnlySpan<char>` cannot be null). An empty string is accepted and encodes to the empty string.

**Example** — two name-specific opening rules, and the `b`/`p` distinction Soundex loses.

```csharp
using Lodestar.Text.Phonetics;

string pf = Nysiis.Encode("Pfister");  // => FASTAR
string sch = Nysiis.Encode("Schmidt");  // => SNAD
string robert = Nysiis.Encode("Robert");  // => RABAD
string rupert = Nysiis.Encode("Rupert");  // => RAPAD
```

**Remarks** — `Pfister` → `FASTAR` is NYSIIS reading an initial `PF` as the `f` sound it is in a
German-derived surname, which is the kind of rule it carries and the reason it is described as a
name encoder rather than a word encoder.

`Robert` and `Rupert` landing on `RABAD` and `RAPAD` is the counterpart to
[`Soundex.Encode`](soundex-encode.md) giving both `R163`. Neither is wrong — they are answering
different questions, and which one you want depends on whether a false match or a missed match is
the expensive mistake.

Because the code is not truncated, its length tracks the length of the name. Two names of very
different lengths will not collide here even when they start alike, which is most of where the
precision comes from.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Nysiis`](nysiis.md), [`Soundex.Encode`](soundex-encode.md),
[the phonetics index](../phonetics.md).
