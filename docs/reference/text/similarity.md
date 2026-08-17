# Set similarity — `Lodestar.Text.Similarity`

How much do two pieces of text have **in common**, when where it appears does not matter? Every
type on this page answers that. They cut each input into q-grams, count them as a bag, and divide
the grams the two share by something — and the something is the only place they disagree.

Two conventions run through the whole namespace, and knowing them saves reading every entry.

- A q-gram is a run of `qval` consecutive characters, and `qval` is `1` by default, which makes the
  grams individual characters. Repeats count: `"apple"` holds `p` **twice**, and a bag that holds it
  once shares only one of them. This is `textdistance`'s reading, and the reason a caller who wants
  words rather than characters has to split and compare the pieces themselves.
- Every member takes a `TextElement` saying what counts as one character. The default,
  `TextElement.Utf16Unit`, is .NET's own unit and agrees with Python for every character in the
  Basic Multilingual Plane; outside it — emoji, rare ideographs — one character is two UTF-16 units
  and the two disagree on purpose. Pass `TextElement.CodePoint` for Python's answer. The reasoning
  is in [decision 0002](../../decisions/0002-unicode-comparison-unit.md).

Comparing text **position by position** — how many edits turn one string into the other, whether
two names are spelled alike — is a different question, answered by
[`Lodestar.Text.Distances`](distances.md) and not by anything here.

## One numerator, five denominators

Every measure on this page divides `|A∩B|`, the grams the two bags share, by a denominator of its
own. That is the entire difference between them, and it is enough to predict which will disagree
with which.

| Type | Denominator | What that makes it do |
| --- | --- | --- |
| [`Jaccard`](similarity/jaccard.md) | `\|A∪B\|` | Charges for every gram either side holds alone. The strictest of the five. |
| [`SorensenDice`](similarity/sorensendice.md) | `(\|A\| + \|B\|) / 2` | Counts shared grams twice, so it always reads higher than `Jaccard`. |
| [`Overlap`](similarity/overlap.md) | `min(\|A\|, \|B\|)` | Ignores the size gap entirely: `1` whenever one bag is contained in the other. |
| [`Cosine`](similarity/cosine.md) | `√(\|A\| · \|B\|)` | Between the other three — the geometric mean punishes a size gap, but gently. |
| [`Tversky`](similarity/tversky.md) | `\|A∩B\| + α·\|A\B\| + β·\|B\A\|` | The general form the others are cases of, and the only asymmetric one. |

Two consequences are worth having before you choose.

**`Jaccard` and `SorensenDice` rank identically.** `Dice = 2·Jaccard / (1 + Jaccard)`, which rises
with `Jaccard` over the whole of `[0, 1]`, so sorting candidates by one produces the order the
other would. Choosing between them changes the number a threshold has to be set against, never
which match wins.

**`Tversky` is the other four in disguise.** `α = β = 1` is `Jaccard`, `α = β = 0.5` is
`SorensenDice`, and the asymmetric settings are what the other four cannot express: `α = 1, β = 0`
charges only for what the *first* input holds alone, which asks "is A contained in B" rather than
"do A and B agree".

## Which one do I want?

```mermaid
flowchart TD
    A["Comparing two bags of grams"] --> B{"Are the two roughly<br/>the same length?"}
    B -->|yes| C["Any of them agree closely.<br/>Jaccard is the usual default"]
    B -->|no| D{"Is the shorter one supposed to be<br/>a fragment of the longer?"}
    D -->|yes, and a fragment<br/>should score full marks| E["Overlap"]
    D -->|yes, but the extra material<br/>should still cost something| F["Cosine"]
    D -->|no, the gap is real<br/>disagreement| G{"Do the two sides deserve<br/>the same penalty?"}
    G -->|yes| H["Jaccard, or SorensenDice<br/>for a gentler number"]
    G -->|no — one direction matters| I["Tversky, with α ≠ β"]
```

| Type | What it measures |
| --- | --- |
| [`Cosine`](similarity/cosine.md) | Shared grams over the geometric mean of the two bag sizes — the Ochiai coefficient. |
| [`Jaccard`](similarity/jaccard.md) | Shared grams over the grams either side holds at all. |
| [`Overlap`](similarity/overlap.md) | Shared grams over the smaller bag, so containment scores `1`. |
| [`SorensenDice`](similarity/sorensendice.md) | Shared grams counted twice, over the two bag sizes added. |
| [`Tversky`](similarity/tversky.md) | Shared grams against the two sides' surpluses, weighted separately. |

## What every one of them does with an empty input

Two empty inputs share nothing and disagree about nothing, and all five answer `1` — the reference
libraries' choice, kept here so a ported comparison does not change at the boundary. One empty
input against a non-empty one answers `0` everywhere except [`Tversky`](similarity/tversky.md),
whose weights can make the denominator vanish; its entry says when.

**See also** — [distances](distances.md) for position-sensitive comparison, the
[Python equivalence table](../../equivalence.md), and
[`Lodestar.Fuzzy`](../fuzzy/matching.md) for scorers that do the token splitting for you.
