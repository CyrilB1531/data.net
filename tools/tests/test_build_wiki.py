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


def test_an_archived_page_links_to_its_own_frozen_counterpart(tmp_path):
    """The whole point of an archive: one click must not land on what main says today."""
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    build_wiki.build(
        repo, out, MAP, released={"DataNet.Text": "0.3.0"},
        archive=("DataNet.Text", "0.4.0"),
    )

    frozen = (out / "Text-0.4.0-quickstart.md").read_text(encoding="utf-8")
    assert "(Text-0.4.0-distances)" in frozen
    assert "(Text-distances)" not in frozen


def test_an_archived_page_leaves_another_package_and_the_root_pages_alone(tmp_path):
    """Only the named package is frozen, so only its links move."""
    repo = make_repo(tmp_path)
    (repo / "docs" / "guides" / "embeddings.md").write_text("# Embeddings\n", encoding="utf-8")
    (repo / "docs" / "guides" / "quickstart.md").write_text(
        "# Quickstart\n\nSee [distances](../reference/text/distances.md), "
        "[embeddings](embeddings.md) and [the table](../equivalence.md).\n",
        encoding="utf-8",
    )
    mapping = json.loads(json.dumps(MAP))
    mapping["packages"]["DataNet.Embeddings"] = {
        "wiki": "Embeddings",
        "pages": ["docs/guides/embeddings.md"],
        "covered": {},
    }
    out = tmp_path / "wiki"
    build_wiki.build(
        repo, out, mapping, released={"DataNet.Text": "0.3.0"},
        archive=("DataNet.Text", "0.4.0"),
    )

    frozen = (out / "Text-0.4.0-quickstart.md").read_text(encoding="utf-8")
    assert "(Embeddings-embeddings)" in frozen
    assert "(equivalence)" in frozen


def test_an_archived_page_says_which_version_it_is(tmp_path):
    """Nothing else on the page does: its name is in the URL, not in the text."""
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    build_wiki.build(
        repo, out, MAP, released={"DataNet.Text": "0.3.0"},
        archive=("DataNet.Text", "0.4.0"),
    )

    frozen = (out / "Text-0.4.0-distances.md").read_text(encoding="utf-8")
    assert frozen.startswith("> **DataNet.Text 0.4.0.**")
    # The live channel, so a reader can leave the archive deliberately.
    assert "(Text)" in frozen


def test_an_archive_run_leaves_the_sidebar_and_the_banner_naming_the_new_version(tmp_path):
    """A release tag is the one moment the generated banner could go stale."""
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"
    build_wiki.build(repo, out, MAP, released={"DataNet.Text": "0.3.0"})

    build_wiki.build(
        repo, out, MAP, released={"DataNet.Text": "0.4.0"},
        archive=("DataNet.Text", "0.4.0"),
    )

    sidebar = (out / "_Sidebar.md").read_text(encoding="utf-8")
    assert "[0.4.0](Text-0.4.0-quickstart)" in sidebar
    assert "0.4.0" in (out / "Home.md").read_text(encoding="utf-8")
    live = (out / "Text-quickstart.md").read_text(encoding="utf-8")
    assert live.startswith("> **Development build.**")
    assert "(Text-0.4.0-quickstart)" in live


def test_a_package_with_no_pages_yet_is_named_on_home_without_a_link(tmp_path):
    """`[Metrics](Metrics)` was a 404: a glob that matches nothing has no landing page."""
    repo = make_repo(tmp_path)
    mapping = json.loads(json.dumps(MAP))
    mapping["packages"]["DataNet.Metrics"] = {
        "wiki": "Metrics",
        "pages": ["docs/reference/metrics/*.md"],
        "covered": {},
    }
    out = tmp_path / "wiki"

    build_wiki.build(repo, out, mapping, released={"DataNet.Metrics": "0.3.0"})

    home = (out / "Home.md").read_text(encoding="utf-8")
    assert "`DataNet.Metrics`" in home
    assert "[Metrics](Metrics)" not in home
    # The sidebar already skipped the channel; Home now agrees that there is nothing to link.
    assert "Metrics" not in (out / "_Sidebar.md").read_text(encoding="utf-8")
    # A package with neither a covered namespace nor a guide gets no entry page either --
    # one linking nothing would not be navigation, so there is nothing for Home to point at.
    assert not (out / "Metrics.md").exists()


