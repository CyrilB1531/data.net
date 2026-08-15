# 0181 — Documentation per version and per function, implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Spec:** [`2026-08-15_0181_documentation-per-version-and-per-function.md`](../specs/2026-08-15_0181_documentation-per-version-and-per-function.md) ·
**Issue:** [#181](https://github.com/CyrilB1531/data.net/issues/181) ·
**Branch:** `docs/181-documentation-per-version-and-per-function`

**Goal:** Publish the documentation to the GitHub wiki per package and per version, and give
`DataNet.Text.Distances` a reference page laid out like the .NET API reference, with every
declaration, parameter list and example checked by a gate.

**Architecture:** The pages stay in `docs/`, where markdownlint, the snippet compiler and pull-request
review already reach them; `tools/build_wiki.py` turns them into a wiki tree and a workflow pushes it,
refreshing each package's live channel on `main` and freezing an archive directory on each
`DataNet.*/v*` tag. Two gates make the pages honest: the snippet extractor learns to turn a `// =>`
comment into an executed assertion, and an xunit test reflects over the shipped assemblies to check
that every exported type and public method has an entry whose declaration, parameters and *Applies
to* match reality.

**Tech Stack:** Python 3.12 (`tools/`, tested with pytest under `tools/tests`), .NET 10 and
.NET Standard 2.0, xunit, GitHub Actions, markdownlint-cli2, Mermaid.

## Global Constraints

- Everything written in English — code, comments, documents, commit messages, pull-request bodies.
- Commit messages carry **no** `feat:` / `fix:` prefix. One concern per branch; reference the issue
  with `Closes #181` in the pull-request body, not in every commit.
- `dotnet build DataNet.slnx -c Release` treats warnings as errors, on both target frameworks.
- No `src/` project may gain a `ProjectReference`; a CI job asserts it through evaluated MSBuild.
- Comment budgets, enforced by `tools/check_comment_length.py`: two lines for an inline comment,
  eight lines of prose for XML documentation. Past either, the block's first line carries
  `long-comment:` and a reason.
- No absolute path from anyone's machine may reach a tracked file — `tools/check_machine_paths.py`
  fails the build on one. Write `<repo>`, `<worktree>`, `<home>`.
- Every tracked Markdown file must pass
  `npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"`.
  Config is `.markdownlint.json`: defaults on, `MD013` off, `MD024` siblings-only.
- Run `dotnet format DataNet.slnx` **once, at the end** (Task 9), not per task. Run it bare — no
  `env -u DOTNET_ROOT` wrapper.
- Read the test count, never the colour: a `--filter` that matches nothing exits zero.
- Prefix every user-facing status message with `[#181]`.

---

## File structure

### Created

| Path | Responsibility |
| --- | --- |
| `docs/wiki-map.json` | The only declaration of which page belongs to which package, and which namespaces are coverage-enforced. |
| `tools/build_wiki.py` | Turns a checkout plus the map into a wiki tree: channels, archives, link rewriting, banner, `_Sidebar.md`, `Home.md`. |
| `tools/tests/test_build_wiki.py` | pytest cover for the above. |
| `tools/tests/test_extract_doc_snippets.py` | pytest cover for the extractor's new markers. |
| `.github/workflows/wiki.yml` | Publishes the tree on `main` pushes, on `DataNet.*/v*` tags, and on demand. |
| `samples/DataNet.DocSnippets/SnippetAssert.cs` | The assertion a `// =>` comment becomes, and the attribute that opts a snippet out of running. |
| `samples/DataNet.DocSnippets/Program.cs` | Reflection runner: instantiates each generated class and invokes each snippet method. |
| `tests/Shared/ReferenceDocumentation.cs` | The gate's engine — parses a reference page, renders a signature from a `MethodInfo`, and reports what is missing. Linked by each package's test project. |
| `tests/DataNet.Text.Tests/Documentation/ReferenceDocumentationTests.cs` | The xunit facts that run the engine for `DataNet.Text`. |
| `docs/reference/text/distances.md` | The pilot page: every exported type of `DataNet.Text.Distances`. |

### Modified

| Path | Change |
| --- | --- |
| `tools/extract_doc_snippets.py` | `docs/reference/**/*.md` added to `SOURCES`; path-aware class names; the `// =>`, `<!-- docs-declaration -->` and `<!-- docs-run: skip -->` markers. |
| `samples/DataNet.DocSnippets/DataNet.DocSnippets.csproj` | `OutputType=Exe`, and `DataNet.Metrics` left out deliberately (no metrics page in this lot). |
| `tests/DataNet.Text.Tests/DataNet.Text.Tests.csproj` | Links `tests/Shared/ReferenceDocumentation.cs`; copies `docs/reference/text/*.md` and `docs/wiki-map.json` to the output directory. |
| `tests/DataNet.Text.NetStandard.Tests/DataNet.Text.NetStandard.Tests.csproj` | The same two item groups, so the gate runs against the netstandard2.0 assembly too. |
| `.github/workflows/ci.yml` | The `docs-snippets` job runs the snippets after building them. |
| `CONTRIBUTING.md` | The reference-entry convention, in *Definition of done*. |
| `tools/README.md` | `build_wiki.py`, and the extractor's new markers. |
| `CHANGELOG.md` | An `Unreleased` entry. |

---

## Task 1: Prove the wiki can be pushed to

Everything else assumes a workflow can write to `data.net.wiki.git`. The spec records that as a bet.
Settle it before building on it, and throw the proof away.

**Files:**

- Create: `.github/workflows/wiki-probe.yml` (deleted again in step 6)

**Interfaces:**

- Consumes: nothing.
- Produces: a settled answer — either `GITHUB_TOKEN` with `contents: write` can push to the wiki, or
  the branch stops here and the fallback (a fine-grained token as a repository secret) becomes its
  own decision, recorded in the spec before Task 2 starts.

- [ ] **Step 1: Initialise the wiki by hand**

A repository's wiki git remote does not exist until the first page is created through the web UI.
Open `https://github.com/CyrilB1531/data.net/wiki`, create a page titled `Home` with the single line
`Placeholder.`, and save it. Without this, the clone in step 3 fails with
`remote: Repository not found` and the probe would blame the token.

- [ ] **Step 2: Write the probe workflow**

```yaml
name: Wiki probe

# Throwaway. Answers one question — can GITHUB_TOKEN write to the wiki repository —
# and is deleted in the same pull request that answers it.

on:
  workflow_dispatch:

permissions:
  contents: write

jobs:
  probe:
    runs-on: ubuntu-latest
    steps:
      - name: Clone the wiki and push one page
        env:
          TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          set -euo pipefail
          git clone "https://x-access-token:${TOKEN}@github.com/${GITHUB_REPOSITORY}.wiki.git" wiki
          cd wiki
          git config user.name  "github-actions[bot]"
          git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
          printf 'Probe for issue 181.\n' > Probe.md
          git add Probe.md
          git commit -m "Probe the wiki write path"
          git push
```

- [ ] **Step 3: Run it and read the result**

Push the branch, then `gh workflow run "Wiki probe" --ref docs/181-documentation-per-version-and-per-function`.
Watch it with `gh run watch`. Expected: green, and `https://github.com/CyrilB1531/data.net/wiki/Probe`
exists.

- [ ] **Step 4: If it failed, stop**

A `403` on the push means `GITHUB_TOKEN` cannot write the wiki. Do not work around it inside this
task. Record the finding in the spec's *Risks* section, and raise the fallback — a fine-grained
personal access token with `Wiki: read and write`, stored as the secret `WIKI_TOKEN` — with the user
before continuing, because it widens what a compromised workflow can reach.

- [ ] **Step 5: Delete the probe page**

Delete `Probe` through the wiki UI, so the first real publication starts from a clean tree.

- [ ] **Step 6: Delete the probe workflow and commit**

```bash
git rm .github/workflows/wiki-probe.yml
git commit -m "Prove GITHUB_TOKEN can write the wiki, and remove the probe"
```

---

## Task 2: The wiki map and the publisher

**Files:**

- Create: `docs/wiki-map.json`, `tools/build_wiki.py`, `tools/tests/test_build_wiki.py`
- Test: `tools/tests/test_build_wiki.py`

**Interfaces:**

- Consumes: nothing from earlier tasks.
- Produces:
  - `docs/wiki-map.json` with the shape shown in step 1 — `root`, and `packages` keyed by package id,
    each holding `wiki` (the channel directory), `pages` (repo-relative globs) and `covered` (a map
    of namespace to reference page). Task 6 reads `covered`; Task 8 adds an entry to it.
  - `tools/build_wiki.py` exposing `load_map(path) -> dict`, `pages_for(package, mapping, repo) ->
    list[pathlib.Path]`, `rewrite_links(text, page, destination, index) -> str`,
    `banner(package, released) -> str`, `sidebar(tree) -> str`, and
    `build(repo, out, mapping, released, archive=None) -> list[pathlib.Path]`.
  - CLI: `python3 tools/build_wiki.py --repo <dir> --out <dir> --released DataNet.Text=0.3.0
    [--archive DataNet.Text=0.4.0]`.

- [ ] **Step 1: Write the map**

Create `docs/wiki-map.json`:

```json
{
  "root": [
    "docs/equivalence.md",
    "docs/guides/performance.md",
    "docs/decisions/*.md",
    "docs/migration/*.md"
  ],
  "packages": {
    "DataNet.Text": {
      "wiki": "Text",
      "pages": [
        "docs/guides/quickstart.md",
        "docs/guides/vectorization.md",
        "docs/reference/text/*.md"
      ],
      "covered": {}
    },
    "DataNet.Embeddings": {
      "wiki": "Embeddings",
      "pages": [
        "docs/guides/embeddings.md",
        "docs/reference/embeddings/*.md"
      ],
      "covered": {}
    },
    "DataNet.Fuzzy": {
      "wiki": "Fuzzy",
      "pages": [
        "docs/guides/migrating-from-rapidfuzz.md",
        "docs/reference/fuzzy/*.md"
      ],
      "covered": {}
    },
    "DataNet.Metrics": {
      "wiki": "Metrics",
      "pages": [
        "docs/reference/metrics/*.md"
      ],
      "covered": {}
    }
  }
}
```

`README.md` is deliberately absent: GitHub already shows it as the repository front page, and the
wiki's `Home.md` is generated. `covered` is empty everywhere — Task 8 is what turns the gate on for
one namespace.

- [ ] **Step 2: Write the failing tests**

Create `tools/tests/test_build_wiki.py`:

```python
"""The publisher's own tests: a fake repository in, a wiki tree out.

Every assertion is about a property the wiki reader would notice -- a page in
the wrong channel, a link that 404s, a banner naming the wrong version, a
sidebar that omits an archive. The fixtures are built in tmp_path rather than
read from the repository, so these do not fail when a guide is renamed.
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import build_wiki  # noqa: E402

MAP = {
    "root": ["docs/equivalence.md"],
    "packages": {
        "DataNet.Text": {
            "wiki": "Text",
            "pages": ["docs/guides/quickstart.md", "docs/reference/text/*.md"],
            "covered": {},
        }
    },
}


def make_repo(tmp_path: Path) -> Path:
    repo = tmp_path / "repo"
    (repo / "docs" / "guides").mkdir(parents=True)
    (repo / "docs" / "reference" / "text").mkdir(parents=True)
    (repo / "docs" / "equivalence.md").write_text("# Equivalence\n", encoding="utf-8")
    (repo / "docs" / "guides" / "quickstart.md").write_text(
        "# Quickstart\n\nSee [distances](../reference/text/distances.md) and "
        "[the table](../equivalence.md).\n",
        encoding="utf-8",
    )
    (repo / "docs" / "reference" / "text" / "distances.md").write_text(
        "# Distances\n", encoding="utf-8"
    )
    (repo / "docs" / "wiki-map.json").write_text(json.dumps(MAP), encoding="utf-8")
    return repo


def test_live_pages_land_in_the_package_channel(tmp_path):
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    build_wiki.build(repo, out, MAP, released={"DataNet.Text": "0.3.0"})

    assert (out / "Text" / "quickstart.md").exists()
    assert (out / "Text" / "distances.md").exists()
    assert (out / "equivalence.md").exists()


def test_an_archive_freezes_the_same_pages_under_the_version(tmp_path):
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    build_wiki.build(
        repo, out, MAP, released={"DataNet.Text": "0.3.0"},
        archive=("DataNet.Text", "0.4.0"),
    )

    assert (out / "Text" / "0.4.0" / "distances.md").exists()
    # An archive publishes that package only, and never rewrites the live channel.
    assert not (out / "Text" / "quickstart.md").exists()


def test_links_are_rewritten_to_wiki_paths(tmp_path):
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    build_wiki.build(repo, out, MAP, released={"DataNet.Text": "0.3.0"})

    text = (out / "Text" / "quickstart.md").read_text(encoding="utf-8")
    assert "(Text/distances)" in text
    assert "(equivalence)" in text
    assert ".md)" not in text


def test_a_live_page_carries_the_banner_naming_the_released_version(tmp_path):
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    build_wiki.build(repo, out, MAP, released={"DataNet.Text": "0.3.0"})

    text = (out / "Text" / "quickstart.md").read_text(encoding="utf-8")
    assert text.startswith("> **Development build.**")
    assert "0.3.0" in text


def test_an_archived_page_carries_no_banner(tmp_path):
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    build_wiki.build(
        repo, out, MAP, released={"DataNet.Text": "0.3.0"},
        archive=("DataNet.Text", "0.4.0"),
    )

    text = (out / "Text" / "0.4.0" / "distances.md").read_text(encoding="utf-8")
    assert not text.startswith("> **Development build.**")


def test_the_sidebar_lists_channels_and_every_archive_present(tmp_path):
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    (out / "Text" / "0.3.0").mkdir(parents=True)
    (out / "Text" / "0.3.0" / "distances.md").write_text("# Distances\n", encoding="utf-8")

    build_wiki.build(repo, out, MAP, released={"DataNet.Text": "0.3.0"})

    sidebar = (out / "_Sidebar.md").read_text(encoding="utf-8")
    assert "[Text](Text/quickstart)" in sidebar
    assert "[0.3.0](Text/0.3.0/distances)" in sidebar


def test_a_page_declared_in_the_map_but_missing_is_an_error(tmp_path):
    repo = make_repo(tmp_path)
    (repo / "docs" / "guides" / "quickstart.md").unlink()
    out = tmp_path / "wiki"

    with pytest.raises(build_wiki.MapError):
        build_wiki.build(repo, out, MAP, released={"DataNet.Text": "0.3.0"})
```

Add `import pytest` next to the other imports at the top of the file.

- [ ] **Step 2b: Run the tests to verify they fail**

Run: `python3 -m pytest tools/tests/test_build_wiki.py -q`
Expected: collection error, `ModuleNotFoundError: No module named 'build_wiki'`.

- [ ] **Step 3: Write the publisher**

Create `tools/build_wiki.py`:

```python
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
        landing = _landing(channel)
        if landing is None:
            continue
        lines.append(f"- [{package['wiki']}]({package['wiki']}/{landing})")
        for version in sorted(
            (child.name for child in channel.iterdir() if child.is_dir()),
            key=_version_key,
        ):
            archived = _landing(channel / version)
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
        landing = sorted(page.stem for page in pages)[0]
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
```

- [ ] **Step 4: Run the tests until they pass**

Run: `python3 -m pytest tools/tests/test_build_wiki.py -q`
Expected: `8 passed`. Read the count — a typo in the file name makes pytest collect nothing and
still exit zero.

- [ ] **Step 5: Run it against the real repository**

```bash
python3 tools/build_wiki.py --repo . --out /tmp/wiki-check --released DataNet.Text=0.3.0 \
  --released DataNet.Embeddings=0.3.0 --released DataNet.Fuzzy=0.3.0 --released DataNet.Metrics=0.1.0
```

Expected: it prints a page count and exits zero. Open `/tmp/wiki-check/Text/quickstart.md` and check
the banner is there and that the link to `../decisions/0002-unicode-comparison-unit.md` became
`(0002-unicode-comparison-unit)`. `docs/reference/text/*.md` matches nothing yet, which is correct —
the pilot page arrives in Task 7.

- [ ] **Step 6: Lint and commit**

```bash
python3 tools/check_machine_paths.py
git add docs/wiki-map.json tools/build_wiki.py tools/tests/test_build_wiki.py
git commit -m "Build the wiki tree from docs/, per package and per version"
```

---

## Task 3: The publishing workflow

**Files:**

- Create: `.github/workflows/wiki.yml`

**Interfaces:**

- Consumes: `tools/build_wiki.py`'s CLI from Task 2, unchanged.
- Produces: a wiki that tracks `main` and archives on tags. Nothing later in this plan depends on it.

- [ ] **Step 1: Write the workflow**

```yaml
name: Wiki

# The pages live in docs/, where markdownlint, the snippet compiler and review
# reach them; this is what turns them into what a reader sees. A push to main
# refreshes each package's live channel, and a per-package tag freezes an
# archive directory. See docs/superpowers/specs/2026-08-15_0181_documentation-per-version-and-per-function.md.

# One `push` trigger carries both: a `paths` filter narrows branch pushes only, so
# the tag trigger is not silently filtered by it.
on:
  push:
    branches: [main]
    tags: ['DataNet.*/v*']
    paths:
      - 'docs/**'
      - 'CONTRIBUTING.md'
      - 'tools/build_wiki.py'
      - '.github/workflows/wiki.yml'
  workflow_dispatch:
    inputs:
      archive:
        description: 'PACKAGE=VERSION to archive, e.g. DataNet.Text=0.4.0. Empty refreshes the live channels.'
        required: false
        default: ''

permissions:
  contents: write

concurrency:
  group: wiki
  cancel-in-progress: false

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
        with:
          fetch-depth: 0   # the released versions are read from the tags

      - uses: actions/setup-python@5fda3b95a4ea91299a34e894583c3862153e4b97 # v7.0.0
        with:
          python-version: '3.12'

      # The tag is <PackageId>/v<Version>, so the latest release of each package is
      # the highest such tag. Read from git rather than from Version.props, which
      # declares the NEXT version and would name something nobody can install.
      - name: The released version of each package
        id: released
        run: |
          set -euo pipefail
          args=""
          for package in DataNet.Text DataNet.Embeddings DataNet.Fuzzy DataNet.Metrics; do
            version=$(git tag --list "$package/v*" | sed "s|$package/v||" | sort -V | tail -1)
            if [ -n "$version" ]; then
              args="$args --released $package=$version"
            fi
          done
          echo "args=$args" >> "$GITHUB_OUTPUT"

      # A ref name is untrusted input, so it stays in the environment and is never
      # interpolated into the script — the same rule release.yml follows.
      - name: What to publish
        id: what
        env:
          REF_NAME: ${{ github.ref_name }}
          REF_TYPE: ${{ github.ref_type }}
          INPUT_ARCHIVE: ${{ github.event.inputs.archive }}
        run: |
          set -euo pipefail
          if [ "$REF_TYPE" = "tag" ]; then
            echo "archive=--archive ${REF_NAME%/v*}=${REF_NAME##*/v}" >> "$GITHUB_OUTPUT"
          elif [ -n "$INPUT_ARCHIVE" ]; then
            echo "archive=--archive $INPUT_ARCHIVE" >> "$GITHUB_OUTPUT"
          else
            echo "archive=" >> "$GITHUB_OUTPUT"
          fi

      - name: Clone the wiki
        env:
          TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: git clone "https://x-access-token:${TOKEN}@github.com/${GITHUB_REPOSITORY}.wiki.git" wiki

      - name: Build the tree
        run: |
          python tools/build_wiki.py --repo . --out wiki \
            ${{ steps.released.outputs.args }} ${{ steps.what.outputs.archive }}

      - name: Push what changed
        working-directory: wiki
        run: |
          set -euo pipefail
          git config user.name  "github-actions[bot]"
          git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
          git add -A
          if git diff --cached --quiet; then
            echo "The wiki already matches this tree."
            exit 0
          fi
          git commit -m "Publish ${GITHUB_REF_NAME}"
          git push
```

- [ ] **Step 2: Verify by dispatch**

Push the branch, then `gh workflow run Wiki --ref docs/181-documentation-per-version-and-per-function`.
Expected: green, and the wiki holds `Home`, `_Sidebar`, `Text/quickstart`, `Text/vectorization`,
`Embeddings/embeddings`, `Fuzzy/migrating-from-rapidfuzz`, plus the ADRs and migration pages at the
root. Open `Text/quickstart` and confirm the banner names 0.3.0 and its links resolve.

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/wiki.yml
git commit -m "Publish the wiki on main pushes and on per-package tags"
```

---

## Task 4: Teach the extractor the reference pages and the three markers

**Files:**

- Modify: `tools/extract_doc_snippets.py`
- Create: `tools/tests/test_extract_doc_snippets.py`

**Interfaces:**

- Consumes: nothing from earlier tasks.
- Produces:
  - Generated classes for `docs/reference/**` land in namespace `DataNet.DocSnippets.Reference`,
    with a class name built from the path (`docs/reference/text/distances.md` → `TextDistances`).
    Task 5's runner runs exactly that namespace.
  - `// => <expected>` on a local-variable declaration becomes
    `SnippetAssert.Value(<name>, "<expected>", "<file>:<line>");` — Task 5 defines `SnippetAssert`.
  - `<!-- docs-declaration -->` above a fence excludes it from compilation; Task 6's gate reads the
    same marker.
  - `<!-- docs-run: skip - reason -->` above a fence emits `[SnippetSkipRun("reason")]` on the
    generated method; Task 5 defines the attribute.

- [ ] **Step 1: Write the failing tests**

Create `tools/tests/test_extract_doc_snippets.py`:

```python
"""The extractor's tests, over the markers a reference entry uses.

The guides' behaviour is pinned here too: a plain `//` comment must stay a
comment. Turning the ~40 existing ones into assertions would be a silent
change of meaning across five documents.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import extract_doc_snippets as extractor  # noqa: E402


def render(text: str, relative: str = "docs/reference/text/distances.md") -> str:
    path = extractor.REPO / relative
    document, _compiled, _skipped = extractor.render(path, text)
    return document


def test_a_plain_comment_stays_a_comment():
    document = render('```csharp\nint d = Levenshtein.Distance("a", "b");  // 1\n```\n')
    assert "SnippetAssert" not in document
    assert "// 1" in document


def test_an_arrow_comment_becomes_an_assertion():
    document = render('```csharp\nint d = Levenshtein.Distance("kitten", "sitting");  // => 3\n```\n')
    assert 'SnippetAssert.Value(d, "3", "docs/reference/text/distances.md:2");' in document


def test_the_arrow_keeps_the_statement_that_produced_the_value():
    document = render('```csharp\nint d = Levenshtein.Distance("a", "b");  // => 1\n```\n')
    assert 'int d = Levenshtein.Distance("a", "b");' in document


def test_var_declarations_are_asserted_too():
    document = render('```csharp\nvar d = Levenshtein.Distance("a", "b");  // => 1\n```\n')
    assert 'SnippetAssert.Value(d, "1", ' in document


def test_an_arrow_on_a_line_that_binds_nothing_is_an_error():
    text = '```csharp\nLevenshtein.Distance("a", "b");  // => 1\n```\n'
    failures = extractor.arrow_failures(extractor.REPO / "docs/reference/text/distances.md", text)
    assert failures and "bind the value to a variable" in failures[0]


def test_a_declaration_fence_is_not_compiled():
    text = (
        "<!-- docs-declaration -->\n\n"
        "```csharp\npublic static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b)\n```\n"
    )
    assert render(text) == ""


def test_a_run_skip_marker_becomes_an_attribute():
    text = (
        "<!-- docs-run: skip - writes a file -->\n"
        '```csharp\nvectorizer.Save("model.json");\n```\n'
    )
    assert '[SnippetSkipRun("writes a file")]' in render(text)


def test_a_reference_page_lands_in_the_reference_namespace():
    document = render('```csharp\nint d = 1;\n```\n')
    assert "namespace DataNet.DocSnippets.Reference;" in document
    assert "class TextDistances" in document


def test_a_guide_keeps_its_namespace_and_its_class_name():
    document = render('```csharp\nint d = 1;\n```\n', relative="docs/guides/quickstart.md")
    assert "namespace DataNet.DocSnippets;" in document
    assert "class Quickstart" in document
```

- [ ] **Step 1b: Run the tests to verify they fail**

Run: `python3 -m pytest tools/tests/test_extract_doc_snippets.py -q`
Expected: failures — `AttributeError: module 'extract_doc_snippets' has no attribute
'arrow_failures'`, and assertion failures on the namespace and marker tests.

- [ ] **Step 2: Add the sources and the path-aware class name**

In `tools/extract_doc_snippets.py`, replace `SOURCES` and `class_name`:

```python
SOURCES = ["README.md", "docs/guides/*.md", "docs/reference/*/*.md"]

REFERENCE = "docs/reference"


def class_name(path: pathlib.Path) -> str:
    """A C# identifier from the path: docs/reference/text/distances.md -> TextDistances.

    Two areas may share a file name -- persistence.md exists for more than one
    package -- so a reference page's identifier carries its directory. Guides keep
    the stem alone, so their generated file names do not churn.
    """
    relative = path.relative_to(REPO).as_posix()
    parts = relative[len(REFERENCE) + 1:-3].split("/") if relative.startswith(REFERENCE) else [path.stem]
    words = [word for part in parts for word in part.replace("_", "-").split("-") if word]
    return "".join(word[:1].upper() + word[1:] for word in words)


def is_reference(path: pathlib.Path) -> bool:
    return path.relative_to(REPO).as_posix().startswith(REFERENCE)
```

- [ ] **Step 3: Add the three markers**

Add next to the existing `SKIP` regex:

```python
# The value a reader is promised. Bound to a local first, so the assertion names
# something: an expression statement would have to be re-evaluated to be checked.
ARROW = re.compile(r"^(?P<code>.*?;)\s*//\s*=>\s*(?P<expected>.+?)\s*$")
DECLARATION = re.compile(r"^<!--\s*docs-declaration\s*-->\s*$")
RUN_SKIP = re.compile(r"^<!--\s*docs-run:\s*skip\b(?P<reason>.*?)-->\s*$")

# `var d = ...;` or `int d = ...;` -- the shapes an assertion can name.
BINDING = re.compile(r"^\s*(?:var|[\w.<>\[\],?\s]+?)\s+(?P<name>[A-Za-z_]\w*)\s*=")
```

Replace `skip_reason` with a marker reader that answers all three, and add the assertion rewrite:

```python
def marker_above(text: str, fence_start: int) -> tuple[str, str] | None:
    """The marker on the line immediately above the fence: its kind and its reason."""
    preceding = text[:fence_start].rstrip("\n").rsplit("\n", 1)
    line = preceding[-1].strip() if preceding else ""
    for kind, pattern in (("skip", SKIP), ("run-skip", RUN_SKIP)):
        match = pattern.match(line)
        if match is not None:
            return kind, match.group("reason").strip(REASON_SEPARATORS) or "no reason given"
    if DECLARATION.match(line):
        return "declaration", ""
    return None


def assert_lines(body: list[str], origin: str, start_line: int) -> list[str]:
    """Every `// =>` comment, as the assertion it promises."""
    out: list[str] = []
    for offset, source_line in enumerate(body):
        match = ARROW.match(source_line)
        if match is None:
            out.append(source_line)
            continue
        binding = BINDING.match(match.group("code"))
        out.append(match.group("code"))
        out.append(
            f'SnippetAssert.Value({binding.group("name")}, "{match.group("expected")}", '
            f'"{origin}:{start_line + offset}");'
        )
    return out


def arrow_failures(path: pathlib.Path, text: str) -> list[str]:
    """`// =>` comments this file carries that name no local to assert."""
    relative = path.relative_to(REPO).as_posix()
    failures = []
    for number, source_line in enumerate(text.splitlines(), start=1):
        match = ARROW.match(source_line)
        if match is not None and BINDING.match(match.group("code")) is None:
            failures.append(
                f"{relative}:{number}: a '// =>' comment on a line that binds nothing. "
                "Assign the value to a local and bind the value to a variable first."
            )
    return failures
```

- [ ] **Step 4: Wire the markers into `render` and `method`**

Replace `render`, and give `method` the attribute:

```python
def method(index: int, origin: str, body: list[str], skip_run: str | None) -> str:
    """One fence, as a method — which is what keeps two fences' locals apart."""
    indented = "\n".join(f"        {line}" if line.strip() else "" for line in body)
    attribute = f'    [SnippetSkipRun("{skip_run}")]\n' if skip_run is not None else ""
    return (
        f"    /// <summary>The C# fence at {origin}.</summary>\n"
        f"{attribute}"
        f"    public void Snippet{index}()\n    {{\n{indented}\n    }}\n"
    )


def render(path: pathlib.Path, text: str) -> tuple[str, int, list[tuple[int, str]]]:
    """The compilation unit for one Markdown file, and the fences that opted out."""
    usings: list[str] = []
    methods: list[str] = []
    skipped: list[tuple[int, str]] = []
    relative = path.relative_to(REPO).as_posix()
    reference = is_reference(path)

    for index, match in enumerate(FENCE.finditer(text)):
        line = line_of(text, match.start())
        marker = marker_above(text, match.start())
        kind = marker[0] if marker else None
        if kind in ("skip", "declaration"):
            skipped.append((line, marker[1] or "a declaration, not a snippet"))
            continue
        body = hoist_usings(match.group(1), usings)
        if reference:
            body = assert_lines(body, relative, line + 1)
        methods.append(method(index, f"{relative}:{line}", body, marker[1] if kind == "run-skip" else None))

    if not methods:
        return "", 0, skipped
    return compilation_unit(path, usings, methods, reference), len(methods), skipped
```

And give `compilation_unit` the namespace switch:

```python
def compilation_unit(path: pathlib.Path, usings: list[str], methods: list[str], reference: bool) -> str:
    """The generated file: hoisted usings, then one partial class of methods."""
    text = HEADER.format(source=path.relative_to(REPO).as_posix())
    text += "\n" + "\n".join(sorted(usings)) + "\n" if usings else "\n"
    namespace = "DataNet.DocSnippets.Reference" if reference else "DataNet.DocSnippets"
    text += f"\nnamespace {namespace};\n\ninternal sealed partial class {class_name(path)}\n{{\n"
    return text + "\n".join(methods) + "}\n"
```

- [ ] **Step 5: Report the arrow failures**

In `scan`, add them to the existing failure list:

```python
    failures = [
        f"{relative}:{line}: a 'docs-compile: skip' marker that no {FENCE_OPEN} fence "
        "follows. Remove it, or move it back above its fence."
        for line in orphan_markers(text)
    ] + arrow_failures(path, text)
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `python3 -m pytest tools/tests/test_extract_doc_snippets.py -q`
Expected: `9 passed`.

- [ ] **Step 7: Prove the guides did not change meaning**

```bash
python3 tools/extract_doc_snippets.py
```

Expected: the same `snippets compiled` count as before this task (record it by running the tool on
`git stash` first if unsure), and `snippets opted out` unchanged. No `Generated/*.cs` file for a
guide may now contain `SnippetAssert`.

```bash
grep -l SnippetAssert samples/DataNet.DocSnippets/Generated/*.cs
```

Expected: no output.

- [ ] **Step 8: Commit**

```bash
git add tools/extract_doc_snippets.py tools/tests/test_extract_doc_snippets.py
git commit -m "Extract the reference pages, and read the declaration, run-skip and assertion markers"
```

---

## Task 5: Run the snippets instead of only compiling them

**Files:**

- Create: `samples/DataNet.DocSnippets/SnippetAssert.cs`, `samples/DataNet.DocSnippets/Program.cs`
- Modify: `samples/DataNet.DocSnippets/DataNet.DocSnippets.csproj`, `.github/workflows/ci.yml`

**Interfaces:**

- Consumes: from Task 4 — generated methods in `DataNet.DocSnippets.Reference`, calls to
  `SnippetAssert.Value(object? actual, string expected, string origin)`, and
  `[SnippetSkipRun(string reason)]` on methods.
- Produces: `dotnet run --project samples/DataNet.DocSnippets` exits non-zero when a documented
  result is wrong. Nothing later depends on it.

- [ ] **Step 1: Write the assertion and the attribute**

Create `samples/DataNet.DocSnippets/SnippetAssert.cs`:

```csharp
using System.Globalization;

namespace DataNet.DocSnippets;

/// <summary>
/// The check a <c>// =&gt;</c> comment in a reference page becomes.
/// </summary>
internal static class SnippetAssert
{
    /// <summary>
    /// Compares a value against what the page promises its reader.
    /// </summary>
    /// <param name="actual">The value the snippet produced.</param>
    /// <param name="expected">The text written after <c>// =&gt;</c> in the page.</param>
    /// <param name="origin">The page and line the promise is on.</param>
    /// <exception cref="InvalidOperationException">The value and the promise disagree.</exception>
    public static void Value(object? actual, string expected, string origin)
    {
        string rendered = actual switch
        {
            null => "null",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => actual.ToString() ?? "null",
        };

        // A trailing ellipsis means the page truncated an irrational result, which is
        // how the guides already write 0.5714… — so it is a prefix, not an equality.
        bool ok = expected.EndsWith('…')
            ? rendered.StartsWith(expected.TrimEnd('…'), StringComparison.Ordinal)
            : string.Equals(rendered, expected, StringComparison.Ordinal);

        if (!ok)
        {
            throw new InvalidOperationException(
                $"{origin}: the page promises '{expected}', the code produced '{rendered}'.");
        }
    }
}

/// <summary>
/// Marks a snippet that compiles but must not be executed.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class SnippetSkipRunAttribute(string reason) : Attribute
{
    /// <summary>Why this snippet cannot run — an ONNX model, a file, a network call.</summary>
    public string Reason { get; } = reason;
}
```

- [ ] **Step 2: Write the runner**

Create `samples/DataNet.DocSnippets/Program.cs`:

```csharp
using System.Reflection;

namespace DataNet.DocSnippets;

// Runs the fences of docs/reference/** only. The guides stay compile-only: they
// open files and load models on purpose, and retrofitting an opt-out onto every
// one of their fences is a change of its own.
internal static class Program
{
    private const string Runnable = "DataNet.DocSnippets.Reference";

    private static int Main()
    {
        int run = 0;
        int skipped = 0;
        List<string> failures = [];

        IEnumerable<Type> types = typeof(Program).Assembly
            .GetTypes()
            .Where(type => type.Namespace == Runnable)
            .OrderBy(type => type.Name, StringComparer.Ordinal);

        foreach (Type type in types)
        {
            object instance = Activator.CreateInstance(type)!;
            foreach (MethodInfo snippet in type
                         .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                         .OrderBy(method => method.Name, StringComparer.Ordinal))
            {
                if (snippet.GetCustomAttribute<SnippetSkipRunAttribute>() is not null)
                {
                    skipped++;
                    continue;
                }

                try
                {
                    snippet.Invoke(instance, null);
                    run++;
                }
                catch (TargetInvocationException error)
                {
                    failures.Add($"{type.Name}.{snippet.Name}: {error.InnerException?.Message}");
                }
            }
        }

        Console.WriteLine($"snippets run     : {run}");
        Console.WriteLine($"snippets skipped : {skipped}");
        foreach (string failure in failures)
        {
            Console.Error.WriteLine($"::error::{failure}");
        }

        return failures.Count == 0 ? 0 : 1;
    }
}
```

- [ ] **Step 3: Make the project an executable**

In `samples/DataNet.DocSnippets/DataNet.DocSnippets.csproj`, add to the existing `PropertyGroup`:

```xml
    <!--
      Running the reference fences is what checks the value each page promises its
      reader; compiling only ever caught an API that moved. Program.cs runs the
      DataNet.DocSnippets.Reference namespace and nothing else — see
      docs/superpowers/specs/2026-08-15_0181_documentation-per-version-and-per-function.md.
    -->
    <OutputType>Exe</OutputType>
```

- [ ] **Step 4: Verify it builds and runs with no reference page yet**

```bash
for proj in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do
  dotnet pack "$proj" -c Release -o ./artifacts
done
python3 tools/extract_doc_snippets.py
NUGET_PACKAGES=/tmp/snippet-packages dotnet run --project samples/DataNet.DocSnippets -c Release
```

Expected: `snippets run     : 0`, `snippets skipped : 0`, exit 0. Zero is correct here — Task 7
writes the first reference page. Do not treat it as proof the runner works; step 5 is that proof.

- [ ] **Step 5: Prove the runner actually fails on a wrong promise**

Create a scratch page, run, then delete it:

```bash
mkdir -p docs/reference/text
printf '# Scratch\n\n```csharp\nusing DataNet.Text.Distances;\n\nint d = Levenshtein.Distance("kitten", "sitting");   // => 4\n```\n' > docs/reference/text/scratch.md
python3 tools/extract_doc_snippets.py
NUGET_PACKAGES=/tmp/snippet-packages dotnet run --project samples/DataNet.DocSnippets -c Release; echo "exit=$?"
```

Expected: `exit=1`, and an error naming `docs/reference/text/scratch.md` with
`the page promises '4', the code produced '3'`. Then change `4` to `3`, rerun, expect `exit=0` and
`snippets run     : 1`. Finally:

```bash
rm docs/reference/text/scratch.md
python3 tools/extract_doc_snippets.py
```

- [ ] **Step 6: Run the snippets in CI**

In `.github/workflows/ci.yml`, rename the job's display name and add a step after `Build them`:

```yaml
  docs-snippets:
    name: Guide snippets compile, reference snippets run
```

```yaml
      # Compiling caught an API that moved; running catches a documented result
      # that is wrong. Only docs/reference/** fences run — see Program.cs.
      - name: Run them
        env:
          NUGET_PACKAGES: ${{ runner.temp }}/snippet-packages
        run: dotnet run --project samples/DataNet.DocSnippets --configuration Release
```

- [ ] **Step 7: Commit**

```bash
git add samples/DataNet.DocSnippets .github/workflows/ci.yml
git commit -m "Run the reference snippets, so a promised result cannot be wrong"
```

---

## Task 6: The reflection gate

**Files:**

- Create: `tests/Shared/ReferenceDocumentation.cs`,
  `tests/DataNet.Text.Tests/Documentation/ReferenceDocumentationTests.cs`
- Modify: `tests/DataNet.Text.Tests/DataNet.Text.Tests.csproj`,
  `tests/DataNet.Text.NetStandard.Tests/DataNet.Text.NetStandard.Tests.csproj`
- Modify: `CONTRIBUTING.md`

**Interfaces:**

- Consumes: `docs/wiki-map.json`'s `covered` map from Task 2 — namespace to reference page,
  repository-relative.
- Produces:
  - `ReferenceDocumentation.Check(Assembly assembly, string package, string wikiMapPath, string
    referenceRoot) -> IReadOnlyList<string>` — the complaints, empty when the pages are right.
  - `ReferenceDocumentation.RenderSignature(MethodInfo method) -> string` — a declaration as a reader
    would write it. Task 8 reads its output when the gate complains.

- [ ] **Step 1: Write the failing tests**

Create `tests/DataNet.Text.Tests/Documentation/ReferenceDocumentationTests.cs`:

```csharp
using System.Reflection;
using DataNet.Tests.Documentation;
using DataNet.Text.Distances;

namespace DataNet.Text.Tests.Documentation;

public sealed class ReferenceDocumentationTests
{
    private static string Root => Path.Combine(AppContext.BaseDirectory, "reference");

    private static string Map => Path.Combine(AppContext.BaseDirectory, "wiki-map.json");

    [Fact]
    public void Every_covered_namespace_is_documented()
    {
        IReadOnlyList<string> complaints = ReferenceDocumentation.Check(
            typeof(Levenshtein).Assembly, "DataNet.Text", Map, Root);

        Assert.Empty(complaints);
    }

    [Fact]
    public void A_signature_reads_the_way_a_page_writes_it()
    {
        MethodInfo method = typeof(Levenshtein)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(candidate => candidate.Name == "Distance" && candidate.GetParameters().Length == 2);

        Assert.Equal(
            "public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b)",
            ReferenceDocumentation.RenderSignature(method));
    }

    [Fact]
    public void A_missing_entry_is_reported_with_the_member_that_lacks_it()
    {
        string page = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(page);
        File.WriteAllText(Path.Combine(page, "empty.md"), "# Empty\n");
        string map = Path.Combine(page, "wiki-map.json");
        File.WriteAllText(map, """
            {"root":[],"packages":{"DataNet.Text":{"wiki":"Text","pages":[],
             "covered":{"DataNet.Text.Distances":"empty.md"}}}}
            """);

        IReadOnlyList<string> complaints = ReferenceDocumentation.Check(
            typeof(Levenshtein).Assembly, "DataNet.Text", map, page);

        Assert.Contains(complaints, complaint => complaint.Contains("Levenshtein", StringComparison.Ordinal));
        Directory.Delete(page, recursive: true);
    }
}
```

- [ ] **Step 2: Run them to verify they fail**

Run: `dotnet test tests/DataNet.Text.Tests -c Release --filter "FullyQualifiedName~ReferenceDocumentation"`
Expected: compile error, `The name 'ReferenceDocumentation' does not exist`. A compile error is the
failure here; do not move on if the filter reports zero tests instead.

- [ ] **Step 3: Write the engine**

Create `tests/Shared/ReferenceDocumentation.cs`:

```csharp
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace DataNet.Text.Tests.Documentation;

/// <summary>
/// Checks a reference page against the assembly it documents.
/// </summary>
/// <remarks>
/// long-comment: the rule this file enforces is four separate checks, and a
/// reader needs to know which one failed before they can fix a page.
/// Microsoft derives a declaration, a parameter list and an Applies-to from the
/// assembly. Here they are written by hand, so this is what replaces that
/// derivation: every exported type and public method of a covered namespace has
/// an entry, its declaration block lists exactly the overloads reflection
/// reports, every parameter is named, and Applies to names the targets that
/// really export the member. It runs once per target framework, because each
/// test assembly references a different build of the library — which is the
/// only way the fourth check can see the difference.
/// </remarks>
internal static class ReferenceDocumentation
{
    private const string DeclarationMarker = "<!-- docs-declaration -->";

    /// <summary>Every complaint the pages earn; empty when they are right.</summary>
    public static IReadOnlyList<string> Check(
        Assembly assembly, string package, string wikiMapPath, string referenceRoot)
    {
        List<string> complaints = [];
        string moniker = Moniker(assembly);

        foreach ((string space, string page) in Covered(wikiMapPath, package))
        {
            string path = Path.Combine(referenceRoot, Path.GetFileName(page));
            if (!File.Exists(path))
            {
                complaints.Add($"{page}: declared covered for {space}, and not next to the tests.");
                continue;
            }

            Page parsed = Page.Parse(File.ReadAllText(path));
            CheckNamespace(assembly, space, page, parsed, moniker, complaints);
            CheckOverClaims(assembly, space, page, parsed, moniker, complaints);
        }

        return complaints;
    }

    private static void CheckNamespace(
        Assembly assembly, string space, string page, Page parsed, string moniker, List<string> complaints)
    {
        foreach (Type type in assembly.GetExportedTypes()
                     .Where(candidate => candidate.Namespace == space)
                     .OrderBy(candidate => candidate.Name, StringComparer.Ordinal))
        {
            if (!parsed.Entries.ContainsKey(type.Name))
            {
                complaints.Add($"{page}: no entry for the type {type.Name}.");
                continue;
            }

            foreach (IGrouping<string, MethodInfo> overloads in Methods(type))
            {
                string title = $"{type.Name}.{overloads.Key}";
                if (!parsed.Entries.TryGetValue(title, out Entry? entry))
                {
                    complaints.Add($"{page}: no entry for {title}.");
                    continue;
                }

                CheckDeclarations(page, title, entry, overloads, complaints);
                CheckParameters(page, title, entry, overloads, complaints);
                CheckAppliesTo(page, title, entry, moniker, complaints);
            }
        }
    }

    private static void CheckDeclarations(
        string page, string title, Entry entry, IEnumerable<MethodInfo> overloads, List<string> complaints)
    {
        HashSet<string> expected = overloads.Select(RenderSignature).ToHashSet(StringComparer.Ordinal);
        HashSet<string> written = entry.Declarations.ToHashSet(StringComparer.Ordinal);

        foreach (string missing in expected.Except(written).OrderBy(text => text, StringComparer.Ordinal))
        {
            complaints.Add($"{page}: {title} does not declare '{missing}'.");
        }

        foreach (string extra in written.Except(expected).OrderBy(text => text, StringComparer.Ordinal))
        {
            complaints.Add($"{page}: {title} declares '{extra}', which the assembly does not export.");
        }
    }

    private static void CheckParameters(
        string page, string title, Entry entry, IEnumerable<MethodInfo> overloads, List<string> complaints)
    {
        foreach (string name in overloads
                     .SelectMany(method => method.GetParameters())
                     .Select(parameter => parameter.Name!)
                     .Distinct(StringComparer.Ordinal)
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            if (!entry.Parameters.Contains(name))
            {
                complaints.Add($"{page}: {title} never describes the parameter '{name}'.");
            }
        }
    }

    private static void CheckAppliesTo(
        string page, string title, Entry entry, string moniker, List<string> complaints)
    {
        if (!entry.AppliesTo.Contains(moniker))
        {
            complaints.Add(
                $"{page}: {title} is exported by {moniker}, and its 'Applies to' does not say so.");
        }
    }

    private static void CheckOverClaims(
        Assembly assembly, string space, string page, Page parsed, string moniker, List<string> complaints)
    {
        HashSet<string> exported = assembly.GetExportedTypes()
            .Where(type => type.Namespace == space)
            .SelectMany(type => Methods(type).Select(group => $"{type.Name}.{group.Key}")
                .Append(type.Name))
            .ToHashSet(StringComparer.Ordinal);

        foreach ((string title, Entry entry) in parsed.Entries)
        {
            if (entry.AppliesTo.Contains(moniker) && !exported.Contains(title))
            {
                complaints.Add(
                    $"{page}: {title} claims {moniker}, which does not export it.");
            }
        }
    }

    private static IEnumerable<IGrouping<string, MethodInfo>> Methods(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                        BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .GroupBy(method => method.Name, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal);

    /// <summary>A declaration as a reader would write it, not as reflection spells it.</summary>
    public static string RenderSignature(MethodInfo method)
    {
        StringBuilder text = new("public ");
        if (method.IsStatic)
        {
            text.Append("static ");
        }

        text.Append(RenderType(method.ReturnType)).Append(' ').Append(method.Name).Append('(');
        text.AppendJoin(", ", method.GetParameters().Select(RenderParameter)).Append(')');
        return text.ToString();
    }

    private static string RenderParameter(ParameterInfo parameter)
    {
        string modifier = parameter.ParameterType.IsByRef ? "ref " : string.Empty;
        Type type = parameter.ParameterType.IsByRef
            ? parameter.ParameterType.GetElementType()!
            : parameter.ParameterType;
        string rendered = $"{modifier}{RenderType(type)} {parameter.Name}";
        return parameter.HasDefaultValue
            ? $"{rendered} = {RenderDefault(parameter.DefaultValue, type)}"
            : rendered;
    }

    private static string RenderDefault(object? value, Type type) => value switch
    {
        null => type.IsValueType ? "default" : "null",
        bool flag => flag ? "true" : "false",
        string text => $"\"{text}\"",
        _ when type.IsEnum => $"{type.Name}.{Enum.GetName(type, value)}",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "default",
    };

    private static string RenderType(Type type)
    {
        if (Aliases.TryGetValue(type, out string? alias))
        {
            return alias;
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        string name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(RenderType))}>";
    }

    private static readonly Dictionary<Type, string> Aliases = new()
    {
        [typeof(void)] = "void",
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(char)] = "char",
        [typeof(double)] = "double",
        [typeof(float)] = "float",
        [typeof(int)] = "int",
        [typeof(long)] = "long",
        [typeof(object)] = "object",
        [typeof(string)] = "string",
        [typeof(uint)] = "uint",
        [typeof(ulong)] = "ulong",
    };

    /// <summary>The target framework of the build under test, as a page spells it.</summary>
    private static string Moniker(Assembly assembly)
    {
        string? name = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        return name is null || !name.Contains("NETStandard", StringComparison.Ordinal)
            ? "net10.0"
            : "netstandard2.0";
    }

    private static IEnumerable<(string Namespace, string Page)> Covered(string wikiMapPath, string package)
    {
        using JsonDocument map = JsonDocument.Parse(File.ReadAllText(wikiMapPath));
        JsonElement covered = map.RootElement
            .GetProperty("packages").GetProperty(package).GetProperty("covered");

        foreach (JsonProperty entry in covered.EnumerateObject())
        {
            yield return (entry.Name, entry.Value.GetString()!);
        }
    }

    private sealed record Entry(List<string> Declarations, HashSet<string> Parameters, string AppliesTo);

    private sealed record Page(Dictionary<string, Entry> Entries)
    {
        /// <summary>Splits a page into its entries, keyed by the heading text.</summary>
        public static Page Parse(string text)
        {
            Dictionary<string, Entry> entries = new(StringComparer.Ordinal);
            string title = string.Empty;
            Entry current = New();
            bool inDeclaration = false;
            bool inFence = false;

            foreach (string line in text.Split('\n').Select(raw => raw.TrimEnd('\r')))
            {
                if (line.StartsWith("###", StringComparison.Ordinal))
                {
                    Store(entries, title, current);
                    title = line.TrimStart('#').Trim();
                    current = New();
                    inDeclaration = false;
                    continue;
                }

                if (line.Trim() == DeclarationMarker)
                {
                    inDeclaration = true;
                    continue;
                }

                if (line.StartsWith("```", StringComparison.Ordinal))
                {
                    inFence = !inFence;
                    if (!inFence)
                    {
                        inDeclaration = false;
                    }

                    continue;
                }

                if (inDeclaration && inFence && line.Trim().Length > 0)
                {
                    current.Declarations.Add(line.Trim());
                }
                else if (line.StartsWith("**Parameters**", StringComparison.Ordinal))
                {
                    foreach (string name in Backticked(line))
                    {
                        current.Parameters.Add(name);
                    }
                }
                else if (line.StartsWith("**Applies to**", StringComparison.Ordinal))
                {
                    current = current with { AppliesTo = line };
                }
                else if (current.Parameters.Count > 0 && line.StartsWith('`'))
                {
                    foreach (string name in Backticked(line))
                    {
                        current.Parameters.Add(name);
                    }
                }
            }

            Store(entries, title, current);
            return new Page(entries);
        }

        private static Entry New() => new([], new HashSet<string>(StringComparer.Ordinal), string.Empty);

        private static void Store(Dictionary<string, Entry> entries, string title, Entry entry)
        {
            if (title.Length > 0)
            {
                entries[title] = entry;
            }
        }

        private static IEnumerable<string> Backticked(string line)
        {
            int index = 0;
            while ((index = line.IndexOf('`', index)) >= 0)
            {
                int end = line.IndexOf('`', index + 1);
                if (end < 0)
                {
                    yield break;
                }

                yield return line[(index + 1)..end];
                index = end + 1;
            }
        }
    }
}
```

- [ ] **Step 4: Wire the file and the content into both test projects**

In `tests/DataNet.Text.Tests/DataNet.Text.Tests.csproj`, add:

```xml
  <!-- The gate's engine is shared by every package's suite, so it is linked rather
       than copied; the pages and the map are read from the output directory, the
       way the oracle corpora already are. -->
  <ItemGroup>
    <Compile Include="../Shared/ReferenceDocumentation.cs" Link="Documentation/ReferenceDocumentation.cs" />
    <None Include="../../docs/reference/text/*.md" CopyToOutputDirectory="PreserveNewest" LinkBase="reference" />
    <None Include="../../docs/wiki-map.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```

