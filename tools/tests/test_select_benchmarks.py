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

A third, added with the "dispatch_only" exemption (#465): every way of failing to
find the exempted construct has to fall to "not exempt", because the exemption is
the one place this tool can under-include. The case that did not was the keyword
appearing in a comment first (#480), which made the next braced block the exempt
region and hid a change to real code.
"""
import json
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import select_benchmarks  # noqa: E402
from select_benchmarks import dispatch_only, resolves, select, strip_construct  # noqa: E402

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


PROGRAM_PATH = "bench/Lodestar.Text.Benchmarks/Program.cs"

# Program.cs's shape, not its contents: the header comment holding the word "switch"
# above the statement, and one line of real code below the block.
PROGRAM = """using BenchmarkDotNet.Running;

// long-comment: the entry points, and why the chain of ifs became a switch.
switch (args.Length > 0 ? args[0] : string.Empty)
{
    case "compare":
        LevenshteinCrossLang.Run(args);
        return;
    case "":
        break;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
"""

ADDED_ARM = """    case "ingest-phases":
        IngestPhasesBench.Run();
        return;
    case "":
"""

# The word in a comment and no statement at all, with a braced block after it: what
# the old anchor exempted, and the reason the anchor is now the statement.
COMMENT_ONLY = """using BenchmarkDotNet.Running;

// why the chain of ifs became a switch.
if (args.Length == 0) { Log(1); }

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
"""


def exempt(monkeypatch, before: str, after: str) -> bool:
    """dispatch_only's verdict on two revisions handed to it as text rather than as commits."""
    sources = {"before": before, "after": after}
    monkeypatch.setattr(select_benchmarks, "file_at", lambda rev, path: sources[rev])
    data = {"dispatch_only": {PROGRAM_PATH: "switch"}}
    return dispatch_only(data, PROGRAM_PATH, "before", "after")


def test_adding_a_subcommand_leaves_program_cs_exempt(monkeypatch):
    assert exempt(monkeypatch, PROGRAM, PROGRAM.replace('    case "":\n', ADDED_ARM))


def test_a_subcommand_and_a_line_outside_the_switch_is_not_exempt(monkeypatch):
    after = PROGRAM.replace('    case "":\n', ADDED_ARM).replace(
        "BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);",
        "BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAll(args);")
    assert not exempt(monkeypatch, PROGRAM, after)


def test_a_file_that_has_stopped_having_the_construct_is_not_exempt(monkeypatch):
    without = PROGRAM[:PROGRAM.index("// long-comment")] + PROGRAM[PROGRAM.index("BenchmarkSwitcher"):]
    assert strip_construct(without, "switch") is None
    assert not exempt(monkeypatch, PROGRAM, without)


def test_the_keyword_in_a_comment_does_not_exempt_the_block_below_it(monkeypatch):
    assert strip_construct(COMMENT_ONLY, "switch") is None
    changed = COMMENT_ONLY.replace("Log(1)", "Log(2)")
    assert not exempt(monkeypatch, COMMENT_ONLY, changed)


def test_a_second_switch_is_ambiguous_and_so_not_exempt(monkeypatch):
    two = PROGRAM + "\nswitch (args.Length)\n{\n    default:\n        break;\n}\n"
    assert strip_construct(two, "switch") is None
    assert not exempt(monkeypatch, two, two.replace("args.Length)", "args.Length + 1)"))


def test_a_file_absent_at_one_revision_is_not_exempt(monkeypatch):
    monkeypatch.setattr(select_benchmarks, "file_at", lambda rev, path: None)
    assert not dispatch_only({"dispatch_only": {PROGRAM_PATH: "switch"}},
                             PROGRAM_PATH, "before", "after")
