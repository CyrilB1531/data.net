# Process.Extract

The best candidates, ranked.

<!-- docs-declaration -->

```csharp
public static IReadOnlyList<ExtractResult> Extract(string query, IEnumerable<string> choices, Func<string, string, double> scorer = null, int? limit = 5, double scoreCutoff = 0)
```

**Parameters** — `query` is what to match. `choices` are the candidates. `scorer` is the scoring
function, [`Fuzz.WRatio`](fuzz-wratio.md) when omitted. `limit` caps how many come back, `5` by
default and `null` for all of them. `scoreCutoff` drops anything scoring below it.

**Returns** — `IReadOnlyList<ExtractResult>`, best first, at most `limit` long.

**Example** — the two best of four candidates.

```csharp
using Lodestar.Fuzzy;

string[] choices = ["apple pie", "apple tart", "banana bread", "cherry pie"];

IReadOnlyList<ExtractResult> best = Process.Extract("apple pie", choices, limit: 2);

int returned = best.Count;  // => 2
string first = best[0].Choice;  // => apple pie
int where = best[0].Index;  // => 0
```

**Remarks** — the default `limit` of `5` is rapidfuzz's, and it is a **cap rather than a
guarantee**: fewer come back when fewer clear the cutoff, and passing `null` returns every
candidate ranked, which on a large list is the expensive call.

`scoreCutoff` is worth setting rather than filtering afterwards: it lets the scorer abandon a
candidate early, so it is faster as well as shorter.

Each result carries its `Index`, which is how a match is traced back to the record it came from
rather than to the string.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Process.ExtractOne`](process-extractone.md),
[`ExtractResult`](extractresult.md).
