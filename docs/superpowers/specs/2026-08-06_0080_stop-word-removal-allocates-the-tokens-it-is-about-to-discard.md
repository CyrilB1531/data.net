# Design — #80: discard a stop word without allocating it first

**Date:** 2026-08-06 · **Issue:** #80 · **Branch:** `perf/80-stop-word-lookup` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

Stop-word removal sits in the **per-token path of every word-analyzed document**,
and stop words are by definition the tokens that occur most. Three costs, all paid
on the tokens that are about to be thrown away.

## Decisions

### D1 — Judge the token before allocating it

On `net10.0`, `TextAnalyzer` tests `s.AsSpan(m.Index, m.Length)` against a
`FrozenSet<string>` through its `AlternateLookup<ReadOnlySpan<char>>`, so **a stop
word is discarded without ever becoming a string**. Only survivors reach
`m.Value`.

### D2 — One list per first use, not six behind one static constructor

The six lists were static property initialisers sharing one static constructor, so
`StopWords.English` hashed **1 493 words to hand back 318**.

Each list now lives in its own nested holder type. The property is deliberately
`=> FrenchList.Value` and **not** `{ get; }`: a static field on `StopWords` puts
all six back behind one initialiser — and that is exactly what the laziness test
catches.

The test is what makes this decision durable. Without it, the next person tidies
the property into an auto-property and silently restores the cost.

### D3 — A shipped list is adopted, a caller's set is copied

`ToFrozenSet(StringComparer.Ordinal)` returns its argument when the set is already
frozen with that comparer — **verified, including that it copies when the
comparer differs** — so no vectorizer re-hashes `StopWords.English`.

A caller's `HashSet<string>` is still copied. It is theirs to keep mutating, and a
fitted vectorizer that followed along would remove words its options never
declared. There is a test for that.

The asymmetry is deliberate and is the interesting part: adopting is safe only
because the shipped lists are immutable.

### D4 — `netstandard2.0` keeps the path it always had

It has neither `FrozenSet` nor `AlternateLookup`. The choice is made **once**, in
`StopWordSet`, behind a single `#if`, in the shape of `src/Shared/Guard.cs` — not
one directive per call site.

### D5 — Which words are removed does not change

This is a pure allocation and lookup change. The corpora prove it.

## Out of scope

- Any change to the lists themselves (#13).
- The tokenization regex.

## What "done" means

Stop words discarded without allocating; one list initialised per first use, with
a test that catches a regression to the shared static constructor; the shipped
lists adopted rather than re-hashed; corpora unchanged; before/after allocation
numbers on the pull request.