Add the identical `ItemGroup` to `tests/DataNet.Text.NetStandard.Tests/DataNet.Text.NetStandard.Tests.csproj`.
Its `Compile Include="../DataNet.Text.Tests/**/*.cs"` already picks up the test file; the shared
engine and the content items are per-project and do not travel with it.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/DataNet.Text.Tests -c Release --filter "FullyQualifiedName~ReferenceDocumentation"`
Expected: `Passed! - Failed: 0, Passed: 3`. `Every_covered_namespace_is_documented` passes
vacuously — `covered` is still empty — and that is deliberate: Task 8 is what turns it on.

Run: `dotnet test tests/DataNet.Text.NetStandard.Tests -c Release --filter "FullyQualifiedName~ReferenceDocumentation"`
Expected: the same three.

- [ ] **Step 6: Write the convention into CONTRIBUTING.md**

In *Definition of done*, after item 5 (the doc-snippets opt-out), add item 6:

```markdown
6. **A new public type or method carries a reference entry.** The pages under
   `docs/reference/<package>/` follow the layout of the .NET API reference: a `###` entry per
   exported type, a `####` entry per public method with all overloads sharing it, and inside an
   entry, in order — a one-sentence summary, the declaration under a `<!-- docs-declaration -->`
   marker, **Parameters**, **Returns**, **Exceptions**, **Example**, **Remarks**, **Applies to**,
   **See also**. Empty rubrics are left out rather than filled with "none".

   The prose a reader came for lives in **Remarks**: what the member is for, when to prefer it to
   its neighbour, and the trap. The Python counterpart is not repeated — link
   [`docs/equivalence.md`](docs/equivalence.md) under **See also**.

   In an **Example**, a `// =>` comment is an assertion the CI executes; a plain `//` stays a
   comment. The value must be bound to a local first, and a trailing `…` means prefix match. A
   fence that cannot be executed carries `<!-- docs-run: skip - reason -->` on the line above.

   A Mermaid diagram is welcome where it shows a mechanism prose cannot hand a reader in one
   glance, and is removed in review when it only restates the sentence above it.

   Which namespaces are enforced is declared in [`docs/wiki-map.json`](docs/wiki-map.json), and
   `ReferenceDocumentationTests` fails the build when a page and the assembly disagree.
