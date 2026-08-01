# PyTorch → .NET

**Verdict : utiliser l'existant, rien à écrire.** TorchSharp *est* libtorch (le
même moteur C++ que PyTorch), avec autograd, modules `nn` et CUDA.

| Besoin PyTorch | .NET recommandé |
|---|---|
| Tenseurs, autograd, entraînement, GPU | **TorchSharp** (`TorchSharp`) |
| Inférence d'un modèle pré-entraîné, sans entraîner | **ONNX Runtime** (`Microsoft.ML.OnnxRuntime`) |
| Modèles Keras/TF | TensorFlow.NET |

```bash
dotnet add package TorchSharp
dotnet add package TorchSharp-cpu     # ou libtorch-cuda-* pour le GPU
```

```csharp
using static TorchSharp.torch;

var x = randn(3, 4);
var w = randn(4, 2, requires_grad: true);
var y = x.matmul(w).relu().sum();
y.backward();                 // autograd, comme PyTorch
```

## Pièges

- **API très proche mais pas identique** : `torch.xxx` → `TorchSharp.torch.xxx`,
  nommage en PascalCase pour les modules `nn`. Le portage d'un script est
  mécanique mais pas automatique.
- **Gestion mémoire.** Les tenseurs natifs se libèrent via `using`/`Dispose` ou
  `DisposeScope` — pas de GC pour la mémoire native. Piège classique de fuite.
- **Inférence seule → ONNX.** Pour servir un modèle entraîné en Python, exporter
  en ONNX et charger avec ONNX Runtime : plus léger que TorchSharp. C'est la voie
  retenue par le lot 3 de `DataNet.Text` (embeddings).

_Guide à étoffer au fil des besoins réels._
