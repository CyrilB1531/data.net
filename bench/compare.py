#!/usr/bin/env python3
"""Merge the Python and C# cross-language results into one comparison table.

Run both harnesses first (see bench/README.md), then:
    python bench/compare.py
    python bench/compare.py --format=gfm   # a real markdown table, for a page that renders one
    python bench/compare.py --bands        # the banded buckets instead, which place the gate
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


BANDED_NOTE = (
    "Note: banded buckets, whose pattern after trimming is exactly the band named. "
    "They exist to place the bit-parallel gate, which the scattered buckets cannot "
    "reach below 8 (#409); they are not a claim against rapidfuzz."
)


def main(fmt: str = "text", kind: str = "scattered") -> None:
    pairs_report(
        "levenshtein",
        BANDED_NOTE if kind == "banded" else
        "Note: Python times the realistic per-call loop; rapidfuzz's C core uses "
        "the bit-parallel Myers algorithm, so it scales better on long strings.",
        fmt,
        kind,
    )


def indel(fmt: str = "text", kind: str = "scattered") -> None:
    pairs_report(
        "indel",
        BANDED_NOTE if kind == "banded" else
        "Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the "
        "subsequence kernels. Lodestar's is Hyyro's bit-parallel LLCS above a pattern "
        "of 8 and a rolling-row dynamic program below it (#273).",
        fmt,
        kind,
    )


def pairs_row(key: tuple[str, str, int], band, p: float, c: float) -> tuple:
    alphabet, _kind, length = key
    return alphabet, (band if band is not None else length), p, c, p / c


def faster_than(ratio: float, width: int = 0) -> str:
    side = f"{ratio:{width}.2f}x C# faster" if ratio >= 1 else f"{1 / ratio:{width}.2f}x Py faster"
    return side


def print_pairs_text(rows: list[tuple], unit: str = "length") -> None:
    header = f"{'alphabet':>8} | {unit:>6} | {'Python ns/pair':>16} | {'C# ns/pair':>14}"
    print(f"{header} | {'speedup (py/C#)':>16}")
    print(f"{'-' * 8}-+-{'-' * 6}-+-{'-' * 16}-+-{'-' * 14}-+-{'-' * 16}")
    for alphabet, length, p, c, ratio in rows:
        print(f"{alphabet:>8} | {length:>6} | {p:>16.1f} | {c:>14.1f} | {faster_than(ratio, 6):>16}")


def print_pairs_gfm(rows: list[tuple], unit: str = "length") -> None:
    print(f"| alphabet | {unit} | Python ns/pair | C# ns/pair | speedup (py/C#) |")
    print("|---|---:|---:|---:|:---|")
    for alphabet, length, p, c, ratio in rows:
        print(f"| {alphabet} | {length} | {p:.1f} | {c:.1f} | {faster_than(ratio)} |")


def pairs_report(bench: str, note: str, fmt: str = "text", kind: str = "scattered") -> None:
    """One length-bucket table, shared by every benchmark over the pair corpus.

    Levenshtein and Indel differ only in which result files they read and what
    the closing note says. A second copy of this loop would be free to drift from
    the first while still printing a table that looks comparable.
    """
    py = load("python", bench)
    cs = load("csharp", bench)
    # Keyed on the alphabet and the kind too: two buckets answer to every length since
    # #406, and a dict on the length alone loses one of each group, silently.
    def key_of(r):
        return (r.get("alphabet", ""), r.get("kind", "scattered"), r["length"])

    cs_by_bucket = {key_of(r): r["ns_per_pair"] for r in cs["results"]}

    rows = []
    for r in py["results"]:
        if r.get("kind", "scattered") != kind:
            continue
        c = cs_by_bucket.get(key_of(r))
        if c is not None:
            rows.append(pairs_row(key_of(r), r.get("band"), r["ns_per_pair"], c))

    metadata_block(
        fmt,
        f"Python: rapidfuzz {py['metadata']['library_version']} (py {py['metadata']['python']})",
        f"C#:     {cs['metadata']['library']} on .NET {cs['metadata']['runtime']} "
        f"(mode {cs['metadata']['mode']})",
    )
    unit = "band" if kind == "banded" else "length"
    (print_pairs_gfm if fmt == "gfm" else print_pairs_text)(rows, unit)
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


def size_columns(sizes: dict | None, op: str) -> tuple[str, str]:
    """The two byte counts for one row, blank when the operation moves no artifact.

    #378 needs the size and the time on the same line -- a compressed row is only
    worth reading next to what the compression cost. Rows without a size print
    nothing extra, so a table whose rows all lack one is unchanged.
    """
    if not sizes or op not in sizes:
        return "", ""
    c_b, p_b = sizes[op]
    return (f"{c_b:,}" if c_b else "-"), (f"{p_b:,}" if p_b else "-")


def print_wallcpu_text(rows: list[tuple], op_width: int, num_width: int,
                       sizes: dict | None = None) -> None:
    head = f"{'operation':<{op_width}} {'C# ms':>{num_width}} {'Py ms':>{num_width}} " \
           f"{'wall':>7} | {'C# cpu':>{num_width}} {'Py cpu':>{num_width}} {'cpu':>7}"
    print(head + (f" | {'C# bytes':>13} {'Py bytes':>13}" if sizes else ""))
    for op, c_w, p_w, wall, c_c, p_c, cpu, _ in rows:
        line = (f"{op:<{op_width}} {c_w:>{num_width}.3f} {p_w:>{num_width}.3f} {wall:>7} | "
                f"{c_c:>{num_width}.3f} {p_c:>{num_width}.3f} {cpu:>7}")
        c_b, p_b = size_columns(sizes, op)
        print(line + (f" | {c_b:>13} {p_b:>13}" if sizes else ""))


def print_wallcpu_gfm(rows: list[tuple], sizes: dict | None = None) -> None:
    print("| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |"
          + (" C# bytes | Py bytes |" if sizes else ""))
    print("|:---|---:|---:|---:|---:|---:|---:|" + ("---:|---:|" if sizes else ""))
    for op, c_w, p_w, wall, c_c, p_c, cpu, _ in rows:
        c_b, p_b = size_columns(sizes, op)
        print(f"| {op} | {c_w:.3f} | {p_w:.3f} | {wall} | {c_c:.3f} | {p_c:.3f} | {cpu} |"
              + (f" {c_b} | {p_b} |" if sizes else ""))


def wallcpu_rows(bench: str) -> tuple[dict, dict, list[tuple]]:
    """Both sides loaded, and the rows their shared operations produce.

    persistence() and metrics() differ only in column widths and which lines
    follow the table, so this is the whole "load, match, compute" step both need.
    """
    py = load("python", bench)
    cs = load("csharp", bench)
    cs_by_op = {r["operation"]: r for r in cs["results"]}
    rows = [wallcpu_row(row["operation"], cs_by_op[row["operation"]], row)
            for row in py["results"] if row["operation"] in cs_by_op]
    return py, cs, rows


def persistence(fmt: str = "text") -> None:
    py, cs, rows = wallcpu_rows("persistence")

    cs_bytes = {r["operation"]: r.get("artifact_bytes", 0) for r in cs["results"]}
    py_bytes = {r["operation"]: r.get("artifact_bytes", 0) for r in py["results"]}
    sizes = {op: (cs_bytes.get(op, 0), py_bytes.get(op, 0)) for op, *_ in rows}
    sizes = sizes if any(c or p for c, p in sizes.values()) else None

    metadata_block(
        fmt,
        f"Python: {py['metadata']['libraries']} (py {py['metadata']['python']})",
        f"C#:     Lodestar on .NET {cs['metadata']['runtime']}",
    )
    if fmt == "gfm":
        print_wallcpu_gfm(rows, sizes)
    else:
        print_wallcpu_text(rows, op_width=28, num_width=8, sizes=sizes)
    print()
    print("ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time")
    print("hides work .NET does on background GC threads; CPython is single-threaded.")
    if sizes:
        print("bytes is what the row wrote or read; a results file from before #378")
        print("carries none, and the two columns then disappear rather than read zero.")


def metrics(fmt: str = "text") -> None:
    py, cs, rows = wallcpu_rows("metrics")

    # A filtered C# run has fewer rows; comparing only what's there would print
    # a partial table as if it were a green, full-matrix gate. Refuse instead.
    filtered = cs["metadata"].get("filtered")
    if filtered:
        raise SystemExit(
            f"the C# results are from a filtered run ({filtered}); the merge gate "
            "needs the whole matrix — rerun `compare-metrics` with no --only/--shapes"
        )

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
    # The scattered buckets are the claim against rapidfuzz; the banded ones exist to
    # place the gate. Twenty band rows in the comparison table would bury it (#409).
    bucket_kind = "banded" if "--bands" in argv else "scattered"
    positional = [a for a in argv if a not in ("--format=gfm", "--bands")]
    selected = positional[0] if positional else ""

    if selected == "persistence":
        persistence(output_format)
    elif selected == "metrics":
        metrics(output_format)
    elif selected == "indel":
        indel(output_format, bucket_kind)
    else:
        main(output_format, bucket_kind)
