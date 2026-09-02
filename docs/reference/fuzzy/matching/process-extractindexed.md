# Process.ExtractIndexed

A `BkTree` prefilter in front of `Extract`.

<!-- docs-declaration -->

```csharp
public static IReadOnlyList<ExtractResult> ExtractIndexed(string query, BkTree index, int maxDistance, Func<string, string, double> scorer = null, int? limit = 5, double scoreCutoff = 0)
```

**Parameters** — `query` is what to match. `index` is a tree already holding the choices.
`maxDistance` is the tree's radius, in that tree's own metric. `scorer` defaults to
[`Fuzz.WRatio`](fuzz-wratio.md). `limit` caps how many come back, `5` by default and `null` for
all of them. `scoreCutoff` drops anything scoring below it.

**Returns** — `IReadOnlyList<ExtractResult>`, ranked exactly as `Extract` ranks its own choices.

**Example** — every choice within edit distance 1 of `"book"`.

```csharp
using Lodestar.Fuzzy;
using Lodestar.Text.Indexing;

BkTree index = BkTree.OverLevenshtein();
index.AddRange(["book", "books", "boo", "cook", "cake"]);

IReadOnlyList<ExtractResult> best = Process.ExtractIndexed("book", index, maxDistance: 1);

int found = best.Count;  // => 4
string top = best[0].Choice;  // => book
double topScore = best[0].Score;  // => 100
```

**Remarks** — this is a prefilter, not a faster `Extract`: it scores only the choices `index` puts
within `maxDistance`, so it returns what `Extract` returns over the same choices **only if** every
choice further away would have scored below `scoreCutoff`. The tree filters on an integer edit
distance; the default `WRatio` scorer is a similarity in `[0, 100]` that is not a function of that
distance, so a caller who leaves `scoreCutoff` at its default of `0` gets a subset of `Extract`'s
result, silently. `cake` is one such choice above: two edits away from `"book"`, so the tree drops
it before `WRatio` ever sees it.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Process.Extract`](process-extract.md), [`ExtractResult`](extractresult.md).
