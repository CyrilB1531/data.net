# Core — `Lodestar.Text`

The `Lodestar.Text` namespace itself holds one type, and it is one every other namespace in the
package takes as a parameter: [`TextElement`](core/textelement.md), which says what counts as **one
character**.

That question has two answers in .NET and they disagree wherever text leaves the Basic
Multilingual Plane — an emoji, an ancient script, a rare CJK ideograph. A `string` is a sequence of
UTF-16 code units, and such a character occupies **two** of them. Treating those two as separate
characters is fast and is what .NET does by default; treating them as one is what Python does, and
what a user comparing two names would expect.

Every distance in [`Lodestar.Text.Distances`](distances.md) takes this as its last parameter, so
the choice is made per call rather than per process.

## Types

| Type | What it is |
| --- | --- |
| [`TextElement`](core/textelement.md) | Whether one character means a UTF-16 unit or a code point. |

## See also

- [Distances](distances.md) — every member that takes a `TextElement`.
- [Python → C# equivalence](../../equivalence.md) — where the code-point mode is what matches.
