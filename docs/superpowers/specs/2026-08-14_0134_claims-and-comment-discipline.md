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

### D5 — nine claims in ten cite nothing

504 comment lines across the tracked tree name a reference library. **46 of them — 9% — carry a pointer to
anything that would check them**: a corpus file, an oracle case, a measurement, an ADR, an issue.

| zone | claims | citing evidence |
| --- | ---: | ---: |
| `src/DataNet.Metrics` | 162 | 2 |
| `src/DataNet.Embeddings` | 113 | 12 |
| `src/DataNet.Text` | 76 | 3 |
| `tests/DataNet.Embeddings.Tests` | 42 | 7 |
| `tests/DataNet.Metrics.Tests` | 35 | 6 |
| `tools/generate_oracles.py` | 28 | 10 |
| `samples/`, `bench/`, the rest | 48 | 6 |

That distribution is what orders the sweep, and it also says what the sweep can honestly be. **Nobody can
re-derive 504 claims by hand**, and a sweep that said it had would be the most expensive false claim in
this issue. What it can do is make each one checkable or make it go away.

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

**It cannot be switched on before the 354 blocks are dealt with**, which is why the sweep is in this
lot rather than after it, and why the guard lands last.

### The sweep, and what it can honestly claim

The 354 over-length blocks and the 504 claims are in scope, and the guard cannot be switched on until the
blocks are dealt with — that ordering is what makes them one lot rather than two.

**Its product is not "504 claims verified".** It is: every claim either carries a pointer to what would
check it, or no longer exists. Each is triaged into one of three, and the triage is the work:

1. **A corpus already answers it.** Cite the file and the case. This is the cheap tier and it is why the
   sweep starts where the corpora are thickest.
2. **It is executable but nothing frozen answers it.** Run it once and cite the output — or, where the
   answer deserves freezing, add the corpus case and cite that instead.
3. **Nothing reasonable checks it.** Then it is an opinion wearing a measurement's clothes, and it gets
   cut or rewritten as the opinion it is. A comment that cannot be checked is not thereby exempt; it is
   thereby not a claim.

The same pass handles the block that sits around it: over eight lines, it is cut, moved to an ADR and cited
from one line, or marked with its reason.

**Order, from D5's distribution:** `src/DataNet.Metrics` with `tests/DataNet.Metrics.Tests` first — the
most claims, the fewest citations, and oracle corpora that make tier 1 nearly free. Then
`src/DataNet.Embeddings` with its tests, then `src/DataNet.Text`, then `tools/generate_oracles.py`, then
`bench/` and `samples/`. Each is its own plan; the guard is switched on by the last of them.

`tests/` is not the lesser half. A comment in `src/` asserts what the reference does; a comment in `tests/`
asserts **what the corpus proves**, which is the evidence a reviewer reaches for when judging everything
else. `tools/generate_oracles.py`'s 28 are the same shape one step earlier — they explain why a corpus
contains what it contains, and they are read by whoever regenerates it.

### The prose documents, read against each other

`CONTRIBUTING.md`, `README.md`, `docs/equivalence.md`, `CLAUDE.md` and `docs/guides/` are in scope for the
same reason the comments are: they assert things, they were written at different times, and nothing checks
that they still agree. A duplicated paragraph is where a correction lands in one copy and not the other —
which is how three of the eight 2026-08-13 failures happened, inside single documents.

This pass looks for three things, and they need different fixes:

- **A statement in two places.** One of them is the home; the other becomes a pointer. Which is which is
  decided by where a reader would look first, not by which was written first.
- **Two statements that disagree.** Measure which is true before choosing — a contradiction resolved by
  preferring the newer sentence is a coin toss with extra steps.
- **A statement that was true and is not.** The same defect as a stale comment, in a file the build never
  reads.

`docs/decisions/` is included, and the sweep will have added to it: an ADR contradicting a newer one is
worse than a stale comment, because ADRs are what the repository consults to settle exactly that kind of
question.

## Evidence

`tools/tests/test_check_comment_length.py`, beside the existing guard tests that CI already runs through
`python -m pytest tools/tests -q`. Its fixtures are **real blocks from this repository**, recovered from
the measured list — the 63-line one, a nine-line one just over the threshold, an eight-line one just under,
a marked block, and a block interrupted by a blank line, which is where a naive counter is wrong.

The rule's own text is checked by the thing it describes: this spec and its plan are inside the scope it
declares.

## What done looks like

This lot is large enough that "finished" has to be a state rather than a list of tasks completed:

- `tools/check_comment_length.py` runs in `Lint` **and passes**, which means every block over eight lines
  carries a marker whose reason a reviewer accepted.
- Every comment naming a reference library either cites what would check it or has been rewritten as the
  opinion it was. The count that is 46 of 504 today is the measure, and it is checkable the same way it was
  measured.
- No comment paraphrases the line below it.
- `CONTRIBUTING.md` states the rule, `CLAUDE.md` points at it, and `.github/instructions/` carries the
  review step.
- The prose documents say each thing once, and nothing in them contradicts anything else in them.

## Out of scope

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
