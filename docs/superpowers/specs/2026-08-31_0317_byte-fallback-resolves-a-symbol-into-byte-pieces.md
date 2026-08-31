# 0317 — `byte_fallback` resolves a symbol into byte pieces, not into the unknown token

**Issue:** [#317](https://github.com/CyrilB1531/lodestar/issues/317) ·
**Status:** accepted · **Date:** 2026-08-31

## Problem

[#175](https://github.com/CyrilB1531/lodestar/issues/175) wants two files a user actually has:
Llama-2 and Mistral v0.1. [#316](https://github.com/CyrilB1531/lodestar/issues/316) took the
first of the two refusals they hit — the whitespace escape. This lot takes the second, and it is
the one that opens the door: after it, both files load.

[Decision 0050 §3](../../decisions/0050-the-sentencepiece-bpe-lineage-stays-a-bpe-model.md)
already settled *that* `byte_fallback` is reproduced rather than refused. It did not settle
*what* to reproduce, and #317 is explicit that a boolean does not express it:

> an uncovered character resolves into `<0x..>` byte pieces in Python where these tokenizers emit
> the unknown piece. Reproducing it needs the byte pieces to exist in the vocabulary and a
> resolution order defined — neither of which a boolean expresses.

So this spec opens on measurement, the way [#105](https://github.com/CyrilB1531/lodestar/issues/105)'s
divergences were measured rather than inferred.

## What `tokenizers` 0.23.1 actually does

Probed against hand-written `tokenizer.json` files over a four-piece BPE (`a`, `b`, `c`, `ab`)
plus a controlled set of `<0xXX>` pieces. Every line below is a measurement, not a reading of the
Rust.

**1. The unit is the symbol, and the symbol is a code point.** A symbol absent from the
vocabulary becomes one piece per UTF-8 byte of it: `é` → `<0xC3> <0xA9>`, `日` →
`<0xE6> <0x97> <0xA5>`, `🙂` → four. A symbol present is never expanded — with `é` in the
vocabulary, `é` stays `['é']`.

**2. It is all-or-nothing per symbol.** With `<0xC3>` present and `<0xA9>` missing, `é` is
`['<unk>']` — not `<0xC3>` and then an unknown. Each symbol decides for itself: with only
`<0x58>` present, `XY` is `['<0x58>', '<unk>']`.

**3. The spelling is uppercase hexadecimal.** `<0xC3>` is the piece; a vocabulary spelling it
`<0xc3>` resolves nothing and falls to `<unk>`.

**4. The expansion happens before the merges, and byte pieces are ordinary symbols.** Declaring
the merge `<0xC3> <0xA9>` gives `é` → `['<0xC3><0xA9>']`; declaring `a <0xC3>` gives `aé` →
`['a<0xC3>', '<0xA9>']`; declaring both, the merge of lower rank wins, as for any pair. A
post-pass over unmergeable symbols could not produce these.

**5. The decorated symbol is what gets expanded.** With `continuing_subword_prefix: "##"`, `aé`
is `['a', '<0x23>', '<0x23>', '<0xC3>', '<0xA9>']` — the `##` is itself encoded, as two `#`
bytes. With `end_of_word_suffix: "</w>"`, `aé` ends `<0x3C> <0x2F> <0x77> <0x3E>`. Neither target
model declares either, but this is what makes the rule statable: **expand the string the symbol
already is**, decoration included.

**6. `fuse_unk` fuses only what is left.** A byte-resolved symbol is never fused —
`aXXb` is `['a', '<0x58>', '<0x58>', 'b']` under `fuse_unk: true`. Symbols that still fall to the
unknown token fuse among themselves as before: with no byte pieces at all, `XY` is `['<unk>']`.

### The two traps, and one upstream bug

**A vocabulary declaring `byte_fallback` without the pieces is not refused.** `tokenizers`
degrades silently to `<unk>`, per symbol. And with `unk_token: null` it **drops the symbol
entirely** — `aXb` becomes `['ab']`, the neighbours merging across the hole. Neither is a stream
any caller would want, and #317 already names this as a file *"this library should refuse with a
message naming it rather than encode wrongly"*.

**The order is wrong when a byte-resolved symbol follows an unknown one.** Measured:

| text | vocabulary | `tokenizers` 0.23.1 | offsets |
| --- | --- | --- | --- |
| `XY` | only `<0x58>` | `['<0x58>', '<unk>']` | `(0,1) (1,2)` |
| `YX` | only `<0x58>` | `['<0x58>', '<unk>']` | `(0,1) (1,2)` |
| `YYX` | only `<0x58>` | `['<unk>', '<0x58>', '<unk>']` | `(0,1) (1,2) (2,3)` |

`XY` and `YX` produce the same stream, and the offsets place `<0x58>` on the `Y`. The pending
unknown is flushed after the byte-fallback branch rather than before it. It reproduces with
`fuse_unk` off, so it is not the fusing.

## The decision this rests on

**A vocabulary declaring `byte_fallback: true` must carry all 256 `<0xXX>` pieces, or the load
is refused naming the first one missing.**

This is stricter than `tokenizers`, deliberately, and it is what #317 asks for. It also settles
the rest of the lot for free: with the complete alphabet **no symbol ever falls to the unknown
token**, because every character's UTF-8 bytes are all present. The two traps are unreachable,
and so is the ordering bug — there is no buggy region left to reproduce or to diverge from.

The alternative — accept the partial alphabet and reproduce the degradation — loses on 0050 §4's
own rule: refusing beats producing embeddings that are quietly wrong, and a silently dropped
symbol whose neighbours then merge is as wrong as it gets. Reproducing the *ordering* on top of
that would mean writing a known-wrong stream into an oracle and defending it later.

**This rests on an assumption, and the assumption is named rather than assumed.** SentencePiece
writes the 256 byte pieces into the vocabulary when a model is trained with `byte_fallback` —
that is where the pieces come from at all — so a checkpoint of this lineage carries them. That
was **not** verifiable from the session that wrote this spec: `huggingface.co` is unreachable
behind the network policy, and no model artifact is committed here (CONTRIBUTING.md). If a real
file ever turns out to omit one, the refusal is what surfaces it, by name, at load — which is the
outcome to want from a wrong assumption.

## What is settled and out of scope

- **Whether `byte_fallback` is reproduced at all.** 0050 §3 decided it. This spec is the *how*.
- **The whitespace escape.** #316 landed it; nothing here touches `MetaspaceEscape` or its two
  spellings.
- **`SentencePieceTokenizer` and the Unigram path.** Both `SentencePieceModelLoader` and
  `LoadUnigram` keep refusing a model trained with `byte_fallback`; that is the Unigram lineage
  and a separate lot.
- **`ignore_merges`.** Llama-3 declares it, this lineage does not, and its interaction with byte
  pieces is not measured here.
- **The upstream ordering bug.** Not reproduced, because the refusal above makes it unreachable.
  It is recorded so a later reader does not rediscover it as ours.

## The shape

### `BpeVocabulary` gains one flag

```csharp
/// <summary>Whether an uncovered symbol resolves into <c>&lt;0xXX&gt;</c> byte pieces.</summary>
public bool ByteFallback { get; init; }
```

`false` is today's behaviour word for word, and `Equals`/`GetHashCode` cover it.

### The loader stops refusing, and starts checking

`EnsureByteFallbackIsOff` leaves the BPE path — `ReadBpe` reads the flag instead. It **stays** on
the Unigram path, where the refusal is still true.

`EnsureNotByteFallbackBpe` also stays, and its message changes. It sends a reader who called
`LoadUnigram` on a Llama-2 file to `LoadBpe` (#343), and today it does so by saying `LoadBpe`
"refuses byte_fallback by name too -- neither loader reproduces this checkpoint". After this lot
that half is false, and a false reason in a refusal is worse than none: the message keeps the
routing and drops the claim.

In their place, one new check:

```csharp
/// <summary>Every one of the 256 byte pieces a byte_fallback vocabulary promises, or a refusal naming the first missing one.</summary>
private static void EnsureByteAlphabetIsComplete(IReadOnlyDictionary<string, int> vocab)
{
    for (int b = 0; b < 256; b++)
    {
        string piece = BytePieces.Name(b);
        if (!vocab.ContainsKey(piece))
        {
            throw Unsupported(
                $"its model declares byte_fallback and its vocabulary has no '{piece}' piece",
                "the reference resolves an uncovered symbol into byte pieces and silently emits the unknown token when one is missing, so a partial alphabet encodes differently from the model it came from");
        }
    }
}
```

### One place knows the spelling

```csharp
/// <summary>The <c>&lt;0xXX&gt;</c> pieces a byte_fallback vocabulary is required to carry.</summary>
internal static class BytePieces
{
    /// <summary>The piece one byte resolves to, spelled as tokenizers spells it: uppercase hexadecimal.</summary>
    internal static string Name(int value);

    /// <summary>The byte a piece names, or false when the token is not one of the 256.</summary>
    internal static bool TryValue(string token, out byte value);
}
```

Both directions live together because encode needs the first and decode the second, and a
lowercase spelling has to fail in both.

### `InitialSymbols` gains a branch, between the lookup and the unknown

Today: covered → its id; else unknown token; else dropped. The branch goes in the middle, and
because it runs before `Merge`, measurement 4 comes out right with no further work.

```csharp
if (_modelVocab.TryGetValue(symbol, out int id))
{
    symbols[count++] = id;
    previousWasSubstituted = false;
}
else if (_byteFallback)
{
    // Measurement 5: the decorated string is what is expanded, decoration included.
    count += ExpandToBytes(symbol, symbols.Slice(count));
    previousWasSubstituted = false;
}
else if (_hasUnk) { … }
```

`previousWasSubstituted = false` is measurement 6: a byte-resolved symbol breaks a run of
unknowns rather than joining it. The load-time check makes `ExpandToBytes` total — every byte has
a piece — so it cannot half-expand and has nothing to report.

**The span has to grow.** `EncodePiece` sizes `symbols` by `piece.Length` on the non-byte-level
path; one symbol can become four pieces, and with decoration more. Under `byte_fallback` it takes
the UTF-8 byte count the byte-level branch already computes, plus the decoration each symbol can
carry.

### Decode reassembles, because the file says how

The chain Llama-2 declares is `Sequence[Replace ▁→" ", ByteFallback, Fuse, Strip(" ", 1, 0)]`,
and measured end to end it round-trips: `aéb` encodes to `['▁a', '<0xC3>', '<0xA9>', 'b']` and
decodes to `aéb`. `ByteFallback` alone gives `▁aéb`; no decoder at all gives
`▁a <0xC3> <0xA9> b`, which is what this package returns today.

Decode cannot work on the concatenated buffer: `<0xC3>` is a *token*, and its six characters mean
nothing once joined. So `Append` gains the branch — a token `BytePieces.TryValue` recognises goes
to a pending byte buffer, and any other token flushes that buffer through `Utf8Lossy` first.
`Utf8Lossy` is already the substitution
[decision 0023](../../decisions/0023-byte-level-decode-substitutes.md) settled, so a lone
`<0xC3>` decodes to U+FFFD rather than throwing — measured, `tokenizers` does the same.

The declared `decoder` block is **read**, not assumed: the `Sequence` above and a bare
`ByteFallback` are reproduced, and any other shape is refused by name, which is 0050 §4's rule
applied to the decode side. That closes what
[decision 0062](../../decisions/0062-the-two-metaspace-spellings-part-on-the-prepend-twice.md)
left open, where a `Metaspace` decoder loaded and was ignored.

## What proves it

A new corpus, `tests/oracles/bpe_byte_fallback.json`, replayed whole:

- the six measurements above, each as its own case — one model per shape, since the shapes differ
  in their vocabulary, not only in their text;
- a text per byte width: ASCII, `é` (2), `日` (3), `🙂` (4), and a control character;
- `fuse_unk` on and off over the same texts, so measurement 6 is pinned in both directions;
- the decode column, which the metaspace corpus deliberately does not carry — here the decoder is
  declared and reproduced, so it can.

Refusals are pinned by loader tests rather than by oracles, because `tokenizers` accepts the files
they name: a vocabulary missing `<0x00>`, one missing `<0xFF>`, one spelling `<0xc3>` in
lowercase, and a decoder shape outside the two reproduced.

The unigram corpora are the control: `SentencePieceTokenizer` is untouched and its oracles must
not move.

## The gate

- `docs/equivalence.md` gains the `byte_fallback` row and updates `LoadBpe`'s, in the same commit
  as the function.
- An ADR records the refusal as a deliberate divergence, and the upstream ordering bug as measured
  and not reproduced.
- `docs/reference/embeddings/persistence/tokenizerjsonloader-loadbpe.md` loses "Llama-2 and
  Mistral v0.1 are refused here by name" — after this lot they are not.
- `BpeTokenizer.Decode`'s round-trip qualification, added by 0062, is narrowed to what still
  qualifies.

## What this does not claim

It does not claim the two checkpoints are *validated* end to end: no model weights are committed
(CONTRIBUTING.md), and the oracle models here are small and synthetic. What it claims is that
every rule the two files exercise is measured against the reference and reproduced, and that the
shapes outside those rules are refused rather than guessed.
