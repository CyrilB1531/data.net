"""A section stops at the next heading, not only at the next section of its own level."""

import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent.parent))

import render_benchmark_latest as renderer  # noqa: E402


BODY = """### alpha

text A

## Interloper

blurb

### beta

text B
"""


def test_a_higher_level_heading_ends_the_section():
    """#356: swallowing it re-emitted it under whichever method preceded one.

    Two identical headings is what markdownlint's MD024 refuses, and the page this
    script writes is the one the Lint job then failed -- on content no author wrote.
    """
    found = renderer.sections(BODY)

    assert found["alpha"][1] == "text A"
    assert "Interloper" not in found["alpha"][1]


def test_the_sections_themselves_are_still_found():
    found = renderer.sections(BODY)

    assert sorted(found) == ["alpha", "beta"]
    assert found["beta"][1] == "text B"