```

- [ ] **Step 7: Lint and commit**

```bash
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "CONTRIBUTING.md"
python3 tools/check_comment_length.py
git add tests/Shared tests/DataNet.Text.Tests tests/DataNet.Text.NetStandard.Tests CONTRIBUTING.md
git commit -m "Check a reference page against the assembly it documents"
```

---

## Task 7: Write the distances page

**Files:**

- Create: `docs/reference/text/distances.md`

**Interfaces:**

- Consumes: the convention from Task 6, the markers from Task 4, the runner from Task 5.
- Produces: the page Task 8 turns the gate on for.

**The members to cover.** Every exported type of `DataNet.Text.Distances`, which is
`src/DataNet.Text/Distances/` plus the set-similarity types: `Cosine`, `DamerauLevenshtein`,
`Hamming`, `Indel`, `Jaccard`, `Jaro`, `JaroWinkler`, `Lcs`, `Levenshtein`, `Osa`, `Overlap`,
`RatcliffObershelp`, `SorensenDice`, `Tversky`. Confirm the list before writing, and do not trust
this one:

```bash
dotnet build src/DataNet.Text -c Release
grep -o 'T:DataNet\.Text\.Distances\.[A-Za-z]*' src/DataNet.Text/bin/Release/net10.0/DataNet.Text.xml | sort -u
```

- [ ] **Step 1: Write the page head, the member table and the diagram**

````markdown
# Distances — `DataNet.Text.Distances`

How different are two pieces of text? Every type on this page answers that, and they disagree on
what "different" means: some count the edits that turn one string into the other, some compare the
sets of characters, and one looks for the longest stretches the two have in common. Picking the
wrong one is the usual cause of a similarity score that looks nothing like what a reader expects.

```mermaid
flowchart TD
    A[What are you comparing?] --> B[Two short strings:<br/>names, codes, typos]
    A --> C[Two longer texts]
    A --> D[Two sets of words<br/>or characters]
    B --> E{Are transpositions<br/>ab vs ba common?}
    E -->|yes| F[JaroWinkler<br/>or DamerauLevenshtein]
    E -->|no| G[Levenshtein]
    C --> H[RatcliffObershelp<br/>or Indel]
    D --> I{Do you need to weight<br/>the two sides differently?}
    I -->|yes| J[Tversky]
    I -->|no| K[Jaccard, SorensenDice<br/>or Overlap]
