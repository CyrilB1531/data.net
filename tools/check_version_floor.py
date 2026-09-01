#!/usr/bin/env python3
"""Assert the version numbers behind each inter-package floor still agree.

Per-package versioning replaced one repository-wide ``<Version>`` with several
numbers that are related but not equal, and nothing in the build objects when
they drift apart. ``FLOORS`` names every edge between two shipped packages, and
each row holds three numbers that must agree:

* ``src/<package>/Version.props`` — what the dependency *is*.
* ``src/Directory.Packages.props`` — the *floor* its dependent requires of it.
* ``tools/check_nuspec_dependencies.py`` — the floor that check asserts shipped.

Two rules hold on every row, and both fail silently today:

1. The floor must not exceed the declared version. A floor above it asks
   consumers for a version this repository has not written yet.
2. The floor must already be on nuget.org. It is what makes ``git clone &&
   dotnet build`` work with no pack step, so a floor naming an unpublished
   version breaks a clean clone — on a contributor's machine, not on the one
   that raised it, whose cache still has the package.

Rule 2 needs the network, so it is opt-in via ``--check-feed``; CI passes it.

Usage:  python tools/check_version_floor.py [--check-feed]
"""

from __future__ import annotations

import json
import pathlib
import re
import sys
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
from dataclasses import dataclass

ROOT = pathlib.Path(__file__).resolve().parent.parent

PACKAGES_PROPS = ROOT / "src" / "Directory.Packages.props"
NUSPEC_CHECK = ROOT / "tools" / "check_nuspec_dependencies.py"

FLAT_CONTAINER = "https://api.nuget.org/v3-flatcontainer/{id}/index.json"


@dataclass(frozen=True)
class Floor:
    """One edge: a package, the dependents that floor it, and where each number lives."""

    package: str
    version_element: str
    floor_constant: str
    required_by: tuple[str, ...]

    @property
    def version_props(self) -> pathlib.Path:
        """Where the package declares what it is."""
        return ROOT / "src" / self.package / "Version.props"

    @property
    def dependents(self) -> str:
        """The dependents, for a message a reader can act on."""
        return " and ".join(self.required_by)


# One row per inter-package edge. Adding an edge without adding it here is the
# drift this script exists to catch, so the row lands in the same commit.
FLOORS = (
    Floor("Lodestar.Text", "LodestarTextVersion", "TEXT_FLOOR", ("Lodestar.Fuzzy",)),
    Floor("Lodestar.Abstractions", "LodestarAbstractionsVersion", "ABSTRACTIONS_FLOOR",
          ("Lodestar.Text", "Lodestar.Decomposition")),
)


def declared_version(floor: Floor) -> str:
    """The version the package gives itself."""
    root = ET.parse(floor.version_props).getroot()
    version = root.findtext(f".//{floor.version_element}")
    if version is None:
        raise SystemExit(f"{floor.version_props}: no <{floor.version_element}>")
    return version.strip()


def floor_version(floor: Floor) -> str:
    """The minimum the dependent requires of consumers."""
    root = ET.parse(PACKAGES_PROPS).getroot()
    for item in root.iterfind(".//PackageVersion"):
        if item.get("Include") == floor.package:
            version = item.get("Version")
            if version is None:
                raise SystemExit(f"{PACKAGES_PROPS}: {floor.package} has no Version")
            return version.strip()
    raise SystemExit(f"{PACKAGES_PROPS}: no PackageVersion for {floor.package}")


def asserted_floor(floor: Floor) -> str:
    """The floor the .nuspec check expects to find in the shipped package."""
    source = NUSPEC_CHECK.read_text(encoding="utf-8")
    match = re.search(rf'^{floor.floor_constant} = "([^"]+)"', source, re.MULTILINE)
    if match is None:
        raise SystemExit(f"{NUSPEC_CHECK}: no {floor.floor_constant} constant")
    return match.group(1)


def ordering_key(version: str) -> tuple[tuple[int, ...], int]:
    """Order releases numerically, and sort a prerelease below its release.

    Enough for the comparison this script makes — it is not a full SemVer
    implementation, and does not order two prereleases of the same version
    against each other.
    """
    release, _, prerelease = version.partition("-")
    numbers = tuple(int(part) for part in release.split(".") if part.isdigit())
    return numbers, 0 if prerelease else 1


def published_versions(floor: Floor) -> set[str] | None:
    """Every version of the package on nuget.org, or None if it has never shipped."""
    url = FLAT_CONTAINER.format(id=floor.package.lower())
    try:
        with urllib.request.urlopen(url, timeout=30) as response:
            return set(json.load(response)["versions"])
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return None
        raise SystemExit(f"nuget.org answered {error.code} for {floor.package}") from error
    except urllib.error.URLError as error:
        raise SystemExit(f"could not reach nuget.org: {error.reason}") from error


def check(floor: Floor, check_feed: bool) -> list[str]:
    """Both rules, on one edge."""
    declared = declared_version(floor)
    pinned = floor_version(floor)
    asserted = asserted_floor(floor)

    failures: list[str] = []
    if pinned != asserted:
        failures.append(
            f"the {floor.package} floor is {pinned} in "
            f"{PACKAGES_PROPS.relative_to(ROOT)} but {asserted} in "
            f"{NUSPEC_CHECK.relative_to(ROOT)} — they name the same edge and must agree"
        )

    if ordering_key(pinned) > ordering_key(declared):
        failures.append(
            f"the {floor.package} floor {pinned} is above the declared version "
            f"{declared}: {floor.dependents} would require a {floor.package} this "
            f"repository has not written"
        )

    if check_feed:
        published = published_versions(floor)
        if published is None:
            failures.append(
                f"{floor.package} is not on nuget.org at all, so the floor {pinned} "
                f"cannot resolve for a clean clone"
            )
        elif pinned not in published:
            failures.append(
                f"the {floor.package} floor {pinned} is not on nuget.org (published: "
                f"{', '.join(sorted(published, key=ordering_key))}), so a clean clone "
                f"cannot restore {floor.dependents}"
            )

    return failures


def main(argv: list[str]) -> int:
    check_feed = False
    for argument in argv[1:]:
        if argument == "--check-feed":
            check_feed = True
        else:
            print(__doc__, file=sys.stderr)
            return 2

    # Every edge is checked before anything is reported: stopping at the first
    # failure would hide a second one behind a fix for the first.
    failures = [failure for floor in FLOORS for failure in check(floor, check_feed)]

    for failure in failures:
        print(f"::error::{failure}")
    if failures:
        return 1

    for floor in FLOORS:
        print(f"ok  {floor.package} declares {declared_version(floor)}, "
              f"{floor.dependents} floors at {floor_version(floor)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
