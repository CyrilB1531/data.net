# 0043 — The equality table is sized to the pattern, not to Latin-1

**Status:** accepted · **Date:** 2026-08-20 · **Amends:** [`0004`](0004-levenshtein-myers-backlog.md)

Amends two of 0004's backlog bullets — *Lift the Latin-1 restriction* and the
equality table's fixed cost — and retires both. The rest of that record stands.

## Context

The bit-parallel kernels index a 256-entry table by the character. Two costs follow,
and 0004 proposed one fix for both: "a sparse or hashed table would generalise it".

- **The table is zeroed on every call.** `stackalloc` zeroes it because nothing
  disables `localsinit`, so a call whose work is `O(n)` pays 2 KB of memset for a
  table of which at most 64 entries are used.
- **A pattern above U+00FF cannot be represented in it**, so CJK and emoji fell back
  to the dynamic program in the UTF-16 mode.

## Decision

**Generalising the whole table is refused.** That is what the code-point path already
does, and #208 measured its price: renaming both operands through a probe table makes
it cross the dynamic program at a pattern of 10 where the character path crosses at
8. The table is instead sized to what a pattern actually needs, in two independent
ways.

**The table is held between calls in one kernel and not the other.** A
`[ThreadStatic]` table kept all-zero, restored by walking the pattern, costs `O(m)`
rather than `O(256)`. Swept over the pair corpus at a longest-held pattern of 0, 16,
32 and 64, it is worth 13% of the length-32 bucket on Indel and a regression at every
value on Levenshtein, so `BitParallelLcs` holds its table at 32 and `Myers` keeps its
`stackalloc`. The LCS recurrence is four operations per text character against Myers'
dozen, so the identical fixed cost is a far larger share of what its call does.

**The dense table keeps Latin-1 and a side table carries the rest.** Out-of-range
characters are the rare case, so the common path is unchanged and only a pattern that
needs the side table pays for it. In the single-word kernels the side table is a
separate method's `stackalloc`; in the blocked ones its symbols are extra *rows* of
the existing table, `(256 + slots) × blocks` words with slot `k` at row `256 + k`, so
the multi-word carry and borrow are untouched.

## Consequences

- **A `stackalloc` zeroes on entry to the method holding it, taken branch or not.**
  That is why each wide path is its own method: otherwise a Latin-1 pattern would be
  charged the side table on every call. It also forbids the tidier shapes — one
  method choosing its table with a conditional cannot work.
- **The held table's invariant is restored on every exit, the refusal included.** A
  pattern is written character by character and abandoned partway when one leaves
  Latin-1, so entries are already set when the kernel gives up; the damage shows on
  the *next* call, whose text reads a mask its predecessor left behind.
- **The side table's capacity is bounded by the pattern, not by a constant.** 128
  slots are sound for one word only, 64 characters holding at most 64 distinct
  symbols. A blocked pattern has no length bound, so the table can fill and leave the
  probe no free slot to stop on.
- **A microbenchmark sized the first change and got its sign wrong** for one kernel.
  It rotated 64 strings, a working set small enough that the held table never left
  L1; a corpus walk evicts it. Sizing a fixed cost in isolation is worth doing first
  and is not worth believing alone.
- **Whether the added branch costs anything on the Latin-1 path is a question about
  generated code**, and was answered by diffing the JIT's output rather than by
  timing a loaded machine: identical, all 83 instructions.

Numbers, with their machine and their window, in
[`../guides/performance.md`](../guides/performance.md).
