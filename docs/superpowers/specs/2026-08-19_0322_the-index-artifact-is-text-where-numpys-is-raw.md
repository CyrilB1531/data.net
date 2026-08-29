# 0322 — The index artifact is text where numpy's is a raw block

**Issue:** [#0322](https://github.com/CyrilB1531/lodestar/issues/0322) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-19

## Problem

`embedding_index_save` read **1.13× ahead of numpy** in `bench/README.md` and **0.27× behind** in the nightly. Both were right, and that is the finding.

## Why the ratio inverts

**The C# side moved 6% between the two machines and numpy's moved by a factor of four.** `numpy.save` writes a raw block — bandwidth-bound work newer hardware speeds up almost linearly — where this artifact base64-encodes into JSON at a **cost per byte that barely moves**.

**A ratio that inverts on faster hardware is a property of the machine, and publishing it as a property of the code was the fault.** That reframing is what the issue produced, and it is why the guide now names the machine beside every number.

## What followed

[#323](https://github.com/CyrilB1531/lodestar/issues/323) and [#324](https://github.com/CyrilB1531/lodestar/issues/324) took the save and load paths, and [#374](https://github.com/CyrilB1531/lodestar/issues/374) took the size question the format actually carries.
