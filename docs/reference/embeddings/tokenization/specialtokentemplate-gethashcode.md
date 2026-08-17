# SpecialTokenTemplate.GetHashCode

A hash consistent with that equality.

<!-- docs-declaration -->

```csharp
public int GetHashCode()
```

**Returns** — `int`, over the prefix tokens, the suffix tokens and the pad token.

**Example** — the same template hashes alike.

```csharp
using Lodestar.Embeddings.Tokenization;

SpecialTokenTemplate template = SpecialTokenTemplate.Bert;

bool equal = template.Equals(SpecialTokenTemplate.Bert);  // => True
bool hashesAlike = template.GetHashCode() == SpecialTokenTemplate.Bert.GetHashCode();  // => True
```

**Remarks** — the token contents contribute, not the list identities, so a template rebuilt from
the same strings hashes to what the ready-made one does. That is what makes a template usable as a
dictionary key when caching encoders per model.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`SpecialTokenTemplate.Equals`](specialtokentemplate-equals.md).
