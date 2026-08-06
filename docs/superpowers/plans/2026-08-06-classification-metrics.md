# DataNet.Metrics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a dependency-free `DataNet.Metrics` package whose classification
metrics match `sklearn.metrics` to 1e-9 and beat it on processor time.

**Architecture:** `ConfusionMatrix` is the public engine — one weighted pass over
two `ReadOnlySpan<int>`s, from which accuracy, precision, recall, F1, FBeta and
the classification report are derived. ROC-AUC sits apart because it consumes
continuous scores rather than predictions. Every public number is proven by
replaying a frozen scikit-learn corpus.

**Tech Stack:** C# on `net10.0` + `netstandard2.0`, xUnit, BenchmarkDotNet,
Python 3.12 with scikit-learn 1.9.0 for the oracle and the cross-language bench.

**Design spec:** [`../specs/2026-08-06-classification-metrics-design.md`](../specs/2026-08-06-classification-metrics-design.md)
**Issue:** [#61](https://github.com/CyrilB1531/data.net/issues/61)
**Branch:** `feat/61-classification-metrics` (already created, spec committed)

## Global Constraints

- **Warnings are errors** repository-wide (`TreatWarningsAsErrors` in the root
  `Directory.Build.props`), and `GenerateDocumentationFile` is on — every public
  member needs an XML doc comment or the build fails.
- **XML docs name the Python function** they reproduce (`CONTRIBUTING.md`,
  definition of done, point 4).
- **Everything in English**: code, comments, docs, commit messages, PR body.
- **Targets**: `net10.0;netstandard2.0`. No `Math.Clamp`, no `double.IsFinite`,
  no `MathF`, no `ArgumentNullException.ThrowIfNull` — netstandard2.0 has none of
  them. Use the shared `Guard` helpers (globally imported via
  `src/Shared/GlobalUsings.cs`).
- **No dependency**: `DataNet.Metrics` references no package on `net10.0`, and
  only `System.Memory` + `System.Numerics.Vectors` on `netstandard2.0`. Do **not**
  set `DataNetIncludesPersistence`.
- **Oracle tolerance**: `1e-9` for floating-point, exact comparison for strings.
- **Oracle determinism**: fixed seed, no wall-clock, no unordered iteration. The
  `Oracles are reproducible` CI job regenerates and fails on any drift.
- **Run the generator from a neutral directory** and check *its* exit code, never
  a pipeline's:

  ```bash
  cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
  ```

- **`dotnet format` is broken on this machine** — it crashes before reading any
  code. Do not run it, do not try to fix it; the `Lint` CI job is the only
  authority on formatting.
- **A green suite proves nothing until you have seen it red.** Every task below
  runs the new test and requires a *specific* failure message before any
  implementation is written. Check the executed-test count, not just the colour.
- **Merge gate**: processor time ≥ 1× versus scikit-learn on every measured
  operation at every measured size (Task 10). CI cannot hold this gate; the
  numbers live in the PR body and `docs/guides/performance.md`.
- **Commit per task**, message in the repository's style: a sentence that says
  what changed and why, not a conventional-commit prefix. Every commit ends with:

  ```text
  Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
  ```

## File Structure

```text
src/DataNet.Metrics/
  DataNet.Metrics.csproj          package identity, targets
  Version.props                   0.1.0, owned here
  Averaging.cs                    enum: Binary, Micro, Macro, Weighted
  ZeroDivision.cs                 enum: Zero, One, NaN, Throw
  MultiClassStrategy.cs           enum: OneVsRest, OneVsOne
  UndefinedMetricException.cs     thrown by ZeroDivision.Throw
  ConfusionMatrix.cs              the engine: one weighted pass
  Accuracy.cs                     diagonal / total
  Precision.cs  Recall.cs  F1.cs  FBeta.cs      thin facades over Prf
  ClassRow.cs                     one row of the report
  ClassificationReport.cs         structured report
  RocAuc.cs                       binary + multiclass entry points
  Internal/LabelIndex.cs          label set resolution + label -> ordinal
  Internal/Prf.cs                 _prf_divide, averaging, fbeta
  Internal/ReportText.cs          the sklearn text layout
  Internal/BinaryRoc.cs           _binary_clf_curve + auc
  Internal/MultiClassRoc.cs       ovr / ovo

tests/DataNet.Metrics.Tests/                 xUnit suite + OracleLoader
tests/DataNet.Metrics.NetStandard.Tests/     same sources, netstandard2.0 build

tests/oracles/classification_metrics.json    frozen corpus
tests/oracles/roc_auc.json                   frozen corpus

bench/DataNet.Text.Benchmarks/MetricsBenchmarks.cs        BenchmarkDotNet
bench/DataNet.Text.Benchmarks/CrossLang/MetricsCrossLang.cs
bench/python/bench_metrics.py
bench/corpus/generate_metrics.py

samples/DataNet.Sample/Lot5Metrics.cs        the metrics lot + reachability gate
```

---

### Task 1: The package, its two test projects, and the enums

**Files:**

- Create: `src/DataNet.Metrics/DataNet.Metrics.csproj`
- Create: `src/DataNet.Metrics/Version.props`
- Create: `src/DataNet.Metrics/Averaging.cs`, `ZeroDivision.cs`,
  `MultiClassStrategy.cs`, `UndefinedMetricException.cs`
- Create: `tests/DataNet.Metrics.Tests/DataNet.Metrics.Tests.csproj`
- Create: `tests/DataNet.Metrics.Tests/OracleLoader.cs`
- Create: `tests/DataNet.Metrics.NetStandard.Tests/DataNet.Metrics.NetStandard.Tests.csproj`
- Create: `tests/DataNet.Metrics.NetStandard.Tests/NetStandardAssemblyGuardTests.cs`
- Modify: `DataNet.slnx`

**Interfaces:**

- Consumes: nothing.
- Produces: namespace `DataNet.Metrics`; `enum Averaging { Binary, Micro, Macro,
  Weighted }`; `enum ZeroDivision { Zero, One, NaN, Throw }`; `enum
  MultiClassStrategy { OneVsRest, OneVsOne }`; `sealed class
  UndefinedMetricException : InvalidOperationException`; test helper
  `internal static class OracleLoader { static JsonDocument Load(string) }`.

- [ ] **Step 1: Create the library project**

`src/DataNet.Metrics/DataNet.Metrics.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- This package's version, owned here rather than repository-wide. -->
  <Import Project="Version.props" />

  <PropertyGroup>
    <Version>$(DataNetMetricsVersion)</Version>
    <TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>
    <RootNamespace>DataNet.Metrics</RootNamespace>

    <PackageId>DataNet.Metrics</PackageId>
    <Description>Classification metrics for .NET at exact scikit-learn parity: precision, recall, F1, FBeta, confusion matrix, classification report and ROC-AUC, with sklearn's averaging modes and zero-division semantics. No dependencies.</Description>
    <PackageTags>metrics;classification;f1;precision;recall;roc-auc;confusion-matrix;scikit-learn;datanet</PackageTags>
  </PropertyGroup>

</Project>
```

`src/DataNet.Metrics/Version.props`:

```xml
<Project>

  <!--
    DataNet.Metrics owns its version here, independently of the other packages
    (see docs/decisions/0012-per-package-versioning.md).

    0.1.0: this package has never shipped. It creates no inter-package edge —
    nothing depends on it and it depends on nothing — so unlike DataNet.Text it
    is free to release on its own schedule from the start.
  -->
  <PropertyGroup>
    <DataNetMetricsVersion>0.1.0</DataNetMetricsVersion>
  </PropertyGroup>

</Project>
```

- [ ] **Step 2: Write the enums and the exception**

`src/DataNet.Metrics/Averaging.cs`:

```csharp
namespace DataNet.Metrics;

/// <summary>
/// How a per-class score is reduced to a single number — the equivalent of
/// scikit-learn's <c>average=</c> parameter on <c>precision_score</c>,
/// <c>recall_score</c>, <c>f1_score</c> and <c>fbeta_score</c>.
/// </summary>
/// <remarks>
/// scikit-learn's <c>average=None</c> has no member here: it changes the return
/// type rather than the value. Call <c>PerClass</c> instead.
/// </remarks>
public enum Averaging
{
    /// <summary>Report the positive class only (<c>average="binary"</c>). Valid for two-class problems.</summary>
    Binary,

    /// <summary>Sum the true positives, false positives and false negatives over all classes, then divide once (<c>average="micro"</c>).</summary>
    Micro,

    /// <summary>Unweighted mean of the per-class scores (<c>average="macro"</c>).</summary>
    Macro,

    /// <summary>Mean of the per-class scores weighted by support (<c>average="weighted"</c>).</summary>
    Weighted,
}
```

`src/DataNet.Metrics/ZeroDivision.cs`:

```csharp
namespace DataNet.Metrics;

/// <summary>
/// What a metric returns when its denominator is zero — the equivalent of
/// scikit-learn's <c>zero_division=</c> parameter.
/// </summary>
/// <remarks>
/// scikit-learn's default returns <c>0.0</c> <em>and</em> emits an
/// <c>UndefinedMetricWarning</c>. <see cref="Zero"/> reproduces the value, which
/// is what parity requires; <see cref="Throw"/> is the opt-in equivalent of the
/// warning, for callers who would rather be told than get a silent zero.
/// </remarks>
public enum ZeroDivision
{
    /// <summary>Return <c>0.0</c> — scikit-learn's default value.</summary>
    Zero,

    /// <summary>Return <c>1.0</c> (<c>zero_division=1</c>).</summary>
    One,

    /// <summary>Return <see cref="double.NaN"/> (<c>zero_division=np.nan</c>).</summary>
    NaN,

    /// <summary>Throw <see cref="UndefinedMetricException"/>. No scikit-learn equivalent.</summary>
    Throw,
}
```

`src/DataNet.Metrics/MultiClassStrategy.cs`:

```csharp
namespace DataNet.Metrics;

/// <summary>
/// How multiclass ROC-AUC reduces to binary problems — the equivalent of
/// scikit-learn's <c>multi_class=</c> parameter on <c>roc_auc_score</c>.
/// </summary>
public enum MultiClassStrategy
{
    /// <summary>One class against all the others (<c>multi_class="ovr"</c>).</summary>
    OneVsRest,

    /// <summary>Every pair of classes, averaged (<c>multi_class="ovo"</c>, Hand &amp; Till).</summary>
    OneVsOne,
}
```

`src/DataNet.Metrics/UndefinedMetricException.cs`:

```csharp
namespace DataNet.Metrics;

/// <summary>
/// Thrown when a metric is undefined and <see cref="ZeroDivision.Throw"/> was
/// requested — the counterpart of scikit-learn's <c>UndefinedMetricWarning</c>.
/// </summary>
public sealed class UndefinedMetricException : InvalidOperationException
{
    /// <summary>Creates the exception with a default message.</summary>
    public UndefinedMetricException()
        : base("The metric is undefined: its denominator is zero.")
    {
    }

    /// <summary>Creates the exception with the given message.</summary>
    /// <param name="message">A message describing which metric is undefined.</param>
    public UndefinedMetricException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with the given message and inner exception.</summary>
    /// <param name="message">A message describing which metric is undefined.</param>
    /// <param name="innerException">The cause.</param>
    public UndefinedMetricException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

- [ ] **Step 3: Create the net10 test project**

`tests/DataNet.Metrics.Tests/DataNet.Metrics.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/DataNet.Metrics/DataNet.Metrics.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="../oracles/**/*.json" CopyToOutputDirectory="PreserveNewest" LinkBase="oracles" />
  </ItemGroup>

</Project>
```

`tests/DataNet.Metrics.Tests/OracleLoader.cs` — same helper the other suites use:

```csharp
using System.Text.Json;

namespace DataNet.Metrics.Tests;

/// <summary>Minimal loader for the committed oracle JSON files.</summary>
internal static class OracleLoader
{
    public static JsonDocument Load(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "oracles", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Oracle '{fileName}' not found at '{path}'. Run tools/generate_oracles.py.", path);
        }
        return JsonDocument.Parse(File.ReadAllText(path));
    }
}
```

- [ ] **Step 4: Create the netstandard2.0 mirror, deliberately mis-wired**

`tests/DataNet.Metrics.NetStandard.Tests/DataNet.Metrics.NetStandard.Tests.csproj`
— copy the header comment and shape from
`tests/DataNet.Embeddings.NetStandard.Tests/`, but **omit `SetTargetFramework`
for now** so the next step can prove the guard works:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!--
    Replays the entire DataNet.Metrics.Tests suite against the *netstandard2.0*
    build of the library, instead of the net10.0 one the original project
    references.

    netstandard2.0 is a contract, not a runtime, so the tests cannot run *on* it.
    They run on net10.0 — identical host — and only the assembly under test
    changes. Without this, the assemblies shipped to .NET Framework, Mono and
    Unity consumers are compile-verified but never executed.

    The test sources are linked, never copied: one suite, two builds.
  -->

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <AssemblyName>DataNet.Metrics.NetStandard.Tests</AssemblyName>
    <RootNamespace>DataNet.Metrics.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/DataNet.Metrics/DataNet.Metrics.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Compile Include="../DataNet.Metrics.Tests/**/*.cs"
             Exclude="../DataNet.Metrics.Tests/bin/**;../DataNet.Metrics.Tests/obj/**"
             Link="%(RecursiveDir)%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <None Include="../oracles/**/*.json" CopyToOutputDirectory="PreserveNewest" LinkBase="oracles" />
  </ItemGroup>

</Project>
```

`tests/DataNet.Metrics.NetStandard.Tests/NetStandardAssemblyGuardTests.cs`:

```csharp
using System.Reflection;
using System.Runtime.Versioning;
using Xunit;

namespace DataNet.Metrics.Tests;

/// <summary>
/// Guards the premise of this project: that the suite is replaying against the
/// netstandard2.0 assembly and not the net10.0 one.
/// </summary>
/// <remarks>
/// Without this, a reference that quietly resolved back to net10.0 would leave
/// every test passing while proving nothing.
/// </remarks>
public sealed class NetStandardAssemblyGuardTests
{
    [Fact]
    public void Suite_runs_against_the_netstandard2_0_build()
    {
        Assembly assembly = typeof(DataNet.Metrics.Averaging).Assembly;
        string? framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        Assert.Equal(".NETStandard,Version=v2.0", framework);
    }
}
```

- [ ] **Step 5: Add the three projects to the solution**

Add to `DataNet.slnx`, in the existing `/src/` and `/tests/` folders:

```xml
    <Project Path="src/DataNet.Metrics/DataNet.Metrics.csproj" />
```

```xml
    <Project Path="tests/DataNet.Metrics.NetStandard.Tests/DataNet.Metrics.NetStandard.Tests.csproj" />
    <Project Path="tests/DataNet.Metrics.Tests/DataNet.Metrics.Tests.csproj" />
```

- [ ] **Step 6: Run the guard test and watch it fail**

```bash
dotnet test tests/DataNet.Metrics.NetStandard.Tests -c Release
```

Expected: FAIL — `Assert.Equal() Failure … Expected: ".NETStandard,Version=v2.0"
Actual: ".NETCoreApp,Version=v10.0"`. That is the mis-wiring the guard exists to
catch, reproduced on purpose.

- [ ] **Step 7: Pin the reference to the netstandard2.0 build**

In `tests/DataNet.Metrics.NetStandard.Tests/DataNet.Metrics.NetStandard.Tests.csproj`,
replace the `ProjectReference` item group with:

```xml
  <!-- SetTargetFramework is what pins the reference to the netstandard2.0 build. -->
  <ItemGroup>
    <ProjectReference Include="../../src/DataNet.Metrics/DataNet.Metrics.csproj"
                      SetTargetFramework="TargetFramework=netstandard2.0" />
  </ItemGroup>
```

- [ ] **Step 8: Run the whole solution green**

```bash
dotnet build DataNet.slnx -c Release
dotnet test DataNet.slnx -c Release
```

Expected: build clean (warnings are errors), guard test PASSES, and the existing
suites still pass. Confirm the executed-test count went **up** by exactly one.

- [ ] **Step 9: Commit**

```bash
git add src/DataNet.Metrics tests/DataNet.Metrics.Tests tests/DataNet.Metrics.NetStandard.Tests DataNet.slnx
git commit -F - <<'EOF'
Open a fourth package for metrics that owe nothing to text

DataNet.Metrics starts empty but wired: two targets, its own version, and the
netstandard2.0 mirror suite the other three have. The mirror was run once
without SetTargetFramework first, so the guard was seen failing against
.NETCoreApp before it was made to pass — a guard nobody has watched fail is
indistinguishable from one that cannot.

Nothing here depends on DataNet.Text, so the package creates no inter-package
edge and stays outside the publish-then-raise-the-floor cycle.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 2: Packaging plumbing

**Files:**

- Modify: `.github/workflows/ci.yml` (three `for proj in …` loops)
- Modify: `.github/workflows/release.yml` (the package allowlist)
- Modify: `.github/workflows/release-nuget-org.yml` (the package allowlist)
- Modify: `tools/check_nuspec_dependencies.py:33-86`

**Interfaces:**

- Consumes: `src/DataNet.Metrics/DataNet.Metrics.csproj` from Task 1.
- Produces: `dotnet pack src/DataNet.Metrics` produces
  `artifacts/DataNet.Metrics.0.1.0.nupkg`, accepted by
  `check_nuspec_dependencies.py --require-all`.

- [ ] **Step 1: Add the expected dependency set to the nuspec checker**

In `tools/check_nuspec_dependencies.py`, add the constant next to the others
(line 35) and the `EXPECTED` entry (line 86):

```python
METRICS = "DataNet.Metrics"
```

```python
    METRICS: {
        # Nothing on net10.0 and nothing but the polyfills on netstandard2.0:
        # metrics are pure computation over spans, with no I/O to serialise and
        # therefore no System.Text.Json.
        NET: {},
        NETSTANDARD: {**POLYFILLS},
    },
```

- [ ] **Step 2: Run the checker and watch it fail**

```bash
rm -rf ./artifacts
dotnet pack src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy -c Release -o ./artifacts
python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
echo "exit=$?"
```

Expected: non-zero exit naming `DataNet.Metrics` as missing from `./artifacts`.
That proves `--require-all` now knows about the package.

- [ ] **Step 3: Pack the new package and watch the checker pass**

```bash
dotnet pack src/DataNet.Metrics -c Release -o ./artifacts
python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
echo "exit=$?"
```

Expected: `exit=0`.

- [ ] **Step 4: Add the package to the three CI loops**

In `.github/workflows/ci.yml`, every occurrence of

```bash
for proj in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy; do
```

becomes

```bash
for proj in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do
```

There are three (the pack step, the project-reference guard, and the pack step
that feeds the sample). Grep to confirm you changed all of them:

```bash
grep -n "for proj in" .github/workflows/ci.yml
```

Expected: three lines, each ending in `src/DataNet.Metrics; do`.

- [ ] **Step 5: Add the package to both release allowlists**

In `.github/workflows/release.yml` and `.github/workflows/release-nuget-org.yml`:

```bash
            DataNet.Text|DataNet.Embeddings|DataNet.Fuzzy|DataNet.Metrics) ;;
