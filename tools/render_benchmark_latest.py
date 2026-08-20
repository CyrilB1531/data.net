#!/usr/bin/env python3
"""Render the last known result for every benchmark, from the wiki's own history (#11).

nightly_run.md publishes only the classes a night's diff selected -- a class quiet for
weeks does not appear again until something near it moves. This walks the wiki repo's
own git log for that page (already a clone in the nightly job) and keeps, per section,
the newest occurrence: the last time each method was actually measured. Tonight's own
fresh reports, still on disk in this same job, take priority over that history -- run
alongside render_nightly.py rather than after its wiki push, so a class measured tonight
shows up tonight rather than waiting for tomorrow's aggregate to notice.

Numbers from different nights ran on different hosted-runner VMs and are not comparable
to each other any more than nightly_run.md's own numbers are comparable across nights --
this page exists to say "as of when", not to rank one method's history against another's.

Usage:  python tools/render_benchmark_latest.py --wiki <path to a lodestar.wiki.git clone>
            [--commit <sha>] [--max-commits N] [--stdout]
"""

from __future__ import annotations

import argparse
import datetime
import pathlib
import re
import subprocess

import render_nightly as rn

ROOT = pathlib.Path(__file__).resolve().parent.parent

# One home, matching how render_nightly.py fixes nightly_run.md's own path.
PAGE = ROOT / "docs" / "guides" / "benchmark_latest.md"

WIKI_PAGE = "nightly_run.md"

HEADER = "# Latest known benchmark result, per method"

PREAMBLE = """
> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml` from the
> wiki's own history, alongside [nightly_run](nightly_run).

**Not a comparison across methods.** Each section below is the last night that method was
actually re-run -- whichever night touched the source near it, not necessarily last night,
and not the same night as its neighbours here. Every run measures on a GitHub hosted
runner whose hardware differs night to night, so a number here says "this is the last
known reading", never "faster than the section above it".
"""

SECTION = re.compile(r"^### (?P<name>\S.*)$", re.MULTILINE)

# What ends a section, which is not only the next one: a '##' between two methods
# belongs to the page, and swallowing it emitted MD024's duplicate heading (#356).
BOUNDARY = re.compile(r"^#{1,3} ", re.MULTILINE)
COMMIT = re.compile(r"^- Commit: `([0-9a-f]{7,40})`", re.MULTILINE)


def commits(wiki: pathlib.Path, limit: int) -> list[str]:
    """Commits touching the page, newest first, oldest cut off at limit.

    Refuses a non-positive limit: git log reads a leading '-' on this argument
    as an option rather than a count, the same shape select_benchmarks.py guards
    against on its own revision arguments.
    """
    if limit < 1:
        raise ValueError(f"--max-commits must be positive, got {limit}")
    out = subprocess.run(
        ["git", "log", f"-{limit}", "--format=%H", "--", WIKI_PAGE],
        cwd=wiki, capture_output=True, text=True, check=True)
    return [line for line in out.stdout.splitlines() if line]


def show(wiki: pathlib.Path, sha: str) -> str:
    """The page as it stood at one commit, or empty when that revision lacked it."""
    out = subprocess.run(
        ["git", "show", f"{sha}:{WIKI_PAGE}"],
        cwd=wiki, capture_output=True, text=True, check=False)
    return out.stdout if out.returncode == 0 else ""


def commit_date(wiki: pathlib.Path, sha: str) -> str:
    out = subprocess.run(
        ["git", "show", "-s", "--format=%cs", sha],
        cwd=wiki, capture_output=True, text=True, check=True)
    return out.stdout.strip()


def source_commit(body: str) -> str:
    """The main-repo commit this run measured, read from its own '## This run' line."""
    found = COMMIT.search(body)
    return found.group(1) if found else "unknown"


def sections(body: str) -> dict[str, tuple[str, str]]:
    """Every '### name' block: name to (its heading line, the rest verbatim)."""
    matches = list(SECTION.finditer(body))
    found: dict[str, tuple[str, str]] = {}
    for i, match in enumerate(matches):
        following = matches[i + 1].start() if i + 1 < len(matches) else len(body)
        stop = BOUNDARY.search(body, match.end())
        end = min(following, stop.start()) if stop else following
        block = body[match.start():end].strip()
        heading, _, rest = block.partition("\n")
        found[match.group("name")] = (heading, rest.strip())
    return found


def tonight(commit: str) -> dict[str, tuple[str, str, str, str]]:
    """This job's own fresh reports and comparisons, rendered exactly as nightly_run.md's.

    A dict already in this module's (heading, rest, date, source) shape, so render()
    can seed with it before the history loop rather than special-case it.
    """
    reports = sorted(rn.ARTIFACTS.glob("*-report-github.md")) if rn.ARTIFACTS.is_dir() else []
    comparisons = sorted(rn.COMPARISONS.glob("compare-*.md")) if rn.COMPARISONS.is_dir() else []
    body = "\n".join(rn.included_reports(reports + comparisons))
    today = datetime.date.today().isoformat()
    return {name: (heading, rest, today, commit or "unknown")
            for name, (heading, rest) in sections(body).items()}


def render(wiki: pathlib.Path, limit: int, commit: str) -> str:
    # Seeded first: a class measured tonight must win over its own history below.
    latest: dict[str, tuple[str, str, str, str]] = tonight(commit)
    for sha in commits(wiki, limit):
        body = show(wiki, sha)
        if not body:
            continue
        date = commit_date(wiki, sha)
        source = source_commit(body)
        for name, (heading, rest) in sections(body).items():
            if name not in latest:
                latest[name] = (heading, rest, date, source)

    lines = [HEADER, "", PREAMBLE.strip(), "", "## Per method", ""]
    for name in sorted(latest):
        heading, rest, date, source = latest[name]
        lines += [heading, "", f"_As of {date}, measured at commit `{source}`._", "",
                  rest, ""]
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
    parser.add_argument("--wiki", required=True, type=pathlib.Path)
    parser.add_argument("--commit", default="")
    parser.add_argument("--max-commits", type=int, default=200)
    parser.add_argument("--stdout", action="store_true", help="print instead of writing")
    args = parser.parse_args()

    page = render(args.wiki, args.max_commits, args.commit)
    if args.stdout:
        print(page, end="")
        return

    PAGE.parent.mkdir(parents=True, exist_ok=True)
    PAGE.write_text(page, encoding="utf-8")
    print(f"-> {PAGE.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
