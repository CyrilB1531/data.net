#!/usr/bin/env python3
"""Turn a checkout into the wiki tree, per package and per version.

The pages live in docs/ because that is where markdownlint, the snippet
compiler and pull-request review reach them. This turns them into what a
reader sees: a flat file per page, named for its channel and version -- flat
because a GitHub wiki addresses a page by file name alone, with no directory
context, and two files sharing a base name silently collide (one shadows the
other with no warning).

docs/wiki-map.json is the only place that says which page belongs to which
package. A page listed there and absent from the tree is an error, not a
silence: that is how a renamed guide stops being published without anyone
noticing.

Links are rewritten from repository-relative paths to flat wiki names. A wiki
page has no .md suffix and no directory, so `../reference/text/distances.md`
read in a guide has to become `Text-distances` in the wiki or it 404s.

Each live channel also gets a generated entry page, `<channel>.md` -- `Text.md`
and so on -- linking to every namespace the package covers and every guide it
ships, so Home can send a reader one level down without naming a page that
might not exist. It carries no content of its own to freeze, so an archived
version does not get one: `<channel>-<version>-<stem>` already names every
frozen page a reader can reach from the banner, and the entry page's own name
has no stem to sit in that scheme -- inventing one would mean teaching
_archive_pattern, _live_stems and _archive_stems (which infer "archived" from
a trailing `-<stem>` after the version) to special-case a page that has none,
for a hub whose only content is links the frozen pages already carry to each
other via the banner. A reader who follows a banner back to an archived page
lands on that page's own frozen counterpart, never on an index, and that is
enough.

Usage:
    python3 tools/build_wiki.py --repo <dir> --out <dir>
        --released Lodestar.Text=0.3.0 [--released ...]
        [--archive Lodestar.Text=0.4.0]

Without --archive it refreshes every live channel and the root pages, each
named `<channel>-<stem>`. With it, it first freezes that one package under
`<channel>-<version>-<stem>` and then refreshes the live channels on top of
that copy: the sidebar's version list and the live pages' banner are both read
off the tree, so archiving alone would leave them naming the previous release.

Exit:   0 clean, 1 the map disagrees with the tree, 2 bad usage
"""

from __future__ import annotations

import argparse
import json
import os
import pathlib
import re
import sys

BANNER = (
    "> **Development build.** This page describes `main`, not a released package.\n"
    "> The latest published {package} is **{version}** — read [its documentation]"
    "({target}).\n\n"
)

# The other half of BANNER, and the only thing on a frozen page that says which
# version it is: the name carries it, and the name is in the URL, not in the text.
ARCHIVE_BANNER = (
    "> **{package} {version}.** This page is frozen at that release.{exit}\n"
    "> A link to a decision or a migration page follows `main`, and leaves the archive.\n\n"
)

ARCHIVE_EXIT = " Read [the current documentation]({channel}) for what `main` says now."

# [text](path.md) and [text](path.md#anchor). The target excludes '#' as well as
# ')', so it can't blur into the anchor; no lazy quantifier, so no super-linear backtrack.
LINK = re.compile(r"\[(?P<text>[^\]]*)\]\((?P<target>[^)\s#]+\.md)(?P<anchor>#[^)\s]*)?\)")


class MapError(Exception):
    """The map and the tree disagree — a declared page that is not there."""


def _guard(candidate: pathlib.Path, root: pathlib.Path) -> pathlib.Path:
    """Return candidate's real path, refusing it unless it stays inside root.

    docs/wiki-map.json's patterns and the page names built from them are
    trusted as data, not as paths: a glob or a name carrying `..` would
    otherwise read from, write to, or delete outside the tree this run is
    scoped to (S8707). What comes back is the resolved path and every caller
    uses that, because validating one spelling of a path and then opening
    another is the hole the rule is about. `build` resolves `repo` and `out`
    the same way, so `relative_to` still answers against the root a caller
    holds.
    """
    base = os.path.realpath(root)
    resolved = os.path.realpath(candidate)
    if resolved != base and not resolved.startswith(base + os.sep):
        raise MapError(f"{candidate}: escapes {root}")
    return pathlib.Path(resolved)


