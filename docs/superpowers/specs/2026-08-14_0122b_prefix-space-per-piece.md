# 0122b — The prefix space belongs to the ByteLevel step

**Issue:** [#122](https://github.com/CyrilB1531/data.net/issues/122) · **Umbrella:** [#105](https://github.com/CyrilB1531/data.net/issues/105) · **Date:** 2026-08-14

The second and last half of #122. The first half — a pre-tokenizer that does not split — shipped as
[PR #165](https://github.com/CyrilB1531/data.net/pull/165) and deliberately left this rule open, because it
needs a `Split` step to be observable and does not interact with a mode that has no split at all.

## Context

`add_prefix_space` is applied to the whole added-token segment here, and to each `Split`-produced piece in
HuggingFace. Three places in the tree park the divergence on this issue rather than hiding it:
`docs/equivalence.md`'s `Sequence([Split(pattern), ByteLevel(use_regex=True)])` row,
`TokenizerJsonLoader`'s `ReadBpeSequencePreTokenizer`, and the first half's spec under *Out of scope*.

## What is measured

All against `tokenizers` 0.23.1 on 2026-08-14, over the byte-level alphabet with no merges and
`Sequence[Split("|", Isolated, invert=False), ByteLevel(add_prefix_space=true, use_regex=…)]`.

### D1 — the reference prepends per piece, not per text

| text | `use_regex` | pieces |
| --- | --- | --- |
| `"ab\|cd"` | off | `['Ġab', 'Ġ\|', 'Ġcd']` |
| `"ab\|cd"` | on | `['Ġab', 'Ġ\|', 'Ġcd']` |
| `"a b\|c d"` | off | `['ĠaĠb', 'Ġ\|', 'ĠcĠd']` |
| `"a b\|c d"` | on | `['Ġa', 'Ġb', 'Ġ\|', 'Ġc', 'Ġd']` |

Three pieces, three spaces. DataNet produces one, at the front of the segment.

### D2 — a piece that already begins with a space does not gain another

| text | pieces | why |
| --- | --- | --- |
| `"ab\| cd"` | `['Ġab', 'Ġ\|', 'Ġcd']` | the `Split` gives `ab`, `\|`, `" cd"`; the third already starts with a space, so its `Ġ` is the text's own |
| `" ab\|cd"` | `['Ġab', 'Ġ\|', 'Ġcd']` | the first piece keeps the text's space; the other two gain one |
| `"a\| \|b"` | `['Ġa', 'Ġ\|', 'Ġ', 'Ġ\|', 'Ġb']` | the piece that **is** a single space gains nothing and maps to one `Ġ` |

This is the same rule DataNet already applies — prepend unless the string already begins with a space —
at a different granularity. The rule does not change; where it is applied does.

### D3 — the divergence survives the round trip, which is how a user meets it

| text | reference decodes to | DataNet decodes to |
| --- | --- | --- |
| `"ab\|cd"` | `" ab \| cd"` | `" ab\|cd"` |
| `"a\|b\|c\|d"` | `" a \| b \| c \| d"` | `" a\|b\|c\|d"` |

One space per piece, every one surviving `Decode`. A reader needs no knowledge of pre-tokenizers to see
that the second column is wrong.

### D4 — no shipped model reaches it

Every published `Sequence[Split, ByteLevel]` model that could be read declares `add_prefix_space: false`:

| model | source | `add_prefix_space` | `use_regex` |
| --- | --- | --- | --- |
| Llama-3-8B | `NousResearch/Meta-Llama-3-8B` and `unsloth/llama-3-8b`, byte-identical | `false` | `false` |
| Qwen2-0.5B | `Qwen/Qwen2-0.5B` | `false` | `false` |
| Qwen2.5-0.5B | `Qwen/Qwen2.5-0.5B` | `false` | `false` |
| deepseek-coder-1.3b-base | `deepseek-ai/deepseek-coder-1.3b-base` | `false` | `false` |

`meta-llama/Meta-Llama-3-8B` is gated and returns 401, so it was read from the two ungated mirrors
[ADR 0017 §5](../../decisions/0017-bpe-parity-scope.md) already established for exactly this purpose.

**So this lot fixes a wrong answer nobody is currently getting from a published file.** It is reachable
from `BpeVocabulary` in three lines, which is why it is worth fixing rather than documenting — but the
corpus must not be read later as proof that a real model needed it.

## Design

### One rule, three shapes

`add_prefix_space` is a property of the **`ByteLevel` step**, and prepends one space to each piece handed
to that step unless the piece already begins with one. Where the step sits decides everything else:

| vocabulary | pieces reaching `ByteLevel` | result |
| --- | --- | --- |
| bare `ByteLevel` (GPT-2) | the whole segment | one space at the front — unchanged |
| `NoPreTokenizer` | the whole segment | one space at the front — unchanged |
| `PreSplit` declared | the `Split` step's output | **one per piece — the fix** |

The position depends on **whether a `PreSplit` is declared**, and on nothing else. It does not depend on
whether a second pattern exists: `Sequence[Split, ByteLevel(use_regex: false)]` — Llama-3's own shape —
has a `ByteLevel` step that contributes no regex, and the prefix space still applies to each `Split`
piece, as D1's `use_regex` off row measures.

### The move

`BpeTokenizer.EncodeSegment` prepends to the segment and then calls `BpePreTokenizer.Split`. That is
correct exactly when nothing precedes the `ByteLevel` step, which is why GPT-2 parity holds today and the
`Sequence` shape does not.

`BpePreTokenizer` takes the flag instead. It already knows whether a `PreSplit` was declared — the one
thing the position depends on — so the rule lands where the knowledge already is, and `EncodeSegment`
loses a special case rather than gaining one.

The empty-segment guard stays in `EncodeSegment`. It exists because prepending to an empty segment would
emit a `Ġ` the reference does not — for the empty string, and for the gap between two adjacent added
tokens — and that reasoning is about segments, not pieces. Commit `022552b` measured it.

**This rule has moved once already, and for the same reason.** `022552b` moved it from once-per-input to
once-per-segment, having measured that HuggingFace prepends inside the `ByteLevel` pre-tokenizer, after
the added-token scan has cut the input. That was right as far as it went: with no step before `ByteLevel`,
per-segment is per-piece. This lot is the last step of the same migration, and it is the one that stops
the rule being a coincidence.

## Evidence

A corpus `bpe_prefix_space.json`, models carried in `metadata.models`, on the shape #118 through #145
established. Each case records `pieces` as well as `tokens`, `ids` and `decoded`: the rule is about
pieces, and a token list alone would not localise a failure.

| model | what it pins |
| --- | --- |
| `presplit_aps` | the divergence, `use_regex` off — Llama-3's shape |
| `presplit_aps_regex` | both patterns *and* the prefix space, so the interaction is measured rather than assumed |
| `presplit_no_aps` | the control: what every shipped model actually declares |
| `bare_aps` | GPT-2's shape, which must not move — the regression guard |
| `no_split_aps` | the boundary with the first half's mode, where one piece means one space |

Texts, each making one rule falsifiable on its own: `"ab| cd"` and `" ab|cd"` for a piece that already
starts with a space, `"a| |b"` for a piece that **is** a space, `"a|b|c|d"` for D3's round trip, and a
text the `Split` pattern never matches — which must come out identical across all five models, proving
the three shapes coincide when there is nothing to split.

## Out of scope

**`AddPrefixSpace` without `ByteLevel`.** `BpeVocabulary` lets a hand-built vocabulary declare
`AddPrefixSpace = true, ByteLevel = false`. HuggingFace cannot express that at all — the flag lives on the
`ByteLevel` step, so without one there is no flag — which means there is no reference behaviour to measure
against and nothing to be at parity with. It is a pre-existing divergence, older than this issue, and
naming it here is all this lot does about it.

**`dropout`** ([#123](https://github.com/CyrilB1531/data.net/issues/123)) and the rest of #105's lots.

## Risks

- **The change is on the encode path**, which every corpus exercises. That is the mitigation as much as
  the risk: if the move is right, **every existing corpus stays green**, because none of them pairs a
  `PreSplit` with `add_prefix_space: true`. A red corpus means the move is wrong, not that a corpus is
  stale — do not regenerate one to make it pass.
- **`bare_aps` is the guard that matters.** GPT-2 parity is end-to-end over a 50 257-entry vocabulary
  (ADR 0017), and it runs through the same code this lot moves. It must be byte-identical afterwards.
- **Nothing published exercises the fixed path**, so the corpus is the only thing standing behind it.
  That raises rather than lowers the bar on the corpus discriminating: `presplit_aps` and
  `presplit_no_aps` must differ on every text that has a `Split` match, and agree on the text that has
  none. A pair that agrees everywhere measures nothing — the trap the first half hit twice, once in the
  byte-level pair and once in the added-token model.
