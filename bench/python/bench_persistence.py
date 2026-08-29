#!/usr/bin/env python3
"""Time the Python counterparts of the Lodestar persistence work.

Methodology is mirrored by the C# harness (bench/Lodestar.Text.Benchmarks,
`compare-persistence` mode) so the two are comparable:

  * same corpus files (bench/corpus/vocabs/),
  * metric: milliseconds per operation,
  * auto-scaling: repeat until a measurement lasts >= MIN_TIME,
  * report the best (minimum) of REPEATS measurements.

Note what the two sides each build. tokenizers and sentencepiece construct a
whole tokenizer -- the normalizer and pre-tokenizer graph, the Rust or C++
matcher it will encode with. Lodestar's loaders build a validated dictionary and
stop; the guides tell readers to construct a tokenizer from it as a second step.
A margin in Lodestar's favour therefore reflects, in part, work it does not do.
"""

from __future__ import annotations

import io
import json
import os
import pickle
import platform
import tempfile
from importlib.metadata import version
from pathlib import Path
from time import perf_counter, process_time

import numpy as np
import sentencepiece as spm
from sklearn.feature_extraction.text import TfidfVectorizer
from tokenizers import Tokenizer

MIN_TIME = 0.5
REPEATS = 5

ROOT = Path(__file__).resolve().parent.parent
VOCABS = ROOT / "corpus" / "vocabs"
OUT = ROOT / "results" / "python-persistence.json"

REQUIRED = [
    "vocab_30k.txt",
    "tokenizer_30k_wordpiece.json",
    "tokenizer_30k_unigram.json",
    "spiece_30k.model",
    "documents.json",
]


def check_corpus() -> None:
    missing = [name for name in REQUIRED if not (VOCABS / name).exists()]
    if missing:
        raise SystemExit(
            f"benchmark corpus incomplete, missing {missing} in {VOCABS}\n"
            "generate it first: python bench/corpus/generate_vocabs.py"
        )


def measure(operation: str, action, artifact_bytes: int = 0) -> dict:
    """Time one operation, recording both elapsed time and processor time.

    The C# harness records the same pair for the same reason: .NET's background
    collector does its work on other threads, so elapsed time understates what an
    allocation-heavy operation actually costs. CPython's unpickler is strictly
    single-threaded, so cpu/wall lands at 1.00 here -- which is exactly the point
    of reporting both.

    ``artifact_bytes`` is what the operation wrote or read, mirroring the C# field of
    the same name: #100's size figures were taken by hand and could not be re-checked,
    so a row now carries its own size. Zero means the operation moves no artifact.
    """
    best_wall, cpu_of_best = float("inf"), float("nan")
    for _ in range(REPEATS):
        iters = 1
        while True:
            c0, w0 = process_time(), perf_counter()
            for _ in range(iters):
                action()
            dt = perf_counter() - w0
            cpu = process_time() - c0
            if dt >= MIN_TIME:
                break
            iters *= 2
        wall_ms = dt / iters * 1e3
        if wall_ms < best_wall:
            best_wall, cpu_of_best = wall_ms, cpu / iters * 1e3
    size = f"  {artifact_bytes:12,d} B" if artifact_bytes else ""
    print(f"  {operation:<28} {best_wall:10.3f} ms/op  cpu {cpu_of_best:8.3f} ms/op  ({cpu_of_best / best_wall:.2f}x cores){size}")
    row = {"operation": operation, "ms_per_op": best_wall, "cpu_ms_per_op": cpu_of_best}
    if artifact_bytes:
        row["artifact_bytes"] = artifact_bytes
    return row


def build_vectors() -> "np.ndarray":
    """A 10 000 x 384 block, from the same xorshift32 seed the C# side uses.

    The two blocks are the same size and come from the same generator; they are not
    bit-identical, and do not need to be -- Lodestar normalizes on insertion, and what
    is being timed is how many floats there are rather than which ones.

    A .npy file is a short header followed by the raw little-endian block, so this
    row is the binary floor Lodestar's JSON + base64 artifact is measured against --
    not a competitor doing the same job, a lower bound on the job itself.
    """
    count, dimension = 10_000, 384
    out = np.empty((count, dimension), dtype=np.float32)
    state = 12345
    for item in range(count):
        for i in range(dimension):
            # Plain ints masked to 32 bits, which is what C#'s uint does anyway --
            # numpy's uint32 raises on the overflow these shifts depend on.
            state = (state ^ (state << 13)) & 0xFFFFFFFF
            state ^= state >> 17
            state = (state ^ (state << 5)) & 0xFFFFFFFF
            out[item, i] = (state & 0xFFFFFF) / 0xFFFFFF - 0.5
    return out


