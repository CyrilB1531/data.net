# PrecompiledNormalizer.GetHashCode

A hash consistent with that equality.

<!-- docs-declaration -->

```csharp
public int GetHashCode()
```

**Returns** — `int`, derived from the charsmap bytes.

**Example** — equal normalizers hash alike.

<!-- docs-run: skip - a precompiled charsmap is a binary trie shipped inside a spiece.model, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Tokenization;

// charsMap is the precompiled_charsmap blob from the model's own spiece.model.
byte[] charsMap = File.ReadAllBytes("spiece.model");

PrecompiledNormalizer first = PrecompiledNormalizer.FromCharsMap(charsMap);
PrecompiledNormalizer second = PrecompiledNormalizer.FromCharsMap(charsMap);

bool consistent = first.Equals(second) && first.GetHashCode() == second.GetHashCode();
```

**Remarks** — the same bytes that [`Equals`](precompilednormalizer-equals.md) compares feed the
hash, which is the contract. A charsmap is hundreds of kilobytes, so this is not free — cache the
normalizer rather than rebuilding it, which is what a vocabulary already does by holding one.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`PrecompiledNormalizer.Equals`](precompilednormalizer-equals.md).
