# 0007 — Metaphone: validation scope (real words)

**Status:** accepted · **Date:** 2026-08-01

## Context

[`Metaphone.Encode`](../reference/text/phonetics/metaphone-encode.md) reproduces
`jellyfish.metaphone` on **real words** — its domain of use. Unlike Soundex and NYSIIS (validated on 402 inputs, including random
strings), jellyfish's Metaphone exhibits, on **degenerate letter sequences**
(non-words: `"ghhh"`, `"Uugb"`, `"xhdzhumzj"`…), behaviors specific to its C
implementation, hard to distinguish from a quirk and of no practical value to
reproduce (e.g. handling of doubled initial vowels, or of an isolated `H` after an
already-consumed digraph).

## Decision

- **Validate Metaphone on a real-word corpus** (`metaphone.json`, ~120 English
  names and words chosen to cover the rules: `TH`, `CH`, `SH`, `PH`, `GH`/`GHT`,
  final `GN`, initial `KN`/`WR`/`PN`, `DGE`, `-TION`, `-SION`, final `MB`, `SCH`,
  initial `X`…). Exact parity with jellyfish on that corpus.
- The shared random corpus (`phonetics.json`) stays reserved for Soundex and
  NYSIIS, which reach 100% on it.

## Consequences

- [`Metaphone.Encode`](../reference/text/phonetics/metaphone-encode.md) is faithful to
  jellyfish for any real word — the intended use.
- Divergences on adversarial non-words are not reproduced; this is a deliberate,
  documented trade-off (§5 of the brief), not a regression.
