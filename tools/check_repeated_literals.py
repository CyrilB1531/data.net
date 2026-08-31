#!/usr/bin/env python3
"""Refuse a pull request that pushes a Python string literal past S1192's threshold.

SonarCloud's S1192 fires on a literal repeated more than three times in one file,
and it is reported only after the push: `AnalysisMode=All` covers this
repository's C# in the build, but nothing looks at its Python before the
quality gate does. tools/generate_oracles.py has tripped it twice -- the second
time failing an otherwise green pull request, where three added corpus texts
took "the cat" from four occurrences to eight.

The file already holds some 108 literals past that threshold, mostly
JSON keys like "metadata" and "count". Reporting those would drown the signal,
and the gate itself tolerates them because only new code counts. So this script
compares against a base revision and reports a literal only when the change
pushes it *across* the threshold **and** its first occurrence is on a line the
change added. Both halves were measured on pull request #488, where the gate
raised one issue: a literal new to the file at four occurrences. Two others
crossed the threshold in the same diff and were not raised, because the line
S1192 anchors the issue to -- the first occurrence -- was already there, and
only new code counts.

A literal already over it and merely growing is not reported. That is the rule
this repository applies by hand anyway: META_SYMBOL, THE_CAT and BOS_TOKEN in
generate_oracles.py are literals that reached three uses and were given a name.
S1192 is why the question started being asked; the answer here is the naming
convention, which is why the threshold is stated once above rather than chased
against whatever the server's quality profile is set to this week.

Names it does not count: a literal shorter than MIN_LENGTH, which S1192 ignores
too; a docstring, which is documentation rather than a repeated constant; a
literal appearing once per file, which cannot be duplication; and anything under
tools/tests/, which sonar.exclusions already exempts.

Usage:  python tools/check_repeated_literals.py --base <commit>
        python tools/check_repeated_literals.py --report
        python tools/check_repeated_literals.py --help

  --base <commit>  What the pull request is compared against. Required for the
                    check: an implicit default would silently compare against
                    the wrong thing on a rebased or force-pushed branch.
  --report         Print every literal at or over the threshold in the working
                    tree, with its count, and exit 0. The standing backlog, for
                    a contributor who wants to see it rather than be blocked by
                    it.
  --help, -h       Print this message to stdout and exit 0.

Exit:   0 clean, 1 findings printed, 2 bad usage
"""

from __future__ import annotations

import argparse
import ast
import collections
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent

# Measured on #488, not assumed: a literal at four was the gate's only issue
# while three others sat at exactly three, unreported.
THRESHOLD = 4

# Shorter literals are noise -- a repeated "ok" or "id" is not the duplication
# the rule is about, and Sonar does not report them either.
MIN_LENGTH = 5

# Only this directory: the C# under src/ and tests/ has SonarAnalyzer in the
# build, which fails on S1192 long before a push.
SCANNED = "tools/"

# sonarcloud.yml excludes tools/tests/**, so a finding here is one the gate will
# never raise -- and a check reporting what the gate ignores gets ignored itself.
EXCLUDED = "tools/tests/"


# The +side line numbers of a unified diff hunk header, which is where the added
# lines of that hunk start and how many there are.
HUNK = re.compile(r"^@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@")


def added_lines(base: str, path: str) -> set[int]:
    """The line numbers this change added to `path`, as they read in the working tree."""
    out = subprocess.run(
        ["git", "diff", "-U0", base, "--", path],
        cwd=ROOT, capture_output=True, text=True, check=True)
    lines: set[int] = set()
    for line in out.stdout.splitlines():
        found = HUNK.match(line)
        if found:
            start = int(found.group(1))
            lines.update(range(start, start + int(found.group(2) or 1)))
    return lines