def load_map(path: pathlib.Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def pages_for(patterns: list[str], repo: pathlib.Path) -> list[pathlib.Path]:
    """Every page a pattern list names, in declaration order, deduplicated."""
    found: list[pathlib.Path] = []
    for pattern in patterns:
        if "*" in pattern:
            found.extend(_guard(page, repo) for page in sorted(repo.glob(pattern)))
        else:
            page = _guard(repo / pattern, repo)
            if not page.exists():
                raise MapError(f"{pattern}: declared in wiki-map.json, missing from the tree")
            found.append(page)
    return list(dict.fromkeys(found))


def page_stem(page: pathlib.Path) -> str:
    """The name a page is known by, which is not always its file name.

    Both `docs/decisions/` and `docs/migration/` call their index `README.md`,
    and a wiki has no directories to keep the two apart -- so an index takes the
    name of the directory it indexes. Left as the file name, one silently
    shadowed the other, which is what the collision guard in `_write` exists to
    make impossible.
    """
    return page.parent.name if page.stem == "README" else page.stem


def covered_pages(declared: str | list[str]) -> list[str]:
    """The pages one covered namespace maps to, whether it declared one or several.

    `Lodestar.Metrics` is why the plural exists: 31 exported types on one page is a
    scrolling exercise rather than a reference, so its classification and regression
    halves are two documents of one namespace. The gate in
    `tests/Shared/ReferenceDocumentation.cs` reads the same field the same way.
    """
    return [declared] if isinstance(declared, str) else list(declared)


def wiki_name(stem: str, channel: str | None = None, version: str | None = None) -> str:
    """The flat file name a page gets in the wiki: it has no directories."""
    if channel is None:
        return stem
    if version is None:
        return f"{channel}-{stem}"
    return f"{channel}-{version}-{stem}"


def _matches(relative: str, pattern: str) -> bool:
    return pathlib.PurePosixPath(relative).match(pattern)


def link_index(repo: pathlib.Path, mapping: dict) -> dict[str, str]:
    """Every publishable page, keyed by its repository-relative path."""
    index: dict[str, str] = {}
    for page in pages_for(mapping["root"], repo):
        index[page.relative_to(repo).as_posix()] = wiki_name(page_stem(page))
    for package in mapping["packages"].values():
        for page in pages_for(package["pages"], repo):
            index[page.relative_to(repo).as_posix()] = wiki_name(page_stem(page), package["wiki"])
    return index


def rewrite_links(text: str, page: pathlib.Path, repo: pathlib.Path, index: dict[str, str]) -> str:
    """Repository-relative Markdown links, as wiki links."""

    def replace(match: re.Match) -> str:
        target = match.group("target")
        # S5332 false positive: string comparison against an already-written link,
        # never a request -- this function makes no network call of any kind.
        if target.startswith(("http://", "https://")):  # NOSONAR S5332
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
    return BANNER.format(package=package, version=version, target=wiki_name(landing, wiki, version))


def archive_banner(package: str, wiki: str, version: str, has_entry: bool) -> str:
    """What a frozen page opens on.

    The way out is the channel's entry page rather than the live counterpart of
    this page: a `<channel>-<stem>` link is exactly what an archived page must
    not carry, and the hub reaches the same place in one more click. A package
    whose entry page `entry_page` declines to write gets the sentence without
    the link, for the reason D4 gives about the live banner -- a banner naming a
    page the wiki does not hold is worse than no banner.
    """
    return ARCHIVE_BANNER.format(
        package=package,
        version=version,
        exit=ARCHIVE_EXIT.format(channel=wiki) if has_entry else "",
    )


def _archive_pattern(channel: str) -> re.Pattern:
    return re.compile(rf"^{re.escape(channel)}-(?P<version>\d[^-]*)-")


def _archive_stems(out: pathlib.Path, channel: str) -> dict[str, set[str]]:
    """Every archived stem present for a channel, grouped by version."""
    pattern = _archive_pattern(channel)
    by_version: dict[str, set[str]] = {}
    for page in out.glob(f"{channel}-*.md"):
        page = _guard(page, out)
        match = pattern.match(page.stem)
        if match:
            by_version.setdefault(match.group("version"), set()).add(page.stem[match.end():])
    return by_version


def _live_stems(out: pathlib.Path, channel: str) -> set[str]:
    """Every live (non-archived) stem present for a channel."""
    pattern = _archive_pattern(channel)
    prefix_len = len(channel) + 1
    return {
        page.stem[prefix_len:]
        for page in (_guard(page, out) for page in out.glob(f"{channel}-*.md"))
        if not pattern.match(page.stem)
    }


def sidebar(out: pathlib.Path, mapping: dict) -> str:
    """The navigation, read off the tree that was just written."""
    channels = {package["wiki"] for package in mapping["packages"].values()}
    lines = ["### Lodestar", "", "- [Home](Home)", ""]
    for package in mapping["packages"].values():
        channel = package["wiki"]
        landing = _resolve_landing(_live_stems(out, channel), package)
        if landing is None:
            continue
        # The channel's own entry page, not its first guide: D10 makes that page the
        # one place a reader sees the namespaces and the guides together.
        lines.append(f"- [{channel}]({channel})")
        for version, stems in sorted(_archive_stems(out, channel).items(), key=lambda kv: _version_key(kv[0])):
            archived = _resolve_landing(stems, package)
            if archived is not None:
                lines.append(f"  - [{version}]({wiki_name(archived, channel, version)})")
    lines += ["", "### Project", ""]
    for page in sorted(out.glob("*.md")):
        if page.stem in {"Home", "_Sidebar"}:
            continue
        if any(page.stem == channel or page.stem.startswith(f"{channel}-") for channel in channels):
            continue
        lines.append(f"- [{page.stem}]({page.stem})")
    return "\n".join(lines) + "\n"


def _landing(stems: set[str]) -> str | None:
    """The page a channel opens on: the first, alphabetically, that it holds."""
    pages = sorted(stems)
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


def _resolve_landing(stems: set[str], package: dict) -> str | None:
    """The declared guide if these stems actually hold it, else the fallback."""
    declared = _declared_landing(package)
    if declared is not None and declared in stems:
        return declared
    return _landing(stems)


def _version_key(version: str) -> tuple:
    return tuple(int(part) if part.isdigit() else part for part in version.split("."))


def entry_page(repo: pathlib.Path, package: dict, version: str | None = None) -> str | None:
    """A package's hub: a link per namespace it covers, then a link per guide it ships.

    This is the page D10 puts between Home and a reference page -- Home no
    longer links a bare channel name that resolved to nothing. `None` for a
    package that covers no namespace and ships no guide, which is
    `Lodestar.Metrics` before its first reference page lands: a page linking
    nothing is not navigation, and `home` falls back to the same "no pages
    yet" text it already used before this page existed, rather than link to
    an empty one.
    """
    channel = package["wiki"]
    lines = [f"# {channel}", ""]
    linked = False

    if package["covered"]:
        lines += ["## Namespaces", ""]
        for namespace, declared in sorted(package["covered"].items()):
            pages = covered_pages(declared)
            for page in pages:
                stem = page_stem(pathlib.Path(page))
                # The stem disambiguates a namespace with several pages: two rows
                # both reading "Lodestar.Metrics" would be a choice a reader cannot make.
                label = namespace if len(pages) == 1 else f"{namespace} — {stem}"
                lines.append(f"- [{label}]({wiki_name(stem, channel, version)})")
        lines.append("")
        linked = True

    guides = [pattern for pattern in package["pages"] if "*" not in pattern]
    if guides:
        lines += ["## Guides", ""]
        for pattern in guides:
            path = _guard(repo / pattern, repo)
            title = path.read_text(encoding="utf-8").splitlines()[0].lstrip("#").strip()
            lines.append(f"- [{title}]({wiki_name(page_stem(path), channel, version)})")
        lines.append("")
        linked = True

    return "\n".join(lines) + "\n" if linked else None


def home(out: pathlib.Path, mapping: dict, released: dict[str, str]) -> str:
    """The front page, read off the tree that was just written -- as the sidebar is.

    A package with no entry page -- `entry_page` returned `None`, so nothing
    under this channel is covered or guided yet -- has nothing a reader could
    walk down to, and `sidebar` already drops its row for the same reason.
    Home keeps the row, because the released version is the other half of
    what the table is for, and writes no link rather than one to a page that
    does not exist.
    """
    lines = [
        "# Lodestar",
        "",
        "A data-science toolkit for C#/.NET. Each package documents itself, at the version",
        "you installed.",
        "",
        "| Package | Latest released | Documentation |",
        "| --- | --- | --- |",
    ]
    for name, package in mapping["packages"].items():
        version = released.get(name, "unreleased")
        channel = package["wiki"]
        entry_target = _guard(out / f"{channel}.md", out)
        target = f"[{channel}]({channel})" if entry_target.exists() else "no pages yet"
        lines.append(f"| `{name}` | {version} | {target} |")
    return "\n".join(lines) + "\n"


def _build_archive(
    repo: pathlib.Path,
    out: pathlib.Path,
    mapping: dict,
    index: dict[str, str],
    names: dict[str, pathlib.Path],
    archive: tuple[str, str],
) -> list[pathlib.Path]:
    """One package's frozen version, and nothing else -- the --archive branch."""
    name, version = archive
    package = mapping["packages"][name]
    written: list[pathlib.Path] = []
    for stale in out.glob(f"{package['wiki']}-{version}-*.md"):
        _guard(stale, out).unlink()
    frozen = _archive_index(index, repo, package, version)
    prefix = archive_banner(name, package["wiki"], version, entry_page(repo, package) is not None)
    for page in pages_for(package["pages"], repo):
        wname = wiki_name(page_stem(page), package["wiki"], version)
        written.append(_write(page, out, repo, frozen, prefix, wname, names))
    return written


def _archive_index(
    index: dict[str, str], repo: pathlib.Path, package: dict, version: str
) -> dict[str, str]:
    """The link index as a frozen page sees it: this package's pages, versioned.

    Sharing the live index is what sent a reader of 0.4.0 one click into
    whatever `main` says today, which is the one thing an archive exists to
    prevent. Only the frozen package moves: a link to another package's channel
    is right as it stands, because that channel was not frozen, and so is a link
    to a root page, which follows `main` and has no frozen counterpart to point
    at. The banner is what tells the reader those two leave the archive.
    """
    frozen = dict(index)
    for page in pages_for(package["pages"], repo):
        frozen[page.relative_to(repo).as_posix()] = wiki_name(
            page_stem(page), package["wiki"], version)
    return frozen


def _build_channel(
    repo: pathlib.Path,
    out: pathlib.Path,
    index: dict[str, str],
    names: dict[str, pathlib.Path],
    pkg_name: str,
    package: dict,
    released: dict[str, str],
) -> list[pathlib.Path]:
    """One package's live channel: its pages refreshed, banner-prefixed once archived."""
    pages = pages_for(package["pages"], repo)
    if not pages:
        return []

    channel = package["wiki"]
    landing = _declared_landing(package) or min(page_stem(page) for page in pages)
    version = released.get(pkg_name)
    prefix = (
        banner(pkg_name, channel, version, landing)
        if version and version in _archive_stems(out, channel)
        else ""
    )
    archive_pattern = _archive_pattern(channel)
    for stale in out.glob(f"{channel}-*.md"):
        if not archive_pattern.match(stale.stem):
            _guard(stale, out).unlink()

    written = [
        _write(page, out, repo, index, prefix, wiki_name(page_stem(page), channel), names)
        for page in pages
    ]

    entry = entry_page(repo, package)
    entry_target = _guard(out / f"{channel}.md", out)
    if entry is not None:
        entry_target.write_text(prefix + entry, encoding="utf-8")
        written.append(entry_target)
    elif entry_target.exists():
        # Covered before, not now: nothing would link here any more, so the
        # stale page from a previous run must not linger and outlive its link.
        entry_target.unlink()

    return written


def missing_archives(
    out: pathlib.Path, mapping: dict, released: dict[str, str]
) -> list[tuple[str, str]]:
    """The released versions the wiki holds no frozen page for, in map order.

    A tag push archives the version that triggered it, and GitHub keeps at most
    one pending run per concurrency group -- so publishing several tags at once
    drops every archive but the last, silently (#199). This is what a later run
    reads to catch up, and it is why a cancelled run costs nothing.

    The caller archives each from *its own tag*, never from the current tree: the
    documentation moves between releases, and freezing today's pages under an
    older number would be worse than the gap it fills.
    """
    out = pathlib.Path(out)
    behind: list[tuple[str, str]] = []
    for name, package in mapping["packages"].items():
        version = released.get(name)
        if version and version not in _archive_stems(out, package["wiki"]):
            behind.append((name, version))
    return behind


def build(
    repo: pathlib.Path,
    out: pathlib.Path,
    mapping: dict,
    released: dict[str, str],
    archive: tuple[str, str] | None = None,
) -> list[pathlib.Path]:
    """Write the wiki tree. Returns the pages written, for the caller to report.

    An archive is written first and the live channels are then rebuilt on top of
    it, never instead of it. Only the named package is frozen -- that split is
    what lets a per-package tag publish one version -- but the sidebar's version
    list and the banner of D4 are both read off the tree, so returning after the
    archive left the sidebar without the new version and every live page still
    naming the previous release. The banner exists so it cannot go stale, and a
    release tag was the one moment it did.
    """
    # Both roots are resolved once, here, so every path derived from them is
    # already real by the time _guard compares one against them.
    repo, out = pathlib.Path(os.path.realpath(repo)), pathlib.Path(os.path.realpath(out))
    if not out.parent.is_dir():
        raise MapError(f"{out}: its parent directory does not exist")
    # S8707 asks for a path validated against a root; here the argument *is* the root,
    # so only the one level below an existing directory can be checked, and is.
    out.mkdir(exist_ok=True)  # NOSONAR S8707
    index = link_index(repo, mapping)
    names: dict[str, pathlib.Path] = {}

    written: list[pathlib.Path] = (
        _build_archive(repo, out, mapping, index, names, archive) if archive is not None else []
    )

    written += [
        _write(page, out, repo, index, "", wiki_name(page_stem(page)), names)
        for page in pages_for(mapping["root"], repo)
    ]
    for pkg_name, package in mapping["packages"].items():
        written.extend(_build_channel(repo, out, index, names, pkg_name, package, released))

    _guard(out / "_Sidebar.md", out).write_text(sidebar(out, mapping), encoding="utf-8")
    _guard(out / "Home.md", out).write_text(home(out, mapping, released), encoding="utf-8")
    return written


def _write(
    page: pathlib.Path,
    out: pathlib.Path,
    repo: pathlib.Path,
    index: dict[str, str],
    prefix: str,
    name: str,
    names: dict[str, pathlib.Path],
) -> pathlib.Path:
    """Write one page under its flat wiki name, refusing a name collision."""
    if name in names and names[name] != page:
        raise MapError(
            f"{name}: {names[name].relative_to(repo)} and {page.relative_to(repo)} "
            "would both publish under this name"
        )
    names[name] = page
    target = _guard(out / f"{name}.md", out)
    target.write_text(
        prefix + rewrite_links(page.read_text(encoding="utf-8"), page, repo, index),
        encoding="utf-8",
    )
    return target


# S7494 and S7500 disagree on this exact shape: dict() over a generator of pairs,
# or the equivalent comprehension, each draws the other rule's complaint.
def _pairs(values: list[str]) -> dict[str, str]:
    return dict(value.split("=", 1) for value in values)  # NOSONAR S7494


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=pathlib.Path)
    parser.add_argument("--out", required=True, type=pathlib.Path)
    parser.add_argument("--released", action="append", default=[], metavar="PACKAGE=VERSION")
    parser.add_argument("--archive", metavar="PACKAGE=VERSION")
    parser.add_argument(
        "--report-missing-archives",
        action="store_true",
        help="print the released versions the out tree holds no archive for, one per line, and exit",
    )
    arguments = parser.parse_args()

    repo = pathlib.Path(os.path.realpath(arguments.repo))
    archive = tuple(arguments.archive.split("=", 1)) if arguments.archive else None

    try:
        mapping = load_map(_guard(repo / "docs" / "wiki-map.json", repo))
        if arguments.report_missing_archives:
            for name, version in missing_archives(
                arguments.out, mapping, _pairs(arguments.released)
            ):
                print(f"{name}={version}")
            return 0

        written = build(repo, arguments.out, mapping, _pairs(arguments.released), archive)
    except MapError as error:
        print(f"::error::{error}", file=sys.stderr)
        return 1

    print(f"pages written: {len(written)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
