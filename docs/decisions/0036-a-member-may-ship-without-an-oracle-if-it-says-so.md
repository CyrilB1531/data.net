# 0036 — A member may ship without an oracle, if the documentation says so

**Status:** accepted · **Date:** 2026-08-16

## Context

Every number `Lodestar.Metrics` returns is proven the same way: a frozen corpus captured from the
canonical Python library and replayed at `1e-9`. That rule is what the package's parity claim rests
on, and it has held for all 37 members shipped so far.

Mean reciprocal rank has no such reference. Measured on scikit-learn 1.9.0, `dir(sklearn.metrics)`
carries nothing matching `reciprocal`: the function does not exist, so there is nothing to freeze.
It is nevertheless one of the three numbers an information-retrieval reader expects beside NDCG,
and [#173](https://github.com/CyrilB1531/lodestar/issues/173) asked for it explicitly.

The choice is not "ship it or not". It is whether the parity rule admits an exception at all, and
what an exception has to carry to be admissible.

## Decision

**A member may ship without an oracle when three things are true, and it must carry all three.**

1. **No reference exists to freeze.** Not "the reference is inconvenient", not "the corpus would be
   large" — the canonical library does not implement it. If one appears later, the exception is
   retired and the member joins the corpus.
2. **Its definition is pinned by tests that state each choice.** A metric without a reference has
   variants, and the variant chosen is a decision. For `ReciprocalRank`: the reciprocal of the rank
   of the first relevant document, averaged over queries, a query with no relevant document
   contributing `0`. Each of those three clauses gets a test, so a later change to the definition
   fails rather than drifts.
3. **The documentation says it is not verified against a reference** — on the member's reference
   page and in its `docs/equivalence.md` row, where the Python column reads "no counterpart"
   rather than a function name. A reader must learn this from the documentation, never from a
   surprise.

## Consequences

- **`ReciprocalRank` ships under this rule, and is the only member that does.** The count is the
  measure: if a second member ever cites this ADR, that is the moment to ask whether the rule has
  become a habit.
- **The equivalence table gains a shape it did not have** — a row whose Python side is empty on
  purpose. That is more honest than omitting the row, which would leave a reader wondering whether
  the mapping was forgotten.
- **The exception is retirable, and the condition is written down.** Should scikit-learn (or another
  library the project already trusts) implement MRR, the member gets a corpus like every other and
  this ADR is superseded rather than quietly outgrown.

## The alternative, and why it lost

Leaving MRR out entirely was the first recommendation, and it has a real argument: everything the
package exposes is provable, and a number that is not weakens the promise for the rest. It lost
because the promise is not "every number is oracle-backed" but "you can tell which ones are" — and
the documentation can carry that distinction where silence cannot. A reader who needs MRR and does
not find it here does not conclude that the project is rigorous; they conclude it is incomplete, and
they compute it themselves without any of the three protections above.
