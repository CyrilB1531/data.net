#!/usr/bin/env python3
"""Name the benchmark classes a range of commits makes worth re-running (#11).

The nightly run measures only what changed. It reads bench/bench-map.json, asks
git which files moved since the previous run, and prints the classes whose globs
those files match -- one per line, empty when nothing relevant moved.

Two deliberate biases, both toward running too much rather than too little:

  * an entry under "always" (src/Shared, the corpus, the harness entry point)
    selects every class, because a change there can move any measurement;
  * the globs are directory-wide, so a class runs whenever its neighbourhood
    moved. A benchmark run for nothing costs minutes; one not run hides a
    regression, and nothing goes red when it does.

The baseline commit is whatever the caller passes. The nightly reads it from the
page it published last time, so a run that never happened cannot silently narrow
the next one -- an absent baseline selects everything.

Usage:  python tools/select_benchmarks.py --since <commit> [--head <commit>]
        python tools/select_benchmarks.py --all

Exit:   0 always, unless the arguments or the map are unusable (2)
"""

from __future__ import annotations

import argparse
import fnmatch
import json
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
MAP = ROOT / "bench" / "bench-map.json"

# --since comes from a page any pull request may edit, on its way to a subprocess.
# A leading '-' would read to git as an option rather than a revision.
REVISION = re.compile(r"^[0-9A-Za-z][0-9A-Za-z._/~^-]{0,254}$")


def resolves(rev: str) -> bool:
    """Whether git can turn rev into a commit -- tells 'unresolvable' from 'no changes'."""
    return subprocess.run(
        ["git", "rev-parse", "--verify", "--quiet", f"{rev}^{{commit}}"],
        cwd=ROOT, capture_output=True).returncode == 0


def changed_files(since: str, head: str) -> list[str]:
    """The tracked paths that moved, or none when the range is unusable."""
    if not (REVISION.match(since) and REVISION.match(head)):
        print(f"refusing revision range {since!r}..{head!r}", file=sys.stderr)
        return []
    try:
        out = subprocess.run(
            ["git", "diff", "--name-only", f"{since}..{head}"],
            cwd=ROOT, capture_output=True, text=True, check=True)
    except subprocess.CalledProcessError:
        return []
    return [line for line in out.stdout.splitlines() if line]


def print_all(data: dict, kind: str) -> None:
    """Every mapped entry of one kind, one per line -- the safe default in two places."""
    for name in sorted(data.get(kind, {})):
        print(name)


def matches(path: str, glob: str) -> bool:
    """A '**' glob matches the directory's whole subtree, which fnmatch alone does not."""
    if glob.endswith("/**"):
        return path.startswith(glob[:-2])
    return fnmatch.fnmatch(path, glob)


def select(data: dict, files: list[str], kind: str = "benchmarks") -> list[str]:
    """The entries of one kind the changed files reach; every one when 'always' is hit."""
    entries = data.get(kind, {})
    every = sorted(entries)
    if any(matches(path, glob) for glob in data.get("always", []) for path in files):
        return every

    def globs(name: str) -> list[str]:
        entry = entries[name]
        return entry["sources"] if isinstance(entry, dict) else entry

    return [name for name in every
            if any(matches(path, glob) for glob in globs(name) for path in files)]


def main() -> int:
    parser = argparse.ArgumentParser(add_help=True, description=__doc__)
    parser.add_argument("--since", help="baseline commit; omit with --all")
    parser.add_argument("--head", default="HEAD")
    parser.add_argument("--all", action="store_true", help="select every mapped entry")
    parser.add_argument("--harnesses", action="store_true",
                        help="the cross-language pairs instead of the BenchmarkDotNet classes")
    args = parser.parse_args()

    if not MAP.exists():
        print(f"{MAP.relative_to(ROOT)}: missing", file=sys.stderr)
        return 2

    data = json.loads(MAP.read_text(encoding="utf-8"))

    # No baseline is not "nothing changed": it is "we do not know", and the safe
    # reading of not knowing is to measure everything.
    kind = "harnesses" if args.harnesses else "benchmarks"
    if args.all or not args.since:
        print_all(data, kind)
        return 0

    # Same ignorance, wrongly read as the opposite before this: a rebase can
    # orphan the SHA the page recorded, and an empty diff looked like none (#354).
    if REVISION.match(args.since) and not resolves(args.since):
        print(f"baseline {args.since!r} does not resolve; measuring everything", file=sys.stderr)
        print_all(data, kind)
        return 0

    for name in select(data, changed_files(args.since, args.head), kind):
        print(name)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
