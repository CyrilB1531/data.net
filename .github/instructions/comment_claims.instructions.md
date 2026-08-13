---
applyTo: "**/*"
---

# Reviewing a claim in a comment

`CONTRIBUTING.md`'s *Claims in comments* states the rule. This is what a review does about it.

## The trigger

Every comment the diff **modifies or moves**.

Moves are not a formality. Three of the eight false claims found on 2026-08-13 were one sentence corrected
in one place and left standing in its copy — a plan's prose fixed while the commit-message block fifty
lines below kept the old number, twice in one branch. A comment that moved without being rewritten is
exactly where a correction fails to arrive.

## The question

Not "is this claim still true". That question is answered *yes* by re-reading, because the second reader
inherits the first's framing from the diff.

On 2026-08-13, in issue #140, a false claim survived two reviews that were both looking at it: a task
reviewer wrote it, an implementer transcribed it, and the whole-branch reviewer caught it only because it
re-derived the shape from scratch. Of the eight failures that day, six fell to someone re-deriving the
claim independently, one to a differential against a separately written reference, one to an agent blocked
by a criterion that contradicted its measurement, and **none to careful reading**.

So the question is: **what would you run to check this, and did you run it?**

- Where the claim is executable, run it and cite the output — the corpus case, the command, the file and
  line.
- Where a reviewer is checking someone else's claim, derive it independently rather than following their
  reasoning. Reading their derivation confirms it; producing your own does not.
- Where nothing reasonable checks it, it is an opinion. That is allowed, and saying so plainly is the fix —
  a comment that cannot be checked is not thereby exempt, it is thereby not a claim.

## The marker

A comment block over eight lines carries `long-comment:` and a reason on its first line, and
`tools/check_comment_length.py` refuses one that does not. The guard sees only that a marker exists.
**Whether the block deserved one is the review's call**, at the bar a `#pragma warning disable` is held to.
A block that could have been eight lines, or whose reasoning belonged in an ADR, is a finding even though
the guard passed it.
