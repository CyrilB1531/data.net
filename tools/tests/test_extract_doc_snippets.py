"""The extractor's tests, over the markers a reference entry uses.

The guides' behaviour is pinned here too: a plain `//` comment must stay a
comment. Turning the ~40 existing ones into assertions would be a silent
change of meaning across five documents.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import extract_doc_snippets as extractor  # noqa: E402


def render(text: str, relative: str = "docs/reference/text/distances.md") -> str:
    path = extractor.REPO / relative
    document, _compiled, _skipped = extractor.render(path, text)
    return document


def test_a_plain_comment_stays_a_comment():
    document = render('```csharp\nint d = Levenshtein.Distance("a", "b");  // 1\n```\n')
    assert "SnippetAssert" not in document
    assert "// 1" in document


def test_an_arrow_comment_becomes_an_assertion():
    document = render('```csharp\nint d = Levenshtein.Distance("kitten", "sitting");  // => 3\n```\n')
    assert 'SnippetAssert.Value(d, "3", "docs/reference/text/distances.md:2");' in document


def test_the_arrow_keeps_the_statement_that_produced_the_value():
    document = render('```csharp\nint d = Levenshtein.Distance("a", "b");  // => 1\n```\n')
    assert 'int d = Levenshtein.Distance("a", "b");' in document


def test_var_declarations_are_asserted_too():
    document = render('```csharp\nvar d = Levenshtein.Distance("a", "b");  // => 1\n```\n')
    assert 'SnippetAssert.Value(d, "1", ' in document


def test_an_arrow_on_a_line_that_binds_nothing_is_an_error():
    text = '```csharp\nLevenshtein.Distance("a", "b");  // => 1\n```\n'
    failures = extractor.arrow_failures(extractor.REPO / "docs/reference/text/distances.md", text)
    assert failures and "bind the value to a variable" in failures[0]


def test_a_declaration_fence_is_not_compiled():
    text = (
        "<!-- docs-declaration -->\n\n"
        "```csharp\npublic static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b)\n```\n"
    )
    assert render(text) == ""


def test_a_run_skip_marker_becomes_an_attribute():
    text = (
        "<!-- docs-run: skip - writes a file -->\n"
        '```csharp\nvectorizer.Save("model.json");\n```\n'
    )
    assert '[SnippetSkipRun("writes a file")]' in render(text)


def test_a_reference_page_lands_in_the_reference_namespace():
    document = render('```csharp\nint d = 1;\n```\n')
    assert "namespace DataNet.DocSnippets.Reference;" in document
    assert "class TextDistances" in document


def test_a_guide_keeps_its_namespace_and_its_class_name():
    document = render('```csharp\nint d = 1;\n```\n', relative="docs/guides/quickstart.md")
    assert "namespace DataNet.DocSnippets;" in document
    assert "class Quickstart" in document
