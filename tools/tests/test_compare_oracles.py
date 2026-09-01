"""compare_oracles.py's own tests: the tolerance is not a licence to miss things.

A comparator that only ever passes is worse than the byte-for-byte diff it
replaces, so most of what is below is a difference that must still fail --
a string, an integer, a key, an array's length and order, a file. Only two
tests assert that something passes, and one of those is the whole point:
a float that moved by less than the tolerance the suites compare at.

The corpora are synthetic. The real ones under tests/oracles/ are 24 MB and
would make each case an exercise in finding the value it perturbed.
"""
import json
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import compare_oracles as comparator  # noqa: E402
from compare_oracles import main  # noqa: E402

# argv[0], unused by main() (only argv[1:] matters) -- a shared constant
# keeps python:S1192 quiet past three literal repeats.
PROG = "compare_oracles.py"

CORPUS = "metrics.json"
OTHER = "distances.json"
FIXTURE = "vocabulary.txt"

BASE = {"metadata": {"seed": 7}, "cases": [{"id": 1, "name": "edge", "score": 0.5}]}


def write(directory: Path, corpora: dict) -> Path:
    directory.mkdir()
    for name, content in corpora.items():
        text = content if isinstance(content, str) else json.dumps(content, indent=1)
        (directory / name).write_text(text, encoding="utf-8")
    return directory


def compare(tmp_path: Path, expected: dict, actual: dict) -> int:
    """main() over two directories built from two {filename: content} maps."""
    left = write(tmp_path / "expected", expected)
    right = write(tmp_path / "actual", actual)
    return main([PROG, str(left), str(right)])


def scored(score) -> dict:
    """BASE with its one float replaced -- the value most cases perturb."""
    return {"metadata": {"seed": 7}, "cases": [{"id": 1, "name": "edge", "score": score}]}


def test_identical_directories_pass(tmp_path):
    assert compare(tmp_path, {CORPUS: BASE}, {CORPUS: BASE}) == 0


def test_a_float_under_the_tolerance_passes(tmp_path):
    """The reason this script exists: host-dependent last bits are not a change."""
    assert compare(tmp_path, {CORPUS: scored(0.5)}, {CORPUS: scored(0.5 + 1e-12)}) == 0


def test_a_float_at_the_tolerance_passes(tmp_path):
    assert compare(tmp_path, {CORPUS: scored(0.5)}, {CORPUS: scored(0.5 + comparator.TOLERANCE)}) == 0


def test_a_float_over_the_tolerance_fails(tmp_path):
    assert compare(tmp_path, {CORPUS: scored(0.5)}, {CORPUS: scored(0.5 + 1e-6)}) == 1


def test_a_float_over_the_tolerance_names_its_json_path(tmp_path, capsys):
    compare(tmp_path, {CORPUS: scored(0.5)}, {CORPUS: scored(0.5 + 1e-6)})
    assert "metrics.json: cases[0].score" in capsys.readouterr().out


def test_a_changed_string_fails(tmp_path):
    changed = {"metadata": {"seed": 7}, "cases": [{"id": 1, "name": "middle", "score": 0.5}]}
    assert compare(tmp_path, {CORPUS: BASE}, {CORPUS: changed}) == 1


def test_a_changed_integer_fails(tmp_path):
    changed = {"metadata": {"seed": 7}, "cases": [{"id": 2, "name": "edge", "score": 0.5}]}
    assert compare(tmp_path, {CORPUS: BASE}, {CORPUS: changed}) == 1


def test_an_integer_where_a_float_was_fails(tmp_path):
    """1 and 1.0 compare equal in Python; the corpus committed one of the two."""
    assert compare(tmp_path, {CORPUS: scored(1.0)}, {CORPUS: scored(1)}) == 1


def test_a_boolean_where_an_integer_was_fails(tmp_path):
    """bool subclasses int, so True == 1 unless the kinds are checked first."""
    assert compare(tmp_path, {CORPUS: scored(1)}, {CORPUS: scored(True)}) == 1


def test_a_null_where_a_value_was_fails(tmp_path):
    assert compare(tmp_path, {CORPUS: scored(0.5)}, {CORPUS: scored(None)}) == 1


def test_an_added_key_fails(tmp_path):
    added = {"metadata": {"seed": 7, "revision": 2}, "cases": BASE["cases"]}
    assert compare(tmp_path, {CORPUS: BASE}, {CORPUS: added}) == 1


