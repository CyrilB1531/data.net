# 0228 — Reference pages for Lodestar.Text.Stemming

**Issue:** [#0228](https://github.com/CyrilB1531/lodestar/issues/0228) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

**Parent:** [#204](https://github.com/CyrilB1531/lodestar/issues/204), one lot of twelve.

## The shape every lot of #204 follows

An index, a page per type, a page per public method, and **the `covered` entry in the same commit** — the entry turns the gate on for the namespace, so it cannot land before the pages it would fail on. Declarations are replayed against both target frameworks' assemblies and examples are executed, so a signature that drifts fails CI rather than a reader.

## What this lot found

**Every example is taken from the frozen oracle corpora rather than written by hand**, so each `// =>` is a value `nltk` produced rather than one an author believed. Two are worth their page on their own:

- `geração` and `gerações` stem to **different** keys, which is the thing a reader would assume otherwise;
- Italian `esistenza` stems to `esistt`, per [decision 0008](../../decisions/0008-snowball-italian.md).

## What shipped

An index, seven type pages and seven member pages, with the `covered` entry in the same commit.