```

Confirm:

```bash
grep -n "DataNet.Metrics)" .github/workflows/release.yml .github/workflows/release-nuget-org.yml
```

Expected: one hit in each file.

- [ ] **Step 6: Commit**

```bash
git add .github/workflows tools/check_nuspec_dependencies.py
git commit -F - <<'EOF'
Teach the release machinery that a fourth package exists

Packing, the project-reference guard, the two tag allowlists and the nuspec
dependency check each enumerate the packages by hand. A package missing from
any one of them fails late and confusingly: an unknown tag, or a sample that
restores a package nobody packed.

The nuspec check was run against an artifacts folder without DataNet.Metrics
first, to see it refuse, before the package was packed into it.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 3: The oracle generator and the two frozen corpora

**Files:**

- Modify: `tools/generate_oracles.py` (imports at line 25-38, new generator
  sections, `main()` registry at line 1322)
- Create: `tests/oracles/classification_metrics.json` (generated)
- Create: `tests/oracles/roc_auc.json` (generated)

**Interfaces:**

- Consumes: nothing from earlier tasks.
- Produces: two corpora consumed by Tasks 4-9. Case shape:
  `{fixture, weighted, y_true[], y_pred[], sample_weight[]|null, labels[]|null,
  target_names[]|null, pos_label, expected_labels[], confusion_matrix[][],
  accuracy, accuracy_count, averaged{"<avg>|<zd>": {precision, recall, f1}},
  per_class{"<zd>": {precision[], recall[], f1[], support[]}},
  fbeta{"<beta>|<avg>|<zd>": value}, reports{"<digits>": text}}` and
  `{fixture, kind, y_true[], scores[], class_count, sample_weight[]|null,
  binary{...}|multiclass{"<strategy>|<average>": value}}`.

- [ ] **Step 1: Recreate the oracle virtualenv**

It is git-ignored and absent from a fresh clone:

```bash
python3 -m venv .venv-oracles
.venv-oracles/bin/pip install --require-hashes -r tools/requirements.lock.txt
.venv-oracles/bin/python -c "import sklearn, numpy; print(sklearn.__version__, numpy.__version__)"
```

Expected: `1.9.0 2.5.1`. Any other version and the corpora will not match what CI
regenerates.

- [ ] **Step 2: Add the two imports the new sections need**

In `tools/generate_oracles.py`, alongside the existing imports:

```python
import math
import warnings

import numpy as np
from sklearn import metrics as skm
```

- [ ] **Step 3: Add the classification-metrics generator**

Append these sections before `main()`:

```python
# --- Classification metrics (issue #61) --------------------------------------
#
# Fixtures target the cases where implementations actually diverge rather than
# average behaviour: a class that is never predicted, a class absent from the
# truth, a labels= subset (which drops samples and turns the report's accuracy
# row into a micro-avg row), and non-contiguous label values that catch any
# implementation assuming 0..k-1. Each fixture is emitted twice, unweighted and
# weighted, because sample_weight changes the dtype of every count upstream.

METRIC_SEED = SEED + 61
ZERO_DIVISIONS = (0, 1)
BETAS = (0.5, 2.0)
REPORT_DIGITS = (2, 3)


def _metric_fixtures() -> list[dict]:
    rng = random.Random(METRIC_SEED)
    fixtures: list[dict] = []

    def noisy(truth: list[int], classes: list[int], flip: float) -> list[int]:
        return [
            t if rng.random() >= flip else rng.choice([c for c in classes if c != t])
            for t in truth
        ]

    def add(name, y_true, y_pred, labels=None, target_names=None, pos_label=1):
        fixtures.append({
            "name": name,
            "y_true": [int(v) for v in y_true],
            "y_pred": [int(v) for v in y_pred],
            "labels": labels,
            "target_names": target_names,
            "pos_label": pos_label,
            "sample_weight": [round(rng.uniform(0.1, 3.0), 3) for _ in y_true],
        })

    balanced = [rng.randint(0, 1) for _ in range(200)]
    add("binary_balanced", balanced, noisy(balanced, [0, 1], 0.2),
        target_names=["negative", "positive"])

    imbalanced = [0] * 190 + [1] * 10
    add("binary_imbalanced", imbalanced, noisy(imbalanced, [0, 1], 0.3))

    three = [rng.randint(0, 2) for _ in range(300)]
    add("multiclass_3", three, noisy(three, [0, 1, 2], 0.35),
        target_names=["alpha", "beta", "gamma"])

    ten = [rng.randint(0, 9) for _ in range(500)]
    add("multiclass_10", ten, noisy(ten, list(range(10)), 0.5))

    # Class 2 is in y_true and never predicted: its precision divides by zero.
    add("class_never_predicted", [0, 0, 1, 1, 2, 2, 1, 0], [0, 1, 1, 1, 0, 1, 1, 0])

    # Class 3 is predicted and absent from y_true: its recall divides by zero.
    add("class_absent_from_truth", [0, 0, 1, 1, 0, 1], [0, 3, 1, 3, 0, 1])

    perfect = [rng.randint(0, 2) for _ in range(50)]
    add("perfect", perfect, list(perfect))
    add("all_wrong", perfect, [(v + 1) % 3 for v in perfect])

    add("single_sample", [1], [1])
    add("single_class", [1, 1, 1, 1], [1, 1, 1, 1])

    subset = [rng.randint(0, 3) for _ in range(120)]
    add("labels_subset", subset, noisy(subset, [0, 1, 2, 3], 0.4), labels=[0, 2])

    sparse = [rng.choice([-1, 5, 42]) for _ in range(120)]
    add("non_contiguous_labels", sparse, noisy(sparse, [-1, 5, 42], 0.4), pos_label=5)

    return fixtures


def _binary_average_applies(observed: list[int], pos_label: int) -> bool:
    """Mirror scikit-learn's own admissibility rule for average="binary"."""
    if len(observed) > 2:
        return False
    return pos_label in observed or len(observed) < 2


def _metric_case(fx: dict, weighted: bool) -> dict:
    y_true, y_pred = fx["y_true"], fx["y_pred"]
    labels, pos_label = fx["labels"], fx["pos_label"]
    sw = fx["sample_weight"] if weighted else None
    observed = sorted(set(y_true) | set(y_pred))
    effective = labels if labels is not None else observed
    averages = ["micro", "macro", "weighted"]
    if _binary_average_applies(observed, pos_label):
        averages.append("binary")

    cm = skm.confusion_matrix(y_true, y_pred, labels=labels, sample_weight=sw)
    case = {
        "fixture": fx["name"],
        "weighted": weighted,
        "y_true": y_true,
        "y_pred": y_pred,
        "sample_weight": sw,
        "labels": labels,
        "target_names": fx["target_names"],
        "pos_label": pos_label,
        "expected_labels": [int(v) for v in effective],
        "confusion_matrix": [[float(v) for v in row] for row in cm.tolist()],
        "accuracy": float(skm.accuracy_score(y_true, y_pred, sample_weight=sw)),
        "accuracy_count": float(
            skm.accuracy_score(y_true, y_pred, normalize=False, sample_weight=sw)),
        "averaged": {},
        "per_class": {},
        "fbeta": {},
        "reports": {},
    }

    for zd in ZERO_DIVISIONS:
        for avg in averages:
            p, r, f, _ = skm.precision_recall_fscore_support(
                y_true, y_pred, labels=labels, average=avg, pos_label=pos_label,
                sample_weight=sw, zero_division=zd)
            case["averaged"][f"{avg}|{zd}"] = {
                "precision": float(p), "recall": float(r), "f1": float(f)}
            for beta in BETAS:
                case["fbeta"][f"{beta}|{avg}|{zd}"] = float(skm.fbeta_score(
                    y_true, y_pred, beta=beta, labels=labels, average=avg,
                    pos_label=pos_label, sample_weight=sw, zero_division=zd))
        p, r, f, s = skm.precision_recall_fscore_support(
            y_true, y_pred, labels=labels, average=None, sample_weight=sw,
            zero_division=zd)
        case["per_class"][str(zd)] = {
            "precision": [float(v) for v in p],
            "recall": [float(v) for v in r],
            "f1": [float(v) for v in f],
            "support": [float(v) for v in s],
        }

    for digits in REPORT_DIGITS:
        case["reports"][str(digits)] = skm.classification_report(
            y_true, y_pred, labels=labels, target_names=fx["target_names"],
            digits=digits, sample_weight=sw, zero_division=0)
    return case


def generate_classification_metrics() -> dict:
    with warnings.catch_warnings():
        # scikit-learn warns on every undefined metric; the corpus records the
        # value it returns, which is the thing under test.
        warnings.simplefilter("ignore")
        cases = [
            _metric_case(fx, weighted)
            for fx in _metric_fixtures()
            for weighted in (False, True)
        ]
    return {
        "metadata": {
            "algorithm": "ClassificationMetrics",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.accuracy_score",
                "sklearn.metrics.confusion_matrix",
                "sklearn.metrics.precision_recall_fscore_support",
                "sklearn.metrics.fbeta_score",
                "sklearn.metrics.classification_report",
            ],
            "count": len(cases),
        },
        "cases": cases,
    }
```

- [ ] **Step 4: Add the ROC-AUC generator**

```python
# --- ROC-AUC (issue #61) ------------------------------------------------------


def _softmax(row: list[float]) -> list[float]:
    top = max(row)
    exps = [math.exp(v - top) for v in row]
    total = sum(exps)
    return [v / total for v in exps]


def _roc_fixtures() -> list[dict]:
    rng = random.Random(METRIC_SEED + 1)
    fixtures: list[dict] = []

    def weights(n: int) -> list[float]:
        return [round(rng.uniform(0.1, 3.0), 3) for _ in range(n)]

    def informative(truth: list[int]) -> list[float]:
        # Overlapping but separable: an AUC around 0.8 rather than 0.5 or 1.0.
        return [round(rng.random() * 0.6 + 0.4 * t, 12) for t in truth]

    balanced = [rng.randint(0, 1) for _ in range(300)]
    fixtures.append({"name": "binary_balanced", "kind": "binary", "y_true": balanced,
                     "scores": informative(balanced), "class_count": 2,
                     "sample_weight": weights(len(balanced))})

    imbalanced = [0] * 280 + [1] * 20
    fixtures.append({"name": "binary_imbalanced", "kind": "binary", "y_true": imbalanced,
                     "scores": informative(imbalanced), "class_count": 2,
                     "sample_weight": weights(len(imbalanced))})

    tied = [rng.randint(0, 1) for _ in range(200)]
    fixtures.append({"name": "binary_heavy_ties", "kind": "binary", "y_true": tied,
                     # One decimal: many samples share a score, which is where a
                     # rank-based shortcut and a real ROC curve part company.
                     "scores": [round(v, 1) for v in informative(tied)], "class_count": 2,
                     "sample_weight": weights(len(tied))})

    for k, size in ((3, 240), (5, 400)):
        truth = [rng.randint(0, k - 1) for _ in range(size)]
        rows = []
        for t in truth:
            logits = [rng.gauss(0.0, 1.0) for _ in range(k)]
            logits[t] += 1.5
            rows.append([round(v, 12) for v in _softmax(logits)])
        fixtures.append({"name": f"multiclass_{k}", "kind": "multiclass", "y_true": truth,
                         "scores": rows, "class_count": k,
                         "sample_weight": weights(size)})

    return fixtures


def _roc_case(fx: dict, weighted: bool) -> dict:
    sw = fx["sample_weight"] if weighted else None
    y_true = fx["y_true"]
    case = {
        "fixture": fx["name"],
        "kind": fx["kind"],
        "weighted": weighted,
        "y_true": y_true,
        "scores": fx["scores"],
        "class_count": fx["class_count"],
        "sample_weight": sw,
        "values": {},
    }
    if fx["kind"] == "binary":
        case["values"]["binary"] = float(
            skm.roc_auc_score(y_true, fx["scores"], sample_weight=sw))
        return case

    scores = np.array(fx["scores"], dtype=float)
    classes = list(range(fx["class_count"]))
    for strategy in ("ovr", "ovo"):
        # scikit-learn refuses sample_weight for one-vs-one, and so do we.
        if strategy == "ovo" and weighted:
            continue
        for average in ("macro", "weighted"):
            case["values"][f"{strategy}|{average}"] = float(skm.roc_auc_score(
                y_true, scores, multi_class=strategy, average=average,
                labels=classes, sample_weight=sw))
    return case


def generate_roc_auc() -> dict:
    cases = [
        _roc_case(fx, weighted)
        for fx in _roc_fixtures()
        for weighted in (False, True)
    ]
    return {
        "metadata": {
            "algorithm": "RocAuc",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": ["sklearn.metrics.roc_auc_score"],
            "count": len(cases),
        },
        "cases": cases,
    }
```

- [ ] **Step 5: Register both in `main()`**

Add to the `generators` dict, after `"process.json": generate_process,`:

```python
        "classification_metrics.json": generate_classification_metrics,
        "roc_auc.json": generate_roc_auc,
```

- [ ] **Step 6: Generate, checking the generator's own exit code**

```bash
cd /tmp && PYTHONSAFEPATH=1 "$OLDPWD/.venv-oracles/bin/python" "$OLDPWD/tools/generate_oracles.py"
echo "exit=$?"
```

Expected: `exit=0`, and two new lines in the output:
`classification_metrics.json: 24 cases -> …` (twelve fixtures, each weighted and
unweighted) and `roc_auc.json: 10 cases -> …` (five fixtures, likewise).
Do **not** pipe this through `tail` or `grep`: the shell would report the
filter's status and a failed generation would look successful.

- [ ] **Step 7: Prove the corpora are byte-reproducible**

```bash
cd /home/cyril/Documents/devs/data.net-58 && git status --short tests/oracles/
```

Expected: only the two new files. If any *pre-existing* corpus shows as modified,
stop — a dependency drifted and that must be resolved deliberately, in its own
commit, not folded in here.

Then regenerate a second time and confirm nothing moves:

```bash
cd /tmp && PYTHONSAFEPATH=1 "$OLDPWD/.venv-oracles/bin/python" "$OLDPWD/tools/generate_oracles.py" >/dev/null
cd /home/cyril/Documents/devs/data.net-58 && git diff --stat tests/oracles/
```

Expected: empty output — the `Oracles are reproducible` job will do exactly this.

- [ ] **Step 8: Eyeball one frozen report, because Task 7 must reproduce it exactly**

```bash
python3 -c "
import json
d = json.load(open('tests/oracles/classification_metrics.json'))
c = next(x for x in d['cases'] if x['fixture'] == 'binary_balanced' and not x['weighted'])
print(c['reports']['2'])
"
```

Expected: the familiar sklearn table with `negative` / `positive` rows, an
`accuracy` row, then `macro avg` and `weighted avg`. Keep it in view for Task 7.

- [ ] **Step 9: Commit**

```bash
git add tools/generate_oracles.py tests/oracles/classification_metrics.json tests/oracles/roc_auc.json
git commit -F - <<'EOF'
Freeze what scikit-learn actually returns, before writing any C#

Twelve fixtures, each emitted unweighted and weighted, chosen for the places a
reimplementation drifts rather than for coverage: a class never predicted, a
class absent from the truth, a labels= subset that drops samples, and label
values that are not 0..k-1. Every averaging mode is recorded, not just the
default, along with both zero_division values and the report text at two digit
settings.

The corpus is written first so the implementation has something to be wrong
against. Generation was run twice and diffed to confirm byte reproducibility,
which is what the CI job asserts.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 4: Label resolution, `ConfusionMatrix` and `Accuracy`

**Files:**

- Create: `src/DataNet.Metrics/Internal/LabelIndex.cs`
- Create: `src/DataNet.Metrics/ConfusionMatrix.cs`
- Create: `src/DataNet.Metrics/Accuracy.cs`
- Create: `tests/DataNet.Metrics.Tests/MetricsCorpus.cs`
- Create: `tests/DataNet.Metrics.Tests/ConfusionMatrixTests.cs`
- Create: `tests/DataNet.Metrics.Tests/AccuracyTests.cs`

**Interfaces:**

- Consumes: the `classification_metrics.json` corpus from Task 3.
- Produces:
  - `ConfusionMatrix.Compute(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred,
    ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
    -> ConfusionMatrix`
  - `ConfusionMatrix.Labels -> IReadOnlyList<int>`, `this[int, int] -> double`,
    `TotalWeight -> double`, `ToArray() -> double[,]`
  - internal to the assembly: `ConfusionMatrix.Size -> int`,
    `ConfusionMatrix.Cells -> ReadOnlySpan<double>` (flat, row-major),
    `IsWeighted -> bool`, `DroppedSamples -> bool`, `ExplicitLabels -> bool`
  - `Accuracy.Score(ConfusionMatrix, bool normalize = true) -> double` and
    `Accuracy.Score(ReadOnlySpan<int>, ReadOnlySpan<int>, bool normalize = true,
    ReadOnlySpan<double> sampleWeight = default) -> double`
  - internal `LabelIndex.Create(...) -> LabelIndex` with `Count`, `Labels`,
    `Explicit`, `IndexOf(int) -> int` (`-1` when absent)

- [ ] **Step 1: Write the corpus helper the next six tasks all use**

`tests/DataNet.Metrics.Tests/MetricsCorpus.cs`:

```csharp
using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

/// <summary>Shared access to the frozen classification-metrics corpus.</summary>
internal static class MetricsCorpus
{
    /// <summary>The tolerance the whole repository uses for oracle replay.</summary>
    public const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("classification_metrics.json");

    public static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("cases").EnumerateArray()];

    /// <summary>One theory row per case, so a failure names the case that failed.</summary>
    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }
        return data;
    }

    public static string Describe(JsonElement c) =>
        $"{c.GetProperty("fixture").GetString()} (weighted={c.GetProperty("weighted").GetBoolean()})";

    public static int[] Ints(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetInt32())];

    public static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    /// <summary>Reads an array property that the corpus writes as null when absent.</summary>
    public static int[] OptionalInts(JsonElement c, string name) =>
        c.GetProperty(name).ValueKind == JsonValueKind.Null ? [] : Ints(c, name);

    public static double[] OptionalDoubles(JsonElement c, string name) =>
        c.GetProperty(name).ValueKind == JsonValueKind.Null ? [] : Doubles(c, name);
}
```

- [ ] **Step 2: Write the failing confusion-matrix replay test**

`tests/DataNet.Metrics.Tests/ConfusionMatrixTests.cs`:

```csharp
using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class ConfusionMatrixTests
{
    [Theory]
    [MemberData(nameof(MetricsCorpus.Indices), MemberType = typeof(MetricsCorpus))]
    public void Matches_sklearn_confusion_matrix(int index)
    {
        JsonElement c = MetricsCorpus.Cases[index];
        string what = MetricsCorpus.Describe(c);

        ConfusionMatrix cm = ConfusionMatrix.Compute(
            MetricsCorpus.Ints(c, "y_true"),
            MetricsCorpus.Ints(c, "y_pred"),
            MetricsCorpus.OptionalInts(c, "labels"),
            MetricsCorpus.OptionalDoubles(c, "sample_weight"));

        Assert.Equal(MetricsCorpus.Ints(c, "expected_labels"), cm.Labels);

        JsonElement expected = c.GetProperty("confusion_matrix");
        int k = cm.Labels.Count;
        Assert.Equal(k, expected.GetArrayLength());
        for (int row = 0; row < k; row++)
        {
            JsonElement expectedRow = expected[row];
            for (int col = 0; col < k; col++)
            {
                Assert.True(
                    Math.Abs(expectedRow[col].GetDouble() - cm[row, col]) < MetricsCorpus.Tolerance,
                    $"{what}: cell [{row},{col}] expected {expectedRow[col].GetDouble()}, got {cm[row, col]}");
            }
        }
    }

    [Fact]
    public void Rejects_mismatched_lengths()
    {
        int[] yTrue = [0, 1, 0];
        int[] yPred = [0, 1];
        Assert.Throws<ArgumentException>(() => ConfusionMatrix.Compute(yTrue, yPred));
    }

    [Fact]
    public void Rejects_empty_input()
    {
        Assert.Throws<ArgumentException>(() => ConfusionMatrix.Compute([], []));
    }

    [Fact]
    public void Rejects_mismatched_sample_weight_length()
    {
        int[] yTrue = [0, 1, 0];
        int[] yPred = [0, 1, 1];
        double[] weight = [1.0, 2.0];
        Assert.Throws<ArgumentException>(
            () => ConfusionMatrix.Compute(yTrue, yPred, default, weight));
    }

    [Fact]
    public void Rejects_duplicate_labels()
    {
        int[] yTrue = [0, 1, 0];
        int[] yPred = [0, 1, 1];
        int[] labels = [0, 1, 0];
        Assert.Throws<ArgumentException>(() => ConfusionMatrix.Compute(yTrue, yPred, labels));
    }

    [Fact]
    public void Rejects_labels_that_appear_in_no_true_value()
    {
        int[] yTrue = [0, 0, 1];
        int[] yPred = [0, 1, 1];
        int[] labels = [7, 8];
        Assert.Throws<ArgumentException>(() => ConfusionMatrix.Compute(yTrue, yPred, labels));
    }

    [Fact]
    public void Keeps_the_caller_s_label_order_unsorted()
    {
        int[] yTrue = [0, 1, 2, 2];
        int[] yPred = [0, 1, 2, 1];
        int[] labels = [2, 0, 1];

        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred, labels);

        Assert.Equal(labels, cm.Labels);
        Assert.Equal(1.0, cm[0, 0]);   // true 2, predicted 2
        Assert.Equal(1.0, cm[0, 2]);   // true 2, predicted 1
    }

    [Fact]
    public void Handles_label_values_that_are_not_zero_based()
    {
        int[] yTrue = [-1, 42, 5, 42];
        int[] yPred = [-1, 5, 5, 42];

        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred);

        Assert.Equal([-1, 5, 42], cm.Labels);
        Assert.Equal(4.0, cm.TotalWeight);
    }
}
```

- [ ] **Step 3: Run it and watch it fail to compile**

```bash
dotnet test tests/DataNet.Metrics.Tests -c Release
```

Expected: FAIL — `CS0103: The name 'ConfusionMatrix' does not exist`. That is the
red state; do not write implementation before seeing it.

- [ ] **Step 4: Implement label resolution**

`src/DataNet.Metrics/Internal/LabelIndex.cs`:

```csharp
namespace DataNet.Metrics.Internal;