```

| Type | What it measures |
| --- | --- |
| `Levenshtein` | Insertions, deletions and substitutions. |
| … | … |
````

Fill the table with one row per type, in alphabetical order, and one sentence each.

- [ ] **Step 2: Write one entry, end to end, and prove the machinery on it**

Append this to the page, and change nothing about its shape — the twelve entries after it copy it:

````markdown
### Levenshtein

The edit distance most people mean when they say "how close are these two strings".

#### Levenshtein.Distance

Counts the fewest insertions, deletions and substitutions that turn one string into the other.

<!-- docs-declaration -->

```csharp
public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement unit)
```

**Parameters** — `a` and `b` are the two strings to compare; a `string` converts implicitly, so
nothing is allocated for them. `unit` says what counts as one character: `TextElement.Utf16` by
default, the native and fastest choice, or `TextElement.CodePoint` to match Python outside the Basic
Multilingual Plane.

**Returns** — `int`, the number of edits. Zero when the two are equal, and never negative.

**Example**

```csharp
using DataNet.Text.Distances;

int d = Levenshtein.Distance("kitten", "sitting");   // => 3
```

**Remarks** — this is the ordinary answer to "how different are these two texts", and the right
tool for typing mistakes and mis-keyed names. To compare sets of words rather than characters,
`Jaccard` is the better fit; to weight a common prefix, `JaroWinkler`.

The trap is that the result is not bounded. Three edits are enormous between two six-letter words
and negligible between two paragraphs, so a raw distance cannot be compared across pairs of
different lengths — `NormalizedSimilarity` is what you want for a score in `[0, 1]`.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Levenshtein.NormalizedSimilarity`, `Indel.Distance`, `DamerauLevenshtein.Distance`,
the [Python equivalence table](../../equivalence.md).
````

