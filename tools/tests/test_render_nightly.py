"""render_nightly.py's own tests: two reports can carry the same stem.

MD024 has sunk this page once before, from a different cause -- #356, where a
page header was captured as one method's trailing content and two sections ended
up carrying the same sibling heading. This is the other way in: `included_reports`
emitted one `### {stem}` per path with nothing checking that a stem appears once,
so a class measured twice in a single run produced two byte-identical headings and
the whole lint job failed on them.

Numbering the later occurrence rather than dropping it is the deliberate half. The
second measurement is real data, it is almost certainly not wanted, and a reader
who can see it is the one who can go and remove whatever produced it.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from render_nightly import heading_for, included_reports  # noqa: E402

REPORT = """```

BenchmarkDotNet v0.14.0
Job=ShortRun

```

| Method | Mean |
|---|---:|
| Cjk | {mean} ms |
"""


def write(directory: Path, name: str, mean: str) -> Path:
    directory.mkdir(parents=True, exist_ok=True)
    path = directory / name
    path.write_text(REPORT.format(mean=mean), encoding="utf-8")
    return path


def test_a_repeated_stem_is_numbered_not_dropped(tmp_path: Path) -> None:
    first = write(tmp_path / "a", "X-report-github.md", "1")
    second = write(tmp_path / "b", "X-report-github.md", "2")

    lines = included_reports([first, second])
    headings = [line for line in lines if line.startswith("### ")]

    assert headings == ["### X-report-github", "### X-report-github (run 2)"]
    # Both measurements survive: numbering is not a synonym for deduplication.
    body = "\n".join(lines)
    assert "| Cjk | 1 ms |" in body
    assert "| Cjk | 2 ms |" in body


def test_distinct_stems_are_left_alone(tmp_path: Path) -> None:
    first = write(tmp_path / "a", "X-report-github.md", "1")
    second = write(tmp_path / "a", "Y-report-github.md", "2")

    headings = [line for line in included_reports([first, second]) if line.startswith("### ")]

    assert headings == ["### X-report-github", "### Y-report-github"]


def test_an_empty_report_does_not_consume_an_occurrence(tmp_path: Path) -> None:
    # A report skipped for being empty must not push the next one to "(run 2)":
    # the numbering counts what the page shows, not what the directory held.
    empty = tmp_path / "empty-report-github.md"
    empty.write_text("   \n", encoding="utf-8")
    later = write(tmp_path / "a", "empty-report-github.md", "1")

    headings = [line for line in included_reports([empty, later]) if line.startswith("### ")]

    assert headings == ["### empty-report-github"]


def test_heading_for_names_the_first_occurrence_bare() -> None:
    # The overwhelmingly common case must be byte-identical to what it was, or
    # every existing page churns on the next render for no reason.
    assert heading_for("X-report-github", 1) == "X-report-github"
    assert heading_for("X-report-github", 2) == "X-report-github (run 2)"
