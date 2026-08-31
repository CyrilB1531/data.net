"""The interpreter floor refuses what it says it refuses, and matches CI.

Issue #486: `tools/seeded_random.py` uses PEP 695 syntax, so an interpreter
below the floor failed with a `SyntaxError` in a file the contributor never
opened. `require_supported_python` turns that into a sentence, and the pin
check below keeps the sentence true.
"""

from __future__ import annotations

import pathlib
import re
import sys

import pytest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))

from python_floor import (  # noqa: E402
    MINIMUM_PYTHON,
    require_supported_python,
)

WORKFLOWS = pathlib.Path(__file__).resolve().parents[2] / ".github" / "workflows"
PIN = re.compile(r"^\s*python-version:\s*'([^']+)'\s*$", re.MULTILINE)

SCRIPT = "tools/generate_oracles.py"


def _at(version, monkeypatch):
    monkeypatch.setattr(sys, "version_info", version)


def test_an_interpreter_at_the_floor_passes(monkeypatch):
    _at((*MINIMUM_PYTHON, 0, "final", 0), monkeypatch)
    require_supported_python(SCRIPT)


def test_an_interpreter_above_the_floor_passes(monkeypatch):
    _at((MINIMUM_PYTHON[0], MINIMUM_PYTHON[1] + 3, 1, "final", 0), monkeypatch)
    require_supported_python(SCRIPT)


def test_the_minor_below_the_floor_is_refused(monkeypatch):
    _at((MINIMUM_PYTHON[0], MINIMUM_PYTHON[1] - 1, 15, "final", 0), monkeypatch)
    with pytest.raises(SystemExit) as refusal:
        require_supported_python(SCRIPT)
    assert SCRIPT in str(refusal.value)


def test_the_refusal_names_both_versions(monkeypatch):
    _at((3, 10, 12, "final", 0), monkeypatch)
    with pytest.raises(SystemExit) as refusal:
        require_supported_python(SCRIPT)
    message = str(refusal.value)
    assert "3.12" in message, message
    assert "3.10.12" in message, message


def test_a_major_below_the_floor_is_refused(monkeypatch):
    _at((2, 7, 18, "final", 0), monkeypatch)
    with pytest.raises(SystemExit):
        require_supported_python(SCRIPT)


def test_every_workflow_pin_equals_the_floor():
    floor = ".".join(str(part) for part in MINIMUM_PYTHON)
    pins = {
        (path.name, pin)
        for path in sorted(WORKFLOWS.glob("*.yml"))
        for pin in PIN.findall(path.read_text(encoding="utf-8"))
    }
    assert pins, "no python-version pin found; the regex or the layout moved"
    assert {pin for _, pin in pins} == {floor}, sorted(pins)
