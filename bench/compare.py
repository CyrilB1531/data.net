#!/usr/bin/env python3
"""Merge the Python and C# cross-language results into one comparison table.

Run both harnesses first (see bench/README.md), then:
    python bench/compare.py
    python bench/compare.py --format=gfm   # a real markdown table, for a page that renders one
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


def metadata_block(fmt: str, *lines: str) -> None:
    """The two 'Python:'/'C#:' lines -- plain in text mode, unchanged from before.

    Fenced with a bare, unlabelled ``` in gfm mode only: tools/render_nightly.py's
    included_reports() already knows how to split a file shaped this way --
    preamble fenced, table native -- so --format=gfm's whole file reads through
    that one function too, with nothing gfm-specific for it to know about.
    """
    if fmt == "gfm":
        print("```")
        for line in lines:
            print(line)
        print("```")
        print()
        return
    print()
    for line in lines:
        print(line)
    print()


def main(fmt: str = "text") -> None:
    pairs_report(
        "levenshtein",
        "Note: Python times the realistic per-call loop; rapidfuzz's C core uses "
        "the bit-parallel Myers algorithm, so it scales better on long strings.",
        fmt,
    )


def indel(fmt: str = "text") -> None:
    pairs_report(
        "indel",
        "Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the "
        "subsequence kernels. Lodestar's is a rolling-row dynamic program (#273).",
        fmt,
    )


def pairs_row(length: int, p: float, c: float) -> tuple[int, float, float, float]:
    return length, p, c, p / c


def print_pairs_text(rows: list[tuple[int, float, float, float]]) -> None:
    print(f"{'length':>8} | {'Python ns/pair':>16} | {'C# ns/pair':>14} | {'speedup (py/C#)':>16}")
    print(f"{'-' * 8}-+-{'-' * 16}-+-{'-' * 14}-+-{'-' * 16}")
    for length, p, c, ratio in rows:
        faster = f"{ratio:6.2f}x C# faster" if ratio >= 1 else f"{1 / ratio:6.2f}x Py faster"
        print(f"{length:>8} | {p:>16.1f} | {c:>14.1f} | {faster:>16}")


def print_pairs_gfm(rows: list[tuple[int, float, float, float]]) -> None:
    print("| length | Python ns/pair | C# ns/pair | speedup (py/C#) |")
    print("|---:|---:|---:|:---|")
    for length, p, c, ratio in rows:
        faster = f"{ratio:.2f}x C# faster" if ratio >= 1 else f"{1 / ratio:.2f}x Py faster"
        print(f"| {length} | {p:.1f} | {c:.1f} | {faster} |")


def pairs_report(bench: str, note: str, fmt: str = "text") -> None:
    """One length-bucket table, shared by every benchmark over the pair corpus.

    Levenshtein and Indel differ only in which result files they read and what
    the closing note says. A second copy of this loop would be free to drift from
    the first while still printing a table that looks comparable.
    """
    py = load("python", bench)
    cs = load("csharp", bench)
    cs_by_len = {r["length"]: r["ns_per_pair"] for r in cs["results"]}

    rows = []
    for r in py["results"]:
        c = cs_by_len.get(r["length"])
        if c is not None:
            rows.append(pairs_row(r["length"], r["ns_per_pair"], c))

    metadata_block(
        fmt,
        f"Python: rapidfuzz {py['metadata']['library_version']} (py {py['metadata']['python']})",
        f"C#:     {cs['metadata']['library']} on .NET {cs['metadata']['runtime']} "
        f"(mode {cs['metadata']['mode']})",
    )
    (print_pairs_gfm if fmt == "gfm" else print_pairs_text)(rows)
    print()
    print(note)


def wallcpu_row(op: str, cs_row: dict, py_row: dict) -> tuple:
    """One comparison row's raw values, shared by every printer and the merge gate.

    Returned as data rather than a formatted line: text and gfm want different
    column widths (or none), and the merge gate below reads cpu_ratio directly.
    """
    c_w, p_w = cs_row["ms_per_op"], py_row["ms_per_op"]
    c_c, p_c = cs_row.get("cpu_ms_per_op"), py_row.get("cpu_ms_per_op")
    wall = f"{p_w / c_w:.2f}x" if c_w else "n/a"
    cpu_ratio = (p_c / c_c) if c_c and p_c else None
    cpu = f"{cpu_ratio:.2f}x" if cpu_ratio is not None else "n/a"
    return op, c_w, p_w, wall, (c_c or 0), (p_c or 0), cpu, cpu_ratio


