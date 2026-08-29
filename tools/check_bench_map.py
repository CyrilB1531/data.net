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
PROGRAM = BENCH_DIR / "Program.cs"
PYTHON_DIR = ROOT / "bench" / "python"

CLASS = re.compile(r"^\s*public\s+class\s+(\w+)", re.MULTILINE)
SUBCOMMAND = re.compile(r'args\[0\] == "(compare[a-z-]*)"')
ANY_SUBCOMMAND = re.compile(r'args\[0\] == "([a-z][a-z-]*)"')


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


def declared_harnesses() -> set[str]:
    """Every cross-language subcommand Program.cs dispatches, and every bench_*.py."""
    program = PROGRAM.read_text(encoding="utf-8") if PROGRAM.exists() else ""
    found = set(SUBCOMMAND.findall(program))
    found |= {path.name for path in PYTHON_DIR.glob("bench_*.py")}
    return found


def diagnostic_findings(diagnostics: list) -> list[str]:
    """A diagnostic is exempt from the nightly, not from existing.

    roc-parallel, save-phases and heap-warmth answer a question a lot asks once, so no
    harness names them and the nightly never runs one. That is deliberate. What is not
    deliberate is a renamed subcommand or a deleted script leaving an entry pointing at
    nothing, which is the same rot the harness rules catch.
    """
    program = PROGRAM.read_text(encoding="utf-8") if PROGRAM.exists() else ""
    dispatched = set(ANY_SUBCOMMAND.findall(program))

    findings = [
        f"bench/bench-map.json: diagnostic '{entry['subcommand']}' is not dispatched by "
        f"bench/Lodestar.Text.Benchmarks/Program.cs"
        for entry in diagnostics if entry.get("subcommand") not in dispatched
    ]
    findings += [
        f"bench/bench-map.json: diagnostic '{entry['subcommand']}' names {entry['python']}, "
        f"which does not exist"
        for entry in diagnostics
        if "python" in entry and not (ROOT / entry["python"]).exists()
    ]
    return findings


def harness_findings(harnesses: dict, diagnostics: list) -> list[str]:
    """A comparison the map does not carry is one the nightly never runs."""
    mapped_subcommands = {entry.get("subcommand") for entry in harnesses.values()}
    mapped_python = {pathlib.Path(entry.get("python", "")).name for entry in harnesses.values()}
    # A diagnostic's Python half is named here rather than by a harness, on purpose.
    mapped_python |= {
        pathlib.Path(entry["python"]).name for entry in diagnostics if "python" in entry
    }
    declared = declared_harnesses()

    findings = [
        f"bench/Lodestar.Text.Benchmarks/Program.cs: '{name}' is dispatched and no harness in "
        f"bench/bench-map.json names it, so the nightly would never run that comparison"
        for name in sorted(declared & {n for n in declared if n.startswith("compare")})
        if name not in mapped_subcommands
    ]
    findings += [
        f"bench/python/{name}: no harness in bench/bench-map.json names it"
        for name in sorted(n for n in declared if n.endswith(".py")) if name not in mapped_python
    ]
    findings += [
        f"bench/bench-map.json: harness '{key}' names {entry['python']}, which does not exist"
        for key, entry in sorted(harnesses.items())
        if not (ROOT / entry.get("python", "")).exists()
    ]
    findings += [
        f"bench/bench-map.json: harness '{key}' generates with {script}, which does not exist"
        for key, entry in sorted(harnesses.items())
        for script in entry.get("generate", []) if not (ROOT / script).exists()
    ]
    return findings


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
        f"bench/bench-map.json: harness {key}'s '{glob}' matches nothing"
        for key, entry in sorted(data.get("harnesses", {}).items())
        for glob in entry.get("sources", []) if not any(ROOT.glob(glob))
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
    diagnostics = data.get("diagnostics", [])
    findings = coverage_findings(data.get("benchmarks", {}), declared_classes())
    findings += harness_findings(data.get("harnesses", {}), diagnostics)
    findings += diagnostic_findings(diagnostics)
    findings += glob_findings(data)

    for finding in findings:
        print(finding)
    return 1 if findings else 0


if __name__ == "__main__":
    raise SystemExit(main())
