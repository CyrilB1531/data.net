#!/usr/bin/env python3
"""Turn a checkout into the wiki tree, per package and per version.

The pages live in docs/ because that is where markdownlint, the snippet
compiler and pull-request review reach them. This turns them into what a reader
sees: one channel per package following main, one frozen directory per released
version, and a generated sidebar -- generated because a hand-written one is
wrong the second time a version is published.

docs/wiki-map.json is the only place that says which page belongs to which
package. A page listed there and absent from the tree is an error, not a
silence: that is how a renamed guide stops being published without anyone
noticing.

Links are rewritten from repository-relative paths to wiki paths. A wiki page
has no .md suffix and no directory context, so `../reference/text/distances.md`
read in a guide has to become `Text/distances` in the wiki or it 404s.

Usage:
    python3 tools/build_wiki.py --repo <dir> --out <dir>
        --released DataNet.Text=0.3.0 [--released ...]
        [--archive DataNet.Text=0.4.0]

Without --archive it refreshes every live channel and the root pages. With it,
it writes that one package's frozen directory and touches nothing else.

Exit:   0 clean, 1 the map disagrees with the tree, 2 bad usage
"""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import shutil
import sys

BANNER = (
    "> **Development build.** This page describes `main`, not a released package.\n"
    "> The latest published {package} is **{version}** — read [its documentation]"
    "({channel}/{version}/{landing}).\n\n"
)

# [text](path.md) and [text](path.md#anchor). Bounded by ')' rather than a lazy
# quantifier between optional groups, which backtracks super-linearly.
LINK = re.compile(r"\[(?P<text>[^\]]*)\]\((?P<target>[^)\s]+\.md)(?P<anchor>#[^)\s]*)?\)")


class MapError(Exception):
    """The map and the tree disagree — a declared page that is not there."""


