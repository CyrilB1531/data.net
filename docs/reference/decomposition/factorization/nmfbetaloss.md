# NmfBetaLoss

What [`Nmf.Fit`](nmf-fit.md) is asked to minimise, and therefore what "a good factorization" means
for the data in hand.

The beta divergence is one family with a parameter; this package ships the two members of it that
`solver="mu"` computes in closed form. They are not two ways of reaching one answer. Each is the
maximum-likelihood fit under a different noise model, so they disagree about which errors are
worth trading against which — and a matrix of counts and a matrix of measurements do not want the
same trade.

## Members

| Member | Value | What it does |
| --- | --- | --- |
| `Frobenius` | `0` | Half the squared Frobenius norm of `X − W H`, `β = 2`. The Gaussian noise model, and what a matrix of continuous measurements wants. scikit-learn's `beta_loss="frobenius"`. |
| `KullbackLeibler` | `1` | The generalised Kullback–Leibler divergence, `β = 1`. The Poisson noise model, and what counts want — a term-document matrix included, which is why it is here. scikit-learn's `beta_loss="kullback-leibler"`. |

The choice changes the arithmetic and not only the objective. Frobenius updates `W` through two
dense products; Kullback–Leibler divides `X` by `W H` where `X` is non-zero and broadcasts a row
of sums as the denominator, which is why it is the slower of the two on a wide matrix and why it
snaps `H` below machine epsilon to zero where Frobenius does not.

`ReconstructionError` is reported in the loss that produced it, so numbers from two different
members are not comparable — a Kullback–Leibler fit of a corpus is routinely the "larger" of the
two and is not the worse one. Compare a loss against itself, across ranks or across
initialisations, and never across this enum.

Other β values — Itakura–Saito at `β = 0`, or anything between — are not offered. They cost a
general power in the inner loop, and neither of the two data shapes this package targets asks for
one.
