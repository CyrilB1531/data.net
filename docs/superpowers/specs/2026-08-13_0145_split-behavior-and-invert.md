# 0145 — A `Split` step's `behavior` and `invert`

**Issue:** [#145](https://github.com/CyrilB1531/data.net/issues/145) · **Found under:** [#105](https://github.com/CyrilB1531/data.net/issues/105) · **Date:** 2026-08-13

## Context

`BpePreTokenizer.Apply` keeps the regex matches and nothing else:

```csharp
IEnumerable<Match> matches = pattern.Matches(text).Cast<Match>();
pieces.AddRange(matches.Select(m => m.Value));
```

`TokenizerJsonLoader.ReadBpeSequencePreTokenizer` reads the `Split` step's `pattern.Regex` and nothing
else — `behavior` and `invert` are never read.

Found by the final review of [#143](https://github.com/CyrilB1531/data.net/issues/143), which had just
made a `Sequence` apply both its patterns and needed to know whether its equivalence row overclaimed
parity. It did.

## What the reference does

All measured against `tokenizers` 0.23.1 before this spec was written.

### D1 — DataNet already implements one exact combination, unconditionally

`Split` takes `invert` as well as `behavior`. DataNet's "keep the matches and nothing else" is not the
absence of a behaviour; it is precisely one:

```text
Split(pattern, behavior="Removed", invert=true)
```

Checked over 7 patterns × 13 texts = **91 combinations, zero mismatches** against
`[m.Value for m in pattern.Matches(text)]`.

So the defect is not that DataNet implements no behaviour. It is that it implements **one** of the ten,
always, and never reads which one the file declares.

### D2 — the ten combinations

Over `"ab cd!"` with a pattern of `\w+`, whose matches are `['ab','cd']` and whose gaps are `[' ','!']`:

| `behavior` | `invert: false` | `invert: true` |
| --- | --- | --- |
| `Isolated` | `['ab', ' ', 'cd', '!']` | same |
| `Contiguous` | `['ab', ' ', 'cd', '!']` | same |
| `Removed` | `[' ', '!']` | `['ab', 'cd']` — **what DataNet always does** |
| `MergedWithPrevious` | `['ab', ' cd', '!']` | `['ab ', 'cd!']` |
| `MergedWithNext` | `['ab ', 'cd!']` | `['ab', ' cd', '!']` |

### D3 — `invert` swaps the roles of match and gap, and nothing else

The table above is not five rules and five exceptions. One model produces every cell:

1. Segment the text into alternating **gaps** and **matches**.
2. If `invert`, swap the two labels.
3. Apply the behaviour.
4. Drop empty pieces.

That reading predicts, and the measurements confirm, all three of the table's oddities: `invert` is a
no-op for `Isolated` because both kinds are kept either way; it is a no-op for `Contiguous` because two
gaps are never adjacent, so there is nothing for the merge to do; and it exchanges `MergedWithPrevious`
with `MergedWithNext`, because "a match joins the preceding gap" with the labels swapped is "a gap joins
the preceding match", which is the same thing as "a match joins the following gap".

### D4 — `Isolated` and `Contiguous` are genuinely different

They agree on `"ab cd!"`, which is why one example is not enough. They part wherever two matches are
adjacent:

| pattern | text | `Isolated` | `Contiguous` |
| --- | --- | --- | --- |
| `X` | `aXXb` | `['a', 'X', 'X', 'b']` | `['a', 'XX', 'b']` |
| `\.` | `a..b` | `['a', '.', '.', 'b']` | `['a', '..', 'b']` |
| `[abc]` | `abc` | `['a', 'b', 'c']` | `['abc']` |

So reproducing `behavior` is four distinct arrangements, not two.

### D5 — both fields are required, and the reference refuses their absence

A `Split` step serializes as `{"type":"Split","pattern":{"Regex":"\\w+"},"behavior":"Isolated","invert":false}`.
Handed a document with either field removed, `Tokenizer.from_str` refuses:

| omitted | error |
| --- | --- |
| `behavior` | ``missing field `behavior` `` |
| `invert` | ``missing field `invert` `` |
| both | ``missing field `behavior` `` |

There is therefore **no default to invent**. DataNet refuses an absent field, which is the same shape as
its existing refusal of an absent `add_prefix_space` on a `ByteLevel` step, taken for the same reason.

### D6 — the file spells the behaviour in PascalCase, and unknown variants are refused

`Removed`, `Isolated`, `MergedWithPrevious`, `MergedWithNext`, `Contiguous`. The snake_case spellings are
the Python constructor's API, not the format: a document declaring `"isolated"` is refused with
``unknown variant `isolated` ``, as is `"Nonsense"`.

### D7 — the edge cases the segmentation has to answer

| pattern | text | `Isolated` | `Removed` | `Removed`, inverted |
| --- | --- | --- | --- | --- |
| `\w+` | `""` | `[]` | `[]` | `[]` |
| `\w+` | `abc` | `['abc']` | `[]` | `['abc']` |
| `\w+` | `"  "` | `['  ']` | `['  ']` | `[]` |
| `\s+` | `" ab "` | `[' ', 'ab', ' ']` | `['ab']` | `[' ', ' ']` |
| `X` | `aXXb` | `['a', 'X', 'X', 'b']` | `['a', 'b']` | `['X', 'X']` |
| `` (empty) | `ab` | `['a', 'b']` | `['a', 'b']` | `[]` |
| `\b` | `ab cd` | `['ab', ' ', 'cd']` | `['ab', ' ', 'cd']` | `[]` |

Two rules cover all of it. **Empty pieces are dropped** — an empty input yields nothing, and a text the
pattern covers entirely has no gaps to emit under `Removed`. **A zero-width match is still a match**, and
being empty it is then dropped: the last two rows are a pattern that matches only empty strings, so
`Isolated` emits the gaps between those matches and `Removed` inverted emits nothing at all.

### D8 — why nothing has noticed

With a pattern that matches every character, `Isolated` and `Removed`-inverted agree. Measured over seven
texts each with Llama-3's pattern and with GPT-2's: **equal on all of them**. Both patterns end in
alternatives covering whitespace and any remaining character, so there are no gaps for the two readings to
disagree about.

Every committed corpus that declares a `behavior` declares `Isolated` — `bpe_tokenizer_json.json`,
`bpe_no_op_settings.json`, `bpe_sequence_split.json`. So the shipped models are unaffected, and the
divergence needs a `Split` pattern narrower than its input to appear at all.

It is not hypothetical, though: with a `Split` of `\w+`, `"ab cd!"` gives `['ab','Ġ','cd','!']` in the
reference and `['ab','cd']` here — the space and the `!` are dropped before the merge loop sees them. For
a byte-level model that also breaks the round-trip guarantee, since a dropped character cannot be decoded
back. The repository's own fixture at `TokenizerJsonLoaderTests.LoadBpe_accepts_use_regex_off_on_the_byte_level_step_of_a_split_sequence`
declares exactly that pattern.

## Design

| Where | What |
| --- | --- |
| `SplitBehavior` | **New** public enum: `Isolated`, `Removed`, `MergedWithPrevious`, `MergedWithNext`, `Contiguous`, spelled as the file spells them. |
| `BpeSplitStep` | **New** public record carrying `Pattern`, `Behavior` and `Invert` together. |
| `BpeVocabulary` | `PreSplitPattern` (a `string?`) becomes `PreSplit` (a `BpeSplitStep?`). |
| `BpePreTokenizer` | Applies D3's model: segment, optionally swap labels, recombine, drop empties. |
| `TokenizerJsonLoader` | Reads `behavior` and `invert`; refuses an absent or unknown one by name. |

One type rather than three properties, because the reference requires the three fields together and
because two of them are meaningless while the pattern is null. #143 made the same argument two lots ago
about a different shape — naming the positions is what keeps a contradictory state unconstructible — and
the conclusion is the same here.

An enum rather than a string, because the set is closed by the format and the reference refuses anything
outside it (D6). A caller who wants a value the format does not define is a caller writing a file no
tokenizer will load.

### What this costs, stated rather than discovered later

`BpeSplitStep` is a new public type, so `samples/DataNet.Sample` must reference a member of it or the
packaging gate fails (ADR 0009). And `PreSplitPattern` is replaced one lot after #143 introduced it — a
second breaking change to `BpeVocabulary` inside one unreleased version. `DataNet.Embeddings` is at
`0.3.0` and unpublished, so no released version is affected.

## Evidence

A corpus `bpe_split_behavior.json`, generated against `tokenizers` 0.23.1, models carried in
`metadata.models` — the shape #118, #119, #120, #130 and #143 established, recording `pieces` beside
`tokens` and `ids` as #143 did, because the pre-tokenizer's own output is where the behaviour lives.

**Ten models**, one per `behavior` × `invert` combination, over a text set chosen so that each of D2's
cells is distinguished from every other:

- `ab cd!` — D2's own row, which separates `Removed`, `MergedWithPrevious`, `MergedWithNext` and their
  inversions from each other;
- `aXXb` with an adjacency-producing pattern — the only shape that tells `Isolated` from `Contiguous`
  (D4), and without it two of the ten models would be indistinguishable;
- `abc` fully matched, `"  "` unmatched, `" ab "` with leading and trailing gaps — D7's boundary rows,
  which are where an off-by-one in the segmentation lands;
- the empty string.

Plus the two refusals of D5 and one of D6, recorded the way #118, #130 and #120 record theirs: the exact
document handed to the reference and the error it answered with.

The corpus uses `add_prefix_space: false` throughout, for the reason ADR 0022 §10 records and #143 repeated:
the prefix space is applied at a different point here than in HuggingFace, that divergence belongs to
[#122](https://github.com/CyrilB1531/data.net/issues/122), and with it on every case here would measure the
two on top of each other.

## Out of scope

**`add_prefix_space`'s placement** ([#122](https://github.com/CyrilB1531/data.net/issues/122)) and **the
no-split mode** for `use_regex: false` on a bare `ByteLevel` and for an absent `pre_tokenizer` (also #122).

**A `Split` step inside anything but the `Sequence` shape `LoadBpe` already accepts.** The loader
reproduces `Sequence[Split, ByteLevel]` and refuses every other pre-tokenizer arrangement by name; this lot
changes what the `Split` step means, not which arrangements are read.

**`WordPieceTokenizer`**, which does not read a `Split` step at all.

## Risks

- **The fix changes tokens for any file whose `Split` pattern is narrower than its input.** That is the
  point, and no shipped model or committed corpus is in that set (D8) — but it is a behaviour change
  rather than an added refusal, and the CHANGELOG has to say so plainly.
- **`BpeVocabulary` takes its third breaking change in one unreleased version**, after #104's
  `AddedTokens` and #143's `PreSplitPattern`. The alternative is three loose properties whose validity
  depends on a fourth, which is the shape #143 argued against on measured grounds; taking the break now,
  while nothing is published, is cheaper than carrying the dead states.
- **Four arrangements are being implemented for zero known consumer.** `Isolated` is what every shipped
  file declares. The narrower option — implement `Isolated`, refuse the rest — was considered and
  rejected deliberately: it leaves the library answering "not reproduced" for values the format defines
  and the reference implements, and the segmentation that makes `Isolated` correct produces the other four
  from the same three lines.
