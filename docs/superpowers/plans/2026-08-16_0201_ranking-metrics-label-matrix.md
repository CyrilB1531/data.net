# 0201 — Ranking metrics, lot 2: implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Spec:** [`2026-08-16_0201_ranking-metrics-label-matrix.md`](../specs/2026-08-16_0201_ranking-metrics-label-matrix.md) ·
**Issues:** [#201](https://github.com/CyrilB1531/lodestar/issues/201), which closes
[#173](https://github.com/CyrilB1531/lodestar/issues/173) ·
**Branch:** `feat/201-ranking-metrics-label-matrix`

**Goal:** `Lodestar.Metrics` scores a boolean label matrix — `LabelRankingAveragePrecision`,
`CoverageError` and `LabelRankingLoss` at scikit-learn parity.

**Architecture:** one rank function serves all three. Measured against scikit-learn 1.9.0, `lrap`
ranks with `rankdata(-y_score, "max")`, which is the 1-based rank of each label with the best score
first and every member of a tied group taking the group's **worst** rank. That is exactly
`rank[j] = |{k : score[k] >= score[j]}|`, computed with no permutation at all — sort a copy of the
scores ascending, then take `labelCount − lowerBound(sorted, score[j])`. Coverage is the largest
such rank over the relevant labels; the loss counts wrongly ordered pairs directly. No sort of
*indices* appears anywhere, which is why the tie order cannot be observed.

**Tech Stack:** C# on `net10.0` and `netstandard2.0`, xunit, frozen JSON oracles from scikit-learn
1.9.0 via `tools/generate_oracles.py`.

## Global Constraints

- English everywhere; no `feat:`/`fix:` prefix on commit subjects; the closing keywords go in the
  pull-request body only.
- Warnings are errors on both target frameworks, with SonarAnalyzer running in the build.
- Comment budgets: two lines inline, eight of prose in XML documentation. **This branch installs no
  `long-comment:` marker** — lot 1 removed the two it had written after measuring that both blocks
  fit without them ([#187](https://github.com/CyrilB1531/lodestar/issues/187)).
- Oracles generated from a neutral directory with `PYTHONSAFEPATH=1`, reading **the generator's own
  exit code**, never a pipeline's.
- The four guards see only tracked files: `git add -N` a new file before running them. Never
  `git checkout --` a file `git status` shows as `A` — the intent-to-add blob is empty and the
  checkout truncates the file.
- Every new public type exercised from `samples/Lodestar.Sample/Lot5Metrics.cs` — the packaging gate.
- A member page per method under `docs/reference/metrics/ranking/`, with its type page and the index
  updated. `covered` already names that directory; nothing to add there.
- `dotnet format` runs **once, at the end**, not per task.
- **A code review before the pull request exists.** On lot 1 it found eight defects the gates could
  not: the gates check declarations and replay a corpus, and none of them reads the arithmetic.

## File Structure

| File | Responsibility |
| --- | --- |
| `src/Lodestar.Metrics/Internal/LabelRanking.cs` | create — the max rank of a row, the relevant count, and the shared validation. Shaped so [#210](https://github.com/CyrilB1531/lodestar/issues/210) extends it rather than copying it. |
| `src/Lodestar.Metrics/CoverageError.cs` | create — the public type, one `Score`. |
| `src/Lodestar.Metrics/LabelRankingLoss.cs` | create — the public type, one `Score`. |
| `src/Lodestar.Metrics/LabelRankingAveragePrecision.cs` | create — the public type, one `Score`. |
| `tools/generate_oracles.py` | modify — a `generate_label_ranking` beside `generate_ranking` (line 2152), registered in the map at line 4935. |
| `tests/oracles/label_ranking.json` | create — generated, never hand-edited. |
| `tests/Lodestar.Metrics.Tests/MetricsCorpus.cs` | modify — add a `Bools` reader beside `Ints`. |
| `tests/Lodestar.Metrics.Tests/LabelRankingTests.cs` | create — the corpus replay, one theory row per fixture. |
| `tests/Lodestar.Metrics.Tests/LabelRankingFactsTests.cs` | create — what the corpus cannot state. |
| `docs/reference/metrics/ranking.md` | modify — three rows in the type table, and the prose that separates this family from lot 1's. |
| `docs/reference/metrics/ranking/*.md` | create — three type pages, three member pages. |
| `docs/equivalence.md` | modify — one row per function in the ranking section. |
| `samples/Lodestar.Sample/Lot5Metrics.cs` | modify — extend `Ranking()` with the three. |
| `CHANGELOG.md` | modify — under the unreleased `Lodestar.Metrics`, `#### Added — ranking`. |

**Not in this plan, deliberately:** no `NaN` guard on `yScore`. scikit-learn refuses it through
`check_array`, and no member of `Lodestar.Metrics` checks for one today — adding it here alone would
make this family inconsistent with the other 42. If it is worth having it is worth having
everywhere, which is an issue rather than a step.

---

## Task 1 — the frozen corpus

**Files:** modify `tools/generate_oracles.py`; create `tests/oracles/label_ranking.json`.

**Produces:** a corpus whose cases carry `name`, `y_true` (flat `0`/`1`), `y_score` (flat),
`label_count`, `sample_weight` (or `null`), and the three values `lrap`, `coverage`, `ranking_loss`.

- [x] **Step 1: Write `_label_ranking_fixtures()` beside `_ranking_fixtures`.** One corpus for the
  three metrics, because they take the same two inputs — a file each would triple the fixtures to
  say the same thing. The fixtures, chosen to separate implementations rather than to exercise them:

```python
def _label_ranking_fixtures() -> list[dict]:
    """Rows where a plausible implementation and the reference part company."""
    wide = [0] * 20
    for j in (0, 9, 19):
        wide[j] = 1
    return [
        {"name": "the worked case", "true": [[1, 0, 0], [0, 0, 1]],
         "score": [[0.75, 0.5, 1.0], [1.0, 0.2, 0.1]], "weight": None},
        {"name": "the worked case, weighted", "true": [[1, 0, 0], [0, 0, 1]],
         "score": [[0.75, 0.5, 1.0], [1.0, 0.2, 0.1]], "weight": [1.0, 2.0]},
        {"name": "every label relevant", "true": [[1, 1, 1]],
         "score": [[0.7, 0.2, 0.1]], "weight": None},
        {"name": "no label relevant", "true": [[0, 0, 0]],
         "score": [[0.7, 0.2, 0.1]], "weight": None},
        {"name": "an empty row beside a scoring one", "true": [[0, 0, 0], [1, 0, 0]],
         "score": [[0.7, 0.2, 0.1], [0.7, 0.2, 0.1]], "weight": None},
        {"name": "every score equal, two of three relevant", "true": [[1, 1, 0]],
         "score": [[0.5, 0.5, 0.5]], "weight": None},
        {"name": "negative scores", "true": [[1, 0, 0]],
         "score": [[-0.7, -0.2, -0.1]], "weight": None},
        {"name": "relevant on top", "true": [[1, 1, 0, 0]],
         "score": [[0.9, 0.8, 0.2, 0.1]], "weight": None},
        {"name": "relevant at the bottom", "true": [[0, 0, 1, 1]],
         "score": [[0.9, 0.8, 0.2, 0.1]], "weight": None},
        # 20 columns: the width at which lot 1's Array.Sort stopped being stable. The
        # tie order is unobservable here, and a case is worth more than the claim.
        {"name": "twenty columns, every score tied", "true": [wide],
         "score": [[0.5] * 20], "weight": None},
        {"name": "twenty columns, strictly ordered", "true": [wide],
         "score": [[(20 - j) / 20 for j in range(20)]], "weight": None},
    ]
```

- [x] **Step 2: Write `generate_label_ranking()`**, in the shape `generate_ranking` uses at line 2152:

```python
def generate_label_ranking() -> dict:
    import numpy as np
    from sklearn.metrics import coverage_error, label_ranking_loss
    from sklearn.metrics import label_ranking_average_precision_score as lrap

    cases = []
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        for fixture in _label_ranking_fixtures():
            true = np.array(fixture["true"])
            score = np.array(fixture["score"])
            kw = {} if fixture["weight"] is None else {
                "sample_weight": np.array(fixture["weight"])}
            cases.append({
                "name": fixture["name"],
                "y_true": [v for row in fixture["true"] for v in row],
                "y_score": [v for row in fixture["score"] for v in row],
                "label_count": true.shape[1],
                "sample_weight": fixture["weight"],
                "lrap": float(lrap(true, score, **kw)),
                "coverage": float(coverage_error(true, score, **kw)),
                "ranking_loss": float(label_ranking_loss(true, score, **kw)),
            })

    return {
        "metadata": {
            "algorithm": "LabelRanking",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.label_ranking_average_precision_score",
                "sklearn.metrics.coverage_error",
                "sklearn.metrics.label_ranking_loss",
            ],
            "count": len(cases),
        },
        "cases": cases,
    }
```

- [x] **Step 3: Register it** in the map at `tools/generate_oracles.py:4935`, beside the two lot 1
  added:

```python
        "label_ranking.json": generate_label_ranking,
```

- [x] **Step 4: Generate, and read the generator's own exit code.**

```bash
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
echo "generator exit=$?"
```

Expected: `generator exit=0`, and `git status` shows `tests/oracles/label_ranking.json` as the only
new corpus with no drift in the others.

- [x] **Step 5: Check the corpus can tell the three metrics apart.** A corpus where the three agree
  everywhere would prove nothing.

```bash
python3 -c "
import json; c=json.load(open('tests/oracles/label_ranking.json'))['cases']
print(len(c), 'cases')
for k in ('lrap','coverage','ranking_loss'):
    print(k, sorted({round(x[k],6) for x in c}))
"
```

Expected: at least **five** distinct values for each of the three, and `coverage` holding `0.5` —
the row that drags the mean below `1`. Five rather than six because the loss is `0` on all three
degenerate rows and `1` on three saturated ones by construction; measured on these fixtures it takes
five distinct values where the other two take seven and eight.

- [x] **Step 6: Run the Python guards on the generator, then commit.**

```bash
git add -N tools/generate_oracles.py tests/oracles/label_ranking.json
python3 tools/check_comment_length.py; echo "comment_length=$?"
python3 tools/check_machine_paths.py; echo "machine_paths=$?"
git add tools/generate_oracles.py tests/oracles/label_ranking.json
git commit -m "Freeze the label-ranking corpus, twenty columns included"
```

---

## Task 2 — `Internal/LabelRanking.cs`, the rank that has no order

**Files:** create `src/Lodestar.Metrics/Internal/LabelRanking.cs`; create
`tests/Lodestar.Metrics.Tests/LabelRankingFactsTests.cs`.

**Interfaces produced** — Tasks 3, 4 and 5 and issue #210 all consume these:

```csharp
internal static class LabelRanking
{
    public static void MaxRank(ReadOnlySpan<double> scores, Span<int> ranks);
    public static int RelevantCount(ReadOnlySpan<bool> row);
    public static void Validate(
        ReadOnlySpan<bool> yTrue, ReadOnlySpan<double> yScore, int labelCount,
        ReadOnlySpan<double> sampleWeight, bool singleLabelAllowed);
    public static double Weighted(ReadOnlySpan<double> perRow, ReadOnlySpan<double> sampleWeight);
}
```

- [x] **Step 1: Write the failing test for the rank.** This is the whole family's foundation, and
  the property that matters is that a tied group takes its **worst** rank.

```csharp
using Lodestar.Metrics.Internal;
using Xunit;

namespace Lodestar.Metrics.Tests;

public sealed class LabelRankingFactsTests
{
    [Fact]
    public void The_best_score_ranks_first_and_a_tied_group_takes_its_worst_rank()
    {
        double[] scores = [0.75, 0.5, 1.0];
        int[] ranks = new int[3];
        LabelRanking.MaxRank(scores, ranks);
        Assert.Equal([2, 3, 1], ranks);

        double[] tied = [0.5, 0.5, 0.5];
        LabelRanking.MaxRank(tied, ranks);
        Assert.Equal([3, 3, 3], ranks);
    }
}
```

- [x] **Step 2: Run it and watch it fail.**

```bash
dotnet test tests/Lodestar.Metrics.Tests -c Release --filter "FullyQualifiedName~LabelRankingFactsTests"
```

Expected: a compile error, `LabelRanking` does not exist. **Read the count, not the colour** — a
filter that matches nothing exits zero and reports success.

- [x] **Step 3: Write `MaxRank`.** No permutation is built, which is the point: there is no tie
  order to get wrong.

```csharp
namespace Lodestar.Metrics.Internal;

/// <summary>The ranks of one row of scores, and the shape the three label-matrix metrics share.</summary>
/// <remarks>
/// The rank is <c>rankdata(-y_score, "max")</c>: 1 is the best score, and every member of a tied
/// group takes the group's worst rank. Written as a count rather than a sorted permutation —
/// <c>|{k : score[k] >= score[j]}|</c> — so no ordering of equal scores exists to be wrong about.
/// </remarks>
internal static class LabelRanking
{
    /// <summary>The 1-based rank of each label, best first, ties taking the group's worst.</summary>
    public static void MaxRank(ReadOnlySpan<double> scores, Span<int> ranks)
    {
        double[] sorted = scores.ToArray();
        Array.Sort(sorted);
        for (int j = 0; j < scores.Length; j++)
        {
            ranks[j] = scores.Length - LowerBound(sorted, scores[j]);
        }
    }

    /// <summary>How many of <paramref name="sorted"/> are strictly below <paramref name="value"/>.</summary>
    private static int LowerBound(double[] sorted, double value)
    {
        int low = 0;
        int high = sorted.Length;
        while (low < high)
        {
            int mid = low + ((high - low) / 2);
            if (sorted[mid] < value)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    /// <summary>How many labels of one row are relevant.</summary>
    public static int RelevantCount(ReadOnlySpan<bool> row)
    {
        int count = 0;
        foreach (bool relevant in row)
        {
            if (relevant)
            {
                count++;
            }
        }

        return count;
    }
}
```

- [x] **Step 4: Run the test and watch it pass.** Expected: `réussite : 1`.

- [x] **Step 5: Write the failing test for the validation**, one per refusal the spec measured.

```csharp
    [Fact]
    public void The_refusals_are_sklearns_with_its_sentences()
    {
        bool[] truth = [true, false];
        double[] scores = [0.7, 0.2];

        // A single label column: refused here, accepted by LabelRankingAveragePrecision.
        ArgumentException single = Assert.Throws<ArgumentException>(
            () => LabelRanking.Validate([true], [0.7], 1, default, singleLabelAllowed: false));
        Assert.Contains("binary format is not supported", single.Message, StringComparison.Ordinal);

        // ...and accepted when the caller allows it.
        LabelRanking.Validate([true], [0.7], 1, default, singleLabelAllowed: true);

        Assert.Throws<ArgumentException>(
            () => LabelRanking.Validate(truth, [0.7], 2, default, singleLabelAllowed: false));
        Assert.Throws<ArgumentException>(
            () => LabelRanking.Validate([], [], 2, default, singleLabelAllowed: false));
        Assert.Throws<ArgumentException>(
            () => LabelRanking.Validate(truth, scores, 2, [1.0, 2.0], singleLabelAllowed: false));
    }
```

- [x] **Step 6: Run it and watch it fail**, then write `Validate` and `Weighted`:

```csharp
    /// <summary>Refuses the shapes scikit-learn refuses, with the sentences it prints.</summary>
    /// <remarks>
    /// <paramref name="singleLabelAllowed"/> because the reference is not of one mind:
    /// <c>label_ranking_average_precision_score</c> scores a single label column and returns 1,
    /// where <c>coverage_error</c> and <c>label_ranking_loss</c> refuse it. Reproduced rather
    /// than smoothed — making the three agree would invent a divergence instead of copying one.
    /// </remarks>
    public static void Validate(
        ReadOnlySpan<bool> yTrue,
        ReadOnlySpan<double> yScore,
        int labelCount,
        ReadOnlySpan<double> sampleWeight,
        bool singleLabelAllowed)
    {
        if (labelCount < 1)
        {
            throw new ArgumentException(
                $"yScore holds {labelCount} labels; a label matrix needs at least 1.",
                nameof(labelCount));
        }

        if (labelCount == 1 && !singleLabelAllowed)
        {
            throw new ArgumentException("binary format is not supported", nameof(labelCount));
        }

        if (yTrue.Length != yScore.Length)
        {
            throw new ArgumentException(
                $"yTrue holds {yTrue.Length} values and yScore holds {yScore.Length}; " +
                "y_true and y_score have different shape.",
                nameof(yScore));
        }

        if (yTrue.Length == 0 || yTrue.Length % labelCount != 0)
        {
            throw new ArgumentException(
                $"yTrue holds {yTrue.Length} values, which is not a whole number of rows " +
                $"of {labelCount}.",
                nameof(yTrue));
        }

        if (sampleWeight.Length != 0 && sampleWeight.Length != yTrue.Length / labelCount)
        {
            throw new ArgumentException(
                $"sampleWeight holds {sampleWeight.Length} values for " +
                $"{yTrue.Length / labelCount} samples; they must agree.",
                nameof(sampleWeight));
        }
    }

    /// <summary>The mean of the per-row values, weighted when weights are given.</summary>
    /// <remarks>
    /// A weight vector summing to zero raises, in <c>numpy.average</c>'s sentence — the one the
    /// regression metrics already reproduce. <c>LabelRankingAveragePrecision</c> does not call
    /// this: the reference divides directly there and returns <c>NaN</c>, which C# does too.
    /// </remarks>
    public static double Weighted(ReadOnlySpan<double> perRow, ReadOnlySpan<double> sampleWeight)
    {
        if (sampleWeight.Length == 0)
        {
            double plain = 0.0;
            foreach (double value in perRow)
            {
                plain += value;
            }

            return plain / perRow.Length;
        }

        double total = 0.0;
        double weights = 0.0;
        for (int row = 0; row < perRow.Length; row++)
        {
            total += perRow[row] * sampleWeight[row];
            weights += sampleWeight[row];
        }

        // S1244: the reference compares the sum to zero exactly, and a tolerance would
        // refuse weights numpy accepts. Its own message is reproduced below.
#pragma warning disable S1244
        if (weights == 0.0)
#pragma warning restore S1244
        {
            throw new ArgumentException(
                "Weights sum to zero, can't be normalized.", nameof(sampleWeight));
        }

        return total / weights;
    }
```

- [x] **Step 7: Run the tests and watch them pass**, then clear the analyzers before committing —
  a green build is not a clean Sonar, and a finding blocks the merge.

```bash
dotnet build src/Lodestar.Metrics -c Release --no-incremental
dotnet test tests/Lodestar.Metrics.Tests -c Release --filter "FullyQualifiedName~LabelRankingFactsTests"
git add -N src/Lodestar.Metrics/Internal/LabelRanking.cs tests/Lodestar.Metrics.Tests/LabelRankingFactsTests.cs
python3 tools/check_comment_length.py; echo "comment_length=$?"
git add -A && git commit -m "Rank a row without ordering its ties"
```

---

## Task 3 — `CoverageError`

**Files:** create `src/Lodestar.Metrics/CoverageError.cs`; modify
`tests/Lodestar.Metrics.Tests/LabelRankingFactsTests.cs` — every `[Fact]` below joins that class.

**Interfaces consumed:** `LabelRanking.MaxRank`, `RelevantCount`, `Validate`, `Weighted`.

- [x] **Step 1: Write the failing test.** Two rows of the worked case, and the empty row that drags
  the mean below `1` — the number an implementation treating "nothing relevant" as "all covered"
  gets wrong while looking plausible.

```csharp
    [Fact]
    public void A_row_with_nothing_relevant_covers_zero_labels_not_all_of_them()
    {
        bool[] truth = [false, false, false, true, false, false];
        double[] scores = [0.7, 0.2, 0.1, 0.7, 0.2, 0.1];

        // The scoring row covers 1 label, the empty row covers 0: the mean is 0.5.
        // Treating the empty row as fully covered would give 2.0 and look reasonable.
        Assert.Equal(0.5, CoverageError.Score(truth, scores, 3), MetricsCorpus.Tolerance);
    }
```

- [x] **Step 2: Run it and watch it fail.** Expected: `CoverageError` does not exist.

- [x] **Step 3: Write the implementation.**

```csharp
using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// How far down the ranking you must read to have seen every relevant label — the
/// equivalent of <c>sklearn.metrics.coverage_error</c>.
/// </summary>
public static class CoverageError
{
    /// <summary>Scores a boolean label matrix — <c>sklearn.metrics.coverage_error(y_true, y_score, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">Whether each label is relevant, row-major: one row per sample, <paramref name="labelCount"/> values each.</param>
    /// <param name="yScore">The scores the ranking was made from, same shape as <paramref name="yTrue"/>.</param>
    /// <param name="labelCount">How many labels each row holds.</param>
    /// <param name="sampleWeight">One weight per sample, or empty for an unweighted mean.</param>
    /// <returns>The mean position of the worst-ranked relevant label. <c>1</c> is the best a row can do; a row with no relevant label contributes <c>0</c>, so the mean can sit below <c>1</c>.</returns>
    /// <exception cref="ArgumentException">The shapes disagree, <paramref name="labelCount"/> is <c>1</c>, or <paramref name="sampleWeight"/> sums to zero.</exception>
    public static double Score(
        ReadOnlySpan<bool> yTrue,
        ReadOnlySpan<double> yScore,
        int labelCount,
        ReadOnlySpan<double> sampleWeight = default)
    {
        LabelRanking.Validate(yTrue, yScore, labelCount, sampleWeight, singleLabelAllowed: false);

        int rows = yTrue.Length / labelCount;
        double[] perRow = new double[rows];
        int[] ranks = new int[labelCount];
        for (int row = 0; row < rows; row++)
        {
            ReadOnlySpan<bool> relevant = yTrue.Slice(row * labelCount, labelCount);
            LabelRanking.MaxRank(yScore.Slice(row * labelCount, labelCount), ranks);

            int worst = 0;
            for (int label = 0; label < labelCount; label++)
            {
                if (relevant[label] && ranks[label] > worst)
                {
                    worst = ranks[label];
                }
            }

            perRow[row] = worst;
        }

        return LabelRanking.Weighted(perRow, sampleWeight);
    }
}
```

- [x] **Step 4: Run the test and watch it pass.** Expected: `réussite : 1`.

- [x] **Step 5: Commit.**

```bash
git add -N src/Lodestar.Metrics/CoverageError.cs
python3 tools/check_comment_length.py; echo "comment_length=$?"
git add -A && git commit -m "Read down to the worst-ranked relevant label"
```

---

## Task 4 — `LabelRankingLoss`

**Files:** create `src/Lodestar.Metrics/LabelRankingLoss.cs`; modify
`tests/Lodestar.Metrics.Tests/LabelRankingFactsTests.cs`.

- [x] **Step 1: Write the failing test.** A tie between a relevant and an irrelevant label counts
  as an error, which is the choice the reference makes and the one an implementation is most likely
  to get backwards.

```csharp
    [Fact]
    public void A_tie_between_a_relevant_and_an_irrelevant_label_counts_as_an_error()
    {
        // Two relevant, one irrelevant, every score equal: both pairs are wrong, so 1.
        Assert.Equal(1.0,
            LabelRankingLoss.Score([true, true, false], [0.5, 0.5, 0.5], 3),
            MetricsCorpus.Tolerance);

        // The same row with the irrelevant label scored strictly lower: nothing is wrong.
        Assert.Equal(0.0,
            LabelRankingLoss.Score([true, true, false], [0.5, 0.5, 0.1], 3),
            MetricsCorpus.Tolerance);
    }
```

- [x] **Step 2: Run it and watch it fail.**

- [x] **Step 3: Write the implementation.** The reference reaches the same count through
  `np.unique` and a cumulative sum; the pair loop is the definition itself, and it is `O(p·q)` on
  the relevant and irrelevant counts of one row, which for a label matrix is small.

```csharp
using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// How often an irrelevant label outranks a relevant one — the equivalent of
/// <c>sklearn.metrics.label_ranking_loss</c>.
/// </summary>
public static class LabelRankingLoss
{
    /// <summary>Scores a boolean label matrix — <c>sklearn.metrics.label_ranking_loss(y_true, y_score, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">Whether each label is relevant, row-major: one row per sample, <paramref name="labelCount"/> values each.</param>
    /// <param name="yScore">The scores the ranking was made from, same shape as <paramref name="yTrue"/>.</param>
    /// <param name="labelCount">How many labels each row holds.</param>
    /// <param name="sampleWeight">One weight per sample, or empty for an unweighted mean.</param>
    /// <returns>The mean fraction of wrongly ordered pairs, in <c>[0, 1]</c>. <c>0</c> is perfect, and a row where every label or no label is relevant contributes <c>0</c> — it holds no pair to order.</returns>
    /// <exception cref="ArgumentException">The shapes disagree, <paramref name="labelCount"/> is <c>1</c>, or <paramref name="sampleWeight"/> sums to zero.</exception>
    public static double Score(
        ReadOnlySpan<bool> yTrue,
        ReadOnlySpan<double> yScore,
        int labelCount,
        ReadOnlySpan<double> sampleWeight = default)
    {
        LabelRanking.Validate(yTrue, yScore, labelCount, sampleWeight, singleLabelAllowed: false);

        int rows = yTrue.Length / labelCount;
        double[] perRow = new double[rows];
        for (int row = 0; row < rows; row++)
        {
            ReadOnlySpan<bool> relevant = yTrue.Slice(row * labelCount, labelCount);
            ReadOnlySpan<double> scores = yScore.Slice(row * labelCount, labelCount);
            int positives = LabelRanking.RelevantCount(relevant);
            if (positives == 0 || positives == labelCount)
            {
                continue;
            }

            long wrong = 0;
            for (int r = 0; r < labelCount; r++)
            {
                if (!relevant[r])
                {
                    continue;
                }

                for (int f = 0; f < labelCount; f++)
                {
                    // A tie is an error: the reference counts an irrelevant label sharing a
                    // relevant one's score as outranking it, and the corpus pins that.
                    if (!relevant[f] && scores[r] <= scores[f])
                    {
                        wrong++;
                    }
                }
            }

            perRow[row] = (double)wrong / ((long)positives * (labelCount - positives));
        }

        return LabelRanking.Weighted(perRow, sampleWeight);
    }
}
```

- [x] **Step 4: Run the test and watch it pass.**

- [x] **Step 5: Commit.**

```bash
git add -N src/Lodestar.Metrics/LabelRankingLoss.cs
python3 tools/check_comment_length.py; echo "comment_length=$?"
git add -A && git commit -m "Count the pairs an irrelevant label wins, ties included"
```

---

## Task 5 — `LabelRankingAveragePrecision`

**Files:** create `src/Lodestar.Metrics/LabelRankingAveragePrecision.cs`; modify
`tests/Lodestar.Metrics.Tests/LabelRankingFactsTests.cs`.

- [x] **Step 1: Write the failing test.** The two degenerate rows both score `1`, and a single
  label column is **accepted** here where the other two refuse it.

```csharp
    [Fact]
    public void Both_degenerate_rows_score_one_and_a_single_label_column_is_accepted()
    {
        Assert.Equal(1.0,
            LabelRankingAveragePrecision.Score([true, true, true], [0.7, 0.2, 0.1], 3),
            MetricsCorpus.Tolerance);
        Assert.Equal(1.0,
            LabelRankingAveragePrecision.Score([false, false, false], [0.7, 0.2, 0.1], 3),
            MetricsCorpus.Tolerance);

        // coverage_error and label_ranking_loss refuse this; lrap returns 1. Measured.
        Assert.Equal(1.0,
            LabelRankingAveragePrecision.Score([true], [0.7], 1),
            MetricsCorpus.Tolerance);
        Assert.Throws<ArgumentException>(() => CoverageError.Score([true], [0.7], 1));
        Assert.Throws<ArgumentException>(() => LabelRankingLoss.Score([true], [0.7], 1));
    }
```

- [x] **Step 2: Run it and watch it fail.**

- [x] **Step 3: Write the implementation.** `L / rank` averaged over the relevant labels, where
  `rank` is the label's rank among all labels and `L` its rank among the relevant ones.

```csharp
using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// How much of the ranking above each relevant label is itself relevant — the
/// equivalent of <c>sklearn.metrics.label_ranking_average_precision_score</c>.
/// </summary>
public static class LabelRankingAveragePrecision
{
    /// <summary>Scores a boolean label matrix — <c>sklearn.metrics.label_ranking_average_precision_score(y_true, y_score, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">Whether each label is relevant, row-major: one row per sample, <paramref name="labelCount"/> values each.</param>
    /// <param name="yScore">The scores the ranking was made from, same shape as <paramref name="yTrue"/>.</param>
    /// <param name="labelCount">How many labels each row holds.</param>
    /// <param name="sampleWeight">One weight per sample, or empty for an unweighted mean.</param>
    /// <returns><c>1</c> when every relevant label outranks every irrelevant one. A row where every label or no label is relevant scores <c>1</c> too — its ranking carries no information, and the reference says so in a comment.</returns>
    /// <exception cref="ArgumentException">The shapes disagree, or <paramref name="sampleWeight"/> has the wrong length.</exception>
    public static double Score(
        ReadOnlySpan<bool> yTrue,
        ReadOnlySpan<double> yScore,
        int labelCount,
        ReadOnlySpan<double> sampleWeight = default)
    {
        LabelRanking.Validate(yTrue, yScore, labelCount, sampleWeight, singleLabelAllowed: true);

        int rows = yTrue.Length / labelCount;
        int[] ranks = new int[labelCount];
        double[] relevantScores = new double[labelCount];
        int[] relevantRanks = new int[labelCount];

        double total = 0.0;
        double weights = 0.0;
        for (int row = 0; row < rows; row++)
        {
            ReadOnlySpan<bool> relevant = yTrue.Slice(row * labelCount, labelCount);
            ReadOnlySpan<double> scores = yScore.Slice(row * labelCount, labelCount);
            int positives = LabelRanking.RelevantCount(relevant);
            double aux = positives == 0 || positives == labelCount
                ? 1.0
                : Precision(relevant, scores, positives, ranks, relevantScores, relevantRanks);

            double weight = sampleWeight.Length == 0 ? 1.0 : sampleWeight[row];
            total += aux * weight;
            weights += weight;
        }

        // Divided directly rather than through LabelRanking.Weighted: the reference divides
        // here too, so a weight vector summing to zero gives NaN where the other two throw.
        return total / weights;
    }

    /// <summary>One row's mean of <c>L / rank</c> over its relevant labels.</summary>
    private static double Precision(
        ReadOnlySpan<bool> relevant,
        ReadOnlySpan<double> scores,
        int positives,
        int[] ranks,
        double[] relevantScores,
        int[] relevantRanks)
    {
        LabelRanking.MaxRank(scores, ranks);

        int taken = 0;
        for (int label = 0; label < relevant.Length; label++)
        {
            if (relevant[label])
            {
                relevantScores[taken++] = scores[label];
            }
        }

        LabelRanking.MaxRank(
            relevantScores.AsSpan(0, positives), relevantRanks.AsSpan(0, positives));

        double sum = 0.0;
        taken = 0;
        for (int label = 0; label < relevant.Length; label++)
        {
            if (relevant[label])
            {
                sum += (double)relevantRanks[taken++] / ranks[label];
            }
        }

        return sum / positives;
    }
}
```

- [x] **Step 4: Run the test and watch it pass.**

- [x] **Step 5: Build both frameworks and clear the analyzers.**

```bash
dotnet build Lodestar.slnx -c Release --no-incremental
```

Expected: `0 Avertissement(s)`, `0 Erreur(s)`.

- [x] **Step 6: Commit.**

```bash
git add -N src/Lodestar.Metrics/LabelRankingAveragePrecision.cs
python3 tools/check_comment_length.py; echo "comment_length=$?"
git add -A && git commit -m "Average how much of each relevant label's lead is relevant"
```

---

## Task 6 — the corpus replay, and the facts a corpus cannot state

**Files:** modify `tests/Lodestar.Metrics.Tests/MetricsCorpus.cs`; create
`tests/Lodestar.Metrics.Tests/LabelRankingTests.cs`; modify
`tests/Lodestar.Metrics.Tests/LabelRankingFactsTests.cs`.

- [x] **Step 1: Add a `Bools` reader** to `MetricsCorpus.cs`, beside `Ints`:

```csharp
    /// <summary>An array the corpus writes as 0/1, read as the boolean it means.</summary>
    public static bool[] Bools(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetInt32() != 0)];
```

- [x] **Step 2: Write the replay**, one theory row per fixture so a failure names it:

```csharp
using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>The three label-matrix metrics against the frozen corpus.</summary>
public sealed class LabelRankingTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("label_ranking.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("cases").EnumerateArray()];

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_sklearn_on_every_case(int index)
    {
        JsonElement c = Cases[index];
        bool[] yTrue = MetricsCorpus.Bools(c, "y_true");
        double[] yScore = MetricsCorpus.Doubles(c, "y_score");
        double[] weight = MetricsCorpus.OptionalDoubles(c, "sample_weight");
        int labels = c.GetProperty("label_count").GetInt32();

        Assert.Equal(c.GetProperty("lrap").GetDouble(),
                     LabelRankingAveragePrecision.Score(yTrue, yScore, labels, weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("coverage").GetDouble(),
                     CoverageError.Score(yTrue, yScore, labels, weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("ranking_loss").GetDouble(),
                     LabelRankingLoss.Score(yTrue, yScore, labels, weight),
                     MetricsCorpus.Tolerance);
    }
}
```

- [x] **Step 3: Run it, and read the count.** Expected: 11 passing theory rows — one per fixture of
  Task 1. A number below that means `Indices()` is not seeing the corpus.

```bash
dotnet test tests/Lodestar.Metrics.Tests -c Release --filter "FullyQualifiedName~LabelRankingTests"
```

- [x] **Step 4: Assert the tie indifference as an indifference**, in
  `LabelRankingFactsTests.cs`. Two permutations compared to *each other*, not to a frozen number:
  a frozen number would pass if both sides were wrong the same way.

```csharp
    [Fact]
    public void Permuting_a_tied_group_changes_nothing_at_any_width()
    {
        // 20 columns: past the 16 where lot 1's Array.Sort stopped being stable, and the
        // width at which a permutation-based implementation would start to disagree.
        const int n = 20;
        bool[] first = new bool[n];
        bool[] second = new bool[n];
        double[] tied = new double[n];
        for (int i = 0; i < n; i++)
        {
            tied[i] = 0.5;
        }

        first[0] = true;
        first[9] = true;
        second[10] = true;
        second[19] = true;

        Assert.Equal(LabelRankingAveragePrecision.Score(first, tied, n),
                     LabelRankingAveragePrecision.Score(second, tied, n),
                     MetricsCorpus.Tolerance);
        Assert.Equal(CoverageError.Score(first, tied, n),
                     CoverageError.Score(second, tied, n), MetricsCorpus.Tolerance);
        Assert.Equal(LabelRankingLoss.Score(first, tied, n),
                     LabelRankingLoss.Score(second, tied, n), MetricsCorpus.Tolerance);
    }
```

- [x] **Step 5: Add the weight facts**, both divergences the spec measured:

```csharp
    [Fact]
    public void A_weight_vector_summing_to_zero_throws_for_two_and_gives_NaN_for_the_third()
    {
        bool[] truth = [true, false, false, false, false, true];
        double[] scores = [0.7, 0.2, 0.1, 0.7, 0.2, 0.1];
        double[] zeroSum = [0.0, 0.0];

        Assert.True(double.IsNaN(
            LabelRankingAveragePrecision.Score(truth, scores, 3, zeroSum)));

        ArgumentException coverage = Assert.Throws<ArgumentException>(
            () => CoverageError.Score(truth, scores, 3, zeroSum));
        Assert.Contains("Weights sum to zero", coverage.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(
            () => LabelRankingLoss.Score(truth, scores, 3, zeroSum));
    }

    [Fact]
    public void A_negative_weight_is_accepted_and_takes_the_result_out_of_its_range()
    {
        // Measured against scikit-learn 1.9.0: -0.33333333333333337, 5.0 and 2.0 — a
        // metric documented in [0, 1] returning a negative number, as the reference does.
        bool[] truth = [true, false, false, false, false, true];
        double[] scores = [0.7, 0.2, 0.1, 0.7, 0.2, 0.1];
        double[] weight = [-1.0, 2.0];

        Assert.Equal(-0.33333333333333337,
            LabelRankingAveragePrecision.Score(truth, scores, 3, weight), MetricsCorpus.Tolerance);
        Assert.Equal(5.0, CoverageError.Score(truth, scores, 3, weight), MetricsCorpus.Tolerance);
        Assert.Equal(2.0, LabelRankingLoss.Score(truth, scores, 3, weight), MetricsCorpus.Tolerance);
    }
```

- [x] **Step 6: Run the whole Metrics suite on both frameworks, and read the count.**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~Lodestar.Metrics"
```

Expected: the lot 1 total plus 11 theory rows and 8 facts — two from Task 2, one each
from Tasks 3, 4 and 5, three from this one — failing none.

- [x] **Step 7: Commit.**

```bash
git add -N tests/Lodestar.Metrics.Tests/LabelRankingTests.cs
python3 tools/check_comment_length.py; echo "comment_length=$?"
git add -A && git commit -m "Replay the corpus, and pin what it cannot say"
```

---

## Task 7 — the documents and the gates

**Files:** modify `docs/reference/metrics/ranking.md`, `docs/equivalence.md`,
`samples/Lodestar.Sample/Lot5Metrics.cs`, `CHANGELOG.md`; create six pages under
`docs/reference/metrics/ranking/`.

- [x] **Step 1: Write the six reference pages**, in the layout lot 1 established — read
  `docs/reference/metrics/ranking/ndcg.md` and `ndcg-score.md` first and copy their rubric order:
  title, one-line summary, `<!-- docs-declaration -->` and its fence, then **Parameters**,
  **Returns**, **Exceptions**, **Example**, **Remarks**, **Applies to**, **See also**.

  The files are `labelrankingaverageprecision.md`, `labelrankingaverageprecision-score.md`,
  `coverageerror.md`, `coverageerror-score.md`, `labelrankingloss.md`, `labelrankingloss-score.md`.

  **Each type page carries a `## Members` table linking its member page in the exact form the gate
  matches** — `| [`CoverageError.Score`](coverageerror-score.md) | … |` — and the index links each
  type page as `[`CoverageError`](ranking/coverageerror.md)`. The gate compares those two strings
  literally; a link that reads well but differs by a character fails it.
  Every parameter must be named in backticks inside the **Parameters** rubric, and **Applies to**
  must read `net10.0, netstandard2.0` — the gate replays each declaration against both assemblies.

  Two facts go on the pages **next to the number**, because a reader will otherwise take them for
  bugs: coverage's empty row contributing `0` so the mean can sit below `1`, and the single-label
  column that `LabelRankingAveragePrecision` accepts while the other two refuse it.

  The `// =>` on an example fence is an **assertion** that is executed. Use measured values:

```csharp
using Lodestar.Metrics;

bool[] truth = [true, false, false, false, false, true];
double[] scores = [0.75, 0.5, 1.0, 1.0, 0.2, 0.1];

double lrap = LabelRankingAveragePrecision.Score(truth, scores, labelCount: 3);  // => 0.4166…
double coverage = CoverageError.Score(truth, scores, labelCount: 3);  // => 2.5
double loss = LabelRankingLoss.Score(truth, scores, labelCount: 3);  // => 0.75
```

- [x] **Step 2: Extend `docs/reference/metrics/ranking.md`.** Three rows in the type table, in the
  exact form the gate demands — `[`CoverageError`](ranking/coverageerror.md)` and so on — plus a
  short section separating the two halves of the page: lot 1 scores **one ordered list**, these
  three score a **label matrix**, and the tie discussion above them does not apply here because the
  rank is a count rather than an order.

- [x] **Step 3: Run the reference gate.** It names the exact declaration it wants when it disagrees.

```bash
dotnet test tests/Lodestar.Metrics.Tests -c Release --filter "FullyQualifiedName~ReferenceDocumentation"
```

- [x] **Step 4: Add one `docs/equivalence.md` row per function**, in the ranking section lot 1
  created, each linking its member page. The Differences column carries: the single-column
  divergence, the zero-sum weight divergence, negative weights leaving the range, and — for the
  loss — that a tie counts as an error.

- [x] **Step 5: Exercise the three from the sample.** Extend `Ranking()` in `Lot5Metrics.cs`; the
  packaging gate needs a **member reference**, not a `typeof`.

- [x] **Step 6: Add the changelog entry**, under `## [Unreleased]` → `### Lodestar.Metrics` →
  `#### Added — ranking`, beside the lot 1 entries. No version bump: `Lodestar.Metrics` is at
  `0.2.0` and unreleased.

- [x] **Step 7: Run the whole battery.** Each on its own exit code — a pipeline's status is the
  last command's.

```bash
dotnet build Lodestar.slnx -c Release --no-incremental
dotnet test Lodestar.slnx -c Release
dotnet format Lodestar.slnx --verify-no-changes; echo "format=$?"
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
git add -N .
python3 tools/check_machine_paths.py;  echo "machine_paths=$?"
python3 tools/check_comment_length.py; echo "comment_length=$?"
python3 tools/check_version_floor.py;  echo "version_floor=$?"
```

- [x] **Step 8: Run the packaging and snippet gates with an isolated package cache**, or they judge
  the published packages instead of the working tree.

```bash
export NUGET_PACKAGES=/tmp/lot2-packages
rm -rf ./artifacts
for p in src/Lodestar.Text src/Lodestar.Embeddings src/Lodestar.Fuzzy src/Lodestar.Metrics; do
  dotnet pack "$p" -c Release -o ./artifacts
done
dotnet run --project samples/Lodestar.Sample -c Release
python3 tools/extract_doc_snippets.py; echo "extract=$?"
dotnet build samples/Lodestar.DocSnippets -c Release
dotnet run --project samples/Lodestar.DocSnippets -c Release
```

Expected: `every public type is reachable.` from the sample, and `snippets skipped : 0` with no
`::error::` line from the snippets.

- [x] **Step 9: Regenerate the corpora and confirm no drift**, then commit.

```bash
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
echo "generator exit=$?"
cd <repo> && git status --short tests/oracles/
```

Expected: nothing modified. The `Oracles are reproducible` job is occasionally flaky — re-run before
believing a failure.

---

## Task 8 — review, then the pull request

- [x] **Step 1: Ask for a code review of the diff, before the pull request exists.** What it is for
  here: whether `MaxRank`'s count really is `rankdata(-y_score, "max")` at every width, whether the
  loss counts the pairs the reference counts rather than their complement, and whether the pages say
  what the code does rather than what the spec hoped. On lot 1 the equivalent review found eight
  defects, three of which the corpus could not see.

- [x] **Step 2: Act on it, re-run the battery, then open the pull request.** `gh pr edit` fails
  silently on this repository; assignment and label edits go through `gh api`.

  The body carries **`Closes #201`** and **`Closes #173`**, once, next to each number and with no
  other `close`/`fix`/`resolve` word anywhere near an issue number — lot 1's body wrote "this does
  not close #173" and closed it, because GitHub's parser ignores the negation.

---

## What this plan does not do

**`average_precision_score`** — [#210](https://github.com/CyrilB1531/lodestar/issues/210). Its
multilabel form takes this same boolean matrix, which is why `Internal/LabelRanking.cs` exposes
`MaxRank` and `RelevantCount` rather than hiding them inside the three types. It is a lot of its
own, and reaching for it here is what `CONTRIBUTING.md` forbids.

**A `NaN` guard on `yScore`** — see the File Structure note above: it belongs to all 45 members or
to none, and this branch is not where that is decided.

**A metrics guide** — [#203](https://github.com/CyrilB1531/lodestar/issues/203). The ranking index
page gains the prose that separates the two halves of this family, and nothing more.
