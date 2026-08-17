# AddedToken.GetHashCode

A hash consistent with that equality.

<!-- docs-declaration -->

```csharp
public int GetHashCode()
```

**Returns** — `int`, over the content, the id and the flags.

**Example** — equal tokens hash alike.

```csharp
using Lodestar.Embeddings.Tokenization;

var first = new AddedToken("[MASK]", 103) { Special = true };
var second = new AddedToken("[MASK]", 103) { Special = true };

bool consistent = first.Equals(second) && first.GetHashCode() == second.GetHashCode();  // => True
```

**Remarks** — every field that [`Equals`](addedtoken-equals.md) reads contributes, which is what
the contract requires and what makes an `AddedToken` safe as a dictionary key or in a set — the
shape a vocabulary's added-token list is usually searched as.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`AddedToken.Equals`](addedtoken-equals.md).
