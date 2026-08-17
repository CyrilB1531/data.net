# SpecialTokenTemplate.Equals

Value equality over the prefix, the suffix and the pad token.

<!-- docs-declaration -->

```csharp
public bool Equals(SpecialTokenTemplate other)
```

**Parameters** — `other` is the template to compare against.

**Returns** — `bool`, true when both wrap a sequence identically.

**Example** — the ready-made templates differ.

```csharp
using Lodestar.Embeddings.Tokenization;

bool same = SpecialTokenTemplate.Bert.Equals(SpecialTokenTemplate.None);  // => False
```

**Remarks** — the token **lists** are compared element by element rather than by reference, which
a `record`'s synthesised equality would do and which would report two identical templates as
different.

Comparing templates is how to check that a saved encoder and a model still agree — the kind of
check worth doing once at start-up rather than discovering through bad vectors.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SpecialTokenTemplate`](specialtokentemplate.md).
