#!/usr/bin/env python3
"""Deliberately broken. Exists only to prove the quality gate blocks a merge.

DO NOT MERGE THE PULL REQUEST THAT CARRIES THIS FILE. Delete the file and its
branch once the gate has been observed failing.

Why Python in tools/, and not C#: the point is to test the *quality gate*, not
the build. SonarAnalyzer.CSharp runs inside `dotnet build` with warnings as
errors (ADR 0015), so a C# reliability bug in src/, tests/ or bench/ would fail
the compilation before the scanner ever reached the gate — the job would go red
for the wrong reason and prove nothing about SonarQube Cloud.

tools/ is analysed by the scanner (`sonar.exclusions` does not list it; only
`sonar.coverage.exclusions` does) and is touched by no compiler, so the three
bugs below reach the gate and nothing else. Nothing imports or executes this
module either, so no CI job's result depends on it.

Each function is annotated with the rule it is meant to trip. Any one new
reliability issue is enough: the gate condition is `Reliability Rating on New
Code >= A`, and A means zero.
"""


def compare_a_value_with_itself(threshold: float) -> bool:
    """Trips S1764 — identical expressions on both sides of a comparison."""
    return threshold == threshold


def assign_a_variable_to_itself(count: int) -> int:
    """Trips S1656 — self-assignment, which does nothing."""
    count = count
    return count


def compare_incompatible_types(attempts: int) -> bool:
    """Trips S2159 — an equality check that cannot ever be true."""
    return attempts == "1"
