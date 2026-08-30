# 0323 — embedding_index_save reads 1.13x on one machine and 0.27x on another

**Issue:** [#0323](https://github.com/CyrilB1531/lodestar/issues/0323) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-19

## Problem

Two published figures for one row, on two machines, pointing opposite ways — and only one of them was on the page. See [#322](https://github.com/CyrilB1531/lodestar/issues/322) for why they invert.

## What was under it

`Base64Numbers.WriteSingles` **allocated a full copy of the vector block** in order to swap bytes that, on a little-endian machine, never need swapping. The copy is the entire cost of a branch nothing takes.

## What shipped

The copy removed on the little-endian path, the byte-swapping kept for the machines that need it, and the guide's save section rewritten to name its machine. What it did **not** do is measure where the rest of the save goes — that stayed an assumption until [ADR 0051](../../decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md)'s step 0 finally measured it and found the encode at 17.7%, not most of it.
