#!/usr/bin/env python3
"""Assert every netstandard2.0 mirror really mirrors, and says so.

Each ``tests/<Package>.NetStandard.Tests`` project links its sibling suite's sources
and pins the library under test to the netstandard2.0 build with
``SetTargetFramework``. That pin is the whole point of the project: without it the
assemblies shipped to .NET Framework, Mono and Unity are compile-verified but never
executed.

Two things make the pin fail silently, and this checks both.

1. **``SetTargetFramework`` does not travel across a ``PackageReference``.** ``src/``
   projects reach each other through published packages, and NuGet resolves package
   assets against the *consuming* project's framework — net10.0 for a mirror. So a
   mirror that pins only its own library still loads its dependencies' net10.0 build.
   Every ``Lodestar.*`` package a library depends on therefore needs its own pinned
   ``ProjectReference`` in the mirror. Measured on 2026-09-02: ``Lodestar.Text`` and
   ``Lodestar.Decomposition`` were running against the net10.0 ``Lodestar.Abstractions``,
   832 tests green and half of each one proving nothing (#529).

2. **Nothing asserts the pin at run time** unless the mirror carries
   ``NetStandardAssemblyGuardTests.cs``, which reads the loaded assembly's
   ``TargetFrameworkAttribute``. Three of seven mirrors had no such file, which is how
   rule 1's breakage survived.

Usage:  python tools/check_netstandard_guards.py
"""

from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent

GUARD = "NetStandardAssemblyGuardTests.cs"

# A library's dependencies, as the src project declares them.
SRC_PACKAGE_REFERENCE = re.compile(r'<PackageReference\s+Include="(Lodestar\.[A-Za-z.]+)"')

# A mirror's pins, as the test project declares them.
MIRROR_PROJECT_REFERENCE = re.compile(
    r'<ProjectReference\s+Include="[^"]*/(Lodestar\.[A-Za-z.]+)\.csproj"'
    r'\s+SetTargetFramework="TargetFramework=netstandard2\.0"')


def mirrors() -> list[pathlib.Path]:
    return sorted((ROOT / "tests").glob("*.NetStandard.Tests"))


def failures_in(mirror: pathlib.Path) -> list[str]:
    package = mirror.name[: -len(".NetStandard.Tests")]
    found = []

    if not (mirror / GUARD).is_file():
        found.append(
            f"{mirror.relative_to(ROOT)}: no {GUARD}. Nothing asserts this suite loads the "
            f"netstandard2.0 assembly, so a reference that resolved back to net10.0 would "
            f"leave every test passing while proving nothing.")

    project = next(mirror.glob("*.csproj"), None)
    if project is None:
        found.append(f"{mirror.relative_to(ROOT)}: no .csproj.")
        return found

    source = ROOT / "src" / package / f"{package}.csproj"
    if not source.is_file():
        found.append(f"{mirror.relative_to(ROOT)}: no library at src/{package}.")
        return found

    pinned = set(MIRROR_PROJECT_REFERENCE.findall(project.read_text(encoding="utf-8")))
    for dependency in sorted(set(SRC_PACKAGE_REFERENCE.findall(source.read_text(encoding="utf-8")))):
        if dependency not in pinned:
            found.append(
                f"{project.relative_to(ROOT)}: {package} depends on {dependency}, which is not "
                f"pinned here. SetTargetFramework does not cross a PackageReference, so this "
                f"suite loads {dependency}'s net10.0 build. Add a ProjectReference to "
                f"../../src/{dependency}/{dependency}.csproj with "
                f"SetTargetFramework=\"TargetFramework=netstandard2.0\".")
    return found


def main() -> int:
    projects = mirrors()
    if not projects:
        print("::error::no tests/*.NetStandard.Tests projects found")
        return 1

    # Every mirror is checked before anything is reported: stopping at the first
    # failure would hide a second one behind a fix for the first.
    found = [failure for mirror in projects for failure in failures_in(mirror)]

    for failure in found:
        print(f"::error::{failure}")
    if found:
        return 1

    print(f"ok  {len(projects)} netstandard2.0 mirrors carry a guard and pin every "
          f"Lodestar dependency they load")
    return 0


if __name__ == "__main__":
    sys.exit(main())
