# 0023 — Byte-level `Decode` substitutes U+FFFD instead of throwing

**Status:** accepted · **Date:** 2026-08-14

## Context

`BpeTokenizer.Decode`, for a byte-level model, assembled the raw bytes each
token symbol spells and handed them to `JsonArtifact.Utf8NoBom.GetString`,
the same strict `UTF8Encoding` the persistence layer uses. A byte sequence
that is not well-formed UTF-8 made that call throw
`DecoderFallbackException`. Issue #149 was filed about one way that shape
arises — an added token whose content is not byte-level decodable end to end
— and #121's own added-token corpus already carried three cases that threw
for exactly that reason.

## Decision

### 1. What changed

`Decode` now substitutes U+FFFD for a byte sequence that is not well-formed
UTF-8, at the one call site that converted bytes to a string, instead of
throwing `DecoderFallbackException`. `Encode`'s own strict byte conversion
is untouched, and so is `JsonArtifact.Utf8NoBom` itself — the substitution
is local to `Decode`'s output path, via a second `UTF8Encoding` constructed
with `throwOnInvalidBytes: false`.

### 2. The measurement that forced it

Issue #149's own investigation went past the added-token shape it was filed
about and measured plain streaming decode — no added token involved,
against stock GPT-2 through `tokenizers` 0.23.1, one id at a time, the way a
language model is normally consumed. U+FFFD is what the reference itself
returns for a multi-byte character split across token boundaries:

| Text | Ids producing U+FFFD |
| --- | --- |
| `東京 👋` | 6 of 6 |
| `日本語のテキスト` | 6 of 10 |
| `🇫🇷 emoji` | 6 of 7 |
| `déjà vu` | 0 of 6 |

DataNet threw on every one of those before this branch — `Decode` was
unusable one id at a time for any text outside Latin-1, which is not an
edge case the issue happened to also cover; it is the shape of ordinary CJK
and emoji text under streaming decode. `tests/oracles/bytelevel_decode_stream.json`
carries the frozen corpus.

### 3. Why `Encode` stays strict

`Encode` still throws on a lone surrogate. A lone surrogate is not
well-formed UTF-16, so there is no byte sequence for it to be lossless
about — nothing for a substitution to produce in its place. The asymmetry
between a forgiving `Decode` and a strict `Encode` is not invented here; it
is the reference's own. `tokenizers` decodes lossily and encodes strictly,
and this decision reproduces exactly that shape rather than picking a
uniform policy `tokenizers` does not have.

### 4. What a caller loses

Before this change, a byte sequence that could not be decoded raised
`DecoderFallbackException` — a loud signal that usually meant the caller had
truncated a stream mid-character or hand-built an id list incorrectly.
After this change, the same input returns a string silently, containing
U+FFFD in place of the bytes that did not decode. That string is
indistinguishable, at the type level, from one that decoded cleanly. A
caller who wants to know now has to test the result for U+FFFD themselves;
nothing forces them to. This is a real loss, not a cosmetic one — it trades
an exception a caller could not ignore for a value a caller can, and it is
the cost this decision accepts in exchange for one-id-at-a-time decoding
being usable at all.

### 5. What is unchanged

A complete, valid byte sequence — anything `Encode` itself produced and
handed back whole — never reaches the fallback path: `Utf8Lossy` and the
strict encoding agree on every well-formed input, so the byte-level round
trip holds exactly where it held before this change. Only a byte sequence
that was already malformed changes behaviour, from throwing to
substituting.

### 6. What was rejected

**Refusing such a model at load.** `TokenizerJsonLoader.LoadBpe` could
refuse a `tokenizer.json` whose added tokens are not byte-level decodable,
the way it already refuses `byte_fallback` and other shapes. That addresses
only the added-token case #149 was filed about — it does nothing for the
streaming case in Decision 2, where the split arises from ordinary
multi-byte text and no added token is involved. A load-time refusal would
leave the more common shape unfixed while adding a new way to reject a
model.

**An opt-in parameter.** `Decode` could take a flag choosing between
throwing and substituting, leaving today's strict behaviour as the default.
`tokenizers` offers no such choice — every caller gets the same lossy
decode — so a parameter here would double a public method's contract for a
choice the reference does not offer its own callers, without bringing
DataNet closer to what it is measured against.

## Consequences

- `docs/equivalence.md`'s `decode` row no longer claims an unqualified
  byte-exact round trip; it states what happens to a byte sequence that is
  not well-formed instead.
- `docs/equivalence.md`'s `LoadBpe` row drops the clause #121 added naming
  this issue as an open divergence — it is closed.
- `docs/guides/embeddings.md` states what `Decode` does with an added token
  that is not byte-level decodable, instead of naming an exception that no
  longer happens, and says that decoding a token at a time now works.
