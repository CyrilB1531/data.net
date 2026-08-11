# Design — #77: analyse the samples, which the solution build never reached

**Date:** 2026-08-06 · **Issue:** #77 · **Branch:** `chore/77-sonar-analyses-the-samples` ·
**Checkout:** `<repo>`

## Problem

The scanner reads **whatever MSBuild compiles between `begin` and `end`**, so its
view of the repository is exactly `DataNet.slnx` — and both samples are
deliberately outside it (ADR 0009).

The consequence compounds with #72: the packaging gate now requires every new
exported type to be reachable from `samples/DataNet.Sample`. **New code lands
there on every feature branch, in the one area no analyser reads.**

## Decisions

### D1 — Add the sample builds to the existing scanner window

Between `begin` and `end`, after the pack. Not by adding them to the solution.

### D2 — Neither sample joins `DataNet.slnx`, and that is verified

Adding them would destroy the thing being analysed: inside the solution,
`ProjectReference` resolution quietly satisfies the package references and the
packaging gate stops proving anything (ADR 0009).

So this change must leave the gate intact, and the pull request must show it.

### D3 — Demonstrated by mutation, on the branch itself

A finding introduced on purpose in `samples/DataNet.Sample/*.cs` and in
`samples/DataNet.DocSnippets/SnippetContext.cs` must appear in SonarCloud. Adding
a build step and observing no new findings is indistinguishable from adding a
build step that analyses nothing.

### D4 — First-pass findings are triaged with a count, not silently absorbed

The samples have never been analysed. Whatever appears is a first-pass backlog:
fix it or triage it, and **say how many** — a number is what lets a reader judge
whether the area was in reasonable shape.

### D5 — Exclusion decisions go in the workflow with their reason

`DocSnippets/Generated/` is generated from the Markdown on every run and is
already excluded from SonarCloud's analysis. Any further exclusion is written
where it takes effect, with why.

## Out of scope

- Making `dotnet build DataNet.slnx` reach the samples. It cannot without putting
  them in the solution, which ADR 0009 forbids.
- The .NET code-quality analysers, which are a separate gap (later #107).

## What "done" means

SonarCloud reporting on `samples/DataNet.Sample/*.cs` and
`samples/DataNet.DocSnippets/SnippetContext.cs`; demonstrated by mutation on the
pull request; neither sample in the solution and the packaging gate still failing
on an unreachable type; first-pass findings fixed or triaged with a count;
exclusions carrying their reason.
