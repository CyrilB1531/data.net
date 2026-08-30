# 0226 — Reference pages for Lodestar.Embeddings.Tokenization

**Issue:** [#0226](https://github.com/CyrilB1531/lodestar/issues/0226) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-17

**Parent:** [#204](https://github.com/CyrilB1531/lodestar/issues/204), one lot of twelve.

## The shape every lot of #204 follows

An index, a page per type, a page per public method, and **the `covered` entry in the same commit** — the entry turns the gate on for the namespace, so it cannot land before the pages it would fail on. Declarations are replayed against both target frameworks' assemblies and examples are executed, so a signature that drifts fails CI rather than a reader.

## What this lot found

**Lodestar.Embeddings had no reference gate at all.** Only `Lodestar.Text` and `Lodestar.Metrics` ran one, so a `covered` entry here would have been prose nothing reads — exactly the state #204 exists to end rather than to reproduce. Six of the twelve lots land in Embeddings or Fuzzy, so one of them was always going to discover it.

**The gate is wired into both the net10 project and the netstandard mirror.** The mirror links the same test sources, so it compiles the new test file too and needs the shared engine, the pages and the map exactly as its sibling does — without that, the linked file does not compile.

## What shipped

The largest lot of the twelve: 21 types, 147 members.