Then:

```bash
python3 tools/extract_doc_snippets.py
NUGET_PACKAGES=/tmp/snippet-packages dotnet run --project samples/DataNet.DocSnippets -c Release
```

Expected: `snippets run` equals the number of `// =>` lines you wrote, exit 0. If a promise is wrong,
the runner names the page and the line — fix the page, never the assertion.

- [ ] **Step 3: Write the remaining thirteen entries**

One `###` per type, one `####` per public method under it. For each, read the implementation and its
tests before writing the *Remarks*: `tests/DataNet.Text.Tests/Distances/` holds the cases that show
what each one actually does at its edges, and those edges are what a reader needs warned about.

Every entry needs, in order: a one-sentence summary, `<!-- docs-declaration -->` and its fence,
**Parameters**, **Returns**, **Exceptions** where the method throws, **Example** with a `// =>`
assertion, **Remarks**, **Applies to**, **See also**.

For **Applies to**, both targets export everything in this namespace, so every entry reads
`**Applies to** — net10.0, netstandard2.0.` Do not copy that blindly for a later package:
`VectorMath.Dot` is the counter-example, and Task 8's gate is what catches a wrong claim.

- [ ] **Step 4: Check the page**

```bash
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "docs/**/*.md"
python3 tools/check_machine_paths.py
python3 tools/extract_doc_snippets.py
NUGET_PACKAGES=/tmp/snippet-packages dotnet run --project samples/DataNet.DocSnippets -c Release
```

