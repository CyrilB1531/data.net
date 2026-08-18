#!/usr/bin/env python3
"""Refuse a benchmark class that bench/bench-map.json does not know about.

The nightly run (#11) executes only the benchmark classes whose sources changed
since the previous run, and it reads bench/bench-map.json to decide which those
are. A class missing from that map is never selected, so it would stop being
measured without anything going red -- the same silence as a benchmark that
compiles and is never run, which is the gap #11 exists to close.

The map cannot be derived. FuzzBenchmarks names only Fuzz, which reaches Indel,
then Lcs, then Affixes; LevenshteinCodePointBenchmarks depends on the decoder in
Text/. Naming conventions do not carry that, so the map is hand-written and this
guard keeps it honest about the one thing it can check: completeness.

What it does NOT check is whether a class's globs are *right*. Being too narrow
is invisible here and is why the map is written at directory granularity: a
benchmark run for nothing costs minutes, one not run hides a regression.

Usage:  python tools/check_bench_map.py

Exit:   0 clean, 1 findings printed, 2 bad usage
"""

from __future__ import annotations

import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
MAP = ROOT / "bench" / "bench-map.json"
BENCH_DIR = ROOT / "bench" / "Lodestar.Text.Benchmarks"

CLASS = re.compile(r"^\s*public\s+class\s+(\w+)", re.MULTILINE)


def declared_classes() -> dict[str, pathlib.Path]:
    """Every class in the benchmark project carrying at least one [Benchmark]."""
    found: dict[str, pathlib.Path] = {}
    for path in sorted(BENCH_DIR.glob("*.cs")):
        text = path.read_text(encoding="utf-8")
        if "[Benchmark" not in text:
            continue
        match = CLASS.search(text)
        if match:
            found[match.group(1)] = path
    return found


def coverage_findings(mapped: dict, declared: dict) -> list[str]:
    """A [Benchmark] class the map forgot, and a mapped name that no longer is one."""
    findings = [
        f"{path.relative_to(ROOT)}: {name} carries [Benchmark] and is not in "
        f"bench/bench-map.json, so the nightly run would never select it"
        for name, path in declared.items() if name not in mapped
    ]
    findings += [
        f"bench/bench-map.json: {name} is mapped and no longer declares a [Benchmark]"
        for name in sorted(set(mapped) - set(declared))
    ]
    return findings


def glob_findings(data: dict) -> list[str]:
    """A glob matching nothing is a rename nobody carried through."""
    findings = [
        f"bench/bench-map.json: {name}'s '{glob}' matches nothing"
        for name, globs in sorted(data.get("benchmarks", {}).items())
        for glob in globs if not any(ROOT.glob(glob))
    ]
    findings += [
        f"bench/bench-map.json: 'always' entry '{glob}' matches nothing"
        for glob in data.get("always", []) if not any(ROOT.glob(glob))
    ]
    return findings


def main() -> int:
    if len(sys.argv) > 1:
        print(__doc__)
        return 0 if sys.argv[1] in ("--help", "-h") else 2

    if not MAP.exists():
        print(f"{MAP.relative_to(ROOT)}: missing")
        return 1

    data = json.loads(MAP.read_text(encoding="utf-8"))
    findings = coverage_findings(data.get("benchmarks", {}), declared_classes())
    findings += glob_findings(data)

    for finding in findings:
        print(finding)
    return 1 if findings else 0


if __name__ == "__main__":
    raise SystemExit(main())
