# 0411 — The shared bit-parallel gate is wrong in three of its four cases

**Issue:** [#0411](https://github.com/CyrilB1531/lodestar/issues/0411) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

With [#409](https://github.com/CyrilB1531/lodestar/issues/409)'s banded buckets the gate could finally be read where it acts, and **three of its four cases were wrong**: one constant per kernel, two alphabets, and only one combination landed near its crossing.

## What shipped

`Lcs.SubsequenceLength` takes the bit-parallel route from a pattern of **2** and `Levenshtein.Distance` from **5**, while a pattern holding a character above U+00FF is refused below **6** and **10** instead — [ADR 0049](../../decisions/0049-two-gates-per-kernel-tested-where-the-width-is-known.md), two gates per kernel, tested where the width is already known.

## The record this thread leaves

Three ADRs on one question — [0047](../../decisions/0047-one-gate-per-kernel-not-one-per-alphabet.md), [0048](../../decisions/0048-the-gate-depends-on-the-kernel-and-the-alphabet.md), [0049](../../decisions/0049-two-gates-per-kernel-tested-where-the-width-is-known.md) — **each overturned by evidence the previous one could not have had**, because the corpus that would have produced it did not exist yet. The lesson is not that the first two were careless; it is that a measurement is only as good as the corpus can see.