Expected: no lint issues, no machine paths, and every snippet running green.

- [ ] **Step 5: Check the diagram renders**

Push the branch and open `docs/reference/text/distances.md` on GitHub. A Mermaid syntax error renders
as a red box, and nothing in CI catches it.

- [ ] **Step 6: Commit**

```bash
git add docs/reference/text/distances.md
git commit -m "Document every distance in DataNet.Text.Distances"
```

---

## Task 8: Turn the gate on

**Files:**

- Modify: `docs/wiki-map.json`, `docs/reference/text/distances.md`

**Interfaces:**

- Consumes: Task 6's `Check`, Task 7's page.
- Produces: a `covered` entry that later lots copy for their own namespace.

- [ ] **Step 1: Declare the namespace covered**

In `docs/wiki-map.json`, under `DataNet.Text`:

```json
      "covered": {
        "DataNet.Text.Distances": "docs/reference/text/distances.md"
      }
```

- [ ] **Step 2: Run the gate and read every complaint**

Run: `dotnet test tests/DataNet.Text.Tests -c Release --filter "FullyQualifiedName~ReferenceDocumentation"`
Expected: **failure**, listing what the page is missing. This is the first time the gate has anything
to say, so read the whole list rather than fixing the first line.

- [ ] **Step 3: Fix the page until it passes**

