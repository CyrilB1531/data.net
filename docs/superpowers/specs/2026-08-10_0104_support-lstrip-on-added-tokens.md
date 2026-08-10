# Design — #104: added-token matching flags, for BPE and WordPiece

**Date:** 2026-08-10 · **Issue:** #104 · **Branch:** `feat/104-added-token-lstrip` ·
**Checkout:** `/home/cyril/Documents/devs/data.net` · **Base:** `c09b95f` (main, with #110 merged)

## Problem

`TokenizerJsonLoader` refuses an `added_tokens` entry carrying `lstrip`, `rstrip`
or `single_word`, because neither tokenizer reproduces those behaviours. Refusing
rather than ignoring is this library's rule, so the refusal is correct as it
stands — but `roberta-base`'s own `tokenizer.json` declares `lstrip=True` on
`<mask>`, so a model family `SpecialTokenTemplate.Roberta` already advertises
cannot be loaded.

## Scope, as decided

Wider than the issue body, on the user's instruction (2026-08-10):

- **All four flags**, not the three the issue names — `special` is forced in by
  the WordPiece half; see D3.
- **Both tokenizers.** The issue describes only BPE. WordPiece is not a
  flag pass-through: it has no added-token concept at all, and gaining one
  changes its behaviour for every file carrying `added_tokens`.
- **One spec, one pull request**, on the user's instruction, against the
  recommendation to split it.

Out of scope, and untouched: the five model settings #105 covers, and the
per-segment prefix-space rule — measured and recorded here, changed there.

## Measurements

All against `tokenizers` 0.23.1 in `.venv-oracles`, on a byte-level BPE with
`ByteLevel(add_prefix_space=False, use_regex=True)` unless stated. Nothing below
is inferred.

### The three matching flags

| Input | no flags | `lstrip=True` | `rstrip=True` | `single_word=True` |
| --- | --- | --- | --- | --- |
| `a <mask> b` | `a Ġ <mask> Ġ b` | `a ' <mask>' Ġ b` | `a Ġ '<mask> ' b` | `a Ġ <mask> Ġ b` |
| `a<mask>b` | `a <mask> b` | `a <mask> b` | `a <mask> b` | **no match** |
| `a  <mask>  b` | `a Ġ Ġ <mask> Ġ Ġ b` | `a '  <mask>' Ġ Ġ b` | `a Ġ Ġ '<mask>  ' b` | `a Ġ Ġ <mask> Ġ Ġ b` |
| `<mask> a` | `<mask> Ġ a` | `<mask> Ġ a` | `'<mask> ' a` | `<mask> Ġ a` |

- **`lstrip` absorbs *all* contiguous left whitespace**, not one character.
  `\t`, `\n` and U+00A0 are all absorbed; `.` is not. `char.IsWhiteSpace` is the
  matching predicate in .NET.
- **`rstrip` is the exact mirror.**
- **The id does not change.** `'a <mask> b'` under `lstrip` gives ids
  `[0, 7, 6, 1]` against `[0, 6, 7, 6, 1]` without it — same mask id 7, one
  fewer piece. The whole effect on the id stream is that the `Ġ` the whitespace
  would have produced disappears.
- **The emitted surface carries the absorbed span** — `' <mask>'` — with offsets
  `(1, 8)`. `token_to_id(" <mask>")` is `None`, so the span is a surface, not a
  vocabulary entry.
- **`single_word` requires both neighbours to be non-word or a string edge.**
  Measured: `a`, `1`, `_`, `é` are word characters (no match); `.`, `-`, space
  and the string edges are boundaries (match). In .NET,
  `char.IsLetterOrDigit(c) || c == '_'`.

### Round-trip

`'a <mask> b'` under `lstrip` decodes to `'a<mask> b'`. **HuggingFace loses the
absorbed whitespace too.** The round-trip guarantee byte-level BPE otherwise
provides does not survive an `lstrip`ped added token, in either implementation.
Following it is parity; restoring the space would be a silent divergence in the
more dangerous direction.

### Normalization — the finding that widened the scope

**Superseded by `docs/decisions/0022-added-token-matching-flags.md` §3**: the
`special`-based partition drawn from the table below was refuted by later
measurement — the discriminator is the entry's `normalized` field, not `special`
— and the ADR carries the corrected rule. The measurements themselves stand and
are left here as the record; only the conclusion drawn from them changed.

WordPiece with a `Lowercase` normalizer, added token `[CLS]`:

| added as | input `a [CLS] b` | input `a [cls] b` |
| --- | --- | --- |
| **special** | matches, emits `[CLS]` | **no match** — falls through to the model |
| **ordinary** | matches, emits `[cls]` | matches, emits `[cls]` |

So the rule is **not** "added tokens are matched before normalization". It is:

- an **ordinary** added token is itself normalized, and matched against
  normalized text;
- a **special** added token is exempt, and matched against the raw text — which
  is why `[CLS]` still matches under a lowercasing normalizer.

The two-stage model this implies: split on added tokens first — specials against
raw text, ordinary ones against normalized text — then normalize only the
segments between them.

## Decisions

### D1 — one shared `AddedToken` type

```csharp
public sealed record AddedToken(string Content, int Id)
{
    public bool Lstrip { get; init; }
    public bool Rstrip { get; init; }
    public bool SingleWord { get; init; }
    public bool Special { get; init; }
}
```

`BpeVocabulary.AddedTokens` changes from `IReadOnlyDictionary<string, int>` to
`IReadOnlyList<AddedToken>`; `WordPieceVocabulary` gains the same property. Both
records are **new in the unreleased 0.3.0** — verified: neither file exists at
tag `DataNet.Embeddings/v0.2.0` — so the shape is free to change and no parallel
structure is needed beside the dictionary.

One type rather than one per family, because the matching semantics must be
identical and two types would let them drift.

### D2 — one scanner, called by both tokenizers

An internal `AddedTokenScanner` answers exactly one question: *what is the next
added token at or after this position, and what span does it consume?* It owns
the priority rule already in `BpeTokenizer.NextAddedToken` — leftmost, then
longest — plus the three modifiers, and nothing else.

Both `BpeTokenizer` and `WordPieceTokenizer` call it. That shared call is what
guarantees the two cannot diverge on a flag.

`BpeTokenizer.NextAddedToken`'s existing search-window bound is preserved: once a
candidate is found, only a match starting at or before it can win, so later
candidates need a window reaching `bestAt + token.Length`. Llama-3 declares 256
added tokens and the bound is why that is not quadratic.

### D3 — `special` is carried, and closes a recorded divergence

The WordPiece half forces it: `WordPieceTokenizer` lowercases, and without
`special` there is no way to know whether `[CLS]` should be lowercased.

It also closes a divergence the code already documents.
`BpeVocabulary.AddedTokens`'s own remarks say the `special` flag "is not carried
here, so that flag drops every added token where Python drops only the special
ones", with `docs/equivalence.md` recording it. Carrying `special` makes
`BpeTokenizer.Decode(ids, skipSpecialTokens: true)` drop what Python drops. One
field, two defects.

### D4 — WordPiece stops folding, and scans

`WordPieceTokenizer.Encode` currently lowercases, then regex-splits, then calls
`TokenizeWord` per piece. It becomes: scan for added tokens first — specials
against raw text, ordinary ones against lowercased text — then lowercase and
regex-split only the segments between them.

The loader stops folding added tokens into `WordPieceVocabulary.Vocab`. Ids still
resolve, through the added-token list.

**This changes behaviour for every WordPiece file carrying `added_tokens`,
flags or no flags** — `tests/oracles/tokenizer_json.json` included. That is the
cost of the scope chosen, and the oracle regeneration is where it shows.

### D5 — the loader

`EnsureAddedTokenMatchesPlainly` and its three `EnsureAddedTokenFlagIsOff` calls
go. `ReadAddedTokens` builds `AddedToken` values instead of folding, and both
`ReadWordPiece` and `ReadBpeAddedTokens` receive the list.

The refusals `ReadBpe` raises for the five model settings #105 covers are not
touched.

### D6 — documentation

The ADR is **0022**, not the next free number: 0020 and 0021 are taken by work in
flight elsewhere (the user, 2026-08-10). Only 0019 exists in this checkout, so
re-check before writing the file.

**ADR 0022** records: the measured semantics of the four flags; the
special-raw / ordinary-normalized rule; the round-trip loss under `lstrip`, as
parity rather than defect; and, explicitly, **what #105 inherits** — the
scan-versus-normalization order settled here, and the measurement of what
`lstrip` does to a segment boundary, with the per-segment prefix-space rule
itself left for #105 to change.

`docs/equivalence.md` loses its `skipSpecialTokens` divergence and gains the
`lstrip` round-trip one.

## Verification

- Oracles regenerated from `tokenizers` 0.23.1 for all four flags on **both**
  tokenizers, including the measured edge cases: multiple whitespace, `\t`,
  `\n`, U+00A0, the `.`/`-`/string-edge boundaries, and `[CLS]` special versus
  ordinary under `Lowercase`.
- `roberta-base`'s five `added_tokens` entries load, `<mask>` with `lstrip=True`
  among them — the acceptance test for the issue.
- The existing WordPiece oracle is regenerated and its diff read, not
  regenerated silently: D4 changes it.
- `dotnet build DataNet.slnx -c Release --no-incremental` green with 0 warnings,
  `dotnet format --verify-no-changes` exit 0, the suite green with its counts
  read, both samples builds green against an isolated `NUGET_PACKAGES`.

## Risks

- **The WordPiece behaviour change is the largest risk**, and it is invisible in
  the flags: a file with unflagged `added_tokens` tokenizes differently once
  they stop being folded. The regenerated oracle diff is the evidence, and it
  must be read rather than accepted.
- **`rstrip` and `single_word` have no carrier in any corpus this repository
  holds.** They are implemented here against fixtures built for the purpose, not
  against a shipped model. That is a weaker footing than `lstrip`, which
  `roberta-base` exercises, and the ADR should say so.
