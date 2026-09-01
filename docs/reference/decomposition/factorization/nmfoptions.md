# NmfOptions

What [`Nmf.Fit`](nmf-fit.md) is allowed to vary. Every property has an initialiser, so
`new NmfOptions()` is scikit-learn's own default configuration and you set only what you are
changing.

## Properties

| Property | Default | What it does |
| --- | --- | --- |
| `BetaLoss` | `Frobenius` | What the factorization minimises — see [`NmfBetaLoss`](nmfbetaloss.md). |
| `Initialization` | `NndSvd` | Where the iteration starts — see [`NmfInitialization`](nmfinitialization.md). Ignored by the overload handed `W₀` and `H₀`. |
| `MaxIterations` | `200` | The iteration cap, and the exact iteration count when `Tolerance` is zero. scikit-learn's default is the same 200. |
| `Tolerance` | `1e-4` | The relative improvement below which the loop stops, measured every tenth iteration. Zero disables the stop. |
| `Seed` | `0` | Seeds this package's generator for the initialisation's Ω when `RandomMatrix` is null. It reproduces a run of Lodestar, never a run of NumPy. |
| `RandomMatrix` | `null` | Ω itself, row-major and `FeatureCount × (componentCount + 10)`. Given, it replaces the draw entirely. |

**The ten in that shape is not this type's to choose.** NNDSVD reaches for the randomized SVD with
scikit-learn's own defaults rather than the ones
[`TruncatedSvdOptions`](truncatedsvdoptions.md) exposes, and ten oversamples is one of them — so
the block an initialisation wants is wider than the rank by exactly that, and an Ω of any other
length is refused rather than silently reshaped. See
[ADR 0072](../../../decisions/0072-omega-is-an-input-not-a-seed.md) for why Ω is an input at all.

**`Tolerance = 0` is a feature, not a way to disable a safety net.** With the stop off,
`MaxIterations` stops being a cap and becomes the number of updates that will run, which is what
makes two implementations comparable: an early stop that fires one check apart leaves two correct
runs disagreeing on every digit. It is also the slowest setting, since nothing can end the loop
early.

`MaxIterations` below one is refused, and so is a negative or `NaN` `Tolerance`. A `BetaLoss` and
an `Initialization` are enums, and a value outside either falls back to nothing: the loss that is
not `KullbackLeibler` is Frobenius, and the initialisation that is not `NndSvda` leaves the zeros
alone.
