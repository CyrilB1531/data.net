# Parallel multiclass ROC-AUC — implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a caller of `RocAuc.MultiClass` run the per-class (one-vs-rest) and
per-pair (one-vs-one) loops on several threads, opt-in and sequential by default,
with results bit-identical to the sequential path.

**Architecture:** The four trailing optional parameters of `RocAuc.MultiClass`
become one `readonly ref struct MultiClassRocOptions` carrying a
`MaxDegreeOfParallelism`. The per-class scoring body is extracted into one static
kernel taking `(offset, stride)` into a score buffer; the sequential driver calls
it with the caller's row-major span in place, and the parallel driver calls it
from a `Parallel.For` over a rented column-major transpose, because a
`ReadOnlySpan<T>` cannot be captured by a lambda. Scratch buffers become one
rental per worker via `localInit`/`localFinally`.

**Tech Stack:** C# on `net10.0` and `netstandard2.0`, xUnit, BenchmarkDotNet plus
the hand-rolled `CrossLang/Harness.cs` timing loop, `ArrayPool<T>`,
`System.Threading.Tasks.Parallel`, `ExceptionDispatchInfo`.

**Spec:** `docs/superpowers/specs/2026-08-08-parallel-multiclass-roc-auc-design.md`

**Issue:** [#86](https://github.com/CyrilB1531/data.net/issues/86) ·
**Branch:** `perf/86-parallelise-multiclass-roc-auc` (already created off `main`)

## Global Constraints

- **Warnings are errors repository-wide** (`TreatWarningsAsErrors` in the root
  `Directory.Build.props`), across `src`, `tests` and `bench`. A missing XML doc
  comment on a public member is a build error, and so is a SonarAnalyzer finding.
- **Every public member carries XML documentation naming the Python function it
  matches** where one exists.
- **Two target frameworks:** `net10.0;netstandard2.0`. Everything used must exist
  in the netstandard2.0 contract — `Parallel`, `ArrayPool<T>` (via the
  `System.Memory` package this project already references) and
  `ExceptionDispatchInfo` all do.
- **`tests/oracles/roc_auc.json` is never regenerated and must not change by a
  single byte.** The CI job "Oracles are reproducible" compares the committed
  corpora against a fresh generation.
- **`DataNet.Metrics` stays at version `0.1.0`** — it has never been published,
  so the changed surface is an unreleased one. The CHANGELOG entry goes under the
  existing `### DataNet.Metrics — 0.1.0` heading.
- **No `unsafe`.** `DataNet.Text` sets `AllowUnsafeBlocks=false` deliberately; no
  project in `src/` enables it, and this work does not change that.
- **No new package dependency.** `DataNet.Metrics` is dependency-free by design.
- **Analyzer suppressions need a reason a reviewer can disagree with**, written as
  a comment beside the `#pragma`, per `CONTRIBUTING.md`. See the existing examples
  in `src/DataNet.Metrics/Internal/BinaryRoc.cs:110-155`.
- **Definition of done, run before every commit that touches code:**

  ```bash
  dotnet build DataNet.slnx -c Release
  dotnet test DataNet.slnx -c Release
  dotnet format DataNet.slnx --verify-no-changes
  npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
  ```

## File Structure

| File | Responsibility |
| --- | --- |
| `src/DataNet.Metrics/MultiClassRocOptions.cs` | **New.** The public options `ref struct`: strategy, averaging, labels, weights, worker count. |
| `src/DataNet.Metrics/RocAuc.cs` | Public entry point. `MultiClass` loses four parameters and gains `options`. |
| `src/DataNet.Metrics/Internal/BinaryRoc.cs` | Gains a nested `Scratch` (four pooled buffers, one rental per worker) and a `Score` overload that takes one. `Point` stays private to this class. |
| `src/DataNet.Metrics/Internal/MultiClassRoc.cs` | Validation, the `(offset, stride)` scoring kernel, and four drivers: sequential/parallel × one-vs-rest/one-vs-one. |
| `tests/DataNet.Metrics.Tests/RocAucMultiClassTests.cs` | Existing suite, migrated to the options form; gains the options-encoding tests. |
| `tests/DataNet.Metrics.Tests/RocAucParallelTests.cs` | **New.** Bit-identity of parallel against sequential over the frozen corpus, and exception determinism. |
| `bench/DataNet.Text.Benchmarks/CrossLang/RocParallelBench.cs` | **New.** The `roc-parallel` harness mode: seeded inputs, wall and processor time per `(shape, dop)`. |
| `bench/DataNet.Text.Benchmarks/Program.cs` | Routes the `roc-parallel` argument. |
| `samples/DataNet.Sample/Lot5Metrics.cs` | Migrated call sites; must reference a `MultiClassRocOptions` **member** so `PackagingGate` sees the new type as exercised. |
| `docs/decisions/0017-multiclass-roc-auc-parallelism-is-opt-in.md` | **New.** The ADR the issue requires. |
| `docs/guides/performance.md` | The measured before/after, on elapsed time, with processor time beside it. |
| `docs/equivalence.md` | The `roc_auc_score(…, multi_class=…)` row mentions the options type. |
| `CHANGELOG.md` | Entry under `### DataNet.Metrics — 0.1.0`. |
| `bench/README.md` | The new mode's command line, beside the existing `compare-*` ones. |

---

### Task 1: The `MultiClassRocOptions` surface

Behaviour does not change in this task: `MaxDegreeOfParallelism` is validated and
carried, and the computation stays sequential. The tests written here are the
regression net that Task 3 must not break.

**Files:**

- Create: `src/DataNet.Metrics/MultiClassRocOptions.cs`
- Modify: `src/DataNet.Metrics/RocAuc.cs:31-53`
- Modify: `src/DataNet.Metrics/Internal/MultiClassRoc.cs:13-74`
- Modify: `tests/DataNet.Metrics.Tests/RocAucMultiClassTests.cs` (11 call sites)
- Modify: `samples/DataNet.Sample/Lot5Metrics.cs:188-193`
- Modify: `bench/DataNet.Text.Benchmarks/CrossLang/MetricsCrossLang.cs:108`
- Modify: `docs/equivalence.md:147`

**Interfaces:**

- Consumes: nothing.
- Produces:
  - `public readonly ref struct DataNet.Metrics.MultiClassRocOptions` with
    `MultiClassStrategy Strategy { get; init; }`,
    `Averaging? Average { get; init; }`,
    `ReadOnlySpan<int> Labels { get; init; }`,
    `ReadOnlySpan<double> SampleWeight { get; init; }`,
    `int MaxDegreeOfParallelism { get; init; }`.
  - `public static double RocAuc.MultiClass(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int classCount, MultiClassRocOptions options = default)`.
  - `internal static double MultiClassRoc.Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int classCount, MultiClassRocOptions options)`.

- [x] **Step 1: Write the failing tests**

Append to `tests/DataNet.Metrics.Tests/RocAucMultiClassTests.cs`, before the
private `Rows` helper:

```csharp
    [Fact]
    public void Default_options_reproduce_one_vs_rest_macro_exactly()
    {
        int[] yTrue = [0, 1, 2, 2, 1, 0];
        double[] scores = Rows([[0.70, 0.20, 0.10], [0.10, 0.60, 0.30], [0.15, 0.25, 0.60],
                                [0.20, 0.20, 0.60], [0.30, 0.50, 0.20], [0.55, 0.30, 0.15]]);

        double implicitDefaults = RocAuc.MultiClass(yTrue, scores, 3);
        double spelledOut = RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions
        {
            Strategy = MultiClassStrategy.OneVsRest,
            Average = Averaging.Macro,
        });

        Assert.Equal(BitConverter.DoubleToInt64Bits(spelledOut), BitConverter.DoubleToInt64Bits(implicitDefaults));
    }

    [Fact]
    public void Zero_and_one_workers_are_the_same_sequential_path()
    {
        int[] yTrue = [0, 1, 2, 2, 1, 0];
        double[] scores = Rows([[0.70, 0.20, 0.10], [0.10, 0.60, 0.30], [0.15, 0.25, 0.60],
                                [0.20, 0.20, 0.60], [0.30, 0.50, 0.20], [0.55, 0.30, 0.15]]);

        double zero = RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions { MaxDegreeOfParallelism = 0 });
        double one = RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions { MaxDegreeOfParallelism = 1 });

        Assert.Equal(BitConverter.DoubleToInt64Bits(one), BitConverter.DoubleToInt64Bits(zero));
    }

    [Fact]
    public void Rejects_a_negative_degree_of_parallelism()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.2], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6]]);

        Assert.Throws<ArgumentOutOfRangeException>(() => RocAuc.MultiClass(
            yTrue, scores, 3, new MultiClassRocOptions { MaxDegreeOfParallelism = -1 }));
    }

    [Fact]
    public void Rejects_binary_averaging_even_though_it_is_the_enum_default()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.2], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6]]);

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(
            yTrue, scores, 3, new MultiClassRocOptions { Average = Averaging.Binary }));
    }
```

The last test is the one that matters most: it pins the `Averaging?` encoding. If
someone later "simplifies" the property to a non-nullable `Averaging`, `default`
starts meaning `Binary` and this test fails instead of every caller failing.

- [x] **Step 2: Run the tests to verify they fail**

```bash
dotnet test tests/DataNet.Metrics.Tests/DataNet.Metrics.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RocAucMultiClassTests"
```

Expected: compile error, `CS0246: The type or namespace name 'MultiClassRocOptions' could not be found`.

- [x] **Step 3: Create the options type**

`src/DataNet.Metrics/MultiClassRocOptions.cs`:

```csharp
namespace DataNet.Metrics;

/// <summary>
/// The optional settings of <see cref="RocAuc.MultiClass"/> — scikit-learn's
/// <c>multi_class</c>, <c>average</c>, <c>labels</c> and <c>sample_weight</c>
/// arguments to <c>roc_auc_score</c>, plus the parallelism this library adds and
/// scikit-learn has no equivalent for.
/// </summary>
/// <remarks>
/// <para>
/// A <c>ref struct</c> because <see cref="Labels"/> and <see cref="SampleWeight"/>
/// are spans, which nothing else can hold as a field; any other shape would turn
/// them into arrays and impose an allocation on every caller. Build it at the call
/// site — it cannot be stored in a field, captured by a lambda, or held across an
/// <c>await</c>.
/// </para>
/// <para>
/// <c>default</c> reproduces scikit-learn's own defaults: one-vs-rest, macro
/// averaging, labels read from <c>yTrue</c>, no sample weights, one thread.
/// </para>
/// </remarks>
public readonly ref struct MultiClassRocOptions
{
    /// <summary>
    /// One-vs-rest or one-vs-one (<c>multi_class=</c>). Defaults to
    /// <see cref="MultiClassStrategy.OneVsRest"/>.
    /// </summary>
    public MultiClassStrategy Strategy { get; init; }

    /// <summary>
    /// <see cref="Averaging.Macro"/> or <see cref="Averaging.Weighted"/>
    /// (<c>average=</c>). <see langword="null"/> — the default — means
    /// <see cref="Averaging.Macro"/>, and is nullable for a reason:
    /// <c>default(Averaging)</c> is <see cref="Averaging.Binary"/>, which
    /// multiclass ROC-AUC refuses, so a non-nullable property would make
    /// <c>default</c> of this type throw instead of meaning the default.
    /// </summary>
    public Averaging? Average { get; init; }

    /// <summary>
    /// The classes the score columns stand for, sorted ascending and unique
    /// (<c>labels=</c>). Empty — the default — reads the sorted distinct labels of
    /// <c>yTrue</c>. Pass it when a class is absent from <c>yTrue</c>.
    /// </summary>
    public ReadOnlySpan<int> Labels { get; init; }

    /// <summary>
    /// A weight per sample (<c>sample_weight=</c>). Empty — the default — weights
    /// every sample by 1. Refused with <see cref="MultiClassStrategy.OneVsOne"/>,
    /// which scikit-learn also refuses.
    /// </summary>
    public ReadOnlySpan<double> SampleWeight { get; init; }

    /// <summary>
    /// How many workers run the per-class loop (one-vs-rest) or the per-pair loop
    /// (one-vs-one). 0 and 1 — the default — are sequential, and the sequential
    /// path is unchanged: it reads the caller's spans in place and copies nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The result is bit-identical whatever this is set to: every class and every
    /// pair writes its own slot, and the averaging runs afterwards on the calling
    /// thread in array order.
    /// </para>
    /// <para>
    /// Above 1, the inputs are copied. A span cannot be handed to another thread,
    /// so the parallel path rents a copy of <c>yTrue</c>, of the sample weights if
    /// any, and a transposed copy of the score matrix — about
    /// <c>samples × classes × 8</c> bytes, returned to the pool on the way out.
    /// That is the price of the opt-in, which is why the default does not pay it.
    /// </para>
    /// <para>
    /// The setting is honoured as given, at any input size, and there is no
    /// sentinel for "all cores": write <see cref="Environment.ProcessorCount"/> if
    /// that is what is meant, so the number is visible at the call site.
    /// scikit-learn does not parallelise <c>roc_auc_score</c> at all — see
    /// <c>docs/decisions/0017-multiclass-roc-auc-parallelism-is-opt-in.md</c>.
    /// </para>
    /// </remarks>
    public int MaxDegreeOfParallelism { get; init; }
}
```

- [x] **Step 4: Rewrite the public entry point**

Replace the `MultiClass` method in `src/DataNet.Metrics/RocAuc.cs` (lines 31-52)
with:

```csharp
    /// <summary>
    /// The multiclass case —
    /// <c>roc_auc_score(y_true, y_score, multi_class=…, average=…, labels=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true labels, one per sample.</param>
    /// <param name="yScore">Class probabilities, row-major: sample 0's classes, then sample 1's. Length must be <paramref name="classCount"/> times the sample count, and each row must sum to 1.</param>
    /// <param name="classCount">How many classes each row scores.</param>
    /// <param name="options">Strategy, averaging, labels, sample weights and worker count. <c>default</c> is scikit-learn's own defaults, on one thread.</param>
    /// <exception cref="ArgumentException">Any of the rules above is broken.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="classCount"/> is below two, or <see cref="MultiClassRocOptions.MaxDegreeOfParallelism"/> is negative.</exception>
    public static double MultiClass(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int classCount,
        MultiClassRocOptions options = default) =>
        MultiClassRoc.Score(yTrue, yScore, classCount, options);
```

- [x] **Step 5: Thread the options through the internal entry point**

In `src/DataNet.Metrics/Internal/MultiClassRoc.cs`, replace `Score` and `Validate`
(lines 13-74) with:

```csharp
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int classCount,
        MultiClassRocOptions options)
    {
        Averaging average = options.Average ?? Averaging.Macro;
        int n = Validate(yTrue, yScore, classCount, options, average);
        int[] classes = ResolveLabels(yTrue, options.Labels, classCount);
        ValidateRowSums(yScore, n, classCount);

        return options.Strategy == MultiClassStrategy.OneVsRest
            ? OneVsRest(yTrue, yScore, classes, average, options.SampleWeight)
            : OneVsOne(yTrue, yScore, classes, average);
    }

    private static int Validate(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int classCount,
        MultiClassRocOptions options, Averaging average)
    {
        int n = yTrue.Length;
        if (options.MaxDegreeOfParallelism < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options), options.MaxDegreeOfParallelism,
                "MaxDegreeOfParallelism cannot be negative. 0 and 1 are both sequential.");
        }
        if (n == 0)
        {
            throw new ArgumentException("yTrue is empty; there is nothing to score.", nameof(yTrue));
        }
        if (classCount < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(classCount), classCount, "Multiclass ROC AUC needs at least two classes.");
        }
        if (yScore.Length != (long)n * classCount)
        {
            throw new ArgumentException(
                $"yScore has {yScore.Length} entries; {n} samples over {classCount} classes needs {(long)n * classCount}.",
                nameof(yScore));
        }
        if (average is not (Averaging.Macro or Averaging.Weighted))
        {
            throw new ArgumentException(
                "Multiclass ROC AUC accepts only Averaging.Macro and Averaging.Weighted, as scikit-learn does.",
                nameof(options));
        }
        if (!options.SampleWeight.IsEmpty)
        {
            if (options.SampleWeight.Length != n)
            {
                throw new ArgumentException(
                    $"sampleWeight has {options.SampleWeight.Length} entries but there are {n} samples.",
                    nameof(options));
            }
            if (options.Strategy == MultiClassStrategy.OneVsOne)
            {
                throw new ArgumentException(
                    "scikit-learn does not support sampleWeight for one-vs-one ROC AUC, and neither does this.",
                    nameof(options));
            }
        }

        return n;
    }
```

The three `nameof(...)` arguments that used to name `average` and `sampleWeight`
now name `options`, because that is the parameter the caller actually passes.
`ArgumentException.Message` gains `(Parameter 'options')` in place of
`(Parameter 'average')` — no test asserts on those strings, and the tests below
assert the exception *type*.

- [x] **Step 6: Migrate the 11 test call sites**

In `tests/DataNet.Metrics.Tests/RocAucMultiClassTests.cs`, replace each positional
call. The corpus test at line 26:

```csharp
            double actual = RocAuc.MultiClass(yTrue, scores, classCount, new MultiClassRocOptions
            {
                Strategy = strategy,
                Average = average,
                SampleWeight = weight,
            });
```

Line 41 (`Rejects_one_vs_one_with_sample_weights_as_sklearn_does`):

```csharp
        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions
        {
            Strategy = MultiClassStrategy.OneVsOne,
            SampleWeight = weight,
        }));
```

Line 70 (`Rejects_micro_averaging`):

```csharp
        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(
            yTrue, scores, 3, new MultiClassRocOptions { Average = Averaging.Micro }));
```

Lines 81 and 130 (`Rejects_unsorted_labels`, `Rejects_labels_whose_count_disagrees_with_class_count`):

```csharp
        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(
            yTrue, scores, 3, new MultiClassRocOptions { Labels = labels }));
```

Line 110 (`Rejects_a_sample_weight_whose_length_disagrees_with_y_true`):

```csharp
        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(
            yTrue, scores, 3, new MultiClassRocOptions { SampleWeight = weight }));
```

The five three-argument calls (lines 52, 61, 91, 100, 120) need no change.

**Watch out:** a `ref struct` cannot be captured by a lambda, but *constructing*
one inside a lambda body is fine — which is what every `Assert.Throws` above does.
Hoisting `new MultiClassRocOptions { … }` into a local outside the lambda would
not compile.

- [x] **Step 7: Run the tests to verify they pass**

```bash
dotnet test tests/DataNet.Metrics.Tests/DataNet.Metrics.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RocAuc"
```

Expected: PASS, including the four new tests and the whole frozen corpus replay.

If SonarAnalyzer rejects the new type — `S3898`/`CA1815` ask value types to
implement `IEquatable<T>` and override `Equals`, neither of which a `ref struct`
holding spans can usefully do — suppress it *at the type* with the reason spelled
out, in the style of `BinaryRoc.cs:110-122`:

```csharp
// S3898/CA1815 ask a value type for value equality. This one holds two spans:
// comparing them would compare memory addresses, not contents, and a ref struct
// cannot implement IEquatable<T> in a way a caller could reach through a
// generic. There is nothing here for equality to mean — the type is an argument
// bundle, constructed at a call site and consumed immediately.
#pragma warning disable S3898, CA1815
public readonly ref struct MultiClassRocOptions
#pragma warning restore S3898, CA1815
```

- [x] **Step 8: Migrate the sample, and make `PackagingGate` see the new type**

In `samples/DataNet.Sample/Lot5Metrics.cs`, replace lines 188-193:

```csharp
        Console.WriteLine($"  MultiClass ovr macro  = "
            + $"{RocAuc.MultiClass(truth, probabilities, classCount: 3):F3}");
        Console.WriteLine($"  MultiClass ovr weight = "
            + $"{RocAuc.MultiClass(truth, probabilities, classCount: 3, new MultiClassRocOptions { Average = Averaging.Weighted }):F3}");
        Console.WriteLine($"  MultiClass ovo macro  = "
            + $"{RocAuc.MultiClass(truth, probabilities, classCount: 3, new MultiClassRocOptions { Strategy = MultiClassStrategy.OneVsOne }):F3}");

        // Opt-in parallelism over the per-class loop. Six samples and three
        // classes is far too small to gain anything — the point here is that the
        // number does not move, which is the guarantee the knob carries.
        Console.WriteLine($"  MultiClass ovr macro  = "
            + $"{RocAuc.MultiClass(truth, probabilities, classCount: 3, new MultiClassRocOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }):F3}"
            + "  (parallel, same value)");
```

`PackagingGate` requires a **member reference** to every exported public type, not
a mere type reference: the property setters above are what satisfy it for
`MultiClassRocOptions`. Verify:

```bash
dotnet run --project samples/DataNet.Sample -c Release
```

Expected: the four `MultiClass` lines print, the last two printing the same
number, and the gate does not fail the run.

- [x] **Step 9: Migrate the bench call site and the equivalence row**

`bench/DataNet.Text.Benchmarks/CrossLang/MetricsCrossLang.cs:108` keeps its
three-argument form and needs no edit — confirm with a build. In
`docs/equivalence.md:147`, replace the row's notes column:

```markdown
| `roc_auc_score(…, multi_class=…)` | scikit-learn | `RocAuc.MultiClass(…, MultiClassRocOptions)` | `ovr` and `ovo`. Separate method: the overloads would be ambiguous. Strategy, averaging, labels and weights travel in `MultiClassRocOptions`, which also carries `MaxDegreeOfParallelism` — no scikit-learn equivalent, opt-in, sequential by default. `sampleWeight` refused for `ovo`, as in scikit-learn. |
```

- [x] **Step 10: Full gate, then commit**

```bash
dotnet build DataNet.slnx -c Release
dotnet test DataNet.slnx -c Release
dotnet format DataNet.slnx --verify-no-changes
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
```

Expected: all four clean. Then:

```bash
git add src/DataNet.Metrics tests/DataNet.Metrics.Tests samples/DataNet.Sample docs/equivalence.md
git commit -m "$(cat <<'EOF'
Gather the multiclass ROC-AUC arguments into an options value

RocAuc.MultiClass had seven parameters and needs an eighth for the worker
count issue #86 asks for. A ref struct is the only shape that can carry
labels and sampleWeight, which are spans, without forcing an allocation
on the caller.

Average is nullable because default(Averaging) is Binary, which
multiclass ROC-AUC refuses: a non-nullable property would make
default(MultiClassRocOptions) throw instead of meaning the documented
defaults. A test pins that, so the "simplification" fails loudly.

Nothing computes differently yet. MaxDegreeOfParallelism is validated
and carried; honouring it is the next commit.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: One scoring kernel, one buffer rental per caller

Still sequential. This task extracts the per-class body into a kernel addressed by
`(offset, stride)` so that Task 3's parallel driver can feed it a transposed copy,
and moves `keys`/`points` out of `BinaryRoc.Score`'s allocation path — at
n=100 000 those are 800 KB and 1.6 MB, which is the large-object heap, whose
allocation takes a lock. Eight workers allocating 2.4 MB of LOH per class would
serialise the gain this whole plan exists to collect.

**Files:**

- Modify: `src/DataNet.Metrics/Internal/BinaryRoc.cs`
- Modify: `src/DataNet.Metrics/Internal/MultiClassRoc.cs:134-253`

**Interfaces:**

- Consumes: `MultiClassRoc.Score(…, MultiClassRocOptions)` from Task 1.
- Produces:
  - `internal sealed class BinaryRoc.Scratch` with
    `internal static Scratch Rent(int minimumLength)`, `internal void Return()`,
    `internal int[] Binary { get; }`, `internal double[] Column { get; }`.
  - `internal static double BinaryRoc.Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel, ReadOnlySpan<double> sampleWeight, Scratch scratch)`.
  - `private static double MultiClassRoc.ClassScore(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> scores, int offset, int stride, int positiveLabel, ReadOnlySpan<double> sampleWeight, Scratch scratch, out double positiveWeight)`.
  - `private static double MultiClassRoc.PairScore(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> scores, int offset, int stride, int labelA, int labelB, int positiveLabel, Scratch scratch)`.

- [x] **Step 1: Capture the current output bit-for-bit, before touching anything**

This refactor must not move a value, and the committed corpus test only asserts
1e-9. So take a raw-bits fingerprint of the current code first. Create
`/tmp/claude-49201103/-home-cyril-Documents-devs-data-net2/7a731faa-cc89-49bb-ba20-60f8be57968a/scratchpad/BitsFingerprint.cs`
as a temporary test file, copied into the test project:

```csharp
using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

