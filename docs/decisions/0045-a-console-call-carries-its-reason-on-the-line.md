# 0045 — A `Console` call carries its reason on the line, not in an exemption list

**Status:** accepted · **Date:** 2026-08-21

## Context

Ten `Console` calls under `bench/` narrated a run: four banners naming the harness, one line
per measured row, one per length bucket, two `-> path` lines after a results file was written,
and a `filtered:` notice duplicating what `metadata.filtered` already carries into the JSON —
where `bench/compare.py` refuses a filtered run outright.

None of them was in a timed region. `Harness.Measure` prints after its measurement loop closes;
`PairsHarness` prints after `TimeBucket` returns; `BucketRouteDiagnostics` prints inside
`[GlobalSetup]`, which BenchmarkDotNet runs once and does not time. **No `[Benchmark]` method in
the repository contains one.** So no published number was ever wrong because of them.

That is precisely why they accumulated. Each was individually harmless, each was added by
someone who wanted to see progress during a multi-minute run, and nothing was counting. A guard
that only fires on damage would never have fired here.

`src/` had none, and has never had any. A library that writes to a console its caller did not
open is deciding for an application it cannot see — a rule this project has kept by everyone
happening to remember it.

## Decision

**`check_no_console_writeline.py` refuses a `Console` call under `src/` outright, and one under
`bench/` that does not carry a `console-print:` marker naming its reason.**

The marker sits above the call or trails it, and an empty one is refused — the same shape and
the same refusal as `check_comment_length.py`'s `long-comment:`. Whether the reason is a good
one is a review's call, not the guard's, which is the division
[`0015`](0015-sonar-rules-in-the-build.md) already draws for an analyzer suppression.

**`src/` gets no marker, and will not.** There is no reason a shipped package should print that
would survive review, so offering a way to write one down invites the argument rather than
settling it. The guard is a ratchet on a tree that is already at zero.

**No exemption list in the script**, which is the decision this record exists for.
`check_machine_paths.py`'s own docstring states the case against one: *an exemption list that
grows is a guard being switched off one file at a time*. A list in the script is edited by
whoever is annoyed by the guard, in a file the reviewer of the offending change has no reason to
open. A marker is written in the diff that needs it, on the line that needs it, in front of the
person who can refuse the reason.

Four calls carry a marker today, each holding something no file holds:

| call | what it says |
| --- | --- |
| `Program.cs` — `Console.Error.WriteLine` | the benchmark loaded the wrong build, so every number it prints is meaningless |
| `Program.cs` — `Console.WriteLine` | which assembly and framework it did load: the pair of the refusal above |
| `RocParallelBench.cs` | why a cell is missing from the table, rather than letting it vanish |
| `BucketRouteDiagnostics.cs` | the two group sizes its own two measured rows are read against |

## Consequences

The guard is offline and reads only tracked sources, so it joins the hook rather than the
exclusions — [`0037`](0037-the-guards-run-before-the-commit.md) sets that rule, and
`tools/tests/test_pre_commit_hook.py` enforced it here: wiring the guard into CI and not into
`.githooks/pre-commit` failed that test immediately, naming the guard and the two places it
could go. Measured on the machine that ADR timed the others on: **0.05 s**.

The loser is worth naming. A guard with no escape at all was the alternative, and it would have
cost the four calls above — a silently wrong build being the one this project can least afford,
since #354, #356 and #351 were all defects that corrupted measurements while everything looked
green. The marker keeps them and still stops the tenth banner.

What this does not claim: **removing the ten changed no measurement.** They were never in a
timed region, and anyone reading the guard as a performance fix would be reading it wrong. What
it buys is a CI log a reader can use and a habit that stops growing.
