"""The guard's own tests, written against the calls that were actually in the tree.

Ten Console calls under bench/ narrated a run and nobody was counting them; four
carry something no file does. Both halves are exercised here, on temporary files
rather than on the repository, so a later cleanup of the real ones cannot make
these pass by accident.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import check_no_console_writeline as guard  # noqa: E402

PROG = "check_no_console_writeline.py"


def _scan(tmp_path, monkeypatch, sources: dict[str, str]) -> tuple[int, str]:
    """Run main() over `sources` alone, and return its code with what it wrote."""
    for name, text in sources.items():
        target = tmp_path / name
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(text, encoding="utf-8")
    monkeypatch.setattr(guard, "ROOT", tmp_path)
    monkeypatch.setattr(guard, "tracked_sources", lambda: list(sources))
    return guard.main([PROG])


def test_a_shipped_package_may_not_print(tmp_path, monkeypatch, capsys):
    code = _scan(tmp_path, monkeypatch, {
        "src/Lodestar.Text/Thing.cs": "class Thing { void M() { Console.WriteLine(1); } }\n"})
    assert code == 1
    assert "src/Lodestar.Text/Thing.cs:1" in capsys.readouterr().err


def test_a_marker_does_not_excuse_a_shipped_package(tmp_path, monkeypatch, capsys):
    """There is no reason that survives review, so the marker is not read under src/."""
    code = _scan(tmp_path, monkeypatch, {
        "src/Lodestar.Text/Thing.cs":
            "// console-print: a reason someone believed\nConsole.WriteLine(1);\n"})
    assert code == 1


def test_an_unmarked_bench_call_is_refused(tmp_path, monkeypatch):
    code = _scan(tmp_path, monkeypatch, {
        "bench/Harness.cs": 'Console.WriteLine("C# persistence cross-lang bench");\n'})
    assert code == 1


def test_a_marked_bench_call_passes(tmp_path, monkeypatch):
    code = _scan(tmp_path, monkeypatch, {
        "bench/Program.cs":
            "// console-print: the wrong build was measured, so the numbers are a lie.\n"
            "Console.Error.WriteLine(x);\n"})
    assert code == 0


def test_a_trailing_marker_passes_too(tmp_path, monkeypatch):
    """A one-line call puts its reason on the line; a multi-line one cannot."""
    code = _scan(tmp_path, monkeypatch, {
        "bench/Program.cs": "Console.WriteLine(x); // console-print: why the cell is absent\n"})
    assert code == 0


def test_an_empty_marker_is_the_cheapest_rubber_stamp(tmp_path, monkeypatch):
    code = _scan(tmp_path, monkeypatch, {
        "bench/Program.cs": "// console-print:\nConsole.WriteLine(x);\n"})
    assert code == 1


def test_the_word_in_a_comment_is_not_a_call(tmp_path, monkeypatch):
    code = _scan(tmp_path, monkeypatch, {
        "bench/Program.cs": "// This used to Console.WriteLine the row as it landed.\n"})
    assert code == 0


def test_every_console_entry_point_is_caught(tmp_path, monkeypatch):
    """Write, WriteLine and the Error/Out properties, not WriteLine alone."""
    for call in ("Console.Write(x);", "Console.WriteLine(x);",
                 "Console.Error.Write(x);", "Console.Out.WriteLine(x);",
                 "Console . WriteLine (x);"):
        assert _scan(tmp_path, monkeypatch, {"bench/P.cs": call + "\n"}) == 1, call


def test_report_judges_nothing(tmp_path, monkeypatch, capsys):
    code = _scan(tmp_path, monkeypatch, {"bench/P.cs": "Console.WriteLine(x);\n"})
    assert code == 1
    monkeypatch.setattr(guard, "tracked_sources", lambda: ["bench/P.cs"])
    assert guard.main([PROG, "--report"]) == 0
    assert "0 marked, 1 unmarked" in capsys.readouterr().out


def test_an_unknown_argument_is_refused(tmp_path, monkeypatch):
    monkeypatch.setattr(guard, "ROOT", tmp_path)
    assert guard.main([PROG, "--fix"]) == 2
