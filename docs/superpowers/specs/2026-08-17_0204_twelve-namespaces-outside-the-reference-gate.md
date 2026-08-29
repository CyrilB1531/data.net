# 0204 — Twelve namespaces outside the reference gate, one lot each

**Issue:** [#0204](https://github.com/CyrilB1531/lodestar/issues/0204) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-17

## Problem

Twelve namespaces were documented by nothing the build could check. `docs/wiki-map.json`'s `covered` table turns the gate on per namespace, and turning it on for a namespace whose pages do not exist fails the build — so the gate and the pages had to land together, one namespace at a time.

## What decided the shape

**The `covered` entry lands in the commit that completes a lot, never before it.** Lot 1 shipped an index and six type pages first with the entry deliberately withheld: six of twelve types would have failed the gate. A gate turned on early is a red build that teaches nothing.

**The index page is the half a generator cannot produce.** Which of three vectorizers to reach for, and what the choice costs — `HashingVectorizer` learns nothing and therefore has no `GetFeatureNames`, which is the trade rather than an omission.

## What executing the examples was worth

The snippet gate went from 92 executed to 136, none skipped, and **the executed half earned its keep on four pages**:

- `RowL2Norm` after weighting is `0.9999999999999999`, not `1`. Two pages promised the round number; those assertions moved to a shape that is exact.
- "the cat eats" hashed into 16 columns leaves **one** stored cell, not three. A real collision — the trade `HashingVectorizer` exists to make — so the pages now assert that a restored vectorizer hashes identically rather than quoting a count that reads like a bug.

## What shipped

Lot 1: the index, 12 type pages, 32 member pages, and the `covered` entry that makes all of it enforced. Every declaration replayed against both target frameworks' assemblies.