/// <summary>
/// The set of labels a metric is computed over, plus the map from a label value
/// to its ordinal in that set.
/// </summary>
/// <remarks>
/// Two lookup strategies, chosen from the data rather than fixed: a direct
/// offset table when the label values are packed closely enough that the table
/// is cheaper than the samples it will serve, and a binary search over the
/// sorted values otherwise. A dictionary is never the right answer here — the
/// lookup runs twice per sample, and both strategies beat hashing an int.
/// </remarks>
internal sealed class LabelIndex
{
    // Above this, the offset table stops being a table and starts being a leak.
    private const int MaxDirectTableSize = 1 << 22;

    private readonly int[] _labels;
    private readonly int[]? _direct;     // (value - _min) -> ordinal, -1 when absent
    private readonly int _min;
    private readonly int[]? _sorted;     // ascending label values
    private readonly int[]? _ordinals;   // _sorted[i] -> ordinal in _labels

    private LabelIndex(int[] labels, bool isExplicit)
    {
        _labels = labels;
        Explicit = isExplicit;

        int min = labels[0];
        int max = labels[0];
        foreach (int value in labels)
        {
            if (value < min) { min = value; }
            if (value > max) { max = value; }
        }

        long range = (long)max - min + 1;
        if (range <= MaxDirectTableSize)
        {
            _min = min;
            _direct = new int[(int)range];
            for (int i = 0; i < _direct.Length; i++) { _direct[i] = -1; }
            for (int i = 0; i < labels.Length; i++)
            {
                int slot = labels[i] - min;
                if (_direct[slot] >= 0)
                {
                    throw new ArgumentException(
                        $"Label {labels[i]} appears more than once.", nameof(labels));
                }
                _direct[slot] = i;
            }
            return;
        }

        _sorted = (int[])labels.Clone();
        _ordinals = new int[labels.Length];
        for (int i = 0; i < _ordinals.Length; i++) { _ordinals[i] = i; }
        Array.Sort(_sorted, _ordinals);
        for (int i = 1; i < _sorted.Length; i++)
        {
            if (_sorted[i] == _sorted[i - 1])
            {
                throw new ArgumentException(
                    $"Label {_sorted[i]} appears more than once.", nameof(labels));
            }
        }
    }

    /// <summary>The labels, in the order metrics report them.</summary>
    public int[] Labels => _labels;

    /// <summary>How many labels the set holds.</summary>
    public int Count => _labels.Length;

    /// <summary>True when the caller supplied the label set explicitly.</summary>
    public bool Explicit { get; }

    /// <summary>The ordinal of <paramref name="label"/>, or -1 when it is not in the set.</summary>
    public int IndexOf(int label)
    {
        if (_direct is not null)
        {
            int slot = label - _min;
            return (uint)slot < (uint)_direct.Length ? _direct[slot] : -1;
        }

        int found = Array.BinarySearch(_sorted!, label);
        return found < 0 ? -1 : _ordinals![found];
    }

    /// <summary>
    /// Resolves the label set: the caller's order when supplied, otherwise the
    /// ascending sorted union of both inputs — scikit-learn's rule exactly.
    /// </summary>
    public static LabelIndex Create(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, ReadOnlySpan<int> labels)
    {
        if (!labels.IsEmpty)
        {
            return new LabelIndex(labels.ToArray(), isExplicit: true);
        }

        return new LabelIndex(SortedUnion(yTrue, yPred), isExplicit: false);
    }

    private static int[] SortedUnion(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred)
    {
        int min = yTrue[0];
        int max = yTrue[0];
        Extend(yTrue, ref min, ref max);
        Extend(yPred, ref min, ref max);

        long range = (long)max - min + 1;
        if (range <= MaxDirectTableSize && range <= (4L * yTrue.Length) + 1024)
        {
            // Dense enough: mark presence in one pass, then read the marks in
            // order. O(n + range) with no sort and no hashing.
            bool[] seen = new bool[(int)range];
            Mark(yTrue, seen, min);
            Mark(yPred, seen, min);

            int count = 0;
            foreach (bool present in seen) { if (present) { count++; } }

            int[] union = new int[count];
            int next = 0;
            for (int i = 0; i < seen.Length; i++)
            {
                if (seen[i]) { union[next++] = min + i; }
            }
            return union;
        }

        int[] all = new int[yTrue.Length + yPred.Length];
        yTrue.CopyTo(all);
        yPred.CopyTo(all.AsSpan(yTrue.Length));
        Array.Sort(all);

        int unique = 1;
        for (int i = 1; i < all.Length; i++)
        {
            if (all[i] != all[i - 1]) { all[unique++] = all[i]; }
        }
        int[] result = new int[unique];
        Array.Copy(all, result, unique);
        return result;
    }

    private static void Extend(ReadOnlySpan<int> values, ref int min, ref int max)
    {
        foreach (int value in values)
        {
            if (value < min) { min = value; }
            if (value > max) { max = value; }
        }
    }

    private static void Mark(ReadOnlySpan<int> values, bool[] seen, int min)
    {
        foreach (int value in values) { seen[value - min] = true; }
    }
}
```

If SonarAnalyzer flags `S3776` (cognitive complexity) on the constructor or
`SortedUnion`, add the pragma **in this file** with a reason, per
`CONTRIBUTING.md`. Do not reach for `.editorconfig`.

- [ ] **Step 5: Implement the matrix**

`src/DataNet.Metrics/ConfusionMatrix.cs`:

```csharp
using System.Collections.ObjectModel;
using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// A confusion matrix over predicted labels — the equivalent of
/// <c>sklearn.metrics.confusion_matrix</c>, and the shared engine every other
/// metric in this package derives from.
/// </summary>
/// <remarks>
/// <para>
/// Rows are true labels and columns are predicted ones, which is scikit-learn's
/// orientation. Computing the matrix once and asking it for several metrics
/// costs one pass; calling the scalar helpers separately counts once each.
/// </para>
/// <para>
/// Counts are <see cref="double"/> rather than <see cref="int"/> because
/// <c>sampleWeight</c> is supported throughout. Unweighted counts are exact:
/// a <see cref="double"/> represents every integer up to 2^53.
/// </para>
/// </remarks>
public sealed class ConfusionMatrix
{
    private readonly double[] _cells;
    private readonly int[] _labels;
    private readonly ReadOnlyCollection<int> _labelView;

    private ConfusionMatrix(
        double[] cells, int[] labels, double totalWeight, bool weighted, bool dropped, bool explicitLabels)
    {
        _cells = cells;
        _labels = labels;
        _labelView = Array.AsReadOnly(labels);
        TotalWeight = totalWeight;
        IsWeighted = weighted;
        DroppedSamples = dropped;
        ExplicitLabels = explicitLabels;
    }

    /// <summary>The labels, in the order rows and columns use.</summary>
    /// <remarks>
    /// The ascending sorted union of both inputs when <c>labels</c> was omitted;
    /// otherwise the caller's order, left unsorted — scikit-learn's rule.
    /// </remarks>
    public IReadOnlyList<int> Labels => _labelView;

    /// <summary>The total weight the matrix counted (the sample count when unweighted).</summary>
    public double TotalWeight { get; }

    /// <summary>The weight of samples whose true label is at <paramref name="trueIndex"/> and predicted label at <paramref name="predictedIndex"/>.</summary>
    /// <param name="trueIndex">Row: the index into <see cref="Labels"/> of the true label.</param>
    /// <param name="predictedIndex">Column: the index into <see cref="Labels"/> of the predicted label.</param>
    public double this[int trueIndex, int predictedIndex] => _cells[(trueIndex * _labels.Length) + predictedIndex];

    internal int Size => _labels.Length;

    internal ReadOnlySpan<double> Cells => _cells;

    internal bool IsWeighted { get; }

    /// <summary>True when at least one sample fell outside the label set and was not counted.</summary>
    internal bool DroppedSamples { get; }

    internal bool ExplicitLabels { get; }

    /// <summary>Copies the matrix into a two-dimensional array.</summary>
    /// <returns>A fresh <c>[rows, columns]</c> array; the matrix keeps its own storage.</returns>
    public double[,] ToArray()
    {
        int k = _labels.Length;
        double[,] result = new double[k, k];
        for (int row = 0; row < k; row++)
        {
            for (int col = 0; col < k; col++)
            {
                result[row, col] = _cells[(row * k) + col];
            }
        }
        return result;
    }

    /// <summary>
    /// Counts predictions against truth — the equivalent of
    /// <c>sklearn.metrics.confusion_matrix(y_true, y_pred, labels=…, sample_weight=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs. Samples whose true or predicted label falls outside this set are not counted, as in scikit-learn.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, contain duplicate labels, or no supplied label occurs in <paramref name="yTrue"/>.</exception>
    public static ConfusionMatrix Compute(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default)
    {
        if (yTrue.Length != yPred.Length)
        {
            throw new ArgumentException(
                $"yTrue has {yTrue.Length} entries and yPred has {yPred.Length}; they must agree.",
                nameof(yPred));
        }
        if (yTrue.Length == 0)
        {
            throw new ArgumentException("yTrue and yPred are empty; there is nothing to score.", nameof(yTrue));
        }
        if (!sampleWeight.IsEmpty && sampleWeight.Length != yTrue.Length)
        {
            throw new ArgumentException(
                $"sampleWeight has {sampleWeight.Length} entries but there are {yTrue.Length} samples.",
                nameof(sampleWeight));
        }

        LabelIndex index = LabelIndex.Create(yTrue, yPred, labels);
        int k = index.Count;
        double[] cells = new double[k * k];
        bool weighted = !sampleWeight.IsEmpty;
        double total = 0.0;
        bool dropped = false;
        bool anyTrueLabelKnown = false;

        for (int i = 0; i < yTrue.Length; i++)
        {
            int row = index.IndexOf(yTrue[i]);
            if (row >= 0)
            {
                anyTrueLabelKnown = true;
            }

            int col = index.IndexOf(yPred[i]);
            if (row < 0 || col < 0)
            {
                dropped = true;
                continue;
            }

            double weight = weighted ? sampleWeight[i] : 1.0;
            cells[(row * k) + col] += weight;
            total += weight;
        }

        if (index.Explicit && !anyTrueLabelKnown)
        {
            throw new ArgumentException(
                "At least one supplied label must occur in yTrue.", nameof(labels));
        }

        return new ConfusionMatrix(cells, index.Labels, total, weighted, dropped, index.Explicit);
    }
}
```

- [ ] **Step 6: Run the matrix tests green**

```bash
dotnet test tests/DataNet.Metrics.Tests -c Release --filter "FullyQualifiedName~ConfusionMatrixTests"
```

Expected: PASS, **31 tests** — 24 corpus rows plus the 7 hand-written ones. If
the count is lower, `MemberData` is not enumerating the corpus and the replay is
not happening.

- [ ] **Step 7: Write the failing accuracy tests**

`tests/DataNet.Metrics.Tests/AccuracyTests.cs`:

```csharp
using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class AccuracyTests
{
    [Theory]
    [MemberData(nameof(MetricsCorpus.Indices), MemberType = typeof(MetricsCorpus))]
    public void Matches_sklearn_accuracy_score(int index)
    {
        JsonElement c = MetricsCorpus.Cases[index];
        int[] yTrue = MetricsCorpus.Ints(c, "y_true");
        int[] yPred = MetricsCorpus.Ints(c, "y_pred");
        double[] weight = MetricsCorpus.OptionalDoubles(c, "sample_weight");

        Assert.Equal(c.GetProperty("accuracy").GetDouble(),
                     Accuracy.Score(yTrue, yPred, sampleWeight: weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("accuracy_count").GetDouble(),
                     Accuracy.Score(yTrue, yPred, normalize: false, sampleWeight: weight),
                     MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Reads_the_same_number_off_a_matrix_that_dropped_nothing()
    {
        int[] yTrue = [0, 1, 2, 2];
        int[] yPred = [0, 1, 2, 1];

        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred);

        Assert.Equal(Accuracy.Score(yTrue, yPred), Accuracy.Score(cm), MetricsCorpus.Tolerance);
    }
}
```

- [ ] **Step 8: Run it and watch it fail**

```bash
dotnet test tests/DataNet.Metrics.Tests -c Release --filter "FullyQualifiedName~AccuracyTests"
```

Expected: FAIL — `CS0103: The name 'Accuracy' does not exist`.

- [ ] **Step 9: Implement accuracy**

`src/DataNet.Metrics/Accuracy.cs`:

```csharp
namespace DataNet.Metrics;

/// <summary>
/// Plain agreement between truth and prediction — the equivalent of
/// <c>sklearn.metrics.accuracy_score</c>.
/// </summary>
public static class Accuracy
{
    /// <summary>
    /// The fraction of correctly predicted samples —
    /// <c>sklearn.metrics.accuracy_score(y_true, y_pred, normalize=…, sample_weight=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="normalize">When true (the default) return the fraction; when false, the weight of the correct samples.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length or are empty.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        bool normalize = true,
        ReadOnlySpan<double> sampleWeight = default)
    {
        if (yTrue.Length != yPred.Length)
        {
            throw new ArgumentException(
                $"yTrue has {yTrue.Length} entries and yPred has {yPred.Length}; they must agree.",
                nameof(yPred));
        }
        if (yTrue.Length == 0)
        {
            throw new ArgumentException("yTrue and yPred are empty; there is nothing to score.", nameof(yTrue));
        }
        if (!sampleWeight.IsEmpty && sampleWeight.Length != yTrue.Length)
        {
            throw new ArgumentException(
                $"sampleWeight has {sampleWeight.Length} entries but there are {yTrue.Length} samples.",
                nameof(sampleWeight));
        }

        bool weighted = !sampleWeight.IsEmpty;
        double correct = 0.0;
        double total = 0.0;
        for (int i = 0; i < yTrue.Length; i++)
        {
            double weight = weighted ? sampleWeight[i] : 1.0;
            if (yTrue[i] == yPred[i])
            {
                correct += weight;
            }
            total += weight;
        }

        return normalize ? correct / total : correct;
    }

    /// <summary>
    /// The same number read off an already-computed matrix: the weight on the
    /// diagonal over the total.
    /// </summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="normalize">When true (the default) return the fraction; when false, the weight on the diagonal.</param>
    /// <remarks>
    /// This is accuracy over the samples the matrix <em>kept</em>. A matrix built
    /// with an explicit label subset drops the samples outside it, so the result
    /// then differs from <c>accuracy_score</c>, which scores every sample.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    public static double Score(ConfusionMatrix cm, bool normalize = true)
    {
        Guard.NotNull(cm);

        int k = cm.Size;
        ReadOnlySpan<double> cells = cm.Cells;
        double diagonal = 0.0;
        for (int i = 0; i < k; i++)
        {
            diagonal += cells[(i * k) + i];
        }

        return normalize ? diagonal / cm.TotalWeight : diagonal;
    }
}
```

- [ ] **Step 10: Run the whole suite green**

```bash
dotnet build DataNet.slnx -c Release
dotnet test DataNet.slnx -c Release
```

Expected: clean build, and both new suites pass on **both** the net10 and the
netstandard2.0 mirror, which links the same sources and so runs the same suite.

- [ ] **Step 11: Commit**

```bash
git add src/DataNet.Metrics tests/DataNet.Metrics.Tests
git commit -F - <<'EOF'
Count once, and let every other metric read off that count

ConfusionMatrix is the engine rather than one more function: labels resolved
scikit-learn's way (sorted union, or the caller's order left unsorted), samples
outside an explicit label set dropped exactly as sklearn drops them, and one
weighted pass producing counts everything else divides.

