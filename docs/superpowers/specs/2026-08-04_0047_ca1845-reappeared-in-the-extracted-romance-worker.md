# Design — #47: `CA1845` reappeared in the extracted Romance worker

**Date:** 2026-08-04 · **Issue:** #47 · **Branch:** `fix/romance-base-ca1845` ·
**Checkout:** `<repo>`

## Problem

The quality gate on `main` fails with **Maintainability: 1 open issue** —
`CA1845` against `src/DataNet.Text/Stemming/RomanceSnowballWorker.cs`.

Extracting the shared Snowball framework (#45) moved `Delete` and `Replace` out of
the four language stemmers into a new file. **The suppression stayed behind in the
files the code left**, so the rule reappeared against the new one:

```csharp
protected void Replace(int suffixLen, string repl) => S = S.Substring(0, S.Length - suffixLen) + repl;
```

## Decisions

### D1 — Suppress, with the same justification as the four language stemmers

Unchanged from everywhere else it appears: the span-based `string.Concat` overload
is **net-only**, and the `Substring` form is what makes the file compile for
`netstandard2.0`. Following the rule here breaks that target.

Copy the wording from the language files rather than paraphrasing. A reader
comparing the five should find them identical; a paraphrase invites the question
of whether the reasons differ.

### D2 — This is the tail of a change that worked, and the record should say so

Duplication on `main` went **5.9 % → 4.1 %** with the extraction. #47 is the
residue of that, not evidence against it. Stating so keeps the next reader from
concluding the refactor was a mistake.

### D3 — The general lesson is written down, not just the fix

**When code carrying a justified suppression moves to a new file, the suppression
does not move with it.** Nothing enforces that, which is why it slipped here — and
the build stayed green throughout, because at this point nothing in it runs the
analyzer. Only the dashboard caught it.

This has now happened once. It will happen again on the next extraction, and the
observation belongs in `CONTRIBUTING.md` rather than in this branch's commit
message.

## Out of scope

- Any change to `Delete` or `Replace`.
- Making the build run the analyzer, which is the real fix for the class and is
  its own issue (later #84).

## What "done" means

`CA1845` suppressed in `RomanceSnowballWorker` with the same justification as the
language stemmers; both frameworks green, 164/164; the quality gate green on
`main`.
