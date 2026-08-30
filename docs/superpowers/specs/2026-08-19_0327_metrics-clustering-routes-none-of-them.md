# 0327 — metrics/clustering lists 13 metrics and routes none of them

**Issue:** [#0327](https://github.com/CyrilB1531/lodestar/issues/0327) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-19

## Problem

Thirteen types reached through paragraphs. **The page was not missing the reasoning** — it already separated corrected from uncorrected, symmetric from asymmetric, and warned that `DaviesBouldin` reads the other way round. **What it was missing was the arrangement.**

## The branches, and why each is where it is

- **First: is there a reference partition at all**, because it partitions the namespace. Three of the thirteen read the samples instead, and one of those three is the only one taking a distance matrix — **a reader arriving from `silhouette_score(metric='precomputed')` needs that before the type list, not after it.**
- **Corrected-for-chance is placed to be hard to miss.** The uncorrected three are the ones easy to reach for by name and rarely wanted.

## What shipped

The diagram, and the prose left as it was — it was already good enough that the diagram had only to arrange it.
