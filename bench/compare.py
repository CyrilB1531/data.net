#!/usr/bin/env python3
"""Merge the Python and C# cross-language results into one comparison table.

Run both harnesses first (see bench/README.md), then:
    python bench/compare.py
"""

from __future__ import annotations

import json
from pathlib import Path

RESULTS = Path(__file__).resolve().parent / "results"


def load(side: str, bench: str) -> dict:
    path = RESULTS / f"{side}-{bench}.json"
    if not path.exists():
        raise SystemExit(f"missing {path} — run the {side} harness first")
    return json.loads(path.read_text(encoding="utf-8"))


def main() -> None:
    py = load("python", "levenshtein")
    cs = load("csharp", "levenshtein")

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


def persistence() -> None:
    py = load("python", "persistence")
    cs = load("csharp", "persistence")
    cs_by_op = {r["operation"]: r for r in cs["results"]}

    print()
    print(f"Python: {py['metadata']['libraries']} (py {py['metadata']['python']})")
    print(f"C#:     DataNet on .NET {cs['metadata']['runtime']}")
    print()
    print(f"{'operation':<28} {'C# ms':>8} {'Py ms':>8} {'wall':>7} | {'C# cpu':>8} {'Py cpu':>8} {'cpu':>7}")
    for row in py["results"]:
        op = row["operation"]
        if op not in cs_by_op:
            continue
        c_w, p_w = cs_by_op[op]["ms_per_op"], row["ms_per_op"]
        c_c, p_c = cs_by_op[op].get("cpu_ms_per_op"), row.get("cpu_ms_per_op")
        wall = f"{p_w / c_w:.2f}x" if c_w else "n/a"
        cpu = f"{p_c / c_c:.2f}x" if c_c and p_c else "n/a"
        print(f"{op:<28} {c_w:>8.3f} {p_w:>8.3f} {wall:>7} | "
              f"{(c_c or 0):>8.3f} {(p_c or 0):>8.3f} {cpu:>7}")
    print()
    print("ratio > 1 means DataNet is faster. cpu is the honest one: elapsed time")
    print("hides work .NET does on background GC threads; CPython is single-threaded.")


def metrics() -> None:
    py = load("python", "metrics")
    cs = load("csharp", "metrics")
    cs_by_op = {r["operation"]: r for r in cs["results"]}

    print()
    print(f"Python: {py['metadata']['libraries']} (py {py['metadata']['python']})")
    print(f"C#:     DataNet on .NET {cs['metadata']['runtime']}")
    print()
    print(f"{'operation':<32} {'C# ms':>10} {'Py ms':>10} {'wall':>7} | {'C# cpu':>10} {'Py cpu':>10} {'cpu':>7}")
    below_gate = []
    for row in py["results"]:
        op = row["operation"]
        if op not in cs_by_op:
            continue
        c_w, p_w = cs_by_op[op]["ms_per_op"], row["ms_per_op"]
        c_c, p_c = cs_by_op[op].get("cpu_ms_per_op"), row.get("cpu_ms_per_op")
        wall = f"{p_w / c_w:.2f}x" if c_w else "n/a"
        cpu_ratio = (p_c / c_c) if c_c and p_c else None
        cpu = f"{cpu_ratio:.2f}x" if cpu_ratio is not None else "n/a"
        print(f"{op:<32} {c_w:>10.3f} {p_w:>10.3f} {wall:>7} | "
              f"{(c_c or 0):>10.3f} {(p_c or 0):>10.3f} {cpu:>7}")
        if cpu_ratio is not None and cpu_ratio < 1.0:
            below_gate.append((op, cpu_ratio))
    print()
    print("ratio > 1 means DataNet is faster. cpu is the merge gate for this branch")
    print("(docs/guides/performance.md): every operation, every size, must be >= 1x.")
    if below_gate:
        print()
        print("BELOW GATE on processor time:")
        for op, ratio in below_gate:
            print(f"  {op:<32} {ratio:.2f}x")


if __name__ == "__main__":
    import sys
    mode = sys.argv[1] if len(sys.argv) > 1 else ""
    if mode == "persistence":
        persistence()
    elif mode == "metrics":
        metrics()
    else:
        main()
