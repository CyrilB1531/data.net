# CountVectorizerOptions.GetHashCode

A hash consistent with that equality, computed in constant time.

<!-- docs-declaration -->

```csharp
public int GetHashCode()
```

**Returns** — `int`, a hash of the scalar settings plus whether stop words are present at all.

**Example** — equal options hash alike, which is the contract.

```csharp
using Lodestar.Text.Vectorization;

var first = new CountVectorizerOptions { StopWords = new HashSet<string> { "the", "and" } };
var second = new CountVectorizerOptions { StopWords = new HashSet<string> { "and", "the" } };

bool consistent = first.Equals(second) && first.GetHashCode() == second.GetHashCode();  // => True
```

**Remarks** — the stop-word set contributes only **whether it is present**, and deliberately not
its contents or its count. Its count cannot be used: `Equals` compares as a set, so `["the",
"the"]` equals `["the"]` while the counts differ, and equal objects are required to hash alike.
Hashing the words themselves would mean hashing all of them, order-independently, which is the
O(n) this exists to avoid on a type that may carry a 318-word list.

Unequal options are allowed to collide, and two option sets differing only in their stop words
will. That is the correct trade for a type used as a dictionary key occasionally and constructed
often.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizerOptions`](countvectorizeroptions.md),
[`CountVectorizerOptions.Equals`](countvectorizeroptions-equals.md), [`StopWords`](stopwords.md).
