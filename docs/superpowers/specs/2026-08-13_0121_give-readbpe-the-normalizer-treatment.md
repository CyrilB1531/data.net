# 0121 — Give ReadBpe the normalizer treatment its siblings already have

**Issue:** [#121](https://github.com/CyrilB1531/data.net/issues/121) · **Date:** 2026-08-13 ·
**Branch:** `feat/121-bpe-normalizer` · **Lot 4 of:** [#105](https://github.com/CyrilB1531/data.net/issues/105)

## Context

`LoadBpe` refuses **any** normalizer wholesale (`EnsureBpeNormalizerIsAbsent`,
`TokenizerJsonLoader.cs:623`), where its siblings reproduce a named set and refuse the rest by name —
`ReadUnigramNormalizer` reads `Precompiled`, `ReadLowercaseFrom` reads `Lowercase`, a `Sequence` and a
plain `BertNormalizer`.

The issue asked which normalizers should make the reproduced set. That was answered by measuring sixteen
public `tokenizer.json` files rather than by intuition, and the measurement moved the lot twice.

### What the survey found

| Model | model type | `byte_fallback` | pre-tokenizer | normalizer |
| --- | --- | --- | --- | --- |
| EleutherAI/gpt-neox-20b | BPE | no | `ByteLevel` | **NFC** |
| EleutherAI/pythia-160m | BPE | no | `ByteLevel` | **NFC** |
| Qwen/Qwen2-0.5B | BPE | no | `Sequence[Split, ByteLevel]` | **NFC** |
| allenai/OLMo-1B-hf | BPE | no | `ByteLevel` | **NFC** |
| deepseek-ai/deepseek-coder-1.3b-base | BPE | no | `Sequence[Split×4, Digits, …]` | **`Sequence[]`, empty** |
| gpt2, roberta-base, bart-large, bloom-560m, falcon-7b, phi-2, starcoder2-3b, SmolLM2-135M, stablelm-2, codegen-350M | BPE | no | `ByteLevel` or a `Sequence` | `null` |

Five of sixteen declare one, four of them `NFC`. **None declares `byte_fallback` and none uses
`Metaspace`**, so all five are the lineage `BpeTokenizer` already implements: the blanket refusal is the
only thing stopping them. Qwen2 is the model [#143](https://github.com/CyrilB1531/data.net/issues/143) and
[#145](https://github.com/CyrilB1531/data.net/issues/145) are working on right now, and this library
refuses it over an `NFC`.

**Two corrections the survey forced, recorded because both were nearly built on.**

- The lot was first scoped around Llama-2's `Sequence[Prepend "▁", Replace " " → "▁"]` as "the only real
  uncovered case". [ADR 0017 §3](../../decisions/0017-bpe-parity-scope.md) says Llama-2 and
  Mistral v0.1 are SentencePiece BPE with a `Metaspace` pre-tokenizer — a third lineage, refused for
  `byte_fallback` *and* for its pre-tokenizer shape. Reproducing those two normalizers would unblock
  nothing: the file is refused two checks later.
- The design nearly refused "a normalizer together with a `normalized: true` added token" as a cheap way to
  avoid a second scanner. **`normalized: true` is the majority** in four of the five files — 23 of 25 in
  gpt-neox and pythia, 26 of 28 in OLMo, 22 of 22 in deepseek (Qwen2 has none). That refusal would have
  rejected four of the five files this lot exists to accept.

### The constraint the sibling cannot lend

`WordPieceTokenizer` normalizes the whole input once and **indexes the normalized string with positions
found in the raw text**. Its own comment says this is sound "only because `ToLowerInvariant` maps char to
char and so preserves length — an assumption about the scripts in scope, not a fact of Unicode". Every
normalizer here breaks it: NFC and NFKC recompose, NFD and NFKD decompose, and all four change length.

So BPE copies the sibling's *shape* — two scanners split by `AddedToken.Normalized` — but not its
mechanism. It normalizes **each gap in isolation**, which removes the assumption instead of extending it,
and which is what [ADR 0022 §10](../../decisions/0022-added-token-matching-flags.md) already settled: "added
tokens are split out first, raw entries against raw text and normalized entries against normalized text,
and only the gaps between them are normalized".

## Decisions

### D1 — the reproduced set is the four Unicode forms and `Sequence`

`NFC`, `NFKC`, `NFD` and `NFKD` are reproduced. Only `NFC` was observed, but all four are the same call —
`String.Normalize(NormalizationForm)`, present on both target frameworks, so **no `#if`** — and
reproducing one of four would be an arbitrary line a reader would have to ask about.

`Sequence` composes them, an **empty one included**: deepseek's `Sequence[]` does nothing and is refused
today for nothing, which is the same defect class [#118](https://github.com/CyrilB1531/data.net/issues/118)
closed for `continuing_subword_prefix: ""` and `dropout: 0.0`.

A declared sequence is applied **in its declared order**, not collapsed. Composing these four forms does
reduce to "the last one wins" through NFKC's idempotence, but a reader would have to verify that identity
to trust the code, and the loop costs nothing.

Refused, each named in its own message in the shape `ReadLowercaseFrom` already uses: `Replace`,
`Prepend`, `Strip`, `StripAccents`, `Lowercase`, `Precompiled`, `BertNormalizer`, `Nmt`, and any type the
reader does not know. `Replace` is the one worth a sentence in the message: its pattern may be a Rust
regex, whose flavour is not .NET's, and that divergence would have to be measured before it could be
promised.

### D2 — the vocabulary carries the forms, and the tokenizer applies them per gap

`BpeVocabulary` gains `NormalizationForms`, an ordered read-only list, empty when the file declares none.
It is public API, so the packaging gate applies: a member reference lands in `samples/DataNet.Sample/Lot*.cs`
in the same lot (ADR 0009).

`BpeTokenizer.Encode` takes the sibling's shape:

1. the **raw** scanner (entries with `Normalized == false`) runs over the raw text and emits the raw slice,
   unchanged from today and from ADR 0022 §2;
2. each gap between raw matches is **normalized on its own**;
3. the **normalized** scanner runs inside that normalized gap — its entries' own content normalized with
   the same forms, as `WordPieceTokenizer` does for lowercase;
4. what remains goes to `EncodeSegment`, where `add_prefix_space` and the split already live.

Normalization therefore happens **before** `add_prefix_space`, on gap text. That order is asserted here
and **measured in the implementation**, not assumed: a normalizer that emitted a leading space would
otherwise interact with the "only when the segment does not already begin with one" rule.

With no normalizer declared the list is empty, normalization is the identity, and the second scanner is
empty — so every file that loads today keeps its exact token stream. That is a claim the existing corpora
already test, and it must hold with **no oracle byte moving**.

### D3 — the round trip degrades to the normalized text, as it does in Python

`Decode(Encode(x))` returns `NFC(x)` rather than `x` once a normalizer is declared. This is accepted and
documented rather than refused: HuggingFace does the same, and refusing would put the four measured models
back where they are today. What must be proven is not that the round trip survives — it does not — but
that **it fails identically to Python's**, which the corpus measures on a text that normalization changes.

The byte-level guarantee itself is untouched: it is a statement about the bytes of the string the model
sees, and it still holds over the normalized text.

### D4 — the corpus is synthetic, and one fixture carries the case that matters

Fixtures are hand-built `tokenizer.json` files, as the rest of the BPE corpus is — no model weights, no new
`tools/fetch_*.py` (a real Qwen2 file is 11 MB, and the provenance rule is the repository's oldest).

Cases, generated from `tokenizers` 0.23.1:

- one fixture per form, on text the form actually changes: `e` + U+0301 against precomposed `é` (NFC/NFD),
  U+212B ANGSTROM SIGN (NFC folds it to `Å`), the `ﬁ` ligature U+FB01 and `①` (NFKC/NFKD only);
- a `Sequence` of two forms, and an **empty** `Sequence`;
- **a `normalized: true` added token beside a normalizer** — the gpt-neox shape, where the entry's own
  content must be normalized before it can match — together with a `normalized: false` entry in the same
  file, so the two halves are separated by evidence rather than by construction;
- a text where the normalizer changes the **token count**, not merely the characters, so a fixture that
  passes by accident is visible.

### D5 — the risk that decides whether there is an ADR

`String.Normalize` uses the platform's Unicode tables; Rust's `unicode-normalization` carries its own. On
rare code points the two can disagree, and this lot cannot promise parity it has not measured. The corpus
includes code points chosen to expose it. **If a divergence is found, it is an ADR** — the shape ADR 0017
and ADR 0022 already use — and the affected form is refused rather than reproduced wrongly. If none is
found, that is a sentence in `docs/equivalence.md`, not a decision document.

## Documentation

- `docs/equivalence.md` — `LoadBpe`'s row: the blanket normalizer refusal becomes a named set, with what is
  refused and why, and the round-trip consequence of D3.
- `docs/guides/` — the tokenizer guide gains the round-trip note, since it is user-visible behaviour.
- An ADR only under D5's condition.

## Out of scope

`use_regex: false` (lot 5, [#122](https://github.com/CyrilB1531/data.net/issues/122)) and the model settings
of lots 1-3 and 6. `Replace`, `Prepend`, `Strip`, `StripAccents` and `Lowercase`, which are refused by name
here and would each need their own measurement. `WordPieceTokenizer`'s length-preservation assumption,
which is correct for the one normalizer it applies and is **not** to be extended — noted here because this
lot is where a reader would be tempted to share the mechanism.

## Risks

- **The Unicode-table divergence of D5**, which is the one thing that could turn a small lot into an ADR.
- **`BpeTokenizer.Encode` is being edited in parallel** by #145, which has ~37 uncommitted lines in that
  file. This branch works in its own worktree and rebases on `main` after each of that lot's merges.
- **The second scanner is a real behaviour change for files that load today** — none of them can declare a
  normalizer, so the normalized scanner is empty for all of them, but the restructuring of `Encode` is not
  a no-op in shape. The existing corpora are the guard, and not one byte of them may move.
