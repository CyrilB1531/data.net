# Migrating from rapidfuzz — fuzzy matching

`DataNet.Fuzzy` reproduces `rapidfuzz.fuzz` and `rapidfuzz.process`, plus a
blocking deduplication.

```bash
dotnet add package DataNet.Fuzzy
```

## The `fuzz.*` ratios

All return a score in `[0, 100]`. Like rapidfuzz, **no preprocessing** by default
(case-sensitive).

| rapidfuzz | DataNet.Fuzzy |
|---|---|
| `fuzz.ratio(a, b)` | `Fuzz.Ratio(a, b)` |
| `fuzz.partial_ratio(a, b)` | `Fuzz.PartialRatio(a, b)` |
| `fuzz.token_sort_ratio(a, b)` | `Fuzz.TokenSortRatio(a, b)` |
| `fuzz.token_set_ratio(a, b)` | `Fuzz.TokenSetRatio(a, b)` |
| `fuzz.WRatio(a, b)` | `Fuzz.WRatio(a, b)` |

```csharp
using DataNet.Fuzzy;

Fuzz.Ratio("new york mets", "new york yankees");             // 65.0
Fuzz.TokenSortRatio("hello world", "world hello");           // 100.0 (order ignored)
Fuzz.PartialRatio("new york", "the wonderful new york mets");// 100.0 (substring)
Fuzz.WRatio("fuzzy wuzzy was a bear", "wuzzy fuzzy was a bear"); // 95.0
```

> Pitfall #1: `fuzz.ratio` is **not** Levenshtein — it's the **Indel** similarity
> ×100. `DataNet.Fuzzy` builds on `DataNet.Text`'s `Indel`.

## Finding the best candidate — `process`

```csharp
string[] choices = ["new york mets", "new york yankees", "boston red sox"];

// best candidates (default WRatio scorer), sorted, with a cutoff
IReadOnlyList<ExtractResult> top = Process.Extract("new york", choices, limit: 3, scoreCutoff: 50);

// the single best (or null)
ExtractResult? best = Process.ExtractOne("new york mets", choices);
```

Any scorer can be supplied: `Process.Extract(q, choices, scorer: Fuzz.Ratio)`.

## Deduplicating records (with blocking)

To avoid quadratic comparison, first partition by a **blocking key** (initial,
Soundex code, postal code…), then compare only within each block.

```csharp
string[] records = ["John Smith", "Jon Smith", "Jane Doe", "Jayne Doe", "Bob Brown"];

IReadOnlyList<IReadOnlyList<int>> clusters = Deduplicator.FindClusters(
    records,
    blockingKey: r => r[..1],                  // block = first letter
    similarity: Fuzz.TokenSetRatio,
    threshold: 80);

// clusters: { {0,1}, {2,3}, {4} } — indices of duplicates grouped together
```

Grouping is the **transitive closure** (union-find): if a~b and b~c, all three are
in the same cluster. Accepted trade-off: two true duplicates in different blocks
are never compared (recall ↓, speed ↑).
