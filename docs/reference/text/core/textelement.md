# TextElement

Whether one character means a UTF-16 code unit or a Unicode code point.

<!-- docs-declaration -->

```csharp
public enum TextElement { Utf16Unit, CodePoint }
```

**Members** — `Utf16Unit` treats each `char` of the string as one element. It is the default, it is
what .NET's own string operations do, and it is the faster of the two because no decoding happens.
`CodePoint` decodes surrogate pairs first, so a character outside the Basic Multilingual Plane
counts as one element rather than two — which is what Python's `len` counts, and what
`textdistance` and `jellyfish` compare.

**Example** — one emoji between two letters, measured both ways.

```csharp
using Lodestar.Text;
using Lodestar.Text.Distances;

// "a😀b" against "ab": the emoji is one code point and two UTF-16 units.
string withEmoji = "a\U0001F600b";

int units = Levenshtein.Distance(withEmoji, "ab");                        // => 2
int points = Levenshtein.Distance(withEmoji, "ab", TextElement.CodePoint); // => 1
```

**Remarks** — the two numbers are both correct answers to different questions. Deleting the emoji
is **one** edit a person would make and **two** `char` values a program removes, and which one you
want depends on whether the distance will be shown to someone or fed to something.

Reach for `CodePoint` when the answer is compared against Python's, when it is shown to a user as
"how many changes", or when the text is user-supplied and may hold emoji. Leave the default when
the text is ASCII or near it — the two agree exactly there, and the decoding is not free.

The choice is per call, not per process, so one comparison can use each. Nothing about it is
global state.

It does **not** reach graphemes. A family emoji built from several code points joined by
zero-width joiners counts as several elements under `CodePoint`, not one, and no mode here counts
it as one — that is a third answer this enum does not offer.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Levenshtein.Distance`](../distances/levenshtein-distance.md),
[`Hamming.Distance`](../distances/hamming-distance.md), [the distances index](../distances.md),
the [Python equivalence table](../../../equivalence.md).
