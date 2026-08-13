# 0134 — Comments: what they may claim, and how long they may be

**Issue:** [#134](https://github.com/CyrilB1531/data.net/issues/134) · **Date:** 2026-08-14

## Context

This repository's comments carry unusual weight. A comment saying *why* a divergence from the Python
reference exists is what makes that divergence reviewable, and reviewers reach for those sentences when
deciding whether code is right. That is what makes them load-bearing, and it is also what makes them
dangerous: nothing checks them, and they rot silently when the code beside them moves.

Two separate failures live here, and they need different instruments.

**Claims go stale.** Eight false claims were produced in two lots on 2026-08-13 alone, none caught by a
tool. Four more came from #127, four from #140 and #121, three predate the issue.

**Prose has crowded out code.** Measured across `src/`, `tests/`, `tools/`, `bench/` and `samples/`:
**1837 comment blocks, 9803 lines.** 354 of those blocks run over eight lines — 19% of the blocks, holding
**5532 lines, 56% of all the prose**. One block in five carries more than half the text. The longest is 63
lines.

## What is measured

### D1 — the rule this repository is said to have does not exist

The issue's own opening sentence asserts that `CONTRIBUTING.md` says a wrong claim in a comment is a
defect. It does not. `CONTRIBUTING.md` mentions comments four times — a reason above a `#pragma`, a comment
naming each `NoWarn`ed rule, a divergence belonging in `docs/decisions/` rather than a comment alone, and
an analyzer finding being a compile error rather than a pull-request comment. None states the rule. Neither
does `CLAUDE.md`, any ADR, or any guide.

**So the first step is writing the rule, not enforcing it.**

### D2 — re-reading a claim confirms it; only derivation breaks it

This is the finding that invalidates the fix the issue first proposed. On 2026-08-13, in #140, a false
claim **survived two reviews that were both looking at it**: a task reviewer wrote it, an implementer
transcribed it, and the whole-branch reviewer caught it only because it re-derived the shape from scratch
rather than reading the sentence.

The same pattern holds across every case with a known provenance. Of the eight from 2026-08-13:

| how it fell | count |
| --- | --- |
| someone re-derived the claim independently (ran it, re-implemented it, recounted it) | 6 |
| a differential against an independently written reference | 1 |
| an agent blocked by a criterion that contradicted the measurement | 1 |
| **someone read the sentence carefully** | **0** |

A review step that asks "is this comment still true?" buys the appearance of a check. The instruction has
to ask **what would you run to check it, and did you run it** — and where the claim is executable, to run
it and paste the output.

### D3 — the length problem is mechanisable, and the truth problem is not

| | blocks | lines | share of prose |
| --- | ---: | ---: | ---: |
| all comment blocks | 1837 | 9803 | 100% |
| blocks over 8 lines | 354 | 5532 | **56%** |

The ten longest run 35 to 63 lines. `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs` holds three of
them; `TokenizerJsonLoader.cs:7` holds the longest.

Counting a block's lines is exact and cheap, which makes it a `Lint` job in the shape
`tools/check_machine_paths.py` established for #133. Deciding whether a sentence is true is not, which is
why the two halves of this lot use different instruments and why neither substitutes for the other.

### D4 — the long explanations already have a home

`CLAUDE.md` requires a deliberate divergence from the Python reference to go in `docs/decisions/`. A
63-line comment is an ADR that was never written. The 5532 lines are therefore not waste to delete but
prose in the wrong place: it belongs where someone goes to understand a decision, not where someone is
trying to read code.

## Design

### The rule, in `CONTRIBUTING.md`

A new section beside `Performance claims`, which is the precedent — this repository already governs one
class of claim by requiring its evidence, and ends that section with the same principle this lot
generalises: *"Verify what you are actually measuring before quoting a result."*

Four rules, stated once:

1. **A comment says why, never what.** Paraphrasing the line below it is noise that goes stale faster than
   the code does.
2. **A claim carries what would check it.** Where the claim is executable — a measurement, a reference
   library's output, a count — run it and cite the output or the corpus case. "Measured" without a pointer
   is an assertion.
3. **Eight lines above a member.** Beyond that, the reasoning belongs in `docs/decisions/`, cited from one
   line — or it needs cutting.
4. **A block over eight lines carries a marker naming its reason**, in the shape every other exemption in
   this repository takes.

**Scope: every tracked file, including `docs/superpowers/`.** A spec that overclaims what its corpus proves
is the same defect; #119's and #130's did exactly that, and both were caught by an implementer refusing to
proceed, which is not a process.

`CLAUDE.md` gains a pointer to the section, since that is what agents read.

### The derivation step, in `.github/instructions/`

`.github/instructions/sonarqube_mcp.instructions.md` is this repository's only durable instruction file
that review flows cite, and `CLAUDE.md` names it. A new sibling carries the review step, because a rule
that lives only in a session's dispatch prompt does not survive that session.

It states the trigger and the question:

- **Trigger:** every comment the diff **modifies or moves**. Moves matter: three of the eight 2026-08-13
  failures were one sentence corrected in one place and left standing in its copy.
- **Question:** not "is this true", which re-reading answers yes to (D2), but "what would you run, and did
  you run it" — and for a reviewer, derive it independently rather than checking the author's reasoning.
- **A marker is a claim too, and the review judges it.** The guard can only see that a marker exists; only
  a reader can see whether the block deserved one. So a length marker in the diff is reviewed like any
  other exemption in this repository — against the same bar `#pragma warning disable` is held to, where
  "too noisy" is explicitly not a reason. A block that could have been eight lines, or whose reasoning
  belonged in an ADR, is a finding even though the guard passed it.

### The length guard, in `Lint`

`tools/check_comment_length.py`, on the pattern `check_machine_paths.py` established: standard library
only, `git ls-files`-scoped, a docstring naming the drift it catches, invoked from CI.

It counts consecutive comment lines and refuses a block over eight that carries no marker. **The marker is
what keeps this from being a ban**: longer is allowed where it is necessary, and the marker plus its reason
is what stops it becoming the norm — the same bargain `#pragma warning disable` strikes. The guard also
reports the marker count, so growth is visible rather than gradual.

**It cannot be switched on before the 354 blocks are dealt with**, which is what makes this two lots.

## Evidence

`tools/tests/test_check_comment_length.py`, beside the existing guard tests that CI already runs through
`python -m pytest tools/tests -q`. Its fixtures are **real blocks from this repository**, recovered from
the measured list — the 63-line one, a nine-line one just over the threshold, an eight-line one just under,
a marked block, and a block interrupted by a blank line, which is where a naive counter is wrong.

The rule's own text is checked by the thing it describes: this spec and its plan are inside the scope it
declares.

## Out of scope

**The sweep of the 354 blocks and the existing claims** — 504 in `src/` naming a reference
library, and 197 more outside it. It inherits the definition these two
parts freeze, and specifying it now would mean guessing what that definition says. It gets its own spec,
sized on `DataNet.Metrics` **and** `tests/DataNet.Metrics.Tests` together — the two halves of one claim are
usually split across them, and `tests/` is the worse half: a comment there asserts what the corpus
*proves*, which is the evidence a reviewer reaches for.

**The prose-document audit** — `CONTRIBUTING.md`, `README.md`, `docs/equivalence.md` and the guides read
against each other for duplication and contradiction. That is a cross-reading, not a rule, and it is its
own lot.

**Automating truth.** No tool decides whether a sentence is true. The guard counts lines; the review step
asks for derivation. Anything claiming to do more would be the third instrument this lot does not have.

## Risks

- **A marker becomes a rubber stamp.** This is the likeliest failure, because the guard passes a marked
  block whatever its reason says. Two things push back and neither is complete: the review step judges the
  marker's legitimacy explicitly, held to the bar `#pragma warning disable` is held to, where "too noisy"
  is not a reason; and the guard reports the marker count, so the norm drifting is visible rather than
  gradual. Nothing makes it impossible.
- **Eight is a round number.** It was chosen rather than measured, and the distribution does not argue for
  a different one — 80% of blocks already fit. If it proves wrong the constant is in one place, and the
  marker means being wrong costs a marker rather than a deletion.
- **The derivation step costs review time**, and it is the expensive half. The bound is the diff, not the
  repository, and D2's count is the argument for paying it: six of eight failures needed something
  executed, and none was caught by reading.
