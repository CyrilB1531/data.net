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
            --runner <label> [--selected Class ...] [--reason text] [--branch] [--stdout]
"""

from __future__ import annotations

import argparse
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent

# long-comment: two homes, not an argument -- a workflow input still cannot aim a
# write anywhere but one of these two constants. PAGE is the one docs/wiki-map.json
# declares and only a main run may write; BRANCH_PAGE is where a workflow_dispatch on
# another ref writes instead, so that branch keeps its own run's numbers without ever
# touching the page a merge into main could carry along (#379). BRANCH_PAGE is not in
# wiki-map.json's page lists, unlike PAGE, so it never reaches the wiki even if it
# somehow reached main. --stdout is what a local check uses.
PAGE = ROOT / "docs" / "guides" / "nightly_run.md"
BRANCH_PAGE = ROOT / "docs" / "guides" / "branch" / "nightly_run.md"

# BenchmarkDotNet writes here by its own convention, so the reports are found rather
# than passed in: no path this script touches comes from an argument.
ARTIFACTS = ROOT / "BenchmarkDotNet.Artifacts" / "results"

# What bench/compare.py prints, captured per comparison. This is where the claim against
# rapidfuzz is measured; a BenchmarkDotNet class only measures us against ourselves.
COMPARISONS = ROOT / "bench" / "results"

HEADER = "# Nightly benchmark run"

MARKER = "<!-- nightly-baseline: {sha} -->"

PREAMBLE = """
> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance]({performance}). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.
"""


def split_report(body: str) -> tuple[str, str] | None:
    """BenchmarkDotNet's own preamble fence, and the table that follows it.

    A *-report-github.md opens a bare, unlabelled ``` around its host/job
    summary, closes it, then prints the results as a plain GFM table -- one
    following the other, never nested. None when a report does not have that
    shape, so the caller can fall back rather than guess where a table starts.
    """
    lines = body.splitlines()
    fences = [i for i, line in enumerate(lines) if line.strip() == "```"]
    if len(fences) < 2:
        return None
    preamble = "\n".join(lines[fences[0] + 1:fences[1]]).strip()
    table = "\n".join(lines[fences[1] + 1:]).strip()
    return preamble, table


def heading_for(stem: str, occurrence: int) -> str:
    """The section heading for one report, unique when a stem repeats within a run."""
    return stem if occurrence == 1 else f"{stem} (run {occurrence})"


def included_reports(paths: list[pathlib.Path]) -> list[str]:
    """Each report's own section: its preamble fenced, its results table native.

    The preamble is prose, always safe to fence. The table is real GFM (a
    `---:` header row, `**bold**` baseline cells) and renders as a table only
    when nothing wraps it -- but BenchmarkDotNet pads its own pipes to content
    width, which this repo's MD060 alignment style does not match, so that one
    rule is switched off around it: generated content, never hand-edited, has
    no convention of its own to keep.

    A stem can arrive twice -- the same class measured more than once in one run --
    and two identical `###` lines fail markdownlint's MD024 and sink the whole
    lint job. Later occurrences are numbered rather than dropped: the second
    measurement is real, it is usually not wanted, and a reader who can see it is
    the one who can go and remove its cause.
    """
    lines: list[str] = []
    seen: dict[str, int] = {}
    for path in paths:
        body = path.read_text(encoding="utf-8").strip() if path.exists() else ""
        if not body:
            continue
        seen[path.stem] = seen.get(path.stem, 0) + 1
        occurrence = seen[path.stem]
        lines += [f"### {heading_for(path.stem, occurrence)}", ""]
        split = split_report(body)
        if split is None:
            lines += ["````text", body, "````", ""]
            continue
        preamble, table = split
        if preamble:
            lines += ["```text", preamble, "```", ""]
        if table:
            lines += ["<!-- markdownlint-disable MD060 -->", "", table, "",
                       "<!-- markdownlint-enable MD060 -->", ""]
    return lines


def has_content(paths: list[pathlib.Path]) -> bool:
    """Whether at least one path exists and holds more than whitespace."""
    return any(path.exists() and path.read_text(encoding="utf-8").strip() for path in paths)


def listing(title: str, note: str, names: list[str]) -> list[str]:
    """One section headed by its own reason, then the names it covers."""
    return ["", f"## {title}", "", note, ""] + [f"- `{name}`" for name in names] + [""]


def nothing_ran(reason: str) -> list[str]:
    return ["## Nothing was re-run", "",
            reason or "No source a benchmark measures changed since the previous run, so "
            "nothing needed measuring. `bench/bench-map.json` decides that, and "
            "`tools/check_bench_map.py` refuses an entry it does not name.", ""]


def render(args: argparse.Namespace,
           reports: list[pathlib.Path],
           comparisons: list[pathlib.Path] | None = None) -> str:
    lines = [HEADER, "", MARKER.format(sha=args.baseline or "none"), ""]
    lines.append(PREAMBLE.format(performance="performance").strip())
    lines += ["", "## This run", "", f"- Commit: `{args.commit}`",
              f"- Previous run: `{args.baseline or 'none — every entry was selected'}`",
              f"- Runner: {args.runner}", ""]

    if not args.selected and not args.harnesses:
        return collapse(lines + nothing_ran(args.reason))

    if args.selected:
        lines += listing(
            "Classes re-run",
            "Selected by `tools/select_benchmarks.py` from the sources that changed since "
            "the previous run:", args.selected)
        lines += included_reports(reports)

    if args.harnesses:
        lines += listing(
            "Against rapidfuzz, in this same run",
            "Both sides on this VM in these minutes, which is what makes the ratio readable "
            "where the absolutes are not.", args.harnesses)
        lines += included_reports(comparisons or [])

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
    parser.add_argument("--harnesses", nargs="*", default=[])
    parser.add_argument("--reason", default="")
    parser.add_argument("--branch", action="store_true",
                        help="write BRANCH_PAGE instead of PAGE (a workflow_dispatch off main)")
    parser.add_argument("--stdout", action="store_true", help="print instead of writing")
    args = parser.parse_args()

    reports = sorted(ARTIFACTS.glob("*-report-github.md")) if ARTIFACTS.is_dir() else []
    comparisons = sorted(COMPARISONS.glob("compare-*.md")) if COMPARISONS.is_dir() else []
    page = render(args, reports, comparisons)
    if args.stdout:
        print(page, end="")
        return

    target = BRANCH_PAGE if args.branch else PAGE

    # Nothing selected, or everything selected produced no result: leaving the file
    # untouched is what "Open the pull request" and the wiki's own push already skip.
    if not has_content(reports) and not has_content(comparisons):
        print(f"nothing worth publishing; {target.relative_to(ROOT)} left unchanged")
        return

    target.parent.mkdir(parents=True, exist_ok=True)
    # S2083 and S8707 read a tainted path here. target is one of two fixed constants
    # chosen by a boolean flag, not an argument: dropping --branch did not convince.
    target.write_text(page, encoding="utf-8")  # NOSONAR
    print(f"-> {target.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
