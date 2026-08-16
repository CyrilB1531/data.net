# ReciprocalRank.Score

The mean of `1 / rank` over the queries, where `rank` is the position of the first relevant document.

**Not verified against a reference.** There is no `reciprocal` function in `sklearn.metrics` to
freeze a corpus from, so this member's definition is pinned by tests rather than by an oracle —
[decision 0036](../../../decisions/0036-a-member-may-ship-without-an-oracle-if-it-says-so.md) is the
rule that admits it, and says what would retire the exception.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> relevance, ReadOnlySpan<double> yScore, int labelCount)
```

**Parameters** — `relevance` says whether each document is relevant and `yScore` holds the scores
the ranking was made from, both row-major: one row per query, `labelCount` values each, and the same
length. Relevance is read as a judgement, not a magnitude: any non-zero value is relevant, and `3`
counts no more than `1`. `labelCount` is how many documents each row holds.

**Returns** — `double` in `[0, 1]`. `1` when every query puts a relevant document first, `0` when no
query retrieves one at all.

**Exceptions** — `ArgumentException` when `labelCount` is below `2`, when `relevance` and `yScore`
disagree in length, or when the length is not a whole number of rows of `labelCount`.

**Example** — two queries, the first relevant document second and then first.

```csharp
using Lodestar.Metrics;

double[] relevance = [0, 1, 0, 0, 1, 0, 0, 0];
double[] scores = [0.9, 0.5, 0.4, 0.1, 0.9, 0.5, 0.4, 0.1];

double mrr = ReciprocalRank.Score(relevance, scores, labelCount: 4);  // => 0.75
```

**Remarks** — the definition, in the three clauses the tests pin one by one: the reciprocal of the
rank of the **first** relevant document, averaged over queries, with a query holding no relevant
document contributing `0` rather than being dropped from the average. That last clause is the one
implementations disagree about — dropping such queries raises the score and makes two runs over
different query sets incomparable.

Everything after the first relevant document is invisible to this number, which is what makes it the
wrong metric when the reader consumes the whole list. Report it beside
[`Ndcg.Score`](ndcg-score.md), not instead of one.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Ndcg.Score`](ndcg-score.md), [`Dcg.Score`](dcg-score.md), the
[Python equivalence table](../../../equivalence.md).
