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
against `tokenizers` 0.23.1; nothing below is inferred. Two corpora carry it:

- `tests/oracles/bpe_added_token_flags.json` — 26 cases over byte-level GPT-2
  with `add_prefix_space` off, one added token per flag (`<mask>` `lstrip`,
  `<pad>` `rstrip`, `<m>` `single_word`), recording `tokens`, `ids`, `decoded`
  and `decoded_skip_specials`.
- `tests/oracles/wordpiece_added_tokens.json` — 29 cases over a WordPiece model
  under a `Lowercase` normalizer, with eight added tokens spanning every
  combination of `special` and `normalized`.

The second corpus exists because the first cannot show the interesting half:
`LoadBpe` refuses any normalizer at all, so on the BPE side there is no
normalized text for an entry to be matched against, and the rule that decides
*which text* an entry is matched against is invisible.

## Decision

### 1. What the three matching flags do

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

The emitted surface carries the absorbed span: the token is `' <mask>'`, with
offsets `(1, 8)`, not `<mask>` with the space dropped. `token_to_id(" <mask>")`
is `None` in Python, so that surface is a *surface* and never a vocabulary entry.
This library follows: the tokenizers emit the raw slice the match consumed, not
the entry's own `Content`.

### 2. The id stream loses a piece; no id ever changes

`'a <mask> b'` is `[64, 50257, 275]` under `lstrip` against `[64, 220, 50257,
275]` without it. Same `<mask>` id, one fewer piece. **The entire effect of a
strip on the id stream is that the piece the absorbed whitespace would have
produced disappears** — the `Ġ` on a byte-level model, a pre-token on WordPiece.
This is worth stating because the flags read like they might renumber something,
and a reader who assumes they do will look for a second vocabulary that is not
there.

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
a `tokenizer.json`; `wordpiece_added_tokens.json` carries them.

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
- No committed WordPiece corpus showed the change, because none of them declares
  an `added_tokens` table — `tokenizer_json.json` carries `"added_tokens": []`
  and regenerating it produced no diff. The plan expected that diff to be the
  evidence; it does not exist, which is what makes `wordpiece_added_tokens.json`
  the sole replayed evidence for the whole WordPiece half rather than a
  supplement to it.

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

`lstrip` is the flag with a carrier. `roberta-base` declares it on `<mask>`,
which is the whole reason this issue exists, and Task 7 committed
`tests/oracles/roberta_shaped_model.json` — a hand-constructed `tokenizer.json`
carrying `roberta-base`'s exact five-entry added-token table, `<mask>` at id
50264 with `lstrip: true` among them, over a toy eight-entry vocabulary. It
proves the table loads, flags intact. It is **not** `roberta-base`'s own file:
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
two tokens over eleven characters with nothing counted twice. Raw-position
comparison with strip-after remains the untested fallback the design calls for,
and the comment naming it stays until a case changes it.

### 10. What #105 inherits

Issue #105 covers the five model settings `LoadBpe` still refuses, and the
per-segment prefix-space rule. Two things are settled here and are not its to
decide again:

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
