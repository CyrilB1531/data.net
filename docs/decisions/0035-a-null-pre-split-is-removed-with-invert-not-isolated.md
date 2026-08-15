# 0035 — A null pre-split drives `Apply` as Removed with invert, not Isolated

**Status:** accepted · **Date:** 2026-08-15

## Context

`BpePreTokenizer` runs two patterns: a pre-split first, and a second pattern that re-splits the
pieces it produced ([#143](https://github.com/CyrilB1531/data.net/issues/143)). Only the pre-split
carries a declared behaviour. A `null` pre-split still needs one, because `Apply` is driven by a
behaviour whatever the caller declared, and the choice was made in a comment rather than recorded —
[#187](https://github.com/CyrilB1531/data.net/issues/187) is what moved it here.

The two candidates look interchangeable, and are, whenever the pattern leaves no gap between its
matches. Every shipped byte-level pattern — `Gpt2`, `Llama3`, `Qwen2` — is of that kind.

## Decision

The default is **Removed with invert on** — "keep the regex matches, drop everything else" — never
Isolated.

`BpePatterns.Whitespace` is the pattern that separates them. Its `\w+|[^\w\s]+` never matches a run
of whitespace, so under Isolated that whitespace would surface as a piece of its own and reach the
merge loop as an uncovered symbol. Measured against `bpe.json` — the `" leading space"` case —
the reference produces no such piece, and no substituted token for one. Removed with invert keeps
that path byte-for-byte unchanged.

## Consequences

- The default is not exercised by `BpeSplitBehaviorTests`: every corpus case supplies its own
  `BpeSplitStep`, so the branch is reached only by a caller who declares no pre-split. A test that
  covers it would have to be written against the reference, not against the corpus.
- `BpePreTokenizer` carries a two-line comment pointing here, rather than the fifteen-line argument
  it used to carry inline.
