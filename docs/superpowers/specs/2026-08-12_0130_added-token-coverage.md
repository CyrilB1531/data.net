# 0130 — An added token is a token, not a vocabulary entry

**Issue:** [#130](https://github.com/CyrilB1531/data.net/issues/130) · **Date:** 2026-08-12

## Context

`BpeTokenizer`'s constructor folds `AddedTokens` into `_vocab` (`BpeTokenizer.cs:86-90`), and seven call
sites then read that one map. They do not all want the same thing. Two ask *what id does this text carry*,
where the fold is right. Five ask *does the model cover this symbol*, where it is wrong: an added token
that `model.vocab` does not declare makes a character look covered, so it is never substituted with the
unknown token.

Found by the final review of #119, which measured it rather than inferring it.

## What the reference does

All measured against `tokenizers` 0.23.1. Vocabulary `{[UNK], a, b, ab}`, added token `Q` at id 4 with
`single_word: true`, `Q` absent from `model.vocab`.

| Question | Text | HuggingFace | DataNet today |
| --- | --- | --- | --- |
| Inside a word | `aQa` | `['a', '[UNK]', 'a']` | `['a', 'Q', 'a']` |
| A run around it | `ZQZ` | `['[UNK]', '[UNK]', '[UNK]']` | `['[UNK]', 'Q', '[UNK]']` |
| Standalone | `Q` | `['Q']` | `['Q']` |
| Standalone between words | `a Q a` | `['a', 'Q', 'a']` | `['a', 'Q', 'a']` |

The DataNet column is measured, not inferred: the final review of #119 built a probe against the packed
`net10.0` assembly and ran these texts through it.

The last two agree, and that is the point: the added-token scanner matches `Q` when `single_word` permits
it, and the character never reaches `InitialSymbols`. The divergence appears only where the scanner
correctly **declines** — which is what makes it invisible without a `single_word` fixture.

Under `single_word: false` the scanner matches inside words too, so `aQa` is `['a', 'Q', 'a']` on both
sides. That case is this change's own regression proof.

### D1 — the fold is load-bearing for identity

`token_to_id('Q')` is `4`, `id_to_token(4)` is `'Q'`, and `decode([4])` is `'Q'`. `TryGetId` documents
itself as matching `token_to_id`, and `Decode` reads the id → text table. **Both keep the folded view.**

### D2 — `unk_token` resolves against `model.vocab` alone, and the reference defers the check

A `tokenizer.json` whose unknown token appears only in `added_tokens` **loads**. `token_to_id` answers,
and encoding text the model covers succeeds. The failure comes later, from `encode`, and only when a
character actually needs substituting:

```text
Tokenizer.from_str(document)   ->  OK
encode("ab")                   ->  ['a', 'b']
encode("aZb")                  ->  Exception: Unk token `<unk>` not found in the vocabulary
```

An earlier draft of this spec said the reference refuses at construction. It does not; that measurement
came from a probe that called `encode` and attributed its exception to the constructor.

DataNet today is worse than either: the fold makes `<unk>` resolvable, so `_unkId` is the added token's id
and an uncovered character is silently substituted with it — a token stream where the reference raises.

**DataNet refuses at construction**, which is earlier than the reference and is a deliberate divergence in
*timing*, never in outcome: every file this refuses would have thrown from the reference on its first
input needing substitution. Failing at load rather than on some later input is the invariant this package
already documents — what it cannot reproduce fails at load with a message naming it — and a deferred
failure that depends on which text a caller happens to pass is the shape that invariant exists to avoid.
`docs/equivalence.md` records it as a divergence rather than as parity.

### D3 — merges resolve against `model.vocab` alone

A merge naming a token only `added_tokens` declares does not build:
``Error while initializing BPE: Token `Q` out of vocabulary``. DataNet resolves merge pairs through the fold.

### D4 — `ignore_merges` consults the model, not the added tokens

Vocabulary `{[UNK]: 0, a: 1, !: 2}`, added token `!!` at id 3 with `single_word: true`, `ignore_merges` on.
`Whitespace` splits `a!!` into `a` / `!!`; `single_word` declines the scanner on `a`, a word character, so
`!!` reaches the whole-piece shortcut while being an added token's exact content rather than a
`model.vocab` entry. Measured: `a!!` is `['a', '!', '!']` — the shortcut does not fire, and the piece falls
through to ordinary per-character encoding. The control, with `!!` carried in `model.vocab` too, gives
`['a', '!!']`: the shortcut fires once the model itself declares the entry.

An earlier version of this evidence cited `aQQa` with `ignore_merges` on, `['a', '[UNK]', '[UNK]', 'a']`.
That case does not discriminate: the same text with `ignore_merges` off gives the identical stream, so the
output was explained entirely by the `InitialSymbols` fix (the issue) and said nothing about which map the
shortcut itself consults.

### D5 — the comment justifying `SkippedMerges` is false

`BpeTokenizer.cs:116-118` says a merge naming a token the vocabulary does not contain is dropped because
"HuggingFace tolerates it, so refusing the file would be a divergence."

Measured on both paths — the constructor and `Tokenizer.from_str` over a `tokenizer.json`, which is the
one DataNet reproduces — HuggingFace raises ``Token `x` out of vocabulary``. It does not tolerate it.
`BpeVocabulary.SkippedMerges` therefore counts a case the reference makes impossible, and reads `0` on
every BPE fixture committed to this repository.

### D6 — a merge whose *result* is absent is refused too, and not the way the reference does it

D3 and D5 are about a merge naming an absent left or right side, which is what
``Token `x` out of vocabulary`` reports. A merge whose two sides are present but whose *concatenation* is
not in the vocabulary is a third shape, and there the reference does not raise: it panics —
`range end index 2 out of range for slice of length 1`, from `models/bpe/model.rs`, surfacing in Python as
`pyo3_runtime.PanicException`.

A panic is a bug in the reference, not behaviour to reproduce. DataNet refuses it, with a message of its
own naming the merge and saying the result is missing rather than pretending to quote HuggingFace. This is
the one refusal here that is not a transcription, and it is recorded as such in `docs/equivalence.md`.

Both loaders already compute their skipped count against the model vocabulary alone and check only the two
sides, never the result — so this shape reaches `BpeTokenizer`'s constructor today and is silently dropped
there.

## Design

### Two views of the vocabulary

`_vocab` stays exactly as it is. A second map, built from `vocabulary.Vocab` alone, answers the coverage
question. No public API is added; the cost is one hash table per tokenizer, built once.

| Call site | View | Why |
| --- | --- | --- |
| `TryGetId` (`:196`) | folded | reproduces `token_to_id`, which returns `4` for `Q` (D1) |
| `_tokens`, the id → text table | folded | `decode([4])` is `'Q'` (D1) |
| `InitialSymbols` (`:334`) | **model** | the issue |
| `ignore_merges` (`:254`) | **model** | D4 |
| `ByteLevelSymbols` (`:388`) | **model** | an added token does not make a model byte-level |
| merge resolution (`:119-121`) | **model** | D3 |
| `unk_token` resolution (`:103`) | **model** | D2 |

### Two messages, three decisions, in the reference's words

``Unk token `X` not found in the vocabulary`` for D2, and ``Token `X` out of vocabulary`` for D3 and D5.
`ArgumentException` for HuggingFace's build error, matching how this package already maps a refusal.

### `SkippedMerges` is removed

The property counted a case that cannot occur once D5's refusal lands. It arrived with #59, in
`DataNet.Embeddings` 0.3.0, which is still unreleased — the last tag is 0.2.0 — so no published consumer
can reference it. Removed from `BpeVocabulary`, from its `Equals` and `GetHashCode`, and from
`samples/DataNet.Sample/Lot3Embeddings.cs:130`, which the packaging gate would otherwise fail on.

**This sets a precedent lot 3 (#120) faces next**, for `ContinuingSubwordPrefix`: a public property on an
unreleased version that can never be true is removed rather than kept. #105's gap list records both.

## Sequencing against #120

Lot 3 changes what the merge rank table is **keyed on**, so that a prefixed symbol can be found. This
change alters **which map** those same lines look in. They do not conflict, but this one should land
first: otherwise #120 builds its new keying on a resolution already known to be wrong, and would have to
redo it.

## Evidence

A corpus `bpe_added_token_coverage.json`, generated by a `generate_bpe_added_token_coverage` section
against `tokenizers` 0.23.1, with the models built inside the generator and carried in
`metadata.models` — the shape #118 and #119 established.

Two models over the same vocabulary and added token, differing only in `single_word`:

- `single_word: true`, where the scanner declines inside a word and the divergence is live;
- `single_word: false`, where the scanner matches and both sides already agree, which makes the
  untouched path its own regression proof.

Texts: `aQa`, `ZQZ`, `QQ`, `Q`, `a Q a`, and `aQ` — the last so that an added token at the end of a piece
is covered as well as in the middle.

The three refused shapes are recorded the way #118 records its own: the exact document handed to `tokenizers`,
and the error it answered with, so that "the reference refuses this too" stays a measurement rather than a
claim in a commit message.

## Out of scope

`WordPieceTokenizer`, which carries its added tokens in a different structure and is not affected. The
added-token scanner itself, which is already correct — the divergence is visible precisely because it
declines correctly under `single_word`. Everything under umbrella #105: the model settings of lots 1-3
and 6, the normalizer of lot 4, and the no-split mode of lot 5.

## Risks

- **Removing a public property**, even an unreleased one. Mitigated by the version being untagged and by
  the property reading `0` on every fixture, but it is the one irreversible part of this change.
- **A file that loads today stops loading.** Any `tokenizer.json` with an orphan merge, or with its
  unknown token only in `added_tokens`, is refused after this. No fixture in the repository has either —
  verified by scanning every committed BPE model — and the reference refuses both, so a file that stops
  loading here never worked in Python either.