def _docstring_ids(tree: ast.AST) -> set[int]:
    """Every string constant that is a docstring, which is prose and not a constant."""
    ids = set()
    for node in ast.walk(tree):
        if not isinstance(node, (ast.Module, ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
            continue
        body = node.body
        if (body and isinstance(body[0], ast.Expr)
                and isinstance(body[0].value, ast.Constant)
                and isinstance(body[0].value.value, str)):
            ids.add(id(body[0].value))
    return ids


def first_lines(source: str) -> dict[str, int]:
    """The line each countable literal first appears on, which is where S1192 anchors."""
    try:
        tree = ast.parse(source)
    except SyntaxError:
        return {}
    docstrings = _docstring_ids(tree)
    first: dict[str, int] = {}
    for node in ast.walk(tree):
        if (isinstance(node, ast.Constant) and isinstance(node.value, str)
                and id(node) not in docstrings and len(node.value) >= MIN_LENGTH):
            first[node.value] = min(first.get(node.value, node.lineno), node.lineno)
    return first


def counts(source: str) -> dict[str, int]:
    """How often each countable string literal appears in one file's source.

    Every count, not only those over the threshold: the base revision's two
    occurrences are what make a third "was 2" rather than "new", and a message
    that called a grown literal new would send the reader looking for the wrong
    line.
    """
    try:
        tree = ast.parse(source)
    except SyntaxError:
        # A file this interpreter cannot parse is one it cannot judge. Silence is
        # the honest answer; the build is what reports a syntax error.
        return {}
    docstrings = _docstring_ids(tree)
    tally: collections.Counter[str] = collections.Counter()
    for node in ast.walk(tree):
        if (isinstance(node, ast.Constant) and isinstance(node.value, str)
                and id(node) not in docstrings and len(node.value) >= MIN_LENGTH):
            tally[node.value] += 1
    return dict(tally)


def over_threshold(source: str) -> dict[str, int]:
    """Only the literals S1192 would report."""
    return {literal: count for literal, count in counts(source).items() if count >= THRESHOLD}


def scanned_files() -> list[str]:
    """Every tracked .py file under SCANNED, in a stable order."""
    out = subprocess.run(
        ["git", "ls-files", "--cached", "--others", "--exclude-standard", SCANNED],
        cwd=ROOT, capture_output=True, text=True, check=True)
    return sorted(line for line in out.stdout.splitlines()
                  if line.endswith(".py") and not line.startswith(EXCLUDED))


def at_base(base: str, path: str) -> str:
    """One file's source at `base`, or empty when it did not exist there."""
    out = subprocess.run(
        ["git", "show", f"{base}:{path}"],
        cwd=ROOT, capture_output=True, text=True, check=False)
    return out.stdout if out.returncode == 0 else ""


def crossed(base: str) -> list[tuple[str, str, int, int]]:
    """(path, literal, before, after) for every literal this change pushed over the threshold."""
    findings = []
    for path in scanned_files():
        source = (ROOT / path).read_text(encoding="utf-8")
        after = over_threshold(source)
        if not after:
            continue
        before = counts(at_base(base, path))
        added = added_lines(base, path)
        first = first_lines(source)
        for literal, count in sorted(after.items()):
            if before.get(literal, 0) < THRESHOLD and first.get(literal) in added:
                findings.append((path, literal, before.get(literal, 0), count))
    return findings


def report() -> int:
    """The standing backlog, so a contributor can see it without being blocked."""
    total = 0
    for path in scanned_files():
        over = over_threshold((ROOT / path).read_text(encoding="utf-8"))
        if not over:
            continue
        total += len(over)
        print(f"{path}: {len(over)} literal(s) at {THRESHOLD} occurrences or more")
        for literal, count in sorted(over.items(), key=lambda item: -item[1]):
            print(f"    {count:4}x  {literal!r}")
    print(f"\n{total} literal(s) repeated {THRESHOLD} times or more under {SCANNED}")
    return 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(add_help=False)
    parser.add_argument("--base")
    parser.add_argument("--report", action="store_true")
    parser.add_argument("--help", "-h", action="store_true", dest="help_wanted")
    try:
        arguments = parser.parse_args(argv[1:])
    except SystemExit:
        print(__doc__, file=sys.stderr)
        return 2
    if arguments.help_wanted:
        print(__doc__)
        return 0
    if arguments.report:
        return report()
    if not arguments.base:
        print(__doc__, file=sys.stderr)
        return 2

    findings = crossed(arguments.base)
    for path, literal, before, after in findings:
        was = "new" if before == 0 else f"was {before}"
        print(f"{path}: {literal!r} now appears {after} times ({was})")

    if findings:
        print(
            f"\n{len(findings)} literal(s) reached {THRESHOLD} occurrences in a file this "
            "change touches. Give each a name beside the other constants at the top of "
            "its file and point every occurrence at it, the way META_SYMBOL and THE_CAT "
            "already are -- SonarCloud's S1192 reports the same thing, but only after "
            "the push. `--report` lists the standing backlog, which is not yours to clear.",
            file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
