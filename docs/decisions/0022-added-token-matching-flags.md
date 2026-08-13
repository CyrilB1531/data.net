# 0022 — How an added token matches, and what it costs the round trip

**Status:** accepted · **Date:** 2026-08-10

## Context

An `added_tokens` table is the list of strings a `tokenizer.json` asks to be
matched as text, ahead of the model, so that `<mask>` is one id rather than
whatever the merge loop or the greedy longest match would make of the five
characters. Each entry carries five flags. `TokenizerJsonLoader` used to
**refuse** `lstrip`, `rstrip` and `single_word`, on this repository's rule that a
pipeline it does not reproduce is named rather than ignored. That was correct
while it was true, and it had one intolerable consequence: `roberta-base` sets
`lstrip: true` on `<mask>`, so a model family `SpecialTokenTemplate.Roberta`
already advertises could not be loaded at all.

Implementing the flags meant measuring them, and the measurement contradicted the
design written from reading the file format. Everything stated here was replayed
against `tokenizers` 0.23.1; nothing below is inferred. Two committed corpora
carry most of it:

- `tests/oracles/bpe_added_token_flags.json` — 26 cases over byte-level GPT-2
  with `add_prefix_space` off, one added token per flag (`<mask>` `lstrip`,
  `<pad>` `rstrip`, `<m>` `single_word`), recording `tokens`, `ids`, `decoded`
  and `decoded_skip_specials`.
- `tests/oracles/wordpiece_added_tokens.json` — 29 cases over a WordPiece model
  under a `Lowercase` normalizer, with eight added tokens covering **three** of
  the four combinations of `special` and `normalized`: three entries
  `special: true, normalized: false`, four `special: false, normalized: true`,
  and one `special: true, normalized: true`.

The fourth corner — `special: false, normalized: false`, the other combination
that runs in the raw pass — is in no committed corpus. It was established by a
probe run against the same `tokenizers` 0.23.1, on a `Lowercase` WordPiece with
`AddedToken("[CLS]", special=False, normalized=False)`: the entry matches
`'a [CLS] b'` and not `'a [cls] b'`, exactly as its `special: true` twin does.
That probe is working-note material and is **not** committed, which is why the
claim is attributed here rather than left to look corpus-backed. Three of four
corners are replayed on every test run; the fourth is not, and §9 says so again
where it counts.

A handful of other values below come from the same uncommitted probe runs rather
than from a corpus, and each is labelled where it appears. The measurements
themselves are written down in this repository, in the issue's design note
`docs/superpowers/specs/2026-08-10_0104_support-lstrip-on-added-tokens.md`, under
*Measurements* — a reader can reach the numbers even though the scripts that
produced them are not tracked.

The second corpus exists because the first cannot show the interesting half:
`LoadBpe` refuses any normalizer at all, so on the BPE side there is no
normalized text for an entry to be matched against, and the rule that decides
*which text* an entry is matched against is invisible.

## Decision

### 1. What the three span-shaping flags do

Four of the five flags decide **where** an entry matches. Three of them shape the
span a match consumes and are the subject of this section; the fourth,
`normalized`, decides which *text* the match is looked for in, and has §3 to
itself. `special`, the fifth, decides nothing about matching at all — §5.

**`lstrip`** absorbs the whitespace immediately to the left of a match into the
match. All of it, not one character:

| Input | Tokens | Ids |
| --- | --- | --- |
| `a <mask> b` | `a`, `' <mask>'`, `Ġb` | 64, 50257, 275 |
| `a<mask>b` | `a`, `<mask>`, `b` | 64, 50257, 65 |
| `a  <mask>  b` | `a`, `'  <mask>'`, `Ġ`, `Ġb` | 64, 50257, 220, 275 |
| `<mask> a` | `<mask>`, `Ġa` | 50257, 257 |
| `a. <mask>` | `a`, `.`, `' <mask>'` | 64, 13, 50257 |

A tab and U+00A0 are absorbed exactly as U+0020 is; `.` is not, and stops the
expansion where it stands. The matching predicate here is
`char.IsWhiteSpace`, which agrees with Rust's `char::is_whitespace` over every
character the corpus carries.

**`rstrip`** is the exact mirror, and the corpus is the mirror image:
`a <pad> b` gives `a`, `Ġ`, `'<pad> '`, `b` — the space to the *left* survives as
its own `Ġ` piece, the one to the right is swallowed. `a <pad>` gives
`a`, `Ġ`, `<pad>`: a strip at the end of the text has nothing to reach for.

