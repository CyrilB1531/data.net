# ExtractResult

One candidate's choice, score and index.

<!-- docs-declaration -->

```csharp
public readonly record struct ExtractResult
```

**Properties** — `Choice` is the candidate string, `Score` its similarity in `[0, 100]`, and
`Index` its position in the list that was searched.

**Example** — the best of several candidates, traced back to its row.

```csharp
using Lodestar.Fuzzy;

string[] choices = ["apple pie", "apple tart", "banana bread", "cherry pie"];

ExtractResult? best = Process.ExtractOne("appel pie", choices);

string choice = best?.Choice;  // => apple pie
int index = best?.Index ?? -1;  // => 0
```

**Remarks** — `Index` is what makes the result usable. Candidates usually come from a list of
records rather than a list of strings, and the score alone cannot say **which record** matched —
matching the string back by value fails the moment two records share one.

A `readonly record struct`, so it is copied rather than referenced and compares by value.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Process.Extract`](process-extract.md),
[`Process.ExtractOne`](process-extractone.md).
