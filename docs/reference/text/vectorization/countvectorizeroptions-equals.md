# CountVectorizerOptions.Equals

Value equality, comparing the stop words as a set rather than as a sequence.

<!-- docs-declaration -->

```csharp
public bool Equals(CountVectorizerOptions other)
```

**Parameters** — `other` is the options object to compare against.

**Returns** — `bool`, true when every setting matches and the two stop-word sets hold the same
words.

**Example** — the same words in a different order are the same options.

```csharp
using Lodestar.Text.Vectorization;

var first = new CountVectorizerOptions { StopWords = new HashSet<string> { "the", "and" } };
var second = new CountVectorizerOptions { StopWords = new HashSet<string> { "and", "the" } };

bool same = first.Equals(second);  // => True
```

**Remarks** — a `record`'s synthesised equality would compare `StopWords` by reference, so two
options objects carrying identical words would be unequal — the answer nobody wants and the reason
this is hand-written. `SetEquals` is what it uses, so order does not matter and neither does
duplication: `["the", "the"]` equals `["the"]`.

That has a consequence worth knowing, and it is stated on
[`GetHashCode`](countvectorizeroptions-gethashcode.md): because equal objects must hash alike, the
hash cannot use the stop-word **count**, since two equal sets can differ in it.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CountVectorizerOptions`](countvectorizeroptions.md),
[`CountVectorizerOptions.GetHashCode`](countvectorizeroptions-gethashcode.md).