**`single_word`** matches only where both neighbours are non-word characters or
the ends of the text. Measured: `.`, `-`, whitespace and the string edges are
boundaries and the match stands; `a`, `1`, `_` and `é` are word characters and
the entry does not match at all, falling through to the model — `1<m>1` comes
back as `1`, `<`, `m`, `>`, `1`. In .NET this is
`char.IsLetterOrDigit(c) || c == '_'`, with the divergence recorded in §8.

The emitted surface carries the absorbed span: the token is `' <mask>'`, not
`<mask>` with the space dropped, and the corpus records it that way in every
`lstrip` case above. Two further facts about that surface come from an
uncommitted probe run and are recorded in the design note's *Measurements*
section rather than in a corpus, since no corpus here stores offsets or replays a
`token_to_id` call: HuggingFace gives the token offsets `(1, 8)` — the span
including the absorbed space — and `token_to_id(" <mask>")` returns `None`. The
surface is therefore a *surface*, never a vocabulary entry. This library follows:
the tokenizers emit the raw slice the match consumed, not the entry's own
`Content`.

### 2. The id stream loses a piece; no id ever changes

On a byte-level model, the piece that disappears is the `Ġ`. Two corpus cases of
the same shape show it without a counterfactual having to be constructed:
`'a <mask> b'`, where `lstrip` absorbs the space, is `[64, 50257, 275]`; the
mirror input `'a <pad> b'`, where `<pad>` carries `rstrip` and so leaves the
space on its left alone, is `[64, 220, 50258, 65]` — with 220, `Ġ`, standing in
the stream as its own piece. That pair is what fixes *which* piece the strip
costs. That the entry's own id survives the strip is the other half, and §1's
first two rows carry it: `a <mask> b` and `a<mask>b` hold the same entry with and
without a whitespace neighbour, and `<mask>` is id 50257 in both. Those two rows
also differ on the right — `275` `Ġb` against `65` `b` — so what they establish
is the id, not the piece count. The design note records the direct with/without
pair from an uncommitted probe on a smaller model — `[0, 7, 6, 1]` under `lstrip`
against `[0, 6, 7, 6, 1]` without it, mask id 7 unchanged.

**On WordPiece the id stream does not change at all.** Its pre-tokenizer is
`\w+|[^\w\s]+`, which never emits a whitespace piece, so there is no piece for a
strip to remove. The corpus is explicit: `'the <L> cat'` with `lstrip` is
`[1, 50, 2]`, the same three-id shape as the flagless `'the <MASK> cat'` at
`[1, 46, 2]`. What changes there is the emitted token **string** — `' <L>'`
carries the absorbed space where `<mask>` does not — and nothing else.

So: **a strip never renumbers anything.** On a byte-level model it costs the id
stream exactly the piece the absorbed whitespace would have produced; on
WordPiece it costs the id stream nothing and moves a character into a token
string. This is worth stating because the flags read like they might renumber
something, and a reader who assumes they do will look for a second vocabulary
that is not there.

### 3. `normalized`, not `special`, decides which text an entry is matched against

This is the finding that widened the issue's scope, and the one an implementation
written from the obvious reading gets wrong.

`tokenizers` keeps **two** tries, and the discriminator between them is the
entry's `normalized` field:

- `normalized: false` → the entry runs in an outer pass over the **raw**,
  un-normalized text, and emits the raw slice.
- `normalized: true` → the entry's own `Content` is normalized, and matched
  against the **normalized** text, emitting that.

The raw pass runs first, over the whole input, and the normalized pass runs only
over what the raw pass left. That order is observable:
`'the A<R> cat'`, with `<R>` raw and `A<R>` normalized, gives
`the`, `a`, `<R>`, `cat` — the raw `<R>` wins even though the normalized `A<R>`
starts one character further left, which a single merged leftmost-wins scan would
not reproduce.

