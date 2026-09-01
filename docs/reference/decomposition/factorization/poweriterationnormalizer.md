# PowerIterationNormalizer

What a power iteration does to its block between the two products, and therefore part of the
answer rather than an implementation detail.

A power iteration multiplies the probe block by `A` and then by `Aᵀ`, which sharpens the spectrum
— and, left alone, collapses every column of the block onto the leading singular vector, in
double precision within a handful of iterations. Re-orthogonalizing between the products is what
stops that. Which factorization does the re-orthogonalizing changes the numbers that come out, so
it is a setting on [`TruncatedSvdOptions`](truncatedsvdoptions.md) and never a decision the
implementation makes quietly.

## Members

| Member | Value | What it does |
| --- | --- | --- |
| `Auto` | `0` | `None` below three power iterations, `Lu` at or above. scikit-learn's rule, and — since its own default is five iterations — the reason the default path is the LU one. |
| `None` | `1` | Nothing between the products. Cheapest, and adequate only for one or two iterations. |
| `Qr` | `2` | An economic QR, by Householder reflections. The most accurate and the most expensive. |
| `Lu` | `3` | LU with partial pivoting, keeping `P L`. Its columns are not orthonormal, which is the accuracy it trades away for being cheaper. |

`None` past two iterations is not a saving, it is a wrong answer: the block loses rank to rounding
and the components it produces are approximately parallel. If five iterations are too slow, lower
[`PowerIterations`](truncatedsvdoptions.md) rather than turning the normalizer off.
