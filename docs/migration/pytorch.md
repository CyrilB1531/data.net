# PyTorch → .NET

**Verdict: use what exists, nothing to write.** TorchSharp *is* libtorch (the same
C++ engine as PyTorch), with autograd, `nn` modules and CUDA.

| PyTorch need | Recommended .NET |
| --- | --- |
| Tensors, autograd, training, GPU | **TorchSharp** (`TorchSharp`) |
| Inference of a pretrained model, without training | **ONNX Runtime** (`Microsoft.ML.OnnxRuntime`) |
| Keras/TF models | TensorFlow.NET |

```bash
dotnet add package TorchSharp
dotnet add package TorchSharp-cpu     # or libtorch-cuda-* for GPU
```

```csharp
using static TorchSharp.torch;

var x = randn(3, 4);
var w = randn(4, 2, requires_grad: true);
var y = x.matmul(w).relu().sum();
y.backward();                 // autograd, like PyTorch
```

## Pitfalls

- **Very close API but not identical**: `torch.xxx` → `TorchSharp.torch.xxx`,
  PascalCase naming for `nn` modules. Porting a script is mechanical but not
  automatic.
- **Memory management.** Native tensors are freed via `using`/`Dispose` or a
  `DisposeScope` — there is no GC for native memory. A classic leak pitfall.
- **Inference only → ONNX.** To serve a Python-trained model, export to ONNX and
  load with ONNX Runtime: lighter than TorchSharp. That is the path taken by
  `DataNet.Embeddings`.

*Guide to be expanded as real needs arise.*