def print_wallcpu_text(rows: list[tuple], op_width: int, num_width: int) -> None:
    print(f"{'operation':<{op_width}} {'C# ms':>{num_width}} {'Py ms':>{num_width}} "
          f"{'wall':>7} | {'C# cpu':>{num_width}} {'Py cpu':>{num_width}} {'cpu':>7}")
    for op, c_w, p_w, wall, c_c, p_c, cpu, _ in rows:
        print(f"{op:<{op_width}} {c_w:>{num_width}.3f} {p_w:>{num_width}.3f} {wall:>7} | "
              f"{c_c:>{num_width}.3f} {p_c:>{num_width}.3f} {cpu:>7}")


def print_wallcpu_gfm(rows: list[tuple]) -> None:
    print("| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |")
    print("|:---|---:|---:|---:|---:|---:|---:|")
    for op, c_w, p_w, wall, c_c, p_c, cpu, _ in rows:
        print(f"| {op} | {c_w:.3f} | {p_w:.3f} | {wall} | {c_c:.3f} | {p_c:.3f} | {cpu} |")


def persistence(fmt: str = "text") -> None:
    py = load("python", "persistence")
    cs = load("csharp", "persistence")
    cs_by_op = {r["operation"]: r for r in cs["results"]}
    rows = [wallcpu_row(row["operation"], cs_by_op[row["operation"]], row)
            for row in py["results"] if row["operation"] in cs_by_op]

    metadata_block(
        fmt,
        f"Python: {py['metadata']['libraries']} (py {py['metadata']['python']})",
        f"C#:     Lodestar on .NET {cs['metadata']['runtime']}",
    )
    if fmt == "gfm":
        print_wallcpu_gfm(rows)
    else:
        print_wallcpu_text(rows, op_width=28, num_width=8)
    print()
    print("ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time")
    print("hides work .NET does on background GC threads; CPython is single-threaded.")


def metrics(fmt: str = "text") -> None:
    py = load("python", "metrics")
    cs = load("csharp", "metrics")

    # A filtered C# run has fewer rows; comparing only what's there would print
    # a partial table as if it were a green, full-matrix gate. Refuse instead.
    filtered = cs["metadata"].get("filtered")
    if filtered:
        raise SystemExit(
            f"the C# results are from a filtered run ({filtered}); the merge gate "
            "needs the whole matrix — rerun `compare-metrics` with no --only/--shapes"
        )

    cs_by_op = {r["operation"]: r for r in cs["results"]}
    rows = [wallcpu_row(row["operation"], cs_by_op[row["operation"]], row)
            for row in py["results"] if row["operation"] in cs_by_op]

    metadata_block(
        fmt,
        f"Python: {py['metadata']['libraries']} (py {py['metadata']['python']})",
        f"C#:     Lodestar on .NET {cs['metadata']['runtime']}",
    )
    if fmt == "gfm":
        print_wallcpu_gfm(rows)
    else:
        print_wallcpu_text(rows, op_width=32, num_width=10)
    print()
    print("ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch")
    print("(docs/guides/performance.md): every operation, every size, must be >= 1x.")

    below_gate = [(op, cpu_ratio) for op, *_, cpu_ratio in rows
                  if cpu_ratio is not None and cpu_ratio < 1.0]
    if below_gate:
        print()
        print("BELOW GATE on processor time:")
        for op, ratio in below_gate:
            print(f"  {op:<32} {ratio:.2f}x")


if __name__ == "__main__":
    import sys

    argv = sys.argv[1:]
    output_format = "gfm" if "--format=gfm" in argv else "text"
    positional = [a for a in argv if a != "--format=gfm"]
    selected = positional[0] if positional else ""

    if selected == "persistence":
        persistence(output_format)
    elif selected == "metrics":
        metrics(output_format)
    elif selected == "indel":
        indel(output_format)
    else:
        main(output_format)
