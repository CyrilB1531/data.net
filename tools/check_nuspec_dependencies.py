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


def main(argv: list[str]) -> int:
    arguments = argv[1:]
    require_all = "--require-all" in arguments
    positional = [a for a in arguments if not a.startswith("--")]
    if len(positional) != 1 or len(positional) != len(arguments) - int(require_all):
        print(__doc__, file=sys.stderr)
        return 2

    artifacts = pathlib.Path(positional[0])
    packages = sorted(artifacts.glob("*.nupkg"))
    if not packages:
        print(f"error: no .nupkg found in {artifacts}", file=sys.stderr)
        return 1

    failures: list[str] = []
    seen: set[str] = set()

    for nupkg in packages:
        package_id, version, actual = read_graph(nupkg)
        expected = EXPECTED.get(package_id)
        if expected is None:
            failures.append(f"{package_id}: not in the expected graph table")
            continue

        seen.add(package_id)
        if actual.keys() != expected.keys():
            failures.append(
                f"{package_id} {version}: dependency groups are "
                f"{sorted(actual)}, expected {sorted(expected)}"
            )
            continue

        for framework, ids in expected.items():
            if actual[framework] != ids:
                failures.append(
                    f"{package_id} {version} [{framework}]: dependencies are "
                    f"{sorted(actual[framework])}, expected {sorted(ids)}"
                )
        print(f"ok  {package_id} {version}")

    if require_all:
        for missing in sorted(EXPECTED.keys() - seen):
            failures.append(f"{missing}: expected a package, none was packed")

    for failure in failures:
        print(f"::error::{failure}")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
