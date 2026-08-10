"""Rebuild the tiny fixtures committed under ``tests/oracles/``: two synthetic
ONNX encoders, a trained character-level BPE, a hand-constructed BPE holding
one orphaned vocabulary entry, and a hand-constructed BPE shaped after
``roberta-base``'s own ``added_tokens`` table.

Model weights are never committed (``CONTRIBUTING.md``), so the ONNX path is
exercised against models small enough to read: a couple of nodes and a table of
a few hundred bytes. This script is how they are produced, so "synthetic" is a
verifiable claim rather than an assertion about two opaque binaries.

It is deliberately **not** part of ``generate_oracles.py``: building an ONNX
graph needs the ``onnx`` package, which the oracle lock file does not carry,
and none of the five outputs here are reference values that must track a
library version the way ``generate_oracles.py``'s are — all five are frozen
fixtures, rebuilt only when one of them has to change:

    python -m venv .venv-tiny-models
    .venv-tiny-models/bin/pip install onnx==1.16.0 tokenizers==0.23.1
    .venv-tiny-models/bin/python tools/build_tiny_models.py

``tiny_encoder.onnx``
    ``last_hidden_state[b, t, :] = input_ids[b, t] * W`` — every token maps to a
    multiple of one direction, so the pooled sentence vector is ``W / ||W||``
    whatever the input. Enough to prove the runtime is fed correctly; blind, by
    construction, to anything pooling does.

``tiny_embedder.onnx``
    ``last_hidden_state[b, t, :] = E[input_ids[b, t]]`` — a real embedding
    lookup, so two different token sets pool to two different directions. That is
    what makes a padding leak observable: with the mask wrong, the padding row
    ``E[0]`` enters the mean and moves the vector. It also declares
    ``token_type_ids``, which ``tiny_encoder.onnx`` does not, so the branch that
    feeds it is covered.

``tiny_bpe.json``
    A trained character-level BPE — see ``build_tiny_bpe()`` for what it proves
    and, importantly, for why the committed file rather than this script is the
    reference: its trainer is not byte-reproducible across runs, so rerunning
    this recipe is not the same operation as regenerating an oracle.

``orphan_bpe_model.json``
    A hand-constructed BPE holding one *orphaned* vocabulary entry — see
    ``build_orphan_bpe()`` for what it proves. Unlike ``tiny_bpe.json``, it is
    **constructed, not trained**: every id and every merge is stated directly,
    so it is byte-reproducible across runs and carries none of
    ``tiny_bpe.json``'s caveat above.

``roberta_shaped_model.json``
    A hand-constructed byte-level BPE carrying ``roberta-base``'s own
    ``added_tokens`` table, verbatim — see ``build_roberta_shaped()`` for what
    it proves. Issue #104's own acceptance criterion. Constructed, not
    trained, so it is byte-reproducible across runs the same way
    ``orphan_bpe_model.json`` is.
"""

from __future__ import annotations

from pathlib import Path

import onnx
from onnx import TensorProto, helper, numpy_helper
import numpy as np

ORACLE_DIR = Path(__file__).resolve().parent.parent / "tests" / "oracles"

IR_VERSION = 9
OPSET = 13

# Kept in step with generate_oracles.py, which freezes the same table into
# batch_encoding.json; a C# test compares one gathered row against it, so the two
# cannot drift silently.
EMBEDDING_ROWS = 64
EMBEDDING_DIM = 4


def embedding_table() -> np.ndarray:
    """The synthetic embedding matrix: distinct rows, all exact in float32.

    Every entry is a multiple of 1/64 with magnitude below 1/2, so summing a few
    dozen of them is exact — which is what lets the oracle demand 1e-9 of a
    float32 pipeline instead of the 1e-5 a rounded sum would force.
    """
    table = np.zeros((EMBEDDING_ROWS, EMBEDDING_DIM), dtype=np.float32)
    for i in range(EMBEDDING_ROWS):
        for d in range(EMBEDDING_DIM):
            table[i, d] = (((7 * i + 13 * d) % 64) - 32) / 64.0
    return table


def _int64_input(name: str, dims: list[str]) -> onnx.ValueInfoProto:
    return helper.make_tensor_value_info(name, TensorProto.INT64, dims)


def build_tiny_encoder() -> onnx.ModelProto:
    weights = numpy_helper.from_array(
        np.array([[0.1, 0.2, 0.3, 0.4]], dtype=np.float32), name="W")
    axis2 = numpy_helper.from_array(np.array([2], dtype=np.int64), name="axis2")
    graph = helper.make_graph(
        [
            helper.make_node("Cast", ["input_ids"], ["ids_f"], to=TensorProto.FLOAT),
            helper.make_node("Unsqueeze", ["ids_f", "axis2"], ["ids_3d"]),
            helper.make_node("MatMul", ["ids_3d", "W"], ["last_hidden_state"]),
        ],
        "tiny",
        [_int64_input("input_ids", ["batch", "seq"]),
         _int64_input("attention_mask", ["batch", "seq"])],
        [helper.make_tensor_value_info(
            "last_hidden_state", TensorProto.FLOAT, ["batch", "seq", EMBEDDING_DIM])],
        [weights, axis2],
    )
    return helper.make_model(graph, ir_version=IR_VERSION,
                             opset_imports=[helper.make_opsetid("", OPSET)])