Label lookup picks its strategy from the data — a direct offset table when the
values are packed, a binary search when they are not. It runs twice per sample,
which is why it is not a dictionary.

Accuracy takes the span path rather than building a matrix: it needs no label
set, and a matrix built from a label subset would answer a different question.
The overload that does read a matrix says so in its documentation.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 5: Precision, Recall, F1 and FBeta

**Files:**

- Create: `src/DataNet.Metrics/Internal/Prf.cs`
- Create: `src/DataNet.Metrics/Precision.cs`, `Recall.cs`, `F1.cs`, `FBeta.cs`
- Create: `tests/DataNet.Metrics.Tests/PrfOracleTests.cs`
- Create: `tests/DataNet.Metrics.Tests/PrfValidationTests.cs`

**Interfaces:**

- Consumes: `ConfusionMatrix` (Size, Cells, Labels, ExplicitLabels,
  DroppedSamples) from Task 4; the corpus from Task 3.
- Produces, on each of `Precision`, `Recall`, `F1` (and `FBeta` with a leading
  `double beta` on every overload):
  - `Score(ConfusionMatrix cm, Averaging average = Averaging.Binary,
    int posLabel = 1, ZeroDivision zeroDivision = ZeroDivision.Zero) -> double`
  - `PerClass(ConfusionMatrix cm, ZeroDivision zeroDivision = ZeroDivision.Zero)
    -> double[]`
  - `Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, Averaging average =
    Averaging.Binary, int posLabel = 1, ZeroDivision zeroDivision =
    ZeroDivision.Zero, ReadOnlySpan<int> labels = default,
    ReadOnlySpan<double> sampleWeight = default) -> double`
  - `PerClass(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, ZeroDivision
    zeroDivision = ZeroDivision.Zero, ReadOnlySpan<int> labels = default,
    ReadOnlySpan<double> sampleWeight = default) -> double[]`
- Also produces, internal: `enum PrfMetric { Precision, Recall, FScore }`,
  `Prf.PerClass(ConfusionMatrix, PrfMetric, double beta, ZeroDivision) -> double[]`,
  `Prf.Aggregate(ConfusionMatrix, PrfMetric, double beta, Averaging, int posLabel,
  ZeroDivision) -> double`, `Prf.Support(ConfusionMatrix) -> double[]`.

- [ ] **Step 1: Write the failing replay test**

`tests/DataNet.Metrics.Tests/PrfOracleTests.cs`:

```csharp
using System.Globalization;
using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class PrfOracleTests
{
    [Theory]
    [MemberData(nameof(MetricsCorpus.Indices), MemberType = typeof(MetricsCorpus))]
    public void Matches_sklearn_precision_recall_fscore_support(int index)
    {
        JsonElement c = MetricsCorpus.Cases[index];
        string what = MetricsCorpus.Describe(c);
        ConfusionMatrix cm = Build(c);
        int posLabel = c.GetProperty("pos_label").GetInt32();

        foreach (JsonProperty entry in c.GetProperty("averaged").EnumerateObject())
        {
            (Averaging average, ZeroDivision zero) = ParseKey(entry.Name);
            string key = $"{what} {entry.Name}";

            Assert.Equal(entry.Value.GetProperty("precision").GetDouble(),
                Precision.Score(cm, average, posLabel, zero), MetricsCorpus.Tolerance);
            Assert.Equal(entry.Value.GetProperty("recall").GetDouble(),
                Recall.Score(cm, average, posLabel, zero), MetricsCorpus.Tolerance);
            Assert.True(
                Math.Abs(entry.Value.GetProperty("f1").GetDouble()
                         - F1.Score(cm, average, posLabel, zero)) < MetricsCorpus.Tolerance,
                $"{key}: f1 diverged");
        }

        foreach (JsonProperty entry in c.GetProperty("per_class").EnumerateObject())
        {
            ZeroDivision zero = ParseZeroDivision(entry.Name);
            AssertSequence(entry.Value, "precision", Precision.PerClass(cm, zero), what);
            AssertSequence(entry.Value, "recall", Recall.PerClass(cm, zero), what);
            AssertSequence(entry.Value, "f1", F1.PerClass(cm, zero), what);
            AssertSequence(entry.Value, "support", Support(cm), what);
        }

        foreach (JsonProperty entry in c.GetProperty("fbeta").EnumerateObject())
        {
            string[] parts = entry.Name.Split('|');
            double beta = double.Parse(parts[0], CultureInfo.InvariantCulture);
            Averaging average = ParseAveraging(parts[1]);
            ZeroDivision zero = ParseZeroDivision(parts[2]);

            Assert.Equal(entry.Value.GetDouble(),
                FBeta.Score(cm, beta, average, posLabel, zero), MetricsCorpus.Tolerance);
        }
    }

    private static ConfusionMatrix Build(JsonElement c) => ConfusionMatrix.Compute(
        MetricsCorpus.Ints(c, "y_true"),
        MetricsCorpus.Ints(c, "y_pred"),
        MetricsCorpus.OptionalInts(c, "labels"),
        MetricsCorpus.OptionalDoubles(c, "sample_weight"));

    // Support is the row sum of the matrix; the oracle records it alongside the
    // per-class scores, so it is asserted here rather than trusted.
    private static double[] Support(ConfusionMatrix cm)
    {
        double[] support = new double[cm.Labels.Count];
        for (int row = 0; row < support.Length; row++)
        {
            for (int col = 0; col < support.Length; col++)
            {
                support[row] += cm[row, col];
            }
        }
        return support;
    }

    private static void AssertSequence(JsonElement expected, string name, double[] actual, string what)
    {
        double[] want = MetricsCorpus.Doubles(expected, name);
        Assert.Equal(want.Length, actual.Length);
        for (int i = 0; i < want.Length; i++)
        {
            Assert.True(Math.Abs(want[i] - actual[i]) < MetricsCorpus.Tolerance,
                $"{what}: {name}[{i}] expected {want[i]}, got {actual[i]}");
        }
    }

    private static (Averaging, ZeroDivision) ParseKey(string key)
    {
        string[] parts = key.Split('|');
        return (ParseAveraging(parts[0]), ParseZeroDivision(parts[1]));
    }

    private static Averaging ParseAveraging(string name) => name switch
    {
        "micro" => Averaging.Micro,
        "macro" => Averaging.Macro,
        "weighted" => Averaging.Weighted,
        "binary" => Averaging.Binary,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown averaging in the corpus."),
    };

    private static ZeroDivision ParseZeroDivision(string name) => name switch
    {
        "0" => ZeroDivision.Zero,
        "1" => ZeroDivision.One,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown zero_division in the corpus."),
    };
}
```

`tests/DataNet.Metrics.Tests/PrfValidationTests.cs`:

```csharp
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class PrfValidationTests
{
    // Class 2 is never predicted, so its precision divides by zero.
    private static readonly int[] YTrue = [0, 0, 1, 1, 2, 2];
    private static readonly int[] YPred = [0, 1, 1, 1, 0, 1];

    [Fact]
    public void Zero_division_zero_matches_sklearn_s_default_value()
    {
        double[] perClass = Precision.PerClass(YTrue, YPred);
        Assert.Equal(0.0, perClass[2], MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Zero_division_one_returns_one()
    {
        double[] perClass = Precision.PerClass(YTrue, YPred, ZeroDivision.One);
        Assert.Equal(1.0, perClass[2], MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Zero_division_nan_returns_nan()
    {
        double[] perClass = Precision.PerClass(YTrue, YPred, ZeroDivision.NaN);
        Assert.True(double.IsNaN(perClass[2]));
    }

    [Fact]
    public void Zero_division_throw_raises_instead_of_returning_a_silent_zero()
    {
        Assert.Throws<UndefinedMetricException>(
            () => Precision.PerClass(YTrue, YPred, ZeroDivision.Throw));
    }

    [Fact]
    public void Binary_averaging_rejects_a_three_class_target()
    {
        Assert.Throws<ArgumentException>(
            () => Precision.Score(YTrue, YPred, Averaging.Binary));
    }

    [Fact]
    public void Binary_averaging_rejects_a_pos_label_outside_the_target()
    {
        int[] yTrue = [0, 1, 1, 0];
        int[] yPred = [0, 1, 0, 0];
        Assert.Throws<ArgumentException>(
            () => Precision.Score(yTrue, yPred, Averaging.Binary, posLabel: 9));
    }

    [Fact]
    public void FBeta_rejects_a_negative_beta()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FBeta.Score(YTrue, YPred, -1.0, Averaging.Macro));
    }

    [Fact]
    public void Micro_averaged_f1_equals_accuracy_when_no_label_is_excluded()
    {
        double micro = F1.Score(YTrue, YPred, Averaging.Micro);
        Assert.Equal(Accuracy.Score(YTrue, YPred), micro, MetricsCorpus.Tolerance);
    }
}
```

- [ ] **Step 2: Run both and watch them fail**

```bash
dotnet test tests/DataNet.Metrics.Tests -c Release --filter "FullyQualifiedName~Prf"
```

Expected: FAIL — `CS0103: The name 'Precision' does not exist`.

- [ ] **Step 3: Implement the shared core**

`src/DataNet.Metrics/Internal/Prf.cs`:

```csharp
namespace DataNet.Metrics.Internal;

/// <summary>Which of the three related scores is being computed.</summary>
internal enum PrfMetric
{
    Precision,
    Recall,
    FScore,
}

/// <summary>
/// The arithmetic behind precision, recall and F-beta, kept in one place because
/// scikit-learn's zero-division and averaging rules are the whole difficulty and
/// are identical across the three.
/// </summary>
internal static class Prf
{
    /// <summary>
    /// scikit-learn's <c>_prf_divide</c>: the zero-division policy applies to a
    /// zero denominator per class, before any averaging.
    /// </summary>
    public static double Divide(double numerator, double denominator, ZeroDivision zeroDivision, string metric)
    {
        if (denominator != 0.0)
        {
            return numerator / denominator;
        }

        return Undefined(zeroDivision, metric);
    }

    public static double Undefined(ZeroDivision zeroDivision, string metric) => zeroDivision switch
    {
        ZeroDivision.Zero => 0.0,
        ZeroDivision.One => 1.0,
        ZeroDivision.NaN => double.NaN,
        _ => throw new UndefinedMetricException(
            $"{metric} is undefined here: no sample contributes to its denominator. "
            + "Pass ZeroDivision.Zero, One or NaN to get a value instead."),
    };

    /// <summary>Row sums of the matrix: the support of each class.</summary>
    public static double[] Support(ConfusionMatrix cm)
    {
        int k = cm.Size;
        ReadOnlySpan<double> cells = cm.Cells;
        double[] support = new double[k];
        for (int row = 0; row < k; row++)
        {
            double sum = 0.0;
            int offset = row * k;
            for (int col = 0; col < k; col++)
            {
                sum += cells[offset + col];
            }
            support[row] = sum;
        }
        return support;
    }

    /// <summary>Column sums: how much weight was predicted into each class.</summary>
    public static double[] PredictedSum(ConfusionMatrix cm)
    {
        int k = cm.Size;
        ReadOnlySpan<double> cells = cm.Cells;
        double[] predicted = new double[k];
        for (int row = 0; row < k; row++)
        {
            int offset = row * k;
            for (int col = 0; col < k; col++)
            {
                predicted[col] += cells[offset + col];
            }
        }
        return predicted;
    }

    /// <summary>The diagonal: correctly predicted weight per class.</summary>
    public static double[] TruePositives(ConfusionMatrix cm)
    {
        int k = cm.Size;
        ReadOnlySpan<double> cells = cm.Cells;
        double[] tp = new double[k];
        for (int i = 0; i < k; i++)
        {
            tp[i] = cells[(i * k) + i];
        }
        return tp;
    }

    public static double[] PerClass(ConfusionMatrix cm, PrfMetric metric, double beta, ZeroDivision zeroDivision)
    {
        double[] tp = TruePositives(cm);
        double[] predicted = PredictedSum(cm);
        double[] support = Support(cm);
        double[] result = new double[cm.Size];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = metric switch
            {
                PrfMetric.Precision => Divide(tp[i], predicted[i], zeroDivision, "Precision"),
                PrfMetric.Recall => Divide(tp[i], support[i], zeroDivision, "Recall"),
                _ => FScore(
                    Divide(tp[i], predicted[i], zeroDivision, "Precision"),
                    Divide(tp[i], support[i], zeroDivision, "Recall"),
                    beta,
                    zeroDivision),
            };
        }

        return result;
    }

    public static double Aggregate(
        ConfusionMatrix cm, PrfMetric metric, double beta, Averaging average, int posLabel, ZeroDivision zeroDivision)
    {
        if (average == Averaging.Micro)
        {
            return Micro(cm, metric, beta, zeroDivision);
        }

        double[] perClass = PerClass(cm, metric, beta, zeroDivision);

        switch (average)
        {
            case Averaging.Macro:
                double total = 0.0;
                foreach (double value in perClass)
                {
                    total += value;
                }
                return total / perClass.Length;

            case Averaging.Weighted:
                double[] support = Support(cm);
                double weightSum = 0.0;
                double weighted = 0.0;
                for (int i = 0; i < perClass.Length; i++)
                {
                    weighted += perClass[i] * support[i];
                    weightSum += support[i];
                }
                // scikit-learn returns 0.0 rather than dividing by zero here.
                return weightSum == 0.0 ? 0.0 : weighted / weightSum;

            case Averaging.Binary:
                return perClass[BinaryOrdinal(cm, posLabel)];

            default:
                throw new ArgumentOutOfRangeException(nameof(average), average, "Unknown averaging mode.");
        }
    }

    private static double Micro(ConfusionMatrix cm, PrfMetric metric, double beta, ZeroDivision zeroDivision)
    {
        double[] tp = TruePositives(cm);
        double[] predicted = PredictedSum(cm);
        double[] support = Support(cm);

        double tpSum = 0.0;
        double predictedSum = 0.0;
        double supportSum = 0.0;
        for (int i = 0; i < tp.Length; i++)
        {
            tpSum += tp[i];
            predictedSum += predicted[i];
            supportSum += support[i];
        }

        double precision = Divide(tpSum, predictedSum, zeroDivision, "Precision");
        double recall = Divide(tpSum, supportSum, zeroDivision, "Recall");

        return metric switch
        {
            PrfMetric.Precision => precision,
            PrfMetric.Recall => recall,
            _ => FScore(precision, recall, beta, zeroDivision),
        };
    }

    private static double FScore(double precision, double recall, double beta, ZeroDivision zeroDivision)
    {
        if (beta == 0.0)
        {
            return precision;
        }

        double beta2 = beta * beta;
        double denominator = (beta2 * precision) + recall;
        if (denominator == 0.0 || double.IsNaN(denominator))
        {
            return Undefined(zeroDivision, "F-score");
        }

        return (1.0 + beta2) * precision * recall / denominator;
    }

    private static int BinaryOrdinal(ConfusionMatrix cm, int posLabel)
    {
        // scikit-learn refuses average="binary" as soon as the *observed* target
        // has more than two classes. A matrix that dropped samples is exactly a
        // matrix whose label set did not cover what was observed.
        if (cm.Size > 2 || (cm.ExplicitLabels && cm.DroppedSamples))
        {
            throw new ArgumentException(
                "Averaging.Binary needs a two-class target. Use Micro, Macro or Weighted, or PerClass.",
                nameof(posLabel));
        }

        for (int i = 0; i < cm.Labels.Count; i++)
        {
            if (cm.Labels[i] == posLabel)
            {
                return i;
            }
        }

        throw new ArgumentException(
            $"posLabel {posLabel} does not occur in the data.", nameof(posLabel));
    }

    public static void ValidateBeta(double beta)
    {
        if (double.IsNaN(beta) || double.IsInfinity(beta) || beta < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(beta), beta, "beta must be a finite number greater than or equal to zero.");
        }
    }
}
```

- [ ] **Step 4: Implement the four public facades**

`src/DataNet.Metrics/Precision.cs`:

```csharp
using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// Of everything predicted into a class, how much belonged there — the
/// equivalent of <c>sklearn.metrics.precision_score</c>.
/// </summary>
public static class Precision
{
    /// <summary>Precision read off an existing matrix (<c>precision_score</c>).</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="average">How per-class scores are reduced. <c>Binary</c>, the default, matches scikit-learn.</param>
    /// <param name="posLabel">The class reported under <see cref="Averaging.Binary"/>.</param>
    /// <param name="zeroDivision">What to return when nothing was predicted into a class.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    /// <exception cref="ArgumentException"><see cref="Averaging.Binary"/> on a target with more than two classes, or a <paramref name="posLabel"/> that does not occur.</exception>
    /// <exception cref="UndefinedMetricException"><paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/> and the metric is undefined.</exception>
    public static double Score(
        ConfusionMatrix cm,
        Averaging average = Averaging.Binary,
        int posLabel = 1,
        ZeroDivision zeroDivision = ZeroDivision.Zero)
    {
        Guard.NotNull(cm);
        return Prf.Aggregate(cm, PrfMetric.Precision, 1.0, average, posLabel, zeroDivision);
    }

    /// <summary>Precision for every class, in label order (<c>precision_score(average=None)</c>).</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="zeroDivision">What to return when nothing was predicted into a class.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    public static double[] PerClass(ConfusionMatrix cm, ZeroDivision zeroDivision = ZeroDivision.Zero)
    {
        Guard.NotNull(cm);
        return Prf.PerClass(cm, PrfMetric.Precision, 1.0, zeroDivision);
    }

    /// <summary>Precision straight from the labels, counting the matrix on the way.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="average">How per-class scores are reduced.</param>
    /// <param name="posLabel">The class reported under <see cref="Averaging.Binary"/>.</param>
    /// <param name="zeroDivision">What to return when nothing was predicted into a class.</param>
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        Averaging average = Averaging.Binary,
        int posLabel = 1,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        Score(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), average, posLabel, zeroDivision);

    /// <summary>Per-class precision straight from the labels.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="zeroDivision">What to return when nothing was predicted into a class.</param>
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    public static double[] PerClass(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        PerClass(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), zeroDivision);
}
```

`src/DataNet.Metrics/Recall.cs` — identical shape, `PrfMetric.Recall`, summary
"Of everything that belonged to a class, how much was found — the equivalent of
`sklearn.metrics.recall_score`", and the zero-division wording becomes "when a
class has no samples". Copy the four members and swap the enum value.

`src/DataNet.Metrics/F1.cs` — identical shape, `PrfMetric.FScore` with
`beta: 1.0`, summary "The harmonic mean of precision and recall — the equivalent
of `sklearn.metrics.f1_score`".

`src/DataNet.Metrics/FBeta.cs` — same four members, each with `double beta` as
the **second** parameter (after `cm` or after `yPred`), each calling
`Prf.ValidateBeta(beta)` before delegating:

