"""render_benchmark_latest.py's own tests: a page header is not a method's content.

nightly_run.md's own '## Against rapidfuzz, in this same run' header sits between
the report-github sections and the compare-* ones. Two methods whose latest
revisions differ -- the common case, since a night only remeasures what its own
diff touched -- each captured that header as trailing content of their own before
#356, and markdownlint's MD024 refused the result once two sections carried the
same sibling heading.
"""
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from render_benchmark_latest import render, sections  # noqa: E402

PAGE_HEADER = "## Against rapidfuzz, in this same run"

DAY = """# Nightly benchmark run

## This run

- Commit: `{sha}`

### {name}-report-github

```text
preamble
```

| Method | Mean |
|---|---:|
| {name} | 1 ms |

{header}

Both sides on this VM in these minutes, which is what makes the ratio readable
where the absolutes are not.

### compare-{compare}

```text
compare content
```
"""


def make_wiki(tmp_path: Path) -> Path:
    """Two revisions, each remeasuring a different class -- day1's ClassA content
    is never touched again, so its own copy of the page header is what leaks."""
    wiki = tmp_path / "wiki"
    wiki.mkdir()
    run = lambda *args: subprocess.run(  # noqa: E731
        args, cwd=wiki, check=True, capture_output=True)
    run("git", "init", "-q")
    run("git", "config", "user.email", "t@t")
    run("git", "config", "user.name", "t")

    for sha, name, compare in [
        ("1" * 40, "ClassA", "indel"),
        ("2" * 40, "ClassB", "levenshtein"),
    ]:
        (wiki / "nightly_run.md").write_text(
            DAY.format(sha=sha, name=name, compare=compare, header=PAGE_HEADER),
            encoding="utf-8")
        run("git", "add", "-A")
        run("git", "commit", "-q", "-m", f"Nightly benchmark run for {name}")
    return wiki


def test_sections_does_not_bleed_a_page_header_into_the_previous_method():
    body = DAY.format(sha="1" * 40, name="ClassA", compare="indel", header=PAGE_HEADER)
    _, rest = sections(body)["ClassA-report-github"]
    assert PAGE_HEADER not in rest


def test_the_aggregate_carries_the_page_header_at_most_once_per_source_revision(tmp_path):
    wiki = make_wiki(tmp_path)
    page = render(wiki, limit=200, commit="")
    assert page.count(PAGE_HEADER) <= 1
