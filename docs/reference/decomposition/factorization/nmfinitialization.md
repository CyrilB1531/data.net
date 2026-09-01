# NmfInitialization

Where a non-negative matrix factorization starts from, and — because multiplicative updates are
multiplicative — most of what decides the shape of where it ends up.

An update multiplies every entry of `W` and `H` by a non-negative ratio, so a zero stays a zero
for as long as the loop runs. The initialisation is therefore the only thing that can put a zero
in the answer, and choosing between the two members below is choosing between a sparse
factorization and a dense one, not between two ways of seeding the same result.

Both members are NNDSVD — the non-negative double SVD of Boutsidis and Gallopoulos, which takes
the leading singular triplets of the matrix and splits each one into its positive and its negative
part, keeping whichever carries more energy. Nothing about it is random once Ω is fixed, which is
why an NMF fit is reproducible in a way its Python counterpart's `random` initialisation is not.

## Members

| Member | Value | What it does |
| --- | --- | --- |
| `NndSvd` | `0` | Leaves the zeros NNDSVD produces in place. scikit-learn's `nndsvd`, and the choice when a sparse factorization is the point. |
| `NndSvda` | `1` | Fills each zero with the mean of every cell of the matrix, zeros included. scikit-learn's `nndsvda`: denser, and it converges in fewer iterations because no entry is frozen at zero. |

Neither is a tuning knob to sweep. `NndSvd` is the one that answers "which words does this
component *not* contain"; `NndSvda` is the one to reach for when the factorization is a
compression rather than a description, and the zeros are an accident of the corpus.

## Why `nndsvdar` is not here

scikit-learn offers a third member, `nndsvdar`, which fills the zeros with small draws from
numpy's own normal stream instead of the mean. This package does not, because it does not
reproduce `RandomState` — see [ADR 0072](../../../decisions/0072-omega-is-an-input-not-a-seed.md)
for the rule Ω follows and why a seed is not portable across the two ecosystems. An
initialisation that could not be compared entry by entry against the reference is one no oracle
can pin, and this package ships nothing it cannot check.

`NndSvda` is what `nndsvdar` approximates: the same fill, without the noise. If you were reaching
for `nndsvdar` to break ties between identical rows, the noise is doing that job and a different
Ω does it too.
