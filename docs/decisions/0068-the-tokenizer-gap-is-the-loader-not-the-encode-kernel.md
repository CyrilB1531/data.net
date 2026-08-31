# 0068 — The tokenizer gap is the loader, not the encode kernel

**Status:** accepted · **Date:** 2026-08-31

## Context

[#438](https://github.com/CyrilB1531/lodestar/issues/438)'s Embeddings box measured
`Microsoft.ML.Tokenizers` 2.0.0 against ours over the same artefacts, both sides returning
identical ids:

| model | Lodestar | ML.Tokenizers | ratio | Lodestar alloc. | ML.Tokenizers alloc. |
| --- | ---: | ---: | ---: | ---: | ---: |
| WordPiece | 112.36 ms | 54.33 ms | 0.48 | 118.84 MB | 3.55 MB |
| SentencePiece (unigram) | 682.64 ms | 56.94 ms | 0.08 | 519.51 MB | 3.09 MB |

Container timings, so the ratios wait on a named machine
([ADR 0051](0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md)); the allocation is a
property of the code path and does not.

That is the shape of evidence that should retire a "gap", and `CLAUDE.md` names tokenizers as one:

> write native code only where .NET has a real gap — text (distances, vectorization, **tokenizers**,
> embeddings) […] Everything else is delegated to existing .NET libraries.

[#500](https://github.com/CyrilB1531/lodestar/issues/500) asks whether that clause still holds.

## What was established

**1. `Microsoft.ML.Tokenizers` cannot load a `tokenizer.json`.** Every public factory on the
2.0.0 assembly, by reflection — `BertTokenizer`, `BpeTokenizer`, `CodeGenTokenizer`,
`EnglishRobertaTokenizer`, `LlamaTokenizer`, `Phi2Tokenizer`, `SentencePieceTokenizer`,
`TiktokenTokenizer`, `WordPieceTokenizer` — takes a vocabulary file, a merges file, a
`spiece.model` or an options object. `BpeTokenizer.Create` accepts a `PreTokenizer` and a
`Normalizer` as **constructed C# objects**, never as a file to read. No exported type or member
contains `Json` or `HuggingFace`.

**2. That artefact is the one users have.** Llama-2 and Mistral v0.1 ship `tokenizer.json`:
normalizer, pre-tokenizer with its metaspace prepend scheme, model, decoder sequence and added
tokens, in one file. Reading it is what
[#316](https://github.com/CyrilB1531/lodestar/issues/316) and
[#317](https://github.com/CyrilB1531/lodestar/issues/317) built, and
[#175](https://github.com/CyrilB1531/lodestar/issues/175) opens on the fact that neither tokenizer
in .NET loads them.

**3. The two-target rule survives either way.** The package ships `net8.0` and `netstandard2.0`,
so delegating would not force a reduced API on the broad-reach target — the objection that would
have settled this cheaply does not apply. It costs `Google.Protobuf` on both targets and nine more
packages on `netstandard2.0`, against a `Lodestar.Embeddings` that isolates ONNX Runtime and reads
`spiece.model` through `ProtobufReader`, its own 200-line reader of the wire format's four fields.

## Decision

**The gap is the loader, and it stays ours. The encode kernels are not a gap, and losing to the
incumbent there is a defect rather than an argument to delegate.**

Those are two different questions and #500 was right to ask whether they are one. They are not:

- **The loader** — `TokenizerJsonLoader`, `SentencePieceModelLoader`, the vocabulary types — has
  no counterpart. Delegating it would mean writing the parser anyway, then handing
  `Microsoft.ML.Tokenizers` the pieces it accepts, and discovering which parts of a
  `tokenizer.json` its object model cannot express. The metaspace prepend scheme 0062 records and
  the strict decoder sequence 0063 requires are both in that category.
- **The kernels** — the encode paths behind those types — are ordinary code that is currently
  worse than an available alternative by two orders of magnitude of allocation.
  [#498](https://github.com/CyrilB1531/lodestar/issues/498) is that defect, and it is fixable
  without changing one public signature or one oracle.

So no dependency is added, no `docs/migration/` row moves to *Use*, and `CLAUDE.md`'s clause holds
— narrowed to what the measurement supports: the gap is loading the artefacts a user actually has,
not the arithmetic underneath.

## Options refused

**Delegate the tokenizers to `Microsoft.ML.Tokenizers`.** It would drop `tokenizer.json` support,
which is the only reason `Lodestar.Embeddings` can load Llama-2 and Mistral v0.1 at all — #175's
entire premise. Faster code that cannot open the file is not a substitute.

**Delegate the kernels, keep the loader — parse the JSON and construct their tokenizers.** The
tempting middle. Refused for now on two grounds: their object model has to be able to express what
we parse (the prepend scheme and the four-step decoder sequence are the known doubts, and finding
the rest is the work), and it trades a fixable allocation defect for a permanent dependency on a
type surface we do not control. Worth revisiting **if** #498 turns out not to be fixable — this
ADR does not close that door, it declines to walk through it before trying the cheap thing.

**Keep the kernels as they are, since the loader justifies the package.** That is the reading this
ADR most wants to refuse. The measurement is not invalidated by the gap being elsewhere: 519 MB to
encode 5 000 short documents is bad on its own terms, and #438 exists so that a claim of
"comparable" is checked rather than assumed.

## Consequences

- `Lodestar.Embeddings` gains no dependency, and its public surface does not change.
- #498 is the follow-up this decision points at, and its outcome is the input to any revisit.
- `docs/migration/README.md` states which half of the tokenizer work is native and why, so a
  reader meeting `Microsoft.ML.Tokenizers` first is not left to guess.
- The claim is now narrower than `CLAUDE.md`'s single word "tokenizers", and narrower on purpose.