// TEMPORARY — not committed. Writes every corpus value's raw bits so the
// (offset, stride) refactor can be diffed against pre-refactor output.
public sealed class BitsFingerprint
{
    [Fact]
    public void Dump()
    {
        var lines = new List<string>();
        for (int i = 0; i < RocCorpus.Cases.Count; i++)
        {
            JsonElement c = RocCorpus.Cases[i];
            if (c.GetProperty("kind").GetString() != "multiclass")
            {
                continue;
            }
            int[] yTrue = RocCorpus.YTrue(c);
            double[] scores = RocCorpus.RowMajorScores(c);
            double[] weight = RocCorpus.SampleWeight(c);
            int classCount = c.GetProperty("class_count").GetInt32();

            foreach (JsonProperty entry in c.GetProperty("values").EnumerateObject())
            {
                string[] parts = entry.Name.Split('|');
                double actual = RocAuc.MultiClass(yTrue, scores, classCount, new MultiClassRocOptions
                {
                    Strategy = parts[0] == "ovr" ? MultiClassStrategy.OneVsRest : MultiClassStrategy.OneVsOne,
                    Average = parts[1] == "macro" ? Averaging.Macro : Averaging.Weighted,
                    SampleWeight = weight,
                });
                lines.Add($"{i} {entry.Name} {BitConverter.DoubleToInt64Bits(actual):x16}");
            }
        }

        File.WriteAllLines(Environment.GetEnvironmentVariable("BITS_OUT")!, lines);
    }
}
```

```bash
SCRATCH=/tmp/claude-49201103/-home-cyril-Documents-devs-data-net2/7a731faa-cc89-49bb-ba20-60f8be57968a/scratchpad
cp $SCRATCH/BitsFingerprint.cs tests/DataNet.Metrics.Tests/
BITS_OUT=$SCRATCH/bits-before.txt dotnet test tests/DataNet.Metrics.Tests/DataNet.Metrics.Tests.csproj \
  -c Release --filter "FullyQualifiedName~BitsFingerprint" -e BITS_OUT=$SCRATCH/bits-before.txt
