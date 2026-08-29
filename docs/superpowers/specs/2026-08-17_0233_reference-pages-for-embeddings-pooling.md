# 0233 — Reference pages for Lodestar.Embeddings.Pooling

**Issue:** [#0233](https://github.com/CyrilB1531/lodestar/issues/0233) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-17

**Parent:** [#204](https://github.com/CyrilB1531/lodestar/issues/204), one lot of twelve.

## The shape every lot of #204 follows

An index, a page per type, a page per public method, and **the `covered` entry in the same commit** — the entry turns the gate on for the namespace, so it cannot land before the pages it would fail on. Declarations are replayed against both target frameworks' assemblies and examples are executed, so a signature that drifts fails CI rather than a reader.

## What this lot found

Landed with the commit that gave `Lodestar.Embeddings` its first reference gate — see [#226](https://github.com/CyrilB1531/lodestar/issues/226).

## What shipped

1 type, 5 members. The smallest Embeddings lot, and the one whose whole content is which pooling a caller wants.
