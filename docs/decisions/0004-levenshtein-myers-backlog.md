# 0004 — Levenshtein: bit-parallel (Myers) optimization

**Status:** accepted · **Date:** 2026-08-01 · **Revised:** 2026-08-19

## Context

The cross-language bench (`bench/`) shows that the initial implementation of
[`Levenshtein.Distance`](../reference/text/distances/levenshtein-distance.md) — a rolling-row DP `O(n·m)` — is markedly slower than
rapidfuzz on long strings (≈ 37× at 512 characters), while being faster on short
strings (no call overhead). rapidfuzz owes that advantage to the **bit-parallel
Myers algorithm** (1999), in `O(n·⌈m/w⌉)` with `w = 64` bits per machine word.

Since performance is the project's central argument, this algorithmic gap must be
closed for medium/long strings.

## Done

Both the single-word and the blocked (multi-word) path shipped, the second in
the 2026-08-05 revision below.

- **Single-word Myers shipped** (`src/DataNet.Text/Distances/Myers.cs`), wired as
  the fast path of `Distance` on the `char` path for a pattern of length 16–64 in
  Latin-1; falls back to the DP otherwise. Zero allocation (`Peq` table in
  `stackalloc`). Validated with no extra test code by the BMP oracle cases
  (`Distance_default_utf16_matches_rapidfuzz_for_bmp_cases`).

- **Blocked (multi-word) Myers shipped.** The bit vectors span `⌈m/64⌉` words and
  the horizontal deltas carry from each word into the next; only the last word's
  bit at `(m-1) mod 64` moves the score. The 64-character cap on the fast path is
  gone.

  | Length | Python (rapidfuzz) | before | after |
  | ---: | ---: | ---: | ---: |
  | 128 | 2 693 ns | 36 178 ns | **1 777 ns** |
  | 512 | 21 688 ns | 683 581 ns | **20 555 ns** |

  33× at 512 characters, which turns a 31× deficit into a slight lead.

  Micro-optimising the DP was measured first and rejected: a char-specialised
  version with bounds checks elided via refs came out *slower* than the generic
  `Dp<char>` (3.97 vs 3.50 ns/cell). A scalar rolling-row DP was already at its
  floor, and rapidfuzz's 0.08 ns/cell-equivalent is unreachable without doing 64
  cells per word operation. The gap was never micro-architectural.

## To do

> **#208 update:** the first item is done for the code-point mode, and the second
> has changed character rather than been solved.
>
> The code-point mode takes the fast path by **renaming** rather than by a second
> kernel: the pattern's distinct code points are numbered `0..k-1`, every text
> symbol the pattern lacks is renamed to one reserved slot, and the existing
> `char` kernels run unchanged over the result. Measured in
> [`../guides/performance.md`](../guides/performance.md): 2× at 32 code points,
> 15× at 128, **32×–36× at 512**, zero allocation.
>
> That buys a ceiling instead of a restriction. 255 distinct symbols fit the
> renamed alphabet and a pattern holding more falls back to the DP, which the
> benchmark measures rather than hides — the failed attempt costs ≈1%.
>
> **The second item is now tractable and deliberately not taken.** The same
> renaming would lift the Latin-1 restriction on the `char` path, which is what
> "a sparse or hashed table would generalise it" was reaching for. It is not done
> here because that path is the hot one — this file's own note that helper calls
> cost measurably is about it — so it needs its own measurement and its own
> branch, not a change smuggled in beside a code-point lot. `Indel` and the
> length-32 bucket remain untouched; #208 is an umbrella and this was one of its
> three lots.
>
> `MyersMinPatternLength` stays at 16, now measured rather than inherited: at a
> pattern of exactly that length the code-point fast path is 1.5× ahead of the DP.
> Whether a lower gate would win more is untested and would move the character
> path too, the constant being shared.

- Extend the fast path to the code-point mode (`Distance<int>`) and to `Indel`.

> **#273 update: the `Indel` half is done, and it was never Indel's.** `Indel` is
> `len(a) + len(b) - 2·LCS`, so the kernel belongs in
> [`Lcs.SubsequenceLength`](../reference/text/distances/lcs-subsequencelength.md) —
> which `fuzz.ratio`, `process.extract` and blocking deduplication all run.
> Hyyrö's bit-parallel LLCS recurrence over the same alphabet-table machinery,
> since Myers' own recurrence carries substitution and LCS does not.
>
> Two separable wins, and the second was not on this list: `Lcs` was not trimming
> the common prefix and suffix that `Levenshtein` already stripped. Measured in
> [`../guides/performance.md`](../guides/performance.md): the 128 and 512 buckets
> go from 95–98× behind rapidfuzz to **2.07× and 2.15×**, a 46× and 44×
> improvement, of which trimming alone accounts for 1.94× and 1.10×.
>
> **The corpus reached none of it.** Of 1 522 oracle cases, 97 reach the kernel
> and **zero** reach the blocked path, every pair fitting one machine word once
> trimmed — the failure this file records for #52, one lot later. Property tests
> against the dynamic program cover it now, and a mutation is what proved they
> had power: they were vacuous at first because with two arguments C# resolves
> [`Lcs.SubsequenceLength`](../reference/text/distances/lcs-subsequencelength.md) to the
> generic overload, so both sides ran the DP.
>
> `BitParallelMinPatternLength` stays at 16, measured this time rather than
> inherited: at that band the kernel is already 2.85× ahead of the DP. It is
> likely conservative — the kernel's floor is ~149 ns and the DP costs 161 ns at
> band 8 — which wants a sweep below 8 rather than a guess.

- **Lift the Latin-1 restriction.** The equality table is 256 entries, so a
  pattern containing CJK or emoji still falls back to the DP — the figures above
  do not describe those inputs. A sparse or hashed table would generalise it.