Each complaint names its own fix:

- `no entry for the type X` — add a `###` entry.
- `no entry for X.Y` — add a `####` entry.
- `does not declare '<signature>'` — copy the signature from the message into the
  `<!-- docs-declaration -->` fence. It is rendered from reflection, so it is right by construction.
- `declares '<signature>', which the assembly does not export` — a signature was mistyped, or an
  overload was removed.
- `never describes the parameter 'p'` — name it in backticks in **Parameters**.
- `is exported by <moniker>, and its 'Applies to' does not say so` — add the moniker.

- [ ] **Step 4: Run both suites**

```bash
dotnet test tests/DataNet.Text.Tests -c Release --filter "FullyQualifiedName~ReferenceDocumentation"
dotnet test tests/DataNet.Text.NetStandard.Tests -c Release --filter "FullyQualifiedName~ReferenceDocumentation"
```

Expected: three tests passed in each. The netstandard run is what proves the **Applies to** claims,
so a green net10 run alone is not the answer.

- [ ] **Step 5: Commit**

```bash
git add docs/wiki-map.json docs/reference/text/distances.md
git commit -m "Enforce the reference gate on DataNet.Text.Distances"
```

---

## Task 9: Documents, formatting, and the whole-repository verification

**Files:**

- Modify: `tools/README.md`, `CHANGELOG.md`, `CLAUDE.md`

