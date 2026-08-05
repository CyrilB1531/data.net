#!/usr/bin/env python3
"""Assert the DataNet.Text version numbers that must agree still agree.

Per-package versioning replaced one repository-wide ``<Version>`` with several
numbers that are related but not equal, and nothing in the build objects when
they drift apart:

* ``src/DataNet.Text/Version.props`` — what DataNet.Text *is*.
* ``src/Directory.Packages.props`` — the *floor* DataNet.Fuzzy requires of it.
* ``tools/check_nuspec_dependencies.py`` — the floor that check asserts shipped.

Two rules hold between them, and both fail silently today:

1. The floor must not exceed the declared version. A floor above it asks
   consumers for a DataNet.Text that this repository has not written yet.
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

ROOT = pathlib.Path(__file__).resolve().parent.parent

VERSION_PROPS = ROOT / "src" / "DataNet.Text" / "Version.props"
PACKAGES_PROPS = ROOT / "src" / "Directory.Packages.props"
NUSPEC_CHECK = ROOT / "tools" / "check_nuspec_dependencies.py"

PACKAGE = "DataNet.Text"
FLAT_CONTAINER = "https://api.nuget.org/v3-flatcontainer/{id}/index.json"


def declared_version() -> str:
    """The version DataNet.Text gives itself."""
    root = ET.parse(VERSION_PROPS).getroot()
    version = root.findtext(".//DataNetTextVersion")
    if version is None:
        raise SystemExit(f"{VERSION_PROPS}: no <DataNetTextVersion>")
    return version.strip()


def floor_version() -> str:
    """The minimum DataNet.Text that DataNet.Fuzzy requires of consumers."""
    root = ET.parse(PACKAGES_PROPS).getroot()
    for item in root.iterfind(".//PackageVersion"):
        if item.get("Include") == PACKAGE:
            version = item.get("Version")
            if version is None:
                raise SystemExit(f"{PACKAGES_PROPS}: {PACKAGE} has no Version")
            return version.strip()
    raise SystemExit(f"{PACKAGES_PROPS}: no PackageVersion for {PACKAGE}")


def asserted_floor() -> str:
    """The floor the .nuspec check expects to find in the shipped package."""
    source = NUSPEC_CHECK.read_text(encoding="utf-8")
    match = re.search(r'^TEXT_FLOOR = "([^"]+)"', source, re.MULTILINE)
    if match is None:
        raise SystemExit(f"{NUSPEC_CHECK}: no TEXT_FLOOR constant")
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


def published_versions() -> set[str] | None:
    """Every DataNet.Text version on nuget.org, or None if it has never shipped."""
    url = FLAT_CONTAINER.format(id=PACKAGE.lower())
    try:
        with urllib.request.urlopen(url, timeout=30) as response:
            return set(json.load(response)["versions"])
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return None
        raise SystemExit(f"nuget.org answered {error.code} for {PACKAGE}") from error
    except urllib.error.URLError as error:
        raise SystemExit(f"could not reach nuget.org: {error.reason}") from error


def main(argv: list[str]) -> int:
    check_feed = False
    for argument in argv[1:]:
        if argument == "--check-feed":
            check_feed = True
        else:
            print(__doc__, file=sys.stderr)
            return 2

    declared = declared_version()
    floor = floor_version()
    asserted = asserted_floor()

    failures: list[str] = []

    if floor != asserted:
        failures.append(
            f"the floor is {floor} in {PACKAGES_PROPS.relative_to(ROOT)} but "
            f"{asserted} in {NUSPEC_CHECK.relative_to(ROOT)} — they name the "
            f"same edge and must agree"
        )

    if ordering_key(floor) > ordering_key(declared):
        failures.append(
            f"the floor {floor} is above the declared version {declared}: "
            f"DataNet.Fuzzy would require a {PACKAGE} this repository has not "
            f"written"
        )

    if check_feed:
        published = published_versions()
        if published is None:
            failures.append(
                f"{PACKAGE} is not on nuget.org at all, so the floor {floor} "
                f"cannot resolve for a clean clone"
            )
        elif floor not in published:
            failures.append(
                f"the floor {floor} is not on nuget.org (published: "
                f"{', '.join(sorted(published, key=ordering_key))}), so a clean "
                f"clone cannot restore DataNet.Fuzzy"
            )

    for failure in failures:
        print(f"::error::{failure}")
    if failures:
        return 1

    print(f"ok  {PACKAGE} declares {declared}, DataNet.Fuzzy floors at {floor}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