`special` contributes **nothing** to that choice. Under a `Lowercase` normalizer,
with the input `'the [SEP] cat'` and `[SEP]` declared `special: true,
normalized: true`, `tokenizers` emits `[sep]` — lowercased, matched against
lowercased text, behaving in every respect as an ordinary token. It matches
`'the [sep] cat'` too. Change nothing but `normalized`, to `false`, and the
lowercased spelling stops matching entirely. The `special` flag is the same in
both. All four combinations of the two are representable and round-trip through
a `tokenizer.json`. `wordpiece_added_tokens.json` replays three of them; the
fourth, `special: false, normalized: false`, comes from the uncommitted probe
named in the Context above, and runs in the raw pass exactly as its
`special: true` twin does. `special` is therefore not merely a poor predictor of
the pass — it is not a predictor of it at all.

Two summaries are therefore both wrong, and both are the natural guess:

- **"Added tokens are matched before normalization."** Only the non-normalized
  half is. A normalized entry is matched after, against normalized text, with its
  own content normalized to meet it.
- **"Special tokens are exempt from the normalizer."** They are not; `normalized`
  entries are not exempt whether or not they are special, and non-`normalized`
  entries are exempt whether or not they are special.

The two look identical on every file anyone has, because HuggingFace's
`add_special_tokens` and `add_tokens` set `normalized = !special`. That is the
door every naive probe goes through, which is exactly why the wrong rule is so
believable: it is true of the whole population and false of the mechanism.

### 4. The `!special` default lives on the type, not in the loader

`tokenizers` **refuses** a `tokenizer.json` whose added-token entries omit
`normalized`, so no corpus can measure what an absent field means. `!special` is
Rust's `AddedToken::from(content, special)` default, read from the constructor
rather than observed — and it is recorded as such.

Here it lives on `AddedToken.Normalized`, over a `bool?` backing field, with
`Equals` and `GetHashCode` written by hand over the **resolved** value. A token
loaded from a file that stated `normalized: false, special: true` and one a
caller wrote as `new AddedToken("<s>", 0) { Special = true }` therefore describe
the same token and compare equal. Had the default sat in the loader, the two
would have disagreed in exactly the case that matters — a vocabulary read from a
file compared against one built by hand in a test — and the generated record
equality would have compared the backing field, reporting two observably
identical vocabularies unequal.

### 5. `special` survives for exactly one job

It decides nothing about where an entry matches. It decides what
`Decode(ids, skipSpecialTokens: true)` drops: the entries whose `special` is
true, and only those. That is Python's `skip_special_tokens`, and carrying the
flag closes a divergence `docs/equivalence.md` used to record — the previous
table had no `special` field, so `skipSpecialTokens` dropped *every* added token
where Python dropped only the special ones.

### 6. The round trip loses the absorbed whitespace — in HuggingFace too

`'a <mask> b'` under `lstrip` decodes to `'a<mask> b'`. The space is gone. The
byte-level round-trip guarantee `BpeTokenizer` otherwise provides — every byte in,
every byte out — does not survive an `lstrip`ped added token.

**HuggingFace loses it identically**, and the corpus records its `decoded` field
saying so. Following it is parity. Restoring the space would be a silent
divergence in the more dangerous direction: text that round-trips here and not in
the library this one is measured against, discovered by whoever compares two
pipelines rather than by a test. It is recorded on the `Decode` row of
`docs/equivalence.md` so it is read rather than discovered.

### 7. WordPiece stops folding added tokens into the vocabulary

`WordPieceTokenizer` had no added-token concept: the loader folded every
`added_tokens` entry into `WordPieceVocabulary.Vocab`, where the greedy longest
match found it as an ordinary whole-word entry. That is a different tokenizer as
soon as an entry carries a flag, and it cannot honour `normalized` at all, since
a folded entry is matched against whatever the pre-tokenizer hands it.

It now scans, through the same `AddedTokenScanner` `BpeTokenizer` uses — one
object answering *what is the next added token at or after this position, and
what span does it consume*, so the two tokenizers cannot drift apart on a flag.
The raw pass runs over the un-lowercased input; the gaps between raw matches are
lowercased and run through the normalized pass; what neither claims is
pre-tokenized and greedily matched as before.

**This changes behaviour for every WordPiece file carrying `added_tokens`,
flags or no flags.** Two consequences follow, and both are stated rather than
left to be met:

- `WordPieceVocabulary.Count` now under-counts what `Encode` can emit, because
  the added tokens are no longer members of `Vocab`. `BpeVocabulary` already had
  this property; the two now agree.
