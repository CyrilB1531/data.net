#!/usr/bin/env python3
"""Generate the vocabulary corpus for the persistence benchmarks.

Both the Python harness and the C# harness load *these same files*, so the
comparison measures the loaders on identical input. Output is git-ignored: it is
~3 MB of generated data, and comparability comes from both sides reading the same
file on disk rather than from the bytes being reproducible across machines.

    python bench/corpus/generate_vocabs.py
"""

from __future__ import annotations

import json
import sys
import tempfile
from pathlib import Path

# These generators are standalone scripts run by hand, not a package. Adding the
# repository root rather than tools/ is what lets the import below be spelled as
# a path from the root, which is where every static analyser starts looking too.
sys.path.append(str(Path(__file__).resolve().parents[2]))

from tools.seeded_random import SeededRandom  # noqa: E402

import sentencepiece as spm
from tokenizers import Tokenizer
from tokenizers.models import Unigram, WordPiece
from tokenizers.normalizers import Lowercase
from tokenizers.pre_tokenizers import Metaspace, Whitespace

SEED = 20260805
VOCAB_SIZE = 30_522          # what BERT ships
DOCUMENT_COUNT = 5_000
WORDS_PER_DOCUMENT = 40
SPECIALS = ["[UNK]", "[CLS]", "[SEP]", "[PAD]", "[MASK]"]

OUT = Path(__file__).resolve().parent / "vocabs"


def make_tokens(rng: SeededRandom) -> list[str]:
    """A BERT-shaped vocabulary: specials, whole words, then ## continuations."""
    alphabet = "abcdefghijklmnopqrstuvwxyz"
    tokens = list(SPECIALS)
    seen = set(tokens)
    while len(tokens) < VOCAB_SIZE:
        length = rng.randint(2, 9)
        word = "".join(rng.choice(alphabet) for _ in range(length))
        # Roughly a third of a real WordPiece table is continuation pieces.
        token = f"##{word}" if rng.random() < 0.33 else word
        if token not in seen:
            seen.add(token)
            tokens.append(token)
    return tokens


def write_vocab_txt(tokens: list[str]) -> None:
    (OUT / "vocab_30k.txt").write_text("\n".join(tokens) + "\n", encoding="utf-8")


def write_wordpiece_json(tokens: list[str]) -> None:
    """Saved by `tokenizers` itself, so the Python side is guaranteed to load it."""
    vocab = {token: index for index, token in enumerate(tokens)}
    tokenizer = Tokenizer(WordPiece(vocab, unk_token="[UNK]", max_input_chars_per_word=100))
    tokenizer.normalizer = Lowercase()
    tokenizer.pre_tokenizer = Whitespace()
    tokenizer.save(str(OUT / "tokenizer_30k_wordpiece.json"))


def write_unigram_json(tokens: list[str], rng: SeededRandom) -> None:
    # Unigram entries are (piece, log-probability) and id 0 must be the unknown.
    pieces = [("<unk>", 0.0)]
    pieces += [(f"▁{t.lstrip('#')}", -rng.uniform(1.0, 14.0)) for t in tokens[1:]]
    tokenizer = Tokenizer(Unigram(pieces, unk_id=0, byte_fallback=False))
    tokenizer.pre_tokenizer = Metaspace()
    tokenizer.save(str(OUT / "tokenizer_30k_unigram.json"))


def write_bpe_json(documents: list[str]) -> None:
    """A byte-level BPE, trained on the same documents the other three see.

    Task 13 benchmarks this against the shipped unigram tokenizer, so the
    comparison is only meaningful if both saw the same text at the same
    vocabulary size -- hence the shared ``documents`` and ``VOCAB_SIZE`` rather
    than a fresh corpus or a different target.
    """
    from tokenizers import Tokenizer  # noqa: PLC0415
    from tokenizers.decoders import ByteLevel as ByteLevelDecoder  # noqa: PLC0415
    from tokenizers.models import BPE  # noqa: PLC0415
    from tokenizers.pre_tokenizers import ByteLevel  # noqa: PLC0415
    from tokenizers.trainers import BpeTrainer  # noqa: PLC0415

    tokenizer = Tokenizer(BPE(unk_token=None))
    tokenizer.pre_tokenizer = ByteLevel(add_prefix_space=False)
    tokenizer.decoder = ByteLevelDecoder()
    tokenizer.train_from_iterator(
        documents,
        BpeTrainer(
            vocab_size=VOCAB_SIZE,
            min_frequency=1,
            initial_alphabet=ByteLevel.alphabet(),
            show_progress=False,
        ),
    )
    tokenizer.save(str(OUT / "tokenizer_30k_bpe.json"))


def write_spiece_model(documents: list[str]) -> None:
    """Train a real model: SentencePieceProcessor is the Python side of this
    comparison and validates more than a hand-assembled proto is known to satisfy.

    The trained vocabulary lands slightly under VOCAB_SIZE (~29.9k). The corpus is
    random letter strings, which share no morphology, so the unigram trainer has
    little reason to decompose words and converges near one piece per unique word.
    hard_vocab_limit=False accepts that shortfall. It does not matter here: this
    corpus exists to be *loaded*, and a 2% difference in piece count does not change
    what loading costs.
    """
    with tempfile.TemporaryDirectory() as tmp:
        corpus = Path(tmp) / "corpus.txt"
        corpus.write_text("\n".join(documents) + "\n", encoding="utf-8")
        spm.SentencePieceTrainer.train(
            input=str(corpus),
            model_prefix=str(Path(tmp) / "spiece"),
            vocab_size=VOCAB_SIZE,
            model_type="unigram",          # the loader reproduces unigram only
            normalization_rule_name="identity",   # and the identity normalizer only
            byte_fallback=False,
            num_threads=1,
            unk_id=0,
            bos_id=1,
            eos_id=2,
            pad_id=3,
            hard_vocab_limit=False,
        )
        (OUT / "spiece_30k.model").write_bytes((Path(tmp) / "spiece.model").read_bytes())


def make_documents(rng: SeededRandom, tokens: list[str]) -> list[str]:
    """Documents for the TF-IDF volet, drawn from the same token pool so the
    fitted vocabulary lands within a couple of percent of VOCAB_SIZE."""
    words = sorted({t.lstrip("#") for t in tokens if not t.startswith("[")})
    return [
        " ".join(rng.choice(words) for _ in range(WORDS_PER_DOCUMENT))
        for _ in range(DOCUMENT_COUNT)
    ]


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    rng = SeededRandom(SEED)
    tokens = make_tokens(rng)
    documents = make_documents(rng, tokens)

    write_vocab_txt(tokens)
    write_wordpiece_json(tokens)
    write_unigram_json(tokens, rng)
    write_bpe_json(documents)
    write_spiece_model(documents)
    (OUT / "documents.json").write_text(json.dumps(documents), encoding="utf-8")

    for path in sorted(OUT.iterdir()):
        print(f"  {path.name:32} {path.stat().st_size / 1024:8.0f} KB")
    print(f"-> {OUT}")


if __name__ == "__main__":
    main()