def load_map(path: pathlib.Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def pages_for(patterns: list[str], repo: pathlib.Path) -> list[pathlib.Path]:
    """Every page a pattern list names, in declaration order, deduplicated."""
    found: list[pathlib.Path] = []
    for pattern in patterns:
        if "*" in pattern:
            found.extend(sorted(repo.glob(pattern)))
        else:
            page = repo / pattern
            if not page.exists():
                raise MapError(f"{pattern}: declared in wiki-map.json, missing from the tree")
            found.append(page)
    return list(dict.fromkeys(found))


def wiki_path(page: pathlib.Path, repo: pathlib.Path, mapping: dict) -> str:
    """Where one repository page lands in the wiki, without its .md suffix."""
    relative = page.relative_to(repo).as_posix()
    for package in mapping["packages"].values():
        if any(_matches(relative, pattern) for pattern in package["pages"]):
            return f"{package['wiki']}/{page.stem}"
    return page.stem


def _matches(relative: str, pattern: str) -> bool:
    return pathlib.PurePosixPath(relative).match(pattern)


def link_index(repo: pathlib.Path, mapping: dict) -> dict[str, str]:
    """Every publishable page, keyed by its repository-relative path."""
    index: dict[str, str] = {}
    for page in pages_for(mapping["root"], repo):
        index[page.relative_to(repo).as_posix()] = page.stem
    for package in mapping["packages"].values():
        for page in pages_for(package["pages"], repo):
            index[page.relative_to(repo).as_posix()] = f"{package['wiki']}/{page.stem}"
    return index


def rewrite_links(text: str, page: pathlib.Path, repo: pathlib.Path, index: dict[str, str]) -> str:
    """Repository-relative Markdown links, as wiki links."""

    def replace(match: re.Match) -> str:
        target = match.group("target")
        if target.startswith(("http://", "https://")):
            return match.group(0)
        resolved = (page.parent / target).resolve()
        try:
            key = resolved.relative_to(repo.resolve()).as_posix()
        except ValueError:
            return match.group(0)
        if key not in index:
            return match.group(0)
        return f"[{match.group('text')}]({index[key]}{match.group('anchor') or ''})"

    return LINK.sub(replace, text)


def banner(package: str, wiki: str, version: str, landing: str) -> str:
    return BANNER.format(package=package, channel=wiki, version=version, landing=landing)


def sidebar(out: pathlib.Path, mapping: dict) -> str:
    """The navigation, read off the tree that was just written."""
    lines = ["### DataNet", "", "- [Home](Home)", ""]
    for name, package in mapping["packages"].items():
        channel = out / package["wiki"]
        landing = _resolve_landing(channel, package)
        if landing is None:
            continue
        lines.append(f"- [{package['wiki']}]({package['wiki']}/{landing})")
        for version in sorted(
            (child.name for child in channel.iterdir() if child.is_dir()),
            key=_version_key,
        ):
            archived = _resolve_landing(channel / version, package)
            if archived is not None:
                lines.append(f"  - [{version}]({package['wiki']}/{version}/{archived})")
    lines += ["", "### Project", ""]
    for page in sorted(out.glob("*.md")):
        if page.stem not in {"Home", "_Sidebar"}:
            lines.append(f"- [{page.stem}]({page.stem})")
    return "\n".join(lines) + "\n"


def _landing(directory: pathlib.Path) -> str | None:
    """The page a channel opens on: the first, alphabetically, that it holds."""
    pages = sorted(page.stem for page in directory.glob("*.md")) if directory.exists() else []
    return pages[0] if pages else None


def _declared_landing(package: dict) -> str | None:
    """The guide the map declares first for a package, if it declares one.

    Every package lists its guide pages before its `docs/reference/**/*.md`
    glob (see docs/wiki-map.json) -- a reference page such as `distances.md`
    would otherwise win `_landing`'s alphabetical fallback over `quickstart.md`
    and become the channel's front door instead of the guide.
    """
    for pattern in package["pages"]:
        if "*" not in pattern:
            return pathlib.PurePosixPath(pattern).stem
    return None


def _resolve_landing(directory: pathlib.Path, package: dict) -> str | None:
    """The declared guide if this directory actually holds it, else the fallback."""
    declared = _declared_landing(package)
    if declared is not None and (directory / f"{declared}.md").exists():
        return declared
    return _landing(directory)


def _version_key(version: str) -> tuple:
    return tuple(int(part) if part.isdigit() else part for part in version.split("."))


def home(mapping: dict, released: dict[str, str]) -> str:
    lines = [
        "# DataNet",
        "",
        "A data-science toolkit for C#/.NET. Each package documents itself, at the version",
        "you installed.",
        "",
        "| Package | Latest released | Documentation |",
        "| --- | --- | --- |",
    ]
    for name, package in mapping["packages"].items():
        version = released.get(name, "unreleased")
        lines.append(f"| `{name}` | {version} | [{package['wiki']}]({package['wiki']}) |")
    return "\n".join(lines) + "\n"


def build(
    repo: pathlib.Path,
    out: pathlib.Path,
    mapping: dict,
    released: dict[str, str],
    archive: tuple[str, str] | None = None,
) -> list[pathlib.Path]:
    """Write the wiki tree. Returns the pages written, for the caller to report."""
    repo, out = pathlib.Path(repo), pathlib.Path(out)
    index = link_index(repo, mapping)
    written: list[pathlib.Path] = []

    if archive is not None:
        name, version = archive
        package = mapping["packages"][name]
        destination = out / package["wiki"] / version
        _clear(destination)
        for page in pages_for(package["pages"], repo):
            written.append(_write(page, destination, repo, index, prefix=""))
        return written

    for page in pages_for(mapping["root"], repo):
        written.append(_write(page, out, repo, index, prefix=""))

    for name, package in mapping["packages"].items():
        pages = pages_for(package["pages"], repo)
        if not pages:
            continue
        destination = out / package["wiki"]
        for stale in destination.glob("*.md"):
            stale.unlink()
        landing = _declared_landing(package) or sorted(page.stem for page in pages)[0]
        version = released.get(name)
        prefix = banner(name, package["wiki"], version, landing) if version else ""
        for page in pages:
            written.append(_write(page, destination, repo, index, prefix))

    (out / "_Sidebar.md").write_text(sidebar(out, mapping), encoding="utf-8")
    (out / "Home.md").write_text(home(mapping, released), encoding="utf-8")
    return written


def _clear(destination: pathlib.Path) -> None:
    if destination.exists():
        shutil.rmtree(destination)


def _write(
    page: pathlib.Path,
    destination: pathlib.Path,
    repo: pathlib.Path,
    index: dict[str, str],
    prefix: str,
) -> pathlib.Path:
    destination.mkdir(parents=True, exist_ok=True)
    target = destination / page.name
    target.write_text(
        prefix + rewrite_links(page.read_text(encoding="utf-8"), page, repo, index),
        encoding="utf-8",
    )
    return target


def _pairs(values: list[str]) -> dict[str, str]:
    return dict(value.split("=", 1) for value in values)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=pathlib.Path)
    parser.add_argument("--out", required=True, type=pathlib.Path)
    parser.add_argument("--released", action="append", default=[], metavar="PACKAGE=VERSION")
    parser.add_argument("--archive", metavar="PACKAGE=VERSION")
    arguments = parser.parse_args()

    mapping = load_map(arguments.repo / "docs" / "wiki-map.json")
    archive = tuple(arguments.archive.split("=", 1)) if arguments.archive else None

    try:
        written = build(
            arguments.repo, arguments.out, mapping, _pairs(arguments.released), archive
        )
    except MapError as error:
        print(f"::error::{error}", file=sys.stderr)
        return 1

    print(f"pages written: {len(written)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
