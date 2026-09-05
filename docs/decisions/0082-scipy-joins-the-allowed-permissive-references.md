# 0082 — scipy joins the allowed permissive references, for the Kolmogorov branch table

**Status:** accepted · **Date:** 2026-09-05

## Context

[Decision 0003](0003-provenance-and-licensing.md) allows "permissively licensed implementations
as a *behavior reference* only," naming rapidfuzz (MIT), jellyfish (MIT), textdistance (MIT) and
scikit-learn (BSD-3). It does not name scipy — no package before `Lodestar.Stats` (#442) had
reason to consult it.

`KolmogorovSmirnov.TwoSample` needs the finite-sample Kolmogorov distribution's survival
function, `P(D_n > d)`. Durbin's (1968) recursion and the Marsaglia-Tsang-Wang (2003) scaling
give the exact route; Pelz-Good's asymptotic expansion gives the other end. Between them is a
**dispatch problem the published papers do not settle**: which of several exact methods, direct
formulas and asymptotic approximations applies for a given `(n, d)`, and where each cedes to the
next. Durbin's paper does not say when its own recursion stops being the cheapest correct answer
against a direct combinatorial route; Pelz-Good's does not say at what `n·d²` an asymptotic
expansion becomes accurate enough to prefer over an exact but slower one. Answering that from
first principles for every one of the ten rows `Internal/Kolmogorov.cs`'s dispatch table carries
(`n > 140`, `n·d² < 2.2` vs `≥ 2.2` vs `≥ 370`, `n·d^1.5 ≤ 1.4`, and so on) would mean deriving
error bounds for four numerical methods against each other — a research project, not an
implementation task.

scipy's own `_ksstats.py` (`scipy.stats`, BSD-3) already carries exactly that dispatch,
`_kolmogn`, tuned against decades of use. Task 8 (`.superpowers/sdd/2026-09-05_0442_lodestar-stats/task-8-report.md`,
finding 4) found and corrected a case where this had been done *without saying so plainly*: the
comment on `DurbinCdf`'s scaling claimed no reference implementation was transcribed, when the
scaling constant (`ScaleBits = 128`, scipy's own `_E128`/`_EP128`, `_ksstats.py:73-75`) and the
rescale trigger's placement did follow `_ksstats.py` structurally, not merely arrive at the same
answer independently. The mathematics — Durbin's recursion, Marsaglia-Tsang-Wang's scaling
identity, Pelz-Good's expansion — is independently implemented from the published papers; the
**decision of which branch runs where**, and the specific thresholds bounding each one, is read
from scipy's dispatch table, attributed as such in the file's own long comment.

## Decision

**scipy (BSD-3) is added to [decision 0003](0003-provenance-and-licensing.md)'s named list of
allowed permissive references**, alongside scikit-learn — the same licence, the same
"behavioural reference only" rule, and the same "we reproduce inputs/outputs and analogous
naming, never the source" boundary. This is recorded as its own decision rather than by editing
0003's body: `docs/decisions/README.md` states the convention this repository already
follows — "a decision record is not edited; an amendment is its own record" — and
`tools/check_adr_immutable.py` enforces it mechanically for every ADR that already existed before
this pull request. 0003 stays exactly as accepted; this decision is what a reader follows to see
that its rule was extended and why.

**What was read from scipy, precisely.** `Internal/Kolmogorov.cs`'s branch conditions and their
order — the ten-row table in the file's own comment, and the two scaling constants inside
`DurbinCdf` — are scipy's `_kolmogn`'s dispatch, read and reproduced structurally. No scipy source
line is transcribed: the C# is an independent recursion (`DurbinCdf`) collapsing scipy's first
three closed-form branches into one general one, verified by derivation rather than copied
algebra (the file's own comment shows the derivation for `n·d ∈ (0.5, 1]`, cross-checked against
scipy numerically, `n=2, d=0.49 → 0.5392` on both sides). What crosses over is *which formula
applies where*, which is behaviour, not code — exactly what 0003 already permits from a
permissively licensed reference, extended here to name the specific package doing the permitting.

## Consequences

- `docs/decisions/README.md`'s row for [`0003`](0003-provenance-and-licensing.md) gains a pointer
  to this decision in its relationships column — one-sided, not a cross-reference: 0003 was
  already `accepted` when this was written, so `tools/check_adr_immutable.py` refuses a diff that
  edits it, and 0003's own body says nothing about scipy. The index carries the back-reference
  instead, the same shape [`0057`](0057-the-npy-read-serves-a-stream-and-a-buffer-differently.md)
  and [`0058`](0058-the-npy-ingest-is-memcpy-bound-and-the-allocation-is-not-the-cost.md) already
  use for an amended decision that cannot name its own amendment.
- A future package that reads scipy's own dispatch logic to settle a branch-selection question —
  rather than a formula it derives independently — cites this decision rather than reopening
  0003 or writing its own.
- `Internal/Kolmogorov.cs`'s long comment remains the concrete, line-numbered record of what was
  read (`_ksstats.py:73-75` for the two scaling constants, the ten-row table for the dispatch);
  this decision is the policy that comment operates under, not a restatement of it.
