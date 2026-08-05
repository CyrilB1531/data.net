#!/usr/bin/env python3
"""Assert the shipped NuGet dependency graph is exactly the intended one.

`dotnet pack` derives a package's ``<dependencies>`` from whatever the project
resolved at restore time, so the graph consumers see is a *build output*, not
something anyone wrote down. That makes it easy to change by accident: a
PackageReference added for a compile-time helper without ``PrivateAssets=all``
lands in it, and a dependency that quietly disappears is worse still — the
package installs and then fails at run time on a missing assembly.

The expected graph below is the written-down version. It is deliberately exact:
an unexpected dependency fails just as loudly as a missing one.

Usage:  python tools/check_nuspec_dependencies.py <artifacts-directory> [--require-all]

``--require-all`` additionally fails when a known package is absent, which is
what CI wants after packing all three. A release job packs exactly one package,
so it omits the flag.
"""

from __future__ import annotations

import pathlib
import sys
import xml.etree.ElementTree as ET
import zipfile

NET = "net10.0"
NETSTANDARD = ".NETStandard2.0"

TEXT = "DataNet.Text"
FUZZY = "DataNet.Fuzzy"
EMBEDDINGS = "DataNet.Embeddings"
ONNX = "Microsoft.ML.OnnxRuntime"

# Span, Memory and Vector<T> are in-box on net10.0 and come from packages on
# netstandard2.0, so every package carries this pair in that group and only
# in that group.
POLYFILLS = {"System.Memory", "System.Numerics.Vectors"}

# package id -> target framework -> the complete set of dependency ids.
#
# DataNet.Text has nothing of its own by design: it is the dependency-free core
# of the toolkit. DataNet.Fuzzy depends on it because Fuzz.Ratio is built on
# Indel — a genuine transitive dependency, and the only inter-package edge that
# exists.
EXPECTED: dict[str, dict[str, set[str]]] = {
    TEXT: {
        NET: set(),
        NETSTANDARD: POLYFILLS,
    },
    FUZZY: {
        NET: {TEXT},
        NETSTANDARD: {TEXT} | POLYFILLS,
    },
    EMBEDDINGS: {
        NET: {ONNX},
        NETSTANDARD: {ONNX} | POLYFILLS,
    },
}

NUSPEC_NS = "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"


def read_graph(nupkg: pathlib.Path) -> tuple[str, str, dict[str, set[str]]]:
    """Return (package id, version, {target framework: {dependency ids}})."""
    with zipfile.ZipFile(nupkg) as archive:
        name = next(n for n in archive.namelist() if n.endswith(".nuspec"))
        root = ET.fromstring(archive.read(name))

    metadata = root.find(f"{{{NUSPEC_NS}}}metadata")
    if metadata is None:
        raise SystemExit(f"{nupkg.name}: no <metadata> element")

    package_id = metadata.findtext(f"{{{NUSPEC_NS}}}id", default="")
    version = metadata.findtext(f"{{{NUSPEC_NS}}}version", default="")

    graph: dict[str, set[str]] = {}
    for group in metadata.iterfind(
        f"{{{NUSPEC_NS}}}dependencies/{{{NUSPEC_NS}}}group"
    ):
        framework = group.get("targetFramework", "")
        graph[framework] = {
            dependency.get("id", "")
            for dependency in group.iterfind(f"{{{NUSPEC_NS}}}dependency")
        }
    return package_id, version, graph


def parse_arguments(arguments: list[str]) -> tuple[pathlib.Path, bool] | None:
    """Return (artifacts directory, require-all), or None on a usage error."""
    require_all = False
    positional: list[str] = []
    for argument in arguments:
        if argument == "--require-all":
            require_all = True
        elif argument.startswith("--"):
            return None
        else:
            positional.append(argument)

    if len(positional) != 1:
        return None
    return pathlib.Path(positional[0]), require_all


def check_package(nupkg: pathlib.Path) -> tuple[str, str, list[str]]:
    """Return the package id, its version, and every way its graph differs."""
    package_id, version, actual = read_graph(nupkg)

    expected = EXPECTED.get(package_id)
    if expected is None:
        return package_id, version, [f"{package_id}: not in the expected table"]

    if actual.keys() != expected.keys():
        return (
            package_id,
            version,
            [
                f"{package_id} {version}: dependency groups are "
                f"{sorted(actual)}, expected {sorted(expected)}"
            ],
        )

    return (
        package_id,
        version,
        [
            f"{package_id} {version} [{framework}]: dependencies are "
            f"{sorted(actual[framework])}, expected {sorted(ids)}"
            for framework, ids in expected.items()
            if actual[framework] != ids
        ],
    )


def main(argv: list[str]) -> int:
    parsed = parse_arguments(argv[1:])
    if parsed is None:
        print(__doc__, file=sys.stderr)
        return 2
    artifacts, require_all = parsed

    packages = sorted(artifacts.glob("*.nupkg"))
    if not packages:
        print(f"error: no .nupkg found in {artifacts}", file=sys.stderr)
        return 1

    failures: list[str] = []
    seen: set[str] = set()

    for nupkg in packages:
        package_id, version, package_failures = check_package(nupkg)
        seen.add(package_id)
        failures.extend(package_failures)
        if not package_failures:
            print(f"ok  {package_id} {version}")

    if require_all:
        failures.extend(
            f"{missing}: expected a package, none was packed"
            for missing in sorted(EXPECTED.keys() - seen)
        )

    for failure in failures:
        print(f"::error::{failure}")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
