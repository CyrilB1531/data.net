#!/usr/bin/env python3
"""Assert the shipped NuGet dependency graph is exactly the intended one.

`dotnet pack` derives a package's ``<dependencies>`` from whatever the project
resolved at restore time, so the graph consumers see is a *build output*, not
something anyone wrote down. That makes it easy to change by accident: a
PackageReference added for a compile-time helper without ``PrivateAssets=all``
lands in it, and a dependency that quietly disappears is worse still — the
package installs and then fails at run time on a missing assembly.

The expected graph below is the written-down version. It is deliberately exact:
an unexpected dependency fails just as loudly as a missing one, and so does a
dependency whose declared version range has moved — an edge with the wrong floor
is a different edge, however right its id looks.

Usage:  python tools/check_nuspec_dependencies.py <artifacts-directory> [--require-all]

``--require-all`` additionally fails when a known package is absent, which is
what CI wants after packing all three. A release job packs exactly one package,
so it omits the flag.

``EXPECTED`` states the intended graph directly: ``DataNet.Text`` carries
nothing on ``net10.0`` (the dependency-free core) and only
``System.Text.Json`` on ``netstandard2.0`` (the one deliberate exception, so
persisting a fitted model does not mean hand-rolling a JSON writer);
``DataNet.Fuzzy`` depends on ``DataNet.Text`` because ``Fuzz.Ratio`` is built
on ``Indel`` -- the only inter-package edge that exists. The ranges are
asserted too, not only the ids: a bare ``"0.2.0"`` is NuGet's shorthand for
``[0.2.0, )``, and an edge with the wrong floor is a different edge.
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
METRICS = "DataNet.Metrics"
ONNX = "Microsoft.ML.OnnxRuntime"
STJ = "System.Text.Json"

# Span/Memory/Vector<T> are in-box on net10.0, packaged on netstandard2.0 --
# every package carries this pair there only. Floors match what STJ 10.0.x needs.
POLYFILLS = {"System.Memory": "4.6.3", "System.Numerics.Vectors": "4.6.1"}

# netstandard2.0-only, two-package status: docs/decisions/0011-persistence-format.md.
PERSISTENCE = {STJ: "10.0.10"}

# Must equal Directory.Packages.props' PackageVersion: a PackageReference
# emits this floor, but DataNetUseProjectRefs emits Text's own version instead -- catching the escape hatch left on.
TEXT_FLOOR = "0.2.0"

# package id -> target framework -> {dependency id: declared version range}.
# See this module's docstring for what EXPECTED's shape and ranges prove.
EXPECTED: dict[str, dict[str, dict[str, str]]] = {
    TEXT: {
        NET: {},
        NETSTANDARD: {**POLYFILLS, **PERSISTENCE},
    },
    FUZZY: {
        NET: {TEXT: TEXT_FLOOR},
        NETSTANDARD: {TEXT: TEXT_FLOOR, **POLYFILLS},
    },
    EMBEDDINGS: {
        NET: {ONNX: "1.28.0"},
        NETSTANDARD: {ONNX: "1.28.0", **POLYFILLS, **PERSISTENCE},
    },
    METRICS: {
        # Nothing on net10.0, only the polyfills on netstandard2.0: metrics
        # are pure span computation, no I/O to serialise, so no System.Text.Json.
        NET: {},
        NETSTANDARD: {**POLYFILLS},
    },
}

NUSPEC_NS = "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"


def read_nuspec(nupkg: pathlib.Path) -> ET.Element:
    """Return the parsed .nuspec, or fail with a message naming the package.

    A package that cannot be read is a failure like any other, and it earns the
    same attributable one-line message as a wrong dependency: an unreadable
    archive here means `dotnet pack` produced something no consumer can install.
    Left to propagate, these surface as a bare traceback under a step named for
    the dependency graph, which describes neither the file nor the problem.
    """
    try:
        with zipfile.ZipFile(nupkg) as archive:
            names = [n for n in archive.namelist() if n.endswith(".nuspec")]
            if not names:
                raise SystemExit(f"{nupkg.name}: no .nuspec inside the package")
            content = archive.read(names[0])
    except zipfile.BadZipFile as error:
        raise SystemExit(f"{nupkg.name}: not a readable package ({error})") from error

    try:
        return ET.fromstring(content)
    except ET.ParseError as error:
        raise SystemExit(f"{nupkg.name}: malformed .nuspec ({error})") from error


def read_graph(nupkg: pathlib.Path) -> tuple[str, str, dict[str, dict[str, str]]]:
    """Return (id, version, {target framework: {dependency id: version range}})."""
    root = read_nuspec(nupkg)

    metadata = root.find(f"{{{NUSPEC_NS}}}metadata")
    if metadata is None:
        raise SystemExit(f"{nupkg.name}: no <metadata> element")

    package_id = metadata.findtext(f"{{{NUSPEC_NS}}}id", default="")
    version = metadata.findtext(f"{{{NUSPEC_NS}}}version", default="")

    graph: dict[str, dict[str, str]] = {}
    for group in metadata.iterfind(
        f"{{{NUSPEC_NS}}}dependencies/{{{NUSPEC_NS}}}group"
    ):
        framework = group.get("targetFramework", "")
        graph[framework] = {
            dependency.get("id", ""): dependency.get("version", "")
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


def describe(dependencies: dict[str, str]) -> str:
    """Render a dependency group the way the failure message should read it."""
    if not dependencies:
        return "none"
    return ", ".join(f"{name} {range_}" for name, range_ in sorted(dependencies.items()))


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
            f"{describe(actual[framework])}, expected {describe(dependencies)}"
            for framework, dependencies in expected.items()
            if actual[framework] != dependencies
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