def build_tiny_embedder() -> onnx.ModelProto:
    table = numpy_helper.from_array(embedding_table(), name="E")
    graph = helper.make_graph(
        [helper.make_node("Gather", ["E", "input_ids"], ["last_hidden_state"], axis=0)],
        "tiny_embedder",
        [_int64_input("input_ids", ["batch", "seq"]),
         _int64_input("attention_mask", ["batch", "seq"]),
         _int64_input("token_type_ids", ["batch", "seq"])],
        [helper.make_tensor_value_info(
            "last_hidden_state", TensorProto.FLOAT, ["batch", "seq", EMBEDDING_DIM])],
        [table],
    )
    return helper.make_model(graph, ir_version=IR_VERSION,
                             opset_imports=[helper.make_opsetid("", OPSET)])


# A character-level BPE, the subword-nmt lineage: no byte alphabet, an explicit
# end-of-word marker, and a vocabulary small enough to read in a diff. It exists
# to exercise the merge loop on its own, with none of the byte-level mapping the
# GPT-2 fixture brings.
BPE_CORPUS = [
    "the quick brown fox jumps over the lazy dog",
    "tokenization is embedding embeddings",
    "the cat sat on the mat and the cat sat again",
    "lovely love loved lover loving",
    "bigger biggest big",
    "natural language processing processes language naturally",
    "machine learning and data science",
    "programming programs a program",
]


def build_tiny_bpe() -> str:
    """A trained character-level BPE, serialized as a tokenizer.json.

    ``BpeTrainer`` is not byte-reproducible across process runs: tokens and
    merges that tie in frequency break ties differently each time, because the
    Rust ``HashMap`` behind the frequency counts seeds its hash randomly per
    process. Vocabulary size and merge count come out the same every run; the
    ids assigned to tied tokens and their order among tied merges do not. The
    committed ``tests/oracles/tiny_bpe.json`` is therefore authoritative, not
    this function — running it again produces a valid but different model, so
    a diff there is expected and must never be committed without regenerating
    ``bpe.json`` in the same commit.
    """
    from tokenizers import Tokenizer  # noqa: PLC0415
    from tokenizers.models import BPE  # noqa: PLC0415
    from tokenizers.pre_tokenizers import Whitespace  # noqa: PLC0415
    from tokenizers.trainers import BpeTrainer  # noqa: PLC0415

    tokenizer = Tokenizer(BPE(unk_token="[UNK]", end_of_word_suffix="</w>"))
    tokenizer.pre_tokenizer = Whitespace()
    tokenizer.train_from_iterator(
        BPE_CORPUS,
        BpeTrainer(
            vocab_size=200,
            min_frequency=1,
            special_tokens=["[UNK]"],
            end_of_word_suffix="</w>",
            show_progress=False,
        ),
    )
    return tokenizer.to_str(pretty=True)


# A hand-constructed BPE, the shape ``ignore_merges`` exists for: a vocabulary
# entry unreachable by replaying the merge table. Three base characters (a, b,
# c) give two ordinary two-symbol entries via registered merges -- "ab" from
# (a, b), "bc" from (b, c) -- the same rule ``build_tiny_bpe()``'s trainer would
# have applied. A third, longer entry, "abc", is then added straight to the
# vocabulary, skipping the merge that would make it reachable: neither ("ab",
# "c") nor ("a", "bc") is registered, so encoding "abc" greedily lands on
# ["ab", "c"], not on the single token that is sitting right there in the
# vocabulary. That is the orphan, and it is the only reason this fixture
# exists. Two more base characters, x and y, round it out with pieces that
# never touch the orphan, for a corpus that can show the flag changing nothing
# where there is nothing to rescue.
ORPHAN_VOCAB = {"a": 0, "b": 1, "c": 2, "x": 3, "y": 4, "ab": 5, "bc": 6, "abc": 7}
ORPHAN_MERGES = [("a", "b"), ("b", "c")]  # neither ("ab","c") nor ("a","bc") is registered


def build_orphan_bpe() -> str:
    """A hand-constructed BPE with one orphaned vocabulary entry, serialized as a tokenizer.json.

    Unlike ``build_tiny_bpe()``, this model is **constructed, not trained**:
    ``ORPHAN_VOCAB`` and ``ORPHAN_MERGES`` are stated directly rather than
    produced by ``BpeTrainer``, so there is no tie-breaking hash to reseed and
    the output is byte-identical on every run. The committed file and this
    function can never drift from each other the way ``tiny_bpe.json`` and
    ``build_tiny_bpe()`` can.

    A pre-tokenizer is still needed to turn a multi-word input into pieces a
    merge can never cross, so this reuses ``Whitespace()`` -- the same type
    ``build_tiny_bpe()`` uses, and the split ``BpeVocabulary.PreTokenizerPattern
    = null`` reproduces on the C# side. Nothing else about the model -- no
    end-of-word suffix, no byte-level mapping, no unknown token -- is needed to
    make the orphan reachable or unreachable, so nothing else is declared.
    """
    from tokenizers import Tokenizer  # noqa: PLC0415
    from tokenizers.models import BPE  # noqa: PLC0415
    from tokenizers.pre_tokenizers import Whitespace  # noqa: PLC0415

    tokenizer = Tokenizer(BPE(ORPHAN_VOCAB, ORPHAN_MERGES))
    tokenizer.pre_tokenizer = Whitespace()
    return tokenizer.to_str(pretty=True)


