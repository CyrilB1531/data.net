# 0431 — Parallel base64 encoding is refused

**Issue:** [#0431](https://github.com/CyrilB1531/lodestar/issues/0431) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-28

## Problem

**No code to write.** The issue exists so the advice does not come back a fourth time: parallelising the base64 encode had been proposed twice on arithmetic nobody had checked.

## The measurement that refuses it

`bench/Lodestar.Text.Benchmarks -- save-phases`, five phases each a strict subset of the one above:

| | median | GB/s |
| --- | ---: | ---: |
| `Base64.EncodeToUtf8` | 3.211 ms | 4.78 |
| `memcpy` of the same block | 3.251 ms | 4.72 |

**Encoding costs nothing over copying.** The vectorised encoder already saturates the memory subsystem on one core, so extra cores contend for the same memory controller rather than adding throughput. **The 2.5–3× estimate assumed the encode was compute-bound; it is not.**

## The second, independent reason

The lever is worth **17.7% of `embedding_index_save`** against a bar of **≥ 2×** set before measuring. A free, perfectly scaling encode caps the row at **1.25×**.

## What would reopen it

A machine where the encode is genuinely compute-bound. **Re-run `save-phases` and compare `base64_encode` against `block_copy_floor` first**: if they are still the same number, the answer is unchanged. The subcommand is committed for exactly that.
