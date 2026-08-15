"""The publisher's own tests: a fake repository in, a wiki tree out.

Every assertion is about a property the wiki reader would notice -- a page in
the wrong channel, a link that 404s, a banner naming the wrong version, a
sidebar that omits an archive. The fixtures are built in tmp_path rather than
read from the repository, so these do not fail when a guide is renamed.
"""
import json
import sys
from pathlib import Path

import pytest

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