wc -l $SCRATCH/bits-before.txt
```

Expected: a non-empty file, one line per multiclass case and averaging key.

- [x] **Step 2: Give `BinaryRoc` a scratch type and a `Score` that takes one**

Replace `src/DataNet.Metrics/Internal/BinaryRoc.cs` lines 13-36 with:

```csharp
internal static class BinaryRoc
{
    private struct Point
    {
        public double Weight;
        public double PositiveWeight;
    }

    /// <summary>
    /// The four buffers one ROC curve needs, rented once and reused across
    /// curves. Going parallel means one of these per worker — never one per
    /// class, which is what the sequential loop's shared buffers already avoid,
    /// and never one per call, which is what <c>keys</c> and <c>points</c> used
    /// to be. At n=100 000 those two are 800 KB and 1.6 MB: large-object heap,
    /// whose allocation takes a lock that eight workers would queue on.
    /// </summary>
    internal sealed class Scratch
    {
        private readonly double[] _keys;
        private readonly Point[] _points;

        private Scratch(int[] binary, double[] column, double[] keys, Point[] points)
        {
            Binary = binary;
            Column = column;
            _keys = keys;
            _points = points;
        }

        internal int[] Binary { get; }

        internal double[] Column { get; }

        internal static Scratch Rent(int minimumLength)
        {
            int length = Math.Max(1, minimumLength);
            return new Scratch(
                ArrayPool<int>.Shared.Rent(length),
                ArrayPool<double>.Shared.Rent(length),
                ArrayPool<double>.Shared.Rent(length),
                ArrayPool<Point>.Shared.Rent(length));
        }

        internal void Return()
        {
            ArrayPool<int>.Shared.Return(Binary);
            ArrayPool<double>.Shared.Return(Column);
            ArrayPool<double>.Shared.Return(_keys);
            ArrayPool<Point>.Shared.Return(_points);
        }

        // _keys and _points never leave this class: only this Score touches them,
        // and Point is private to BinaryRoc, so exposing the array would be an
        // inconsistent-accessibility error as well as a wider surface than anyone
        // needs.
        internal double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel, ReadOnlySpan<double> sampleWeight)
        {
            int n = Validate(yTrue, yScore, sampleWeight);
            BuildPoints(yTrue, yScore, posLabel, sampleWeight, _keys, _points);
            Array.Sort(_keys, _points, 0, n);
            return Accumulate(_keys, _points, n);
        }
    }

    public static double Score(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel, ReadOnlySpan<double> sampleWeight)
    {
        Scratch scratch = Scratch.Rent(yTrue.Length);
        try
        {
            return scratch.Score(yTrue, yScore, posLabel, sampleWeight);
        }
        finally
        {
            scratch.Return();
        }
    }

    public static double Score(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel, ReadOnlySpan<double> sampleWeight,
        Scratch scratch) =>
        scratch.Score(yTrue, yScore, posLabel, sampleWeight);