```csharp
using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// Precision and recall combined with a tunable balance — the equivalent of
/// <c>sklearn.metrics.fbeta_score</c>. <c>beta &lt; 1</c> favours precision,
/// <c>beta &gt; 1</c> favours recall, <c>beta = 1</c> is <see cref="F1"/>.
/// </summary>
public static class FBeta
{
    /// <summary>F-beta read off an existing matrix (<c>fbeta_score</c>).</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="beta">The weight of recall relative to precision. Must be finite and non-negative; <c>0</c> yields precision.</param>
    /// <param name="average">How per-class scores are reduced.</param>
    /// <param name="posLabel">The class reported under <see cref="Averaging.Binary"/>.</param>
    /// <param name="zeroDivision">What to return when the metric is undefined.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="beta"/> is negative, NaN or infinite.</exception>
    public static double Score(
        ConfusionMatrix cm,
        double beta,
        Averaging average = Averaging.Binary,
        int posLabel = 1,
        ZeroDivision zeroDivision = ZeroDivision.Zero)
    {
        Guard.NotNull(cm);
        Prf.ValidateBeta(beta);
        return Prf.Aggregate(cm, PrfMetric.FScore, beta, average, posLabel, zeroDivision);
    }

    /// <summary>F-beta for every class, in label order (<c>fbeta_score(average=None)</c>).</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="beta">The weight of recall relative to precision.</param>
    /// <param name="zeroDivision">What to return when the metric is undefined.</param>
    public static double[] PerClass(ConfusionMatrix cm, double beta, ZeroDivision zeroDivision = ZeroDivision.Zero)
    {
        Guard.NotNull(cm);
        Prf.ValidateBeta(beta);
        return Prf.PerClass(cm, PrfMetric.FScore, beta, zeroDivision);
    }

    /// <summary>F-beta straight from the labels, counting the matrix on the way.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="beta">The weight of recall relative to precision.</param>
    /// <param name="average">How per-class scores are reduced.</param>
    /// <param name="posLabel">The class reported under <see cref="Averaging.Binary"/>.</param>
    /// <param name="zeroDivision">What to return when the metric is undefined.</param>
    /// <param name="labels">The label set and its order.</param>
    /// <param name="sampleWeight">A weight per sample.</param>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        double beta,
        Averaging average = Averaging.Binary,
        int posLabel = 1,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        Score(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), beta, average, posLabel, zeroDivision);

    /// <summary>Per-class F-beta straight from the labels.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="beta">The weight of recall relative to precision.</param>
    /// <param name="zeroDivision">What to return when the metric is undefined.</param>
    /// <param name="labels">The label set and its order.</param>
    /// <param name="sampleWeight">A weight per sample.</param>
    public static double[] PerClass(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        double beta,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        PerClass(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), beta, zeroDivision);
}
```

- [ ] **Step 5: Run the replay green**

```bash
dotnet test tests/DataNet.Metrics.Tests -c Release --filter "FullyQualifiedName~Prf"
```

Expected: PASS, 32 tests (24 corpus rows + 8 validation facts). A corpus row that
fails names its fixture and the exact key — read the key before changing code:
`macro|1` failing while `macro|0` passes means the zero-division path is wrong,
not the averaging.

- [ ] **Step 6: Run everything and commit**

```bash
dotnet build DataNet.slnx -c Release && dotnet test DataNet.slnx -c Release
git add src/DataNet.Metrics tests/DataNet.Metrics.Tests
git commit -F - <<'EOF'
Put sklearn's averaging rules in one place, not six

Precision, recall and F-beta differ by two lines of arithmetic and share every
difficulty: which denominator can be zero, what a zero denominator returns, and
how per-class values collapse into one number. Prf holds that once and the four
public types are facades over it.

The rules that are easy to get subtly wrong are the ones the corpus pins: micro
sums the counts and divides once rather than averaging quotients, weighted
returns zero when every support is zero, and F-beta takes its zero-division
value from a zero *denominator*, not from a zero precision.

Averaging.Binary refuses a target that a label subset left uncovered, which is
how scikit-learn decides the same question.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 6: `ClassificationReport`, the structured half

**Files:**

- Create: `src/DataNet.Metrics/ClassRow.cs` (holds `ClassRow` and `AverageRow`)
- Create: `src/DataNet.Metrics/ClassificationReport.cs`
- Create: `tests/DataNet.Metrics.Tests/ClassificationReportTests.cs`

**Two refinements to the design spec**, both forced by the text layout Task 7
must reproduce, both worth carrying into the structured type:

1. The average rows are an `AverageRow` record (`Name`, `Precision`, `Recall`,
   `F1`, `Support`) rather than a `ClassRow`. A macro average has no label, and
   inventing one — `-1`, `0` — would be a lie the type tells forever.
2. `MicroAverage` is a nullable property, non-null exactly when scikit-learn's
   report would print a `micro avg` row instead of the `accuracy` row: when an
   explicit label set failed to cover the observed labels.

**Interfaces:**

- Consumes: `ConfusionMatrix`, `Prf`, `Accuracy` from Tasks 4-5.
- Produces:
  - `sealed record ClassRow(int Label, string? Name, double Precision,
    double Recall, double F1, double Support)`
  - `sealed record AverageRow(string Name, double Precision, double Recall,
    double F1, double Support)`
  - `ClassificationReport.Compute(ConfusionMatrix cm,
    IReadOnlyList<string>? targetNames = null,
    ZeroDivision zeroDivision = ZeroDivision.Zero) -> ClassificationReport`
  - the same `Compute` from spans, with `labels` and `sampleWeight` trailing
  - `Classes -> IReadOnlyList<ClassRow>`, `Accuracy -> double`,
    `MacroAverage -> AverageRow`, `WeightedAverage -> AverageRow`,
    `MicroAverage -> AverageRow?`, `TotalSupport -> double`
  - internal `IsWeighted -> bool`, `Digits`-free (Task 7 adds `ToText`)

- [ ] **Step 1: Write the failing test**

`tests/DataNet.Metrics.Tests/ClassificationReportTests.cs`:

```csharp
using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class ClassificationReportTests
{
    [Theory]
    [MemberData(nameof(MetricsCorpus.Indices), MemberType = typeof(MetricsCorpus))]
    public void Rows_carry_the_same_numbers_as_precision_recall_fscore_support(int index)
    {
        JsonElement c = MetricsCorpus.Cases[index];
        string what = MetricsCorpus.Describe(c);
        ConfusionMatrix cm = ConfusionMatrix.Compute(
            MetricsCorpus.Ints(c, "y_true"),
            MetricsCorpus.Ints(c, "y_pred"),
            MetricsCorpus.OptionalInts(c, "labels"),
            MetricsCorpus.OptionalDoubles(c, "sample_weight"));

        ClassificationReport report = ClassificationReport.Compute(cm, TargetNames(c));

        JsonElement perClass = c.GetProperty("per_class").GetProperty("0");
        double[] precision = MetricsCorpus.Doubles(perClass, "precision");
        double[] recall = MetricsCorpus.Doubles(perClass, "recall");
        double[] f1 = MetricsCorpus.Doubles(perClass, "f1");
        double[] support = MetricsCorpus.Doubles(perClass, "support");

        Assert.Equal(precision.Length, report.Classes.Count);
        for (int i = 0; i < precision.Length; i++)
        {
            ClassRow row = report.Classes[i];
            Assert.Equal(cm.Labels[i], row.Label);
            Assert.Equal(precision[i], row.Precision, MetricsCorpus.Tolerance);
            Assert.Equal(recall[i], row.Recall, MetricsCorpus.Tolerance);
            Assert.Equal(f1[i], row.F1, MetricsCorpus.Tolerance);
            Assert.Equal(support[i], row.Support, MetricsCorpus.Tolerance);
        }

        JsonElement macro = c.GetProperty("averaged").GetProperty("macro|0");
        Assert.Equal(macro.GetProperty("precision").GetDouble(), report.MacroAverage.Precision, MetricsCorpus.Tolerance);
        Assert.Equal(macro.GetProperty("f1").GetDouble(), report.MacroAverage.F1, MetricsCorpus.Tolerance);

        JsonElement weighted = c.GetProperty("averaged").GetProperty("weighted|0");
        Assert.Equal(weighted.GetProperty("recall").GetDouble(), report.WeightedAverage.Recall, MetricsCorpus.Tolerance);

        // Without an explicit label set nothing can have been dropped, so the micro
        // row must never appear. The subset case has its own fact below, because it
        // also depends on whether the subset happened to cover the data.
        if (c.GetProperty("labels").ValueKind == JsonValueKind.Null)
        {
            Assert.True(report.MicroAverage is null,
                $"{what}: a micro row appeared without an explicit label set");
        }
    }

    [Fact]
    public void Names_the_classes_when_target_names_are_supplied()
    {
        int[] yTrue = [0, 1, 1, 0];
        int[] yPred = [0, 1, 0, 0];

        ClassificationReport report = ClassificationReport.Compute(
            yTrue, yPred, targetNames: ["negative", "positive"]);

        Assert.Equal("negative", report.Classes[0].Name);
        Assert.Equal("positive", report.Classes[1].Name);
    }

    [Fact]
    public void Rejects_target_names_of_the_wrong_length()
    {
        int[] yTrue = [0, 1, 2];
        int[] yPred = [0, 1, 2];

        Assert.Throws<ArgumentException>(
            () => ClassificationReport.Compute(yTrue, yPred, targetNames: ["a", "b"]));
    }

    [Fact]
    public void Reports_a_micro_row_instead_of_accuracy_when_labels_exclude_something()
    {
        int[] yTrue = [0, 1, 2, 2, 0];
        int[] yPred = [0, 1, 1, 2, 2];

        ClassificationReport covered = ClassificationReport.Compute(yTrue, yPred);
        ClassificationReport partial = ClassificationReport.Compute(yTrue, yPred, labels: [0, 1]);

        Assert.Null(covered.MicroAverage);
        Assert.NotNull(partial.MicroAverage);
    }

    private static string[]? TargetNames(JsonElement c) =>
        c.GetProperty("target_names").ValueKind == JsonValueKind.Null
            ? null
            : [.. c.GetProperty("target_names").EnumerateArray().Select(x => x.GetString()!)];
}
```

- [ ] **Step 2: Run it and watch it fail**

```bash
dotnet test tests/DataNet.Metrics.Tests -c Release --filter "FullyQualifiedName~ClassificationReportTests"
```

Expected: FAIL — `CS0103: The name 'ClassificationReport' does not exist`.

- [ ] **Step 3: Implement the rows**

`src/DataNet.Metrics/ClassRow.cs`:

```csharp
namespace DataNet.Metrics;

/// <summary>One class's line in a <see cref="ClassificationReport"/>.</summary>
/// <param name="Label">The label value this line scores.</param>
/// <param name="Name">The readable name supplied through <c>targetNames</c>, or null.</param>
/// <param name="Precision">Precision for this class.</param>
/// <param name="Recall">Recall for this class.</param>
/// <param name="F1">F1 for this class.</param>
/// <param name="Support">The weight of samples whose true label is this class.</param>
public sealed record ClassRow(
    int Label, string? Name, double Precision, double Recall, double F1, double Support);

/// <summary>An averaged line in a <see cref="ClassificationReport"/>.</summary>
/// <param name="Name">The average's name, as scikit-learn prints it: <c>macro avg</c>, <c>weighted avg</c>, <c>micro avg</c>.</param>
/// <param name="Precision">The averaged precision.</param>
/// <param name="Recall">The averaged recall.</param>
/// <param name="F1">The averaged F1.</param>
/// <param name="Support">The total support the average covers.</param>
public sealed record AverageRow(
    string Name, double Precision, double Recall, double F1, double Support);
```

- [ ] **Step 4: Implement the report**

`src/DataNet.Metrics/ClassificationReport.cs`:

```csharp
using System.Collections.ObjectModel;
using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// The per-class table people actually read — the equivalent of
/// <c>sklearn.metrics.classification_report</c>.
/// </summary>
/// <remarks>
/// A class rather than a record: its rows live in a list, over which a
/// synthesised equality would compare references, and bit-exact equality over
/// computed <see cref="double"/> values would be misleading even if they did not.
/// </remarks>
public sealed class ClassificationReport
{
    private ClassificationReport(
        ReadOnlyCollection<ClassRow> classes,
        double accuracy,
        AverageRow macro,
        AverageRow weighted,
        AverageRow? micro,
        double totalSupport,
        bool isWeighted)
    {
        Classes = classes;
        Accuracy = accuracy;
        MacroAverage = macro;
        WeightedAverage = weighted;
        MicroAverage = micro;
        TotalSupport = totalSupport;
        IsWeighted = isWeighted;
    }

    /// <summary>One line per class, in the matrix's label order.</summary>
    public IReadOnlyList<ClassRow> Classes { get; }

    /// <summary>Accuracy over the samples the matrix counted.</summary>
    public double Accuracy { get; }

    /// <summary>The unweighted mean of the per-class scores.</summary>
    public AverageRow MacroAverage { get; }

    /// <summary>The support-weighted mean of the per-class scores.</summary>
    public AverageRow WeightedAverage { get; }

    /// <summary>
    /// The micro average, non-null exactly when an explicit label set left an
    /// observed label out — which is when scikit-learn's text prints a
    /// <c>micro avg</c> row in place of the <c>accuracy</c> row.
    /// </summary>
    public AverageRow? MicroAverage { get; }

    /// <summary>The total weight the report covers.</summary>
    public double TotalSupport { get; }

    internal bool IsWeighted { get; }

    /// <summary>
    /// Builds the report from an existing matrix —
    /// <c>classification_report(y_true, y_pred, target_names=…, zero_division=…)</c>.
    /// </summary>
    /// <param name="cm">The matrix to summarise.</param>
    /// <param name="targetNames">Readable names, one per label, in label order. The equivalent of scikit-learn's <c>target_names</c>.</param>
    /// <param name="zeroDivision">What an undefined per-class score returns.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="targetNames"/> has a different length from the label set.</exception>
    public static ClassificationReport Compute(
        ConfusionMatrix cm,
        IReadOnlyList<string>? targetNames = null,
        ZeroDivision zeroDivision = ZeroDivision.Zero)
    {
        Guard.NotNull(cm);

        int k = cm.Size;
        if (targetNames is not null && targetNames.Count != k)
        {
            throw new ArgumentException(
                $"targetNames has {targetNames.Count} entries but there are {k} labels.",
                nameof(targetNames));
        }

        double[] precision = Prf.PerClass(cm, PrfMetric.Precision, 1.0, zeroDivision);
        double[] recall = Prf.PerClass(cm, PrfMetric.Recall, 1.0, zeroDivision);
        double[] f1 = Prf.PerClass(cm, PrfMetric.FScore, 1.0, zeroDivision);
        double[] support = Prf.Support(cm);

        ClassRow[] rows = new ClassRow[k];
        double totalSupport = 0.0;
        for (int i = 0; i < k; i++)
        {
            rows[i] = new ClassRow(cm.Labels[i], targetNames?[i], precision[i], recall[i], f1[i], support[i]);
            totalSupport += support[i];
        }

        AverageRow macro = new(
            "macro avg",
            Prf.Aggregate(cm, PrfMetric.Precision, 1.0, Averaging.Macro, 0, zeroDivision),
            Prf.Aggregate(cm, PrfMetric.Recall, 1.0, Averaging.Macro, 0, zeroDivision),
            Prf.Aggregate(cm, PrfMetric.FScore, 1.0, Averaging.Macro, 0, zeroDivision),
            totalSupport);

        AverageRow weighted = new(
            "weighted avg",
            Prf.Aggregate(cm, PrfMetric.Precision, 1.0, Averaging.Weighted, 0, zeroDivision),
            Prf.Aggregate(cm, PrfMetric.Recall, 1.0, Averaging.Weighted, 0, zeroDivision),
            Prf.Aggregate(cm, PrfMetric.FScore, 1.0, Averaging.Weighted, 0, zeroDivision),
            totalSupport);

        AverageRow? micro = null;
        if (cm.ExplicitLabels && cm.DroppedSamples)
        {
            micro = new AverageRow(
                "micro avg",
                Prf.Aggregate(cm, PrfMetric.Precision, 1.0, Averaging.Micro, 0, zeroDivision),
                Prf.Aggregate(cm, PrfMetric.Recall, 1.0, Averaging.Micro, 0, zeroDivision),
                Prf.Aggregate(cm, PrfMetric.FScore, 1.0, Averaging.Micro, 0, zeroDivision),
                totalSupport);
        }

        // Fully qualified on purpose: this class has an `Accuracy` property, and an
        // unqualified `Accuracy.Score(cm)` binds to it rather than to the type.
        double accuracy = DataNet.Metrics.Accuracy.Score(cm);

        return new ClassificationReport(
            Array.AsReadOnly(rows), accuracy, macro, weighted, micro, totalSupport, cm.IsWeighted);
    }

    /// <summary>Builds the report straight from the labels, counting the matrix on the way.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="targetNames">Readable names, one per label, in label order.</param>
    /// <param name="zeroDivision">What an undefined per-class score returns.</param>
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    public static ClassificationReport Compute(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        IReadOnlyList<string>? targetNames = null,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        Compute(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), targetNames, zeroDivision);
}
```

- [ ] **Step 5: Run green and commit**

```bash
dotnet test tests/DataNet.Metrics.Tests -c Release --filter "FullyQualifiedName~ClassificationReportTests"
dotnet build DataNet.slnx -c Release && dotnet test DataNet.slnx -c Release
git add src/DataNet.Metrics tests/DataNet.Metrics.Tests
git commit -F - <<'EOF'
Give the report a shape before giving it a layout

ClassificationReport is a class, not a record: its rows live in a list, where a
synthesised equality compares references, and value equality over computed
doubles would answer "different" for two reports anyone would call identical.

Two departures from the design, both forced by what the text layout has to
render. Average rows are their own record rather than a labelled class row — a
macro average has no label and inventing one would be a lie the type keeps
telling. And the micro average is nullable, present exactly when an explicit
label set left an observed label out, which is when scikit-learn replaces the
accuracy row with a micro avg row.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 7: `ToText`, character for character

**Files:**

- Create: `src/DataNet.Metrics/Internal/ReportText.cs`
- Modify: `src/DataNet.Metrics/ClassificationReport.cs` (add `ToText`, `ToString`)
- Create: `tests/DataNet.Metrics.Tests/ReportTextTests.cs`

**Interfaces:**

- Consumes: `ClassificationReport` from Task 6, the `reports` field of the corpus.
- Produces: `ClassificationReport.ToText(int digits = 2) -> string` and
  `ToString()` returning `ToText()`.

**The layout, from scikit-learn's own format strings** — reproduce these, do not
improvise:

