# 0430 — Write the vector block a slice at a time

**Issue:** [#0430](https://github.com/CyrilB1531/lodestar/issues/0430) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-28

## Problem

`Utf8JsonWriter.WriteBase64String` encodes the whole block in one call, so the writer's buffer must grow to hold the entire 20.48 MB encoding **by successive doubling** — a large-object-heap allocation and an OS page commit per growth, plus a copy of everything written so far.

Step 0 measured writing the vector block alone at **16.938 ms of which the encode is 3.211**.

## What made it safe

**Slices of 245 760 bytes — a multiple of 12**, so every slice but the last is a whole number of base64 groups *and* of floats. That is exactly the condition under which concatenating slice encodings equals encoding the concatenation, so **the bytes on disk do not change**. `ChunkedBlockTests` pins it at nine sizes around the boundary against `WriteSingles`, which stays in the codebase off every save path purely as that oracle.

## What it was worth, and what was withdrawn

**A 1.61× first published from the container did not survive the nightly runner and was withdrawn.** What replaces it: allocation **39.64 MB → 19.87 MB**, collections 445.3 → 273.4 in every generation, and the published ratio against `numpy.save` **0.29× → 0.39×**.

**The load pays part of it back** — identical 35.35 MB allocated on both sides, 1.10–1.22× slower — because it had been subsidised by the buffer the save used to leave behind. The save's 1.35× is a trade, not a free win.

## What shipped

`Base64Numbers.WriteSinglesChunked`, `ArtifactIo.SaveWithBlock` owning the whole writer sequence, `SaveAsync` losing its intermediate `MemoryStream`, and [ADR 0051](../../decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md).
