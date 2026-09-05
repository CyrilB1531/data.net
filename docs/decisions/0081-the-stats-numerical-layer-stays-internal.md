# 0081 — The stats numerical layer stays internal

**Status:** accepted · **Date:** 2026-09-05

## Context

`Lodestar.Stats` (#442) reproduces `scipy.stats` 1.18.0 across ten families of hypothesis test
without carrying a numerical dependency: every tail probability a test needs — the Student and
Welch *t*-distributions, the chi-square and *F* distributions, the incomplete beta and incomplete
gamma functions underneath them, the normal CDF, the finite-sample Kolmogorov distribution the
two-sample KS test consults — is computed by `Lodestar.Stats.Internal` (`Beta`, `Gamma`,
`Normal`, `Kolmogorov`, `RankDistributions`), not by a package this one references.

That layer is capable enough to be useful on its own: an incomplete beta function, a log-gamma,
and an `erfc` accurate into the far tail are exactly what a caller reaching for a p-value from a
statistic it already has would want. Three ways of exposing it were on the table, and only one
was taken.

## Decision

**The numerical layer stays `internal`, reachable only through the ten test families that already
use it.** No public surface, no reference page, no sample reference.

**Refused: a public `Lodestar.Stats.Special` namespace.** Publishing `Beta.IncompleteRegularized`,
`Gamma.IncompleteRegularized`, `Normal.Erfc` and the rest as their own namespace would make each
one a parity promise in its own right — its own reference page under the packaging gate, its own
line in `docs/wiki-map.json`'s `covered` table, its own use in `samples/Lodestar.DocSnippets` to
satisfy the packaging gate, and its own oracle corpus checked at the tolerance a general-purpose
special-function caller would need, not the one the ten tests above it happen to need. Issue #442
asks for ten hypothesis tests; it does not ask for a special-functions library, and a promise made
without a stated need behind it is a promise this repository would then have to keep whether or
not anyone used it. Publishing later stays possible the day a second package needs the same
functions; unpublishing a public API once shipped does not.

**Refused: putting it in `Lodestar.Abstractions`.** `Lodestar.Abstractions` holds `CsrMatrix`,
`SparseNorm` and the dense-block products — the sparse primitive `Lodestar.Text` and
`Lodestar.Decomposition` both depend on as a published package floor
(`src/Directory.Packages.props`). `Lodestar.Stats` has no sparse matrix in it anywhere; putting
its incomplete beta function in `Abstractions` would buy a published floor between two packages
for code only one of them calls, and would widen `Abstractions`' own subject — the sparse
primitive — to include a special-functions library it was never about. The edge `Stats →
Abstractions` would exist for one function's convenience, not for a shared type either package's
public surface exposes.

**Recorded: `Normal.Erfc(x)` is computed as `Q(1/2, x²)`, not a rational fit of its own.** `erfc`
appears twice in this package — directly, in the normal approximation every `Auto`/`Asymptotic`
rank-based test falls back to, and indirectly, since the chi-square and Student tails both reduce
to the same regularized incomplete gamma `Q` that a rational `erfc` approximation would have to
agree with independently. Deriving `erfc(x) = Q(1/2, x²)` for `x ≥ 0` from the incomplete gamma
already implemented for chi-square means there is exactly one far-tail approximation in the
package to get right, not two whose disagreement would show up as a discrepancy between, say, a
two-sided z-test and a chi-square test on the same squared statistic. The far-tail accuracy `Q`
already needs for a chi-square test with many degrees of freedom comes free to `erfc` as a
consequence, rather than being a second accuracy target chosen independently.

**Recorded: the p-value tolerance is relative, and the oracle comparator was not changed to
accommodate it.** `tests/oracles/*.json` compares floats at `1e-9` **absolute** everywhere else in
the repository, and a statistic (the *t*, the *F*, the *U*, *W*, *H*, *D*) lives on a scale the
data sets, so the repository's default is the right one for it. A p-value does not live on such a
scale: measured on ordinary corpus cases — not adversarial ones — `TTest.Independent` reaches a
p-value of `7.85e-26` and `OneWayAnova.Test` reaches `2.38e-53`. At `1e-9` absolute, an
implementation whose incomplete beta or incomplete gamma returned exactly `0.0` in the far tail
would pass every one of those comparisons; the tail is exactly where a hand-written incomplete
beta or incomplete gamma is most likely to go wrong, first through underflow and then through
cancellation, so an absolute tolerance there proves nothing over the range it matters most.
`StatsOracleAsserts` (`tests/Lodestar.Stats.Tests/Oracles/`) therefore compares a p-value at
`1e-9` **relative** and a statistic at `1e-9` **absolute**, in the same assertion helper, so the
two never get silently swapped.

`tools/compare_oracles.py` — the *Oracles are reproducible* job's comparator — was deliberately
**not** changed to add a per-package or per-field relative mode. [Decision
0073](0073-the-oracle-gate-compares-numbers-not-bytes.md) already chose numeric comparison at a
fixed tolerance as that job's whole contract, and [decision
0079](0079-tied-textrank-scores-canonicalize-by-phrase-not-blas.md) is the precedent for refusing
to weaken an ordering or a comparison rule to serve one package's corpus rather than fixing the
corpus itself — there the fix was a canonical sort order in the generator, not a looser diff.
Here the parallel fix is the one already in place: `StatsOracleAsserts` applies the relative check
inside the C# suite, where the assertion has a name and a reason attached to it, while the
reproducibility job keeps comparing the numbers the generator wrote against a fresh run of the
same generator at the one tolerance every other package's corpus is held to. Loosening
`compare_oracles.py` for p-values would also loosen it for every statistic already compared there,
which is a wider change than this package asked for.

## Consequences

- `Lodestar.Stats.Internal` carries `InternalsVisibleTo` only for
  `Lodestar.Stats.Tests` and `Lodestar.Stats.NetStandard.Tests` (`Lodestar.Stats.csproj`), the
  same shape every other package's internal layer uses. Nothing under
  `Lodestar.Stats.Internal` appears in `docs/reference/`, and `docs/wiki-map.json`'s `covered`
  entry for `Lodestar.Stats` names only the public `Lodestar.Stats` namespace.
- A future package that needs an incomplete beta, an incomplete gamma, or a normal CDF makes its
  own case for a public special-functions surface; this decision is not a standing refusal of
  one, only of publishing this package's internals as a side effect of shipping ten tests.
- `tests/Lodestar.Stats.Tests/Oracles/StatsOracleAsserts.cs` is the one place the relative
  p-value tolerance is defined; a new stats test family uses it rather than inventing its own
  comparison.
