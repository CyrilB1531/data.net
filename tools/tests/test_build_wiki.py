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


def test_a_live_page_is_named_for_its_channel(tmp_path):
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    build_wiki.build(repo, out, MAP, released={"DataNet.Text": "0.3.0"})

    assert (out / "Text-quickstart.md").exists()
    assert (out / "Text-distances.md").exists()
    assert (out / "equivalence.md").exists()
    # Nothing nests: a subdirectory is storage the reader cannot name.
    assert not any(child.is_dir() for child in out.iterdir())


def test_an_archived_page_carries_the_version_in_its_name(tmp_path):
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    build_wiki.build(
        repo, out, MAP, released={"DataNet.Text": "0.3.0"},
        archive=("DataNet.Text", "0.4.0"),
    )

    assert (out / "Text-0.4.0-distances.md").exists()
    assert (out / "Text-0.4.0-quickstart.md").exists()
    # An archive publishes that package only, and never rewrites the live channel.
    assert not (out / "Text-quickstart.md").exists()


def test_links_are_rewritten_to_flat_wiki_names(tmp_path):
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    build_wiki.build(repo, out, MAP, released={"DataNet.Text": "0.3.0"})

    text = (out / "Text-quickstart.md").read_text(encoding="utf-8")
    assert "(Text-distances)" in text
    assert "(equivalence)" in text
    assert ".md)" not in text


def test_the_sidebar_links_resolve_and_group_the_archives(tmp_path):
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    out.mkdir()
    (out / "Text-0.3.0-quickstart.md").write_text("# Quickstart\n", encoding="utf-8")

    build_wiki.build(repo, out, MAP, released={"DataNet.Text": "0.3.0"})

    sidebar = (out / "_Sidebar.md").read_text(encoding="utf-8")
    assert "[Text](Text-quickstart)" in sidebar
    assert "[0.3.0](Text-0.3.0-quickstart)" in sidebar


def test_the_banner_is_written_only_for_a_version_the_wiki_holds(tmp_path):
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    build_wiki.build(repo, out, MAP, released={"DataNet.Text": "0.3.0"})

    # No Text-0.3.0-* page exists, so a banner would link to a 404.
    assert not (out / "Text-quickstart.md").read_text(encoding="utf-8").startswith(">")


def test_two_pages_that_would_share_a_name_are_refused(tmp_path):
    repo = make_repo(tmp_path)
    (repo / "docs" / "migration").mkdir(parents=True)
    (repo / "docs" / "migration" / "equivalence.md").write_text("# Clash\n", encoding="utf-8")
    mapping = json.loads(json.dumps(MAP))
    mapping["root"].append("docs/migration/equivalence.md")

    with pytest.raises(build_wiki.MapError):
        build_wiki.build(repo, tmp_path / "wiki", mapping, released={})


def test_a_page_declared_in_the_map_but_missing_is_an_error(tmp_path):
    repo = make_repo(tmp_path)
    (repo / "docs" / "guides" / "quickstart.md").unlink()
    out = tmp_path / "wiki"

    with pytest.raises(build_wiki.MapError):
        build_wiki.build(repo, out, MAP, released={"DataNet.Text": "0.3.0"})


def test_an_index_takes_the_name_of_the_directory_it_indexes(tmp_path):
    """Both docs/decisions/ and docs/migration/ call their index README.md."""
    repo = make_repo(tmp_path)
    for area in ("decisions", "migration"):
        (repo / "docs" / area).mkdir(parents=True)
        (repo / "docs" / area / "README.md").write_text(f"# {area}\n", encoding="utf-8")
    mapping = json.loads(json.dumps(MAP))
    mapping["root"] += ["docs/decisions/*.md", "docs/migration/*.md"]
    out = tmp_path / "wiki"

    build_wiki.build(repo, out, mapping, released={})

    assert (out / "decisions.md").exists()
    assert (out / "migration.md").exists()
    assert not (out / "README.md").exists()
