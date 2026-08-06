"""Rebuild the synthetic ONNX encoders committed under ``tests/oracles/``.

Model weights are never committed (``CONTRIBUTING.md``), so the ONNX path is
exercised against models small enough to read: a couple of nodes and a table of
a few hundred bytes. This script is how they are produced, so "synthetic" is a
verifiable claim rather than an assertion about two opaque binaries.

It is deliberately **not** part of ``generate_oracles.py``: building an ONNX
graph needs the ``onnx`` package, which the oracle lock file does not carry, and
the models are frozen fixtures rather than reference values that must track a
library version. Run it only when a fixture has to change:

    python -m venv .venv-onnx
    .venv-onnx/bin/pip install onnx==1.16.0
    .venv-onnx/bin/python tools/build_tiny_models.py

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


def main() -> None:
    for filename, build in (("tiny_encoder.onnx", build_tiny_encoder),
                            ("tiny_embedder.onnx", build_tiny_embedder)):
        model = build()
        onnx.checker.check_model(model)
        path = ORACLE_DIR / filename
        path.write_bytes(model.SerializeToString())
        print(f"{filename}: {path.stat().st_size} bytes -> {path}")


if __name__ == "__main__":
    main()