```text
width      = max(longest row name, len("weighted avg"), digits)
header     = "{:>{width}s} ".format("") + "".join(" {:>9}".format(h) for h in
             ["precision", "recall", "f1-score", "support"]) + "\n\n"
class row  = "{:>{width}s} ".format(name)
             + "".join(" {:>9.{digits}f}".format(v) for v in [p, r, f1])
             + " {:>9}".format(support) + "\n"
(blank line after the class rows)
accuracy   = "{:>{width}s} ".format("accuracy")
             + " " + " " * 9 + " " + " " * 9
             + " {:>9.{digits}f}".format(accuracy) + " {:>9}".format(support) + "\n"
micro avg  = the class-row format, name "micro avg"   (replaces the accuracy row)
macro avg  = the class-row format, name "macro avg"
weighted   = the class-row format, name "weighted avg"
```

Two details the frozen strings will punish you for:

- **Rounding.** Python's format rounds half to even on the exact binary value;
  .NET's `"F2"` does not. Use
  `Math.Round(v, digits, MidpointRounding.ToEven).ToString("F" + digits, CultureInfo.InvariantCulture)`.
- **Support.** Unweighted, scikit-learn prints a NumPy integer: `3`. Weighted, it
  prints a NumPy float through `repr`: `3.5`, and `4.0` for a whole number where
  .NET would write `4`. Branch on `IsWeighted`, and append `.0` when the
  round-tripped text has no `.`, `e` or `E`.

- [ ] **Step 1: Write the failing test**

`tests/DataNet.Metrics.Tests/ReportTextTests.cs`:

```csharp
using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class ReportTextTests
{
    [Theory]
    [MemberData(nameof(MetricsCorpus.Indices), MemberType = typeof(MetricsCorpus))]
    public void Renders_the_sklearn_table_character_for_character(int index)
    {
        JsonElement c = MetricsCorpus.Cases[index];
        string what = MetricsCorpus.Describe(c);

        ClassificationReport report = ClassificationReport.Compute(
            MetricsCorpus.Ints(c, "y_true"),
            MetricsCorpus.Ints(c, "y_pred"),
            TargetNames(c),
            ZeroDivision.Zero,
            MetricsCorpus.OptionalInts(c, "labels"),
            MetricsCorpus.OptionalDoubles(c, "sample_weight"));

        foreach (JsonProperty entry in c.GetProperty("reports").EnumerateObject())
        {
            int digits = int.Parse(entry.Name, System.Globalization.CultureInfo.InvariantCulture);
            string expected = entry.Value.GetString()!;
            string actual = report.ToText(digits);

            Assert.True(expected == actual,
                $"{what} at {digits} digits:\n--- expected ---\n{expected}\n--- actual ---\n{actual}");
        }
    }

    [Fact]
    public void ToString_is_the_two_digit_table()
    {
        int[] yTrue = [0, 1, 1, 0];
        int[] yPred = [0, 1, 0, 0];
        ClassificationReport report = ClassificationReport.Compute(yTrue, yPred);

        Assert.Equal(report.ToText(), report.ToString());
    }

    [Fact]
    public void Rejects_a_digit_count_below_zero()
    {
        int[] yTrue = [0, 1];
        int[] yPred = [0, 1];
        ClassificationReport report = ClassificationReport.Compute(yTrue, yPred);

        Assert.Throws<ArgumentOutOfRangeException>(() => report.ToText(-1));
    }

    private static string[]? TargetNames(JsonElement c) =>
        c.GetProperty("target_names").ValueKind == JsonValueKind.Null
            ? null
            : [.. c.GetProperty("target_names").EnumerateArray().Select(x => x.GetString()!)];
}
```

- [ ] **Step 2: Run it and watch it fail**

```bash
dotnet test tests/DataNet.Metrics.Tests -c Release --filter "FullyQualifiedName~ReportTextTests"
```

Expected: FAIL — `'ClassificationReport' does not contain a definition for 'ToText'`.

- [ ] **Step 3: Implement the renderer**

`src/DataNet.Metrics/Internal/ReportText.cs`:

```csharp
using System.Globalization;
using System.Text;

namespace DataNet.Metrics.Internal;

/// <summary>
/// Renders a <see cref="ClassificationReport"/> in scikit-learn's own layout,
/// character for character.
/// </summary>
/// <remarks>
/// The format strings are transcribed from <c>classification_report</c>: a name
/// column as wide as the longest heading, then four columns of width 9 each
/// preceded by a space. The frozen oracle is what proves the transcription.
/// </remarks>
internal static class ReportText
{
    private const int ColumnWidth = 9;
    private static readonly string[] Headers = ["precision", "recall", "f1-score", "support"];

    public static string Render(ClassificationReport report, int digits)
    {
        int width = ColumnNameWidth(report, digits);
        var text = new StringBuilder();

        text.Append(new string(' ', width)).Append(' ');
        foreach (string header in Headers)
        {
            text.Append(' ').Append(header.PadLeft(ColumnWidth));
        }
        text.Append('\n').Append('\n');

        foreach (ClassRow row in report.Classes)
        {
            AppendRow(text, NameOf(row), width, digits, report.IsWeighted,
                      row.Precision, row.Recall, row.F1, row.Support);
        }
        text.Append('\n');

        if (report.MicroAverage is AverageRow micro)
        {
            AppendRow(text, micro.Name, width, digits, report.IsWeighted,
                      micro.Precision, micro.Recall, micro.F1, micro.Support);
        }
        else
        {
            // The accuracy row leaves the precision and recall columns blank.
            text.Append("accuracy".PadLeft(width)).Append(' ');
            text.Append(' ').Append(new string(' ', ColumnWidth));
            text.Append(' ').Append(new string(' ', ColumnWidth));
            text.Append(' ').Append(Number(report.Accuracy, digits).PadLeft(ColumnWidth));
            text.Append(' ').Append(Support(report.TotalSupport, report.IsWeighted).PadLeft(ColumnWidth));
            text.Append('\n');
        }

        AppendRow(text, report.MacroAverage.Name, width, digits, report.IsWeighted,
                  report.MacroAverage.Precision, report.MacroAverage.Recall,
                  report.MacroAverage.F1, report.MacroAverage.Support);
        AppendRow(text, report.WeightedAverage.Name, width, digits, report.IsWeighted,
                  report.WeightedAverage.Precision, report.WeightedAverage.Recall,
                  report.WeightedAverage.F1, report.WeightedAverage.Support);

        return text.ToString();
    }

    private static void AppendRow(
        StringBuilder text, string name, int width, int digits, bool weighted,
        double precision, double recall, double f1, double support)
    {
        text.Append(name.PadLeft(width)).Append(' ');
        text.Append(' ').Append(Number(precision, digits).PadLeft(ColumnWidth));
        text.Append(' ').Append(Number(recall, digits).PadLeft(ColumnWidth));
        text.Append(' ').Append(Number(f1, digits).PadLeft(ColumnWidth));
        text.Append(' ').Append(Support(support, weighted).PadLeft(ColumnWidth));
        text.Append('\n');
    }

    private static int ColumnNameWidth(ClassificationReport report, int digits)
    {
        int width = "weighted avg".Length;
        foreach (ClassRow row in report.Classes)
        {
            int length = NameOf(row).Length;
            if (length > width)
            {
                width = length;
            }
        }
        return width > digits ? width : digits;
    }

    private static string NameOf(ClassRow row) =>
        row.Name ?? row.Label.ToString(CultureInfo.InvariantCulture);

    private static string Number(double value, int digits) =>
        // Python rounds half to even on the exact binary value; "F" alone does not.
        Math.Round(value, digits, MidpointRounding.ToEven)
            .ToString("F" + digits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    private static string Support(double value, bool weighted)
    {
        if (!weighted)
        {
            // Unweighted counts are whole, and scikit-learn prints a NumPy integer.
            return ((long)Math.Round(value, MidpointRounding.AwayFromZero))
                .ToString(CultureInfo.InvariantCulture);
        }

        string text = value.ToString("R", CultureInfo.InvariantCulture);
        bool looksIntegral = text.IndexOf('.') < 0
            && text.IndexOf('e') < 0
            && text.IndexOf('E') < 0;

        // Python's float repr always carries a decimal point: 4.0, never 4.
        return looksIntegral ? text + ".0" : text;
    }
}
```

- [ ] **Step 4: Wire it into the report**

Add to `src/DataNet.Metrics/ClassificationReport.cs`:

```csharp
    /// <summary>
    /// Renders the table the way <c>classification_report</c> prints it, to the
    /// character.
    /// </summary>
    /// <param name="digits">Decimal places for the three score columns, as scikit-learn's <c>digits</c>.</param>
    /// <remarks>
    /// Parity is asserted for <see cref="ZeroDivision.Zero"/> and
    /// <see cref="ZeroDivision.One"/>. A report built with
    /// <see cref="ZeroDivision.NaN"/> renders .NET's <c>NaN</c> where Python
    /// writes <c>nan</c>; the numbers still match, the text does not.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="digits"/> is negative.</exception>
    public string ToText(int digits = 2)
    {
        Guard.NotLessThan(digits, 0);
        return ReportText.Render(this, digits);
    }

    /// <summary>The two-digit table, as <see cref="ToText"/> renders it.</summary>
    public override string ToString() => ToText();
```

- [ ] **Step 5: Iterate against the frozen strings**

```bash
dotnet test tests/DataNet.Metrics.Tests -c Release --filter "FullyQualifiedName~ReportTextTests"
```

The failure message prints both tables one under the other. Fix by column: a
shifted name column is `width`, a shifted number column is a missing leading
space, `4` where `4.0` was wanted is the weighted-support branch, and a last-digit
difference is the rounding mode. Expected when done: PASS, 26 tests (24 corpus
rows + 2 facts).

- [ ] **Step 6: Commit**

```bash
dotnet build DataNet.slnx -c Release && dotnet test DataNet.slnx -c Release
git add src/DataNet.Metrics tests/DataNet.Metrics.Tests
git commit -F - <<'EOF'
Print the table sklearn prints, not one that looks like it

The layout is transcribed from classification_report's own format strings
rather than approximated, because the point of this row in the migration guide
is that a reader can put the two outputs side by side.

Two details the frozen strings caught rather than the reading did: Python
rounds half to even on the exact binary value where .NET's "F" format does not,
and a weighted support prints as 4.0 through NumPy's repr where .NET writes 4.

ZeroDivision.NaN is documented as outside text parity — the numbers still
match, but .NET writes NaN where Python writes nan.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 8: ROC-AUC, the binary case

**Files:**

- Create: `src/DataNet.Metrics/Internal/BinaryRoc.cs`
- Create: `src/DataNet.Metrics/RocAuc.cs`
- Create: `tests/DataNet.Metrics.Tests/RocCorpus.cs`
- Create: `tests/DataNet.Metrics.Tests/RocAucBinaryTests.cs`

**Interfaces:**

- Consumes: the `roc_auc.json` corpus from Task 3.
- Produces: `RocAuc.Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore,
  int posLabel = 1, ReadOnlySpan<double> sampleWeight = default) -> double`, and
  internal `BinaryRoc.Score(ReadOnlySpan<int>, ReadOnlySpan<double>, int,
  ReadOnlySpan<double>) -> double`.

- [ ] **Step 1: Write the corpus helper and the failing test**

`tests/DataNet.Metrics.Tests/RocCorpus.cs`:

```csharp
using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

/// <summary>Shared access to the frozen ROC-AUC corpus.</summary>
internal static class RocCorpus
{
    private static readonly JsonDocument Document = OracleLoader.Load("roc_auc.json");

