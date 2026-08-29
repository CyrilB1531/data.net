# 0227 — Reference pages for Lodestar.Fuzzy

**Issue:** [#0227](https://github.com/CyrilB1531/lodestar/issues/0227) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

**Parent:** [#204](https://github.com/CyrilB1531/lodestar/issues/204), one lot of twelve.

## The shape every lot of #204 follows

An index, a page per type, a page per public method, and **the `covered` entry in the same commit** — the entry turns the gate on for the namespace, so it cannot land before the pages it would fail on. Declarations are replayed against both target frameworks' assemblies and examples are executed, so a signature that drifts fails CI rather than a reader.

## What this lot found

**Like Embeddings, `Lodestar.Fuzzy` ran no reference gate**, so a `covered` entry would have been prose nothing reads. Both the net10 project and the netstandard mirror now carry it.

**The index's job is *which scorer*,** because that is the decision a caller actually makes and the seven are one scorer applied to different things. What breaks each is what the page leads with: **length** breaks `Ratio`; **word order** breaks `Ratio` and `PartialRatio`; `TokenSetRatio` stops counting extra words.

## What shipped

15 pages for 4 types, 18 members, and the gate wiring for a third package.