**Interfaces:**

- Consumes: everything above.
- Produces: a branch a reviewer can read.

- [ ] **Step 1: Document the new tool and the new markers**

In `tools/README.md`, add a `build_wiki.py` entry describing what it does and its CLI, and extend the
`extract_doc_snippets.py` entry with the three markers — `<!-- docs-declaration -->`,
`<!-- docs-run: skip - reason -->` and `// =>`. Follow the file's existing entry shape.

- [ ] **Step 2: Add the CHANGELOG entry**

Under `## [Unreleased]`, replacing `Nothing yet.`:

```markdown
### DataNet.Text

#### Added

- `docs/reference/text/distances.md` documents every type of `DataNet.Text.Distances` in the layout of the .NET API reference, and a test checks each declaration, parameter list and `Applies to` against the assembly. ([#181](https://github.com/CyrilB1531/data.net/issues/181))
```

- [ ] **Step 3: Point CLAUDE.md at the gate**

In *Two gates that constrain how code is written*, add a third bullet naming the reference gate: a
new public type or method needs an entry in its package's reference page, and
`docs/wiki-map.json` is what says which namespaces are enforced.

- [ ] **Step 4: Format once**

```bash
dotnet format DataNet.slnx
git diff --stat
```

Run it bare. Review what it touched — it sees rules the build does not, so a change here is a
finding, not noise.

- [ ] **Step 5: Verify the whole repository**

```bash
dotnet build DataNet.slnx -c Release --no-incremental
dotnet test DataNet.slnx -c Release
dotnet format DataNet.slnx --verify-no-changes
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 -m pytest tools/tests -q
python3 tools/check_version_floor.py
python3 tools/check_machine_paths.py
python3 tools/check_comment_length.py
```

Expected: every one green. Read the test **count** from `dotnet test`, not the colour — it must be
the pre-branch count plus six (three new facts, run twice).

- [ ] **Step 6: Check Sonar before pushing**

The SonarQube MCP server has no C# analyser, so use it on what it can read — the two Python files
this branch adds:

```text
analyze_code_snippet on tools/build_wiki.py and the changed part of tools/extract_doc_snippets.py
```

Clear anything it reports before the push. A green build is not a clean Sonar.

- [ ] **Step 7: Commit and open the pull request**

```bash
git add tools/README.md CHANGELOG.md CLAUDE.md
git commit -m "Document the wiki publisher, the snippet markers and the reference gate"
git push -u origin docs/181-documentation-per-version-and-per-function
gh pr create --fill
```

The body carries `Closes #181`, names what the branch adds, and says explicitly that the four
package lots follow.

---

## What this plan does not do

The reference pages for `DataNet.Embeddings`, `DataNet.Fuzzy`, `DataNet.Metrics` and the rest of
`DataNet.Text` — five namespaces beyond `Distances`. Each is its own issue, and each turns its own
`covered` entry on. `DataNet.Metrics` will also need adding to
`samples/DataNet.DocSnippets/DataNet.DocSnippets.csproj`, which references the other three packages
only.