# roberta-base's own added_tokens table, reproduced verbatim: ids 0-3 for <s>,
# <pad>, </s>, <unk> with every matching flag false, and id 50264 for <mask>
# with lstrip=True and every other flag false. All five sit in model.vocab at
# the same ids, which is how roberta-base itself writes them -- and 50264 is
# nowhere near contiguous with a tiny vocabulary's handful of ids, on purpose:
# the loader must not assume added-token ids are contiguous with the rest of
# the vocabulary just because this fixture's are small numbers.
ROBERTA_UNK_TOKEN = "<unk>"
ROBERTA_VOCAB = {
    "<s>": 0, "<pad>": 1, "</s>": 2, ROBERTA_UNK_TOKEN: 3, "a": 4, "b": 5, "ab": 6, "<mask>": 50264,
}
ROBERTA_MERGES = [("a", "b")]


def build_roberta_shaped() -> str:
    """A hand-constructed byte-level BPE carrying roberta-base's added_tokens table.

    Issue #104's own acceptance criterion: the library must load the
    added_tokens table roberta-base actually ships, not a synthetic stand-in
    for it. ``ROBERTA_VOCAB`` and ``ROBERTA_MERGES`` are stated directly, so
    -- like ``build_orphan_bpe()`` and unlike ``build_tiny_bpe()`` -- this is
    byte-reproducible across runs.

    The five entries are added through ``Tokenizer.add_special_tokens`` with
    every flag stated explicitly, which is what makes ``tokenizers`` itself
    write ``normalized`` into each entry: its deserializer refuses a
    tokenizer.json whose added-token entries omit that field, so the loader
    under test has to read it, not merely tolerate its absence, for a fixture
    this close to the real file. A bare ``ByteLevel`` pre_tokenizer and
    decoder, add_prefix_space off, match roberta-base's own pipeline shape.
    """
    from tokenizers import AddedToken, Tokenizer  # noqa: PLC0415
    from tokenizers.decoders import ByteLevel as ByteLevelDecoder  # noqa: PLC0415
    from tokenizers.models import BPE  # noqa: PLC0415
    from tokenizers.pre_tokenizers import ByteLevel as ByteLevelPreTokenizer  # noqa: PLC0415

    tokenizer = Tokenizer(BPE(ROBERTA_VOCAB, ROBERTA_MERGES, unk_token=ROBERTA_UNK_TOKEN))
    tokenizer.pre_tokenizer = ByteLevelPreTokenizer(add_prefix_space=False)
    tokenizer.decoder = ByteLevelDecoder()
    tokenizer.add_special_tokens([
        AddedToken("<s>", lstrip=False, rstrip=False, single_word=False, normalized=False, special=True),
        AddedToken("<pad>", lstrip=False, rstrip=False, single_word=False, normalized=False, special=True),
        AddedToken("</s>", lstrip=False, rstrip=False, single_word=False, normalized=False, special=True),
        AddedToken(ROBERTA_UNK_TOKEN, lstrip=False, rstrip=False, single_word=False, normalized=False, special=True),
        AddedToken("<mask>", lstrip=True, rstrip=False, single_word=False, normalized=False, special=True),
    ])
    return tokenizer.to_str(pretty=True)


def main() -> None:
    for filename, build in (("tiny_encoder.onnx", build_tiny_encoder),
                            ("tiny_embedder.onnx", build_tiny_embedder)):
        model = build()
        onnx.checker.check_model(model)
        path = ORACLE_DIR / filename
        path.write_bytes(model.SerializeToString())
        print(f"{filename}: {path.stat().st_size} bytes -> {path}")

    path = ORACLE_DIR / "tiny_bpe.json"
    path.write_text(build_tiny_bpe() + "\n", encoding="utf-8")
    print(f"tiny_bpe.json: {path.stat().st_size} bytes -> {path}")

    path = ORACLE_DIR / "orphan_bpe_model.json"
    path.write_text(build_orphan_bpe() + "\n", encoding="utf-8")
    print(f"orphan_bpe_model.json: {path.stat().st_size} bytes -> {path}")

    path = ORACLE_DIR / "roberta_shaped_model.json"
    path.write_text(build_roberta_shaped() + "\n", encoding="utf-8")
    print(f"roberta_shaped_model.json: {path.stat().st_size} bytes -> {path}")


if __name__ == "__main__":
    main()
