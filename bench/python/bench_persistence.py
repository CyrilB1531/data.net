#!/usr/bin/env python3
"""Time the Python counterparts of the DataNet persistence work.

Methodology is mirrored by the C# harness (bench/DataNet.Text.Benchmarks,
`compare-persistence` mode) so the two are comparable:

  * same corpus files (bench/corpus/vocabs/),
  * metric: milliseconds per operation,
  * auto-scaling: repeat until a measurement lasts >= MIN_TIME,
  * report the best (minimum) of REPEATS measurements.

Note what the two sides each build. tokenizers and sentencepiece construct a
whole tokenizer -- the normalizer and pre-tokenizer graph, the Rust or C++
matcher it will encode with. DataNet's loaders build a validated dictionary and
stop; the guides tell readers to construct a tokenizer from it as a second step.
A margin in DataNet's favour therefore reflects, in part, work it does not do.
"""

from __future__ import annotations

import json
import pickle
import platform
from importlib.metadata import version
from pathlib import Path
from time import perf_counter, process_time

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


def measure(operation: str, action) -> dict:
    """Time one operation, recording both elapsed time and processor time.

    The C# harness records the same pair for the same reason: .NET's background
    collector does its work on other threads, so elapsed time understates what an
    allocation-heavy operation actually costs. CPython's unpickler is strictly
    single-threaded, so cpu/wall lands at 1.00 here -- which is exactly the point
    of reporting both.
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
    print(f"  {operation:<28} {best_wall:10.3f} ms/op  cpu {cpu_of_best:8.3f} ms/op  ({cpu_of_best / best_wall:.2f}x cores)")
    return {"operation": operation, "ms_per_op": best_wall, "cpu_ms_per_op": cpu_of_best}


def main() -> None:
    check_corpus()

    vocab_txt = str(VOCABS / "vocab_30k.txt")
    wordpiece_json = str(VOCABS / "tokenizer_30k_wordpiece.json")
    unigram_json = str(VOCABS / "tokenizer_30k_unigram.json")
    spiece = str(VOCABS / "spiece_30k.model")

    documents = json.loads((VOCABS / "documents.json").read_text(encoding="utf-8"))
    fitted = TfidfVectorizer().fit(documents)
    artifact = pickle.dumps(fitted)

    from tokenizers.models import WordPiece

    print("Python persistence bench")
    results = [
        measure("vocab_txt", lambda: WordPiece.from_file(vocab_txt, unk_token="[UNK]")),
        measure("tokenizer_json_wordpiece", lambda: Tokenizer.from_file(wordpiece_json)),
        measure("tokenizer_json_unigram", lambda: Tokenizer.from_file(unigram_json)),
        measure("spiece_model", lambda: spm.SentencePieceProcessor(model_file=spiece)),
        measure("tfidf_save", lambda: pickle.dumps(fitted)),
        measure("tfidf_load", lambda: pickle.loads(artifact)),
    ]

    payload = {
        "metadata": {
            "side": "python",
            "libraries": {
                "tokenizers": version("tokenizers"),
                "sentencepiece": version("sentencepiece"),
                "scikit-learn": version("scikit-learn"),
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
