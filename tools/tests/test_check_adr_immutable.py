"""check_adr_immutable.py's own tests: an accepted ADR is untouchable, full stop.

A synthetic repo, not the real one -- the check reads git diffs between two
commits, and issue #399's own findings are the ADRs this guard exists to have
caught, not fixtures to replay it against.
"""
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import check_adr_immutable as guard  # noqa: E402
from check_adr_immutable import main  # noqa: E402


def make_repo(tmp_path: Path) -> Path:
    repo = tmp_path / "repo"
    (repo / "docs" / "decisions").mkdir(parents=True)
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
    # --allow-empty: the "empty" base commit in one test adds no file at all,
    # since git tracks no directory that holds none.
    run("git", "commit", "-q", "--allow-empty", "-m", message)
    return run("git", "rev-parse", "HEAD").stdout.strip()


def check(repo: Path, base: str) -> int:
    """main(), pointed at the synthetic repo rather than this one -- every
    subprocess call in the module under test already takes cwd=ROOT."""
    guard.ROOT = repo
    return main(["prog", "--base", base])


def test_a_brand_new_adr_is_unrestricted(tmp_path):
    repo = make_repo(tmp_path)
    base = commit(repo, "empty")

    (repo / "docs" / "decisions" / "0001-new.md").write_text("# 0001 -- New\n\nFresh.\n")
    commit(repo, "add 0001")

    assert check(repo, base) == 0


def test_a_new_adr_still_being_drafted_in_the_same_pr_is_unrestricted(tmp_path):
    """A file absent at --base stays exempt through further edits in the same PR."""
    repo = make_repo(tmp_path)
    base = commit(repo, "empty")

    adr = repo / "docs" / "decisions" / "0001-new.md"
    adr.write_text("# 0001 -- New\n\nDraft.\n")
    commit(repo, "add 0001")
    adr.write_text("# 0001 -- New\n\nRevised before merge.\n")
    commit(repo, "revise 0001 before it ever reached main")

    assert check(repo, base) == 0


def test_even_a_pure_addition_to_an_existing_adr_is_refused(tmp_path):
    """Superseded convention: an amendment is its own ADR, never a blockquote
    appended to the original -- 'Amend 0004 in a decision of its own instead
    of editing it' is why this is stricter than append-only."""
    repo = make_repo(tmp_path)
    adr = repo / "docs" / "decisions" / "0001-old.md"
    adr.write_text("# 0001 -- Old\n\nOriginal claim.\n")
    base = commit(repo, "add 0001")

    adr.write_text(
        "# 0001 -- Old\n\n"
        "> **#42 update:** the claim below is stale in one place.\n\n"
        "Original claim.\n")
    commit(repo, "append an update blockquote to 0001")

    assert check(repo, base) == 1


def test_a_rewritten_line_in_an_existing_adr_fails(tmp_path, capsys):
    repo = make_repo(tmp_path)
    adr = repo / "docs" / "decisions" / "0001-old.md"
    adr.write_text("# 0001 -- Old\n\nOriginal claim.\n")
    base = commit(repo, "add 0001")

    adr.write_text("# 0001 -- Old\n\nRewritten claim.\n")
    commit(repo, "silently rewrite 0001")

    assert check(repo, base) == 1
    assert "0001-old.md" in capsys.readouterr().out


def test_a_deleted_adr_fails(tmp_path):
    repo = make_repo(tmp_path)
    adr = repo / "docs" / "decisions" / "0001-old.md"
    adr.write_text("# 0001 -- Old\n\nOriginal claim.\n")
    base = commit(repo, "add 0001")

    adr.unlink()
    commit(repo, "delete 0001")

    assert check(repo, base) == 1


def test_the_decisions_index_is_not_covered(tmp_path):
    repo = make_repo(tmp_path)
    readme = repo / "docs" / "decisions" / "README.md"
    readme.write_text("# Index\n\n| 0001 | ... |\n")
    base = commit(repo, "add index")

    readme.write_text("# Index\n\n| 0002 | ... |\n")
    commit(repo, "replace the index row")

    assert check(repo, base) == 0


def test_a_pull_request_that_never_touches_decisions_is_unaffected(tmp_path):
    repo = make_repo(tmp_path)
    (repo / "README.md").write_text("hello\n")
    base = commit(repo, "init")

    (repo / "README.md").write_text("hello, updated\n")
    commit(repo, "unrelated change")

    assert check(repo, base) == 0


def test_bad_usage_exits_2():
    assert main(["prog"]) == 2
    assert main(["prog", "--base"]) == 2
    assert main(["prog", "--base", "x", "--extra"]) == 2
