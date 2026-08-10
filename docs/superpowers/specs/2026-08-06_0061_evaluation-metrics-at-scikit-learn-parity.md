# Classification metrics at scikit-learn parity — design

Issue: [#61](https://github.com/CyrilB1531/data.net/issues/61)
Branch: `feat/61-classification-metrics`

## Objective

A dependency-free `DataNet.Metrics` package providing classification metrics at
exact numeric parity with `sklearn.metrics`, so a migrated pipeline can be
compared against its Python original number for number — and faster than it.

`docs/migration/sklearn.md` currently names the trap ("check the definitions:
macro/micro averaging, handling of absent classes") and leaves the reader in it.
The three-column inventory in `docs/migration/README.md` resolves a row either to
an existing .NET building block or to something DataNet builds. Metrics do
neither today; this work moves them to *build*.

## Resolved decisions

| Question | Decision | Why |
| --- | --- | --- |
| Placement | New `DataNet.Metrics` package | Metrics are not text-specific; extracting them from `DataNet.Text` later would be breaking. Recorded in `docs/decisions/0013`. |
| Persistence plumbing | Off (`DataNetIncludesPersistence` not set) | Nothing here is fitted state: every function is pure. Keeping it off preserves "no dependency on any target". |
| Label typing | `ReadOnlySpan<int>` only, plus `targetNames` on the report | One surface, honours the repository's span convention. `targetNames` mirrors sklearn's `target_names`. |
| `zero_division` | `ZeroDivision { Zero, One, NaN, Throw }`, default `Zero` | `Zero` is sklearn's default *value*. `Throw` is the .NET answer to the `UndefinedMetricWarning` sklearn also emits — opt-in, so parity is the default. |
| `classification_report` | Structured type **and** character-exact sklearn text | scikit-learn is pinned with hashes in `tools/requirements.lock.txt`, so the frozen string can only move on a deliberate bump, which the `Oracles are reproducible` job surfaces in the commit that makes it. |
| ROC-AUC scope | Binary **and** multiclass (`ovr`, `ovo`) | Explicit scope call. It is the largest single piece of this branch; if the pull request becomes unreadable, it is the natural split point. |
| Score matrix shape | Flattened row-major span + explicit `classCount` | Spans are one-dimensional; `double[,]` would break the convention and force an allocation on callers who already hold flat data. |
| `sample_weight` | Supported everywhere, now | Explicit scope call. Consequence: `ConfusionMatrix` counts are `double`, `Support` is `double`, and every oracle fixture exists weighted and unweighted. |
| API organisation | One static type per metric | Matches `DataNet.Text` (`Levenshtein.Distance`, `Soundex.Encode`) and sklearn's function names. |
| Shared computation | Public `ConfusionMatrix` as the engine | Callers who want several metrics count once. Metric types take either a matrix or the raw spans. |
| Report equality | `ClassificationReport` is a **class**, not a record | A record would synthesise reference equality over `IReadOnlyList<ClassRow>`, and bit-exact equality over computed `double`s is misleading anyway. `ClassRow` stays a record: scalars only. |
| Performance | Merge gate: **processor time ≥ 1× versus scikit-learn on every operation at every measured size** | Stated requirement. Processor time is the axis `bench/README.md` already argues is the honest one. |

## Package and plumbing

`src/DataNet.Metrics`, `net10.0;netstandard2.0`, version `0.1.0` in its own
`Version.props`. No external dependency; on netstandard2.0 it carries only
`System.Memory` and `System.Numerics.Vectors`, like every package here.

It declares no dependency on `DataNet.Text`, so it creates no inter-package edge
and does not participate in the "publish first, then raise the floor" cycle in
`CONTRIBUTING.md`. `tools/check_version_floor.py` is unaffected.

Files to touch outside the new project:

| File | Change |
| --- | --- |
| `DataNet.slnx` | three projects: the library, `DataNet.Metrics.Tests`, `DataNet.Metrics.NetStandard.Tests` |
| `.github/workflows/ci.yml` | the three `for proj in src/DataNet.Text …` loops (pack, project-reference guard, pack for the sample); a second `dotnet run` for the new sample |
| `.github/workflows/release.yml`, `release-nuget-org.yml` | the `DataNet.Text\|DataNet.Embeddings\|DataNet.Fuzzy` allowlist |
| `tools/check_nuspec_dependencies.py` | an `EXPECTED` entry: `{net10.0: {}, .NETStandard2.0: POLYFILLS}` |
| `samples/DataNet.Metrics.Sample` | new project (see below) |
| `README.md`, `CHANGELOG.md` | package table, `DataNet.Metrics 0.1.0` section |

### Sample

A new `samples/DataNet.Metrics.Sample` rather than an extension of
`DataNet.Sample`, whose header comment defines it as "one thing per lot" — a
deliberately thin packaging gate for the text toolkit.

The new project exercises the whole surface: the four averaging modes and the
per-class form, the absent-class case under each `ZeroDivision` value, weighted
inputs, the report's text rendering, and ROC-AUC binary / `ovr` / `ovo`. Because
it restores from `./artifacts` through the existing `samples/NuGet.config`, it is
also the packaging gate for `DataNet.Metrics`. Like its sibling it stays **out of
`DataNet.slnx`**, so `ProjectReference` resolution cannot quietly satisfy its
references.

## Public API

```csharp
namespace DataNet.Metrics;

public enum Averaging          { Binary, Micro, Macro, Weighted }
public enum ZeroDivision       { Zero, One, NaN, Throw }
public enum MultiClassStrategy { OneVsRest, OneVsOne }

// CA1032 requires the three standard constructors on a public exception type.
public sealed class UndefinedMetricException : InvalidOperationException { … }

public sealed class ConfusionMatrix
{
    public static ConfusionMatrix Compute(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default);

    public IReadOnlyList<int> Labels { get; }
    public double this[int trueIndex, int predIndex] { get; }
    public double TotalWeight { get; }
    public double[,] ToArray();
}

public static class Precision   // Recall and F1 are identical
{
    public static double Score(
        ConfusionMatrix cm,
        Averaging average = Averaging.Binary,
        int posLabel = 1,
        ZeroDivision zeroDivision = ZeroDivision.Zero);

    public static double[] PerClass(
        ConfusionMatrix cm,
        ZeroDivision zeroDivision = ZeroDivision.Zero);

    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        Averaging average = Averaging.Binary,
        int posLabel = 1,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default);

    public static double[] PerClass(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default);
}

// FBeta has the same shape as Precision, with a leading `double beta`.

public static class Accuracy
{
    public static double Score(ConfusionMatrix cm, bool normalize = true);
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        bool normalize = true,
        ReadOnlySpan<double> sampleWeight = default);
}

public sealed record ClassRow(
    int Label, string? Name,
    double Precision, double Recall, double F1, double Support);

public sealed class ClassificationReport
{
    public static ClassificationReport Compute(
        ConfusionMatrix cm,
        IReadOnlyList<string>? targetNames = null,
        ZeroDivision zeroDivision = ZeroDivision.Zero);

    public static ClassificationReport Compute(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        IReadOnlyList<string>? targetNames = null,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default);

    public IReadOnlyList<ClassRow> Classes { get; }
    public double Accuracy { get; }
    public ClassRow MacroAverage { get; }
    public ClassRow WeightedAverage { get; }

    public string ToText(int digits = 2);
    public override string ToString() => ToText();
}

public static class RocAuc
{
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int posLabel = 1,
        ReadOnlySpan<double> sampleWeight = default);

    public static double MultiClass(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,          // row-major, length n * classCount
        int classCount,
        MultiClassStrategy strategy = MultiClassStrategy.OneVsRest,
        Averaging average = Averaging.Macro,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default);
}
```

### Three deliberate departures from a literal transcription

**`Averaging.None` does not exist; `PerClass` replaces it.** sklearn's
`average=None` changes the return type based on an argument's value. In C# that
would be a `Score` returning `double` that throws for one enum member out of
five. Two methods state the same thing to the compiler.

**`RocAuc.Score` and `RocAuc.MultiClass` have different names.** Both signatures
would otherwise be `(ReadOnlySpan<int>, ReadOnlySpan<double>, int, …)`, making
`Score(y, s, 3)` ambiguous at the call site — a compile error in consumer code.

**`ZeroDivision.Throw` raises `UndefinedMetricException`.** The name echoes
sklearn's `UndefinedMetricWarning` so a web search leads to the right page.

## Parity semantics

The places where a reimplementation drifts silently, each asserted by the oracle:

- **Label ordering.** With `labels` omitted: the ascending sorted union of
  `yTrue ∪ yPred`. With `labels` supplied: **the given order, unsorted** — what
  sklearn does. Sorting "to be helpful" would break parity.
- **`_prf_divide`.** `zeroDivision` applies to a zero denominator *per class*,
  before averaging — not to the final result. `Micro` sums tp/fp/fn across
  classes and divides once. `Weighted` weights by support and returns `0.0` when
  every support is zero.
- **Micro equals accuracy** for single-label multiclass, but stops doing so as
  soon as `labels` is a strict subset. That is the worked example for
  `docs/migration/sklearn.md`.
- **The report prints a `micro avg` row instead of `accuracy`** when `labels` is
  a strict subset of the observed labels. A real sklearn quirk, required for text
  parity.
- **Binary averaging** reports the `posLabel` class only.
- **ROC-AUC, binary.** Curve built in descending score order with ties grouped,
  then trapezoidal integration — the mechanics of `_binary_clf_curve` + `auc`,
  which is also what makes sample weights come out right.
- **ROC-AUC, `ovr`.** Per-class binarisation, then `macro` or `weighted` (by
  prevalence). sklearn accepts **only those two** for a multiclass target —
  `micro` and `binary` raise, and so do we.
- **ROC-AUC, `ovo`.** Hand & Till, not a naive average over pairs.

Exact text-layout details of `ToText` — column widths, the support column under
fractional weights, `digits` rounding — are pinned by the oracle rather than
transcribed from memory. If a detail is wrong the frozen string says so.

## Errors

Mirrored from sklearn, raise for raise. None of these belong in the oracle — a
corpus records values, not exceptions — so each gets a unit test:

- `yTrue`, `yPred` or `sampleWeight` of mismatched lengths;
- empty input;
- duplicate entries in `labels`;
- `posLabel` absent from the label set;
- `Averaging.Binary` on more than two classes;
- `MultiClass` whose score rows do not sum to 1 (sklearn's tolerance);
- `MultiClass` whose span length is not `n * classCount`;
- `MultiClass` with an `average` other than `Macro` or `Weighted`;
- `OneVsOne` together with `sampleWeight` — sklearn refuses this explicitly;
- `ZeroDivision.Throw` on any undefined metric.

## Oracle corpus

Two files, following the repository's one-per-family style, each stamped with the
scikit-learn version that produced it so a future diff explains itself:

- `tests/oracles/classification_metrics.json`
- `tests/oracles/roc_auc.json`

Generated by two new sections in `tools/generate_oracles.py`. scikit-learn is
already in the oracle environment, so `tools/requirements.txt` is unchanged.

Fixtures, each in an **unweighted and a weighted** variant (weights drawn from a
fixed seed):

1. binary, balanced;
2. binary, heavily imbalanced;
3. multiclass, 3 classes;
4. multiclass, 10 classes;
5. a class present in `yTrue` but never predicted;
6. a class predicted but absent from `yTrue`;
7. a perfect classifier;
8. an all-wrong classifier;
9. a single sample;
10. a single class;
11. `labels` as a strict subset;
12. non-contiguous label values (`{-1, 5, 42}`) — this one catches any
    implementation that assumes `0..k-1`.

Frozen per fixture: the confusion matrix; accuracy; precision, recall and F1 in
all four averaging modes **plus** the per-class form; FBeta at β ∈ {0.5, 2}; the
report text at `digits` ∈ {2, 3}; and `zero_division` ∈ {0, 1} on the
absent-class fixtures.

The ROC corpus covers binary balanced / imbalanced / heavily tied / weighted, and
multiclass at 3 and 5 classes with rows normalised to sum to 1, across `ovr` and
`ovo` × `macro` and `weighted` (`ovo` unweighted only, since sklearn refuses
weights there).

Generation stays deterministic — fixed seed, no wall-clock, no unordered
iteration — so the `Oracles are reproducible` job passes.

## Tests

- `tests/DataNet.Metrics.Tests` replays both corpora: `1e-9` tolerance for
  numbers, **exact** comparison for the report text.
- Unit tests for every error above and for each `ZeroDivision` member.
- `tests/DataNet.Metrics.NetStandard.Tests` links the same sources and pins the
  reference to the netstandard2.0 build via `SetTargetFramework`, matching the
  three existing mirrors.

## Performance

**Merge gate: processor time ≥ 1× against scikit-learn on every measured
operation at every measured size.** CI cannot hold this gate — benchmark results
are machine-specific and git-ignored — so the measurement is attached to the pull
request body and captured in `docs/guides/performance.md`.

Measured on the three tiers `bench/README.md` already defines:

1. **Intra-C#** (BenchmarkDotNet): every metric at n ∈ {10³, 10⁵, 10⁶},
   k ∈ {2, 10}.
2. **net10 vs netstandard2.0**: free once tier 1 exists, via the linked sources
   in `DataNet.NetStandard.Benchmarks`.
3. **Cross-language**: `bench/python/bench_metrics.py` against
   `MetricsCrossLang.cs` — same corpus, same per-operation metric, same
   auto-scaling best-of-N harness, wall **and** processor time, extended
   `bench/compare.py`.

Corpus: `bench/corpus/generate_metrics.py` writes `bench/corpus/metrics/`
(git-ignored, fixed seed), read by both sides.

The benchmarks go in `bench/DataNet.Text.Benchmarks` rather than a new project.
That project already covers `DataNet.Embeddings` as well as `DataNet.Text`, and
more importantly the netstandard2.0 mirror is wired there and nowhere else —
duplicating that wiring would duplicate the fragile part `bench/README.md`
documents at length.

### Design consequences, taken up front

- Label → index mapping is on the hot path: a direct offset table when
  `max − min` is small relative to *n*, binary search over the sorted label array
  otherwise. Not an unconditional `Dictionary`.
- One pass for the matrix, into a contiguous `double[]` indexed flat — `double[,]`
  access goes through a call. `ToArray()` materialises the 2-D shape only when
  asked.
- ROC sorts an array of `(score, label, weight)` structs for locality, rather
  than an index array that dereferences on every comparison.
- No LINQ, no per-sample allocation.

### Where the margin is expected, and where it is thin

sklearn pays heavily per call: `_check_targets`, `type_of_target`,
`unique_labels`, and a `confusion_matrix` that builds a sparse COO matrix and
densifies it — several passes, several allocations, interpreted glue around them.
Everything derived from the confusion matrix, and the report's pure-Python text
formatting, should win comfortably.

The thin one is `roc_auc_score` at large *n*, dominated by a numpy `argsort` in
well-optimised C. Our sort will be the same order of magnitude and the margin has
to come from the rest of the call. If that row falls below 1×, the options are a
radix sort over the `double` bit patterns or splitting ROC-AUC out of this
branch — it is the one piece whose removal leaves a coherent whole.

## Documentation

- `docs/decisions/0013-metrics-package-placement.md` — the package choice, and
  why the confusion matrix is public rather than an internal detail.
- `docs/equivalence.md` — a new section, one row per function, naming the
  `sklearn.metrics` call and any deliberate divergence.
- `docs/migration/sklearn.md` — the "check the definitions" bullet becomes a real
  section: macro vs micro vs weighted explained once, with a worked example where
  the three differ, and a pointer to the implementation.
- `docs/migration/README.md` — the scikit-learn row moves metrics from *decide*
  to *build*; "What DataNet writes natively" gains a fifth lot.
- `docs/guides/performance.md` — the captured cross-language table.
- `bench/README.md` — a fifth section for the metrics harness.
- XML documentation on every public member, naming the Python function it
  matches (`CONTRIBUTING.md`, definition of done, point 4).

## Out of scope

`normalize=` on the confusion matrix; `balanced_accuracy`, `matthews_corrcoef`,
`cohen_kappa`; regression metrics (MSE, MAE, R²); clustering metrics; `roc_curve`
and precision-recall curves as plot data; persisting a `ClassificationReport` as
a versioned artifact.

## Follow-up issues to open

1. `TokenizationResult` in `src/DataNet.Embeddings/Tokenization/WordPieceTokenizer.cs`
   is a record over two `IReadOnlyList<T>` members, so its synthesised equality
   compares by reference — the same trap this design avoids for
   `ClassificationReport`.
2. Regression metrics as a second lot in `DataNet.Metrics`.
3. The neighbouring classification metrics listed as out of scope above.
