#!/usr/bin/env python3
"""Merge the Python and C# cross-language results into one comparison table.

Run both harnesses first (see bench/README.md), then:
    python bench/compare.py
"""

from __future__ import annotations

import json
from pathlib import Path

RESULTS = Path(__file__).resolve().parent / "results"


def load(name: str) -> dict:
    path = RESULTS / name
    if not path.exists():
        raise SystemExit(f"missing {path} — run the {name.split('-')[0]} harness first")
    return json.loads(path.read_text(encoding="utf-8"))


def main() -> None:
    py = load("python-levenshtein.json")
    cs = load("csharp-levenshtein.json")

    cs_by_len = {r["length"]: r["ns_per_pair"] for r in cs["results"]}

    print()
    print(f"Python: rapidfuzz {py['metadata']['library_version']} (py {py['metadata']['python']})")
    print(f"C#:     {cs['metadata']['library']} on .NET {cs['metadata']['runtime']} "
          f"(mode {cs['metadata']['mode']})")
    print()
    print(f"{'length':>8} | {'Python ns/pair':>16} | {'C# ns/pair':>14} | {'speedup (py/C#)':>16}")
    print(f"{'-'*8}-+-{'-'*16}-+-{'-'*14}-+-{'-'*16}")
    for r in py["results"]:
        length = r["length"]
        p = r["ns_per_pair"]
        c = cs_by_len.get(length)
        if c is None:
            continue
        ratio = p / c
        faster = f"{ratio:6.2f}x C# faster" if ratio >= 1 else f"{1/ratio:6.2f}x Py faster"
        print(f"{length:>8} | {p:>16.1f} | {c:>14.1f} | {faster:>16}")
    print()
    print("Note: Python times the realistic per-call loop; rapidfuzz's C core uses "
          "the bit-parallel Myers algorithm, so it scales better on long strings.")


if __name__ == "__main__":
    main()
