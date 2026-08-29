# 0372 — The ADR index is missing the 0041 row

**Issue:** [#0372](https://github.com/CyrilB1531/lodestar/issues/0372) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-20

## Problem

`docs/decisions/README.md` jumped from 0040 straight to 0042. The row for `0041-one-sample-file-per-public-class.md` was never added.

## Why it matters more than its size

That index is the page a reader consults **instead of** the fifty-odd ADRs. A decision missing from it is a decision that does not exist for anyone who trusts the index — which is what the index asks them to do.

## What shipped

The row. The class of fault — an ADR landing without its index row — has no guard, and that remains true.
