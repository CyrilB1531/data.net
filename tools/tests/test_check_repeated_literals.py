"""check_repeated_literals.py's own tests: what a change adds, not what it inherits.

A synthetic repo, not the real one. The check reads git, and tools/ already
holds some 200 literals over the threshold -- replaying it against the real
tree would measure the backlog rather than the rule.
"""
import subprocess
import sys
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import check_repeated_literals as guard  # noqa: E402
from check_repeated_literals import main, over_threshold  # noqa: E402


def make_repo(tmp_path: Path) -> Path:
    repo = tmp_path / "repo"
    (repo / "tools").mkdir(parents=True)
    run = lambda *args: subprocess.run(  # noqa: E731
        args, cwd=repo, check=True, capture_output=True)
    run("git", "init", "-q")
    run("git", "config", "user.email", "t@t")
    run("git", "config", "user.name", "t")
    return repo


def commit(repo: Path, message: str) -> str:
    run = lambda *args: subprocess.run(  # noqa: E731
        args, cwd=repo, check=True, capture_output=True, text=True)
    run("git", "add", "-A")
    run("git", "commit", "-q", "-m", message)
    return run("git", "rev-parse", "HEAD").stdout.strip()


def write(repo: Path, name: str, body: str) -> None:
    (repo / "tools" / name).write_text(body, encoding="utf-8")


@pytest.fixture
def rooted(tmp_path, monkeypatch):
    """The guard pointed at a synthetic repo instead of this one."""
    repo = make_repo(tmp_path)
    monkeypatch.setattr(guard, "ROOT", repo)
    return repo


def test_a_literal_reaching_the_threshold_is_reported(rooted, capsys):
    write(rooted, "gen.py", 'A = "the cat"\nB = "the cat"\n')
    base = commit(rooted, "two occurrences")
    write(rooted, "gen.py", 'A = "the cat"\nB = "the cat"\nC = "the cat"\n')
    commit(rooted, "three")

    assert main(["prog", "--base", base]) == 1
    assert "'the cat' now appears 3 times (was 2)" in capsys.readouterr().out


def test_a_literal_already_over_the_threshold_is_reported_only_when_it_grows(rooted, capsys):
    over = 'A = "metadata"\nB = "metadata"\nC = "metadata"\n'
    write(rooted, "gen.py", over)
    base = commit(rooted, "already over")
    write(rooted, "gen.py", over + 'D = "unrelated value"\n')
    commit(rooted, "untouched backlog")

    assert main(["prog", "--base", base]) == 0

    write(rooted, "gen.py", over + 'E = "metadata"\n')
    commit(rooted, "grown")

    assert main(["prog", "--base", base]) == 1
    assert "'metadata' now appears 4 times (was 3)" in capsys.readouterr().out


def test_two_occurrences_are_not_a_finding(rooted):
    write(rooted, "gen.py", "A = None\n")
    base = commit(rooted, "empty")
    write(rooted, "gen.py", 'A = "the cat"\nB = "the cat"\n')
    commit(rooted, "two")

    assert main(["prog", "--base", base]) == 0


def test_naming_the_literal_clears_the_finding(rooted):
    write(rooted, "gen.py", 'A = "the cat"\nB = "the cat"\n')
    base = commit(rooted, "two occurrences")
    write(rooted, "gen.py", 'CAT = "the cat"\nA = CAT\nB = CAT\nC = CAT\n')
    commit(rooted, "named once, used three times")

    assert main(["prog", "--base", base]) == 0


def test_a_file_the_change_adds_is_judged_on_its_own(rooted, capsys):
    write(rooted, "gen.py", "A = None\n")
    base = commit(rooted, "one file")
    write(rooted, "extra.py", 'A = "brand new"\nB = "brand new"\nC = "brand new"\n')
    commit(rooted, "a new file already over")

    assert main(["prog", "--base", base]) == 1
    assert "(new)" in capsys.readouterr().out


def test_a_short_literal_is_not_counted():
    assert over_threshold('A = "ok"\nB = "ok"\nC = "ok"\n') == {}


def test_a_docstring_is_not_a_repeated_constant():
    source = (
        'def a():\n    "shared documentation"\n\n'
        'def b():\n    "shared documentation"\n\n'
        'def c():\n    "shared documentation"\n')
    assert over_threshold(source) == {}


def test_an_unparsable_file_is_not_judged():
    assert over_threshold("def choice[T](seq): ...\nthis is not python\n") == {}


def test_report_lists_the_backlog_and_exits_zero(rooted, capsys):
    write(rooted, "gen.py", 'A = "metadata"\nB = "metadata"\nC = "metadata"\n')
    commit(rooted, "backlog")

    assert main(["prog", "--report"]) == 0
    out = capsys.readouterr().out
    assert "3x  'metadata'" in out
    assert "1 literal(s) repeated 3 times or more" in out


def test_help_exits_zero_and_prints_to_stdout(capsys):
    assert main(["prog", "--help"]) == 0
    assert "S1192" in capsys.readouterr().out


def test_no_base_is_bad_usage(capsys):
    assert main(["prog"]) == 2
    assert "S1192" in capsys.readouterr().err
