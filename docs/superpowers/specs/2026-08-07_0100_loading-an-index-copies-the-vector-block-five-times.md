# Design — #100: loading an index copies the vector block five times

**Date:** 2026-08-07 · **Issue:** #100 · **Branch:** `perf/100-index-load-copies` ·
**Checkout:** `<repo>`

## Problem

`EmbeddingIndex.Load` moves 15 MB of floats in **five passes** and allocates
**90 MB** to do it — six times the payload, on the path a consumer takes at
startup.

## Decisions

### D1 — One buffer on the read path, sized before it is filled

`JsonArtifact.ReadAllBytes` and its async counterpart return a
`ReadOnlyMemory<byte>` over a buffer sized from **the stream's own length**,
instead of accumulating into a growable `MemoryStream` and calling `.ToArray()`.

A stream that will not say how long it is — **or that says it wrong** — falls back
to the growable path, **with its position put back first** so nothing is silently
truncated. That fallback is the whole correctness argument for the change.

Ten loaders across both packages follow the type change; all of it is `internal`,
so no public signature moves.

### D2 — Base64 decodes straight into its destination

`ReadSingles` and `ReadDoubles` size the `float[]` / `double[]` from the token's
**encoded** length and decode into it with
`System.Buffers.Text.Base64.DecodeFromUtf8`.

`ReadBoundedRaw` and `ReadUnboundedRaw` are replaced by one `Decode<T>` that falls
through to the old `TryGetBytesFromBase64` path for any token that is **not
canonical** — same exception types, same messages, on every path.

Keeping the exception surface identical is what allows this to be a performance
change rather than a behavioural one.

### D3 — Vectorize the non-finite scan on `net10.0`, after measuring its share

Measured at **18 % of the load figure** before being touched. A optimisation
applied without knowing its share is a guess with extra steps.

**The vector pass only detects; the scalar loop still locates**, so the exception
message still names the exact item and component. Speed does not cost
diagnosability.

### D4 — The size bound gets stronger, not weaker

`MaxTotalBytes` still caps the payload **before anything is allocated**, which is
what bounds the vector block now that it has no element-count limit (#62).

The argument improves: the destination is sized from a length that gate has
already bounded, rather than from a count discovered *after* decoding.

### D5 — Not a byte of the artifact changes

No format change, no public signature moved, and **every hardening suite passes
unmodified**. That last point is the evidence: the suites written to attack the
loader were not adjusted to accommodate it.

## Expected result

| | before | after |
| --- | ---: | ---: |
| Passes over the block | 5 | **3** |
| Allocated | 90 MB | **35 MB** |

## Out of scope

- The artifact format (ADR 0011).
- The write path.

## What "done" means

Three passes and 35 MB; the length-lying stream covered by a test; the exception
surface identical on every path; the hardening suites unmodified; before/after
published with the machine named.
