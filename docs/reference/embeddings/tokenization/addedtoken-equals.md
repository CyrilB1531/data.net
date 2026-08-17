# AddedToken.Equals

Value equality over the content, the id and every flag.

<!-- docs-declaration -->

```csharp
public bool Equals(AddedToken other)
```

**Parameters** — `other` is the token to compare against.

**Returns** — `bool`, true when the content, the id and all five flags agree.

**Example** — same content, different flag, not equal.

```csharp
using Lodestar.Embeddings.Tokenization;

var strict = new AddedToken("[MASK]", 103) { SingleWord = true };
var loose = new AddedToken("[MASK]", 103) { SingleWord = false };

bool same = strict.Equals(loose);  // => False
```

**Remarks** — the flags are part of the identity because they change what the token **matches**,
not merely how it is described. Two tokens with the same content and different `Lstrip` produce
different ids for the same text, so treating them as equal would make a vocabulary comparison say
"identical" about two models that tokenize differently.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`AddedToken`](addedtoken.md),
[`AddedToken.GetHashCode`](addedtoken-gethashcode.md).
