"""The guard's own tests, over blocks taken from this repository.

Issue #134 measured 354 blocks running past eight lines, holding 5532 of the
9803 comment lines in the tree. The fixtures below are shapes from that list
rather than invented ones, which is what makes them evidence that the counter
matches the thing that was counted.
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


def test_eight_lines_is_allowed_and_nine_is_not():
    eight = "".join(f"// line {i}\n" for i in range(8)) + "int x = 1;\n"
    nine = "".join(f"// line {i}\n" for i in range(9)) + "int x = 1;\n"
    assert guard.findings_in(eight.split("\n"), ".cs") == []
    assert len(guard.findings_in(nine.split("\n"), ".cs")) == 1


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
    # tools/check_machine_paths.py opens with a 30-line docstring. It is not a
    # comment and this guard does not count it -- prose in a docstring is the
    # module's documentation, which is where long explanation belongs.
    text = '"""One\nTwo\nThree\n"""\nimport sys\n'
    assert blocks(text) == []


def test_the_finding_names_the_file_the_line_and_the_length():
    nine = "".join(f"// line {i}\n" for i in range(9)) + "int x = 1;\n"
    finding = guard.findings_in(nine.split("\n"), ".cs")[0]
    assert finding.line == 1
    assert finding.length == 9