- No corpus that predates this branch showed the change, because none of them
  declares a WordPiece `added_tokens` table:
  `tokenizer_json.json`'s `wordpiece_tokenizer_json` carries
  `"added_tokens": []`, and regenerating the file produced no diff. (Its
  `unigram_tokenizer_json` does carry a non-empty table, which is why the field
  name matters here — Unigram reads that table only to derive `Control` piece
  types and never folded anything.) The plan expected the regenerated diff to be
  the evidence; there is none, which is what makes the new
  `wordpiece_added_tokens.json` the sole replayed evidence for the whole
  WordPiece half rather than a supplement to it.

### 8. `single_word`'s word class is char-based here, code-point-based in HuggingFace

The same boundary `docs/equivalence.md` already records for the BPE split
pattern, in a second place. Rust's `char::is_alphanumeric` tests a code point and
counts the `Nl` and `No` Unicode categories (`Ⅷ`, `²`) as word characters,
together with any letter or digit above the Basic Multilingual Plane. .NET's
`char.IsLetterOrDigit` tests one UTF-16 code unit: it covers neither `Nl`/`No`
nor an astral letter, which arrives as two surrogate halves in category `Cs`,
neither of which is a letter. A `single_word` entry adjacent to such a character
therefore matches here and would not in HuggingFace.

Every case in the measured table agrees, and no corpus probes the gap —
deliberately. A corpus is a thing that must stay green, and committing a case
this library fails would either freeze the divergence as expected behaviour or
break the suite. It is recorded here and in `AddedToken.SingleWord`'s own remarks
as a known, unmeasured boundary rather than left for a user to find on their own
input.

### 9. What is measured, and what rests on fixtures built for the purpose

This section collects everything above that a test run does not check: which
flags have a carrier in a model this repository holds, which corners of the flag
matrix are replayed, and which tie is resolved by argument rather than by
measurement.

`lstrip` is the flag with a carrier. `roberta-base` declares it on `<mask>`,
which is the whole reason this issue exists, and commit `d5e9f5c` added
`tests/oracles/roberta_shaped_model.json` — a `tokenizer.json` generated by
`build_roberta_shaped()` in `tools/build_tiny_models.py`, carrying
`roberta-base`'s exact five-entry added-token table, `<mask>` at id 50264 with
`lstrip: true` among them, over a toy eight-entry vocabulary. Generated rather
than typed, so the next reader can rebuild it and see what is verbatim and what
is toy. It proves the table loads, flags intact. It is **not** `roberta-base`'s
own file:
no model weights and no pretrained vocabulary are committed to this repository
(decision [0003](0003-provenance-and-licensing.md)), and nothing here replays a
`roberta-base` encoding.

`rstrip` and `single_word` have **no carrier in any model this repository
holds**. They are proven against added tokens declared on GPT-2's vendored
vocabulary for the purpose of measuring them — real replayed HuggingFace output,
but over a table no published model ships. That is a weaker footing than
`lstrip`, and it is named here rather than smoothed over: if one of them is wrong
in a way the constructed cases do not reach, nothing else in this repository will
notice.

The `(special, normalized)` matrix §3 rests on is three-quarters replayed.
`wordpiece_added_tokens.json` carries `special: true, normalized: false`,
`special: false, normalized: true` and `special: true, normalized: true`. The
fourth corner, `special: false, normalized: false`, is in no committed corpus and
rests on the uncommitted probe described in the Context — the one claim in §3 no
test run checks, and the one to re-probe first if the rule ever looks wrong.

One tie is genuinely unmeasured, and `AddedTokenScanner` says so at the line that
resolves it. The winner among competing candidates is chosen on the **raw** match
position, before either side's strip is applied. Whether that is right when a
right-hand `lstrip` candidate could expand back past an *earlier* competing
candidate's raw match is untested — and untestable with ordinary content, since
an earlier candidate's own match is not whitespace and so always blocks the
expansion. Only an added token whose `Content` is itself whitespace could create
the conflict, and none was probed.

Cases 23-25 of `bpe_added_token_flags.json` do **not** close it, though they sit
next to it: `'<pad> <mask>'`, `'<mask> <mask>'` and `'a <pad> <mask> b'` all put
the earlier candidate leftmost already, so no conflict arises. What they measure
is the adjacent rule — the `while (start > from …)` clamp. An `rstrip` claims the
shared space first, and the `lstrip` that follows is stopped at `from` instead of
absorbing the same character twice: `'<pad> <mask>'` gives `'<pad> '`, `<mask>`,
two tokens over twelve characters with nothing counted twice. Raw-position
comparison with strip-after remains the untested fallback the design calls for,
and the comment naming it stays until a case changes it.

