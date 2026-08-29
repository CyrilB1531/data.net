# 0436 — Memory-mapping is blocked on a format, not on a measurement

**Issue:** [#436](https://github.com/CyrilB1531/lodestar/issues/436) ·
**Status:** proposed — records the blocking condition; **no plan, because there is nothing to
execute** · **Date:** 2026-08-29

## Why this has a spec and no plan

A plan in `docs/superpowers/plans/` is an instrument for work that can start. #436 cannot: it needs
a decision that belongs to [ADR 0011](../../decisions/0011-persistence-format.md), and writing
tasks for it would produce checkboxes nobody may tick. What is worth writing down is the
**blocking condition** — precisely enough that whoever revisits 0011 can see what turns on it, and
precisely enough that nobody reopens #436 for the wrong reason.

## The condition, restated exactly

Mapping the vector block needs it to be **a contiguous, aligned, unencoded run of bytes at a known
file offset**. Today it is base64 inside a JSON string, which is none of the three.

## The one reason it must not be reopened

Not speed. ADR 0011's own update block settled the read side, and step 0
([#429](https://github.com/CyrilB1531/lodestar/issues/429),
[ADR 0051](../../decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md)) settled
the write side: `base64_encode` costs 3.211 ms against `block_copy_floor`'s 3.251 ms for the same
15.36 MB — **the encode costs nothing over moving the bytes**. Removing the encoding therefore buys
no measurable time in either direction.

**The case for a sidecar is size, and only size**: 5.2 MB of expansion on a 20.5 MB artifact.
Mapping would be a genuine load-time argument, but it is downstream of that decision and is not by
itself a reason to reopen 0011.

## What changed since the issue was written

[#450](https://github.com/CyrilB1531/lodestar/issues/450) shipped `NpyFile`, and it matters here
more than it was meant to. numpy's `.npy` is exactly the shape #436 requires, and the repository
now reads and writes it:

- **Contiguous and unencoded** — a raw little-endian `float32` run after the header.
- **At a known offset** — the header declares its own length, in the first 10 or 12 bytes.
- **Aligned** — numpy pads the header so the payload begins at a multiple of 64, for this reason.
  Measured on our own writer: a 6-float array produces a 152-byte file, so the payload starts at
  128. Any multiple of 64 satisfies `float`'s 4-byte requirement with room to spare.

So a decision to revisit 0011 no longer has to invent a format, specify it, or prove a reader
against numpy — **that work is done and its fixtures were written by numpy itself.** That lowers
the cost of the format decision. It does not make the decision, and it is not an argument for
taking it: `.npy` carries no ids, no normalize flag and no schema, which is exactly why #450 is
interop and not a second artifact format.

## What would have to be true to unblock

1. ADR 0011 revisited **on size**, with its own measurement, and deciding for a sidecar.
2. That sidecar's block laid out contiguously and aligned — `.npy` being the obvious candidate now,
   but the decision's to make.
3. Only then: a lot for the mapping itself, which brings its own questions this spec does not
   answer — the lifetime of a mapped view against `EmbeddingIndex`'s `float[]` backing store, what
   happens when the file changes under a live index, and whether `netstandard2.0` gets the same
   behaviour or a documented divergence.

Point 3 is the one to be wary of. #435's lot is already the smaller version of that lifetime
question, and its answer should be in hand first.

## Acceptance

There is nothing to accept. This spec is done when it is filed, and #436 stays open and blocked
with a link to it. If it is ever closed without a sidecar, it closes as **out of scope**, not as
refused — the mechanism was never in doubt, only its prerequisite.
