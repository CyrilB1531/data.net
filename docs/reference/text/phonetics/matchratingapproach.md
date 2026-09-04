# MatchRatingApproach

The Match Rating Approach (Western Airlines, 1977) — a codex like the other three, plus the rule
for deciding whether two codices name a match.

<!-- docs-declaration -->

```csharp
public static class MatchRatingApproach
```

**Example** — the pair from the 1977 description itself, encoded and then compared.

```csharp
using Lodestar.Text.Phonetics;

string byrne = MatchRatingApproach.Codex("Byrne");   // => BYRN
string boern = MatchRatingApproach.Codex("Boern");   // => BRN
bool? match = MatchRatingApproach.Compare("Byrne", "Boern");   // => True
```

**Remarks** — the odd one out in this namespace on two counts. First, `Compare` answers a question
the other three do not: not "what does this sound like" but "do these two sound alike", and its
answer depends on **both** codices at once — their combined length picks a stricter or looser
threshold from a fixed table, so a caller holding two codices already still needs `Compare` rather
than comparing them by hand. Second, its accepted alphabet is narrower and stricter: any Unicode
letter plus a single space is accepted, and anything else — a digit, an apostrophe, a hyphen — is
**refused** with `ArgumentException`, where [`Soundex`](soundex.md), [`Metaphone`](metaphone.md)
and [`Nysiis`](nysiis.md) silently ignore it. The published algorithm has no rule for what a digit
sounds like, and refusing says so rather than guessing.

Reference behaviour is `jellyfish.match_rating_codex` and `jellyfish.match_rating_comparison`,
matched over 420 words and 212 pairs — except where codex length is measured in UTF-8 bytes rather
than characters, which [decision 0080](../../../decisions/0080-match-rating-approach-comparison-uses-character-length-not-byte-length.md)
does not reproduce.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Soundex`](soundex.md), [`Metaphone`](metaphone.md), [`Nysiis`](nysiis.md),
[the phonetics index](../phonetics.md).

## Members

| Member | What it does |
| --- | --- |
| [`MatchRatingApproach.Codex`](matchratingapproach-codex.md) | The Match Rating codex of one name. |
| [`MatchRatingApproach.Compare`](matchratingapproach-compare.md) | Whether two names' codices rate as a match. |
