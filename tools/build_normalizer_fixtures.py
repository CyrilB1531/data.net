#!/usr/bin/env python3
"""Build the small models that let the normalizer oracle cover more than one blob.

`tests/oracles/xlmr_fairseq.model` already carries the `nmt_nfkc` charsmap — the
one every stock T5, ALBERT, camemBERT and XLM-R ships, byte for byte. It is the
important case, and it is free, but on its own it would leave a claim untested:
that `PrecompiledNormalizer` interprets *a charsmap*, not *that* charsmap.

So two more, trained here rather than downloaded:

  * `nmt_nfkc_cf.model` — the case-folding variant. Its blob is a different one,
    and case folding is the rule whose effect is impossible to mistake for a
    pass-through.
  * `custom_norm.model` — trained from a hand-written
    `--normalization_rule_tsv`, three rules and nothing else: `ß` -> `ss` (one
    character to two), `①` -> `1` (one to one), `¤` -> nothing (a deletion) —
    deliberately none performed by any built-in spec, so a fixture claiming to
    apply them cannot be passing by accident. This is the case Route B could
    never have covered: no name identifies it, and only the compiled map says
    what it does. Its charsmap is a few hundred bytes, which also gives the
    trie interpreter a small input to be exercised on.

Both are trained on a corpus written into this file, with a fixed vocabulary
size and no randomness in the settings. They are *inputs* to
`generate_oracles.py`, committed like `tiny_sp.model`, so CI never retrains
them:

    python tools/build_normalizer_fixtures.py            # rebuild
    python tools/build_normalizer_fixtures.py --check    # verify the checked-in fixtures

`--check` compares against a fresh training run. For that to mean anything, the
three fields recording *where* the training ran are cleared first — see
`strip_local_paths`. What remains is stable run to run on a pinned
`sentencepiece`, and is not guaranteed across versions, which is why the models
are committed rather than produced on demand. A difference reported here is a
decision to make, not a file to overwrite quietly: the ids in `normalizer.json`
move with it.
"""

from __future__ import annotations

import argparse
import pathlib
import sys
import tempfile

import sentencepiece as spm
from sentencepiece import sentencepiece_model_pb2 as model_pb2

ORACLE_DIR = pathlib.Path(__file__).resolve().parent.parent / "tests" / "oracles"

# The content doesn't matter: the charsmap under test comes from the
# normalization rule, not from the corpus.
CORPUS = [
    "the quick brown fox jumps over the lazy dog",
    "le renard brun rapide saute par-dessus le chien paresseux",
    "el zorro marron rapido salta sobre el perro perezoso",
    "der schnelle braune Fuchs springt uber den faulen Hund",
    "быстрая коричневая лиса прыгает через ленивую собаку",
    "速い茶色のキツネが怠け者の犬を飛び越える",
    "tokenization segments text into pieces",
    "la tokenisation decoupe le texte en morceaux",
    "unigram models find the best segmentation",
    "sentencepiece normalizes before it segments",
]

# from-code-points TAB to-code-points, the format spm_train reads. See the
# custom_norm.model bullet in this module's docstring for what each rule proves.
CUSTOM_TSV = "00DF\t0073 0073\n2460\t0031\n00A4\t\n"

FIXTURES = {
    "nmt_nfkc_cf.model": {"normalization_rule_name": "nmt_nfkc_cf"},
    "custom_norm.model": {"normalization_rule_tsv": CUSTOM_TSV},
}


def train(settings: dict) -> bytes:
    """Train one tiny unigram model and return its serialized proto.

    newline="\\n" on the corpus and rules files below: they are inputs to
    spm_train, not its output, but the trained model this function returns is
    committed and compared byte for byte by --check. Left to the platform
    default, a stray "\\r" from a Windows-translated corpus or rules file could
    feed the trainer different bytes than the ones the checked-in model was
    trained on.
    """
    with tempfile.TemporaryDirectory() as tmp:
        work = pathlib.Path(tmp)
        # newline="\n": see this function's docstring for why.
        corpus = work / "corpus.txt"
        corpus.write_text("\n".join(CORPUS) + "\n", encoding="utf-8", newline="\n")

        options = dict(settings)
        tsv = options.pop("normalization_rule_tsv", None)
        if tsv is not None:
            rules = work / "rules.tsv"
            rules.write_text(tsv, encoding="utf-8", newline="\n")
            options["normalization_rule_tsv"] = str(rules)

        spm.SentencePieceTrainer.train(
            input=str(corpus),
            model_prefix=str(work / "model"),
            # The trainer refuses a size the corpus cannot fill, and the ceiling
            # moves with the corpus — 120 leaves room under it for both rules.
            vocab_size=120,
            model_type="unigram",
            character_coverage=0.9995,
            # The layout the loader expects to find declared, spelled out rather
            # than left to the trainer's defaults.
            unk_id=0,
            bos_id=1,
            eos_id=2,
            pad_id=-1,
            add_dummy_prefix=True,
            remove_extra_whitespaces=True,
            **options,
        )
        return strip_local_paths((work / "model.model").read_bytes())


def strip_local_paths(model: bytes) -> bytes:
    """Erase the three fields that record where the training ran.

    Without this the fixtures are not byte-reproducible, for a reason that has
    nothing to do with training: sentencepiece copies `input`, `model_prefix` and
    `normalization_rule_tsv` into the proto verbatim, and every run gets a fresh
    temporary directory. Everything that matters — the charsmap, the pieces,
    their scores — is identical run to run; only the paths move. Clearing them
    also keeps a local directory name out of a committed file.
    """
    proto = model_pb2.ModelProto()
    proto.ParseFromString(model)
    proto.trainer_spec.ClearField("input")
    proto.trainer_spec.ClearField("model_prefix")
    proto.normalizer_spec.ClearField("normalization_rule_tsv")
    return proto.SerializeToString()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="verify the checked-in fixtures, write nothing")
    args = parser.parse_args()

    status = 0
    for filename, settings in FIXTURES.items():
        path = ORACLE_DIR / filename
        built = train(settings)
        if args.check:
            if not path.exists():
                print(f"{path} does not exist; rerun without --check", file=sys.stderr)
                status = 1
            elif path.read_bytes() != built:
                print(
                    f"{path} is not what this script produces "
                    f"({path.stat().st_size} bytes on disk, {len(built)} rebuilt)",
                    file=sys.stderr,
                )
                status = 1
            else:
                print(f"{path} is up to date ({len(built)} bytes)", file=sys.stderr)
            continue
        path.write_bytes(built)
        print(f"wrote {path} ({len(built)} bytes)", file=sys.stderr)
    return status


if __name__ == "__main__":
    raise SystemExit(main())