def test_a_package_entry_page_links_every_covered_namespace_and_guide(tmp_path):
    repo = make_repo(tmp_path)
    mapping = json.loads(json.dumps(MAP))
    mapping["packages"]["DataNet.Text"]["covered"] = {
        "DataNet.Text.Distances": "docs/reference/text/distances.md",
    }
    out = tmp_path / "wiki"

    build_wiki.build(repo, out, mapping, released={"DataNet.Text": "0.3.0"})

    entry = (out / "Text.md").read_text(encoding="utf-8")
    assert "[DataNet.Text.Distances](Text-distances)" in entry
    # The guide's own H1 is the link text, read off the page rather than guessed
    # from its file name.
    assert "[Quickstart](Text-quickstart)" in entry


def test_a_namespace_split_over_several_pages_gets_a_row_each(tmp_path):
    """DataNet.Metrics' shape: one namespace, two documents, two rows a reader can tell apart."""
    repo = make_repo(tmp_path)
    (repo / "docs" / "reference" / "text" / "similarity.md").write_text(
        "# Similarity\n", encoding="utf-8"
    )
    mapping = json.loads(json.dumps(MAP))
    mapping["packages"]["DataNet.Text"]["covered"] = {
        "DataNet.Text.Distances": [
            "docs/reference/text/distances.md",
            "docs/reference/text/similarity.md",
        ],
    }
    out = tmp_path / "wiki"

    build_wiki.build(repo, out, mapping, released={"DataNet.Text": "0.3.0"})

    entry = (out / "Text.md").read_text(encoding="utf-8")
    assert "[DataNet.Text.Distances — distances](Text-distances)" in entry
    assert "[DataNet.Text.Distances — similarity](Text-similarity)" in entry
    # The bare namespace label is what one page gets; two must not both carry it.
    assert "[DataNet.Text.Distances](Text-distances)" not in entry


def test_home_links_a_package_to_its_entry_page_not_its_landing_guide(tmp_path):
    repo = make_repo(tmp_path)
    out = tmp_path / "wiki"

    build_wiki.build(repo, out, MAP, released={"DataNet.Text": "0.3.0"})

    home = (out / "Home.md").read_text(encoding="utf-8")
    assert "[Text](Text)" in home
    assert "[Text](Text-quickstart)" not in home


def test_a_stale_entry_page_is_removed_once_the_package_covers_nothing(tmp_path):
    """A page that stops linking anything must not linger and outlive its link."""
    repo = make_repo(tmp_path)
    mapping = json.loads(json.dumps(MAP))
    mapping["packages"]["DataNet.Text"]["covered"] = {
        "DataNet.Text.Distances": "docs/reference/text/distances.md",
    }
    out = tmp_path / "wiki"
    build_wiki.build(repo, out, mapping, released={"DataNet.Text": "0.3.0"})
    assert (out / "Text.md").exists()

    mapping["packages"]["DataNet.Text"]["pages"] = ["docs/reference/text/*.md"]
    mapping["packages"]["DataNet.Text"]["covered"] = {}
    build_wiki.build(repo, out, mapping, released={"DataNet.Text": "0.3.0"})

    assert not (out / "Text.md").exists()


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
    # The channel points at its entry page, not at its first guide (D10).
    assert "[Text](Text)" in sidebar
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


def test_a_map_entry_escaping_the_repository_is_refused(tmp_path):
    """A glob or a literal pattern in wiki-map.json is trusted as data, not as a path."""
    repo = make_repo(tmp_path)
    (tmp_path / "outside.md").write_text("# Outside\n", encoding="utf-8")
    mapping = json.loads(json.dumps(MAP))
    mapping["root"].append("../outside.md")
    out = tmp_path / "wiki"

    with pytest.raises(build_wiki.MapError):
        build_wiki.build(repo, out, mapping, released={"DataNet.Text": "0.3.0"})


def test_a_page_name_that_would_escape_the_output_directory_is_refused(tmp_path):
    """A channel name feeds every wiki file name -- `..` there must not reach past `out`."""
    repo = make_repo(tmp_path)
    mapping = json.loads(json.dumps(MAP))
    mapping["packages"]["DataNet.Text"]["wiki"] = "../outside"
    out = tmp_path / "wiki"

    with pytest.raises(build_wiki.MapError):
        build_wiki.build(repo, out, mapping, released={"DataNet.Text": "0.3.0"})


def test_an_output_directory_whose_parent_is_missing_is_refused(tmp_path):
    """--out names a wiki clone the caller has made, so one level is all this creates."""
    repo = make_repo(tmp_path)

    with pytest.raises(build_wiki.MapError):
        build_wiki.build(
            repo, tmp_path / "no" / "such" / "wiki", MAP, released={"DataNet.Text": "0.3.0"})


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