def test_a_removed_key_fails(tmp_path):
    removed = {"metadata": {}, "cases": BASE["cases"]}
    assert compare(tmp_path, {CORPUS: BASE}, {CORPUS: removed}) == 1


def test_a_removed_key_is_named_as_such(tmp_path, capsys):
    compare(tmp_path, {CORPUS: BASE}, {CORPUS: {"metadata": {}, "cases": BASE["cases"]}})
    assert "metrics.json: metadata.seed: in the expected corpus only" in capsys.readouterr().out


def test_reordered_keys_fail(tmp_path):
    """The generator's key order is fixed, so a reordering is a change to it."""
    forward = {CORPUS: {"a": 1, "b": 2}}
    backward = {CORPUS: {"b": 2, "a": 1}}
    assert compare(tmp_path, forward, backward) == 1


def test_a_reordered_array_fails(tmp_path):
    assert compare(tmp_path, {CORPUS: {"ids": [1, 2, 3]}}, {CORPUS: {"ids": [1, 3, 2]}}) == 1


def test_a_changed_array_length_fails(tmp_path):
    assert compare(tmp_path, {CORPUS: {"ids": [1, 2, 3]}}, {CORPUS: {"ids": [1, 2]}}) == 1


def test_a_longer_array_reports_the_length_and_not_only_the_values(tmp_path, capsys):
    compare(tmp_path, {CORPUS: {"ids": [1, 2]}}, {CORPUS: {"ids": [1, 2, 3]}})
    assert "2 elements vs 3" in capsys.readouterr().out


def test_an_added_file_fails(tmp_path):
    assert compare(tmp_path, {CORPUS: BASE}, {CORPUS: BASE, OTHER: BASE}) == 1


def test_a_removed_file_fails(tmp_path):
    assert compare(tmp_path, {CORPUS: BASE, OTHER: BASE}, {CORPUS: BASE}) == 1


def test_an_infinity_against_a_finite_value_fails(tmp_path):
    """No tolerance reaches an infinity, and none should be pretended to."""
    assert compare(tmp_path, {CORPUS: {"v": math.inf}}, {CORPUS: {"v": 1e308}}) == 1


def test_a_nan_against_a_finite_value_fails(tmp_path):
    assert compare(tmp_path, {CORPUS: {"v": math.nan}}, {CORPUS: {"v": 0.0}}) == 1


def test_two_nans_agree(tmp_path):
    """`==` says otherwise, and a corpus that spells NaN twice has not changed."""
    assert compare(tmp_path, {CORPUS: {"v": math.nan}}, {CORPUS: {"v": math.nan}}) == 0


def test_two_infinities_of_opposite_sign_fail(tmp_path):
    assert compare(tmp_path, {CORPUS: {"v": math.inf}}, {CORPUS: {"v": -math.inf}}) == 1


def test_a_reformatted_corpus_passes(tmp_path):
    """What byte-identity caught and this does not -- decision 0073 says so."""
    left = write(tmp_path / "expected", {CORPUS: BASE})
    right = tmp_path / "actual"
    right.mkdir()
    (right / CORPUS).write_text(json.dumps(BASE, indent=4), encoding="utf-8")
    assert main([PROG, str(left), str(right)]) == 0


def test_a_non_json_fixture_is_compared_byte_for_byte(tmp_path):
    assert compare(tmp_path, {FIXTURE: "a b\n"}, {FIXTURE: "a c\n"}) == 1


def test_an_identical_non_json_fixture_passes(tmp_path):
    assert compare(tmp_path, {FIXTURE: "a b\n"}, {FIXTURE: "a b\n"}) == 0


def test_a_corpus_that_will_not_parse_fails(tmp_path):
    assert compare(tmp_path, {CORPUS: "{not json"}, {CORPUS: "{not json"}) == 1


def test_a_missing_directory_is_bad_usage(tmp_path):
    left = write(tmp_path / "expected", {CORPUS: BASE})
    assert main([PROG, str(left), str(tmp_path / "absent")]) == 2


def test_one_argument_is_bad_usage(tmp_path):
    assert main([PROG, str(tmp_path)]) == 2


def test_help_exits_zero():
    assert main([PROG, "--help"]) == 0


def test_the_success_line_names_the_tolerance(tmp_path, capsys):
    compare(tmp_path, {CORPUS: BASE}, {CORPUS: BASE})
    assert "1e-09" in capsys.readouterr().out
