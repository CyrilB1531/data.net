# 0231 — Reference pages for Lodestar.Embeddings.Search

**Issue:** [#0231](https://github.com/CyrilB1531/lodestar/issues/0231) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-17

**Parent:** [#204](https://github.com/CyrilB1531/lodestar/issues/204), one lot of twelve.

## The shape every lot of #204 follows

An index, a page per type, a page per public method, and **the `covered` entry in the same commit** — the entry turns the gate on for the namespace, so it cannot land before the pages it would fail on. Declarations are replayed against both target frameworks' assemblies and examples are executed, so a signature that drifts fails CI rather than a reader.

## What this lot found

It landed with the commit that **gave `Lodestar.Embeddings` a reference gate at all** — see [#226](https://github.com/CyrilB1531/lodestar/issues/226) for that discovery. The gate had to be wired into the net10 project *and* the netstandard mirror, because the mirror links the same test sources and would not compile without the shared engine, the pages and the map.

## What shipped

3 types, 22 members — `EmbeddingIndex`, its search result and its options.