def main() -> None:
    check_corpus()

    vocab_txt = str(VOCABS / "vocab_30k.txt")
    wordpiece_json = str(VOCABS / "tokenizer_30k_wordpiece.json")
    unigram_json = str(VOCABS / "tokenizer_30k_unigram.json")
    spiece = str(VOCABS / "spiece_30k.model")

    documents = json.loads((VOCABS / "documents.json").read_text(encoding="utf-8"))
    fitted = TfidfVectorizer().fit(documents)
    artifact = pickle.dumps(fitted)

    vectors = build_vectors()
    buffer = io.BytesIO()
    np.save(buffer, vectors)
    npy_bytes = buffer.getvalue()

    # numpy's own compressed container, the comparand for the gzip rows: savez_compressed
    # is a zip of deflate members, which is the same coder GZipStream runs.
    buffer = io.BytesIO()
    np.savez_compressed(buffer, vectors=vectors)
    npz_bytes = buffer.getvalue()

    # No row measured the file path before #336, and it is the one a caller takes.
    # Written once, outside the timed loop.
    npy_file = tempfile.NamedTemporaryFile(suffix=".npy", delete=False)
    npy_file.write(npy_bytes)
    npy_file.close()

    # Its own path, so neither direction measures a file the other just touched. The
    # .npy suffix is given because np.save appends one, and the unlink would then miss it.
    save_file = tempfile.NamedTemporaryFile(suffix=".npy", delete=False)
    save_file.close()

    from tokenizers.models import WordPiece

    print("Python persistence bench")
    results = [
        measure("vocab_txt", lambda: WordPiece.from_file(vocab_txt, unk_token="[UNK]"), os.path.getsize(vocab_txt)),
        measure("tokenizer_json_wordpiece", lambda: Tokenizer.from_file(wordpiece_json), os.path.getsize(wordpiece_json)),
        measure("tokenizer_json_unigram", lambda: Tokenizer.from_file(unigram_json), os.path.getsize(unigram_json)),
        measure("spiece_model", lambda: spm.SentencePieceProcessor(model_file=spiece), os.path.getsize(spiece)),
        measure("tfidf_save", lambda: pickle.dumps(fitted), len(artifact)),
        measure("tfidf_load", lambda: pickle.loads(artifact), len(artifact)),
        measure("embedding_index_save", lambda: np.save(io.BytesIO(), vectors), len(npy_bytes)),
        # The write counterpart of embedding_index_load_file. Neither side flushes to the
        # device and np.save pre-sizes nothing either, so the two are matched. ADR 0052.
        measure("embedding_index_save_file", lambda: np.save(save_file.name, vectors), len(npy_bytes)),
        measure("embedding_index_load", lambda: np.load(io.BytesIO(npy_bytes)), len(npy_bytes)),
        measure("embedding_index_load_file", lambda: np.load(npy_file.name), len(npy_bytes)),
        measure("embedding_index_load_memory", lambda: np.load(io.BytesIO(npy_bytes)), len(npy_bytes)),
        # A floor, not a comparand: frombuffer views the bytes without parsing the
        # header or validating anything, so it does strictly less than the rows above.
        measure("embedding_index_view_floor", lambda: np.frombuffer(npy_bytes, dtype=np.uint8), len(npy_bytes)),
        # What compression costs beside what it saves. np.load on a .npz is lazy, so
        # the member is read to force the decompression the C# row pays for.
        measure("embedding_index_save_gzip", lambda: np.savez_compressed(io.BytesIO(), vectors=vectors), len(npz_bytes)),
        measure("embedding_index_load_gzip", lambda: np.load(io.BytesIO(npz_bytes))["vectors"], len(npz_bytes)),
    ]

    os.unlink(npy_file.name)
    os.unlink(save_file.name)

    payload = {
        "metadata": {
            "side": "python",
            "libraries": {
                "tokenizers": version("tokenizers"),
                "sentencepiece": version("sentencepiece"),
                "scikit-learn": version("scikit-learn"),
                "numpy": version("numpy"),
            },
            "python": platform.python_version(),
            "machine": platform.machine(),
            "min_time_s": MIN_TIME,
            "repeats": REPEATS,
        },
        "results": results,
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    print(f"-> {OUT}")


if __name__ == "__main__":
    main()
