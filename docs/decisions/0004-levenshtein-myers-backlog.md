# 0004 — Levenshtein: bit-parallel (Myers) optimization

**Status:** accepted · **Date:** 2026-08-01 · **Revised:** 2026-08-05

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
- **Lift the Latin-1 restriction.** The equality table is 256 entries, so a
  pattern containing CJK or emoji still falls back to the DP — the figures above
  do not describe those inputs. A sparse or hashed table would generalise it.
- **The length-32 bucket sits at 1.4× behind rapidfuzz.** It already takes the
  single-word path, so the cause differs from the one fixed here and needs its own
  measurement.

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
