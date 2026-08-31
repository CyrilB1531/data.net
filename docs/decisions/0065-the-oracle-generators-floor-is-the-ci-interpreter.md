# 0065 — The oracle generators' Python floor is the CI interpreter, not the oldest one that parses

**Status:** accepted · **Date:** 2026-08-31

## Context

`tools/seeded_random.py:39` spells one method with PEP 695 type-parameter syntax, which parses on
Python 3.12 and later only:

```python
def choice[T](self, seq: Sequence[T]) -> T:
```

Four entry points import it — `tools/generate_oracles.py` and the three `bench/corpus/generate_*.py`
— so on 3.10 or 3.11 each of them dies with a `SyntaxError` in a file the contributor did not open,
carrying no version number.
[#486](https://github.com/CyrilB1531/lodestar/issues/486) found it on this project's own hosted
session image, where `python3` is 3.11.15, and asked the question the fix cannot dodge: raise the
floor deliberately, or lower it by rewriting the one signature?

Lowering it is cheap. `def choice[T]` is the only PEP 695 site in the repository, and a `TypeVar`
replaces it in three lines. So the syntax does not decide this.

## Decision

**The floor is the interpreter CI runs — 3.12 — held as one constant in `tools/python_floor.py`,
and the generators refuse anything below it with a sentence.**

What decides it is that the committed corpora under `tests/oracles/` **are** these generators'
output, and the `Oracles are reproducible` job diffs them against a fresh generation on every pull
request. A contributor regenerating under 3.10 regenerates under an interpreter no workflow runs;
any value that moved with the interpreter would land on that job as an unexplained drift, on a
change that has nothing to do with it. That is the failure mode the exact pins in
`tools/requirements.txt` exist to prevent — "under a range, a silent minor release would land as
an unexplained oracle diff on an unrelated change" — reintroduced one layer below the libraries.

It is a floor rather than a range for the same reason: one interpreter regenerates the corpora,
not a family of them.

`require_supported_python()` is called before the `seeded_random` import in each of the four entry
points, never after. All four parse under 3.11 today, which is the only thing that makes the guard
reachable at all; placed after the import, the parser would fail first and the message would be
the `SyntaxError` again.

## Options refused

**Lower the floor to 3.10 by rewriting `seeded_random.py`.** It would widen who can regenerate, at
the cost of widening what they can regenerate *differently* — and 3.10 is not a floor anyone
verified either, merely the oldest the remaining syntax happens to accept. It also spends the
3.12 syntax the rest of `tools/` may want next, buying a compatibility the project does not test.

**Guard only `tools/generate_oracles.py`, the script #486 names.** The three bench generators fail
identically and for the same reason; #486 names one because that is where it was found, not
because the others are sound.

**State the floor in `CONTRIBUTING.md` alone.** Prose does not fail a build. A contributor who did
not read the section is exactly the contributor who hits the `SyntaxError`.

**Let `check_version_floor.py` hold the number.** Its subject is the three `Lodestar.Text` version
numbers that must agree; a Python interpreter pin is another document's fact.
`tools/tests/test_python_floor.py` asserts instead that every `python-version:` pin under
`.github/workflows/` equals the constant — the refusal message promises the floor is the CI
interpreter, and that test is what keeps the promise true.

## Consequences

- One number to move when CI's pin moves, and a test that fails until both agree.
- An interpreter below the floor now costs a sentence naming both versions, not a search through
  a helper module.
- `seeded_random.py` keeps its PEP 695 signature, and `tools/` may use 3.12 syntax freely.
- Nothing in the guard runs on a supported interpreter beyond one `sys.version_info` comparison,
  so no corpus changes — which `Oracles are reproducible` proves by staying green with no diff.
