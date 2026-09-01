# 0072 — Ω is an input, not a seed, and two options that need one are not offered

**Status:** accepted · **Date:** 2026-09-01

## Context

`Lodestar.Decomposition` reproduces scikit-learn 1.9.0's
`TruncatedSVD(algorithm="randomized")` and `NMF(solver="mu")`. Three of its divergences from
that reference are not three decisions: they are one, taken once and paid three times. All three
are the random matrix.

**The randomized solver starts from a draw.** `randomized_svd` builds a `n_features × (k + p)`
block Ω from `random_state.normal(size=...)`, pushes it through `A` and `Aᵀ`, and reads the
answer off a small dense problem. Two runs from two different Ω agree on the subspace and
disagree in the last digits of every number in it. So a package that claims parity with
scikit-learn has to say what it does about Ω, and there are only two honest answers: reproduce
numpy's generator, or take Ω from the caller.

**The measurement that decides it.** Over the *same* Ω, with `power_iteration_normalizer="QR"`
and `transpose=False`, a step-by-step reimplementation of `randomized_svd` reproduces its `U`,
its `s` and its `Vᵀ` to **exactly 0.0** — not to `1e-9`, not to a tolerance, to a difference of
zero — on a 40 × 25 fixture at `k = 4`, `p = 6`, `n_iter = 3`. The randomized part of a
randomized algorithm is the draw and nothing else; once the draw is an input, what remains is
ordinary floating-point arithmetic that two implementations can agree on bit for bit. That is
what makes Ω-as-a-parameter affordable, and it is why this package's conformance corpus can
freeze a factorization at all.

Reproducing `RandomState.normal` instead means MT19937 — 624 words of state, a tempering
function, the 32-bit split of every 53-bit double — plus numpy's cached polar Gaussian, which
draws pairs and hands out the second one on the next call. Every one of those is a place a
transcription can be subtly wrong.

**Two more features want the same generator, and get the same answer.**

`randomized_svd` takes `transpose="auto"`, which resolves to `n_samples < n_features` and swaps
the two products, factorizing `Aᵀ` and transposing the result back
(`sklearn/utils/extmath.py`, the `if transpose:` blocks around the range finder).
`TruncatedSVD` calls it with that default. A term-document matrix routinely has fewer rows than
columns — a corpus of 2 000 documents over 50 000 terms is the ordinary case, not the corner —
so on real input the flag is not dormant: it silently decides which of two factorizations ran.

`NMF(init="nndsvdar")` fills the zeros NNDSVD leaves with
`abs(avg * rng.standard_normal(size=...) / 100)` — `sklearn/decomposition/_nmf.py:355-359`, and
that is numpy's **Gaussian** stream, not its uniform one. It is therefore the same dependency on
MT19937 as Ω, with none of Ω's escape hatch: nobody would ever write a call site that passes a
vector of initialisation noise in by hand, so there is no input that turns it into a parity
target the way `RandomMatrix` does for Ω.

## Decision

**Ω is a parameter.**
[`TruncatedSvdOptions.RandomMatrix`](../reference/decomposition/factorization/truncatedsvdoptions.md)
and [`NmfOptions.RandomMatrix`](../reference/decomposition/factorization/nmfoptions.md) take the
block itself, row-major, and when one is given it replaces the draw entirely. That is what the
oracle corpus passes, and it is how the reference values in `tests/oracles/decomposition_svd.json`
and `tests/oracles/decomposition_nmf.json` are checked entry by entry rather than statistically.

**`Seed` answers the other question, and says which one it answers.** With no `RandomMatrix`,
both option types draw Ω from a SplitMix64 generator this package owns. It reproduces a run of
Lodestar and never a run of NumPy, and both properties' documentation says exactly that, because
a `Seed` that looked portable would be the one mistake this record exists to prevent.

**`transpose` is not exposed, and nothing swaps the products.** A matrix is factorized as it was
handed over, whatever its shape.

**`nndsvdar` is not shipped.**
[`NmfInitialization`](../reference/decomposition/factorization/nmfinitialization.md) has two
members, `NndSvd` and `NndSvda`, and no third.

## Options refused

**Reproduce MT19937**, the runner-up. It is the option that would make a seed portable between
the two ecosystems, and it would close all three divergences at once: Ω could be drawn from a
seed, `nndsvdar` could ship, and a Python `random_state=42` would mean something here. Refused
for three reasons, in order of weight. The API already accepts Ω explicitly, so the parity the
generator would buy is parity this package already has by another route — and it is the route
the corpus takes. A hand-written Mersenne Twister that is wrong is wrong *quietly*: the failure
does not surface as a bad random number, it surfaces as a factorization that is plausible,
stable, reproducible and not scikit-learn's, which is the hardest kind of bug this project can
ship. And it is a second conformance surface — numpy's generator, its Gaussian transform and its
`standard_normal` buffering — bought for a package whose subject is two matrix factorizations.

**Offer `transpose="auto"` anyway.** Refused because it is a parity claim with two shapes. A flag
whose value is computed from the input decides, without the caller naming it, which of two
factorizations the "parity" applies to; the corpus would then have to freeze both, and a reader
comparing a run against Python would have to work out which branch each side took before knowing
whether a disagreement is a bug. `transpose` exists in scikit-learn as a performance heuristic —
its own comment says the implementation is faster with a smaller second dimension — and a
performance heuristic is not worth a fork in the promise. This one is recorded in
[`docs/equivalence.md`](../equivalence.md) rather than hidden: a matrix with fewer rows than
columns is factorized as written here and transposed there, and the numbers part in the last
digits accordingly.

**Ship `nndsvdar` with this package's own noise.** Refused for the reason the package exists:
an initialisation that cannot be compared entry by entry against the reference is one no oracle
can pin, and this package ships nothing it cannot check. `NndSvda` is what `nndsvdar`
approximates — the same fill, from the mean, without the noise — and the reference page for the
enum says so, including the one use of the noise (breaking ties between identical rows) that a
different Ω serves just as well.

## Consequences

- The corpus is exact. `decomposition_svd.json` and `decomposition_nmf.json` carry Ω alongside
  the matrix, and the C# theories assert against scikit-learn's own output at `1e-9` rather than
  against a distribution.
- **A caller migrating a scikit-learn call has to carry Ω over, not the seed.** There is no
  `random_state` that means the same thing on both sides; the guide's
  *Reproducing a scikit-learn run* section is where that is stated for a reader, and the two
  `RandomMatrix` properties state it again at the point of use.
- The default path is still deterministic. `Seed` has an initialiser, so two runs of the same
  code on the same input give the same answer with no ceremony — which is the property most
  callers actually want, and the one they would otherwise have reached for `random_state` to get.
- Three shapes of divergence sit in `docs/equivalence.md` because of this record rather than in
  spite of it: `transpose="auto"`, `init="nndsvdar"` and `init="random"`, the last for exactly
  the same reason as the other two.
- `nndsvdar` and `transpose` become cheap to add later, and neither is blocked on anything but a
  generator. If a portable MT19937 ever lands here for another reason, both refusals are
  reopened by it and this record is what says so.
