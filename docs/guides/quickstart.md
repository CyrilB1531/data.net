# Quickstart

Compare two strings in a few lines.

## Install

```bash
dotnet add package DataNet.Text
```

> The package has **no external dependencies on `net10.0`**: it's pure .NET, with
> no Python at runtime. On `netstandard2.0` it takes `System.Memory`,
> `System.Numerics.Vectors` and `System.Text.Json` — all three in-box on the
> modern target, so nothing new is actually being pulled in for consumers there.

## Compare two strings

```csharp
using DataNet.Text.Distances;

// Raw edit distance: number of insertions/deletions/substitutions.
int d = Levenshtein.Distance("kitten", "sitting");     // 3

// Normalized similarity in [0, 1]: 1 = identical.
double sim = Levenshtein.NormalizedSimilarity("kitten", "sitting"); // 0.5714…

// Normalized distance: 1 - similarity.
double nd = Levenshtein.NormalizedDistance("kitten", "sitting");    // 0.4286…
```

`string` literals convert implicitly to `ReadOnlySpan<char>`, so no buffer is
allocated for the inputs.

## Unicode: choosing the comparison unit

By default, comparison is over **UTF-16 units** (`char`) — the native .NET choice
and the fastest. To reproduce Python / rapidfuzz results *exactly* on characters
outside the Basic Multilingual Plane (emoji, rare ideographs), request **code
point** comparison:

```csharp
using DataNet.Text;   // TextElement lives here, not in .Distances

// "a😀" -> "a": the emoji is ONE code point, but TWO UTF-16 units.
Levenshtein.Distance("a\U0001F600", "a");                        // 2 (UTF-16 units)
Levenshtein.Distance("a\U0001F600", "a", TextElement.CodePoint); // 1 (like Python)
```

This is Unicode pitfall #1 when porting from Python; it's documented in detail in
[`../decisions/0002-unicode-comparison-unit.md`](../decisions/0002-unicode-comparison-unit.md).

## Next

- [From string to vector](vectorization.md) — bag of words, TF-IDF, cosine
- [Semantic search with embeddings](embeddings.md)
- [Migrating from rapidfuzz](migrating-from-rapidfuzz.md)
- [Python → C# equivalence table](../equivalence.md)
