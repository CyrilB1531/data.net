# 0002 — Unicode comparison unit

**Status:** accepted · **Date:** 2026-08-01

## Context

This is porting pitfall #1 (§5). In Python 3, a `str` iterates over **code
points**. In C#, a `string` iterates over **UTF-16 code units**: a character
outside the Basic Multilingual Plane (emoji, rare ideograph) is a surrogate pair
and occupies two positions. A naive edit distance in C# therefore diverges from
Python on such inputs.

Three possible units: UTF-16 unit (`char`), code point (Unicode scalar), or
grapheme cluster (perceived character, e.g. emoji + skin-tone modifier).

## Decision

- **Default: UTF-16 unit** (`TextElement.Utf16Unit`). It is the native .NET
  choice, allocation-free, and agrees with Python for all BMP text — the vast
  majority of real cases.
- **Exact parity with Python: code point** (`TextElement.CodePoint`), offered on
  every affected algorithm. Cost: one decode pass into a pooled buffer
  (`ArrayPool`). This is the mode the oracle suite replays (rapidfuzz works on
  code points).
- **Grapheme cluster: deferred.** Requires `StringInfo`/segmentation, allocates,
  and has no direct Python oracle in the targeted libraries. To be added if a
  concrete need appears (e.g. "perceived" comparison of composite emoji).

Lone surrogates are preserved as-is (unit value), like a Python `str`, rather than
throwing.

## Consequences

- Documentation and the equivalence table explicitly flag the default mode and
  when to switch to `CodePoint`.
- Oracle corpora are generated with code-point semantics and replayed with
  `TextElement.CodePoint`; dedicated unit tests verify the expected UTF-16 vs
  code-point divergence on supplementary input.