    public static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("cases").EnumerateArray()];

    public static TheoryData<int> Indices(string kind)
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            if (Cases[i].GetProperty("kind").GetString() == kind)
            {
                data.Add(i);
            }
        }
        return data;
    }

    public static TheoryData<int> BinaryIndices() => Indices("binary");

    public static TheoryData<int> MulticlassIndices() => Indices("multiclass");

    public static string Describe(JsonElement c) =>
        $"{c.GetProperty("fixture").GetString()} (weighted={c.GetProperty("weighted").GetBoolean()})";

    public static int[] YTrue(JsonElement c) =>
        [.. c.GetProperty("y_true").EnumerateArray().Select(x => x.GetInt32())];

    public static double[] SampleWeight(JsonElement c) =>
        c.GetProperty("sample_weight").ValueKind == JsonValueKind.Null
            ? []
            : [.. c.GetProperty("sample_weight").EnumerateArray().Select(x => x.GetDouble())];

    /// <summary>Binary scores: one per sample.</summary>
    public static double[] FlatScores(JsonElement c) =>
        [.. c.GetProperty("scores").EnumerateArray().Select(x => x.GetDouble())];

    /// <summary>Multiclass scores flattened row-major, which is what the API takes.</summary>
    public static double[] RowMajorScores(JsonElement c) =>
        [.. c.GetProperty("scores").EnumerateArray()
              .SelectMany(row => row.EnumerateArray())
              .Select(x => x.GetDouble())];
}
```

`tests/DataNet.Metrics.Tests/RocAucBinaryTests.cs`:

```csharp
using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class RocAucBinaryTests
{
    [Theory]
    [MemberData(nameof(RocCorpus.BinaryIndices), MemberType = typeof(RocCorpus))]
    public void Matches_sklearn_roc_auc_score(int index)
    {
        JsonElement c = RocCorpus.Cases[index];
        double expected = c.GetProperty("values").GetProperty("binary").GetDouble();

        double actual = RocAuc.Score(
            RocCorpus.YTrue(c), RocCorpus.FlatScores(c), sampleWeight: RocCorpus.SampleWeight(c));

        Assert.True(Math.Abs(expected - actual) < MetricsCorpus.Tolerance,
            $"{RocCorpus.Describe(c)}: expected {expected}, got {actual}");
    }

    [Fact]
    public void Rejects_a_single_class()
    {
        int[] yTrue = [1, 1, 1];
        double[] scores = [0.1, 0.4, 0.9];

        Assert.Throws<ArgumentException>(() => RocAuc.Score(yTrue, scores));
    }

    [Fact]
    public void Rejects_mismatched_lengths()
    {
        int[] yTrue = [0, 1, 1];
        double[] scores = [0.1, 0.4];

        Assert.Throws<ArgumentException>(() => RocAuc.Score(yTrue, scores));
    }

    [Fact]
    public void Rejects_a_nan_score()
    {
        int[] yTrue = [0, 1];
        double[] scores = [0.5, double.NaN];

        Assert.Throws<ArgumentException>(() => RocAuc.Score(yTrue, scores));
    }

    [Fact]
    public void A_perfect_ranking_scores_one_and_its_reverse_scores_zero()
    {
        int[] yTrue = [0, 0, 1, 1];
        double[] good = [0.1, 0.2, 0.8, 0.9];
        double[] bad = [0.9, 0.8, 0.2, 0.1];

        Assert.Equal(1.0, RocAuc.Score(yTrue, good), MetricsCorpus.Tolerance);
        Assert.Equal(0.0, RocAuc.Score(yTrue, bad), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void All_scores_tied_gives_one_half()
    {
        int[] yTrue = [0, 1, 0, 1];
        double[] scores = [0.5, 0.5, 0.5, 0.5];

        Assert.Equal(0.5, RocAuc.Score(yTrue, scores), MetricsCorpus.Tolerance);
    }
}
```

- [ ] **Step 2: Run it and watch it fail**

```bash
dotnet test tests/DataNet.Metrics.Tests -c Release --filter "FullyQualifiedName~RocAucBinaryTests"
```

Expected: FAIL — `CS0103: The name 'RocAuc' does not exist`.

- [ ] **Step 3: Implement the curve**

`src/DataNet.Metrics/Internal/BinaryRoc.cs`:

```csharp
namespace DataNet.Metrics.Internal;

/// <summary>
/// The binary ROC curve and the area under it — the mechanics of
/// scikit-learn's <c>_binary_clf_curve</c> followed by <c>auc</c>.
/// </summary>
/// <remarks>
/// Samples are sorted by descending score and equal scores are consumed as one
/// group, which is what makes ties come out the same as scikit-learn's. The
/// trapezoid is accumulated on unnormalised counts and divided once at the end:
/// fewer roundings, and the same number.
/// </remarks>
internal static class BinaryRoc
{
    private struct Point
    {
        public double Weight;
        public double PositiveWeight;
    }

    public static double Score(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel, ReadOnlySpan<double> sampleWeight)
    {
        int n = yTrue.Length;
        if (yScore.Length != n)
        {
            throw new ArgumentException(
                $"yTrue has {n} entries and yScore has {yScore.Length}; they must agree.", nameof(yScore));
        }
        if (n == 0)
        {
            throw new ArgumentException("yTrue and yScore are empty; there is nothing to score.", nameof(yTrue));
        }
        if (!sampleWeight.IsEmpty && sampleWeight.Length != n)
        {
            throw new ArgumentException(
                $"sampleWeight has {sampleWeight.Length} entries but there are {n} samples.",
                nameof(sampleWeight));
        }

        // Negated scores, so an ascending sort walks the curve from the highest
        // score down — and Array.Sort compares doubles natively rather than
        // through a delegate.
        double[] keys = new double[n];
        Point[] points = new Point[n];
        bool weighted = !sampleWeight.IsEmpty;

        for (int i = 0; i < n; i++)
        {
            double score = yScore[i];
            if (double.IsNaN(score))
            {
                throw new ArgumentException($"yScore[{i}] is NaN; scores must be numbers.", nameof(yScore));
            }

            double weight = weighted ? sampleWeight[i] : 1.0;
            keys[i] = -score;
            points[i].Weight = weight;
            points[i].PositiveWeight = yTrue[i] == posLabel ? weight : 0.0;
        }

        Array.Sort(keys, points);

        double truePositives = 0.0;
        double falsePositives = 0.0;
        double previousTrue = 0.0;
        double previousFalse = 0.0;
        double area = 0.0;

        for (int i = 0; i < n; i++)
        {
            truePositives += points[i].PositiveWeight;
            falsePositives += points[i].Weight - points[i].PositiveWeight;

            bool lastOfGroup = i == n - 1 || keys[i] != keys[i + 1];
            if (!lastOfGroup)
            {
                continue;
            }

            area += (falsePositives - previousFalse) * (truePositives + previousTrue) * 0.5;
            previousTrue = truePositives;
            previousFalse = falsePositives;
        }

        if (truePositives == 0.0 || falsePositives == 0.0)
        {
            throw new ArgumentException(
                "Only one class is present in yTrue; ROC AUC is undefined for it.", nameof(yTrue));
        }

        return area / (truePositives * falsePositives);
    }
}
```

- [ ] **Step 4: Implement the public entry point**

`src/DataNet.Metrics/RocAuc.cs`:

```csharp
using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// Area under the receiver-operating-characteristic curve — the equivalent of
/// <c>sklearn.metrics.roc_auc_score</c>.
/// </summary>
/// <remarks>
/// Two entry points rather than scikit-learn's single overloaded function: their
/// parameter lists would be indistinguishable to the C# compiler, and a call
/// like <c>Score(y, s, 3)</c> would fail to compile in consumer code.
/// </remarks>
public static class RocAuc
{
    /// <summary>
    /// The binary case — <c>roc_auc_score(y_true, y_score, sample_weight=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true labels. Exactly two distinct values must occur.</param>
    /// <param name="yScore">A score per sample: the higher, the more the model believes <paramref name="posLabel"/>.</param>
    /// <param name="posLabel">The label counted as positive. scikit-learn infers this; 1 is what it infers for 0/1 labels.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, contain a NaN score, or only one class occurs.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int posLabel = 1,
        ReadOnlySpan<double> sampleWeight = default) =>
        BinaryRoc.Score(yTrue, yScore, posLabel, sampleWeight);
}
```

- [ ] **Step 5: Run green and commit**

```bash
dotnet test tests/DataNet.Metrics.Tests -c Release --filter "FullyQualifiedName~RocAucBinaryTests"
dotnet build DataNet.slnx -c Release && dotnet test DataNet.slnx -c Release
git add src/DataNet.Metrics tests/DataNet.Metrics.Tests
git commit -F - <<'EOF'
Walk the ROC curve rather than rank the samples

Average ranks give the right answer for the unweighted case and quietly the
wrong one once samples carry weights. Sorting by descending score, consuming
equal scores as one group and accumulating trapezoids is what scikit-learn
does, and it is the version that stays correct under weighting — the corpus
covers a heavily tied fixture precisely because that is where a rank shortcut
and a real curve part company.

The area is accumulated on unnormalised counts and divided once at the end,
which is one rounding instead of two per point.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 9: ROC-AUC, one-vs-rest and one-vs-one

**Files:**

- Create: `src/DataNet.Metrics/Internal/MultiClassRoc.cs`
- Modify: `src/DataNet.Metrics/RocAuc.cs` (add `MultiClass`)
- Create: `tests/DataNet.Metrics.Tests/RocAucMultiClassTests.cs`

**Interfaces:**

- Consumes: `BinaryRoc` from Task 8, the multiclass rows of `roc_auc.json`.
- Produces: `RocAuc.MultiClass(ReadOnlySpan<int> yTrue, ReadOnlySpan<double>
  yScore, int classCount, MultiClassStrategy strategy =
  MultiClassStrategy.OneVsRest, Averaging average = Averaging.Macro,
  ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
  -> double`.

**scikit-learn's rules, which differ from `confusion_matrix`'s:**

- `labels` must be **sorted and unique** here (`confusion_matrix` accepts any
  order). Omitted, it is the sorted unique set of `yTrue`.
- `average` must be `Macro` or `Weighted`. `Micro` and `Binary` raise.
- Score rows must sum to 1 within `atol=1e-8, rtol=1e-5` — NumPy's `allclose`
  defaults, applied as `|sum - 1| <= 1e-8 + 1e-5 * |sum|`.
- `OneVsOne` with `sampleWeight` raises. scikit-learn refuses it outright.
- `OneVsOne` + `Weighted` is Hand & Till's weighted variant: each pair is
  weighted by the share of samples belonging to that pair.

- [ ] **Step 1: Write the failing test**

`tests/DataNet.Metrics.Tests/RocAucMultiClassTests.cs`:

```csharp
using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class RocAucMultiClassTests
{
    [Theory]
    [MemberData(nameof(RocCorpus.MulticlassIndices), MemberType = typeof(RocCorpus))]
    public void Matches_sklearn_multiclass_roc_auc_score(int index)
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

            double actual = RocAuc.MultiClass(
                yTrue, scores, classCount, strategy, average, default, weight);

            Assert.True(Math.Abs(entry.Value.GetDouble() - actual) < MetricsCorpus.Tolerance,
                $"{RocCorpus.Describe(c)} {entry.Name}: expected {entry.Value.GetDouble()}, got {actual}");
        }
    }

    [Fact]
    public void Rejects_one_vs_one_with_sample_weights_as_sklearn_does()
    {
        int[] yTrue = [0, 1, 2, 0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.2], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6],
                                [0.5, 0.3, 0.2], [0.3, 0.5, 0.2], [0.1, 0.2, 0.7]]);
        double[] weight = [1, 2, 1, 1, 2, 1];

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(
            yTrue, scores, 3, MultiClassStrategy.OneVsOne, Averaging.Macro, default, weight));
    }

    [Fact]
    public void Rejects_rows_that_do_not_sum_to_one()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.1], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6]]);

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(yTrue, scores, 3));
    }

    [Fact]
    public void Rejects_a_span_whose_length_is_not_a_multiple_of_the_class_count()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = [0.5, 0.5, 0.5, 0.5];

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(yTrue, scores, 3));
    }

    [Fact]
    public void Rejects_micro_averaging()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.2], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6]]);

        Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, 3, MultiClassStrategy.OneVsRest, Averaging.Micro));
    }

    [Fact]
    public void Rejects_unsorted_labels()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.2], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6]]);
        int[] labels = [2, 0, 1];

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(
            yTrue, scores, 3, MultiClassStrategy.OneVsRest, Averaging.Macro, labels));
    }

    private static double[] Rows(double[][] rows) => [.. rows.SelectMany(r => r)];
}
```

- [ ] **Step 2: Run it and watch it fail**

```bash
dotnet test tests/DataNet.Metrics.Tests -c Release --filter "FullyQualifiedName~RocAucMultiClassTests"
```

Expected: FAIL — `'RocAuc' does not contain a definition for 'MultiClass'`.

- [ ] **Step 3: Implement both strategies**

`src/DataNet.Metrics/Internal/MultiClassRoc.cs`:

```csharp
namespace DataNet.Metrics.Internal;

/// <summary>
/// Multiclass ROC-AUC by reduction to binary problems — scikit-learn's
/// <c>multi_class="ovr"</c> and <c>multi_class="ovo"</c>.
/// </summary>
internal static class MultiClassRoc
{
    // NumPy's allclose defaults, which is the comparison sklearn makes.
    private const double RelativeTolerance = 1e-5;
    private const double AbsoluteTolerance = 1e-8;

    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int classCount,
        MultiClassStrategy strategy,
        Averaging average,
        ReadOnlySpan<int> labels,
        ReadOnlySpan<double> sampleWeight)
    {
        int n = Validate(yTrue, yScore, classCount, strategy, average, sampleWeight);
        int[] classes = ResolveLabels(yTrue, labels, classCount);
        ValidateRowSums(yScore, n, classCount);

        return strategy == MultiClassStrategy.OneVsRest
            ? OneVsRest(yTrue, yScore, classes, average, sampleWeight)
            : OneVsOne(yTrue, yScore, classes, average);
    }

    private static int Validate(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int classCount,
        MultiClassStrategy strategy, Averaging average, ReadOnlySpan<double> sampleWeight)
    {
        int n = yTrue.Length;
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
                nameof(average));
        }
        if (!sampleWeight.IsEmpty)
        {
            if (sampleWeight.Length != n)
            {
                throw new ArgumentException(
                    $"sampleWeight has {sampleWeight.Length} entries but there are {n} samples.",
                    nameof(sampleWeight));
            }
            if (strategy == MultiClassStrategy.OneVsOne)
            {
                throw new ArgumentException(
                    "scikit-learn does not support sampleWeight for one-vs-one ROC AUC, and neither does this.",
                    nameof(sampleWeight));
            }
        }

        return n;
    }

    private static int[] ResolveLabels(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> labels, int classCount)
    {
        if (labels.IsEmpty)
        {
            var seen = new SortedSet<int>();
            foreach (int label in yTrue)
            {
                seen.Add(label);
            }
            if (seen.Count != classCount)
            {
                throw new ArgumentException(
                    $"yTrue holds {seen.Count} distinct labels but classCount is {classCount}. "
                    + "Pass labels when a class is absent from yTrue.",
                    nameof(classCount));
            }
            int[] resolved = new int[seen.Count];
            seen.CopyTo(resolved);
            return resolved;
        }

        if (labels.Length != classCount)
        {
            throw new ArgumentException(
                $"labels has {labels.Length} entries but classCount is {classCount}.", nameof(labels));
        }
        for (int i = 1; i < labels.Length; i++)
        {
            if (labels[i] <= labels[i - 1])
            {
                throw new ArgumentException(
                    "labels must be sorted ascending and unique for multiclass ROC AUC, as scikit-learn requires.",
                    nameof(labels));
            }
        }
        return labels.ToArray();
    }

    private static void ValidateRowSums(ReadOnlySpan<double> yScore, int n, int classCount)
    {
        for (int i = 0; i < n; i++)
        {
            double sum = 0.0;
            int offset = i * classCount;
            for (int c = 0; c < classCount; c++)
            {
                sum += yScore[offset + c];
            }

            if (Math.Abs(sum - 1.0) > AbsoluteTolerance + (RelativeTolerance * Math.Abs(sum)))
            {
                throw new ArgumentException(
                    $"yScore row {i} sums to {sum}; multiclass ROC AUC needs probabilities that sum to 1.",
                    nameof(yScore));
            }
        }
    }

    private static double OneVsRest(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes,
        Averaging average, ReadOnlySpan<double> sampleWeight)
    {
        int n = yTrue.Length;
        int k = classes.Length;
        int[] binary = new int[n];
        double[] column = new double[n];
        double[] scores = new double[k];
        double[] weights = new double[k];
        bool weighted = !sampleWeight.IsEmpty;

        for (int c = 0; c < k; c++)
        {
            double positiveWeight = 0.0;
            for (int i = 0; i < n; i++)
            {
                bool positive = yTrue[i] == classes[c];
                binary[i] = positive ? 1 : 0;
                column[i] = yScore[(i * k) + c];
                if (positive)
                {
                    positiveWeight += weighted ? sampleWeight[i] : 1.0;
                }
            }

            scores[c] = BinaryRoc.Score(binary, column, 1, sampleWeight);
            weights[c] = positiveWeight;
        }

        return average == Averaging.Macro ? Mean(scores) : WeightedMean(scores, weights);
    }

    private static double OneVsOne(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes, Averaging average)
    {
        int n = yTrue.Length;
        int k = classes.Length;
        int pairCount = k * (k - 1) / 2;
        double[] pairScores = new double[pairCount];
        double[] prevalence = new double[pairCount];
        int[] binary = new int[n];
        double[] column = new double[n];
        int pair = 0;

        for (int a = 0; a < k; a++)
        {
            for (int b = a + 1; b < k; b++)
            {
                int size = 0;
                for (int i = 0; i < n; i++)
                {
                    if (yTrue[i] == classes[a] || yTrue[i] == classes[b])
                    {
                        size++;
                    }
                }

                // Hand & Till: each ordering of the pair is scored with its own
                // column, and the two are averaged.
                double aScore = PairScore(yTrue, yScore, classes, k, a, b, a, binary, column, size);
                double bScore = PairScore(yTrue, yScore, classes, k, a, b, b, binary, column, size);

                pairScores[pair] = (aScore + bScore) * 0.5;
                prevalence[pair] = (double)size / n;
                pair++;
            }
        }

        return average == Averaging.Macro ? Mean(pairScores) : WeightedMean(pairScores, prevalence);
    }

    private static double PairScore(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int[] classes, int k,
        int a, int b, int positiveClass, int[] binary, double[] column, int size)
    {
        int next = 0;
        for (int i = 0; i < yTrue.Length; i++)
        {
            if (yTrue[i] != classes[a] && yTrue[i] != classes[b])
            {
                continue;
            }

            binary[next] = yTrue[i] == classes[positiveClass] ? 1 : 0;
            column[next] = yScore[(i * k) + positiveClass];
            next++;
        }

        return BinaryRoc.Score(
            binary.AsSpan(0, size), column.AsSpan(0, size), 1, default);
    }

    private static double Mean(double[] values)
    {
        double total = 0.0;
        foreach (double value in values)
        {
            total += value;
        }
        return total / values.Length;
    }

    private static double WeightedMean(double[] values, double[] weights)
    {
        double total = 0.0;
        double weightSum = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            total += values[i] * weights[i];
            weightSum += weights[i];
        }
        return total / weightSum;
    }
}
```

- [ ] **Step 4: Add the public entry point**

Add to `src/DataNet.Metrics/RocAuc.cs`:

```csharp
    /// <summary>
    /// The multiclass case —
    /// <c>roc_auc_score(y_true, y_score, multi_class=…, average=…, labels=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true labels, one per sample.</param>
    /// <param name="yScore">Class probabilities, row-major: sample 0's classes, then sample 1's. Length must be <paramref name="classCount"/> times the sample count, and each row must sum to 1.</param>
    /// <param name="classCount">How many classes each row scores.</param>
    /// <param name="strategy">One-vs-rest or one-vs-one (Hand &amp; Till).</param>
    /// <param name="average">Only <see cref="Averaging.Macro"/> and <see cref="Averaging.Weighted"/>, as scikit-learn allows.</param>
    /// <param name="labels">The classes the columns stand for, sorted ascending and unique. Omit for the sorted distinct labels of <paramref name="yTrue"/>.</param>
    /// <param name="sampleWeight">A weight per sample. Not supported with <see cref="MultiClassStrategy.OneVsOne"/>, which scikit-learn also refuses.</param>
    /// <exception cref="ArgumentException">Any of the rules above is broken.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="classCount"/> is below two.</exception>
    public static double MultiClass(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int classCount,
        MultiClassStrategy strategy = MultiClassStrategy.OneVsRest,
        Averaging average = Averaging.Macro,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        MultiClassRoc.Score(yTrue, yScore, classCount, strategy, average, labels, sampleWeight);
```

- [ ] **Step 5: Run green and commit**

```bash
dotnet test tests/DataNet.Metrics.Tests -c Release --filter "FullyQualifiedName~RocAucMultiClassTests"
dotnet build DataNet.slnx -c Release && dotnet test DataNet.slnx -c Release
git add src/DataNet.Metrics tests/DataNet.Metrics.Tests
git commit -F - <<'EOF'
Reduce multiclass ROC to the binary curve, two ways

One-vs-rest binarises each class and averages, weighting by class prevalence
when asked. One-vs-one is Hand & Till: each pair is scored twice, once from
each class's own column, and the two are averaged — not a single score per
pair, which is the shortcut that reads plausibly and is wrong.

The validation is scikit-learn's, and it differs from confusion_matrix's in
ways worth stating: labels must be sorted and unique here, only macro and
weighted averaging exist, rows must sum to 1 within numpy's allclose defaults,
and sample weights are refused outright for one-vs-one.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 10: Benchmarks, and the merge gate

**Files:**

- Create: `bench/corpus/generate_metrics.py`
- Create: `bench/DataNet.Text.Benchmarks/MetricsBenchmarks.cs`
- Create: `bench/DataNet.Text.Benchmarks/CrossLang/MetricsCrossLang.cs`
- Create: `bench/python/bench_metrics.py`
- Modify: `bench/DataNet.Text.Benchmarks/DataNet.Text.Benchmarks.csproj` (reference
  `DataNet.Metrics`), `Program.cs` (a `compare-metrics` verb)
- Modify: `bench/compare.py` (a `metrics` mode)
- Modify: `bench/README.md` (a fifth section)
- Modify: `.gitignore` (`bench/corpus/metrics/`)

**Interfaces:**

- Consumes: the whole public API from Tasks 4-9.
- Produces: `bench/results/csharp-metrics.json` and `python-metrics.json` with
  the same `{metadata, results:[{operation, ms, cpu_ms}]}` shape the persistence
  harness already writes, and a table in `docs/guides/performance.md`.

**Operations measured, identical on both sides:** `confusion_matrix`,
`accuracy`, `precision_recall_f1_macro`, `classification_report`,
`roc_auc_binary`, `roc_auc_ovr_macro`.

- [ ] **Step 1: Read the two harnesses this one must mirror**

```bash
sed -n '1,200p' bench/DataNet.Text.Benchmarks/CrossLang/PersistenceCrossLang.cs
sed -n '1,200p' bench/python/bench_persistence.py
sed -n '1,200p' bench/compare.py
```

The new harness copies their methodology exactly — same minimum time, same
repeat count, same best-of-N, wall **and** processor time on both sides. Extract
the timing loop from `PersistenceCrossLang` into
`bench/DataNet.Text.Benchmarks/CrossLang/Harness.cs` and have both call it rather
than writing a second one that drifts.

- [ ] **Step 2: Write the corpus generator**

`bench/corpus/generate_metrics.py`:

```python
#!/usr/bin/env python3
"""Generate the benchmark corpus for DataNet.Metrics (issue #61).

Written rather than committed, like bench/corpus/vocabs: both language sides
read these same files, which is what makes the comparison mean anything, and the
bytes do not need to be reproducible across machines.

    python bench/corpus/generate_metrics.py
"""

from __future__ import annotations

import json
import math
import random
from pathlib import Path

SEED = 20260806
OUT = Path(__file__).resolve().parent / "metrics"

# (samples, classes). The 10-class score matrix is only generated up to 100_000
# rows: a million rows by ten classes is 200 MB of JSON, which measures the disk
# rather than the metric.
SHAPES = [(1_000, 2), (1_000, 10), (100_000, 2), (100_000, 10), (1_000_000, 2), (1_000_000, 10)]
SCORE_LIMIT = 100_000


def softmax(row: list[float]) -> list[float]:
    top = max(row)
    exps = [math.exp(v - top) for v in row]
    total = sum(exps)
    return [v / total for v in exps]


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    rng = random.Random(SEED)

    for n, k in SHAPES:
        y_true = [rng.randrange(k) for _ in range(n)]
        y_pred = [t if rng.random() < 0.7 else rng.randrange(k) for t in y_true]
        payload = {
            "samples": n,
            "classes": k,
            "y_true": y_true,
            "y_pred": y_pred,
            "sample_weight": [round(rng.uniform(0.1, 3.0), 3) for _ in range(n)],
            "binary_scores": [round(rng.random() * 0.6 + (0.4 if t == 1 else 0.0), 9)
                              for t in y_true] if k == 2 else None,
            "scores": None,
        }
        if k > 2 and n <= SCORE_LIMIT:
            rows = []
            for t in y_true:
                logits = [rng.gauss(0.0, 1.0) for _ in range(k)]
                logits[t] += 1.5
                rows.append([round(v, 9) for v in softmax(logits)])
            payload["scores"] = rows

        path = OUT / f"metrics_n{n}_k{k}.json"
        with path.open("w", encoding="utf-8") as f:
            json.dump(payload, f)
        print(f"{path.name}: {n} samples, {k} classes")


if __name__ == "__main__":
    main()
```

Add to `.gitignore`:

```gitignore
bench/corpus/metrics/
```

- [ ] **Step 3: Generate it, and check the size before committing to it**

```bash
python3 bench/corpus/generate_metrics.py
du -sh bench/corpus/metrics/
```

Expected: six files, well under 500 MB in total. If the 10-class 100 000-row file
alone is over 100 MB, lower `SCORE_LIMIT` to 50 000 and regenerate — the
comparison is about the metric, not the parser.

- [ ] **Step 4: Write the intra-C# benchmarks**

`bench/DataNet.Text.Benchmarks/MetricsBenchmarks.cs`:

```csharp
using BenchmarkDotNet.Attributes;
using DataNet.Metrics;

namespace DataNet.Text.Benchmarks;

/// <summary>
/// Per-metric cost at three sizes and two class counts. The matrix is built
/// inside each benchmark rather than in the setup, because that is what a caller
/// pays for a single scalar call — the amortised path is ClassificationReport.
/// </summary>
[MemoryDiagnoser]
public class MetricsBenchmarks
{
    private int[] _yTrue = [];
    private int[] _yPred = [];
    private double[] _weight = [];

    [Params(1_000, 100_000, 1_000_000)]
    public int Samples { get; set; }

    [Params(2, 10)]
    public int Classes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(20260806);
        _yTrue = new int[Samples];
        _yPred = new int[Samples];
        _weight = new double[Samples];
        for (int i = 0; i < Samples; i++)
        {
            _yTrue[i] = rng.Next(Classes);
            _yPred[i] = rng.NextDouble() < 0.7 ? _yTrue[i] : rng.Next(Classes);
            _weight[i] = (rng.NextDouble() * 2.9) + 0.1;
        }
    }

    [Benchmark]
    public ConfusionMatrix Matrix() => ConfusionMatrix.Compute(_yTrue, _yPred);

    [Benchmark]
    public ConfusionMatrix MatrixWeighted() =>
        ConfusionMatrix.Compute(_yTrue, _yPred, default, _weight);

    [Benchmark]
    public double AccuracyScore() => Accuracy.Score(_yTrue, _yPred);

    [Benchmark]
    public double F1Macro() => F1.Score(_yTrue, _yPred, Averaging.Macro);

    [Benchmark]
    public string Report() => ClassificationReport.Compute(_yTrue, _yPred).ToText();
}
```

- [ ] **Step 5: Run the intra-C# suite on both targets**

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks        -- --filter '*Metrics*'
dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter '*Metrics*'
```