```

`src/Shared/GlobalUsings.cs` provides only `DataNet.Internal`, so add
`using System.Buffers;` as the first line of this file — the way
`src/DataNet.Text/Distances/Lcs.cs:1` already does for the same type. Do not add
it to the shared global usings, which every package compiles.

Then adjust the two helpers below it — `BuildPoints` writes `[0, n)` of buffers
that may now be longer, and `Accumulate` must be told the length instead of
reading `keys.Length`:

```csharp
    private static double Accumulate(double[] keys, Point[] points, int n)
    {
        double truePositives = 0.0;
```

and inside it:

```csharp
            if (!IsLastOfGroup(keys, i, n))
```

with:

```csharp
    private static bool IsLastOfGroup(double[] keys, int i, int n)
    {
        // …existing S1244 comment, unchanged…
#pragma warning disable S1244
        return i == n - 1 || keys[i] != keys[i + 1];
#pragma warning restore S1244
    }
```

`BuildPoints` already loops on `yTrue.Length`, which is the right bound; it writes
`keys[i]` and `points[i]` for `i < n` and leaves the rented tail alone, which
nothing reads. Delete the two `new double[n]` / `new Point[n]` lines from the old
`Score` body — they are what this step exists to remove.

- [x] **Step 3: Verify the binary path is untouched**

```bash
dotnet test tests/DataNet.Metrics.Tests/DataNet.Metrics.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RocAucBinaryTests"
```

Expected: PASS. The binary corpus replays through the new rented-buffer path;
`Array.Sort(keys, points, 0, n)` on a longer buffer performs the same introsort
over the same values in the same starting order, so the permutation and therefore
every accumulated value is identical.

- [x] **Step 4: Extract the `(offset, stride)` kernel**

Replace `OneVsRest`, `PairContext`, `OneVsOne` and `PairScore` in
`src/DataNet.Metrics/Internal/MultiClassRoc.cs` (lines 134-253) with:

```csharp
    /// <summary>
    /// One binary ROC-AUC over a column of the score matrix, where the column is
    /// addressed as <c>scores[offset + (i * stride)]</c>.
    /// </summary>
    /// <remarks>
    /// The two callers hold the same numbers in two layouts, and this is where
    /// that difference is confined to two integers. The sequential driver passes
    /// the caller's row-major span with <c>offset = c</c> and <c>stride = k</c>,
    /// reading it in place; the parallel driver passes a column-major transpose
    /// with <c>offset = c * n</c> and <c>stride = 1</c>, because a span cannot be
    /// captured by a worker's lambda and the copy may as well be contiguous per
    /// column while it is being made.
    /// </remarks>
    private static double ClassScore(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> scores, int offset, int stride,
        int positiveLabel, ReadOnlySpan<double> sampleWeight, BinaryRoc.Scratch scratch,
        out double positiveWeight)
    {
        int n = yTrue.Length;
        int[] binary = scratch.Binary;
        double[] column = scratch.Column;
        bool weighted = !sampleWeight.IsEmpty;
        positiveWeight = 0.0;

        for (int i = 0; i < n; i++)
        {
            bool positive = yTrue[i] == positiveLabel;
            binary[i] = positive ? 1 : 0;
            column[i] = scores[offset + (i * stride)];
            if (positive)
            {
                positiveWeight += weighted ? sampleWeight[i] : 1.0;
            }
        }

        return BinaryRoc.Score(
            binary.AsSpan(0, n), column.AsSpan(0, n), 1, sampleWeight, scratch);
    }

    /// <summary>
    /// One ordering of one Hand &amp; Till pair: the samples of two classes only,
    /// scored with <paramref name="positiveLabel"/>'s column.
    /// </summary>
    private static double PairScore(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> scores, int offset, int stride,
        int labelA, int labelB, int positiveLabel, BinaryRoc.Scratch scratch)
    {
        int[] binary = scratch.Binary;
        double[] column = scratch.Column;
        int next = 0;

        for (int i = 0; i < yTrue.Length; i++)
        {
            if (yTrue[i] != labelA && yTrue[i] != labelB)
            {
                continue;
            }

            binary[next] = yTrue[i] == positiveLabel ? 1 : 0;
            column[next] = scores[offset + (i * stride)];
            next++;
        }

        return BinaryRoc.Score(
            binary.AsSpan(0, next), column.AsSpan(0, next), 1, default, scratch);
    }

    private static double OneVsRest(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes,
        Averaging average, ReadOnlySpan<double> sampleWeight)
    {
        int k = classes.Length;
        double[] scores = new double[k];
        double[] weights = new double[k];
        BinaryRoc.Scratch scratch = BinaryRoc.Scratch.Rent(yTrue.Length);

        try
        {
            for (int c = 0; c < k; c++)
            {
                scores[c] = ClassScore(
                    yTrue, yScore, c, k, classes[c], sampleWeight, scratch, out double positiveWeight);
                weights[c] = positiveWeight;
            }
        }
        finally
        {
            scratch.Return();
        }

        return average == Averaging.Macro ? Mean(scores) : WeightedMean(scores, weights);
    }

    private static double OneVsOne(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes, Averaging average)
    {
        int n = yTrue.Length;
        int k = classes.Length;
        (int A, int B)[] pairs = Pairs(k);
        double[] pairScores = new double[pairs.Length];
        double[] prevalence = new double[pairs.Length];
        BinaryRoc.Scratch scratch = BinaryRoc.Scratch.Rent(n);

        try
        {
            for (int pair = 0; pair < pairs.Length; pair++)
            {
                ScorePair(yTrue, yScore, classes, k, 1, pairs[pair], pair, pairScores, prevalence, scratch);
            }
        }
        finally
        {
            scratch.Return();
        }

        return average == Averaging.Macro ? Mean(pairScores) : WeightedMean(pairScores, prevalence);
    }

    /// <summary>
    /// The body of one pair, shared by the sequential and parallel drivers so the
    /// arithmetic exists once. Writes only its own two slots.
    /// </summary>
    private static void ScorePair(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> scores, int[] classes, int stride, int columnStride,
        (int A, int B) pair, int index, double[] pairScores, double[] prevalence, BinaryRoc.Scratch scratch)
    {
        int n = yTrue.Length;
        int labelA = classes[pair.A];
        int labelB = classes[pair.B];
        int size = 0;
        for (int i = 0; i < n; i++)
        {
            if (yTrue[i] == labelA || yTrue[i] == labelB)
            {
                size++;
            }
        }

        // Hand & Till: each ordering of the pair is scored with its own column,
        // and the two are averaged.
        int offsetA = columnStride == 1 ? pair.A * n : pair.A;
        int offsetB = columnStride == 1 ? pair.B * n : pair.B;
        int step = columnStride == 1 ? 1 : stride;
        double aScore = PairScore(yTrue, scores, offsetA, step, labelA, labelB, labelA, scratch);
        double bScore = PairScore(yTrue, scores, offsetB, step, labelA, labelB, labelB, scratch);

        pairScores[index] = (aScore + bScore) * 0.5;
        prevalence[index] = (double)size / n;
    }

    /// <summary>Every unordered class pair, in the order the nested loops produced.</summary>
    private static (int A, int B)[] Pairs(int k)
    {
        (int A, int B)[] pairs = new (int, int)[k * (k - 1) / 2];
        int next = 0;
        for (int a = 0; a < k; a++)
        {
            for (int b = a + 1; b < k; b++)
            {
                pairs[next++] = (a, b);
            }
        }
        return pairs;
    }
```

Two things to keep straight. `size` is now computed inside `ScorePair` from the
pair's own labels rather than in the outer loop — same count, same value, and it
is what lets a worker own a pair entirely. And `PairScore` derives its own length
from `next` instead of taking `size`, which is the same number by construction and
one fewer thing for a worker to get wrong.

- [x] **Step 5: Diff the bits against the pre-refactor fingerprint**

```bash
SCRATCH=/tmp/claude-49201103/-home-cyril-Documents-devs-data-net2/7a731faa-cc89-49bb-ba20-60f8be57968a/scratchpad
BITS_OUT=$SCRATCH/bits-after.txt dotnet test tests/DataNet.Metrics.Tests/DataNet.Metrics.Tests.csproj \
  -c Release --filter "FullyQualifiedName~BitsFingerprint" -e BITS_OUT=$SCRATCH/bits-after.txt
diff $SCRATCH/bits-before.txt $SCRATCH/bits-after.txt && echo "IDENTICAL"
```

Expected: `IDENTICAL`. A single differing line means the refactor moved a value —
stop and find it rather than proceeding; the most likely causes are a wrong
`(offset, stride)` pair, or `Accumulate` reading past `n` into rented garbage.

- [x] **Step 6: Remove the temporary fingerprint test and run the full suite**

```bash
rm tests/DataNet.Metrics.Tests/BitsFingerprint.cs
dotnet test DataNet.slnx -c Release
```

Expected: PASS. The fingerprint file is scaffolding for one diff and is not
committed — the durable guarantee is Task 3's sequential-against-parallel test.

- [x] **Step 7: Commit**

```bash
dotnet build DataNet.slnx -c Release && dotnet format DataNet.slnx --verify-no-changes
git add src/DataNet.Metrics
git commit -m "$(cat <<'EOF'
Address a score column by offset and stride, and rent its buffers once

Two preparations for the parallel driver, both worth having on their own.

The per-class body becomes one kernel reading scores[offset + i*stride],
so the row-major span the caller owns and the column-major copy a worker
must be given differ by two integers rather than by a second copy of the
arithmetic.

BinaryRoc.Score allocated keys and points on every call. At n=100 000
they are 800 KB and 1.6 MB — large-object heap, whose allocation takes a
lock. Eight workers would have queued on it and given back much of what
the threads were for. They are now rented once per caller, which also
makes the sequential path allocate less than it did.

Verified bit-for-bit: every multiclass corpus value's raw IEEE-754 bits
are unchanged against a fingerprint taken before the refactor.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: The parallel one-vs-rest driver, and deterministic exceptions

**Files:**

- Modify: `src/DataNet.Metrics/Internal/MultiClassRoc.cs`
- Create: `tests/DataNet.Metrics.Tests/RocAucParallelTests.cs`

**Interfaces:**

- Consumes: `ClassScore(…)`, `BinaryRoc.Scratch` from Task 2.
- Produces:
  - `private static double MultiClassRoc.OneVsRestParallel(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes, Averaging average, ReadOnlySpan<double> sampleWeight, int workers)`.
  - `private static (int[] Labels, double[] ColumnMajor, double[] Weights) MultiClassRoc.CopyForWorkers(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int classCount, ReadOnlySpan<double> sampleWeight)` — rented arrays, returned by the caller.
  - `private static void MultiClassRoc.RethrowFirst(Exception?[] failures)`.

- [x] **Step 1: Write the failing tests**

Create `tests/DataNet.Metrics.Tests/RocAucParallelTests.cs`:

```csharp
using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

/// <summary>
/// The guarantee issue #86 rests on: parallelising the per-class and per-pair
/// loops must not move a single bit. Not "within 1e-9" — identically. Every
/// class writes its own slot and the averaging runs afterwards on the calling
/// thread in array order, so if a value moves, the parallelisation is unsound
/// and the change is wrong.
/// </summary>
public sealed class RocAucParallelTests
{
    private static readonly int[] WorkerCounts = [2, 3, 8];

    [Theory]
    [MemberData(nameof(RocCorpus.MulticlassIndices), MemberType = typeof(RocCorpus))]
    public void Replays_the_frozen_corpus_bit_identically_in_parallel(int index)
    {
        JsonElement c = RocCorpus.Cases[index];
        int[] yTrue = RocCorpus.YTrue(c);
        double[] scores = RocCorpus.RowMajorScores(c);
        double[] weight = RocCorpus.SampleWeight(c);
        int classCount = c.GetProperty("class_count").GetInt32();

        foreach (JsonProperty entry in c.GetProperty("values").EnumerateObject())
        {
            string[] parts = entry.Name.Split('|');
            MultiClassStrategy strategy = parts[0] == "ovr"
                ? MultiClassStrategy.OneVsRest
                : MultiClassStrategy.OneVsOne;
            Averaging average = parts[1] == "macro" ? Averaging.Macro : Averaging.Weighted;

            double sequential = RocAuc.MultiClass(yTrue, scores, classCount, new MultiClassRocOptions
            {
                Strategy = strategy,
                Average = average,
                SampleWeight = weight,
            });

            foreach (int workers in WorkerCounts)
            {
                double parallel = RocAuc.MultiClass(yTrue, scores, classCount, new MultiClassRocOptions
                {
                    Strategy = strategy,
                    Average = average,
                    SampleWeight = weight,
                    MaxDegreeOfParallelism = workers,
                });

                Assert.Equal(
                    BitConverter.DoubleToInt64Bits(sequential),
                    BitConverter.DoubleToInt64Bits(parallel));
            }
        }
    }

    [Fact]
    public void Reports_the_lowest_offending_class_not_the_fastest_worker()
    {
        // Classes 1 and 2 both hold a NaN score, class 1 in an earlier column.
        // Sequential scoring meets class 1 first and names column 1's row; the
        // parallel path must name the same one however the workers are
        // scheduled, so an AggregateException or a race would fail here.
        int[] yTrue = [0, 1, 2, 0, 1, 2];
        double[] scores =
        [
            0.5, 0.3, 0.2,
            0.2, double.NaN, 0.3,
            0.1, 0.2, double.NaN,
            0.6, 0.2, 0.2,
            0.2, double.NaN, 0.3,
            0.1, 0.3, double.NaN,
        ];

        ArgumentException sequential = Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, 3));
        ArgumentException parallel = Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions { MaxDegreeOfParallelism = 8 }));

        Assert.Equal(sequential.Message, parallel.Message);
        Assert.Equal(sequential.ParamName, parallel.ParamName);
    }

    [Fact]
    public void A_class_absent_from_y_true_throws_the_same_way_in_parallel()
    {
        int[] yTrue = [0, 0, 1, 1];
        double[] scores = [0.9, 0.05, 0.05, 0.8, 0.1, 0.1, 0.1, 0.8, 0.1, 0.2, 0.7, 0.1];
        int[] labels = [0, 1, 2];

        ArgumentException sequential = Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions { Labels = labels }));
        ArgumentException parallel = Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions
            {
                Labels = labels,
                MaxDegreeOfParallelism = 8,
            }));

        Assert.Equal(sequential.Message, parallel.Message);
        Assert.Equal(sequential.ParamName, parallel.ParamName);
    }

    [Fact]
    public void More_workers_than_classes_is_not_an_error()
    {
        int[] yTrue = [0, 1, 0, 1];
        double[] scores = [0.9, 0.1, 0.2, 0.8, 0.7, 0.3, 0.4, 0.6];

        double sequential = RocAuc.MultiClass(yTrue, scores, 2);
        double parallel = RocAuc.MultiClass(yTrue, scores, 2,
            new MultiClassRocOptions { MaxDegreeOfParallelism = 64 });

        Assert.Equal(BitConverter.DoubleToInt64Bits(sequential), BitConverter.DoubleToInt64Bits(parallel));
    }
}
```

`RocAucParallelTests.cs` needs no `Rows` helper: the score matrices above are
written flat on purpose, because a NaN in a specific column is the point.

- [x] **Step 2: Run the tests to verify they fail**

```bash
dotnet test tests/DataNet.Metrics.Tests/DataNet.Metrics.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RocAucParallelTests"
```

Expected: the corpus theory and `More_workers_than_classes_is_not_an_error` PASS
(the setting is still ignored — that is what Task 1 left in place), and
`Reports_the_lowest_offending_class_not_the_fastest_worker` and
`A_class_absent_from_y_true_throws_the_same_way_in_parallel` also PASS for the
same reason. **This is the one task in the plan whose tests do not fail first**,
and pretending otherwise would be worse than saying it: they are written now so
that Step 4's switch to a real parallel driver is the thing they judge. Record the
pass, then make the driver real and watch them stay green.

- [x] **Step 3: Route to the parallel driver**

In `MultiClassRoc.Score`, replace the return statement:

```csharp
        int workers = Math.Max(1, options.MaxDegreeOfParallelism);

        if (options.Strategy == MultiClassStrategy.OneVsRest)
        {
            return workers == 1
                ? OneVsRest(yTrue, yScore, classes, average, options.SampleWeight)
                : OneVsRestParallel(yTrue, yScore, classes, average, options.SampleWeight, workers);
        }

        return workers == 1
            ? OneVsOne(yTrue, yScore, classes, average)
            : OneVsOneParallel(yTrue, yScore, classes, average, workers);
```

`OneVsOneParallel` arrives in Task 4. To keep this task's build green, add it now
as a one-line delegation that Task 4 replaces:

```csharp
    // Task 4 makes this parallel. Until then it is the sequential driver, which
    // is a correct answer to any worker count — just not a fast one.
    private static double OneVsOneParallel(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes, Averaging average, int workers) =>
        OneVsOne(yTrue, yScore, classes, average);
```

- [x] **Step 4: Write the copy, the driver and the rethrow**

Add to `src/DataNet.Metrics/Internal/MultiClassRoc.cs`:

```csharp
    /// <summary>
    /// The inputs, in a shape a worker thread can be handed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="ReadOnlySpan{T}"/> cannot be captured by the body of a
    /// <c>Parallel.For</c>:
    /// the caller's span may point at the stack, and nothing in the language lets
    /// it travel to another thread. Pinning it with <c>fixed</c> would cost
    /// nothing and is refused — no project in <c>src/</c> enables unsafe blocks,
    /// and a perf change does not reverse that.
    /// </para>
    /// <para>
    /// So the parallel path copies, and pays for it once: <c>yTrue</c>, the
    /// weights if any, and the score matrix <em>transposed</em>. The transpose
    /// costs the same single pass as a straight copy and leaves each class's
    /// column contiguous for the worker that reads it, instead of reads spaced
    /// <c>classCount</c> apart. Reading rows in order and scattering across
    /// <c>classCount</c> write streams is the right way round: the read side is
    /// then sequential, and hardware handles a handful of write streams well.
    /// </para>
    /// </remarks>
    private static (int[] Labels, double[] ColumnMajor, double[] Weights) CopyForWorkers(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int classCount, ReadOnlySpan<double> sampleWeight)
    {
        int n = yTrue.Length;
        int[] labels = ArrayPool<int>.Shared.Rent(n);
        double[] columnMajor = ArrayPool<double>.Shared.Rent(n * classCount);
        double[] weights = sampleWeight.IsEmpty
            ? []
            : ArrayPool<double>.Shared.Rent(n);

        yTrue.CopyTo(labels.AsSpan(0, n));
        if (!sampleWeight.IsEmpty)
        {
            sampleWeight.CopyTo(weights.AsSpan(0, n));
        }

        for (int i = 0; i < n; i++)
        {
            int row = i * classCount;
            for (int c = 0; c < classCount; c++)
            {
                columnMajor[(c * n) + i] = yScore[row + c];
            }
        }

        return (labels, columnMajor, weights);
    }

    private static void ReturnToPool((int[] Labels, double[] ColumnMajor, double[] Weights) copy)
    {
        ArrayPool<int>.Shared.Return(copy.Labels);
        ArrayPool<double>.Shared.Return(copy.ColumnMajor);
        if (copy.Weights.Length > 0)
        {
            ArrayPool<double>.Shared.Return(copy.Weights);
        }
    }

    /// <summary>
    /// One-vs-rest with the per-class loop spread over workers. Bit-identical to
    /// <see cref="OneVsRest"/>: class <c>c</c> writes <c>scores[c]</c> and
    /// <c>weights[c]</c> and nothing else, and the averaging below runs on this
    /// thread in array order, so no thread's timing can reach a sum.
    /// </summary>
    private static double OneVsRestParallel(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes,
        Averaging average, ReadOnlySpan<double> sampleWeight, int workers)
    {
        int n = yTrue.Length;
        int k = classes.Length;
        double[] scores = new double[k];
        double[] weights = new double[k];
        Exception?[] failures = new Exception?[k];
        var copy = CopyForWorkers(yTrue, yScore, k, sampleWeight);
        var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Min(workers, k) };

        try
        {
            Parallel.For(
                0,
                k,
                options,
                () => BinaryRoc.Scratch.Rent(n),
                (c, _, scratch) =>
                {
                    try
                    {
                        // Spans cannot cross into this lambda, but they can be
                        // made inside it: these are views over the arrays the
                        // closure captured, which is the whole point of the copy.
                        scores[c] = ClassScore(
                            copy.Labels.AsSpan(0, n),
                            copy.ColumnMajor.AsSpan(0, n * k),
                            c * n,
                            1,
                            classes[c],
                            copy.Weights.Length == 0 ? default : copy.Weights.AsSpan(0, n),
                            scratch,
                            out double positiveWeight);
                        weights[c] = positiveWeight;
                    }
                    catch (ArgumentException ex)
                    {
                        // Its own slot, so which worker lost the race cannot
                        // decide which exception the caller sees.
                        failures[c] = ex;
                    }

                    return scratch;
                },
                scratch => scratch.Return());
        }
        finally
        {
            ReturnToPool(copy);
        }

        RethrowFirst(failures);

        return average == Averaging.Macro ? Mean(scores) : WeightedMean(scores, weights);
    }

    /// <summary>
    /// Rethrows the failure of the lowest index, so a bad input produces the same
    /// exception the sequential path would have produced.
    /// </summary>
    /// <remarks>
    /// The loop above deliberately does not stop early.
    /// <see cref="ParallelLoopState.Stop"/> would cancel iterations that had not
    /// started, so a later class's exception could be reported where the
    /// sequential path reports an earlier class's. The error path therefore does
    /// all the work — it has no budget to defend — and
    /// <see cref="ExceptionDispatchInfo"/> rethrows the original instance rather
    /// than wrapping it, so type, message and <c>ParamName</c> survive and no
    /// <see cref="AggregateException"/> crosses the public API.
    /// </remarks>
    private static void RethrowFirst(Exception?[] failures)
    {
        foreach (Exception? failure in failures)
        {
            if (failure is not null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }
    }
```

Add the two usings this file needs. `System.Threading.Tasks` is already implicit
(`ImplicitUsings` is on repository-wide), so `Parallel` and `ParallelOptions`
resolve without help; `ArrayPool` and `ExceptionDispatchInfo` do not:

```csharp
using System.Buffers;
using System.Runtime.ExceptionServices;
```

- [x] **Step 5: Run the parallel tests**

```bash
dotnet test tests/DataNet.Metrics.Tests/DataNet.Metrics.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RocAucParallelTests"
```

Expected: PASS, all four. If the corpus theory fails on a `weighted=true` case,
the likely cause is the weights span: `copy.Weights.Length == 0 ? default : …` must
pass `default` and not a zero-length slice of an empty array, because
`ClassScore` decides `weighted` from `IsEmpty`.

If `Reports_the_lowest_offending_class_not_the_fastest_worker` fails with an
`AggregateException`, an exception escaped the `catch (ArgumentException)` — check
that nothing inside the body throws something else.

- [x] **Step 6: Run everything, on both target frameworks**

```bash
dotnet test DataNet.slnx -c Release
```

Expected: PASS, including `DataNet.Metrics.NetStandard.Tests`, which replays the
same suite against the netstandard2.0 build.

- [x] **Step 7: Commit**

```bash
dotnet build DataNet.slnx -c Release && dotnet format DataNet.slnx --verify-no-changes
git add src/DataNet.Metrics tests/DataNet.Metrics.Tests
git commit -m "$(cat <<'EOF'
Spread the one-vs-rest class loop over workers, bit for bit

Class c writes scores[c] and weights[c] and nothing else, and the
averaging runs afterwards on the calling thread in array order, so no
thread's timing can reach a sum. The frozen corpus replays identically at
2, 3 and 8 workers — compared as raw IEEE-754 bits, not within a
tolerance.

Two things had to be got right beyond the loop itself.

Spans cannot be captured by a worker's lambda, so the parallel path rents
a copy of yTrue, of the weights, and a transposed copy of the score
matrix. Each column is then contiguous for the worker reading it. That
copy is the price of the opt-in, and the reason the default does not pay
it.

Exceptions had to stay deterministic. Workers catch into their own slot,
the loop does not stop early — Stop could cancel an earlier class and
report a later one's failure — and the lowest index's exception is
rethrown through ExceptionDispatchInfo, so type, message and ParamName
match the sequential path and no AggregateException escapes.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: The parallel one-vs-one driver

One-vs-one is the heavier half — at k=10 it is 45 pairs and 90 curves — and it is
the same shape of change, over a flat pair index.

**Files:**

- Modify: `src/DataNet.Metrics/Internal/MultiClassRoc.cs`
- Modify: `tests/DataNet.Metrics.Tests/RocAucParallelTests.cs`

**Interfaces:**

- Consumes: `ScorePair(…)`, `Pairs(int)`, `CopyForWorkers(…)`, `ReturnToPool(…)`,
  `RethrowFirst(…)` from Tasks 2 and 3.
- Produces: `private static double MultiClassRoc.OneVsOneParallel(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes, Averaging average, int workers)` — replacing Task 3's sequential placeholder.

- [x] **Step 1: Write the failing test**

Append to `tests/DataNet.Metrics.Tests/RocAucParallelTests.cs`:

```csharp
    [Fact]
    public void One_vs_one_over_six_classes_is_bit_identical_in_parallel()
    {
        // 15 pairs and 30 curves, more pairs than workers and more workers than
        // any single pair needs: the shape where a per-pair race would show.
        const int k = 6;
        const int n = 240;
        int[] yTrue = new int[n];
        double[] scores = new double[n * k];
        var random = new Random(20260808);

        for (int i = 0; i < n; i++)
        {
            yTrue[i] = i % k;
            double total = 0.0;
            for (int c = 0; c < k; c++)
            {
                double draw = random.NextDouble() + (c == yTrue[i] ? 0.75 : 0.0);
                scores[(i * k) + c] = draw;
                total += draw;
            }
            for (int c = 0; c < k; c++)
            {
                scores[(i * k) + c] /= total;
            }
        }

        foreach (Averaging average in new[] { Averaging.Macro, Averaging.Weighted })
        {
            double sequential = RocAuc.MultiClass(yTrue, scores, k, new MultiClassRocOptions
            {
                Strategy = MultiClassStrategy.OneVsOne,
                Average = average,
            });

            foreach (int workers in WorkerCounts)
            {
                double parallel = RocAuc.MultiClass(yTrue, scores, k, new MultiClassRocOptions
                {
                    Strategy = MultiClassStrategy.OneVsOne,
                    Average = average,
                    MaxDegreeOfParallelism = workers,
                });

                Assert.Equal(
                    BitConverter.DoubleToInt64Bits(sequential),
                    BitConverter.DoubleToInt64Bits(parallel));
            }
        }
    }
```

The rows are normalised to sum to 1 because `ValidateRowSums` demands it within
numpy's `allclose` defaults; an un-normalised matrix would throw and the test
would pass for the wrong reason.

- [x] **Step 2: Run it to verify it passes for the wrong reason, and note that**

```bash
dotnet test tests/DataNet.Metrics.Tests/DataNet.Metrics.Tests.csproj -c Release \
  --filter "FullyQualifiedName~One_vs_one_over_six_classes"
```

Expected: PASS — Task 3's placeholder makes every worker count sequential. As in
Task 3, the test's job is to judge Step 3, and it must still pass afterwards.

- [x] **Step 3: Replace the placeholder with the real driver**

Replace the `OneVsOneParallel` placeholder in
`src/DataNet.Metrics/Internal/MultiClassRoc.cs` with:

```csharp
    /// <summary>
    /// One-vs-one with the per-pair loop spread over workers. Pair
    /// <c>p</c> writes <c>pairScores[p]</c> and <c>prevalence[p]</c> and nothing
    /// else; the averaging runs afterwards on this thread in array order.
    /// </summary>
    /// <remarks>
    /// The nested <c>(a, b)</c> loops become a flat range over a precomputed pair
    /// table — <c>k(k-1)/2</c> tuples, built once — because a triangular index is
    /// cheaper to read from a table than to decode arithmetically, and the table
    /// fixes the pair order to exactly the one the sequential driver used.
    /// </remarks>
    private static double OneVsOneParallel(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes, Averaging average, int workers)
    {
        int n = yTrue.Length;
        int k = classes.Length;
        (int A, int B)[] pairs = Pairs(k);
        double[] pairScores = new double[pairs.Length];
        double[] prevalence = new double[pairs.Length];
        Exception?[] failures = new Exception?[pairs.Length];
        var copy = CopyForWorkers(yTrue, yScore, k, default);
        var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Min(workers, pairs.Length) };

        try
        {
            Parallel.For(
                0,
                pairs.Length,
                options,
                () => BinaryRoc.Scratch.Rent(n),
                (pair, _, scratch) =>
                {
                    try
                    {
                        ScorePair(
                            copy.Labels.AsSpan(0, n),
                            copy.ColumnMajor.AsSpan(0, n * k),
                            classes,
                            k,
                            1,
                            pairs[pair],
                            pair,
                            pairScores,
                            prevalence,
                            scratch);
                    }
                    catch (ArgumentException ex)
                    {
                        failures[pair] = ex;
                    }

                    return scratch;
                },
                scratch => scratch.Return());
        }
        finally
        {
            ReturnToPool(copy);
        }

        RethrowFirst(failures);

        return average == Averaging.Macro ? Mean(pairScores) : WeightedMean(pairScores, prevalence);
    }
