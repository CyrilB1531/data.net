# Design — #26: the complexity finding on the oracle generator

**Date:** 2026-08-04 · **Issue:** #26 · **Branch:** `chore/26-suppress-jaro-reference-complexity` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## The issue asked for the wrong thing

`python:S3776` (Critical) — `tools/generate_oracles.py:263`, complexity 19 against
a threshold of 15. The issue asked to extract the branches into named helpers, and
justified it like this:

> Unlike the C# suppressions of the same rule, this one has no "faithful port of a
> published algorithm" defence: it is our own glue code.

**That premise is false, and the issue's author wrote it.** Line 263 is
`_jaro_reference`: a transcription of the published Jaro algorithm — match window,
then transposition count — and the direct counterpart of `Jaro.SimilarityCore` in
C#, where `S3776` was suppressed in #7 for exactly the reason the issue claims does
not apply.

The design decision is therefore not "how to refactor" but "the refactor should
not happen", and saying why is the deliverable.

## Decisions

### D1 — Suppress, consistently with the C# treatment

The rule is suppressed on the Python side with the reasoning in the **docstring**,
not in a commit message nobody reads later. `#7` suppressed `S3776` on
`Jaro.SimilarityCore` because decomposing a published algorithm breaks the
one-to-one mapping with the reference that makes any divergence auditable. The
same function, in the other language, gets the same treatment.

Python has no pragma, so `# NOSONAR` is used — and it applies only to the line it
terminates, so it must be placed precisely rather than at the top of a block.

### D2 — The argument is stronger here than in C#, not weaker

This function **generates the reference data every other component is validated
against.**

So the usual reassurance — "the tests still pass" — is circular: the tests compare
against exactly this output. A restructuring that silently changed a corpus would
be invisible to the very suite designed to catch such changes.

That is the substantive finding of this branch, and it generalises: code that
produces the oracle cannot be validated by the oracle.

### D3 — Verify by regenerating, and report the drift explicitly

A comment-only change must produce **zero drift**. Regenerate and confirm rather
than reason about it — the cost is one command and the alternative is trusting the
same intuition that produced the wrong issue.

### D4 — Record the operational trap encountered while verifying

The generator must run from a neutral working directory, as `CONTRIBUTING.md`
says. Running it from `/home/cyril` instead of `/tmp` fails **even with
`PYTHONSAFEPATH` set**:

```text
ImportError: Blocked import of regex from current working directory for security reasons
```

This was hit while verifying this very change, and it matters beyond the anecdote:
**a green-looking "no drift" after a failed generator run proves nothing.** The
drift check is only meaningful when the generator exits 0.

## What this changes about how issues are read

An issue is a hypothesis, including one you wrote. This one asserted a
classification — "our own glue code" — that a thirty-second look at the function
disproves. The plan therefore starts by reading the code the issue describes,
before doing what the issue asks.

## Out of scope

- Any actual restructuring of `_jaro_reference`.
- The other `S3776` findings in the generator, if any exist that are genuinely
  glue code — those would be real, and would deserve their own issue.

## What "done" means

`# NOSONAR` with the reasoning in the docstring; corpora regenerated with zero
drift and the generator's own exit code read; the issue's false premise corrected
in the pull request rather than quietly ignored.
