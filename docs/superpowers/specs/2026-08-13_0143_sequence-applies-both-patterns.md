# 0143 — A `Split` + `ByteLevel` `Sequence` applies both patterns

**Issue:** [#143](https://github.com/CyrilB1531/data.net/issues/143) · **Found under:** [#105](https://github.com/CyrilB1531/data.net/issues/105) · **Date:** 2026-08-13

## Context

`TokenizerJsonLoader.ReadBpeSequencePreTokenizer` reads a `Sequence` of `Split` then `ByteLevel` by taking
the `Split` step's `pattern.Regex` and returning it as **the** pattern. `BpePreTokenizer` applies exactly
one regex, and the `ByteLevel` step's `use_regex` is never read on that path.

HuggingFace composes the two steps: `Split` produces pieces, and `ByteLevel` then re-splits each of them on
its own pattern unless `use_regex` is off. DataNet reproduces the `use_regex: false` row and diverges on
the `use_regex: true` row — which is the default, and what Llama-3 and Qwen2 ship.

This was found while measuring [#122](https://github.com/CyrilB1531/data.net/issues/122)'s question "what
does `use_regex: false` do when the file also declares a `Split` step". The answer was fine; the other row
was not.

## What the reference does

All measured against `tokenizers` 0.23.1 before this spec was written.

### D1 — inside a `Sequence`, `ByteLevel` re-splits what `Split` produced

`Split(" ", isolated)` then `ByteLevel(add_prefix_space=false)`, over `"hello123 don't"`:

| `use_regex` | pieces |
| --- | --- |
| `true` | `['hello', '123', 'Ġ', 'don', "'t"]` |
| `false` | `['hello123', 'Ġ', "don't"]` |

The `Split` step alone would give `['hello123', ' ', "don't"]`. The first row is that, with GPT-2's pattern
applied again inside each piece; the second row is that, untouched.

DataNet produces the second row on both, because it never applies a second pattern.

### D2 — the second split runs on raw text, before the bytes are mapped

`Ġ` is U+0120, `LATIN CAPITAL LETTER G WITH DOT ABOVE`, which is a `\p{L}`. So the two readings are
distinguishable: on byte-mapped text `aĠb` matches GPT-2's optional-space-then-letters alternative as one
piece, while on raw text `a b` splits in two.

`Split("\|", isolated)` then `ByteLevel(use_regex=true)`, over `"a b|c d"` — a `Split` chosen so the pieces
still contain spaces:

| reading | predicted | measured |
| --- | --- | --- |
| raw text | `['a', 'Ġb', '\|', 'c', 'Ġd']` | **this one** |
| byte-mapped text | `['aĠb', '\|', 'cĠd']` | — |

Both splits are therefore ordinary regex splits over the undecorated text, and the byte mapping happens
afterwards, per final piece. That is already how DataNet is arranged — `BpePreTokenizer.Split` runs on raw
text and `ByteLevelSymbols` maps each piece — so nothing about the ordering has to be invented.

### D3 — how much the divergence bites

GPT-2's pattern knows the contractions `'s`, `'t`, `'re`, `'ve`, `'m`, `'ll`, `'d` and nothing else. An
apostrophe followed by anything else is split off. Llama-3's and Qwen2's `Split` patterns keep it attached
through `[^\r\n\p{L}\p{N}]?\p{L}+`.

With Llama-3's actual pattern and `add_prefix_space=false`:

| text | HuggingFace | DataNet |
| --- | --- | --- |
| `j'ai vu l'ami d'Anne` | `['j', "'", 'ai', 'Ġvu', 'Ġl', "'", 'ami', 'Ġd', "'", 'Anne']` | `['j', "'ai", 'Ġvu', 'Ġl', "'ami", 'Ġd', "'Anne"]` |
| `aujourd'hui` | `['aujourd', "'", 'hui']` | `['aujourd', "'hui"]` |
| `C'est l'été` | `['C', "'", 'est', 'Ġl', "'", 'Ã©tÃ©']` | `['C', "'est", 'Ġl', "'Ã©tÃ©"]` |
| `O'Brien and D'Angelo` | `['O', "'", 'Brien', 'Ġand', 'ĠD', "'", 'Angelo']` | `['O', "'Brien", 'Ġand', 'ĠD', "'Angelo"]` |
| `rock'n'roll` | `['rock', "'", 'n', "'", 'roll']` | `['rock', "'n", "'roll"]` |
| `it's fine` | identical | identical |
| `don't` | identical | identical |
| `the 'quoted' word` | identical | identical |

Six of nine sample texts diverge. Qwen2 diverges identically. English contractions in GPT-2's list pass,
which is why nothing noticed: **every French elision fails**, and Irish and Italian names with them.

The failure is silent — different tokens, different ids, no exception — on the two byte-level models
[ADR 0017](../../decisions/0017-bpe-parity-scope.md) names as this library's parity targets.

### D4 — `add_prefix_space` sits *between* the two splits, and this lot does not move it

Measured, because a design that placed it wrongly would be a second silent divergence rather than a fix.

`Split("\|", isolated)` then `ByteLevel(add_prefix_space=true, use_regex=true)`:

| text | measured |
| --- | --- |
| `x\|'s` | `['Ġx', 'Ġ\|', "Ġ'", 's']` |
| `x\| already spaced` | `['Ġx', 'Ġ\|', 'Ġalready', 'Ġspaced']` |

The first row discriminates. Prepending the space **before** the second split turns the piece `'s` into
`" 's"`, which GPT-2's pattern then splits in two, because its contraction alternative only matches at the
start. Prepending it after would have kept `'s` whole and given `["Ġ's"]`. The second row shows the space
is not added to a piece that already begins with one.

So HuggingFace's order, per added-token-delimited segment, is: split by the `Split` pattern → per piece,
prepend a space where `add_prefix_space` is on and the piece does not already start with one → per piece,
re-split by `ByteLevel`'s pattern where `use_regex` is on → map the bytes.

**DataNet prepends per segment, before both splits.** That is a second divergence, it is
[#122](https://github.com/CyrilB1531/data.net/issues/122)'s to fix, and this lot leaves it exactly where it
is. The consequence for evidence is in *Evidence* below.

## Design

| Where | What |
| --- | --- |
| `BpeVocabulary` | Gains `PreSplitPattern`; keeps `PreTokenizerPattern`. Both `string?`, both in `Equals` and `GetHashCode`. |
| `BpePreTokenizer` | Takes both, compiles up to two regexes, and applies them in order — the second over the pieces the first produced. |
| `TokenizerJsonLoader.ReadBpeSequencePreTokenizer` | Reads `use_regex` on the `ByteLevel` step, which it ignores today, and returns the `Split` pattern as `PreSplitPattern` with `Gpt2` or `null` as `PreTokenizerPattern`. |
| `TokenizerJsonLoader.ReadByteLevelPreTokenizer` | Unchanged in behaviour: `PreSplitPattern` is null, `PreTokenizerPattern` is `Gpt2`. |
| `BpeFilesLoader` | `PreTokenizerPattern` keeps its value; `PreSplitPattern` is null. A `merges.txt` model has no `Sequence` to describe. |
| `BpeTokenizer` | Passes both to `BpePreTokenizer`. Nothing else changes. |

The rule, stated once: **both null is the classic `Whitespace` split; otherwise each non-null pattern
applies in order, `PreSplitPattern` first and `PreTokenizerPattern` over its pieces.**

| file shape | `PreSplitPattern` | `PreTokenizerPattern` |
| --- | --- | --- |
| absent, or `Whitespace` | `null` | `null` |
| bare `ByteLevel`, `use_regex: true` | `null` | `Gpt2` |
| `Sequence[Split(P), ByteLevel(use_regex: true)]` | `P` | `Gpt2` |
| `Sequence[Split(P), ByteLevel(use_regex: false)]` | `P` | `null` |

Two properties rather than one ordered list, because a list cannot say **whose** pattern a one-element
entry is, and `add_prefix_space` belongs to the `ByteLevel` step. `[Gpt2]` from a bare `ByteLevel` and
`[P]` from a `Sequence` with `use_regex` off are both one-element lists, and D4's rule places the prefix
space on opposite sides of them. Naming the two positions is what keeps that expressible.

### What this leaves for #122, deliberately

With "both null" meaning `Whitespace`, there is still no way to say **no split at all**, which is what
`use_regex: false` on a bare `ByteLevel` and an absent `pre_tokenizer` both need.

The cleanest route is to stop encoding `Whitespace` as a null and give it an explicit member on
`BpePatterns`, after which null means what it should. That is a second breaking change to the same type
and it belongs with the lot that needs it, not with this one. Recorded here so #122 does not have to
rediscover why the state is missing.

## Evidence

A corpus `bpe_sequence_split.json`, generated against `tokenizers` 0.23.1, models carried in
`metadata.models` — the shape #118, #119, #130 and #120 established.

Two models, `use_regex` on and off, over one text set:

1. **`j'ai vu l'ami d'Anne`, `aujourd'hui`, `C'est l'été`, `O'Brien`, `rock'n'roll`** — the divergence
   itself, on five shapes of the same cause rather than five spellings of one shape: elision before a
   vowel, elision before an `h`, an accented letter after the apostrophe, a capitalised name, and a
   double occurrence inside one word.
2. **`it's fine`, `don't`, `the 'quoted' word`** — the cases that must **not** move. Without them the
   corpus would prove that something changed, not that the right thing changed: a fix that split on every
   apostrophe would pass on the first group and fail here.
3. **`hello123 don't`** — the minimal case from D1, so the corpus carries the measurement the design was
   derived from rather than only its consequences.

All generated with **`add_prefix_space: false`**, and that is a decision rather than an omission. D4
established that HuggingFace prepends the space between the two splits while DataNet prepends it per
segment; with `add_prefix_space` on, every case above would measure that divergence on top of this one and
none of them would discriminate. [ADR 0022 §10](../../decisions/0022-added-token-matching-flags.md)
recorded the same reasoning when `bpe_added_token_flags.json` was generated with it off, for the same
reason, and hands the prefix-space rule to #105.

The four existing corpora that already carry `Sequence` models — `bpe_tokenizer_json.json`,
`bpe_added_tokens.json`, `bpe_added_token_flags.json`, `bpe_no_op_settings.json` — are the regression
proof. They pass today and must still pass after the fix.

Measured over their cases: between them they carry **not one apostrophe**, discriminating or otherwise.
That is why the defect survived four corpora, and it is also why they can serve as the regression proof —
they exercise the `Sequence` path on text where the two readings agree. Their continuing to pass is a
requirement of this lot rather than a prediction; a red one means the second pattern is being applied where
it should not be.

## Out of scope

**Everything #122 owns**, which is now sharpened rather than merely deferred: the no-split mode for
`use_regex: false` on a bare `ByteLevel` and for an absent `pre_tokenizer`, and the two prefix-space
rules including D4's.

**`Sequence` shapes other than exactly `[Split, ByteLevel]`**, which the loader refuses by name and this
lot leaves refused.

**A `Sequence` whose `Split` step declares `behavior` or `invert`.** The loader reads only
`pattern.Regex` today. Whether HuggingFace's `behavior` values other than the shipped one change the
pieces is unmeasured, and pretending otherwise would be the same class of untested premise that produced
this issue.

## Risks

- **The fix changes the tokens DataNet produces for Llama-3 and Qwen2 on ordinary text.** That is the
  point, but a caller who stored ids produced by an earlier version will find them no longer reproduced.
  The package is `0.3.0` and unreleased, so no published version is affected; the CHANGELOG entry has to
  say it plainly rather than describing a fix in the abstract.
- **`BpeVocabulary` takes a second additive change in one unreleased version**, after #104's breaking
  change to `AddedTokens` and #120's to `ContinuingSubwordPrefix`'s normalisation. Adding a property is
  the mildest of the three, and the alternative — waiting for #122 to do both at once — leaves a silent
  parity defect live on the flagship models for another lot.
- **Two regexes now run per piece where one ran before**, on the byte-level path only. The second runs
  over pieces the first already reduced, so the work is bounded by the same text; `RegexDefaults.MatchTimeout`
  still applies to both. No benchmark is proposed, because the pattern count is a property of the file
  rather than of the input, and the classic lineage is untouched.