```

`CopyForWorkers(…, default)` passes no weights, which is correct and not an
oversight: `Validate` already refuses `SampleWeight` with `OneVsOne`, as
scikit-learn does.

- [x] **Step 4: Run the parallel suite**

```bash
dotnet test tests/DataNet.Metrics.Tests/DataNet.Metrics.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RocAucParallelTests"
```

Expected: PASS, all five. The corpus theory now exercises the real one-vs-one
driver on every `ovo|macro` and `ovo|weighted` key it holds.

- [x] **Step 5: Run everything and commit**

```bash
dotnet build DataNet.slnx -c Release
dotnet test DataNet.slnx -c Release
dotnet format DataNet.slnx --verify-no-changes
git add src/DataNet.Metrics tests/DataNet.Metrics.Tests
git commit -m "$(cat <<'EOF'
Spread the one-vs-one pair loop over workers

45 pairs and 90 curves at k=10 — the heaviest thing in the package, and
the same argument as one-vs-rest: pair p writes pairScores[p] and
prevalence[p] and nothing else, and the averaging runs afterwards on the
calling thread in array order.

The nested (a, b) loops become a flat range over a precomputed pair
table, which also pins the pair order to the one the sequential driver
used rather than trusting a triangular index decode to agree with it.

Six classes over 240 samples replay bit-identically at 2, 3 and 8 workers
for both macro and weighted averaging, alongside the frozen corpus.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: Measure it

