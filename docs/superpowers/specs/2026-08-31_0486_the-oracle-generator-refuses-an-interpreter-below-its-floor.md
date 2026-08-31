# 0486 — The oracle generator refuses an interpreter below its floor, and says which one it needs

**Issue:** [#486](https://github.com/CyrilB1531/lodestar/issues/486) ·
**Status:** accepted · **Date:** 2026-08-31

## Problem

The command `CONTRIBUTING.md`'s *Oracle validation* documents — and that `CLAUDE.md` repeats —
exits 1 before generating anything when `.venv-oracles` was built with an interpreter below 3.12.
Measured on this project's own hosted session image, where `python3` is 3.11.15:

```text
$ python3 -c "import ast; ast.parse(open('tools/seeded_random.py').read())"
  File "<unknown>", line 39
    def choice[T](self, seq: Sequence[T]) -> T:
              ^
SyntaxError: expected '('
```

`tools/seeded_random.py:39` uses PEP 695 type-parameter syntax, which parses on 3.12 and later
only. Three facts make this worse than a version mismatch usually is:

1. **The message names a file the contributor did not open.** They ran `generate_oracles.py`; the
   error is a syntax error in a helper two imports away, with no version in it.
2. **`CONTRIBUTING.md` names `.venv-oracles` but never says how to build it.** A contributor on
   Ubuntu 22.04 (`python3` is 3.10) or on the hosted image (3.11) reaches for the platform
   interpreter, because nothing told them not to.
3. **It compounds the trap documented three lines above it.** `python … | tail` reports `tail`'s
   status, so the failed generation looks successful — and the drift check that follows then
   proves nothing, because nothing was regenerated.

CI never sees any of it: every workflow pins `python-version: '3.12'` (`ci.yml` ×4, `wiki.yml`,
`bench-nightly.yml`, `bench-ondemand.yml`), so the corpora regenerate correctly on the runner and
`Oracles are reproducible` stays green. The failure is local only, which is exactly why it
survived to be found by the #316 lot: that corpus landed only because a 3.12 interpreter was
found outside the documented path.

## The floor is 3.12, and the reason is not the syntax

Issue #486 asks whether the floor should instead be lowered to 3.10 by rewriting `seeded_random.py`.
The rewrite is small — `def choice[T]` is the **only** PEP 695 site in the repository:

```text
$ grep -rn "def [a-z_]*\[" tools/*.py bench/**/*.py
tools/seeded_random.py:39:    def choice[T](self, seq: Sequence[T]) -> T:
```

So the syntax is not what decides this. What decides it is that **the committed corpora are the
generator's output**, diffed against a fresh generation on every pull request. A contributor
regenerating under 3.10 regenerates under an interpreter CI never runs; any value that moved with
the interpreter would reach `Oracles are reproducible` as an unexplained drift on a change that
has nothing to do with it — the failure mode the exact pins in `tools/requirements.txt` exist to
prevent, reintroduced one layer down.

**The floor is therefore the CI pin, and it is a floor rather than a range so that there is one
interpreter, not a family of them.** Lowering it would widen who can regenerate, at the cost of
widening what they can regenerate *differently*. That is the losing side, and it is recorded as
one in an ADR rather than left implied here.

## What this lot changes

**1. A refusal that is a sentence.** `tools/python_floor.py` holds the floor as a constant and
`require_supported_python()` raises `SystemExit` with the version found and the version needed.
It is called at the top of every entry point that imports `seeded_random`, **before** that import,
so the guard runs rather than the parser failing. All four of those entry points parse under 3.11
today — verified — which is what makes the guard reachable:

| entry point | documented in |
| --- | --- |
| `tools/generate_oracles.py` | `CONTRIBUTING.md`, `CLAUDE.md` |
| `bench/corpus/generate_corpus.py` | nowhere — the harnesses name it when the corpus is missing |
| `bench/corpus/generate_metrics.py` | `bench/README.md` |
| `bench/corpus/generate_vocabs.py` | `bench/README.md` |

The three bench generators are in scope because they fail identically and for the same reason;
Issue #486 names only the first because that is where it was found.

**2. One source of truth for the floor, checked.** The floor lives in `tools/python_floor.py` and
nowhere else. A test in `tools/tests/` asserts that every `python-version:` pin under
`.github/workflows/` equals it — the guard's premise is that the floor *is* the CI interpreter, so
a pin drifting away from the constant would make the refusal message a lie. `pytest tools/tests`
already runs in CI (`ci.yml`), so the check costs nothing new.

**3. The venv's creation step, with its floor.** `CONTRIBUTING.md`'s *Oracle validation* gains the
step that was missing, naming the interpreter and installing from the lock rather than the
human-edited input — the same two flags CI uses, for the same two reasons already commented in
`ci.yml`. `CLAUDE.md` gains a pointer, not a copy: its subject is the traps, and the recipe's
subject is the process.

**4. The generator's own stale recipe.** `tools/generate_oracles.py`'s module docstring still
carries a `Usage:` block reading `pip install rapidfuzz jellyfish` — six of the eight
dependencies short, and unpinned besides. It predates `tools/requirements.lock.txt` and would
send a reader who trusts it into a different failure. It is replaced by a pointer to
`CONTRIBUTING.md`.

## What it does not change

- `seeded_random.py` keeps its PEP 695 signature. Rewriting it would not raise the floor's real
  cost and would spend the syntax the rest of `tools/` may want next.
- No workflow pin moves. The constant is written to match what is already there.
- No corpus is regenerated. Nothing in this lot touches a generated value, so
  `Oracles are reproducible` must stay green with no diff — which is itself the check that the
  guard did not alter a generator's behaviour.

## Testing

`tools/tests/test_python_floor.py`:

- the guard passes on a version at the floor, and on one above it;
- it raises `SystemExit` on one below, and the message names both versions;
- every `python-version:` pin under `.github/workflows/` equals the constant.

Manually, on the hosted image where both interpreters exist:

```text
/usr/bin/python3.11 tools/generate_oracles.py      # a sentence, not a SyntaxError
/usr/bin/python3.12 tools/generate_oracles.py      # generates, and tests/oracles is unchanged
```
