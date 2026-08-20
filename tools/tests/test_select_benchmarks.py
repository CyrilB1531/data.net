"""select_benchmarks.py's own tests: which files select what, and a baseline
the diff can and cannot read.

Two properties worth guarding. bench/bench-map.json's "always" list holds only
files that genuinely affect every harness, not a whole directory that happens
to hold one file per harness -- a regression there reintroduces #351, one line
in a single cross-language file selecting all of them again. An absent
baseline and an unresolvable one must both measure everything -- the same
ignorance, read the same safe way. Before #354 they disagreed, and the
unresolvable case did so silently: a rebase orphans the SHA the previous page
recorded, and an empty diff on it looked exactly like "nothing changed".
"""
import json
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from select_benchmarks import resolves, select  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
MAP = json.loads((REPO / "bench" / "bench-map.json").read_text(encoding="utf-8"))
SCRIPT = REPO / "tools" / "select_benchmarks.py"

ORPHAN_SHA = "0123456789abcdef0123456789abcdef01234567"


def test_a_per_harness_crosslang_file_selects_only_its_own_harness():
    files = ["bench/Lodestar.Text.Benchmarks/CrossLang/PersistenceCrossLang.cs"]
    assert select(MAP, files, "harnesses") == ["persistence"]
    assert select(MAP, files, "benchmarks") == []


def test_the_shared_harness_base_still_selects_everything():
    files = ["bench/Lodestar.Text.Benchmarks/CrossLang/Harness.cs"]
    assert select(MAP, files, "harnesses") == sorted(MAP["harnesses"])
    assert select(MAP, files, "benchmarks") == sorted(MAP["benchmarks"])


def test_pairs_harness_is_shared_by_indel_and_levenshtein_only():
    files = ["bench/Lodestar.Text.Benchmarks/CrossLang/PairsHarness.cs"]
    assert select(MAP, files, "harnesses") == ["indel", "levenshtein"]
    assert select(MAP, files, "benchmarks") == []


def run(*args: str) -> subprocess.CompletedProcess:
    return subprocess.run(
        [sys.executable, str(SCRIPT), *args],
        cwd=REPO, capture_output=True, text=True, check=False)


def test_resolves_is_true_for_a_real_commit():
    assert resolves("HEAD")


def test_resolves_is_false_for_a_well_formed_but_orphaned_sha():
    assert not resolves(ORPHAN_SHA)


def test_resolves_refuses_a_leading_dash_itself_rather_than_trust_the_caller():
    assert not resolves("-rf")


def test_an_orphaned_baseline_measures_everything_like_an_absent_one():
    orphaned = run("--since", ORPHAN_SHA)
    absent = run()
    assert orphaned.returncode == 0
    assert sorted(orphaned.stdout.splitlines()) == sorted(absent.stdout.splitlines())
    assert orphaned.stdout.splitlines() != []


def test_an_orphaned_baseline_says_so_on_stderr():
    result = run("--since", ORPHAN_SHA)
    assert ORPHAN_SHA in result.stderr
    assert "does not resolve" in result.stderr


def test_a_malformed_baseline_is_still_refused_not_measured_as_everything():
    # --since=-rf, not two argv items: argparse itself would read a bare "-rf"
    # as an unrecognised flag rather than pass it through to be checked here.
    result = run("--since=-rf")
    assert result.stdout == ""