The issue's acceptance criteria are specific: before and after on the same machine
in one sitting, **wall and processor time both reported**, elapsed time named as
the axis, a stated speed-up at k=10 and k=5 with the core count named, and no
regression on the small-input path.

BenchmarkDotNet reports elapsed time only, so the vehicle is
`CrossLang/Harness.cs`, which already records both from the same run.

**Files:**

- Create: `bench/DataNet.Text.Benchmarks/CrossLang/RocParallelBench.cs`
- Modify: `bench/DataNet.Text.Benchmarks/Program.cs:1-33`
- Modify: `bench/README.md`

**Interfaces:**

- Consumes: `Harness.Measure(string, Func<object>)`, `Harness.Write(string, Harness.Output)`, `RocAuc.MultiClass(…, MultiClassRocOptions)`.
- Produces: `internal static void RocParallelBench.Run()`, writing
  `bench/results/csharp-roc-parallel.json`.

- [x] **Step 1: Write the harness mode**

Create `bench/DataNet.Text.Benchmarks/CrossLang/RocParallelBench.cs`:

```csharp
using DataNet.Metrics;

namespace DataNet.Text.Benchmarks.CrossLang;

/// <summary>
/// The before-and-after for issue #86: multiclass ROC-AUC at several worker
/// counts, wall and processor time from the same run.
/// </summary>
/// <remarks>
/// <para>
/// Not a <c>compare-*</c> mode. Those are the matched face-offs against Python;
/// this is C# against C#, the sequential path against itself with more threads,
/// so there is no Python side and no shared corpus file to keep in step.
/// </para>
/// <para>
/// The inputs are generated here from a fixed seed rather than read from
/// <c>bench/corpus/metrics/</c>, which holds k=2 and k=10 only and stops its
/// score matrix at 100 000 rows. For a C#-against-C# comparison the only property
/// that matters is the same data on both sides, and a seeded generator guarantees
/// that more firmly than a committed file — while leaving #61's published corpus
/// and its table untouched.
/// </para>
/// <para>
/// Processor time is expected to rise with the worker count. That is not the
/// measurement failing; elapsed time is what this issue is about, and the ratio
/// of the two is printed so the cost is visible rather than implied.
/// </para>
/// </remarks>
internal static class RocParallelBench
{
    private static readonly (int Samples, int Classes)[] Shapes =
    [
        (1_000, 10),      // the small-input path: dispatch must not eat it
        (100_000, 5),
        (100_000, 10),
    ];

    private static readonly int[] WorkerCounts = [1, 2, 4, 8];

    public static void Run()
    {
        string outPath = Path.Combine(BenchCorpus.RepoRoot(), "bench", "results", "csharp-roc-parallel.json");

        Console.WriteLine($"C# multiclass ROC-AUC, sequential vs parallel — {Environment.ProcessorCount} logical cores");
        var results = new List<Harness.OperationResult>();

        foreach ((int n, int k) in Shapes)
        {
            (int[] yTrue, double[] scores) = Generate(n, k);

            foreach (MultiClassStrategy strategy in new[] { MultiClassStrategy.OneVsRest, MultiClassStrategy.OneVsOne })
            {
                // One-vs-one at k=10 is 45 pairs and 90 curves; at n=100 000 that
                // is minutes per repeat, so it is measured at the smaller shapes
                // and named as skipped rather than silently dropped.
                if (strategy == MultiClassStrategy.OneVsOne && n >= 100_000 && k == 10)
                {
                    Console.WriteLine("  ovo_n100000_k10 skipped: 45 pairs over 100 000 samples exceeds the harness's patience");
                    continue;
                }

                foreach (int workers in WorkerCounts)
                {
                    string name = $"{(strategy == MultiClassStrategy.OneVsRest ? "ovr" : "ovo")}_n{n}_k{k}_dop{workers}";
                    results.Add(Harness.Measure(name, () => RocAuc.MultiClass(yTrue, scores, k, new MultiClassRocOptions
                    {
                        Strategy = strategy,
                        Average = Averaging.Macro,
                        MaxDegreeOfParallelism = workers,
                    })));
                }
            }
        }

        Harness.Write(outPath, new Harness.Output
        {
            Metadata = new Harness.OutputMetadata
            {
                Side = "csharp",
                Library = $"DataNet.Metrics ({Environment.ProcessorCount} logical cores)",
                Runtime = Environment.Version.ToString(),
                Os = Environment.OSVersion.ToString(),
                MinTimeS = Harness.MinTimeSeconds,
                Repeats = Harness.RepeatCount,
            },
            Results = results,
        });
    }

    /// <summary>
    /// A separable multiclass problem: the true class gets a bonus draw, then the
    /// row is normalised, because <c>MultiClass</c> requires rows summing to 1.
    /// Seeded, so two runs of this harness score the same numbers.
    /// </summary>
    private static (int[] YTrue, double[] Scores) Generate(int n, int k)
    {
        int[] yTrue = new int[n];
        double[] scores = new double[n * k];
        var random = new Random(86);

        for (int i = 0; i < n; i++)
        {
            yTrue[i] = i % k;
            int row = i * k;
            double total = 0.0;
            for (int c = 0; c < k; c++)
            {
                double draw = random.NextDouble() + (c == yTrue[i] ? 0.75 : 0.0);
                scores[row + c] = draw;
                total += draw;
            }
            for (int c = 0; c < k; c++)
            {
                scores[row + c] /= total;
            }
        }

        return (yTrue, scores);
    }
}
```

