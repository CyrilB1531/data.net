# NumPy → .NET

**Verdict : utiliser l'existant.** L'algèbre dense de NumPy s'appuie sur BLAS/
LAPACK ; on ne réécrit pas ça. On combine deux briques .NET selon le besoin.

| Besoin NumPy | .NET recommandé |
|---|---|
| Vecteurs/matrices, décompositions, résolution linéaire | **Math.NET Numerics** (`MathNet.Numerics`), + fournisseur natif MKL/OpenBLAS pour la perf |
| Opérations élémentaires vectorisées (SIMD) | **`System.Numerics.Tensors`** (`TensorPrimitives`) |
| API « façon NumPy » (ergonomie de migration) | **NumSharp** — pratique, mais moins mûr ; à réserver au confort de portage |

```bash
dotnet add package MathNet.Numerics
dotnet add package MathNet.Numerics.MKL.Win-x64   # ou .Linux-x64 : accélération native
```

```csharp
using MathNet.Numerics.LinearAlgebra;

var a = Matrix<double>.Build.DenseOfArray(new[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
var b = Vector<double>.Build.Dense(new[] { 1.0, 1.0 });
Vector<double> x = a.Solve(b);   // résout a·x = b
```

## Pièges

- **Broadcasting.** Pas d'équivalent implicite universel : on l'écrit
  explicitement, ou on passe par `TensorPrimitives` pour l'élémentaire.
- **`dtype` / vues.** Math.NET est fortement typé (`double`, `float`,
  `Complex`) ; pas de vues à coût nul façon NumPy — les tranches copient souvent.
- **Aléatoire.** `MathNet.Numerics.Random` ≠ générateurs NumPy : ne pas attendre
  une reproductibilité croisée des tirages.

_Guide à étoffer au fil des besoins réels._
