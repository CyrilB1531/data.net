# TextRankOptions

What `TextRank` is built with.

<!-- docs-declaration -->

```csharp
public sealed record TextRankOptions
```

**Properties** — `StopWords` (default `null`, which takes `StopWords.English`) is dropped before
the graph is built. `Window` (default `2`) is how many tokens share a co-occurrence window; `2`
pairs adjacent tokens only. `Damping` (default `0.85`) is the random-surfer damping of the
reference implementation. `Tolerance` (default `1e-12`) is this implementation's own convergence
bound — summa solves the eigenproblem outright and has no tolerance to expose.
`MaxIterations` (default `1_000`) is how many power-iteration steps `Extract` runs before giving up
rather than return a half-ranked vector. `Ratio` (default `0.2`) is what proportion of ranked words
to keep, ignored when `Words` is set. `Words` (default `null`) overrides `Ratio` with an exact
count. `TokenPattern` (default `\b\w+\b`) is what counts as a word.

**Example** — `Words` overriding `Ratio` on the same document.

```csharp
using Lodestar.Text.Keywords;

string doc =
    "Compatibility of systems of linear constraints over the set of natural numbers. " +
    "Criteria of compatibility of a system of linear Diophantine equations.";

var byRatio = new TextRank(new TextRankOptions { Ratio = 0.2 }).Extract(doc);
var byCount = new TextRank(new TextRankOptions { Ratio = 0.2, Words = 5 }).Extract(doc);

bool overridden = byRatio.Count != byCount.Count;  // => True
```

**Remarks** — `Tolerance` and `MaxIterations` have no counterpart in summa, which calls
`scipy.linalg.eig` rather than iterating: this implementation's own power iteration is what makes
`Tolerance` and `MaxIterations` parameters at all, and their defaults are tight enough that the
frozen oracle corpus agrees with summa's closed-form ranking to `1e-12`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TextRank`](textrank.md), [`KeywordMatch`](keywordmatch.md),
the [Python equivalence table](../../../equivalence.md).