**Watch out:** the lambda passed to `Harness.Measure` constructs the
`MultiClassRocOptions` *inside* its body. A `ref struct` cannot be captured, so
hoisting it into a local above the loop would not compile — and the
`foreach` variables `strategy` and `workers` are captured, which is fine.

- [x] **Step 2: Route the argument**

In `bench/DataNet.Text.Benchmarks/Program.cs`, add to the header comment after the
`compare-metrics` line:

```csharp
//   * "roc-parallel" -> multiclass ROC-AUC at several worker counts, C# against
//                       C#: the before/after for issue #86
```

and after the `compare-metrics` block:

```csharp
if (args.Length > 0 && args[0] == "roc-parallel")
{
    RocParallelBench.Run();
    return;
}
```

- [x] **Step 3: Build the bench project**

```bash
dotnet build bench/DataNet.Text.Benchmarks -c Release
```

Expected: clean. Warnings are errors here too.

- [x] **Step 4: Take the measurement, machine as quiet as it can be made**

```bash
uptime          # record the one-minute load average; it goes in the write-up
nproc
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- roc-parallel \
  | tee /tmp/claude-49201103/-home-cyril-Documents-devs-data-net2/7a731faa-cc89-49bb-ba20-60f8be57968a/scratchpad/roc-parallel.txt
uptime          # and again afterwards
```

Expected: one line per `(strategy, shape, dop)`, each with ms/op, cpu ms/op and
the cores ratio. `dop1` rows are the baseline every other row is quoted against.

Record, verbatim, from the output: every row, the load averages either side, and
`nproc`. The write-up quotes measured numbers only — if a row is missing, it is
named as missing rather than estimated.

- [x] **Step 5: Sanity-check the numbers before believing them**

Three things must hold, and if one does not, the finding is the deliverable:

- `ovr_n100000_k10_dop1` should land near 88 ms wall, matching #61's published
  `roc_auc_ovr_macro_n100000_k10` of 88.385 ms. A large gap means the seeded input
  is not comparable in difficulty to the corpus, which is worth saying out loud in
  the write-up.
- Processor time should rise with `dop` while elapsed time falls. Both falling
  would mean the parallel path is not running; both rising means dispatch is
  losing.
- `ovr_n1000_k10_dop8` against `ovr_n1000_k10_dop1` is the small-input answer.
  Whatever it says gets published — the decision was to honour the setting at any
  size and report the cost, not to hide it behind a threshold.

- [x] **Step 6: Document the new mode and commit**

Add to `bench/README.md`, in the section listing the harness modes:

````markdown
## Multiclass ROC-AUC, sequential against parallel (issue #86)

C# against C#: the same operation at one, two, four and eight workers. Inputs are
generated in-process from a fixed seed — there is no Python side here, so there is
no shared corpus to keep in step.

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- roc-parallel
```

The axis is **elapsed time**. Processor time rises with the worker count, which is
the point of spending cores rather than a fault in the measurement, and both are
reported side by side.
````

```bash
npx markdownlint-cli2 "bench/README.md"
git add bench/DataNet.Text.Benchmarks bench/README.md
git commit -m "$(cat <<'EOF'
Measure multiclass ROC-AUC against itself at four worker counts

Wall and processor time from the same run, through the harness #58 and
#61 already share, because BenchmarkDotNet reports elapsed time only and
issue #86 requires both.

Inputs are seeded and generated in-process rather than read from
bench/corpus/metrics, which holds k=2 and k=10 only and stops its score
matrix at 100 000 rows. This comparison is C# against C#: same data on
both sides is the only property that matters, and a seed guarantees it
without touching #61's published corpus.

