# 0374 — The index artifact is 1.34x larger on disk

**Issue:** [#0374](https://github.com/CyrilB1531/lodestar/issues/0374) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-20

## Problem

The text format's expansion is the cost it actually carries, and it had been argued about rather than priced. **1.34× on disk: base64 alone accounts for 1.333× of it** — the vector block is 15 360 000 bytes of floats and 20 480 000 as base64 text — with the remaining 0.5% the `dimension`/`normalize`/`count` fields and 10 000 quoted `doc-N` ids that `.npy`'s header does not carry.

## Where the argument moved

The size is what a sidecar format would buy, and **the speed is not**: [#324](https://github.com/CyrilB1531/lodestar/issues/324) found decoding costs ~1.3 ms *over* moving the bytes, and [ADR 0051](../../decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md)'s step 0 later found the same on the write side. So [ADR 0011](../../decisions/0011-persistence-format.md)'s update block concludes a binary format **"should be argued on the size rather than on the speed"**, and that is where it stands.

## What this lot also surfaced

The expansion is not only disk. It came straight off the index's ceiling — see [#377](https://github.com/CyrilB1531/lodestar/issues/377).
