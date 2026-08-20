"""select_benchmarks.py's own tests: which files select which harness or class.

The one property worth guarding: bench/bench-map.json's "always" list holds
only files that genuinely affect every harness, not a whole directory that
happens to hold one file per harness. A regression there reintroduces #351 --
one line in a single cross-language file selecting all of them again.
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from select_benchmarks import select  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
MAP = json.loads((REPO / "bench" / "bench-map.json").read_text(encoding="utf-8"))


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
