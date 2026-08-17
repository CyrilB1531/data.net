# Process

One query against many candidates.

<!-- docs-declaration -->

```csharp
public static class Process
```

**Example** — the two best of four.

```csharp
using Lodestar.Fuzzy;

string[] choices = ["apple pie", "apple tart", "banana bread", "cherry pie"];

IReadOnlyList<ExtractResult> best = Process.Extract("apple pie", choices, limit: 2);

int returned = best.Count;  // => 2
double top = best[0].Score;  // => 100
```

**Remarks** — the equivalent of rapidfuzz's `process.extract` and `process.extractOne`, scoring
with [`Fuzz.WRatio`](fuzz-wratio.md) unless a scorer is passed. Which scorer is the decision that
matters most, and [the matching index](../matching.md) is where that decision is laid out.

Both members take a `scoreCutoff`, and it is not merely a filter: candidates below it are dropped,
so `ExtractOne` can return **nothing**.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Fuzz`](fuzz.md), [`ExtractResult`](extractresult.md),
[`Deduplicator`](deduplicator.md).

## Members

| Member | What it does |
| --- | --- |
| [`Process.Extract`](process-extract.md) | The best candidates, ranked. |
| [`Process.ExtractOne`](process-extractone.md) | The single best, or nothing. |
