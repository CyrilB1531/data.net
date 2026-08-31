#!/usr/bin/env python3
"""The interpreter floor this repository's generators require, in one place.

The floor is the interpreter CI runs, not the oldest one the syntax happens to
parse under. The corpora under ``tests/oracles/`` are these generators' output,
and the ``Oracles are reproducible`` job diffs them against a fresh generation,
so a contributor regenerating under an interpreter CI never runs would reach
that job as an unexplained drift on an unrelated change. Decision 0065 has the
option that lost.

``tools/tests/test_python_floor.py`` asserts that every ``python-version:`` pin
under ``.github/workflows/`` still equals the constant below: the refusal
message promises the floor is the CI interpreter, and a pin drifting away from
it would make that promise false.
"""

from __future__ import annotations

import sys

MINIMUM_PYTHON = (3, 12)


def _spell(version: tuple[int, ...]) -> str:
    return ".".join(str(part) for part in version)


def require_supported_python(script: str) -> None:
    """Exit with a sentence when the running interpreter is below the floor.

    Call this *before* importing anything written in syntax only the floor
    accepts. Otherwise the parser fails first and the contributor gets a
    ``SyntaxError`` in a file they never opened rather than a version
    (issue #486).
    """
    if sys.version_info >= MINIMUM_PYTHON:
        return
    raise SystemExit(
        f"{script} needs Python {_spell(MINIMUM_PYTHON)} or later; "
        f"this interpreter is {_spell(sys.version_info[:3])} ({sys.executable}). "
        "CONTRIBUTING.md's 'Oracle validation' has the virtualenv's creation step."
    )