The netstandard project links the same sources, so no second file is needed.
Confirm the header prints `// DataNet.Metrics: .NETStandard,Version=v2.0` for the
second run — if it says `.NETCoreApp`, the isolation broke and the numbers are
about the wrong assembly.

- [ ] **Step 6: Write the two cross-language harnesses**

`bench/DataNet.Text.Benchmarks/CrossLang/MetricsCrossLang.cs` measures the six
operations named above over each corpus file, writing
`bench/results/csharp-metrics.json` through the shared harness from Step 1. The
`bench/python/bench_metrics.py` mirror calls, in the same order and over the same
files:

```python
skm.confusion_matrix(y_true, y_pred)
skm.accuracy_score(y_true, y_pred)
skm.precision_recall_fscore_support(y_true, y_pred, average="macro", zero_division=0)
skm.classification_report(y_true, y_pred, zero_division=0)
skm.roc_auc_score(y_true, binary_scores)                       # k == 2 files
skm.roc_auc_score(y_true, scores, multi_class="ovr", average="macro")   # k == 10 files
```

Each side records elapsed **and** processor time per operation — on the C# side
`Stopwatch` plus `Process.GetCurrentProcess().TotalProcessorTime`, on the Python
side `time.perf_counter` plus `time.process_time`, exactly as
`bench_persistence.py` already does.

- [ ] **Step 7: Run both sides back to back on an idle machine**

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- compare-metrics
. .venv-oracles/bin/activate && python bench/python/bench_metrics.py
python bench/compare.py metrics
```

Both sides in one sitting, not the best figure of each across several runs —
picking per-row winners from different runs flatters whichever was measured last.

- [ ] **Step 8: Apply the gate**

Read the **cpu** column. The merge gate is `≥ 1×` on every operation at every
size. For any row below 1×:

1. Profile before changing anything. The likely candidate is `roc_auc_binary` at
   a million samples, where the cost is the sort.
2. If it is the sort, the option on the table is a radix pass over the `double`
   bit patterns (flip the sign bit, invert negatives, sort as `ulong`).
3. If a row still cannot be won, **stop and report it** with the numbers. The
   spec names ROC-AUC as the piece whose removal leaves a coherent whole; that
   decision is the maintainer's, not this plan's.

- [ ] **Step 9: Capture the numbers and commit**

Add the cross-language table to `docs/guides/performance.md` and a fifth section
to `bench/README.md` naming the corpus generator, the two commands, and the
machine the numbers came from.

```bash
git add bench docs/guides/performance.md .gitignore
git commit -F - <<'EOF'
Measure the metrics against scikit-learn, on the axis that does not flatter us

Elapsed time understates what .NET costs: background collection runs on other
threads, so an allocating operation finishes sooner than it is paid for, while
CPython measures 1.00 processor-seconds per elapsed second on every row. The
gate for this branch is therefore processor time, and both columns are
reported.

Same corpus files on both sides, same best-of-N methodology, both sides run
back to back rather than cherry-picked across runs.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 11: The metrics lot in the sample, and the reachability gate

**Rewritten after rebasing onto `origin/main`.** The original task created a
separate `samples/DataNet.Metrics.Sample`, on the reasoning that `DataNet.Sample`
was deliberately thin — "one thing per lot" — and should not be widened. That
reasoning is void: main has split the sample into `Lot1Distances.cs` …
`Lot4Fuzzy.cs` and added `PackagingGate.cs`, which reflects over the **packaged**
assemblies and fails the build when any exported public type is not exercised
from the sample. Covering the surface is now compulsory rather than optional, and
the house pattern is a lot file. A second sample project would sit outside that
gate and leave `DataNet.Metrics` the only package whose surface nothing checks.

**Files:**

- Create: `samples/DataNet.Sample/Lot5Metrics.cs`
- Modify: `samples/DataNet.Sample/Program.cs` (call the new lot)
- Modify: `samples/DataNet.Sample/PackagingGate.cs` (add the assembly)
- Modify: `samples/DataNet.Sample/DataNet.Sample.csproj` (import the version, add
  the `PackageReference`)

**Interfaces:**

- Consumes: every public type of `DataNet.Metrics`, **as a NuGet package**, never
  a project reference.
- Produces: a sample run that exits 0, and a gate that accounts for every
  exported type.

- [ ] **Step 1: Read what the gate demands before writing anything**

```bash
sed -n '1,200p' samples/DataNet.Sample/PackagingGate.cs
sed -n '1,80p' samples/DataNet.Sample/Lot2Vectorization.cs
cat samples/DataNet.Sample/Program.cs
cat samples/DataNet.Sample/DataNet.Sample.csproj
```

Two details of the gate decide whether your lot passes:

- the criterion is a **`MemberReference`**, not a `TypeReference` — `typeof(T)`
  alone does not count as exercising a type; you must call something on it;
- **enums are the documented exception**, because an enum member is a
  compile-time constant and emits no member reference. Naming one is all a
  consumer can do, and all the gate asks.

- [ ] **Step 2: Wire the package into the sample**

Mirror how the other three packages are referenced: import
`../../src/DataNet.Metrics/Version.props` and add
`<PackageReference Include="DataNet.Metrics" Version="$(DataNetMetricsVersion)" />`.
The sample restores from `../artifacts` through `samples/NuGet.config`, which
already maps `DataNet.*` to the local feed, so nothing else is needed.

- [ ] **Step 3: Write `Lot5Metrics.cs`**

Follow the shape of the existing lot files (a `static class` with a method
`Program.cs` calls, printing a heading then its lines). It must exercise, in this
order: a confusion matrix and its `ToArray()`; accuracy, normalized and not;
precision, recall and F1 in **all four** averaging modes on the same data, so a
reader sees macro, micro and weighted disagree; `PerClass` on each of the three;
`FBeta` at β = 0.5 and 2; the absent-class case under all four `ZeroDivision`
values, with `Throw` caught and its message printed; a weighted run; the
report's structured rows **and** its `ToText()`; and ROC-AUC binary, `ovr` and
`ovo`.

That list is not decoration — it is what makes the gate pass. Every public type
must receive a real call: `ConfusionMatrix`, `Accuracy`, `Precision`, `Recall`,
`F1`, `FBeta`, `ClassificationReport`, `ClassRow`, `AverageRow`, `RocAuc`,
`UndefinedMetricException` (name it in a `catch`), and the three enums
`Averaging`, `ZeroDivision`, `MultiClassStrategy` (naming a member suffices).

- [ ] **Step 4: Add the assembly to the gate**

In `PackagingGate.Verify()`, add the `DataNet.Metrics` assembly to the `packaged`
array, reached through a type the sample genuinely uses (for example
`typeof(ConfusionMatrix).Assembly`). Add nothing to `Excluded` — every metrics
type is exercisable, and an exclusion needs a reason a reviewer can disagree
with.

- [ ] **Step 5: Prove it, against the packages rather than the projects**

```bash
rm -rf ./artifacts
for proj in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do
  dotnet pack "$proj" -c Release -o ./artifacts
done
NUGET_PACKAGES="$(mktemp -d)" dotnet run --project samples/DataNet.Sample -c Release
echo "exit=$?"
```

Expected: `exit=0`, the metrics lot printed, and the gate reporting every
exported type accounted for. The scratch `NUGET_PACKAGES` matters: the global
folder is consulted ahead of every source, so without it a stale twin could
satisfy the restore.

Then make the gate prove it can still fail: comment out one call in
`Lot5Metrics.cs` — say the `FBeta` one — re-run, and watch it name that type as
unreachable. Put the call back. A gate nobody has watched fail is
indistinguishable from one that cannot.

- [ ] **Step 6: Commit**

`ci.yml` needs no change: the sample job already runs `samples/DataNet.Sample`,
and the pack loops already include `src/DataNet.Metrics`.

```bash
git add samples/DataNet.Sample
git commit -F - <<'EOF'
Bring the metrics under the gate that counts public types

main turned the sample into a reachability check: it reflects over the packaged
assemblies and fails when an exported type is not exercised. A separate metrics
sample would have sat outside that check and left the new package the only one
whose surface nothing verifies — so the metrics get a lot file like every other
package, and the gate gets a fourth assembly.

The lot shows the three averages disagreeing on the same data and every
zero-division mode, which is the thing the migration guide is about to claim.
The gate was watched failing on a removed call before being left passing.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
```

---


### Task 12: The documentation the issue actually asks for

**Files:**

- Create: `docs/decisions/0015-metrics-package-placement.md`
- Modify: `docs/equivalence.md` (new section at the end)
- Modify: `docs/migration/sklearn.md:34-35` (the "Metrics" bullet)
- Modify: `docs/migration/README.md:19` (the scikit-learn row) and the
  "What DataNet writes natively" list
- Modify: `README.md` (package table), `CHANGELOG.md` (a `DataNet.Metrics 0.1.0`
  heading)

- [ ] **Step 1: Write decision 0015**

Follow the shape of `docs/decisions/0011-persistence-format.md`. It must answer:
why a separate package rather than `DataNet.Text` (metrics are not textual;
extraction later would be breaking); why the confusion matrix is public (callers
who want several metrics should count once, and the type is in the issue's scope
anyway); why `sample_weight` is supported from the start even though it forces
`double` counts; and why `Averaging.None` became `PerClass`.

- [ ] **Step 2: Add the equivalence rows**

Append to `docs/equivalence.md`, matching the existing table style:

```markdown
## DataNet.Metrics — classification metrics

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
| `accuracy_score(y_true, y_pred)` | scikit-learn | `Accuracy.Score(yTrue, yPred)` | Identical, `normalize` included. The overload taking a `ConfusionMatrix` scores only the samples that matrix kept. |
| `confusion_matrix(y_true, y_pred, labels=…)` | scikit-learn | `ConfusionMatrix.Compute(…)` | Rows are true labels. Label order is the sorted union, or the caller's order left unsorted. Counts are `double` because `sampleWeight` is supported. |
| `precision_score(…, average=…)` | scikit-learn | `Precision.Score(…, Averaging…)` | All four modes. `average=None` is `Precision.PerClass`. |
| `recall_score(…, average=…)` | scikit-learn | `Recall.Score(…, Averaging…)` | As above. |
| `f1_score(…, average=…)` | scikit-learn | `F1.Score(…, Averaging…)` | As above. |
| `fbeta_score(…, beta=…)` | scikit-learn | `FBeta.Score(…, beta, …)` | Finite `beta ≥ 0`; scikit-learn also accepts `inf`, which throws here. |
| `classification_report(…)` | scikit-learn | `ClassificationReport.Compute(…)`, `.ToText(digits)` | Structured *and* character-exact text. `ZeroDivision.NaN` renders `NaN` where Python writes `nan`; the numbers still match. |
| `zero_division=0/1/np.nan` | scikit-learn | `ZeroDivision.Zero/One/NaN` | Values identical. The `UndefinedMetricWarning` has no equivalent; `ZeroDivision.Throw` is the opt-in replacement. |
| `roc_auc_score(y_true, y_score)` | scikit-learn | `RocAuc.Score(…)` | Binary. `posLabel` is explicit here (default 1) where scikit-learn infers it. |
| `roc_auc_score(…, multi_class=…)` | scikit-learn | `RocAuc.MultiClass(…)` | `ovr` and `ovo`. Separate method: the overloads would be ambiguous. `sampleWeight` refused for `ovo`, as in scikit-learn. |
```

- [ ] **Step 3: Rewrite the metrics pitfall in the migration guide**

Replace the two-line bullet at `docs/migration/sklearn.md:34-35` with a section
that explains macro, micro and weighted **once, properly**, and carries a worked
example where the three differ — use the `binary_imbalanced` fixture's shape (190
samples of class 0, 10 of class 1) and print the three numbers from the sample.
Then point at `DataNet.Metrics` and at the equivalence table. Verify the numbers
you quote by running them; do not write plausible ones.

- [ ] **Step 4: Move the inventory row**

In `docs/migration/README.md`, the scikit-learn row's verdict becomes
`✅ **Use** *except* text vectorization → **DataNet.Text** and classification
metrics → **DataNet.Metrics**`, and the "What DataNet writes natively" list gains
a fifth entry: **Classification metrics** — sklearn-parity precision, recall, F1,
confusion matrix, report and ROC-AUC. *(done)*

- [ ] **Step 5: Lint and commit**

```bash
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
git add docs README.md CHANGELOG.md
git commit -F - <<'EOF'
Answer the question the migration guide was only asking

"Check the definitions (macro/micro averaging, handling of absent classes)"
named the trap and left the reader in it. The inventory's whole job is that a
row points either at an existing .NET building block or at something DataNet
builds; metrics did neither.

They now point at DataNet.Metrics, the three averages are explained once with
an example where they actually disagree, and every function has a row in the
equivalence table naming its sklearn call and its deliberate divergences.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 13: Follow-ups, and the pull request

- [ ] **Step 1: Run everything one last time**

```bash
dotnet build DataNet.slnx -c Release
dotnet test DataNet.slnx -c Release
python3 tools/check_version_floor.py
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
cd /tmp && PYTHONSAFEPATH=1 "$OLDPWD/.venv-oracles/bin/python" "$OLDPWD/tools/generate_oracles.py" && cd - && git diff --stat tests/oracles/
```

Expected: build clean, all tests pass, version floor fine, markdown clean, and an
**empty** oracle diff. Do not run `dotnet format` — it crashes on this machine;
the `Lint` job decides.

- [ ] **Step 2: Open the follow-up issues**

```bash
gh issue create --title "TokenizationResult compares by reference, not by value" --body "$(cat <<'EOF'
`TokenizationResult` in `src/DataNet.Embeddings/Tokenization/WordPieceTokenizer.cs`
is a `record` over two `IReadOnlyList<T>` members. The synthesised `Equals`
compares those members by reference, so two tokenizations with identical tokens
and ids are not equal — the opposite of what a `record` advertises.

Surfaced while designing `DataNet.Metrics`, where `ClassificationReport` was made
a plain class for exactly this reason.

Either implement structural equality explicitly, or make it a class so nothing is
promised.
EOF
)"

gh issue create --title "Regression metrics in DataNet.Metrics" --body "MSE, MAE, R² and friends, at scikit-learn parity, as a second lot in DataNet.Metrics. Deliberately left out of #61 under the one-branch-one-concern rule."

gh issue create --title "The remaining sklearn classification metrics" --body "balanced_accuracy_score, matthews_corrcoef, cohen_kappa_score, and normalize= on confusion_matrix. Left out of #61 as out of scope."
```

- [ ] **Step 3: Push and open the pull request**

```bash
git push -u origin feat/61-classification-metrics
gh pr create --title "Classification metrics at scikit-learn parity" --body "$(cat <<'EOF'
Closes #61.

A new dependency-free `DataNet.Metrics` package: accuracy, precision, recall,
F1, FBeta, confusion matrix, classification report and ROC-AUC, at exact
scikit-learn parity, with `sample_weight` throughout and multiclass ROC-AUC in
both `ovr` and `ovo`.

## What proves it

Two frozen corpora, twelve fixtures each emitted weighted and unweighted, all
four averaging modes, both zero-division values, and the report text at two
digit settings compared **character for character**. The fixtures are chosen for
where implementations drift: a class never predicted, a class absent from the
truth, a `labels=` subset that drops samples, and label values that are not
`0..k-1`.

## Performance

The merge gate for this branch was processor time against scikit-learn, on every
operation at every size. Numbers below, both sides run back to back on an
otherwise idle machine.

<!-- paste the table from docs/guides/performance.md -->

## Decisions worth reviewing

- `docs/decisions/0015-metrics-package-placement.md` — why a fourth package.
- `Averaging.None` became `PerClass`: a `Score` returning `double` cannot also
  return an array.
- `RocAuc.Score` and `RocAuc.MultiClass` are separate names because the
  overloads would be ambiguous at the call site.
- `ClassificationReport` is a class, not a record: value equality over a list of
  computed doubles would be a promise it cannot keep.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

- [ ] **Step 4: Report, do not merge**

The maintainer merges. Report the PR URL, the performance table, and anything the
gate forced a decision on.

---

## Self-review

**Spec coverage.** Package and plumbing → Tasks 1-2. Public API → Tasks 4-9.
Parity semantics (label order, `_prf_divide`, micro-equals-accuracy, the micro-avg
row, ROC mechanics, `ovr`, `ovo`) → Tasks 4-9, each with its corpus assertion.
Errors → the validation tests in Tasks 4, 5, 8, 9. Oracle corpus → Task 3.
Tests → every task. Performance → Task 10. Documentation → Task 12. Out of scope
and follow-ups → Task 13.

**Two refinements the spec does not carry**, both recorded where they arise:

1. `AverageRow` as a distinct record, and a nullable `MicroAverage` (Task 6).
2. ROC-AUC raises when only one class is present in `yTrue` — a scikit-learn
   error the spec's list missed (Tasks 8-9).

**Type consistency.** `ConfusionMatrix.Compute` keeps the same parameter order
everywhere (`yTrue, yPred, labels, sampleWeight`); `Prf.Aggregate` is called with
`(cm, metric, beta, average, posLabel, zeroDivision)` in Tasks 5 and 6 alike;
`MetricsCorpus.Tolerance` is the single tolerance constant, used by the ROC tests
too.
