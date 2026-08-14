# 0149 — The byte-level decode throws where the reference substitutes

**Issue:** [#149](https://github.com/CyrilB1531/data.net/issues/149) · **Date:** 2026-08-14 ·
**Branch:** `fix/149-lossy-bytelevel-decode`, stacked on `feat/121-bpe-normalizer` ·
**Found by:** [#121](https://github.com/CyrilB1531/data.net/issues/121)

## Context

`BpeTokenizer.Decode` assembles a byte-level model's tokens into bytes and decodes them through
`JsonArtifact.Utf8NoBom`, which is built `throwOnInvalidBytes: true`. HuggingFace's decoder substitutes
U+FFFD instead. The issue was filed about one shape — an added token whose content is not byte-level
decodable — and the measurement that followed found it is the small half of the problem.

### What was measured

Stock GPT-2, no added token involved, each id decoded on its own through `tokenizers` 0.23.1:

| text | tokens | tokens that alone decode to U+FFFD |
| --- | ---: | ---: |
| `東京 👋` | 6 | **6** |
| `日本語のテキスト` | 10 | 6 |
| `🇫🇷 emoji` | 7 | 6 |
| `déjà vu` | 6 | 0 |

Every token of a CJK or emoji text is a fragment of a multi-byte sequence. Python returns a replacement
character for each; DataNet raises `DecoderFallbackException`. **Decoding a token at a time is the normal
way to consume a language model**, and today it is impossible here for any text outside Latin-1.

So there are two shapes, not one:

- **A — a subset of a token stream.** Streaming, or any slice. Throws whenever the slice cuts a character.
  The current remarks scope this to ids *"assembled by hand rather than produced by `Encode`"*, which
  describes it wrongly: streaming ids come from `Encode`.
- **B — a complete `Encode` output.** Throws when an added token's content maps to bytes that are not
  valid UTF-8 on their own — `café` as an added token becomes `63 61 66 E9`, and `E9` is not a lead byte.
  This is the contract violation #149 was filed for.

Shape B is real in the wild: of the fifteen public BPE `tokenizer.json` files surveyed for #121,
**deepseek-coder-1.3b-base carries 18 non-ASCII added tokens out of 22**. It is refused today for its
pre-tokenizer — a `Sequence` of four `Split` steps and `Digits`, where the loader accepts `Split` then
`ByteLevel` — so this lot unblocks no model today. Its value is that the contract stops being false.

## Decisions

### D1 — decode substitutes, encode still throws

One call site changes: the `GetString` in `BpeTokenizer`'s byte-level decode path. It moves to a
decode-only `UTF8Encoding` with the default replacement fallback.

`JsonArtifact.Utf8NoBom` is **not** touched. It is shared with the persistence layer, where refusing a
malformed artifact is right, and with `Encode`'s `GetBytes`, where a lone surrogate must still throw — a
lone surrogate is not well-formed UTF-16 to begin with, so there is no byte sequence to be lossless
*about*, and ADR 0017's reasoning there stands.

The asymmetry is the decision, and it is the reference's: **strict on the way in, forgiving on the way
out**.

### D2 — what the caller loses, stated rather than smoothed over

A caller who relied on the exception to notice a truncated or hand-built id list now gets a string with
U+FFFD in it. That is a silent wrong answer where there was a loud one, and it is the real cost of this
change.

It is accepted because the alternative costs more: refusing means no incremental decode at all for
non-Latin text, which the measurement above shows is every CJK and emoji stream. U+FFFD is also
detectable — `text.Contains('�')` is the check a caller who cared would write, and the reference
gives them nothing better.

### D3 — the evidence, and the test that inverts

`tests/oracles/bpe_normalizer.json` already carries Python's `decoded` values, U+FFFD included, from #121.
Three of its cases currently prove that DataNet throws where Python substitutes. Those become ordinary
parity assertions, and `Decode_throws_where_the_reference_is_lossy` is deleted — a test that pinned a
divergence goes away when the divergence does.

Shape A gets the corpus it never had: **each id of a text decoded on its own**, over `東京 👋`,
`日本語のテキスト`, `🇫🇷 emoji` and a Latin control, compared against `tokenizers` id by id. Six tokens of
six on the first text are cases that did not exist before.

The full-stream corpora are the guard that nothing else moved: a complete, valid byte sequence never
reaches the fallback, so every existing decode oracle must pass **unchanged**.

### D4 — an ADR, because a documented public contract changes

`Decode`'s `<exception>` list loses `DecoderFallbackException`, and the remarks that scope the case to
hand-assembled ids go with it, since they are false. `docs/equivalence.md` gains the parity row.

The ADR records what a reader of the old contract needs: that the throw is gone, what replaced it, why the
encode side is deliberately not symmetric, and how to detect the case now. It is numbered one above the
highest existing decision.

### D5 — the branch is stacked, and the comment budgets are the new ones

The corpus and the pinning test live on `feat/121-bpe-normalizer`, which is not pushed. Building this on
`main` would mean recreating the `café` fixture and the corpus to throw them away at the merge, so this
branch sits on top of #121 and **cannot merge before it**.

[#134](https://github.com/CyrilB1531/data.net/issues/134) is landing a comment-length gate:
`tools/check_comment_length.py`, **two lines for an inline comment and eight lines of prose for XML
documentation**, with anything longer opening on `long-comment:` and a reason. This lot writes to those
budgets from the start rather than being swept afterwards.

**No inline comment in this lot exceeds two lines, and none uses the `long-comment:` escape.** The marker
exists for a block that has earned its length; nothing here has. What does not fit in two lines belongs in
the member's XML documentation, in this spec, or in the ADR — three places a reader can find it without
it standing between them and the code.

## Documentation

- `docs/equivalence.md` — the `decode` row's exact-round-trip promise, and the `LoadBpe` row's clause
  added by #121 which names this issue.
- `docs/guides/embeddings.md` — the note #121 added says a non-ASCII added token makes `Decode` throw. It
  will not.
- An ADR, per D4.

## Out of scope

`WordPieceTokenizer` and `SentencePieceTokenizer`, which assemble no bytes. Refusing such a model at load,
which was considered and rejected: it addresses shape B only and leaves shape A, which the measurement
shows is the larger one. An opt-in parameter to keep the throw, which doubles a public contract for a
choice the reference does not offer.

## Risks

- **A caller depending on the exception.** `DataNet.Embeddings` is 0.x and unreleased on this path, so
  nothing published promises it — but the ADR must say it plainly rather than leave it to a changelog.
- **The stack.** If #121 changes during review, this branch rebases. Its own diff is one call site, so the
  conflict surface is the tests and the documentation, not the fix.
- **Silent substitution in a test fixture.** A corpus case that starts producing U+FFFD by accident would
  now pass where it used to throw. The streaming corpus pins the count of substituted tokens per text for
  that reason, the way #121 pinned the count of throwing cases.
