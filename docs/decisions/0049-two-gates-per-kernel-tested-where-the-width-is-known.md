# 0049 — Two gates per kernel, the second tested where the width is already known

**Status:** accepted · **Date:** 2026-08-21 · **Amends:** [`0048`](0048-the-gate-depends-on-the-kernel-and-the-alphabet.md)

Settles what 0048 deliberately left open. That record establishes that the shared
constant of 8 is wrong in three of its four cases; this one replaces it.

## Context

Decision 0048 measured four crossings over the banded buckets #409 added — `Lcs` at ≤ 2
on Latin and 6 on CJK, `Myers` at 5 and 10 — against one shared constant of 8. The
question it left is what shape replaces it, since the dispatch tests the gate on the
pattern's length **before** anything has looked at the pattern's characters.

## Decision

**Two constants per kernel, and the second is tested where the width has just been
established rather than at the dispatch.**

| | dense gate, at the dispatch | wide gate, at the reroute |
| --- | ---: | ---: |
| `Lcs` / `BitParallelLcs` | 2 | 6 |
| `Levenshtein` / `Myers` | 5 | 10 |

The dispatch keeps one test, now valued at the Latin-1 crossing. Both kernels already
discover that a pattern leaves Latin-1 while building their equality table, and both
already branch there to a wide method; that branch is where the second gate goes, so a
pattern that is wide *and* short returns false and the caller falls back to the dynamic
program it was going to use anyway.

**The refusal precedes the wide method's `stackalloc`**, which 0043 records as zeroing
on entry to the method holding it whether or not the branch needing it is taken.

This is not a new shape in this codebase: `MyersMinCodePointPatternLength` is already a
second gate for a path that crosses later, and its own note gives this decision's
argument — "one constant would have to regress one of the two paths".

## The three shapes refused

- **Four constants at the dispatch.** It does not know the alphabet there, so it would
  have to scan the pattern before choosing — work charged to the Latin-1 path, which is
  what `fuzz.ratio`, `process.extract` and blocking deduplication run. The JIT listing
  says how much is at stake: that path contains no comparison against `0xFF` at all
  today.
- **A gate reading the pattern's width.** The same objection, and the same scan: the
  fact is produced anyway, but *after* the gate is tested, and moving the test forward
  is what costs.
- **One shared constant, re-valued.** Lowering the shared gate to 2 and 5 without a
  wide refusal was measurable rather than hypothetical: #409 priced a short wide pattern
  on the kernel at 1.53× the dynamic program for Myers at band 2 and 1.28× for LCS. The
  reach would have been bought by regressing the alphabet it was bought for.

## Consequences

- **The Latin-1 path executes the same instructions.** Diffing the JIT's output the way
  #302 did — dispatch and both single-word kernels, six methods — every loop is
  unchanged instruction for instruction. Two once-per-call instructions use a different
  register, the allocator having reshuffled around the new blocks, and 45 instructions
  are added: three refusals, their early-return epilogues and the stack checks those
  need. None is on a path a Latin-1 pattern reaches.
- **The two kernels do not benefit equally, and the corpus says why.** On the shipped
  scattered buckets, `Indel` gains 15.4% at length 32 across three interleaved rounds;
  `Levenshtein` shows nothing, its sign flipping between rounds. The bucket has 94% of
  its pairs above band 4 and 86% above 8, so Myers' move from 8 to 5 touches the 8% that
  sit between, at 15–30% each — one to two percent overall, under this machine's noise
  floor. LCS's move from 8 to 2 recovers a far more populated range at up to 2.5× each.
- **Myers' change therefore rests on the banded measurement, not on the corpus.** It is
  kept because the instrument built to answer the question answers it, and because the
  shipped corpus shows no cost; it is not kept on a demonstrated gain, and this record
  says so rather than letting a table imply one.
- **A gate is now two numbers per kernel, so a later sweep has to move both.** The
  property tests assert the route at each of the four values rather than the answers
  alone, which is what makes a silent edit to one of them fail.
