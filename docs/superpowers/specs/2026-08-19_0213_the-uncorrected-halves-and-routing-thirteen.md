# 0213 — The uncorrected halves, and routing thirteen clustering metrics

**Issue:** [#0213](https://github.com/CyrilB1531/lodestar/issues/0213) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-19

## Problem

`mutual_info_score`, `rand_score` and `pair_confusion_matrix` are the uncorrected halves of pairs whose corrected forms already shipped. They are the ones **easy to reach for by name and rarely wanted**.

## What decided the documentation

The clustering page was not missing the reasoning — it already separated corrected from uncorrected, symmetric from asymmetric, and warned that `DaviesBouldin` reads the other way round. **What it was missing was the arrangement**: thirteen types reached through paragraphs, where every sibling index hands the reader a flowchart.

**The first branch is whether a reference partition exists at all**, because it partitions the namespace: three of the thirteen read the samples instead, and one of those three is the only one taking a distance matrix. A reader arriving from `silhouette_score(metric='precomputed')` needs that before the type list, not after it.

**Corrected-for-chance is placed to be hard to miss**, for the reason above.

## What shipped

Three types, and the diagram that routes all thirteen.
