# PrecompiledNormalizer.Equals

Value equality over the charsmap.

<!-- docs-declaration -->

```csharp
public bool Equals(PrecompiledNormalizer other)
public bool Equals(object obj)
```

**Parameters** — `other` or `obj` is the normalizer to compare against.

**Returns** — `bool`, true when both were built from the same charsmap bytes.

**Example** — two normalizers from one model's blob.

<!-- docs-run: skip - a precompiled charsmap is a binary trie shipped inside a spiece.model, and model artifacts are never committed (CONTRIBUTING.md) -->

```csharp
using Lodestar.Embeddings.Tokenization;

// charsMap is the precompiled_charsmap blob from the model's own spiece.model.
byte[] charsMap = File.ReadAllBytes("spiece.model");

PrecompiledNormalizer first = PrecompiledNormalizer.FromCharsMap(charsMap);
PrecompiledNormalizer second = PrecompiledNormalizer.FromCharsMap(charsMap);

bool same = first.Equals(second);
```

**Remarks** — the **bytes** are compared, not the object identities, so two normalizers loaded from
the same model are equal. That is what makes "do these two vocabularies fold text the same way" a
question you can ask, which matters when checking a saved tokenizer against a model.

The `object` overload is present so the type behaves in collections that compare untyped.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`PrecompiledNormalizer.GetHashCode`](precompilednormalizer-gethashcode.md).
