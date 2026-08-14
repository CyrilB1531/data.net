#!/usr/bin/env python3
"""Rebuild tests/oracles/xlmr_fairseq.model from the XLM-R vocabulary.

XLM-R is one of the families `SentencePieceTokenizer` advertises, and the one
issue #63 was filed about: HuggingFace lays its vocabulary out as `<s>`=0,
`<pad>`=1, `</s>`=2, `<unk>`=3 and `<mask>` last, so a tokenizer that decides
what is a control piece from the id fails on it — silently, with a different
segmentation and therefore different embeddings.

The stock `sentencepiece.bpe.model` cannot serve as that fixture as it stands:
**its own layout is the raw one** — `<unk>`=0, `<s>`=1, `</s>`=2, no `<pad>`, no
`<mask>`. The fairseq numbering lives in the HuggingFace tokenizer wrapper, not
in the file, so the stock file exercises exactly the case the old id guess got
*right*.

So this script re-emits the vocabulary rather than copying it. The 250 000
pieces keep their strings, their scores and the ids HuggingFace gives them —
`XLMRobertaTokenizer` maps spm id `i` to `i + 1` for `i >= 3`, which is
reproduced here — while the four fairseq specials take 0-3 and `<mask>` takes
250001. Each special is typed `CONTROL` or `UNKNOWN`, so the file states what
they are instead of leaving it to be inferred.

The **`normalizer_spec` is copied across untouched**, `nmt_nfkc` and its 237 539
byte `precompiled_charsmap` included. It was overwritten with `identity` when
this script was written, because `SentencePieceModelLoader` refused anything
else; #75 removed that limitation, and decision 0013 asked for this to be
revisited the day it did. The fixture is now the stock XLM-R pipeline, with the
vocabulary relabelled — one transformation instead of two.

    python tools/fetch_xlmr_vocab.py           # rebuild
    python tools/fetch_xlmr_vocab.py --check   # verify the checked-in fixture

The download is checked against the SHA-256 pinned below before anything is
written. A mismatch means the upstream file changed: read the diff, update the
pin, regenerate the oracle in the same commit, and expect the ids in
`xlmr_fairseq.json` to move.

`xlm-roberta-base` is MIT-licensed (https://huggingface.co/xlm-roberta-base).
Only the vocabulary is redistributed here — never the weights, per
docs/decisions/0003-provenance-and-licensing.md. The attribution is recorded in
THIRD-PARTY-NOTICES.md.
"""

from __future__ import annotations

import argparse
import hashlib
import pathlib
import sys
import urllib.request

from sentencepiece import sentencepiece_model_pb2 as model_pb2

URL = "https://huggingface.co/xlm-roberta-base/resolve/main/sentencepiece.bpe.model"

# Retrieved 2026-08-06.
PINNED_SHA256 = "cfc8146abe2a0488e9e2a0c56de7952f7c11ab059eca145a0a727afce0db2865"

OUTPUT = pathlib.Path(__file__).resolve().parent.parent / "tests/oracles/xlmr_fairseq.model"

# transformers.XLMRobertaTokenizer: the four specials it places itself, then
# every spm piece shifted by the offset, then <mask> after the last one.
BOS = "<s>"
PAD = "<pad>"
EOS = "</s>"
UNK = "<unk>"
MASK = "<mask>"
FAIRSEQ_SPECIALS = [BOS, PAD, EOS, UNK]
FAIRSEQ_OFFSET = 1

# The spm pieces the fairseq specials replace: <unk>, <s>, </s> at 0, 1, 2.
REPLACED_SPM_PIECES = 3

CONTROL = model_pb2.ModelProto.SentencePiece.CONTROL
UNKNOWN = model_pb2.ModelProto.SentencePiece.UNKNOWN


def download(url: str) -> bytes:
    # S310: the only call passes the https:// constant above, so the scheme
    # can't be steered to file://. Reason lives here, not on the noqa line itself.
    with urllib.request.urlopen(url) as response:  # noqa: S310
        return response.read()


def relabel(stock: bytes) -> bytes:
    """Re-emit the stock vocabulary in fairseq layout, normalizer untouched."""
    source = model_pb2.ModelProto()
    source.ParseFromString(stock)

    for reserved in (*FAIRSEQ_SPECIALS, MASK):
        if any(p.piece == reserved for p in source.pieces[REPLACED_SPM_PIECES:]):
            raise SystemExit(
                f"{reserved} is already an ordinary piece of the stock vocabulary; "
                "adding it as a special would put the same string at two ids."
            )

    out = model_pb2.ModelProto()
    out.trainer_spec.CopyFrom(source.trainer_spec)

    # Copied, not rewritten: nmt_nfkc and its charsmap are half of what makes this
    # XLM-R rather than a vocabulary that once was.
    out.normalizer_spec.CopyFrom(source.normalizer_spec)

    def add(piece: str, score: float, piece_type: int) -> None:
        entry = out.pieces.add()
        entry.piece, entry.score, entry.type = piece, score, piece_type

    for special in FAIRSEQ_SPECIALS:
        add(special, 0.0, UNKNOWN if special == UNK else CONTROL)
    for piece in source.pieces[REPLACED_SPM_PIECES:]:
        add(piece.piece, piece.score, piece.type)
    add(MASK, 0.0, CONTROL)

    expected_mask_id = len(source.pieces) + FAIRSEQ_OFFSET
    if out.pieces[expected_mask_id].piece != MASK:
        raise SystemExit(f"<mask> landed at {len(out.pieces) - 1}, not at {expected_mask_id}.")

    out.trainer_spec.unk_id = FAIRSEQ_SPECIALS.index(UNK)
    out.trainer_spec.bos_id = FAIRSEQ_SPECIALS.index(BOS)
    out.trainer_spec.eos_id = FAIRSEQ_SPECIALS.index(EOS)
    out.trainer_spec.pad_id = FAIRSEQ_SPECIALS.index(PAD)
    out.trainer_spec.vocab_size = len(out.pieces)

    return out.SerializeToString()


def build() -> bytes:
    stock = download(URL)
    digest = hashlib.sha256(stock).hexdigest()
    if digest != PINNED_SHA256:
        raise SystemExit(
            f"{URL}\n  expected sha256 {PINNED_SHA256}\n  got      sha256 {digest}\n"
            "Upstream changed the vocabulary. Read the diff before updating the pin."
        )
    return relabel(stock)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="verify the checked-in fixture, write nothing")
    args = parser.parse_args()

    fixture = build()
    if args.check:
        # A verdict via exit status, like fetch_stopwords.py --check -- not an
        # abort. build()'s pin mismatch is different: the upstream file moved, no answer available.
        if not OUTPUT.exists():
            print(f"{OUTPUT} does not exist; rerun without --check", file=sys.stderr)
            return 1
        current = OUTPUT.read_bytes()
        if current != fixture:
            print(
                f"{OUTPUT} is not what this script produces "
                f"({len(current)} bytes on disk, {len(fixture)} rebuilt)",
                file=sys.stderr,
            )
            return 1
        print(f"{OUTPUT} is up to date ({len(current)} bytes)", file=sys.stderr)
        return 0

    OUTPUT.write_bytes(fixture)
    print(f"wrote {OUTPUT} ({len(fixture)} bytes)", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