One-vs-one at n=100 000, k=10 is 45 pairs and 90 curves; it is skipped
with the reason printed rather than dropped quietly.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 6: Write down the decision and the numbers

**Files:**

- Create: `docs/decisions/0017-multiclass-roc-auc-parallelism-is-opt-in.md`
- Modify: `docs/guides/performance.md` (new subsection after the #61 metrics one)
- Modify: `CHANGELOG.md` (under `### DataNet.Metrics — 0.1.0`)

**Interfaces:**

- Consumes: the measured table from Task 5, Step 4.
- Produces: no code.

- [x] **Step 1: Write the ADR**

Create `docs/decisions/0017-multiclass-roc-auc-parallelism-is-opt-in.md`, matching
the house shape of `0016` — `# 0017 — <title>`, then
`**Status:** accepted · **Date:** 2026-08-08`, then `## Context`, `## Decision`
with sub-headings, `## Consequences`. It must record, each with its reason:

1. **Parallelism is opt-in and the default is sequential.** A library that spawns
   threads a caller did not ask for is hostile inside a server already running one
   request per core, and scikit-learn does not parallelise `roc_auc_score` either.
2. **No `-1` sentinel for "all cores".** The caller writes
   `Environment.ProcessorCount`, so the number is visible at the call site.
3. **The setting is honoured at any input size**, with no internal threshold. The
   measured cost at n=1000 is published — quote the figure from Task 5 — rather
   than hidden behind a crossover calibrated on one workstation.
4. **The arguments moved into `MultiClassRocOptions`, a `ref struct`,** because
   `Labels` and `SampleWeight` are spans; and `Average` is nullable because
   `default(Averaging)` is `Binary`, which the method refuses.
5. **The parallel path copies the inputs**, `yTrue` plus a transposed score
   matrix, because a span cannot be handed to a thread and `unsafe` is refused
   repository-wide. Name the size: `samples × classes × 8` bytes.
6. **Only one-vs-rest and one-vs-one are parallelised**, because they alone
   reduce in array order afterwards. Cite the `classification_report` parity break
   from #61 — a change of summation order alone — as the reason a floating-point
   accumulation is not touched.
7. **What stays sequential and why:** `ValidateRowSums` (its message names the
   first offending row) and the final `Mean`/`WeightedMean`.
8. **Exceptions are rethrown from the lowest index** through
   `ExceptionDispatchInfo`, and the loop does not stop early, because `Stop` could
   cancel an earlier class and report a later one's failure.

- [x] **Step 2: Write the measured subsection of the performance guide**

Append to `docs/guides/performance.md`, after the #61 metrics section. Use the
real numbers from Task 5 — every cell measured, none estimated:

````markdown
## Multiclass ROC-AUC, sequential against parallel (issue #86)

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- roc-parallel
```

**The axis here is elapsed time.** Processor time rises with the worker count —
that is what spending cores means, and it is reported beside every figure rather
than dropped. The #61 table above is a processor-time comparison against
scikit-learn and stays one; this table is a different question asked of the same
code, and switching axes quietly between them would be the easiest way to mislead
a reader.

Intel i7-4770S, **4 physical cores / 8 logical threads**, .NET 10, one sitting,
one-minute load average <FILL FROM `uptime`> before and <FILL> after.

| Operation | dop | wall ms | cpu ms | cores | speed-up vs dop=1 |
| --- | ---: | ---: | ---: | ---: | ---: |
| … one row per measured `(strategy, shape, dop)` … |

Then, in prose: the speed-up at k=10 and at k=5 against the sequential baseline,
with the core count named; what the opt-in costs at n=1000; and the honest reading
of where the ceiling comes from — the copy, `ValidateRowSums`, and the fact that
five classes cannot use eight threads.
````

The `<FILL>` markers are not placeholders left for a reader: they are the two
numbers Task 5 Step 4 recorded, and this step is not done until they are in.

- [x] **Step 3: Write the CHANGELOG entry**

Under the existing `### DataNet.Metrics — 0.1.0` heading in `CHANGELOG.md`, in its
`#### Added` list — the package has never shipped, so this is an addition to an
unreleased surface, not a change to a released one:

```markdown
- **Opt-in parallelism for multiclass ROC-AUC.**
  `RocAuc.MultiClass(yTrue, yScore, classCount, new MultiClassRocOptions { … })`
  gathers the strategy, averaging, labels and sample weights that used to be
  trailing parameters, and adds `MaxDegreeOfParallelism`. One-vs-rest spreads its
  per-class loop and one-vs-one its per-pair loop over that many workers; the
  result is bit-identical, because each class and each pair writes its own slot
  and the averaging happens afterwards in array order. The default is 1 —
  sequential, unchanged, copying nothing — because a library that spawns threads
  a caller did not ask for is hostile inside a server already running one request
  per core, and scikit-learn does not parallelise `roc_auc_score` either. Above 1
  the inputs are copied, which is why the default does not pay for it. Measured
  before and after in [the performance guide](docs/guides/performance.md), and
  the reasoning is in
  [`docs/decisions/0017`](docs/decisions/0017-multiclass-roc-auc-parallelism-is-opt-in.md).
```

- [x] **Step 4: Run the full gate**

```bash
dotnet build DataNet.slnx -c Release
dotnet test DataNet.slnx -c Release
dotnet format DataNet.slnx --verify-no-changes
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 tools/check_version_floor.py
python3 tools/extract_doc_snippets.py && dotnet build samples/DataNet.DocSnippets -c Release
git status --porcelain    # extract_doc_snippets must not have left the generated tree dirty
```

Expected: all clean. `git status` should show nothing under
`samples/DataNet.DocSnippets/Generated` unless the new guide subsection added a
```` ```csharp ```` fence — if it did, that generated file is part of the commit.

- [x] **Step 5: Commit**

```bash
git add docs CHANGELOG.md samples
git commit -m "$(cat <<'EOF'
Record why ROC-AUC parallelism is opt-in, and what it bought

ADR 0017 answers the question issue #86 left open. Opt-in, sequential by
default: a library that spawns threads a caller did not ask for is
hostile inside a server already running one request per core, and
scikit-learn does not parallelise roc_auc_score either. No -1 sentinel —
the caller writes Environment.ProcessorCount, so the number is visible
where it is chosen. No size threshold either: the setting is honoured
whatever the input, and what it costs at n=1000 is published rather than
hidden behind a crossover calibrated on one workstation.

The performance guide carries the before and after. The axis is elapsed
time and it says so: processor time rises with the worker count, which is
what spending cores means, and both are reported so the trade is visible.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

- [x] **Step 6: Push and open the pull request**

```bash
git push -u origin perf/86-parallelise-multiclass-roc-auc
gh pr create --title "Parallelise multiclass ROC-AUC across classes and pairs" --body "$(cat <<'EOF'
Closes #86

## What this does

`RocAuc.MultiClass` takes a `MultiClassRocOptions` carrying a
`MaxDegreeOfParallelism`. One-vs-rest spreads its per-class loop and
one-vs-one its per-pair loop over that many workers. The default is 1 —
sequential, unchanged, copying nothing.

## Bit-identity

<paste: the parallel corpus test's result, and that comparison is on raw
IEEE-754 bits rather than a tolerance>

## Measured

<paste: the table from docs/guides/performance.md, the speed-up at k=10
and k=5 with the core count named, and the n=1000 figure>

Elapsed time is the axis. Processor time rises; that is what spending
cores means, and it is reported beside every figure.

## Decision

`docs/decisions/0017-multiclass-roc-auc-parallelism-is-opt-in.md`

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

The two `<paste: …>` markers are filled from the real output before the PR is
created. A perf PR without its numbers is exactly what `CONTRIBUTING.md`'s `perf/`
prefix rule exists to prevent.

---

## Self-review

**Spec coverage.** Every section of the spec maps to a task: the public API and
its three encodings to Task 1; the span-to-thread copy, the `(offset, stride)`
kernel and per-worker buffers to Tasks 2 and 3; deterministic exceptions to
Task 3; the one-vs-one pair table to Tasks 2 and 4; the four committed tests to
Tasks 1, 3 and 4; the measurement vehicle, the seeded k=5 input and the shape
matrix to Task 5; the ADR, the performance guide, the CHANGELOG and the
equivalence row to Task 6; `PackagingGate` and the version floor to Tasks 1 and 6.

Two things the spec asserted that this plan had to make concrete rather than
inherit. The spec said the parallel path copies "`n` ints + `n×k` doubles"; it also
needs the **sample weights** copied when they are present, which `CopyForWorkers`
does and the spec's prose omitted. And the spec's testing table implied the
bit-identity tests would fail before the parallel driver existed; they cannot,
because Task 1 leaves the setting inert, so Tasks 3 and 4 say so explicitly and
lean on the Task 2 fingerprint diff for the refactor's own guarantee instead of
pretending to a red bar that was never available.

**Placeholders.** The three `<FILL>` / `<paste: …>` markers are all in Task 6 and
Task 5's write-up, all annotated as "fill from the recorded output", and all
pointing at a specific earlier step that produced the number. There is no step
that says "add error handling" or "write tests for the above" without the code.

**Type consistency.** `BinaryRoc.Scratch.Rent(int)` / `.Return()` /
`.Binary` / `.Column` are used with those names in Tasks 2, 3 and 4.
`ClassScore(yTrue, scores, offset, stride, positiveLabel, sampleWeight, scratch, out positiveWeight)`
is defined in Task 2 and called in Tasks 2 and 3 with that arity.
`ScorePair(yTrue, scores, classes, stride, columnStride, pair, index, pairScores, prevalence, scratch)`
is defined in Task 2 and called in Tasks 2 and 4 with that arity. `Pairs(int)`,
`CopyForWorkers`, `ReturnToPool` and `RethrowFirst` are defined once and used
under those names. `MultiClassRocOptions`' five property names are spelled
identically in every task, the sample and the bench.
