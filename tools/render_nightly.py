#!/usr/bin/env python3
"""Render the nightly benchmark page from what the run produced (#11).

The page is generated, published to the wiki, and never hand-edited. It carries
the baseline commit the next run reads, which is why it is a file in docs/ rather
than an artifact: the series has to remember where it stopped.

What it deliberately does NOT do is pretend to be docs/guides/performance.md.
That page's subject is what was measured on a named machine, and its numbers are
comparable to each other because the machine and its load are stated. A GitHub
hosted runner is a shared VM whose hardware differs between runs, so its absolute
figures are not comparable to that table nor, strictly, to last night's. What
survives is the ratio inside one run -- a baseline and its comparands measured in
the same minute on the same VM -- which is what this page is for.

Usage:  python tools/render_nightly.py --commit <sha> --baseline <sha> \\
            --runner <label> [--selected Class ...] [--reason text] [--stdout]
"""

from __future__ import annotations

import argparse
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent

# One home, the one docs/wiki-map.json declares: a constant rather than an argument, so
# no workflow input can aim a write elsewhere. --stdout is what a local check uses.
PAGE = ROOT / "docs" / "guides" / "nightly_run.md"

# BenchmarkDotNet writes here by its own convention, so the reports are found rather
# than passed in: no path this script touches comes from an argument.
ARTIFACTS = ROOT / "BenchmarkDotNet.Artifacts" / "results"

HEADER = "# Nightly benchmark run"

MARKER = "<!-- nightly-baseline: {sha} -->"

PREAMBLE = """
> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance]({performance}).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.
"""


def render(args: argparse.Namespace, reports: list[pathlib.Path]) -> str:
    lines = [HEADER, "", MARKER.format(sha=args.baseline or "none"), ""]
    lines.append(PREAMBLE.format(performance="performance").strip())
    lines += ["", "## This run", "", f"- Commit: `{args.commit}`",
              f"- Previous run: `{args.baseline or 'none — every class was selected'}`",
              f"- Runner: {args.runner}", ""]

    if not args.selected:
        lines += ["## Nothing was re-run", "",
                  args.reason or "No source a benchmark measures changed since the previous run, "
                  "so nothing needed measuring. `bench/bench-map.json` decides that, and "
                  "`tools/check_bench_map.py` refuses a benchmark class it does not name.", ""]
        return collapse(lines)

    lines += ["## Classes re-run", "",
              "Selected by `tools/select_benchmarks.py` from the sources that changed since the "
              "previous run:", ""]
    lines += [f"- `{name}`" for name in args.selected]
    lines.append("")

    for report in reports:
        if not report.exists():
            continue
        body = report.read_text(encoding="utf-8").strip()
        if body:
            lines += [f"### {report.stem}", "", body, ""]

    return collapse(lines)


def collapse(lines: list[str]) -> str:
    """One blank line where the assembly left several: the page is linted like any other."""
    out: list[str] = []
    for line in lines:
        if line == "" and out and out[-1] == "":
            continue
        out.append(line)
    return "\n".join(out).strip() + "\n"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--commit", required=True)
    parser.add_argument("--baseline", default="")
    parser.add_argument("--runner", default="ubuntu-latest")
    parser.add_argument("--selected", nargs="*", default=[])
    parser.add_argument("--reason", default="")
    parser.add_argument("--stdout", action="store_true", help="print instead of writing")
    args = parser.parse_args()

    reports = sorted(ARTIFACTS.glob("*-report-github.md")) if ARTIFACTS.is_dir() else []
    page = render(args, reports)
    if args.stdout:
        print(page, end="")
        return

    PAGE.parent.mkdir(parents=True, exist_ok=True)
    # S2083 and S8707 read a tainted path here. PAGE is ROOT and three literals, ROOT is
    # __file__: no argument reaches it, and dropping --out then --report did not convince.
    PAGE.write_text(page, encoding="utf-8")  # NOSONAR
    print(f"-> {PAGE.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
