# NumPy → .NET

**Verdict: use what exists.** NumPy's dense algebra relies on BLAS/LAPACK; we
don't rewrite that. We combine two .NET building blocks as needed.

| NumPy need | Recommended .NET |
| --- | --- |
| Vectors/matrices, decompositions, linear solves | **Math.NET Numerics** (`MathNet.Numerics`), + native MKL/OpenBLAS provider for performance |
| Element-wise vectorized ops (SIMD) | **`System.Numerics.Tensors`** (`TensorPrimitives`) |
| "NumPy-like" API (migration comfort) | **NumSharp** — handy, but less mature; reserve for porting convenience |

**Two decompositions are the exception, and they ship here.** The row above still holds for a
general dense linear-algebra need — Math.NET is the answer for a solve, an eigendecomposition or
a full SVD of a dense matrix. It is not the answer for a _sparse_ truncated SVD or for
non-negative matrix factorization, which `Lodestar.Decomposition` writes natively over a
`CsrMatrix` at scikit-learn parity: Math.NET 5.0.0 dates from April 2022 and has shipped nothing
but a beta in the four years since, its sparse SVD request has been open since 2013, and
densifying a term-document matrix to reach `Svd()` is the cost the sparse representation existed
to avoid. See [`docs/guides/decomposition.md`](../guides/decomposition.md).

```bash
dotnet add package MathNet.Numerics
dotnet add package MathNet.Numerics.MKL.Win-x64   # or .Linux-x64: native acceleration
```

```csharp
using MathNet.Numerics.LinearAlgebra;

var a = Matrix<double>.Build.DenseOfArray(new[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
var b = Vector<double>.Build.Dense(new[] { 1.0, 1.0 });
Vector<double> x = a.Solve(b);   // solves a·x = b
```

## Pitfalls

- **Broadcasting.** No universal implicit equivalent: write it explicitly, or use
  `TensorPrimitives` for element-wise work.
- **`dtype` / views.** Math.NET is strongly typed (`double`, `float`, `Complex`);
  no zero-cost views like NumPy — slices often copy.
- **Randomness.** `MathNet.Numerics.Random` ≠ NumPy generators: don't expect
  cross-reproducible draws.

_Guide to be expanded as real needs arise._
