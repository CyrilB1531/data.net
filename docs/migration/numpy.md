# NumPy → .NET

**Verdict: use what exists.** NumPy's dense algebra relies on BLAS/LAPACK; we
don't rewrite that. We combine two .NET building blocks as needed.

| NumPy need | Recommended .NET |
| --- | --- |
| Vectors/matrices, decompositions, linear solves | **Math.NET Numerics** (`MathNet.Numerics`), + native MKL/OpenBLAS provider for performance |
| Element-wise vectorized ops (SIMD) | **`System.Numerics.Tensors`** (`TensorPrimitives`) |
| "NumPy-like" API (migration comfort) | **NumSharp** — handy, but less mature; reserve for porting convenience |

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
