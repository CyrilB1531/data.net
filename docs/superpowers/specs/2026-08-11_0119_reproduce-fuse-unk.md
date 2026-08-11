# 0119 — `fuse_unk`, so a run of uncovered characters collapses the way HuggingFace collapses it

**Issue:** [#119](https://github.com/CyrilB1531/data.net/issues/119) · **Umbrella:** [#105](https://github.com/CyrilB1531/data.net/issues/105) · **Date:** 2026-08-11

## Context

`TokenizerJsonLoader.LoadBpe` refuses a model that declares `fuse_unk`
(`TokenizerJsonLoader.cs:601`). HuggingFace collapses a run of consecutive uncovered characters into a
single unknown token; `BpeTokenizer.InitialSymbols` always emits one per code point (`BpeTokenizer.cs:329`),
which is the
`fuse_unk: false` behaviour. This is lot 2 of #105, and the second-least risky: it touches one place, and
every corpus already committed was generated with the flag off, so all of them are the regression proof
for the untouched path.

The issue listed four things to decide by measurement rather than by reasoning. All four were measured
against `tokenizers` 0.23.1 before this spec was written, and the measurements turned up two more that the
issue did not anticipate — one of which is the difference between a correct implementation and a plausible
one.

## The measurement, and what it returned

Vocabulary `{[UNK], a, b, ab, a</w>, b</w>, ab</w>}`, merge `(a, b)`, `Z` uncovered.

| Question | Answer |
| --- | --- |
| A run in the middle — `aZZZa` | `['a','[UNK]','[UNK]','[UNK]','a']` → `['a','[UNK]','a']` |
| A single unknown — `aZa` | identical under both flags |
| A run at either end — `ZZa`, `aZZ` | fuses |
| A whole uncovered text — `ZZZ` | `['[UNK]']` |
| Two runs split by a covered character — `ZZaZZ` | `['[UNK]','a','[UNK]']`, the two runs kept apart |

### D1 — a run stops at the pre-tokenizer's boundary

With a `Whitespace` pre-tokenizer, `"aZ Za"` gives `['a','[UNK]','[UNK]','a']` under **both** flags: the two
uncovered characters are in different pieces and do not fuse. Without a pre-tokenizer the space is itself
uncovered, the run is `Z`-space-`Z`, and all three collapse into one token.

Fusing is therefore **per piece**, which DataNet gets for free: `InitialSymbols` is already called once per
piece.

### D2 — the fused token spans the whole run, and this lot does not expose it

Offsets are `(1, 4)` for a three-character run, indexed in Python `str` code points rather than UTF-16
units — `a😀😁a` gives `(1, 3)`.

`TokenizationResult` is `(Tokens, Ids)`. **DataNet exposes no offsets anywhere in the tokenization path**,
so the issue's second question has no consequence here. It is recorded because a future offsets feature has
to reproduce it, and because measuring it cost nothing.

### D3 — fusing happens *before* merging, and the fused symbol merges

This is the finding that separates a correct implementation from a plausible one. With a vocabulary
containing `[UNK]a` and the merge `([UNK], a)`:

| text | `fuse_unk: false` | `fuse_unk: true` |
| --- | --- | --- |
| `ZZa` | `['[UNK]', '[UNK]a']` | `['[UNK]a']` |

An implementation that fused *after* the merge loop would return `['[UNK]', 'a']` — plausible, and wrong.
DataNet gets the right order for free as long as the fusing happens in `InitialSymbols`, because `Merge`
runs on the array `InitialSymbols` fills.

### D4 — the trigger is "this symbol was substituted", not "this id is the unknown id"

The second unanticipated finding, and the one that will catch a natural implementation. `InitialSymbols`
handles ids, so the obvious test — *is the previous symbol already `_unkId`?* — is wrong whenever the
unknown token is itself a covered single character.

Measured with `unk_token: "?"` where `?` is also a legitimate vocabulary entry:

| text | `fuse_unk: true` | |
| --- | --- | --- |
| `?Z` | `['?', '?']` | does **not** fuse |
| `Z?` | `['?', '?']` | does **not** fuse |
| `ZZ` | `['?']` | fuses |

The rule is carried as a flag on the previous iteration, not read back out of the symbol array.

### D5 — `fuse_unk` without an `unk_token` is accepted, and does nothing

Representable and serializable: `{'unk_token': None, 'fuse_unk': True}`. Uncovered characters are dropped,
and the flag has no observable effect — `['a', 'a']` either way. DataNet accepts it rather than refusing
it, because refusing it would be a divergence invented rather than reproduced.

### D6 — the byte-level path is untouched

All 256 byte-level alphabet characters are covered, so no unknown can arise and `fuse_unk` is a no-op
there. Confirmed: identical output under both flags. `ByteLevelSymbols` gains nothing.

### D7 — the end-of-word suffix never reaches the unknown token

`"aZZ"` with `end_of_word_suffix: "</w>"` gives `['a','[UNK]']`, not `['a','[UNK]</w>']` — the suffix is
appended to the raw character *before* the lookup, so a suffixed uncovered character is still uncovered and
still substituted. This already matches `InitialSymbols`, and is recorded so that the corpus pins it.

## Design

| Where | What |
| --- | --- |
| `BpeVocabulary` | A `FuseUnk` `bool` in `init` position, beside `ContinuingSubwordPrefix` and `UnkToken`, in the value equality and the hash like its neighbours. |
| `TokenizerJsonLoader.LoadBpe` | Stops refusing `fuse_unk` and reads it. |
| `BpeFilesLoader` | Untouched: `merges.txt` has no field to declare it in, so it stays `false`. |
| `BpeTokenizer.InitialSymbols` | The one behavioural change: the `else if (_hasUnk)` branch does not write a symbol when the previous iteration substituted one. |

Nothing else moves. `Merge`, `Decode`, `ByteLevelSymbols` and the added-token scanner are unaffected.

## Evidence

A new corpus, `bpe_fuse_unk.json`, generated by a `generate_bpe_fuse_unk` section against
`tokenizers` 0.23.1, replaying every text **under both values of the flag** — which makes the current
behaviour its own regression proof rather than requiring a second fixture for it.

Two hand-built models, on the pattern of `orphan_bpe_model.json`, because training never produces either:

1. `fuse_unk_bpe_model.json` — covers `a`, `[UNK]`, `[UNK]a`, with the merge `([UNK], a)`, so D3 is pinned
   by something. Without it the order of fusing and merging is a claim in this document that nothing
   verifies.
2. A second model whose `unk_token` is a covered single character, so D4 is pinned. Without it the natural
   id-comparison implementation passes every other case.

Texts: a run in the middle, at the start, at the end, a whole uncovered text, a single unknown, two runs
separated by a covered character, a run spanning a pre-tokenizer boundary, an astral run, a run touching a
merge, and the unknown token written literally in the text.

## Documentation

The `LoadBpe` row of `docs/equivalence.md` loses `fuse_unk` from what it refuses.

**No ADR.** This is parity, and parity needs none. ADR 0017 §3's sentence on `byte_fallback` is re-read so
that it does not end up contradicting this lot — the two answer the same question, "what becomes of a
character the vocabulary does not cover", and one stays refused while the other is reproduced.

## Out of scope

`continuing_subword_prefix` (lot 3), `dropout` (lots 1 and 6), `normalizer` (lot 4), `use_regex: false`
(lot 5), and `byte_fallback`, which remains refused for ADR 0017 §3's reason. Offsets, which this library
does not expose.

## Risks

- **The id-comparison trap (D4).** The natural implementation is wrong and passes every test that does not
  use a single-character unknown token. Mitigated by the second model, which exists only for this.
- **The order trap (D3).** Equally natural to get wrong, invisible without a merge whose left side is the
  unknown token. Mitigated by the first model.
- **A vocabulary declaring both `fuse_unk` and `byte_level`.** Not refused, and a no-op by D6 — because
  every byte is covered — rather than by a guard. Worth one test, so that it stays a no-op deliberately
  rather than because nothing exercises it.
