# 0409 — The corpus has no bucket whose trimmed pattern is short

**Issue:** [#0409](https://github.com/CyrilB1531/lodestar/issues/0409) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

**The corpus had no bucket the gate could see below 8.** A length-8 pair mutated at 10% **trims to a median pattern of 0**, in either alphabet — so every conclusion in that range rested on the length-32 bucket alone, whose median pattern is 16 with 70% of its pairs at or above 12.

**That is above the range where the curves separate**, and it is why [decision 0047](../../decisions/0047-one-gate-per-kernel-not-one-per-alphabet.md) concluded from it that no alphabet wants a different gate.

## What shipped

**Twenty banded buckets spanning bands 2 to 16 in both alphabets, 500 pairs each**, built so the trimmed pattern lands in a known band rather than being whatever mutation leaves behind.

## What it then found

**The gate is wrong in three of its four cases** — [#411](https://github.com/CyrilB1531/lodestar/issues/411). A decision taken on a corpus that could not see the range it was deciding about was wrong, and it took a better corpus to know it.