### 10. What #105 inherits

> **#119 and #120 update:** the paragraph below is as this decision found it,
> and two of its clauses have since gone stale.
> [#119](https://github.com/CyrilB1531/data.net/issues/119) stopped refusing
> `fuse_unk`, and [#120](https://github.com/CyrilB1531/data.net/issues/120)
> stopped refusing `continuing_subword_prefix` — it is applied now, so
> `EnsureBpeModelSettingsAreReproduced` owns `dropout` alone and its summary
> says "the `model` setting", singular. **Two** settings under `model` are
> refused unconditionally by name today, not four: `byte_fallback`
> (`EnsureByteFallbackIsOff`) and `dropout`. A third refusal is conditional
> and new with #120: a non-empty `continuing_subword_prefix` on a byte-level
> model, which `BpeTokenizer` would answer inconsistently, refused by
> `EnsureContinuingPrefixIsNotByteLevel`. The rest of this section — the
> scan-versus-normalization order and what a strip does to a segment boundary
> — is untouched by both and still holds.

Issue #105 covers the `model` settings `LoadBpe` still refuses and the
per-segment prefix-space rule. **Four** settings under `model` are refused by
name, and this decision leaves every one of them where it found it:
`byte_fallback` (`EnsureByteFallbackIsOff`), and `continuing_subword_prefix`,
`fuse_unk` and `dropout` (`EnsureBpeModelSettingsAreReproduced`, whose own
summary counts the three it owns). The issue's design note says five; the code
says four, and the code is what a reader can check. What `LoadBpe` refuses
*outside* `model` — any `normalizer`, `truncation`, `padding`, a
`post_processor`, a `ByteLevel` with `use_regex` off, any other pre-tokenizer
shape, and a `decoder` whose byte-level-ness disagrees with the model's — are
pipeline sections rather than model settings, and `docs/equivalence.md`'s
`LoadBpe` row enumerates all of them without asserting a count.

Two things are settled here and are not #105's to decide again:

- **The scan-versus-normalization order.** Added tokens are split out first, raw
  entries against raw text and normalized entries against normalized text, and
  only the gaps between them are normalized. Anything #105 adds to the normalizer
  applies to the gaps.
- **What a strip does to a segment boundary.** An absorbed space is consumed by
  the added token, so the segment that follows starts *after* it — which is why
  `bpe_added_token_flags.json` was generated with `add_prefix_space` off. With it
  on, a per-segment prefix space would put a `Ġ` beside every match, and a `Ġ`
  beside a match is the exact piece a strip is read from; the two rules would be
  measured on top of each other. `bpe_added_tokens.json` is where
  `add_prefix_space` is measured, and `BpeTokenizer.EncodeSegment` still applies
  it per added-token-delimited segment and only where the segment does not
  already begin with a space, untouched by this decision, for #105 to change.

## Consequences

- `docs/equivalence.md` states, on the `Decode` row, that an `lstrip`ped added
  token loses the absorbed whitespace on the round trip, as parity rather than a
  defect; and on the WordPiece rows, that added tokens are matched as text
  through the two passes instead of folded into the vocabulary.
- `AddedToken` is one type shared by both tokenizers, and the matching lives in
  one internal scanner both call. Two types or two scanners would let `lstrip`
  mean one thing on BPE and another on WordPiece, which is the failure this
  arrangement exists to make impossible.
- `BpeVocabulary.AddedTokens` is `IReadOnlyList<AddedToken>` where it was
  `IReadOnlyDictionary<string, int>` — a breaking change, taken on a version that
  has not shipped, which is the only reason it was available to take.
- `WordPieceVocabulary.Count` counts `Vocab` alone and under-counts what `Encode`
  can emit. Callers sizing an embedding table from it were already wrong for BPE;
  they are now wrong for WordPiece in the same way, consistently.
- A `single_word` entry beside an `Nl`/`No` character or an above-BMP letter is a
  known divergence from HuggingFace, not a defect tracked for a fix. Fixing it
  means a per-code-point word-class scan, which is the same work decision
  [0017](0017-bpe-parity-scope.md) declined for the split pattern and is declined
  here for the same reason.
