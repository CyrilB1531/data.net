# MatchRatingApproach.Compare

Whether two names' codices rate as a match.

<!-- docs-declaration -->

```csharp
public static bool? Compare(string a, string b)
public static bool? Compare(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
```

**Parameters** — `a` and `b` are the two names to compare, under the same rules
[`Codex`](matchratingapproach-codex.md) applies to a single one: any Unicode letter plus a single
space is accepted, and anything else is refused. The `string` overload forwards to the span one.

**Returns** — `bool?`. `true` when the two codices rate as a match, `false` when they do not, and
`null` — not `false` — when their lengths differ by 3 or more characters, which the algorithm
declares too far apart to rate at all.

**Exceptions** — `ArgumentNullException` when `a` or `b` is `null` (the `string` overload only).
`ArgumentException` when `a` or `b` holds a character that is neither a letter nor a space.

**Example** — the 1977 description's own pair, and the length gap that makes a rating impossible.

```csharp
using Lodestar.Text.Phonetics;

bool? byrneBoern = MatchRatingApproach.Compare("Byrne", "Boern");   // => True
bool? timTimothy = MatchRatingApproach.Compare("Tim", "Timothy");   // => null
```

**Remarks** — `Compare` recomputes both codices itself; a caller already holding two from
[`Codex`](matchratingapproach-codex.md) still calls `Compare`; there is no way to reach the same
answer from the codices alone, because the minimum rating a comparison must clear is read from a
table keyed by their **combined** length:

| combined codex length | minimum rating (out of 6) |
| --- | --- |
| 4 or fewer | 5 |
| 5 to 7 | 4 |
| 8 to 11 | 3 |
| 12 or more | 2 |

`Byrne`/`Boern` codes to `BYRN`/`BRN`, a combined length of 7 and a minimum rating of 4; cancelling
shared characters from the start and then the end of what is left rates the pair at 5, which
clears it. `Tim`/`Timothy` codes to `TM`/`TMTHY`, 2 and 5 characters apart — a gap of 3, so the
comparison returns `null` before the table is even consulted.

That table, and the length it is keyed by, were measured directly against `jellyfish` 1.2.1 by
bisection rather than assumed from a textbook — see
[decision 0079](../../../decisions/0079-match-rating-approach-comparison-uses-character-length-not-byte-length.md)
for why the length is counted in **characters**, where jellyfish counts UTF-8 bytes for a handful
of non-Latin inputs, and the two cases that changes.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`MatchRatingApproach`](matchratingapproach.md),
[`MatchRatingApproach.Codex`](matchratingapproach-codex.md), [the phonetics index](../phonetics.md).
