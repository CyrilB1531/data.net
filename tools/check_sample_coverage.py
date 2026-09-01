#!/usr/bin/env python3
"""Refuse a public class with no sample of its own, package by package (#280).

Decision 0041 names a sample after the class it demonstrates, so a type without
one is a type nobody can find an example of. The packaging gate already refuses a
type the sample never *references*; it says nothing about which file that
reference lives in, which is what this adds.

Only the packages listed in CONVERTED are enforced. The rest still carry their
Lot* files and would fail every run until their own lot lands -- the same shape
docs/wiki-map.json's covered table uses, and the same reason: a gate that fails
on work nobody has started yet is noise a contributor learns to skip.

An enum is not a class and is excluded by decision 0041: it is demonstrated
through the class whose parameter it is, and a file exercising one alone would
have to invent a use for it.

Usage:  python tools/check_sample_coverage.py

Exit:   0 clean, 1 findings printed, 2 bad usage
"""

from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
SAMPLES = ROOT / "samples" / "Lodestar.Sample"

# Packages whose samples have been split per class. Each new lot adds its own
# name here, and this list emptying of its "waiting" half is what closes #280.
CONVERTED = ["Lodestar.Text", "Lodestar.Conformal"]
WAITING = ["Lodestar.Fuzzy", "Lodestar.Embeddings", "Lodestar.Metrics"]

DECLARATION = re.compile(
    r"^public\s+(?:static\s+|sealed\s+|abstract\s+|partial\s+|readonly\s+)*"
    r"(class|record|struct|enum|interface)\s+(\w+)", re.MULTILINE)


def public_classes(package: str) -> dict[str, pathlib.Path]:
    """Every public non-enum type of one package, outside its Internal folder."""
    found: dict[str, pathlib.Path] = {}
    for path in sorted((ROOT / "src" / package).rglob("*.cs")):
        if "Internal" in path.parts:
            continue
        for kind, name in DECLARATION.findall(path.read_text(encoding="utf-8")):
            if kind != "enum":
                found[name] = path
    return found


def main() -> int:
    if len(sys.argv) > 1:
        print(__doc__)
        return 0 if sys.argv[1] in ("--help", "-h") else 2

    samples = {path.stem[: -len("Sample")] for path in SAMPLES.glob("*Sample.cs")}
    findings: list[str] = []

    for package in CONVERTED:
        for name, path in sorted(public_classes(package).items()):
            if name not in samples:
                findings.append(
                    f"{path.relative_to(ROOT)}: {name} is public and has no "
                    f"samples/Lodestar.Sample/{name}Sample.cs (decision 0041)")

    for finding in findings:
        print(finding)

    if not findings:
        converted = sum(len(public_classes(p)) for p in CONVERTED)
        print(f"ok  {converted} public classes across {', '.join(CONVERTED)} each have a sample; "
              f"{', '.join(WAITING)} still on Lot*")
    return 1 if findings else 0


if __name__ == "__main__":
    raise SystemExit(main())
