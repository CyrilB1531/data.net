# 0229 — Reference pages for Lodestar.Text.Phonetics

**Issue:** [#0229](https://github.com/CyrilB1531/lodestar/issues/0229) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

**Parent:** [#204](https://github.com/CyrilB1531/lodestar/issues/204), one lot of twelve.

## The shape every lot of #204 follows

An index, a page per type, a page per public method, and **the `covered` entry in the same commit** — the entry turns the gate on for the namespace, so it cannot land before the pages it would fail on. Declarations are replayed against both target frameworks' assemblies and examples are executed, so a signature that drifts fails CI rather than a reader.

## What this lot found

**The index measures the three rather than describing them.** Over the 402-word corpus all three are pinned to, **Soundex has 101 words sharing a code and NYSIIS 13** — which is the recall-against-precision choice a reader actually has to make, and it is a number rather than an adjective.

**Metaphone's examples come from `metaphone.json`, not the shared corpus**: [decision 0007](../../decisions/0007-phonetic-oracles.md) reserves the latter for Soundex and NYSIIS.

## What shipped

An index, three type pages and three member pages, with the `covered` entry in the same commit.
