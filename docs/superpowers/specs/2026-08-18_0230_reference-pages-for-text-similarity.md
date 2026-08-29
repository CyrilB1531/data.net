# 0230 — Reference pages for Lodestar.Text.Similarity

**Issue:** [#0230](https://github.com/CyrilB1531/lodestar/issues/0230) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

**Parent:** [#204](https://github.com/CyrilB1531/lodestar/issues/204), one lot of twelve.

## The shape every lot of #204 follows

An index, a page per type, a page per public method, and **the `covered` entry in the same commit** — the entry turns the gate on for the namespace, so it cannot land before the pages it would fail on. Declarations are replayed against both target frameworks' assemblies and examples are executed, so a signature that drifts fails CI rather than a reader.

## What this lot found

**The five share a numerator and disagree only on the denominator**, so the index is built around that one table rather than around five descriptions. Two consequences follow from the table and are what a reader comes for:

- **Jaccard and SorensenDice rank identically**, being a monotone function of one another — so choosing between them changes a score and never an order;
- **Tversky is the other four in disguise**: `alpha = beta = 1` is Jaccard, `alpha = beta = 0.5` is SorensenDice.

## What shipped

An index, five type pages and five member pages, with the `covered` entry in the same commit.
