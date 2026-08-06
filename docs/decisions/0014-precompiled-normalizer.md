# 0014 — Interpret the `precompiled_charsmap`, do not reimplement the rules

**Status:** accepted · **Date:** 2026-08-06

## Context

`SentencePieceTokenizer` named ALBERT, T5, camemBERT and XLM-R in its
documentation, and `SentencePieceModelLoader` refused all four. Every stock model
of those families is trained with `nmt_nfkc`, the `spm_train` default, and the
loader accepted `identity` alone — correctly, since normalizing differently from
the reference produces different embeddings while looking like it works.

[#75](https://github.com/CyrilB1531/data.net/issues/75) asked for that gap to be
closed, and required the premise to be measured before anything was designed.
Five real models — `t5-small`, `albert-base-v2`, `camembert-base`,
`xlm-roberta-base`, `google/mt5-small`:

| | Finding |
| --- | --- |
| `normalizer_spec.name` | `nmt_nfkc` on all five |
| `precompiled_charsmap` | 237 539 bytes on all five, **byte-identical** (same SHA-256) |
| Loadable before this change | none |

The one `spiece.model` this library could read was the one it had trained itself.

## The choice

The issue offered two routes: interpret the compiled map, or reimplement the
named rules on top of `string.Normalize(NormalizationForm.FormKC)` plus the NMT
adjustments.

The measurement that settled it compares the map against Python's NFKC — the same
algorithm .NET exposes — over all 149 251 assigned code points, with the
whitespace flags off so only the map speaks. **181 code points differ (0.121 %)**,
in three families:

| Family | Count | Examples |
| --- | ---: | --- |
| Dropped by the map, kept by NFKC | 30 | U+0001…U+001F |
| Turned into a space by the map | 15 | U+0009, U+1680, U+200B…U+200F, U+FEFF, U+FFFD, and U+2581 `▁` itself |
| Kept by the map, changed by NFKC | 136 | U+32FF `㋿`, U+A7F2…, the U+10780 block |

The first two families are the NMT adjustments — a small fixed table, exactly
what Route B would hand-write. The third is the one that decides:

**Those 136 code points were added to Unicode after the map was compiled.**
U+32FF arrived in Unicode 12.1, U+A7F2… and U+10780… in Unicode 14. The map is
frozen at the Unicode version of the `sentencepiece` build that produced it;
`string.Normalize` follows the runtime's ICU. The gap is therefore not a table to
patch once — it grows with every Unicode release, and differs between .NET
versions and platforms for the same input and the same model file.

Byte-exact parity, which is this library's contract and the issue's acceptance
criterion, is unreachable that way by construction rather than by effort.

## Decision

**Interpret the blob.** `PrecompiledNormalizer` reads the darts-clone
double-array trie and the replacement strings it indexes, and applies the same
longest-match walk as `sentencepiece`'s `Normalizer`.

Three consequences follow from the shape of that choice rather than from extra
work:

- **Every rule is covered by one implementation.** `nmt_nfkc`, `nfkc`, their
  `_cf` variants and any `--normalization_rule_tsv` compile to the same kind of
  blob. `tests/oracles/custom_norm.model` — three hand-written rules, and a
  `normalizer_spec.name` of merely `user_defined` — is in the corpus to keep that
  claim honest.
- **Validation stays anchored to content.** Nothing is decided from
  `normalizer_spec.name`, which preserves the property the old `|| hasCharsMap`
  guard encoded: a file that declares one thing and carries another cannot slip
  through by naming itself well.
- **`tokenizer.json` agrees with `spiece.model`.** HuggingFace writes the same
  blob, base64-encoded, as `{"type": "Precompiled"}`. `TokenizerJsonLoader` reads
  it through the same class, so the two formats no longer disagree about the same
  model.

What is refused is now the case that cannot be applied, not the case that was not
enumerated: a normalizer named without a map to apply, a map that will not parse,
and — unchanged — `NFKC` in a `tokenizer.json`, which asks for the runtime's
tables where the model asked for a frozen map.

## Two divergences found while doing it

Both were found by the new corpus, not by reading, and both are fixed in the same
change. Neither was reachable before: a corpus of self-covered ASCII under a
normalizer that did nothing cannot show either.

- **Whitespace.** Preprocessing split on every Unicode space. `sentencepiece`
  splits on U+0020 and nothing else — under `identity`, `"a\tb"` keeps its tab as
  an ordinary character that the vocabulary either covers or does not.
- **Unknown runs.** `sentencepiece` emits one unknown piece per *run* of
  uncovered characters, not one per character: full-width `ＬＥ` against a
  vocabulary that does not cover it is a single token.

## Consequences

- `tests/oracles/xlmr_fairseq.model` keeps the stock `nmt_nfkc` map rather than
  having it overwritten with `identity`, which is what
  [`0013`](0013-sentencepiece-parity-scope.md) said to revisit on the day this
  landed. The fixture is now the stock XLM-R pipeline with the vocabulary
  relabelled — one transformation instead of two — and grows by 237 KB.
- `docs/equivalence.md` states parity over the models that load, and the
  `identity`-only restriction is gone from it.
- The normalization pass costs one trie walk per input byte, on text that is
  usually short relative to the Viterbi search that follows.
- A model whose map this interpreter cannot read is refused rather than partly
  normalized. Half-normalized text is the same silent failure as no
  normalization, with a better disguise.
- The 136 divergences will grow. That is a property of the reference, not a
  defect here: `sentencepiece` itself applies the frozen map, so following it is
  what parity means.
