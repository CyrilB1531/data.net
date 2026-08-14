"""The guard's own tests, over the shapes the counter has to get right.

Issue #134 measured 354 C# blocks running past eight lines, holding 5532 of
the 9803 comment lines in C# files -- the figure that set the documentation
budget before it was split from the inline one. These fixtures are minimal
constructions rather than excerpts: what needs pinning is the boundary
between one block and two, and a real 34-line block exercises no boundary a
three-line one does not.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import check_comment_length as guard  # noqa: E402


def blocks(text):
    return guard.blocks_in(text.split("\n"), ".cs")


def test_a_run_of_comment_lines_is_one_block():
    text = "// one\n// two\n// three\nint x = 1;\n"
    assert [b.length for b in blocks(text)] == [3]


def test_a_blank_line_ends_a_block():
    # Where a naive counter is wrong: two paragraphs of prose are two blocks,
    # and neither is over the threshold even though together they would be.
    text = "// one\n// two\n\n// three\n// four\nint x = 1;\n"
    assert [b.length for b in blocks(text)] == [2, 2]


def test_xml_documentation_counts_as_a_comment():
    text = "/// <summary>a</summary>\n/// <remarks>b</remarks>\nint X;\n"
    assert [b.length for b in blocks(text)] == [2]


def test_an_inline_block_gets_two_lines_and_a_third_is_refused():
    # An inline comment stands between a reader and the code, so it is a
    # sentence. Measured when the budget was set: 446 blocks ran past two.
    two = "// one\n// two\nint x = 1;\n"
    three = "// one\n// two\n// three\nint x = 1;\n"
    assert guard.findings_in(two.split("\n"), ".cs") == []
    assert len(guard.findings_in(three.split("\n"), ".cs")) == 1


def test_documentation_gets_eight_lines_of_prose():
    eight = "".join(f"/// line {i}\n" for i in range(8)) + "int X;\n"
    nine = "".join(f"/// line {i}\n" for i in range(9)) + "int X;\n"
    assert guard.findings_in(eight.split("\n"), ".cs") == []
    assert len(guard.findings_in(nine.split("\n"), ".cs")) == 1


def test_structural_elements_do_not_spend_the_documentation_budget():
    # A well-formed member carries <summary>, a <param> each and an
    # <exception>; counting those put 316 of 354 over-length blocks inside
    # public API documentation, which CLAUDE.md requires. Only prose counts.
    block = (
        "/// <summary>\n" + "".join(f"/// prose {i}\n" for i in range(7)) +
        "/// </summary>\n/// <param name=\"a\">a</param>\n"
        "/// <param name=\"b\">b</param>\n/// <exception cref=\"E\">e</exception>\n"
        "int X;\n")
    assert guard.findings_in(block.split("\n"), ".cs") == []


def test_python_comments_are_inline_and_get_two():
    three = "# one\n# two\n# three\nx = 1\n"
    assert len(guard.findings_in(three.split("\n"), ".py")) == 1


def test_a_marked_block_is_allowed_however_long():
    text = "// long-comment: the four measured rows\n" + "".join(
        f"// line {i}\n" for i in range(20)) + "int x = 1;\n"
    assert guard.findings_in(text.split("\n"), ".cs") == []


def test_the_marker_must_be_the_first_line_of_the_block():
    # Buried in the middle it is prose, not a marker, and the block is unmarked.
    text = "// one\n// long-comment: too late\n" + "".join(
        f"// line {i}\n" for i in range(9)) + "int x = 1;\n"
    assert len(guard.findings_in(text.split("\n"), ".cs")) == 1


def test_python_uses_the_same_marker_after_its_own_leader():
    text = "# long-comment: nltk's import refusal\n" + "".join(
        f"# line {i}\n" for i in range(12)) + "x = 1\n"
    assert guard.findings_in(text.split("\n"), ".py") == []


def test_a_shebang_and_a_coding_line_are_not_a_comment_block():
    # Every tool in tools/ opens with these two, and counting them would make
    # each file start one line into a block it never wrote.
    text = "#!/usr/bin/env python3\n# -*- coding: utf-8 -*-\nimport sys\n"
    assert blocks(text) == []


def test_a_python_docstring_is_not_a_comment_block():
    # Prose in a docstring is the module's documentation, which is where a long
    # explanation belongs -- check_machine_paths.py opens with thirty lines of it.
    text = '"""One\nTwo\nThree\n"""\nimport sys\n'
    assert blocks(text) == []


def test_the_finding_names_the_line_and_the_prose_count():
    nine = "".join(f"// line {i}\n" for i in range(9)) + "int x = 1;\n"
    finding = guard.findings_in(nine.split("\n"), ".cs")[0]
    assert finding.line == 1
    assert finding.length == 9


def test_a_marker_with_no_reason_is_not_a_marker():
    # The cheapest possible rubber stamp, and the one a hurried author reaches
    # for. The reason is what a review judges; without it there is nothing to.
    text = "// long-comment:\n// one\n// two\nint x = 1;\n"
    assert len(guard.findings_in(text.split("\n"), ".cs")) == 1


def test_the_marker_is_case_insensitive():
    text = "// LONG-COMMENT: shouting is still a reason\n// one\n// two\nint x = 1;\n"
    assert guard.findings_in(text.split("\n"), ".cs") == []


def test_tracked_files_is_independent_of_the_process_cwd():
    # check_machine_paths.py shipped this bug and fixed it: run from tools/ it
    # scanned 20 of 533 files and exited clean.
    import os
    here = os.getcwd()
    try:
        os.chdir(Path(__file__).resolve().parent)
        from_tools = guard.tracked_files()
    finally:
        os.chdir(here)
    assert len(from_tools) == len(guard.tracked_files()) > 100


def test_help_goes_to_stdout_and_exits_zero(capsys):
    assert guard.main(["check_comment_length.py", "--help"]) == 0
    assert "Usage" in capsys.readouterr().out


def test_a_bad_argument_exits_two():
    assert guard.main(["check_comment_length.py", "--nonsense"]) == 2


def test_report_exits_zero_even_with_findings(capsys):
    assert guard.main(["check_comment_length.py", "--report"]) == 0
    assert "comment blocks" in capsys.readouterr().out


def test_a_reason_above_a_pragma_is_not_counted():
    # See _justifies_a_suppression's own docstring for why this is exempt.
    text = ("// S1244: whether the variance collapsed at all, not whether two\n"
            "// computed quantities are close. scikit-learn tests the same\n"
            "// quantity against exact zero.\n"
            "#pragma warning disable S1244\n"
            "if (x != 0.0) { }\n")
    assert guard.findings_in(text.split("\n"), ".cs") == []


def test_a_long_block_not_above_a_pragma_is_still_counted():
    text = ("// one\n// two\n// three\n"
            "int x = 1;\n")
    assert len(guard.findings_in(text.split("\n"), ".cs")) == 1


def test_documentation_above_a_pragma_keeps_its_own_budget():
    # See _close()'s docstring for the // vs /// split; before this, 25 lines
    # of <remarks> escaped whenever a pragma happened to follow.
    text = ("".join(f"/// prose {i}\n" for i in range(12)) +
            "// S1244: the reason the suppression needs\n"
            "#pragma warning disable S1244\n"
            "int x = 1;\n")
    findings = guard.findings_in(text.split("\n"), ".cs")
    assert len(findings) == 1
    assert findings[0].length == 12


def test_a_pragma_reason_alone_is_still_exempt():
    text = ("// S1244: three lines of reason, which the suppression rule\n"
            "// requires and which this budget does not govern, so nothing\n"
            "// is reported here.\n"
            "#pragma warning disable S1244\n"
            "int x = 1;\n")
    assert guard.findings_in(text.split("\n"), ".cs") == []
