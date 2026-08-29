# 0211 — Six classification metrics #93 left behind

**Issue:** [#0211](https://github.com/CyrilB1531/lodestar/issues/0211) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-19

## Problem

[#93](https://github.com/CyrilB1531/lodestar/issues/93) shipped the classification family and left six: Hamming loss, zero-one loss, Jaccard, multilabel confusion, the two likelihood ratios, and hinge loss.

## What decided the design, lot by lot

**Hinge loss brought an input shape the package had nowhere.** It takes a *decision function* — the signed distance from a boundary — where every other member takes a label or a probability.

**The margin is the point.** A sample costs nothing only once it is right by 1, so a prediction on the correct side but inside the margin is still charged where `ZeroOneLoss` counts it free. A test asserts both on the same input; that is what makes it the loss a support vector machine minimises rather than an error count.

**Only the decision's sign is compared against the label**, so `posLabel` is a parameter as it is on `RocAuc` and `BrierScore`, and relabelling cannot move the number: `-1/1`, `0/1` and `7/3` all score the same on the same decisions — a test spends that rather than stating it.

## What shipped

Six types over four lots, each with its frozen corpus, its reference pages and its equivalence rows.