- **The length-32 bucket sits at 1.4× behind rapidfuzz.** It already takes the
  single-word path, so the cause differs from the one fixed here and needs its own
  measurement.

> **#208 update: closed, and this bullet had the cause wrong.** "It already takes
> the single-word path" was true of 53% of the bucket and false of the rest. Split
> on the dispatch's own criterion, the pairs falling *under* the gate were 47% of
> the bucket and 70% of its cost — 650 ns/pair against 255 for the ones taking
> Myers, on a band a quarter the size. The kernel was never the problem; the gate
> was 16 where the curves cross at 8.
>
> That also retires both "stays at 16" above. Neither was wrong when written —
> each measured a pattern *at* 16 and found the kernel ahead, which says the gate
> is not too low and says nothing about it being too high. Sweeping the constant
> against the committed corpus is what answered the other half, and it is the only
> way to: a constant the dispatch consults cannot be swept from inside the
> dispatch. `MyersMinPatternLength` is 8, `BitParallelMinPatternLength` is 8, and
> the length-32 bucket went 427.6 → 204.8 ns/pair on Levenshtein and 318.7 → 145.6
> on Indel — 1.46× and 1.31× *ahead* of rapidfuzz where it had been 1.4× behind.
>
> **The shared constant is now two.** "Whether a lower gate would win more is
> untested and would move the character path too, the constant being shared" was
> the right worry: it would have, and in the wrong direction. The code-point path
> renames both operands through a 512-entry probe table first, so it carries the
> larger fixed cost and crosses at 10, not 8 — at a pattern of 8 the DP is 11%
> ahead of it. `MyersMinCodePointPatternLength` is its own constant for that
> reason, measured in [`../guides/performance.md`](../guides/performance.md).
>
> **A second finding, independent of the gate.** Both single-word kernels called
> `Clear()` on a 256-entry equality table that `stackalloc` had already zeroed —
> a second 2 KB memset on a call whose work is `O(n)`. This file's own note that
> the probe table needs no fill, because "stackalloc zeroes and nothing here
> disables localsinit", sat three constants above the line that cleared. Worth 12%
> of the bucket on Levenshtein and 17% on Indel, on top of the gate. **Right-sizing
> the table to the pattern's own alphabet is still open**, and is still the same
> change as lifting the Latin-1 restriction above.
>
> **#301 update: the remaining memset is worth one kernel's while and not the
> other's.** `localsinit` zeroes the same 2 KB on every call, which no `Clear()`
> removal reaches and `AllowUnsafeBlocks=false` forbids `[SkipLocalsInit]` from
> suppressing. A `[ThreadStatic]` table held all-zero between calls replaces it
> with a restore loop over the pattern — `O(m)` rather than `O(256)`. Swept over
> the pair corpus at a longest-held-pattern of 0, 16, 32 and 64, it is worth **13%
> of the length-32 bucket on Indel** and a **regression at every value on
> Levenshtein**, so `BitParallelLcs` holds its table at 32 and `Myers` keeps its
> `stackalloc`. The LCS recurrence is four operations per text character against
> Myers' dozen, so the identical fixed cost is a far larger share of what its call
> does. Numbers in [`../guides/performance.md`](../guides/performance.md).
>
> **Where this shape goes wrong is the refusal.** A pattern is written character by
> character and abandoned partway when one leaves Latin-1, so entries are already
> set when the kernel gives up — and the damage never shows on the call that causes
> it, only on the next one, whose text reads a mask its predecessor left behind.
> That is a test rather than a comment, and it failed before it passed.
>
> **A microbenchmark said this won in both kernels.** It timed 64 rotating pairs, a
> working set small enough that the held table never left L1; the corpus evicts it.
> Sizing a fixed cost in isolation is worth doing first and is not worth believing
> on its own.

## Testing note

The blocked path shipped with **zero coverage from the existing corpus**, and the
full suite passed regardless. The `long` family draws from BMP ranges, so every
one of its 85 long cases contains CJK, fails the Latin-1 check and falls back to
the DP — the new code was never executed.

Two `long_ascii`/`long_latin` families were appended to `build_pairs` to fix that;
89 cases now genuinely exercise it. Appending rather than inserting keeps the RNG
stream intact, so every pre-existing case keeps its id and value.

The lesson generalises: a green suite proves nothing until you have checked that
the new path is reached. Coverage of a *file* is not coverage of a *branch*.

> **#208 update:** it generalised, to the same corpus, one plane up. Measured
> before writing the code-point path: of 1425 cases, 283 reached the length gate
> and 194 of those held a character above U+00FF — a real net — but **none** held
> a supplementary character, because the `supplementary` family draws 2–10
> characters and the gate opens at 16. Surrogate decoding was the only genuinely
> new part of the change and had no case long enough to exercise it.
>
> A `long_supplementary` family was appended, again last, again leaving every
> pre-existing id and value untouched. Its 97 cases split 19 on the single-word
> kernel, 60 on the blocked one, and 18 above the 255-symbol ceiling on the
> fallback — so all three outcomes are covered, including the refusal.

## Notes

- Myers manipulates per-alphabet-character bit masks; implementing it cleanly for
  an arbitrary Unicode alphabet needs a `char -> bitmask` table (dictionary or
  array depending on range). Start with the common ASCII/BMP path.
- Allowed inspiration source: Myers' published paper, "A Fast Bit-Vector Algorithm
  for Approximate String Matching Based on Dynamic Programming" (JACM 1999) —
  published pseudo-code, no transcription of copyleft source (cf.
  [`0003-provenance-and-licensing.md`](0003-provenance-and-licensing.md)).
