# 0208 — Levenshtein's fast path stops at three doors

**Issue:** [#0208](https://github.com/CyrilB1531/lodestar/issues/0208) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

The bit-parallel path was reachable only from one of three directions. The code-point mode was dynamic-program-only, `Indel` did not take it, and the length-32 bucket fell below the gate — so [decision 0002](../../decisions/0002-code-point-mode.md) pointed a caller wanting Python's answer on supplementary characters at the slow path.

## The corpus came first, and both corpora failed

The issue asks for exactly that check, and it was worth making:

- `LevenshteinBenchmarks` draws from `"abcdefghijklmnopqrstuvwxyz "`, so its `Distance_CodePoint` row **decodes ASCII and measures nothing the mode is for**.
- Of 1 425 oracle cases, 283 reached the length gate and 194 of those held a character above U+00FF — a real net — but **none held a supplementary character**, because that family draws 2–10 characters and the gate opens at 16. Surrogate decoding was the new part and had no case long enough to run it.

A `long_supplementary` family was appended last, leaving every pre-existing id and value untouched.

## What decided the implementation

**A rename rather than a second kernel.** The code-point mode reaches the same bit-parallel path; nothing about the kernel is mode-specific once the input is decoded.

## What shipped

Three lots. Measured **2.09× for Levenshtein and 2.19× for Indel** on the length-32 bucket, with every other bucket inside noise.
