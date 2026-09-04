# 0079 — MatchRatingApproach.Compare measures codex length in characters, not bytes

**Status:** accepted · **Date:** 2026-09-04

## Context

[#313](https://github.com/CyrilB1531/lodestar/issues/313) is what this records. The Match Rating
Approach's codex ([`MatchRatingApproach.Codex`](../reference/text/phonetics/matchratingapproach-codex.md))
accepts any Unicode letter, not just ASCII, so
validating it against `jellyfish.match_rating_codex` and `jellyfish.match_rating_comparison`
1.2.1 had to cover non-Latin words. Doing that surfaced two places where jellyfish 1.2.1 measures
a codex by its UTF-8 **byte** length rather than its **character** length, both accidental rather
than part of the 1977 algorithm description (which predates Unicode and says "six characters").

**The codex's own six-character truncation.** For a word whose codex is 3 to 6 characters long but
whose UTF-8 encoding runs past 6 bytes — three or more CJK characters, or four or more two-byte
Latin/Cyrillic ones — jellyfish's truncation condition fires on the byte count while the
first-three/last-three slicing it then performs uses the correct character count, producing a
codex **longer** than the untruncated one it started from:

```text
jellyfish.match_rating_codex("並丝七世")  ->  "並丝七丝七世"   # 4 chars in, 6 out
```

The correct codex — the one both the published rule and MatchRatingApproach.Codex give — is the
4-character string unchanged: no rule an English-only 1977 algorithm could state would grow a word
under truncation.

**The comparison's length-gap and minimum-rating steps.** `jellyfish.match_rating_comparison`
computes the "lengths differ by 3 or more -> no rating" check and the combined-length lookup into
the minimum-rating table from the same byte count, not the character count, even when neither
operand's own codex is corrupted by the first divergence:

```text
jellyfish.match_rating_codex("日本")               ->  "日本"    # 2 characters, correct
jellyfish.match_rating_codex("AB")                 ->  "AB"
jellyfish.match_rating_comparison("日本", "AB")     ->  None      # byte lengths 6 and 2 differ by 4
```

Both codices are 2 characters (a gap of 0); by character count the pair is comparable and rates a
definite answer. `docs/decisions/0003-provenance-and-licensing.md` bars deriving an implementation
from a reference one, and reproducing an accidental byte/character mixup would be exactly that —
there is nothing in the published algorithm this could be read out of.

## Decision

**MatchRatingApproach.Codex and [`MatchRatingApproach.Compare`](../reference/text/phonetics/matchratingapproach-compare.md)
measure length in `string.Length`
(UTF-16 code units, equal to character count for every codex either produces — a codex holds no
characters outside the Basic Multilingual Plane) throughout: the six-character truncation, the
length-gap check, and the minimum-rating table lookup.** This matches jellyfish exactly whenever
jellyfish's own byte and character counts agree, which they always do for ASCII and for any
Unicode word short enough to avoid the codex bug above — the whole parity oracle
(`tests/oracles/match_rating_codex.json`, `tests/oracles/match_rating_comparison.json`) is built
from words in that range, deliberately: `tools/generate_oracles.py`'s `MRA_WORDS` keeps each
Unicode fixture at 6 UTF-8 bytes or fewer. `MatchRatingApproachOracleTests` pins the two divergent
examples above as `[InlineData]` facts instead, read against `MatchRatingApproach` directly rather
than jellyfish.

## Consequences

- [`MatchRatingApproach.Codex`](../reference/text/phonetics/matchratingapproach-codex.md) and
  [`MatchRatingApproach.Compare`](../reference/text/phonetics/matchratingapproach-compare.md) give
  a definite, correct answer on every Unicode input their contract accepts — no word makes its own
  codex grow, and no pair of correct codices is refused a rating that their character lengths
  support.
- The parity oracle proves exact agreement with jellyfish only within the byte/character-safe
  range that covers ordinary use (ASCII names, and short non-Latin ones); the two `[InlineData]`
  facts are what pins Lodestar's own behavior past that range, since jellyfish has nothing correct
  there to replay.
- A future change to jellyfish's Rust codex that fixes this (or changes it further) does not
  reopen this decision — Lodestar was never matching the buggy behavior to begin with.

## Alternatives rejected

- **Reproduce the byte-length behavior.** Would require encoding every codex to UTF-8 before
  measuring it, for no reader-facing benefit and an actively wrong-looking result (a codex growing
  under truncation, or a length-3 gap check firing over unrelated character counts) that nothing
  in the 1977 description asks for and ADR 0003 already forbids deriving from a reference
  implementation's accident.
- **Widen the parity oracle to include byte-unsafe Unicode words and mark the mismatches as known
  divergences (the [`decision 0005`](0005-hamming-jellyfish-divergence.md) shape).** Rejected
  because 0005's divergence is on inputs Hamming and Jaro are asked to handle either way (equal or
  near-equal length strings with combining marks); this divergence is systematic and fully
  explained rather than an unresolved quirk, so pinning it as two direct facts against
  `MatchRatingApproach` itself is more informative than a mismatch count against a byte-length rule
  nothing downstream needs reproduced.
