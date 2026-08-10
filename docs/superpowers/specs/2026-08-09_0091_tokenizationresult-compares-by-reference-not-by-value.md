# Design — #91: `TokenizationResult` compares by reference

**Date:** 2026-08-09 · **Issue:** #91 · **Branch:** landed without its own pull request ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

`TokenizationResult` is a `record` over two `IReadOnlyList<T>` members. The
synthesised `Equals` compares those members **by reference**, so two tokenizations
holding identical tokens and ids are not equal — **the opposite of what a `record`
advertises**.

The `record` keyword is a promise about equality. Declaring one over reference-typed
collections quietly breaks that promise, and the type is public API.

## How it surfaced

While designing `DataNet.Metrics`, where `ClassificationReport` was made a plain
class **for exactly this reason**. One type had already been shaped around the
problem before the other was recognised as having it.

That is worth recording: the same trap was met twice, and only the second time was
it named.

## Decisions

### D1 — Keep the `record`, implement structural equality explicitly

Two options were open: implement `Equals` by hand, or demote it to a class so
nothing is promised.

`record` is kept, with an explicit `Equals(TokenizationResult?)` comparing tokens
and ids **element by element**. The declaration then matches what the type
already claims, rather than retreating from the claim.

Demoting to a class would also be a **breaking change** for any caller relying on
`with` or on the deconstructor.

### D2 — The reason goes on the member, not in a commit

The XML documentation states why the generated equality is overridden: it would
compare `Tokens` and `Ids` by reference, so two results holding the same tokens
would be unequal — **in the one place a caller has every reason to compare**:
asserting an encoding against the result written out by hand.

Without that remark, the hand-written `Equals` looks like redundant code
generation and gets deleted.

### D3 — `GetHashCode` moves with `Equals`

A record whose `Equals` is overridden and whose hash is not is worse than the
original defect: it is a type that misbehaves in a dictionary. The two are always
changed together.

## Out of scope

- `ClassificationReport`, already a class for the same reason.
- Any other record in the codebase — though this is worth a sweep, and if one is
  found it is the same fix.

## What "done" means

Two results with equal tokens and ids compare equal; `GetHashCode` agreeing; the
reason recorded on the member; both frameworks green with no corpus movement.
