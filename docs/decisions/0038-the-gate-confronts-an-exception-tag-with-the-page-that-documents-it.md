# 0038 — The gate confronts an exception tag with the page that documents it

**Status:** accepted · **Date:** 2026-08-18

## Context

The prose describing a member exists twice. `src/` carries an XML documentation comment, which
feeds IntelliSense and the emitted `.xml`; `docs/reference/` carries a page, which is published to
the wiki and is what a reader outside an IDE actually reads. Until now nothing confronted the two.

[#217](https://github.com/CyrilB1531/lodestar/issues/217) is what that costs.
[`TopKAccuracy.Score`](../reference/metrics/ranking/topkaccuracy-score.md)
tagged `ArgumentException` where it throws `ArgumentOutOfRangeException`; the page had the correct
name from its first commit. [#222](https://github.com/CyrilB1531/lodestar/pull/222) fixed the
docstring in one file. The divergence was found by reading, not by a gate — and it could as easily
have fallen the other way, leaving the *published* page naming a type a reader would then fail to
catch.

The exception set is the one part of that prose with a machine-readable counterpart on both sides:
a `<exception cref="…">` tag against an **Exceptions** rubric. Summary, Returns and Remarks have no
such shape, and the `// =>` assertions on executed fences check a value rather than a sentence.

Replaying the confrontation over `main` before deciding measured the gap: **195 members carry both
a tag site and a page entry, and 75 of them disagree** — 51 where the page documents an exception
the docstring never tags, 5 the reverse, 19 where both are non-empty and unequal. `TopKAccuracy`
itself agrees, so #222 really did close it, and everything found is new.

## Decision

**The reference gate compares, per member, the set of exception types its documentation comment
tags against the set its page's Exceptions block names, and complains when the two differ.**

Four things fix the shape:

1. **The scope is `<exception>` and nothing else.** Not summary, not remarks, not returns. Those
   have no machine-readable counterpart and comparing them would mean comparing sentences.
   Extending to any of them is a later decision and a later ADR, not a quiet widening of this one.
2. **Types are compared, never the sentence around them.** The gate asserts *which* exceptions a
   member is documented to throw, and says nothing about *when* — which is where the interesting
   divergences live, and which stays a review obligation. A gate that claimed more than it checks
   would be worse than none.
3. **Sets, not sequences.** Measured on #217's own member: the page lists
   `ArgumentOutOfRangeException` before `ArgumentException`, the docstring the reverse, and both
   are correct. An order rule would fail the one member the issue holds up as fixed.
4. **Enforced by default, exempted by namespace.** `docs/wiki-map.json` gains an
   `exceptionsUnchecked` list per package. A namespace absent from it is checked; the 70 members
   still owed are held in that list, by the namespace key `covered` already uses, and burnt down by
   [#266](https://github.com/CyrilB1531/lodestar/issues/266).

The authority over both copies remains the `throw` in the body, which this gate does not read. What
it guarantees is that the two written copies cannot disagree with *each other* unnoticed — so one
reviewer checking one of them against the code checks both.

## Consequences

- **`Lodestar.Fuzzy`, `Lodestar.Metrics`, `Lodestar.Text.Distances`, `Lodestar.Text.Phonetics` and
  `Lodestar.Text.Persistence` are enforced from this change**, 92 members. `Lodestar.Metrics` needed
  five docstrings completed to get there —
  [`F1.PerClass`](../reference/metrics/classification/f1-perclass.md),
  [`Precision.PerClass`](../reference/metrics/classification/precision-perclass.md),
  [`Recall.PerClass`](../reference/metrics/classification/recall-perclass.md),
  [`FBeta.Score`](../reference/metrics/classification/fbeta-score.md) and
  [`FBeta.PerClass`](../reference/metrics/classification/fbeta-perclass.md), each of which threw an
  exception its page documented and its tags omitted. That puts `TopKAccuracy` under the gate,
  which is the point of the exercise.
- **The debt is one shrinking list rather than a marker on each page.** Seventy-five opt-out
  markers is how ADR 0036's exception becomes a habit; eight namespace names in the map is a number
  a reader can see going down. The direction matters as much as the shape: enforced-by-default
  means a namespace added later is covered without anyone remembering to opt in.
- **A missing documentation file is a complaint, not a silence.** The gate reads the `.xml` beside
  the assembly, so it sees the same build reflection does — the netstandard2.0 suite reads that
  target's tags. If the file is absent the gate says so rather than passing every page without
  reading one.
- **`<inheritdoc/>` contributes nothing.** The compiler emits it verbatim, so a member documented
  only that way reads as tagging no exception. One member does it today
  ([`PrecompiledNormalizer.Normalize`](../reference/embeddings/tokenization/precompilednormalizer-normalize.md),
  in an unchecked namespace) and a sibling overload covers it
  through the union taken over the group. If that stops being true, the fix is to resolve
  `<inheritdoc/>` in the gate, not to loosen the comparison.
- **A new exception owes two edits, and CI says so.** Adding a `throw` now means the tag and the
  page, in the same commit — the rule `docs/equivalence.md` rows already follow.

## The options, and why they lost

**Record the boundary instead of gating it** — state that the page is the published contract, the
XML comment serves IntelliSense, and agreement is a review obligation. It costs nothing and it was
seriously in play. It lost on the measurement: 75 divergences is not what a working review
obligation looks like, and #217 was caught by an audit rather than by review. An obligation nobody
can keep is a boundary being recorded around a gap.

**Derive the page's Exceptions block from the XML tag** — remove the second copy rather than police
it. This is the better end state and it is not this change. It presumes the tag is the authority,
which the measurement contradicts in 51 of 75 cases: the page is right and the docstring is silent.
It also cuts across #189's page shape, where the block is prose a human wrote for a human. Worth
reopening once the two sides agree, which is the state this gate produces.

**A frozen baseline of the 75 members** — the usual ratchet. It admits new debt in one member
without admitting it in a namespace, which is finer, but it puts the list somewhere nobody reads
and it makes "add a line to the baseline" the cheapest way past a red build. The namespace list
lives in the map a contributor already opens to add a page.
