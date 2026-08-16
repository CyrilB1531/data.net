"""The guard's own tests, holding the shapes that actually reached this repository.

Issue #205 exists because 88 interpolated holes formatted through CurrentCulture
and nothing caught them for two releases: CA1305 does not reach the syntax, and
the packaging gate checks reachability rather than output. The fixtures below are
the shapes that were really there -- a bare {x:F3}, a call expression carrying a
named argument whose colon is not the format's, and a hole with no specifier at
all -- rather than examples someone invented.

The division of labour is the thing worth pinning: the scan catches holes with a
format specifier, and Program.cs pinning the thread culture catches the rest. A
test for each, because a change that dropped either would still pass the other.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import check_sample_culture as guard  # noqa: E402

PROG = "check_sample_culture.py"

PINNED = (
    "using System.Globalization;\n"
    "CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;\n"
)


def lay_out(tmp_path, monkeypatch, sources, *, entry_point=PINNED):
    """A repository holding only what the guard reads, with git kept out of it."""
    monkeypatch.setattr(guard, "ROOT", tmp_path)
    written = []
    for name, text in sources.items():
        path = tmp_path / guard.SAMPLE / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
        written.append(f"{guard.SAMPLE}/{name}")

    entry = tmp_path / guard.ENTRY_POINT
    entry.parent.mkdir(parents=True, exist_ok=True)
    entry.write_text(entry_point, encoding="utf-8")
    written.append(guard.ENTRY_POINT)

    monkeypatch.setattr(guard, "tracked_sample_sources", lambda: written)
    return written


def test_a_pinned_tree_with_no_formatted_hole_passes(tmp_path, monkeypatch, capsys):
    lay_out(tmp_path, monkeypatch, {
        "Lot1.cs": 'Console.WriteLine($"  score = {Inv.F3(value)}");\n',
    })

    exit_code = guard.main([PROG])

    assert exit_code == 0
    assert "ok" in capsys.readouterr().out


def test_a_formatted_hole_is_a_finding_naming_its_line(tmp_path, monkeypatch, capsys):
    lay_out(tmp_path, monkeypatch, {
        "Lot1.cs": "\n" + 'Console.WriteLine($"  score = {value:F3}");\n',
    })

    exit_code = guard.main([PROG])
    captured = capsys.readouterr().out

    assert exit_code == 1
    assert "Lot1.cs:2" in captured
    assert "{value:F3}" in captured


def test_a_named_argument_inside_the_expression_does_not_end_the_hole(tmp_path, monkeypatch, capsys):
    # The colon in "normalize: false" is not the format separator, and a guard
    # that split on the first one would report the wrong text or miss the hole.
    lay_out(tmp_path, monkeypatch, {
        "Lot5.cs": 'W($"{Accuracy.Score(cm, normalize: false):F0}");\n',
    })

    exit_code = guard.main([PROG])

    assert exit_code == 1
    assert "{Accuracy.Score(cm, normalize: false):F0}" in capsys.readouterr().out


def test_an_aligned_hole_is_reported_and_said_to_be_aligned(tmp_path, monkeypatch, capsys):
    # None exist today. If one is ever written, rewriting it to Inv.F3(expr)
    # has to keep the alignment, so the finding says which kind it is.
    lay_out(tmp_path, monkeypatch, {"Lot1.cs": 'W($"{value,10:F3}");\n'})

    exit_code = guard.main([PROG])

    assert exit_code == 1
    assert "aligned interpolated hole" in capsys.readouterr().out


def test_a_hole_with_no_format_specifier_is_left_to_the_pinned_culture(tmp_path, monkeypatch, capsys):
    # {count} and {ratio} are one syntax and two risks, which Program.cs covers
    # and no scan can: reporting this would make every string a finding.
    lay_out(tmp_path, monkeypatch, {"Lot1.cs": 'W($"{count} items, {ratio}");\n'})

    exit_code = guard.main([PROG])

    assert exit_code == 0
    assert "ok" in capsys.readouterr().out


def test_an_unpinned_entry_point_is_a_finding_even_with_no_holes(tmp_path, monkeypatch, capsys):
    lay_out(tmp_path, monkeypatch,
            {"Lot1.cs": 'W($"{Inv.F3(value)}");\n'},
            entry_point="Console.WriteLine(\"hello\");\n")

    exit_code = guard.main([PROG])
    captured = capsys.readouterr().out

    assert exit_code == 1
    assert "DefaultThreadCurrentCulture" in captured


def test_the_pin_is_matched_on_the_assignment_not_on_its_comment(tmp_path, monkeypatch, capsys):
    # Rewording the comment above the assignment must not fail the build.
    lay_out(tmp_path, monkeypatch,
            {"Lot1.cs": "// nothing\n"},
            entry_point="// any words at all\n"
                        "CultureInfo.DefaultThreadCurrentCulture   =   CultureInfo.InvariantCulture;\n")

    assert guard.main([PROG]) == 0
    assert "ok" in capsys.readouterr().out


def test_no_tracked_source_is_bad_shape_rather_than_a_clean_tree(tmp_path, monkeypatch, capsys):
    # An empty list means the scan looked at nothing, which must never read as ok.
    monkeypatch.setattr(guard, "ROOT", tmp_path)
    monkeypatch.setattr(guard, "tracked_sample_sources", lambda: [])

    exit_code = guard.main([PROG])

    assert exit_code == 2
    assert capsys.readouterr().err


def test_a_missing_entry_point_is_bad_shape_rather_than_a_finding(tmp_path, monkeypatch, capsys):
    # Sources present so the empty-list branch above cannot be what returns 2.
    monkeypatch.setattr(guard, "ROOT", tmp_path)
    source = tmp_path / guard.SAMPLE / "Lot1.cs"
    source.parent.mkdir(parents=True, exist_ok=True)
    source.write_text("// nothing\n", encoding="utf-8")
    monkeypatch.setattr(guard, "tracked_sample_sources",
                        lambda: [f"{guard.SAMPLE}/Lot1.cs"])

    exit_code = guard.main([PROG])

    assert exit_code == 2
    assert "Program.cs" in capsys.readouterr().err


def test_an_unrecognised_argument_is_bad_usage_on_stderr(capsys):
    exit_code = guard.main([PROG, "--nonsense"])
    captured = capsys.readouterr()

    assert exit_code == 2
    assert not captured.out
    assert captured.err


def test_the_sources_come_from_git_rather_than_a_glob():
    # bin/ and obj/ hold copies of every sample source, and editing those would
    # turn the guard green while the files that ship still printed in fr-FR.
    listed = guard.tracked_sample_sources()

    assert listed, "the real repository should track sample sources"
    assert all(part not in ("bin", "obj")
               for path in listed for part in path.split("/"))

def test_a_hole_whose_expression_holds_braces_is_still_found(tmp_path, monkeypatch, capsys):
    # The shape a first sweep missed: an object initializer in an argument list
    # puts balanced braces inside the hole, which no single regex can span.
    lay_out(tmp_path, monkeypatch, {
        "Lot5.cs": 'W($"{RocAuc.MultiClass(t, p, new Options { Average = a }):F3}");\n',
    })

    exit_code = guard.main([PROG])

    assert exit_code == 1
    assert "new Options { Average = a }):F3}" in capsys.readouterr().out


def test_json_in_a_string_literal_is_not_mistaken_for_a_hole(tmp_path, monkeypatch, capsys):
    # The sample embeds vocabularies as JSON. ":10}" there is data, and a guard
    # that flagged it would report findings nobody can act on.
    lay_out(tmp_path, monkeypatch, {
        "Lot3.cs": 'Utf8("""{"a":0,"b":1,"ke":10}""");\n',
    })

    assert guard.main([PROG]) == 0
    assert "ok" in capsys.readouterr().out
