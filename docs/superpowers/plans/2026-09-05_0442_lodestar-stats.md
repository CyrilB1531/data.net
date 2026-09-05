# `Lodestar.Stats` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship `Lodestar.Stats` 0.1.0 — ten families of hypothesis tests at
`scipy.stats` 1.18.0 parity, core tier, on `net10.0` and `netstandard2.0`.

**Architecture:** Static classes, arrays in and numbers out, exactly as
`Lodestar.Metrics` is shaped: nothing is fitted, so nothing is held between two
calls. Every tail probability comes from one `internal` numerical layer under
`Internal/` — a log-gamma, two regularized incomplete functions, a complementary
error function and a Kolmogorov distribution — written from published
mathematics rather than transcribed. Every family replays a frozen `scipy`
corpus in which each case carries the *full* argument set, never a default.

**Tech Stack:** C# on `net10.0;netstandard2.0`, xunit, `scipy 1.18.0` /
`numpy 2.5.1` in `.venv-oracles` as the oracle, `Accord.Statistics` 3.8.0 in
`bench/` as the incumbent.

**Spec:** `docs/superpowers/specs/2026-09-05_0442_lodestar-stats-hypothesis-tests.md`

**Branch:** `feat/442-lodestar-stats` (already created; the spec is committed on
it as `9c2bcfc`).

## Global Constraints

- **Never transcribe a reference implementation.** ADR 0003. The Lanczos
  approximation, modified Lentz continued fractions and Royston's AS R94 are
  *published mathematics* and may be written from their descriptions. Numerical
  Recipes' code, `scipy`'s Cython, `Accord`'s C# and `boost::math`'s C++ are
  implementations and may be **read only to diagnose a single failing case**.
- **Core tier: no external dependency.** `tools/check_nuspec_dependencies.py`
  fails the build, not a review. Decision 0076.
- **One public API across both target frameworks.** `netstandard2.0` reaches the
  same behaviour by conditional compilation, never by a reduced surface. Gaps
  close in the order PolySharp → `System.Memory` / `System.Numerics.Vectors` →
  hand-written fallback.
- **`src/` references published packages, never projects.** `Lodestar.Stats` has
  no sibling edge, so it adds **no** entry to `src/Directory.Packages.props`.
- **Version 0.1.0**, owned in `src/Lodestar.Stats/Version.props` alone. Decision 0012.
- **Warnings are errors**, `AnalysisMode=All`, `AnalysisLevel=10.0`. Clear Sonar
  findings before committing, not after.
- **Comments say why, not what.** Two lines inline, eight lines of prose in XML
  docs. `tools/check_comment_length.py` counts them.
- **Run the oracle generator from a directory that is not an ancestor of the
  checkout**, and read the generator's **own** exit code, never a pipeline's.
  Every command below takes the checkout from `git rev-parse --show-toplevel`
  into `$REPO` rather than writing a path down; `check_machine_paths.py` fails a
  tracked file that holds one. Check where `$SCRATCH` actually lands before
  trusting it: a hosted session can put the worktree under `/tmp` itself, and
  `/var/tmp` serves when it does.
- **Read the test count, not the colour.** A `--filter` matching nothing exits zero.
- **Tolerances:** statistics at `1e-9` absolute; **p-values at `1e-9` relative**.
  `tools/compare_oracles.py` is not modified.
- Everything written in English. Commit messages carry no `feat:`/`fix:` prefix.
  Do not merge or tag. Do not open a pull request unless asked.

## File Structure

| file | responsibility |
| --- | --- |
| `src/Lodestar.Stats/Lodestar.Stats.csproj`, `Version.props` | the package, its id, its description, its version |
| `src/Lodestar.Stats/TestResult.cs` | `TestResult`, `TTestResult`, `Chi2ContingencyResult`, `KsResult` — the four result shapes |
| `src/Lodestar.Stats/Options.cs` | the five enums: `Alternative`, `Variance`, `Continuity`, `ExactMethod`, `ZeroMethod` |
| `src/Lodestar.Stats/Internal/Gamma.cs` | `LogGamma`, `RegularizedIncompleteGammaP`, `RegularizedIncompleteGammaQ` |
| `src/Lodestar.Stats/Internal/Beta.cs` | `RegularizedIncompleteBeta`, and the Student *t* and Fisher *F* tails built on it |
| `src/Lodestar.Stats/Internal/Normal.cs` | `Erfc`, `NormalSf` |
| `src/Lodestar.Stats/Internal/Kolmogorov.cs` | `KolmogorovSf`, the two-sample exact tail |
| `src/Lodestar.Stats/Internal/RankDistributions.cs` | exact rank-sum and signed-rank counts, by dynamic programming |
| `src/Lodestar.Stats/Internal/Ranks.cs` | `Rank` with average ties, and the tie-correction term three tests share |
| `src/Lodestar.Stats/TTest.cs` … `MultipleComparisons.cs` | one file per family, ten files |
| `tests/Lodestar.Stats.Tests/` | the suite; `StatsOracleAsserts.cs` holds the relative comparison |
| `tests/Lodestar.Stats.NetStandard.Tests/` | the same sources, linked, against the `netstandard2.0` build |
| `tests/oracles/stats_*.json` | ten frozen corpora |
| `samples/Lodestar.Sample/<Type>Sample.cs` | fourteen files — the packaging gate |
| `docs/reference/stats/` | thirty-six pages — the reference and doc-snippet gates |
| `bench/Lodestar.Stats.Benchmarks/` | three benchmarks against `Accord.Statistics.Testing` |

---

### Task 1: The package, its two test projects, and the wiring that makes CI see them

**Files:**

- Create: `src/Lodestar.Stats/Lodestar.Stats.csproj`, `src/Lodestar.Stats/Version.props`
- Create: `src/Lodestar.Stats/TestResult.cs`, `src/Lodestar.Stats/Options.cs`
- Create: `tests/Lodestar.Stats.Tests/Lodestar.Stats.Tests.csproj`
- Create: `tests/Lodestar.Stats.NetStandard.Tests/Lodestar.Stats.NetStandard.Tests.csproj`
- Create: `tests/Lodestar.Stats.NetStandard.Tests/NetStandardAssemblyGuardTests.cs`
- Create: `tests/Lodestar.Stats.Tests/ResultShapeTests.cs`
- Modify: `Lodestar.slnx`
- Modify: `tools/check_nuspec_dependencies.py`

**Interfaces:**

- Consumes: nothing.
- Produces: `Lodestar.Stats.TestResult(double Statistic, double PValue)`;
  `TTestResult(double Statistic, double PValue, double Df)` with
  `(double Low, double High) ConfidenceInterval(double level = 0.95)`;
  `Chi2ContingencyResult(double Statistic, double PValue, int Dof, double[][] ExpectedFrequencies)`;
  `KsResult(double Statistic, double PValue, double StatisticLocation, int StatisticSign)`;
  and the enums `Alternative { TwoSided, Less, Greater }`,
  `Variance { Equal, Welch }`, `Continuity { Applied, None }`,
  `ExactMethod { Auto, Exact, Asymptotic }`,
  `ZeroMethod { Wilcox, Pratt, ZSplit }`.

- [ ] **Step 1: Write `src/Lodestar.Stats/Version.props`**

```xml
<Project>

  <!--
    Lodestar.Stats owns its version here, independently of the other packages
    (see docs/decisions/0012-per-package-versioning.md).

    0.1.0 is this package's first release. Like Lodestar.Metrics and
    Lodestar.Conformal it creates no inter-package edge -- nothing depends on it
    and it depends on nothing -- so it releases on its own schedule rather than
    matching the others.
  -->
  <PropertyGroup>
    <LodestarStatsVersion>0.1.0</LodestarStatsVersion>
  </PropertyGroup>

</Project>
```

- [ ] **Step 2: Write `src/Lodestar.Stats/Lodestar.Stats.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- This package's version, owned here rather than repository-wide. -->
  <Import Project="Version.props" />

  <PropertyGroup>
    <Version>$(LodestarStatsVersion)</Version>
    <TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>
    <RootNamespace>Lodestar.Stats</RootNamespace>

    <PackageId>Lodestar.Stats</PackageId>
    <Description>Classical hypothesis tests for .NET at scipy.stats parity: Student and Welch t, Mann-Whitney U, Wilcoxon signed-rank, chi-square, Fisher exact, Kolmogorov-Smirnov, one-way ANOVA, Kruskal-Wallis, Shapiro-Wilk, and Bonferroni / Benjamini-Hochberg / Benjamini-Yekutieli corrections. Arrays in, statistic and p-value out. No model, no training loop, no dependencies.</Description>
    <PackageTags>statistics;hypothesis-test;p-value;t-test;anova;mann-whitney;wilcoxon;chi-square;fisher-exact;shapiro-wilk;scipy;lodestar</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Lodestar.Stats.Tests" />
    <!-- Same suite, replayed against the netstandard2.0 build. -->
    <InternalsVisibleTo Include="Lodestar.Stats.NetStandard.Tests" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Write `src/Lodestar.Stats/Options.cs`**

```csharp
namespace Lodestar.Stats;

/// <summary>Which tail of the null distribution a test's p-value covers.</summary>
/// <remarks>
/// scipy spells this <c>alternative</c> and defaults it to <c>'two-sided'</c>
/// everywhere. It is not a presentation choice: a one-sided p-value is a
/// different number, not half of the two-sided one, once the null distribution
/// is asymmetric or discrete.
/// </remarks>
public enum Alternative
{
    /// <summary>The samples differ, in either direction. scipy's <c>'two-sided'</c>.</summary>
    TwoSided,

    /// <summary>The first sample's distribution is shifted below the second's. scipy's <c>'less'</c>.</summary>
    Less,

    /// <summary>The first sample's distribution is shifted above the second's. scipy's <c>'greater'</c>.</summary>
    Greater,
}

/// <summary>Whether an independent-samples t-test pools the two variances.</summary>
/// <remarks>
/// scipy's <c>ttest_ind</c> defaults to <c>equal_var=True</c>, which is Student's
/// test, not Welch's. Pooling is only correct when the two population variances
/// really are equal; Welch is the safer default in practice and the deliberate
/// non-default here, so the caller has to say which one they mean.
/// </remarks>
public enum Variance
{
    /// <summary>Pool the two variances — Student's t. scipy's <c>equal_var=True</c>.</summary>
    Equal,

    /// <summary>Do not pool; use the Welch-Satterthwaite degrees of freedom. scipy's <c>equal_var=False</c>.</summary>
    Welch,
}

/// <summary>Whether a discrete statistic's normal approximation gets the half-unit correction.</summary>
/// <remarks>
/// One idea, three spellings in scipy: <c>use_continuity</c> on
/// <c>mannwhitneyu</c> (default true), <c>correction</c> on <c>wilcoxon</c>
/// (default false) and <c>correction</c> on <c>chi2_contingency</c> (default
/// true, and applied to 2x2 tables only). The three defaults disagree, which is
/// exactly why this is a named argument here rather than a bool nobody reads.
/// </remarks>
public enum Continuity
{
    /// <summary>Shift the statistic half a unit toward the mean before the normal tail.</summary>
    Applied,

    /// <summary>Take the statistic as it stands.</summary>
    None,
}

/// <summary>Whether a p-value comes from the exact null distribution or its normal approximation.</summary>
/// <remarks>
/// scipy calls this <c>method</c> and defaults it to <c>'auto'</c>, which picks
/// exact for small untied samples and asymptotic otherwise. The choice changes
/// the number returned, not merely how long it takes, so it cannot be hidden.
/// </remarks>
public enum ExactMethod
{
    /// <summary>Exact when the sample is small and free of ties, asymptotic otherwise. scipy's <c>'auto'</c>.</summary>
    Auto,

    /// <summary>
    /// Enumerate the null distribution, whatever the sample. Measured: scipy
    /// computes an exact p-value on tied data too rather than refusing, so this
    /// does the same and the remarks say the number is only approximate there.
    /// </summary>
    Exact,

    /// <summary>Use the normal (or Kolmogorov) approximation, whatever the sample size.</summary>
    Asymptotic,
}

/// <summary>How the Wilcoxon signed-rank test treats pairs whose difference is zero.</summary>
/// <remarks>
/// scipy's <c>zero_method</c>, default <c>'wilcox'</c>. The three rules give
/// three different statistics on the same data, so this is part of the test's
/// definition rather than a tuning knob.
/// </remarks>
public enum ZeroMethod
{
    /// <summary>Discard the zero-difference pairs before ranking. scipy's <c>'wilcox'</c>.</summary>
    Wilcox,

    /// <summary>Rank the zeros, then drop their ranks from the sums. scipy's <c>'pratt'</c>.</summary>
    Pratt,

    /// <summary>Rank the zeros and split their ranks evenly between the two sums. scipy's <c>'zsplit'</c>.</summary>
    ZSplit,
}
```

- [ ] **Step 4: Write `src/Lodestar.Stats/TestResult.cs`**

```csharp
namespace Lodestar.Stats;

/// <summary>A test statistic and the p-value that goes with it.</summary>
/// <remarks>
/// Eight of the ten families return exactly this, because eight of the ten
/// scipy calls return exactly this — measured, not assumed. The three that
/// carry more have their own record below rather than making the other eight
/// pay for fields they would leave empty.
/// </remarks>
/// <param name="Statistic">The test statistic, on whichever scale the family defines.</param>
/// <param name="PValue">The probability of a statistic at least this extreme under the null.</param>
public sealed record TestResult(double Statistic, double PValue);

/// <summary>A t-test's result: the statistic, the p-value and the degrees of freedom.</summary>
/// <param name="Statistic">The t statistic.</param>
/// <param name="PValue">The p-value on the requested tail.</param>
/// <param name="Df">
/// The degrees of freedom. Integral for Student and for the paired and
/// one-sample tests; fractional for Welch, whose Satterthwaite denominator is
/// not a count of anything.
/// </param>
public sealed record TTestResult(double Statistic, double PValue, double Df)
{
    /// <summary>The quantity the test compared: a mean, or a difference of means.</summary>
    /// <remarks>
    /// Internal rather than public. It exists so <see cref="ConfidenceInterval"/>
    /// can be a method on the result instead of a second call that re-derives
    /// everything, and scipy keeps it hidden on its own result for the same
    /// reason. Making it public would buy a reference page and a sample file for
    /// a number the caller already has.
    /// </remarks>
    internal double Estimate { get; init; }

    /// <summary>The standard error of <see cref="Estimate"/>. Internal, as above.</summary>
    internal double StandardError { get; init; }

    /// <summary>Which tail was tested, which decides whether the interval is half-open.</summary>
    internal Alternative Alternative { get; init; }

    /// <summary>The confidence interval for the difference this test measured.</summary>
    /// <remarks>
    /// A method rather than a property because it takes a level, which is how
    /// scipy exposes it too (<c>TtestResult.confidence_interval</c>). Returning
    /// a named tuple rather than a record keeps a two-double carrier from
    /// costing a public type, a reference page and a sample file.
    ///
    /// A one-sided test's interval is half-open, not narrower: asking whether
    /// the difference is greater than zero says nothing about how large it can
    /// be, so the other bound is an infinity rather than a number.
    /// </remarks>
    /// <param name="level">The confidence level, strictly between 0 and 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="level"/> is NaN or outside <c>(0, 1)</c>.
    /// </exception>
    public (double Low, double High) ConfidenceInterval(double level = 0.95)
    {
        // Written in Task 5, which is where the standard error it needs is
        // computed. The shape is fixed here so the reference page and the
        // corpus columns can be written against it.
        throw new NotImplementedException();
    }
}

/// <summary>A contingency-table chi-square result.</summary>
/// <param name="Statistic">The chi-square statistic.</param>
/// <param name="PValue">The upper-tail p-value.</param>
/// <param name="Dof">The degrees of freedom, <c>(rows - 1) * (columns - 1)</c>.</param>
/// <param name="ExpectedFrequencies">
/// The table expected under independence, row-major, same shape as the input.
/// </param>
public sealed record Chi2ContingencyResult(
    double Statistic, double PValue, int Dof, double[][] ExpectedFrequencies);

/// <summary>A two-sample Kolmogorov-Smirnov result.</summary>
/// <param name="Statistic">The supremum distance between the two empirical distributions.</param>
/// <param name="PValue">The p-value on the requested tail.</param>
/// <param name="StatisticLocation">The observed value at which that supremum is attained.</param>
/// <param name="StatisticSign">
/// <c>+1</c> when the first sample's empirical distribution exceeds the second's
/// at that point, <c>-1</c> when it falls below.
/// </param>
public sealed record KsResult(
    double Statistic, double PValue, double StatisticLocation, int StatisticSign);
```

- [ ] **Step 5: Write the two test projects**

`tests/Lodestar.Stats.Tests/Lodestar.Stats.Tests.csproj`:

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
    <ProjectReference Include="../../src/Lodestar.Stats/Lodestar.Stats.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="../oracles/**/*.json" CopyToOutputDirectory="PreserveNewest" LinkBase="oracles" />
  </ItemGroup>

  <!-- The gate's engine is shared by every package's suite, so it is linked rather
       than copied; the pages and the map are read from the output directory, the
       way the oracle corpora already are. -->
  <ItemGroup>
    <Compile Include="../Shared/ReferenceDocumentation.cs" Link="Documentation/ReferenceDocumentation.cs" />
    <None Include="../../docs/reference/stats/**/*.md" CopyToOutputDirectory="PreserveNewest"
          LinkBase="reference" />
    <None Include="../../docs/wiki-map.json" CopyToOutputDirectory="PreserveNewest" />
    <None Include="../../docs/**/*.md" Exclude="../../docs/superpowers/**"
          CopyToOutputDirectory="PreserveNewest" LinkBase="docs" />
  </ItemGroup>

</Project>
```

`tests/Lodestar.Stats.NetStandard.Tests/Lodestar.Stats.NetStandard.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!--
    Replays the entire Lodestar.Stats.Tests suite against the *netstandard2.0*
    build of the library, instead of the net10.0 one the original project
    references.

    netstandard2.0 is a contract, not a runtime, so the tests cannot run *on* it.
    They run on net10.0 -- identical host -- and only the assembly under test
    changes. Without this, the assemblies shipped to .NET Framework, Mono and
    Unity consumers are compile-verified but never executed.

    The test sources are linked, never copied: one suite, two builds.
  -->

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <AssemblyName>Lodestar.Stats.NetStandard.Tests</AssemblyName>
    <RootNamespace>Lodestar.Stats.Tests</RootNamespace>
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

  <!-- SetTargetFramework is what pins the reference to the netstandard2.0 build. -->
  <ItemGroup>
    <ProjectReference Include="../../src/Lodestar.Stats/Lodestar.Stats.csproj"
                      SetTargetFramework="TargetFramework=netstandard2.0" />
  </ItemGroup>

  <ItemGroup>
    <Compile Include="../Lodestar.Stats.Tests/**/*.cs"
             Exclude="../Lodestar.Stats.Tests/bin/**;../Lodestar.Stats.Tests/obj/**"
             Link="%(RecursiveDir)%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <None Include="../oracles/**/*.json" CopyToOutputDirectory="PreserveNewest" LinkBase="oracles" />
  </ItemGroup>

  <!-- The gate's engine is shared by every package's suite, so it is linked rather
       than copied; the pages and the map are read from the output directory, the
       way the oracle corpora already are. -->
  <ItemGroup>
    <Compile Include="../Shared/ReferenceDocumentation.cs" Link="Documentation/ReferenceDocumentation.cs" />
    <None Include="../../docs/reference/stats/**/*.md" CopyToOutputDirectory="PreserveNewest"
          LinkBase="reference" />
    <None Include="../../docs/wiki-map.json" CopyToOutputDirectory="PreserveNewest" />
    <None Include="../../docs/**/*.md" Exclude="../../docs/superpowers/**"
          CopyToOutputDirectory="PreserveNewest" LinkBase="docs" />
  </ItemGroup>

</Project>
```

- [ ] **Step 6: Write the mirror's assembly guard**

`tests/Lodestar.Stats.NetStandard.Tests/NetStandardAssemblyGuardTests.cs`:

```csharp
using System.Reflection;
using System.Runtime.Versioning;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>
/// Guards the premise of this project: that the suite is replaying against the
/// netstandard2.0 assembly and not the net10.0 one.
/// </summary>
/// <remarks>
/// Without this, a reference that quietly resolved back to net10.0 would leave
/// every test passing while proving nothing. The assertion is cheap; the false
/// confidence it prevents is not.
/// </remarks>
public sealed class NetStandardAssemblyGuardTests
{
    [Fact]
    public void Suite_runs_against_the_netstandard2_0_build()
    {
        Assembly assembly = typeof(Lodestar.Stats.TestResult).Assembly;
        string? framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        Assert.Equal(".NETStandard,Version=v2.0", framework);
    }
}
```

- [ ] **Step 7: Write the failing test for the result shapes**

`tests/Lodestar.Stats.Tests/ResultShapeTests.cs`:

```csharp
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>
/// The four result shapes, pinned before any family fills them: eight families
/// return the shared record and three carry extras, and a later task must not
/// quietly widen the shared one.
/// </summary>
public sealed class ResultShapeTests
{
    [Fact]
    public void Shared_result_carries_a_statistic_and_a_p_value()
    {
        TestResult result = new(Statistic: 1.5, PValue: 0.25);

        Assert.Equal(1.5, result.Statistic);
        Assert.Equal(0.25, result.PValue);
    }

    [Fact]
    public void T_result_adds_degrees_of_freedom_that_need_not_be_whole()
    {
        // Welch-Satterthwaite degrees of freedom are not a count of anything, so
        // the field is a double and a fractional value must survive the record.
        TTestResult result = new(Statistic: -2.0, PValue: 0.06, Df: 12.7431)
        {
            Estimate = -1.5,
            StandardError = 0.75,
            Alternative = Alternative.TwoSided,
        };

        Assert.Equal(12.7431, result.Df);
        Assert.Equal(-1.5, result.Estimate);
    }

    [Fact]
    public void Contingency_result_keeps_the_expected_table_row_major()
    {
        double[][] expected = [[5.0, 15.0], [15.0, 45.0]];
        Chi2ContingencyResult result = new(0.0, 1.0, Dof: 1, ExpectedFrequencies: expected);

        Assert.Equal(1, result.Dof);
        Assert.Equal(45.0, result.ExpectedFrequencies[1][1]);
    }

    [Fact]
    public void Ks_result_keeps_where_the_supremum_was_reached_and_its_sign()
    {
        KsResult result = new(0.4, 0.3, StatisticLocation: 2.5, StatisticSign: -1);

        Assert.Equal(2.5, result.StatisticLocation);
        Assert.Equal(-1, result.StatisticSign);
    }
}
```

- [ ] **Step 8: Run the test to verify it fails**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~ResultShapeTests"`

Expected: FAIL — the project does not exist yet if steps 1-7 were skipped, and
otherwise the four facts pass while the build fails on `Lodestar.slnx` not
knowing the projects. Proceed to step 9 either way; the failure that matters is
step 12's.

- [ ] **Step 9: Add the four projects to `Lodestar.slnx`**

Insert `<Project Path="src/Lodestar.Stats/Lodestar.Stats.csproj" />` into the
`/src/` folder after `Lodestar.Onnx`, and these two into `/tests/`:

```xml
    <Project Path="tests/Lodestar.Stats.NetStandard.Tests/Lodestar.Stats.NetStandard.Tests.csproj" />
    <Project Path="tests/Lodestar.Stats.Tests/Lodestar.Stats.Tests.csproj" />
```

- [ ] **Step 10: Teach `tools/check_nuspec_dependencies.py` the new package**

Add the constant beside the others (around line 57):

```python
STATS = "Lodestar.Stats"
```

and the row at the end of `EXPECTED`, after `DECOMPOSITION`:

```python
    STATS: {
        # Nothing on net10.0, only the polyfills on netstandard2.0: a hypothesis
        # test is arithmetic over arrays, with no model to persist. The tail
        # probabilities come from this package's own Internal/ layer rather than
        # from a numerical dependency, which is what keeps it core tier.
        NET: {},
        NETSTANDARD: {**POLYFILLS},
    },
```

- [ ] **Step 11: Build both targets**

Run: `dotnet build Lodestar.slnx -c Release`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 12: Run both suites and read the counts**

Run: `dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~Lodestar.Stats"`
Expected: `Lodestar.Stats.Tests` reports **4 passed**;
`Lodestar.Stats.NetStandard.Tests` reports **5 passed** — the same four plus
`NetStandardAssemblyGuardTests`. A mirror reporting 4 means the guard file
landed in the wrong project.

- [ ] **Step 13: Run the guard gates**

```bash
python3 tools/check_netstandard_guards.py
python3 tools/check_version_floor.py
python3 tools/check_comment_length.py
dotnet format Lodestar.slnx --verify-no-changes
```

Expected: all four silent, exit 0.

- [ ] **Step 14: Commit**

```bash
git add src/Lodestar.Stats tests/Lodestar.Stats.Tests tests/Lodestar.Stats.NetStandard.Tests \
        Lodestar.slnx tools/check_nuspec_dependencies.py
git commit -m "Lodestar.Stats: the package, its two suites and the result shapes

Refs #442. Core tier, net10.0;netstandard2.0, version 0.1.0 owned in its own
Version.props, no entry in src/Directory.Packages.props because the package has
no sibling edge and no external dependency.

The four result shapes are what scipy returns, measured: eight of the ten
families carry a statistic and a p-value and nothing else, so eight of them
share one record rather than each owning a near-empty one."
```

---

### Task 2: The numerical layer

**Files:**

- Create: `src/Lodestar.Stats/Internal/Gamma.cs`, `Internal/Beta.cs`,
  `Internal/Normal.cs`, `Internal/Kolmogorov.cs`
- Test: `tests/Lodestar.Stats.Tests/Internal/GammaTests.cs`,
  `Internal/BetaTests.cs`, `Internal/NormalTests.cs`, `Internal/KolmogorovTests.cs`

**Interfaces:**

- Consumes: nothing from Task 1 but the project.
- Produces, all `internal static`:
  - `double Gamma.LogGamma(double x)`
  - `double Gamma.RegularizedP(double a, double x)`, `double Gamma.RegularizedQ(double a, double x)`
  - `double Beta.RegularizedIncomplete(double a, double b, double x)`
  - `double Beta.StudentSf(double t, double df)` — the upper tail of Student's *t*
  - `double Beta.FisherSf(double f, double dfn, double dfd)` — the upper tail of *F*
  - `double Normal.Erfc(double x)`, `double Normal.Sf(double z)` — the upper tail of the standard normal
  - `double Kolmogorov.Sf(double lambda)` — `Q(λ) = 2 Σ (−1)^{k−1} e^{−2k²λ²}`

**Provenance.** Every function here is written from its published mathematical
description — Lanczos (1964) for the log-gamma, the standard series/continued-fraction
split for the regularized incomplete functions, modified Lentz (1976) for
evaluating the continued fractions, and the Kolmogorov series. **Numerical
Recipes' code is copyrighted and must not be transcribed**, nor may scipy's
Cython, `Accord`'s C# or `boost::math`'s C++. Reading one to diagnose a single
failing case is diagnosis and is allowed; deriving the implementation from it is
not (ADR 0003).

- [ ] **Step 1: Write the failing tests for `Gamma`**

`tests/Lodestar.Stats.Tests/Internal/GammaTests.cs`:

```csharp
using Lodestar.Stats.Internal;
using Xunit;

namespace Lodestar.Stats.Tests.Internal;

/// <summary>
/// The log-gamma and the two regularized incomplete gammas, at values whose
/// closed forms are known exactly, so the test does not need an oracle to say
/// what the answer is.
/// </summary>
public sealed class GammaTests
{
    private const double Tolerance = 1e-13;

    [Theory]
    // Gamma(n) = (n-1)!, so LogGamma(n) = log((n-1)!).
    [InlineData(1.0, 0.0)]
    [InlineData(2.0, 0.0)]
    [InlineData(3.0, 0.6931471805599453)]      // log 2
    [InlineData(6.0, 4.787491742782046)]       // log 120
    // Gamma(1/2) = sqrt(pi), so LogGamma(0.5) = log(pi)/2.
    [InlineData(0.5, 0.5723649429247001)]
    // Gamma(3/2) = sqrt(pi)/2.
    [InlineData(1.5, -0.1207822376352452)]
    // Below 0.5 the reflection formula takes over; Gamma(0.1) = 9.51350769866873.
    [InlineData(0.1, 2.252712651734206)]
    public void LogGamma_matches_the_closed_forms(double x, double expected)
    {
        Assert.Equal(expected, Gamma.LogGamma(x), Tolerance);
    }

    [Fact]
    public void LogGamma_refuses_a_non_positive_argument()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Gamma.LogGamma(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Gamma.LogGamma(-1.5));
    }

    [Theory]
    // P(1, x) = 1 - exp(-x): the exponential distribution's CDF, exactly.
    [InlineData(1.0, 0.5, 0.3934693402873666)]
    [InlineData(1.0, 2.0, 0.8646647167633873)]
    [InlineData(1.0, 12.0, 0.9999938557876467)]
    // P(1/2, x) = erf(sqrt(x)); at x = 0.5 that is erf(1/sqrt2) = 0.6826894921370859,
    // which is also the standard normal's mass within one sigma.
    [InlineData(0.5, 0.5, 0.6826894921370859)]
    public void RegularizedP_matches_the_closed_forms(double a, double x, double expected)
    {
        Assert.Equal(expected, Gamma.RegularizedP(a, x), Tolerance);
    }

    [Theory]
    // The series is used below a + 1 and the continued fraction above it. Both
    // branches must satisfy P + Q = 1, and the crossing itself is the seam that
    // a one-branch implementation gets wrong.
    [InlineData(3.0, 1.0)]
    [InlineData(3.0, 3.9)]
    [InlineData(3.0, 4.0)]
    [InlineData(3.0, 4.1)]
    [InlineData(3.0, 40.0)]
    [InlineData(0.5, 1e-8)]
    [InlineData(200.0, 200.0)]
    public void RegularizedP_and_Q_sum_to_one_across_the_branch_seam(double a, double x)
    {
        Assert.Equal(1.0, Gamma.RegularizedP(a, x) + Gamma.RegularizedQ(a, x), 1e-14);
    }

    [Fact]
    public void RegularizedP_is_zero_at_the_origin_and_one_far_out()
    {
        Assert.Equal(0.0, Gamma.RegularizedP(2.0, 0.0));
        Assert.Equal(1.0, Gamma.RegularizedP(2.0, 400.0), 1e-15);
    }

    [Fact]
    public void RegularizedP_refuses_a_negative_x_or_a_non_positive_a()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Gamma.RegularizedP(1.0, -1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Gamma.RegularizedP(0.0, 1.0));
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~GammaTests"`
Expected: FAIL — `The type or namespace name 'Internal' does not exist`.

- [ ] **Step 3: Write `src/Lodestar.Stats/Internal/Gamma.cs`**

```csharp
namespace Lodestar.Stats.Internal;

/// <summary>The log-gamma function and the two regularized incomplete gammas.</summary>
/// <remarks>
/// Written from the published descriptions -- Lanczos (1964) for the log-gamma,
/// the series-below / continued-fraction-above split for the incomplete pair,
/// and modified Lentz (1976) to evaluate the fraction. No reference
/// implementation is transcribed (ADR 0003).
///
/// The upper tail <c>Q</c> is what a chi-square p-value is: with
/// <c>a = dof / 2</c> and <c>x = statistic / 2</c>, <c>Q(a, x)</c> is the
/// probability of a statistic at least this large.
/// </remarks>
internal static class Gamma
{
    // Lanczos g = 7, nine coefficients. The pair (g, n) is not free: these
    // coefficients are only valid for this g, and mixing a table from one g with
    // another is the classic way to lose eight digits silently.
    private const double LanczosG = 7.0;

    private static readonly double[] LanczosCoefficients =
    [
        0.99999999999980993,
        676.5203681218851,
        -1259.1392167224028,
        771.32342877765313,
        -176.61502916214059,
        12.507343278686905,
        -0.13857109526572012,
        9.9843695780195716e-6,
        1.5056327351493116e-7,
    ];

    private const int MaxIterations = 300;
    private const double Epsilon = 3e-16;

    // Smallest positive normal double: Lentz's method divides by the running
    // denominator, so a zero one is nudged to this rather than producing an
    // infinity that never recovers.
    private const double Tiny = 1e-300;

    internal static double LogGamma(double x)
    {
        if (double.IsNaN(x) || x <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(x), x, "The log-gamma function is defined here for x > 0 only.");
        }

        // Reflection: Gamma(x)Gamma(1-x) = pi / sin(pi x). Below 0.5 the Lanczos
        // sum loses precision, and above it the reflection would.
        if (x < 0.5)
        {
            return Math.Log(Math.PI / Math.Abs(Math.Sin(Math.PI * x))) - LogGamma(1.0 - x);
        }

        double z = x - 1.0;
        double series = LanczosCoefficients[0];
        for (int i = 1; i < LanczosCoefficients.Length; i++)
        {
            series += LanczosCoefficients[i] / (z + i);
        }

        double t = z + LanczosG + 0.5;
        return (0.5 * Math.Log(2.0 * Math.PI)) + ((z + 0.5) * Math.Log(t)) - t + Math.Log(series);
    }

    internal static double RegularizedP(double a, double x)
    {
        Validate(a, x);
        if (x == 0.0)
        {
            return 0.0;
        }

        return x < a + 1.0 ? SeriesP(a, x) : 1.0 - ContinuedFractionQ(a, x);
    }

    internal static double RegularizedQ(double a, double x)
    {
        Validate(a, x);
        if (x == 0.0)
        {
            return 1.0;
        }

        return x < a + 1.0 ? 1.0 - SeriesP(a, x) : ContinuedFractionQ(a, x);
    }

    private static void Validate(double a, double x)
    {
        if (double.IsNaN(a) || a <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(a), a, "The shape must be positive.");
        }
        if (double.IsNaN(x) || x < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "The argument must not be negative.");
        }
    }

    // P(a, x) = x^a e^-x / Gamma(a) * sum_{n>=0} x^n / (a(a+1)...(a+n)).
    private static double SeriesP(double a, double x)
    {
        double term = 1.0 / a;
        double sum = term;
        for (int n = 1; n <= MaxIterations; n++)
        {
            term *= x / (a + n);
            sum += term;
            if (Math.Abs(term) < Math.Abs(sum) * Epsilon)
            {
                break;
            }
        }

        return sum * Math.Exp((a * Math.Log(x)) - x - LogGamma(a));
    }

    // Q(a, x) = x^a e^-x / Gamma(a) * 1/(x+1-a - 1(1-a)/(x+3-a - 2(2-a)/(x+5-a - ...))),
    // evaluated by modified Lentz.
    private static double ContinuedFractionQ(double a, double x)
    {
        double b = x + 1.0 - a;
        double c = 1.0 / Tiny;
        double d = 1.0 / b;
        double h = d;

        for (int i = 1; i <= MaxIterations; i++)
        {
            double an = -i * (i - a);
            b += 2.0;

            d = (an * d) + b;
            if (Math.Abs(d) < Tiny)
            {
                d = Tiny;
            }

            c = b + (an / c);
            if (Math.Abs(c) < Tiny)
            {
                c = Tiny;
            }

            d = 1.0 / d;
            double delta = d * c;
            h *= delta;

            if (Math.Abs(delta - 1.0) < Epsilon)
            {
                break;
            }
        }

        return Math.Exp((a * Math.Log(x)) - x - LogGamma(a)) * h;
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~GammaTests"`
Expected: PASS, **19 tests** (7 + 2 + 4 + 7 theory cases + 2 facts across the class).

- [ ] **Step 5: Write the failing tests for `Beta`**

`tests/Lodestar.Stats.Tests/Internal/BetaTests.cs`:

```csharp
using Lodestar.Stats.Internal;
using Xunit;

namespace Lodestar.Stats.Tests.Internal;

/// <summary>
/// The regularized incomplete beta and the two tails built on it, against
/// closed forms rather than against an oracle: an incomplete beta that is wrong
/// only in the far tail passes every corpus whose p-values sit near 0.05.
/// </summary>
public sealed class BetaTests
{
    private const double Tolerance = 1e-13;

    [Theory]
    // I_x(1, 1) = x: the uniform distribution's CDF.
    [InlineData(1.0, 1.0, 0.25, 0.25)]
    // I_x(1, b) = 1 - (1-x)^b.
    [InlineData(1.0, 3.0, 0.5, 0.875)]
    // I_x(a, 1) = x^a.
    [InlineData(3.0, 1.0, 0.5, 0.125)]
    // Symmetry at the midpoint: I_{1/2}(a, a) = 1/2 for every a.
    [InlineData(7.5, 7.5, 0.5, 0.5)]
    [InlineData(0.5, 0.5, 0.5, 0.5)]
    // The endpoints.
    [InlineData(2.0, 3.0, 0.0, 0.0)]
    [InlineData(2.0, 3.0, 1.0, 1.0)]
    public void RegularizedIncomplete_matches_the_closed_forms(
        double a, double b, double x, double expected)
    {
        Assert.Equal(expected, Beta.RegularizedIncomplete(a, b, x), Tolerance);
    }

    [Theory]
    // The continued fraction converges on one side of (a+1)/(a+b+2) and the
    // reflection I_x(a,b) = 1 - I_{1-x}(b,a) carries the other. Both sides must
    // agree with the complement identity, and the seam is where they can differ.
    [InlineData(4.0, 9.0, 0.3)]
    [InlineData(4.0, 9.0, 0.3333333333333333)]
    [InlineData(4.0, 9.0, 0.4)]
    [InlineData(60.0, 60.0, 0.51)]
    public void RegularizedIncomplete_is_complementary_across_the_seam(double a, double b, double x)
    {
        double left = Beta.RegularizedIncomplete(a, b, x);
        double right = Beta.RegularizedIncomplete(b, a, 1.0 - x);

        Assert.Equal(1.0, left + right, 1e-14);
    }

    [Fact]
    public void RegularizedIncomplete_refuses_an_x_outside_the_unit_interval()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Beta.RegularizedIncomplete(2.0, 2.0, -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Beta.RegularizedIncomplete(2.0, 2.0, 1.1));
    }

    [Theory]
    // Student's t with 1 degree of freedom is the Cauchy distribution, whose
    // upper tail is 1/2 - atan(t)/pi -- a closed form the implementation cannot
    // have been fitted to.
    [InlineData(0.0, 1.0, 0.5)]
    [InlineData(1.0, 1.0, 0.25)]
    [InlineData(-1.0, 1.0, 0.75)]
    [InlineData(10.0, 1.0, 0.03172551743055357)]
    // With 2 degrees of freedom the tail is (1 - t/sqrt(t^2+2))/2.
    [InlineData(2.0, 2.0, 0.09175170953613698)]
    public void StudentSf_matches_the_closed_forms(double t, double df, double expected)
    {
        Assert.Equal(expected, Beta.StudentSf(t, df), Tolerance);
    }

    [Fact]
    public void StudentSf_stays_accurate_in_the_far_tail()
    {
        // Relative, not absolute: at 1e-27 an absolute 1e-9 check would pass an
        // implementation that returned zero. This is the whole reason the layer
        // is tested here and not only through a corpus.
        double actual = Beta.StudentSf(12.0, 30.0);

        Assert.Equal(1.0, actual / 2.7900927075996303e-13, 1e-9);
    }

    [Theory]
    // F(1, d) is the square of a t with d degrees of freedom, so the F upper
    // tail at f equals twice the t upper tail at sqrt(f).
    [InlineData(4.0, 1.0, 10.0)]
    [InlineData(0.5, 1.0, 25.0)]
    public void FisherSf_is_twice_the_student_tail_at_the_square_root(double f, double dfn, double dfd)
    {
        Assert.Equal(
            2.0 * Beta.StudentSf(Math.Sqrt(f), dfd),
            Beta.FisherSf(f, dfn, dfd),
            Tolerance);
    }

    [Fact]
    public void FisherSf_is_one_at_the_origin()
    {
        Assert.Equal(1.0, Beta.FisherSf(0.0, 3.0, 12.0));
    }
}
```

- [ ] **Step 6: Run to verify it fails**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~BetaTests"`
Expected: FAIL — `The name 'Beta' does not exist in the current context`.

- [ ] **Step 7: Write `src/Lodestar.Stats/Internal/Beta.cs`**

```csharp
namespace Lodestar.Stats.Internal;

/// <summary>The regularized incomplete beta, and the Student and Fisher tails it carries.</summary>
/// <remarks>
/// One continued fraction serves three families: a t-test's p-value is a
/// Student tail, an ANOVA's is a Fisher tail, and both are the incomplete beta
/// under a change of variable. Written from the published description and
/// evaluated by modified Lentz (1976); no reference implementation is
/// transcribed (ADR 0003).
/// </remarks>
internal static class Beta
{
    private const int MaxIterations = 300;
    private const double Epsilon = 3e-16;
    private const double Tiny = 1e-300;

    internal static double RegularizedIncomplete(double a, double b, double x)
    {
        if (double.IsNaN(a) || a <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(a), a, "The first shape must be positive.");
        }
        if (double.IsNaN(b) || b <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(b), b, "The second shape must be positive.");
        }
        if (double.IsNaN(x) || x < 0.0 || x > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "The argument must lie in [0, 1].");
        }

        if (x == 0.0 || x == 1.0)
        {
            return x;
        }

        double front = Math.Exp(
            Gamma.LogGamma(a + b) - Gamma.LogGamma(a) - Gamma.LogGamma(b) +
            (a * Math.Log(x)) + (b * Math.Log(1.0 - x)));

        // The fraction converges quickly only on the side of the distribution's
        // mode; past it the reflection is the fast branch, not a fallback.
        return x < (a + 1.0) / (a + b + 2.0)
            ? front * ContinuedFraction(a, b, x) / a
            : 1.0 - (front * ContinuedFraction(b, a, 1.0 - x) / b);
    }

    /// <summary>The upper tail of Student's t distribution: P(T &gt; t).</summary>
    internal static double StudentSf(double t, double df)
    {
        if (double.IsNaN(t))
        {
            return double.NaN;
        }

        // I_{df/(df+t^2)}(df/2, 1/2) is twice the tail beyond |t|, so half of it
        // is the tail on one side and the sign says which side we are on.
        double tail = 0.5 * RegularizedIncomplete(df / 2.0, 0.5, df / (df + (t * t)));
        return t >= 0.0 ? tail : 1.0 - tail;
    }

    /// <summary>The upper tail of the F distribution: P(F &gt; f).</summary>
    internal static double FisherSf(double f, double dfn, double dfd)
    {
        if (double.IsNaN(f))
        {
            return double.NaN;
        }
        if (f <= 0.0)
        {
            return 1.0;
        }

        return RegularizedIncomplete(dfd / 2.0, dfn / 2.0, dfd / (dfd + (dfn * f)));
    }

    private static double ContinuedFraction(double a, double b, double x)
    {
        double c = 1.0;
        double d = 1.0 - ((a + b) * x / (a + 1.0));
        if (Math.Abs(d) < Tiny)
        {
            d = Tiny;
        }

        d = 1.0 / d;
        double h = d;

        for (int i = 1; i <= MaxIterations; i++)
        {
            int m = i / 2;

            // The fraction's numerators alternate between the two forms with the
            // term's parity; folding both into one loop is what keeps the
            // recurrence stable.
            double numerator = i % 2 == 0
                ? m * (b - m) * x / ((a + (2 * m) - 1.0) * (a + (2 * m)))
                : -(a + m) * (a + b + m) * x / ((a + (2 * m)) * (a + (2 * m) + 1.0));

            d = 1.0 + (numerator * d);
            if (Math.Abs(d) < Tiny)
            {
                d = Tiny;
            }
            d = 1.0 / d;

            c = 1.0 + (numerator / c);
            if (Math.Abs(c) < Tiny)
            {
                c = Tiny;
            }

            double delta = c * d;
            h *= delta;

            if (Math.Abs(delta - 1.0) < Epsilon)
            {
                break;
            }
        }

        return h;
    }
}
```

- [ ] **Step 8: Run to verify it passes**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~BetaTests"`
Expected: PASS, **20 tests**.

- [ ] **Step 9: Write the failing tests for `Normal` and `Kolmogorov`**

`tests/Lodestar.Stats.Tests/Internal/NormalTests.cs`:

```csharp
using Lodestar.Stats.Internal;
using Xunit;

namespace Lodestar.Stats.Tests.Internal;

/// <summary>The complementary error function and the standard normal's upper tail.</summary>
public sealed class NormalTests
{
    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(0.5, 0.4795001221869535)]
    [InlineData(1.0, 0.15729920705028516)]
    [InlineData(2.0, 0.004677734981047266)]
    // Negative arguments come back through erfc(-x) = 2 - erfc(x).
    [InlineData(-1.0, 1.8427007929497148)]
    public void Erfc_matches_the_published_values(double x, double expected)
    {
        Assert.Equal(expected, Normal.Erfc(x), 1e-14);
    }

    [Theory]
    // The three sigma landmarks, to fifteen digits.
    [InlineData(0.0, 0.5)]
    [InlineData(1.0, 0.15865525393145707)]
    [InlineData(1.959963984540054, 0.025)]
    [InlineData(-1.0, 0.8413447460685429)]
    public void Sf_matches_the_normal_landmarks(double z, double expected)
    {
        Assert.Equal(expected, Normal.Sf(z), 1e-14);
    }

    [Fact]
    public void Sf_stays_accurate_in_the_far_tail()
    {
        // Relative: P(Z > 10) is 7.6e-24, and an absolute check at 1e-9 would
        // accept a hard zero here.
        Assert.Equal(1.0, Normal.Sf(10.0) / 7.61985302416047e-24, 1e-9);
    }
}
```

`tests/Lodestar.Stats.Tests/Internal/KolmogorovTests.cs`:

```csharp
using Lodestar.Stats.Internal;
using Xunit;

namespace Lodestar.Stats.Tests.Internal;

/// <summary>The Kolmogorov distribution's upper tail.</summary>
public sealed class KolmogorovTests
{
    [Fact]
    public void Sf_is_one_at_and_below_zero()
    {
        Assert.Equal(1.0, Kolmogorov.Sf(0.0));
        Assert.Equal(1.0, Kolmogorov.Sf(-1.0));
    }

    [Theory]
    // Q(lambda) = 2 * sum_{k>=1} (-1)^{k-1} exp(-2 k^2 lambda^2), compared here
    // against scipy.stats.kstwobign.sf, which agrees with the series to every digit.
    [InlineData(0.5, 0.9639452436648751)]
    [InlineData(1.0, 0.26999967167735456)]
    [InlineData(1.36, 0.049485876755377876)]
    [InlineData(2.0, 0.0006709252557796953)]
    public void Sf_matches_the_series(double lambda, double expected)
    {
        Assert.Equal(expected, Kolmogorov.Sf(lambda), 1e-14);
    }

    [Fact]
    public void Sf_is_monotone_decreasing()
    {
        double previous = 1.0;
        for (double lambda = 0.1; lambda < 4.0; lambda += 0.1)
        {
            double current = Kolmogorov.Sf(lambda);
            Assert.True(current < previous, $"Q({lambda}) = {current} did not fall below {previous}.");
            previous = current;
        }
    }
}
```

- [ ] **Step 10: Run to verify both fail**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~NormalTests|FullyQualifiedName~KolmogorovTests"`
Expected: FAIL — neither type exists.

- [ ] **Step 11: Write `src/Lodestar.Stats/Internal/Normal.cs`**

```csharp
namespace Lodestar.Stats.Internal;

/// <summary>The complementary error function, and the standard normal's upper tail.</summary>
/// <remarks>
/// Built on the regularized incomplete gamma rather than on a rational
/// approximation of its own: erfc(x) = Q(1/2, x^2) for x >= 0 is an identity,
/// not a fit, so the accuracy this reaches in the far tail is the accuracy
/// <see cref="Gamma"/> already has to have for the chi-square tests.
/// </remarks>
internal static class Normal
{
    internal static double Erfc(double x)
    {
        if (double.IsNaN(x))
        {
            return double.NaN;
        }

        // erfc(-x) = 2 - erfc(x). Reflecting rather than evaluating at a negative
        // argument keeps the identity above valid, since Q takes x^2 either way.
        if (x < 0.0)
        {
            return 2.0 - Erfc(-x);
        }

        return x == 0.0 ? 1.0 : Gamma.RegularizedQ(0.5, x * x);
    }

    /// <summary>The standard normal's upper tail: P(Z &gt; z).</summary>
    internal static double Sf(double z) => 0.5 * Erfc(z / Math.Sqrt(2.0));
}
```

- [ ] **Step 12: Write `src/Lodestar.Stats/Internal/Kolmogorov.cs`**

```csharp
namespace Lodestar.Stats.Internal;

/// <summary>The Kolmogorov distribution's upper tail.</summary>
/// <remarks>
/// Q(lambda) = 2 * sum_{k>=1} (-1)^{k-1} exp(-2 k^2 lambda^2). The terms fall
/// off as exp(-2 k^2 lambda^2), so the series converges in a handful of terms
/// for every lambda a two-sample KS test produces; the loop stops on the term's
/// own magnitude rather than on a fixed count.
/// </remarks>
internal static class Kolmogorov
{
    private const int MaxTerms = 200;
    private const double Epsilon = 1e-17;

    internal static double Sf(double lambda)
    {
        if (double.IsNaN(lambda))
        {
            return double.NaN;
        }
        if (lambda <= 0.0)
        {
            return 1.0;
        }

        double factor = -2.0 * lambda * lambda;
        double sum = 0.0;
        double sign = 1.0;

        for (int k = 1; k <= MaxTerms; k++)
        {
            double term = Math.Exp(factor * k * k);
            sum += sign * term;
            sign = -sign;

            if (term < Epsilon)
            {
                break;
            }
        }

        double q = 2.0 * sum;

        // The alternating series can overshoot at very small lambda, where the
        // true answer is already 1 to every digit a double holds.
        return q > 1.0 ? 1.0 : q < 0.0 ? 0.0 : q;
    }
}
```

- [ ] **Step 13: Run to verify they pass**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~Internal"`
Expected: PASS. Read the count: **51 tests** across `GammaTests`, `BetaTests`,
`NormalTests` and `KolmogorovTests`. A count near 4 means the filter matched the
class names and not the namespace — re-run without `--filter` and read the
project total instead.

- [ ] **Step 14: Cross-check the layer against scipy once, by hand**

This is a sanity probe, not a committed test — the committed proof is the corpus
in Task 3. Run it from a scratch directory that is not an ancestor of the
checkout:

```bash
REPO=$(git rev-parse --show-toplevel); cd "$(mktemp -d)"
PYTHONSAFEPATH=1 "$REPO/.venv-oracles/bin/python" -c "
from scipy import stats, special
print('student sf(12, 30) =', stats.t.sf(12.0, 30))
print('f sf(4, 3, 12)     =', stats.f.sf(4.0, 3, 12))
print('chi2 sf(7.8, 3)    =', stats.chi2.sf(7.8, 3))
print('norm sf(10)        =', stats.norm.sf(10.0))
print('kolmogorov sf(1.36)=', stats.kstwobign.sf(1.36))
"
```

Compare each against the C# by adding a temporary `Console.WriteLine` in a
scratch console app, or by an `[Fact]` you delete before committing. Every one
must agree to at least twelve significant digits. If one does not, the failure
is in this layer and must be fixed here — a family built on a wrong tail will
"pass" its own corpus only by having its corpus regenerated to match the bug.

- [ ] **Step 15: Run the gates and commit**

```bash
dotnet build Lodestar.slnx -c Release
dotnet format Lodestar.slnx --verify-no-changes
python3 tools/check_comment_length.py
python3 tools/check_repeated_literals.py --base origin/main
git add src/Lodestar.Stats/Internal tests/Lodestar.Stats.Tests/Internal
git commit -m "Lodestar.Stats: the internal numerical layer

Refs #442. Log-gamma by Lanczos, the two regularized incomplete gammas by the
series-below / continued-fraction-above split, the incomplete beta and the
Student and Fisher tails it carries, erfc as Q(1/2, x^2), and the Kolmogorov
series. Written from the published mathematics; no reference implementation
transcribed (ADR 0003).

Tested against closed forms rather than against an oracle, and in the far tail
relatively rather than absolutely: at P(Z > 10) = 7.6e-24 an absolute 1e-9 check
accepts an implementation that returns zero."
```

---

### Task 3: Ranks, and the two exact rank distributions

**Files:**

- Create: `src/Lodestar.Stats/Internal/Ranks.cs`, `src/Lodestar.Stats/Internal/RankDistributions.cs`
- Test: `tests/Lodestar.Stats.Tests/Internal/RanksTests.cs`,
  `tests/Lodestar.Stats.Tests/Internal/RankDistributionsTests.cs`

**Interfaces:**

- Consumes: nothing from Task 2.
- Produces, `internal static`:
  - `double[] Ranks.Average(ReadOnlySpan<double> values)` — mid-ranks, 1-based
  - `double Ranks.TieCorrection(ReadOnlySpan<double> values)` — `Σ (t³ − t)` over tie groups
  - `bool Ranks.HasTies(ReadOnlySpan<double> values)`
  - `double[] RankDistributions.MannWhitneyCounts(int n, int m)` — index `u` holds the
    number of arrangements whose U equals `u`, for `u` in `[0, n·m]`
  - `double[] RankDistributions.SignedRankCounts(int n)` — index `w` holds the number
    of sign assignments of `1..n` whose positive-rank sum equals `w`, for `w` in `[0, n(n+1)/2]`

Counts are `double`, not `long`: the signed-rank count for `n = 60` exceeds
`2^60`, and the tail probability only ever divides by the total, so a `double`'s
53-bit mantissa loses nothing a p-value at `1e-9` relative can see, while a
`long` would overflow silently.

- [ ] **Step 1: Write the failing tests for `Ranks`**

`tests/Lodestar.Stats.Tests/Internal/RanksTests.cs`:

```csharp
using Lodestar.Stats.Internal;
using Xunit;

namespace Lodestar.Stats.Tests.Internal;

/// <summary>
/// Mid-ranks and the tie-correction term. Three families share these, so a bug
/// here would show up as three unrelated corpora disagreeing at once.
/// </summary>
public sealed class RanksTests
{
    [Fact]
    public void Average_ranks_a_strictly_increasing_sample_one_to_n()
    {
        double[] ranks = Ranks.Average([10.0, 20.0, 30.0, 40.0]);

        Assert.Equal([1.0, 2.0, 3.0, 4.0], ranks);
    }

    [Fact]
    public void Average_ranks_are_positional_not_sorted()
    {
        // The result is indexed by the input's own order, not by sorted order:
        // a caller sums the ranks belonging to one of two interleaved samples.
        double[] ranks = Ranks.Average([30.0, 10.0, 20.0]);

        Assert.Equal([3.0, 1.0, 2.0], ranks);
    }

    [Fact]
    public void Average_splits_a_tie_group_at_its_midpoint()
    {
        // Two values tied for ranks 2 and 3 both take 2.5.
        double[] ranks = Ranks.Average([1.0, 5.0, 5.0, 9.0]);

        Assert.Equal([1.0, 2.5, 2.5, 4.0], ranks);
    }

    [Fact]
    public void Average_handles_a_tie_group_of_three_and_one_of_two()
    {
        double[] ranks = Ranks.Average([7.0, 7.0, 7.0, 2.0, 2.0, 9.0]);

        // The two 2.0s take ranks 1 and 2 -> 1.5; the three 7.0s take 3, 4, 5 -> 4.
        Assert.Equal([4.0, 4.0, 4.0, 1.5, 1.5, 6.0], ranks);
    }

    [Fact]
    public void Average_ranks_sum_to_n_times_n_plus_one_over_two_whatever_the_ties()
    {
        double[] ranks = Ranks.Average([3.0, 3.0, 3.0, 3.0, 1.0]);

        Assert.Equal(15.0, ranks.Sum(), 1e-12);
    }

    [Theory]
    [InlineData(new[] { 1.0, 2.0, 3.0 }, 0.0)]
    // One group of two: 2^3 - 2 = 6.
    [InlineData(new[] { 1.0, 5.0, 5.0, 9.0 }, 6.0)]
    // A group of three (24) and a group of two (6).
    [InlineData(new[] { 7.0, 7.0, 7.0, 2.0, 2.0, 9.0 }, 30.0)]
    public void TieCorrection_sums_t_cubed_minus_t_over_the_groups(double[] values, double expected)
    {
        Assert.Equal(expected, Ranks.TieCorrection(values), 1e-12);
    }

    [Fact]
    public void HasTies_answers_the_question_the_exact_branch_asks()
    {
        Assert.False(Ranks.HasTies([1.0, 2.0, 3.0]));
        Assert.True(Ranks.HasTies([1.0, 2.0, 2.0]));
    }

    [Fact]
    public void Average_refuses_an_empty_sample()
    {
        Assert.Throws<ArgumentException>(() => Ranks.Average([]));
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~RanksTests"`
Expected: FAIL — `The name 'Ranks' does not exist in the current context`.

- [ ] **Step 3: Write `src/Lodestar.Stats/Internal/Ranks.cs`**

```csharp
namespace Lodestar.Stats.Internal;

/// <summary>Mid-ranks, and the tie-correction term the rank tests share.</summary>
/// <remarks>
/// The ranks come back indexed by the input's own positions rather than sorted,
/// because every caller here sums the ranks belonging to one of two interleaved
/// samples and would otherwise have to invert the ordering itself.
/// </remarks>
internal static class Ranks
{
    internal static double[] Average(ReadOnlySpan<double> values)
    {
        if (values.Length == 0)
        {
            throw new ArgumentException("Cannot rank an empty sample.", nameof(values));
        }

        int n = values.Length;
        int[] order = new int[n];
        double[] sorted = new double[n];
        for (int i = 0; i < n; i++)
        {
            order[i] = i;
            sorted[i] = values[i];
        }

        Array.Sort(sorted, order);

        double[] ranks = new double[n];
        int start = 0;
        while (start < n)
        {
            int end = start;
            while (end + 1 < n && sorted[end + 1] == sorted[start])
            {
                end++;
            }

            // Ranks are 1-based, so the group spanning positions [start, end]
            // occupies ranks start+1 .. end+1 and every member takes their mean.
            double shared = (start + end + 2) / 2.0;
            for (int i = start; i <= end; i++)
            {
                ranks[order[i]] = shared;
            }

            start = end + 1;
        }

        return ranks;
    }

    internal static double TieCorrection(ReadOnlySpan<double> values)
    {
        if (values.Length == 0)
        {
            return 0.0;
        }

        double[] sorted = values.ToArray();
        Array.Sort(sorted);

        double correction = 0.0;
        int start = 0;
        while (start < sorted.Length)
        {
            int end = start;
            while (end + 1 < sorted.Length && sorted[end + 1] == sorted[start])
            {
                end++;
            }

            double t = end - start + 1;
            correction += (t * t * t) - t;
            start = end + 1;
        }

        return correction;
    }

    internal static bool HasTies(ReadOnlySpan<double> values)
    {
        if (values.Length < 2)
        {
            return false;
        }

        double[] sorted = values.ToArray();
        Array.Sort(sorted);
        for (int i = 1; i < sorted.Length; i++)
        {
            if (sorted[i] == sorted[i - 1])
            {
                return true;
            }
        }

        return false;
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~RanksTests"`
Expected: PASS, **11 tests**.

- [ ] **Step 5: Write the failing tests for `RankDistributions`**

`tests/Lodestar.Stats.Tests/Internal/RankDistributionsTests.cs`:

```csharp
using Lodestar.Stats.Internal;
using Xunit;

namespace Lodestar.Stats.Tests.Internal;

/// <summary>
/// The two exact null distributions, checked against counts a reader can verify
/// by hand and against the total each must sum to.
/// </summary>
public sealed class RankDistributionsTests
{
    [Fact]
    public void MannWhitney_counts_sum_to_the_number_of_arrangements()
    {
        // Choosing 4 of 9 positions: C(9,4) = 126 arrangements, and every one of
        // them lands on exactly one U value.
        double[] counts = RankDistributions.MannWhitneyCounts(4, 5);

        Assert.Equal(21, counts.Length);          // U ranges over [0, 20].
        Assert.Equal(126.0, counts.Sum(), 1e-9);
    }

    [Fact]
    public void MannWhitney_counts_are_symmetric_about_the_midpoint()
    {
        double[] counts = RankDistributions.MannWhitneyCounts(3, 5);

        for (int u = 0; u < counts.Length; u++)
        {
            Assert.Equal(counts[u], counts[counts.Length - 1 - u], 1e-9);
        }
    }

    [Fact]
    public void MannWhitney_two_by_two_is_the_hand_computable_case()
    {
        // n = m = 2: U in [0, 4] with counts 1, 1, 2, 1, 1 -- six arrangements.
        double[] counts = RankDistributions.MannWhitneyCounts(2, 2);

        Assert.Equal([1.0, 1.0, 2.0, 1.0, 1.0], counts);
    }

    [Fact]
    public void SignedRank_counts_sum_to_two_to_the_n()
    {
        double[] counts = RankDistributions.SignedRankCounts(6);

        Assert.Equal(22, counts.Length);          // W ranges over [0, 21].
        Assert.Equal(64.0, counts.Sum(), 1e-9);
    }

    [Fact]
    public void SignedRank_three_is_the_hand_computable_case()
    {
        // Ranks 1, 2, 3: the subset sums are 0,1,2,3,3,4,5,6 -- so W = 3 twice.
        double[] counts = RankDistributions.SignedRankCounts(3);

        Assert.Equal([1.0, 1.0, 1.0, 2.0, 1.0, 1.0, 1.0], counts);
    }

    [Fact]
    public void SignedRank_counts_are_symmetric_about_the_midpoint()
    {
        double[] counts = RankDistributions.SignedRankCounts(7);

        for (int w = 0; w < counts.Length; w++)
        {
            Assert.Equal(counts[w], counts[counts.Length - 1 - w], 1e-9);
        }
    }

    [Fact]
    public void SignedRank_of_zero_is_the_single_empty_assignment()
    {
        Assert.Equal([1.0], RankDistributions.SignedRankCounts(0));
    }

    [Fact]
    public void Both_refuse_a_negative_size()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RankDistributions.MannWhitneyCounts(-1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => RankDistributions.SignedRankCounts(-1));
    }
}
```

- [ ] **Step 6: Run to verify it fails**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~RankDistributionsTests"`
Expected: FAIL — `The name 'RankDistributions' does not exist in the current context`.

- [ ] **Step 7: Write `src/Lodestar.Stats/Internal/RankDistributions.cs`**

```csharp
namespace Lodestar.Stats.Internal;

/// <summary>The exact null distributions of the two rank statistics.</summary>
/// <remarks>
/// Both are counted by dynamic programming rather than by enumerating the
/// arrangements: the signed-rank distribution for n = 25 has 2^25 assignments,
/// which is 33 million enumerations against 325 table entries.
///
/// The counts are doubles because they exceed a long: the signed-rank total for
/// n = 60 is 2^60. Only ratios against the total are ever taken, so a 53-bit
/// mantissa loses nothing a p-value compared at 1e-9 relative can see.
/// </remarks>
internal static class RankDistributions
{
    /// <summary>
    /// How many arrangements of two samples of size <paramref name="n"/> and
    /// <paramref name="m"/> give each value of the Mann-Whitney U statistic.
    /// </summary>
    internal static double[] MannWhitneyCounts(int n, int m)
    {
        if (n < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, "A sample size cannot be negative.");
        }
        if (m < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(m), m, "A sample size cannot be negative.");
        }

        // f(i, j, u): arrangements of i of the first sample and j of the second
        // with statistic u. Rolling the i dimension keeps the table at
        // (m + 1) x (n*m + 1) instead of cubing it.
        int max = n * m;
        double[,] previous = new double[m + 1, max + 1];
        for (int j = 0; j <= m; j++)
        {
            previous[j, 0] = 1.0;
        }

        for (int i = 1; i <= n; i++)
        {
            double[,] current = new double[m + 1, max + 1];
            current[0, 0] = 1.0;

            for (int j = 1; j <= m; j++)
            {
                for (int u = 0; u <= max; u++)
                {
                    // Either the next largest value comes from the first sample,
                    // which adds j to the statistic, or from the second, which
                    // adds nothing.
                    double fromFirst = u >= j ? previous[j, u - j] : 0.0;
                    current[j, u] = fromFirst + current[j - 1, u];
                }
            }

            previous = current;
        }

        double[] counts = new double[max + 1];
        for (int u = 0; u <= max; u++)
        {
            counts[u] = previous[m, u];
        }

        return counts;
    }

    /// <summary>
    /// How many sign assignments of the ranks <c>1..n</c> give each value of the
    /// positive-rank sum W.
    /// </summary>
    internal static double[] SignedRankCounts(int n)
    {
        if (n < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, "A sample size cannot be negative.");
        }

        int max = n * (n + 1) / 2;
        double[] counts = new double[max + 1];
        counts[0] = 1.0;

        // Multiplying by (1 + x^rank) one rank at a time, in place: descending
        // so a rank is never counted twice within its own pass.
        int reach = 0;
        for (int rank = 1; rank <= n; rank++)
        {
            reach += rank;
            for (int w = reach; w >= rank; w--)
            {
                counts[w] += counts[w - rank];
            }
        }

        return counts;
    }
}
```

- [ ] **Step 8: Run to verify it passes**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~RankDistributionsTests"`
Expected: PASS, **8 tests**.

- [ ] **Step 9: Run the gates and commit**

```bash
dotnet build Lodestar.slnx -c Release
dotnet format Lodestar.slnx --verify-no-changes
python3 tools/check_comment_length.py
git add src/Lodestar.Stats/Internal tests/Lodestar.Stats.Tests/Internal
git commit -m "Lodestar.Stats: mid-ranks and the two exact rank distributions

Refs #442. Ranks come back indexed by the input's own positions, because every
caller sums the ranks belonging to one of two interleaved samples. The two null
distributions are counted by dynamic programming rather than enumerated: the
signed-rank distribution for n = 25 has 33 million assignments against 325
table entries.

Counts are doubles, not longs: 2^60 assignments for n = 60 overflows a long
silently, and only ratios against the total are ever taken."
```

---

### Task 4: The ten oracle corpora, and the relative comparison that makes them mean something

**Files:**

- Modify: `tools/generate_oracles.py` — add a `# --- Hypothesis tests (#442) ---`
  section before `def main()`, and ten entries to the `generators` dict
- Create: `tests/oracles/stats_ttest.json`, `stats_mannwhitney.json`,
  `stats_wilcoxon.json`, `stats_chisquare.json`, `stats_fisher.json`,
  `stats_ks.json`, `stats_anova.json`, `stats_kruskal.json`,
  `stats_shapiro.json`, `stats_multiple_comparisons.json` (all generated, all committed)
- Create: `tests/Lodestar.Stats.Tests/Oracles/StatsCorpus.cs`,
  `tests/Lodestar.Stats.Tests/Oracles/StatsOracleAsserts.cs`
- Create: `tests/Lodestar.Stats.Tests/Oracles/CorpusIdentityTests.cs`
- Modify: `docs/equivalence.md` is **not** touched here — it lands with the families

**Interfaces:**

- Consumes: nothing.
- Produces: `StatsCorpus.Load(string fileName)` returning a `JsonDocument`;
  `StatsOracleAsserts.Statistic(double expected, double actual, string caseName)`
  and `StatsOracleAsserts.PValue(double expected, double actual, string caseName)`.

**The corpus shape**, one file per family:

```json
{
 "metadata": {
  "library": "scipy",
  "version": "1.18.0",
  "family": "ttest",
  "count": 42
 },
 "cases": [
  {
   "name": "independent, equal variance, two-sided",
   "call": "ttest_ind",
   "args": {"equal_var": true, "alternative": "two-sided"},
   "a": [1.0, 2.0],
   "b": [3.0, 4.0],
   "statistic": -2.828,
   "pvalue": 0.105,
   "df": 2.0
  }
 ]
}
```

`args` is written out even where it repeats the default. That is the point: a
case's arguments are data, so a scipy release that moves a default fails the
*Oracles are reproducible* job instead of moving a frozen number quietly.

- [ ] **Step 1: Write the shared generator helpers**

Add to `tools/generate_oracles.py`, immediately before `def main()`:

```python
# --- Hypothesis tests (#442) ----------------------------------------------

STATS_LIBRARY = "scipy"
CASES = "cases"
PVALUE = "pvalue"
STATISTIC = "statistic"


def _stats_metadata(family: str, count: int) -> dict:
    """The identity block every stats corpus carries.

    The version is read from the installed distribution rather than written
    down, so a corpus regenerated against a different scipy declares that fact
    instead of silently replacing numbers under the old version's name.
    """
    return {
        "library": STATS_LIBRARY,
        "version": version(STATS_LIBRARY),
        "family": family,
        "count": count,
    }


def _stats_number(value: float) -> float | str:
    """A corpus number, with the three non-finite values spelled as strings.

    main() writes with allow_nan=False, so a bare Infinity would abort the whole
    generation. Two of these corpora produce one legitimately: a one-sided
    t-test's confidence interval is half-open, and Fisher's odds ratio is
    infinite when a diagonal of the table is zero. The spelling is the one
    tools/generate_oracles.py already uses for the ROC thresholds.

    No rounding here, unlike the metrics encoder: a p-value at 1e-53 has to
    survive the round trip to every bit the relative comparison checks.
    """
    if math.isnan(value):
        return "NaN"
    if math.isinf(value):
        return "Infinity" if value > 0 else "-Infinity"
    return float(value)


def _stats_samples() -> list[dict]:
    """Sample pairs that between them exercise every branch the tests have.

    Balanced and unbalanced, tied and untied, small enough for the exact branch
    and large enough for the asymptotic one, plus one pair separated far enough
    that the p-value lands below 1e-15 -- which is where an absolute tolerance
    would stop proving anything.
    """
    rng = SeededRandom(SEED + 442)
    tiny_a = [1.0, 4.0, 7.0, 9.0]
    tiny_b = [2.0, 3.0, 8.0, 12.0, 15.0]
    tied_a = [1.0, 2.0, 2.0, 3.0, 5.0, 5.0]
    tied_b = [2.0, 3.0, 3.0, 4.0, 5.0, 7.0]
    wide_a = [round(rng.gauss(0.0, 1.0), 6) for _ in range(40)]
    wide_b = [round(rng.gauss(3.0, 1.0), 6) for _ in range(40)]
    return [
        {"name": "small and untied, exact branch reachable", "a": tiny_a, "b": tiny_b},
        {"name": "ties in both samples, auto falls back to asymptotic", "a": tied_a, "b": tied_b},
        {"name": "unbalanced, one sample twice the other",
         "a": tiny_a, "b": tiny_b + [20.0, 22.0, 25.0, 30.0, 33.0]},
        {"name": "well separated, p-value below 1e-15", "a": wide_a, "b": wide_b},
    ]


def _stats_paired() -> list[dict]:
    """Paired samples, including the zero differences Wilcoxon's zero_method is about."""
    rng = SeededRandom(SEED + 443)
    drifted = [round(rng.gauss(0.0, 1.0), 6) for _ in range(30)]
    return [
        {"name": "no zero differences", "x": [1.0, 3.0, 5.0, 7.0, 9.0, 11.0],
         "y": [2.0, 3.5, 4.0, 8.0, 8.5, 13.0]},
        {"name": "two zero differences", "x": [1.0, 3.0, 5.0, 7.0, 9.0, 11.0],
         "y": [1.0, 3.5, 5.0, 8.0, 8.5, 13.0]},
        {"name": "thirty pairs, asymptotic branch", "x": drifted,
         "y": [v + 0.8 for v in drifted[:15]] + [v - 0.1 for v in drifted[15:]]},
    ]
```

`SeededRandom` and `SEED` already exist in this file; `version` is already
imported from `importlib.metadata`. Reusing them is what keeps a regenerated
corpus byte-identical.

- [ ] **Step 2: Write the ten generator functions**

Still in `tools/generate_oracles.py`, after the helpers:

```python
def generate_stats_ttest() -> dict:
    """Student, Welch, paired and one-sample t, against scipy.stats (#442)."""
    from scipy import stats as sps

    cases: list[dict] = []
    for fx in _stats_samples():
        for equal_var in (True, False):
            for alternative in ("two-sided", "less", "greater"):
                r = sps.ttest_ind(fx["a"], fx["b"], equal_var=equal_var,
                                  alternative=alternative)
                low, high = r.confidence_interval(0.95)
                cases.append({
                    "name": f"{fx['name']} | ind | equal_var={equal_var} | {alternative}",
                    "call": "ttest_ind",
                    "args": {"equal_var": equal_var, "alternative": alternative},
                    "a": fx["a"], "b": fx["b"],
                    STATISTIC: _stats_number(r.statistic), PVALUE: float(r.pvalue),
                    "df": float(r.df),
                    "ci_low": _stats_number(low), "ci_high": _stats_number(high),
                })

    for fx in _stats_paired():
        for alternative in ("two-sided", "less", "greater"):
            r = sps.ttest_rel(fx["x"], fx["y"], alternative=alternative)
            low, high = r.confidence_interval(0.95)
            cases.append({
                "name": f"{fx['name']} | rel | {alternative}",
                "call": "ttest_rel",
                "args": {"alternative": alternative},
                "a": fx["x"], "b": fx["y"],
                STATISTIC: _stats_number(r.statistic), PVALUE: float(r.pvalue),
                "df": float(r.df),
                "ci_low": _stats_number(low), "ci_high": _stats_number(high),
            })

    for fx in _stats_samples():
        for popmean in (0.0, 5.0):
            for alternative in ("two-sided", "less", "greater"):
                r = sps.ttest_1samp(fx["a"], popmean, alternative=alternative)
                low, high = r.confidence_interval(0.95)
                cases.append({
                    "name": f"{fx['name']} | 1samp | mean={popmean} | {alternative}",
                    "call": "ttest_1samp",
                    "args": {"popmean": popmean, "alternative": alternative},
                    "a": fx["a"], "b": [],
                    STATISTIC: _stats_number(r.statistic), PVALUE: float(r.pvalue),
                    "df": float(r.df),
                    "ci_low": _stats_number(low), "ci_high": _stats_number(high),
                })

    return {"metadata": _stats_metadata("ttest", len(cases)), CASES: cases}


def generate_stats_mannwhitney() -> dict:
    """Mann-Whitney U, over both continuity settings and all three methods (#442)."""
    from scipy import stats as sps

    cases: list[dict] = []
    for fx in _stats_samples():
        for use_continuity in (True, False):
            for method in ("auto", "asymptotic"):
                for alternative in ("two-sided", "less", "greater"):
                    r = sps.mannwhitneyu(fx["a"], fx["b"], use_continuity=use_continuity,
                                         alternative=alternative, method=method)
                    cases.append({
                        "name": f"{fx['name']} | continuity={use_continuity} | "
                                f"{method} | {alternative}",
                        "call": "mannwhitneyu",
                        "args": {"use_continuity": use_continuity,
                                 "alternative": alternative, "method": method},
                        "a": fx["a"], "b": fx["b"],
                        STATISTIC: float(r.statistic), PVALUE: float(r.pvalue),
                    })

    # The exact branch asked for explicitly, including on tied data: measured,
    # scipy computes there rather than refusing, and parity means matching that.
    for fx in _stats_samples()[:3]:
        for alternative in ("two-sided", "less", "greater"):
            r = sps.mannwhitneyu(fx["a"], fx["b"], use_continuity=True,
                                 alternative=alternative, method="exact")
            cases.append({
                "name": f"{fx['name']} | exact | {alternative}",
                "call": "mannwhitneyu",
                "args": {"use_continuity": True, "alternative": alternative,
                         "method": "exact"},
                "a": fx["a"], "b": fx["b"],
                STATISTIC: float(r.statistic), PVALUE: float(r.pvalue),
            })

    return {"metadata": _stats_metadata("mannwhitney", len(cases)), CASES: cases}


def generate_stats_wilcoxon() -> dict:
    """Wilcoxon signed-rank, over the three zero methods (#442)."""
    from scipy import stats as sps

    cases: list[dict] = []
    for fx in _stats_paired():
        for zero_method in ("wilcox", "pratt", "zsplit"):
            for correction in (False, True):
                for method in ("auto", "asymptotic"):
                    for alternative in ("two-sided", "less", "greater"):
                        r = sps.wilcoxon(fx["x"], fx["y"], zero_method=zero_method,
                                         correction=correction, alternative=alternative,
                                         method=method)
                        cases.append({
                            "name": f"{fx['name']} | {zero_method} | "
                                    f"correction={correction} | {method} | {alternative}",
                            "call": "wilcoxon",
                            "args": {"zero_method": zero_method, "correction": correction,
                                     "alternative": alternative, "method": method},
                            "x": fx["x"], "y": fx["y"],
                            STATISTIC: float(r.statistic), PVALUE: float(r.pvalue),
                        })

    return {"metadata": _stats_metadata("wilcoxon", len(cases)), CASES: cases}


def generate_stats_chisquare() -> dict:
    """Goodness of fit and contingency, with Yates on and off (#442)."""
    from scipy import stats as sps

    goodness = [
        {"name": "uniform expectation, six categories",
         "observed": [16.0, 18.0, 16.0, 14.0, 12.0, 12.0], "expected": []},
        {"name": "explicit expectation",
         "observed": [16.0, 18.0, 16.0, 14.0, 12.0, 12.0],
         "expected": [16.0, 16.0, 16.0, 16.0, 16.0, 16.0]},
        {"name": "a far tail, p below 1e-15",
         "observed": [200.0, 10.0, 10.0, 10.0], "expected": []},
    ]
    tables = [
        {"name": "2x2, Yates applies", "table": [[10.0, 20.0], [30.0, 40.0]]},
        {"name": "2x2, strong association", "table": [[100.0, 10.0], [10.0, 100.0]]},
        {"name": "3x2, Yates does not apply",
         "table": [[10.0, 20.0], [30.0, 40.0], [15.0, 5.0]]},
        {"name": "3x3", "table": [[10.0, 20.0, 30.0], [30.0, 40.0, 10.0], [5.0, 15.0, 25.0]]},
    ]

    cases: list[dict] = []
    for fx in goodness:
        expected = fx["expected"] or None
        r = sps.chisquare(fx["observed"], f_exp=expected)
        cases.append({
            "name": f"{fx['name']} | chisquare",
            "call": "chisquare",
            "args": {"f_exp": fx["expected"]},
            "observed": fx["observed"], "expected_input": fx["expected"],
            STATISTIC: float(r.statistic), PVALUE: float(r.pvalue),
        })

    for fx in tables:
        for correction in (True, False):
            r = sps.chi2_contingency(np.array(fx["table"]), correction=correction)
            cases.append({
                "name": f"{fx['name']} | correction={correction}",
                "call": "chi2_contingency",
                "args": {"correction": correction},
                "table": fx["table"],
                STATISTIC: float(r.statistic), PVALUE: float(r.pvalue),
                "dof": int(r.dof),
                "expected_freq": [[float(v) for v in row] for row in r.expected_freq],
            })

    return {"metadata": _stats_metadata("chisquare", len(cases)), CASES: cases}


def generate_stats_fisher() -> dict:
    """Fisher's exact test on 2x2 tables, all three alternatives (#442)."""
    from scipy import stats as sps

    tables = [
        {"name": "Fisher's tea tasting", "table": [[3, 1], [1, 3]]},
        {"name": "a zero cell", "table": [[8, 2], [1, 5]]},
        {"name": "two zero cells", "table": [[5, 0], [0, 5]]},
        {"name": "large counts", "table": [[100, 40], [35, 120]]},
        {"name": "an empty row is refused by the C# side", "table": [[7, 3], [2, 9]]},
    ]

    cases: list[dict] = []
    for fx in tables:
        for alternative in ("two-sided", "less", "greater"):
            r = sps.fisher_exact(np.array(fx["table"]), alternative=alternative)
            cases.append({
                "name": f"{fx['name']} | {alternative}",
                "call": "fisher_exact",
                "args": {"alternative": alternative},
                "table": fx["table"],
                # The odds ratio is infinite when a diagonal is zero, which the
                # "two zero cells" fixture is there to reach.
                STATISTIC: _stats_number(r.statistic), PVALUE: float(r.pvalue),
            })

    return {"metadata": _stats_metadata("fisher", len(cases)), CASES: cases}


def generate_stats_ks() -> dict:
    """Two-sample Kolmogorov-Smirnov, exact and asymptotic (#442)."""
    from scipy import stats as sps

    cases: list[dict] = []
    for fx in _stats_samples():
        for method in ("auto", "asymp", "exact"):
            for alternative in ("two-sided", "less", "greater"):
                r = sps.ks_2samp(fx["a"], fx["b"], alternative=alternative, method=method)
                cases.append({
                    "name": f"{fx['name']} | {method} | {alternative}",
                    "call": "ks_2samp",
                    "args": {"alternative": alternative, "method": method},
                    "a": fx["a"], "b": fx["b"],
                    STATISTIC: float(r.statistic), PVALUE: float(r.pvalue),
                    "statistic_location": float(r.statistic_location),
                    "statistic_sign": int(r.statistic_sign),
                })

    return {"metadata": _stats_metadata("ks", len(cases)), CASES: cases}


def _stats_groups() -> list[dict]:
    """Group sets for the two k-sample tests."""
    rng = SeededRandom(SEED + 444)
    return [
        {"name": "three balanced groups",
         "groups": [[1.0, 2.0, 3.0, 4.0], [2.0, 3.0, 4.0, 5.0], [5.0, 6.0, 7.0, 8.0]]},
        {"name": "unbalanced groups",
         "groups": [[1.0, 2.0], [2.0, 3.0, 4.0, 5.0, 6.0], [5.0, 6.0, 7.0]]},
        {"name": "ties across groups",
         "groups": [[1.0, 2.0, 2.0], [2.0, 2.0, 3.0], [3.0, 3.0, 4.0]]},
        {"name": "three separated groups, p below 1e-15",
         "groups": [[round(rng.gauss(m, 1.0), 6) for _ in range(30)] for m in (0.0, 4.0, 8.0)]},
    ]


def generate_stats_anova() -> dict:
    """One-way ANOVA, against scipy.stats.f_oneway (#442)."""
    from scipy import stats as sps

    cases = []
    for fx in _stats_groups():
        r = sps.f_oneway(*[np.array(g) for g in fx["groups"]])
        cases.append({
            "name": fx["name"], "call": "f_oneway", "args": {},
            "groups": fx["groups"],
            STATISTIC: float(r.statistic), PVALUE: float(r.pvalue),
        })

    return {"metadata": _stats_metadata("anova", len(cases)), CASES: cases}


def generate_stats_kruskal() -> dict:
    """Kruskal-Wallis, against scipy.stats.kruskal (#442)."""
    from scipy import stats as sps

    cases = []
    for fx in _stats_groups():
        r = sps.kruskal(*[np.array(g) for g in fx["groups"]])
        cases.append({
            "name": fx["name"], "call": "kruskal", "args": {},
            "groups": fx["groups"],
            STATISTIC: float(r.statistic), PVALUE: float(r.pvalue),
        })

    return {"metadata": _stats_metadata("kruskal", len(cases)), CASES: cases}


def generate_stats_shapiro() -> dict:
    """Shapiro-Wilk, against scipy.stats.shapiro (#442)."""
    from scipy import stats as sps

    rng = SeededRandom(SEED + 445)
    samples = [
        {"name": "seven normal draws, the smallest n Royston covers",
         "x": [round(rng.gauss(0.0, 1.0), 6) for _ in range(7)]},
        {"name": "twenty normal draws",
         "x": [round(rng.gauss(0.0, 1.0), 6) for _ in range(20)]},
        {"name": "fifty normal draws",
         "x": [round(rng.gauss(0.0, 1.0), 6) for _ in range(50)]},
        {"name": "two hundred exponential draws, p below 1e-15",
         "x": [round(rng.expovariate(1.0), 6) for _ in range(200)]},
        {"name": "a sample with ties",
         "x": [1.0, 1.0, 2.0, 2.0, 3.0, 3.0, 4.0, 5.0, 9.0, 9.0]},
    ]

    cases = []
    for fx in samples:
        r = sps.shapiro(np.array(fx["x"]))
        cases.append({
            "name": fx["name"], "call": "shapiro", "args": {},
            "x": fx["x"],
            STATISTIC: float(r.statistic), PVALUE: float(r.pvalue),
        })

    return {"metadata": _stats_metadata("shapiro", len(cases)), CASES: cases}


def generate_stats_multiple_comparisons() -> dict:
    """Benjamini-Hochberg and Benjamini-Yekutieli from scipy; Bonferroni from its definition.

    scipy has no Bonferroni, and adding statsmodels to reach one would widen the
    surface generate_oracles.py depends on for a rule that is min(p * n, 1). The
    corpus states the definition instead, the way #526 generated the BK-tree
    corpus by brute force rather than by a second library.
    """
    from scipy import stats as sps

    families = [
        {"name": "four p-values, one clearly significant", "p": [0.01, 0.02, 0.2, 0.5]},
        {"name": "already sorted, all small", "p": [0.001, 0.008, 0.039, 0.041, 0.042]},
        {"name": "unsorted, with a tie", "p": [0.3, 0.02, 0.02, 0.9, 0.001]},
        {"name": "a single p-value", "p": [0.04]},
        {"name": "everything at one", "p": [1.0, 1.0, 1.0]},
    ]

    cases = []
    for fx in families:
        n = len(fx["p"])
        cases.append({
            "name": fx["name"], "call": "false_discovery_control", "args": {},
            "p": fx["p"],
            "bonferroni": [min(p * n, 1.0) for p in fx["p"]],
            "bh": [float(v) for v in sps.false_discovery_control(np.array(fx["p"]), method="bh")],
            "by": [float(v) for v in sps.false_discovery_control(np.array(fx["p"]), method="by")],
        })

    return {"metadata": _stats_metadata("multiple_comparisons", len(cases)), CASES: cases}
```

- [ ] **Step 3: Register the ten corpora**

In `main()`'s `generators` dict, after `"bpe_prefix_space.json": generate_bpe_prefix_space,`:

```python
        "stats_ttest.json": generate_stats_ttest,
        "stats_mannwhitney.json": generate_stats_mannwhitney,
        "stats_wilcoxon.json": generate_stats_wilcoxon,
        "stats_chisquare.json": generate_stats_chisquare,
        "stats_fisher.json": generate_stats_fisher,
        "stats_ks.json": generate_stats_ks,
        "stats_anova.json": generate_stats_anova,
        "stats_kruskal.json": generate_stats_kruskal,
        "stats_shapiro.json": generate_stats_shapiro,
        "stats_multiple_comparisons.json": generate_stats_multiple_comparisons,
```

- [ ] **Step 4: Generate the corpora, from a neutral directory, reading the generator's own exit code**

```bash
REPO=$(git rev-parse --show-toplevel)
SCRATCH=$(mktemp -d)
cd "$SCRATCH" && PYTHONSAFEPATH=1 "$REPO/.venv-oracles/bin/python" \
  "$REPO/tools/generate_oracles.py" > "$SCRATCH/gen.log" 2>&1
echo "generator exit: $?"
grep '^stats_' "$SCRATCH/gen.log"
```

Expected: `generator exit: 0`, and ten `stats_*.json: N cases -> …` lines.
**Do not pipe the generator into `tail`** — the pipeline's status is `tail`'s,
and a failed generation then reads as success.

- [ ] **Step 5: Confirm the run is a fixed point**

```bash
git -C "$REPO" status --short tests/oracles
cd "$SCRATCH" && PYTHONSAFEPATH=1 "$REPO/.venv-oracles/bin/python" \
  "$REPO/tools/generate_oracles.py" > "$SCRATCH/gen2.log" 2>&1
echo "second run exit: $?"
git -C "$REPO" status --short tests/oracles
```

`$REPO` and `$SCRATCH` are the ones step 4 set; this step continues that shell.

Expected: the first `git status` lists exactly the ten new files and **nothing
else** — a pre-existing corpus that moved means a shared helper was perturbed
and must be put back. The second run must leave the ten files unchanged.

- [ ] **Step 6: Write `tests/Lodestar.Stats.Tests/Oracles/StatsCorpus.cs`**

```csharp
using System.Text.Json;

namespace Lodestar.Stats.Tests.Oracles;

/// <summary>Loads a frozen stats corpus committed under <c>tests/oracles/</c>.</summary>
internal static class StatsCorpus
{
    internal static JsonDocument Load(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "oracles", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Oracle '{fileName}' not found at '{path}'. Run tools/generate_oracles.py.", path);
        }

        return JsonDocument.Parse(File.ReadAllText(path));
    }

    /// <summary>One corpus number, decoding the three non-finite spellings.</summary>
    /// <remarks>
    /// The generator writes with <c>allow_nan=False</c>, so a one-sided
    /// confidence bound and an infinite odds ratio arrive as the strings
    /// <c>"Infinity"</c>, <c>"-Infinity"</c> and <c>"NaN"</c> rather than as
    /// tokens no strict JSON reader accepts.
    /// </remarks>
    internal static double Number(JsonElement element) =>
        element.ValueKind == JsonValueKind.String
            ? element.GetString() switch
            {
                "Infinity" => double.PositiveInfinity,
                "-Infinity" => double.NegativeInfinity,
                "NaN" => double.NaN,
                var other => throw new InvalidDataException($"Unknown corpus number '{other}'."),
            }
            : element.GetDouble();

    internal static double[] Doubles(JsonElement element) =>
        [.. element.EnumerateArray().Select(Number)];

    internal static double[][] Table(JsonElement element) =>
        [.. element.EnumerateArray().Select(Doubles)];
}
```

- [ ] **Step 7: Write `tests/Lodestar.Stats.Tests/Oracles/StatsOracleAsserts.cs`**

```csharp
using Xunit;

namespace Lodestar.Stats.Tests.Oracles;

/// <summary>The two comparisons a hypothesis-test corpus needs, and why they differ.</summary>
/// <remarks>
/// A statistic lives on a scale the data sets, so the repository's 1e-9
/// absolute tolerance is the right one for it. A p-value does not: measured on
/// ordinary corpus cases it reaches 7.85e-26 for a t-test and 2.38e-53 for an
/// ANOVA, and at 1e-9 absolute an implementation returning 0.0 would pass every
/// one of them. The tail is exactly where a hand-written incomplete beta goes
/// wrong, so the tail is compared relatively.
/// </remarks>
internal static class StatsOracleAsserts
{
    private const double Tolerance = 1e-9;

    internal static void Statistic(double expected, double actual, string caseName)
    {
        if (double.IsNaN(expected))
        {
            Assert.True(double.IsNaN(actual), $"{caseName}: expected NaN, got {actual}.");
            return;
        }

        // Fisher's odds ratio is infinite when a diagonal of the table is zero,
        // and a one-sided confidence bound is half-open: both must match sign
        // and infinitude exactly rather than come within a tolerance of it.
        if (double.IsInfinity(expected))
        {
            Assert.True(
                actual == expected,
                $"{caseName}: expected {expected}, got {actual}.");
            return;
        }

        Assert.True(
            Math.Abs(expected - actual) <= Tolerance,
            $"{caseName}: statistic {actual} is not within {Tolerance} of {expected}.");
    }

    internal static void PValue(double expected, double actual, string caseName)
    {
        if (double.IsNaN(expected))
        {
            Assert.True(double.IsNaN(actual), $"{caseName}: expected NaN, got {actual}.");
            return;
        }

        // An exact zero has no relative neighbourhood, so it is the one value
        // compared absolutely -- and only an exact zero satisfies it.
        if (expected == 0.0)
        {
            Assert.True(actual == 0.0, $"{caseName}: expected an exact zero, got {actual}.");
            return;
        }

        double relative = Math.Abs(expected - actual) / Math.Abs(expected);
        Assert.True(
            relative <= Tolerance,
            $"{caseName}: p-value {actual} differs from {expected} by {relative} relative, " +
            $"which exceeds {Tolerance}.");
    }

    internal static void Vector(double[] expected, double[] actual, string caseName)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            PValue(expected[i], actual[i], $"{caseName}[{i}]");
        }
    }
}
```

- [ ] **Step 8: Write the corpus-identity test**

`tests/Lodestar.Stats.Tests/Oracles/CorpusIdentityTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>
/// Every corpus declares its library, its version and its case count, and the
/// count matches what the file holds.
/// </summary>
/// <remarks>
/// Without this an empty <c>cases</c> array passes as green: each family's
/// replay would iterate nothing and assert nothing. The shape #313 established.
/// </remarks>
public sealed class CorpusIdentityTests
{
    public static TheoryData<string, string> Corpora => new()
    {
        { "stats_ttest.json", "ttest" },
        { "stats_mannwhitney.json", "mannwhitney" },
        { "stats_wilcoxon.json", "wilcoxon" },
        { "stats_chisquare.json", "chisquare" },
        { "stats_fisher.json", "fisher" },
        { "stats_ks.json", "ks" },
        { "stats_anova.json", "anova" },
        { "stats_kruskal.json", "kruskal" },
        { "stats_shapiro.json", "shapiro" },
        { "stats_multiple_comparisons.json", "multiple_comparisons" },
    };

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Corpus_declares_scipy_its_version_its_family_and_a_matching_count(
        string fileName, string family)
    {
        using JsonDocument document = StatsCorpus.Load(fileName);
        JsonElement metadata = document.RootElement.GetProperty("metadata");

        Assert.Equal("scipy", metadata.GetProperty("library").GetString());
        Assert.Equal("1.18.0", metadata.GetProperty("version").GetString());
        Assert.Equal(family, metadata.GetProperty("family").GetString());

        int declared = metadata.GetProperty("count").GetInt32();
        int actual = document.RootElement.GetProperty("cases").GetArrayLength();

        Assert.Equal(declared, actual);
        Assert.True(actual > 0, $"{fileName} holds no cases.");
    }

    [Theory]
    [MemberData(nameof(Corpora))]
    public void Every_case_records_the_arguments_it_was_generated_with(
        string fileName, string family)
    {
        using JsonDocument document = StatsCorpus.Load(fileName);

        foreach (JsonElement testCase in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            // The arguments are data, not a default the replay may assume: this
            // is what makes a scipy upgrade that moves a default fail loudly.
            Assert.True(testCase.TryGetProperty("args", out _), $"{family}: a case has no args.");
            Assert.True(testCase.TryGetProperty("call", out _), $"{family}: a case has no call.");
            Assert.False(
                string.IsNullOrWhiteSpace(testCase.GetProperty("name").GetString()),
                $"{family}: a case has no name.");
        }
    }
}
```

- [ ] **Step 9: Run the identity tests**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~CorpusIdentityTests"`
Expected: PASS, **20 tests** — ten corpora × two facts. A count of 2 means
`MemberData` did not expand; a count of 0 means the filter matched nothing.

- [ ] **Step 10: Confirm the corpora reach the tail the tolerance exists for**

```bash
python3 - <<'PY'
import json, pathlib
for path in sorted(pathlib.Path("tests/oracles").glob("stats_*.json")):
    data = json.loads(path.read_text())
    ps = [c["pvalue"] for c in data["cases"] if "pvalue" in c]
    if ps:
        print(f"{path.name:34s} min p = {min(ps):.3e}  cases = {len(data['cases'])}")
PY
```

Expected: at least four of the corpora report a minimum p-value below `1e-15`.
If none does, the fixtures are not separated enough and the relative tolerance
is proving nothing it could not prove absolutely — widen the separated fixture
in `_stats_samples` until one does.

- [ ] **Step 11: Run the gates and commit**

```bash
dotnet build Lodestar.slnx -c Release
dotnet format Lodestar.slnx --verify-no-changes
python3 tools/check_comment_length.py
python3 tools/check_machine_paths.py --no-environment
git add tools/generate_oracles.py tests/oracles/stats_*.json tests/Lodestar.Stats.Tests/Oracles
git commit -m "Lodestar.Stats: the ten scipy corpora and the relative p-value comparison

Refs #442. Each case carries the full argument set it was generated with, even
where that repeats a scipy default: equal_var=True means Student and not Welch,
correction=True applies Yates to 2x2 tables only, and method='auto' flips
between exact and asymptotic on size and ties. A scipy release that moves one of
those now fails the Oracles are reproducible job instead of moving a frozen
number quietly.

p-values are compared at 1e-9 relative, statistics at 1e-9 absolute. Measured,
ordinary cases reach 7.85e-26 and 2.38e-53, where an absolute check would accept
an implementation that returns zero. tools/compare_oracles.py is unchanged:
corpus reproducibility is its subject, not assertion strength."
```

---

### Task 5: `TTest` — Student, Welch, paired and one-sample

**Files:**

- Create: `src/Lodestar.Stats/TTest.cs`
- Modify: `src/Lodestar.Stats/Internal/Beta.cs` — add `StudentQuantile`
- Modify: `src/Lodestar.Stats/TestResult.cs` — fill in `ConfidenceInterval`
- Test: `tests/Lodestar.Stats.Tests/TTestOracleTests.cs`,
  `tests/Lodestar.Stats.Tests/TTestEdgeTests.cs`,
  `tests/Lodestar.Stats.Tests/Internal/StudentQuantileTests.cs`

**Interfaces:**

- Consumes: `Beta.StudentSf(double t, double df)`; `TTestResult` with its
  internal `Estimate`, `StandardError` and `Alternative`; the `Alternative` and
  `Variance` enums; `StatsOracleAsserts`; `StatsCorpus`.
- Produces:
  - `TTestResult TTest.Independent(ReadOnlySpan<double> a, ReadOnlySpan<double> b, Alternative alternative = Alternative.TwoSided, Variance variance = Variance.Welch)`
  - `TTestResult TTest.Paired(ReadOnlySpan<double> a, ReadOnlySpan<double> b, Alternative alternative = Alternative.TwoSided)`
  - `TTestResult TTest.OneSample(ReadOnlySpan<double> sample, double populationMean, Alternative alternative = Alternative.TwoSided)`
  - `double Beta.StudentQuantile(double p, double df)` — the value `t` with `P(T > t) = p`

**The one deliberate divergence from scipy.** `Independent` defaults to
`Variance.Welch`; scipy's `ttest_ind` defaults to `equal_var=True`, which is
Student. Pooling is only correct when the two population variances really are
equal, and a default that is wrong in the common case is worse than a default
that costs a word at the call site. The corpus covers both, and this divergence
gets a row in `docs/equivalence.md` and its own paragraph in the reference page.

- [ ] **Step 1: Write the failing test for `StudentQuantile`**

`tests/Lodestar.Stats.Tests/Internal/StudentQuantileTests.cs`:

```csharp
using Lodestar.Stats.Internal;
using Xunit;

namespace Lodestar.Stats.Tests.Internal;

/// <summary>The inverse of the Student upper tail, which is what a confidence interval needs.</summary>
public sealed class StudentQuantileTests
{
    [Theory]
    [InlineData(0.5, 1.0)]
    [InlineData(0.5, 30.0)]
    [InlineData(0.025, 1.0)]
    [InlineData(0.025, 12.0)]
    [InlineData(0.025, 12.7431)]
    [InlineData(1e-12, 8.0)]
    [InlineData(0.999, 3.0)]
    public void Quantile_inverts_the_tail(double p, double df)
    {
        double t = Beta.StudentQuantile(p, df);

        // Relative on the probability, not absolute on t: at p = 1e-12 an
        // absolute check on the recovered probability proves nothing.
        Assert.Equal(1.0, Beta.StudentSf(t, df) / p, 1e-9);
    }

    [Fact]
    public void Quantile_is_zero_at_one_half()
    {
        Assert.Equal(0.0, Beta.StudentQuantile(0.5, 7.0), 1e-12);
    }

    [Fact]
    public void Quantile_matches_the_familiar_two_sided_five_percent_points()
    {
        // The numbers every statistics table prints: t(0.025, df).
        Assert.Equal(12.706204736432095, Beta.StudentQuantile(0.025, 1.0), 1e-9);
        Assert.Equal(2.2621571627409915, Beta.StudentQuantile(0.025, 9.0), 1e-9);
        Assert.Equal(1.9599639845400545, Beta.StudentQuantile(0.025, 1e12), 1e-6);
    }

    [Fact]
    public void Quantile_refuses_a_probability_outside_the_open_unit_interval()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Beta.StudentQuantile(0.0, 5.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Beta.StudentQuantile(1.0, 5.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Beta.StudentQuantile(double.NaN, 5.0));
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~StudentQuantileTests"`
Expected: FAIL — `'Beta' does not contain a definition for 'StudentQuantile'`.

- [ ] **Step 3: Add `StudentQuantile` to `src/Lodestar.Stats/Internal/Beta.cs`**

Append inside the `Beta` class, after `FisherSf`:

```csharp
    /// <summary>The t with <c>P(T &gt; t) = p</c>: the inverse of <see cref="StudentSf"/>.</summary>
    /// <remarks>
    /// By bisection on a strictly decreasing function rather than by a rational
    /// approximation of its own. Fifty-odd halvings reach the last bit of a
    /// double, the bracket is found by doubling rather than assumed, and there
    /// is no second approximation to keep in agreement with the tail.
    /// </remarks>
    internal static double StudentQuantile(double p, double df)
    {
        if (double.IsNaN(p) || p <= 0.0 || p >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(p), p, "The tail probability must lie strictly inside (0, 1).");
        }
        if (double.IsNaN(df) || df <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(df), df, "The degrees of freedom must be positive.");
        }

        if (p == 0.5)
        {
            return 0.0;
        }

        // Widen until the answer is bracketed. A Cauchy tail (df = 1) at
        // p = 1e-300 needs a bound near 1e300, so a fixed bracket would fail
        // exactly where the far tail matters.
        double high = 1.0;
        while (StudentSf(high, df) > p && high < 1e300)
        {
            high *= 2.0;
        }

        double low = -high;
        for (int i = 0; i < 200; i++)
        {
            double middle = 0.5 * (low + high);
            if (middle == low || middle == high)
            {
                break;
            }

            if (StudentSf(middle, df) > p)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return 0.5 * (low + high);
    }
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~StudentQuantileTests"`
Expected: PASS, **11 tests**.

- [ ] **Step 5: Write the failing oracle replay**

`tests/Lodestar.Stats.Tests/TTestOracleTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>
/// Replays <c>tests/oracles/stats_ttest.json</c>. Each case names the scipy call
/// and the arguments it was generated with, and the replay reads them rather
/// than assuming a default.
/// </summary>
public sealed class TTestOracleTests
{
    private static Alternative ParseAlternative(string value) => value switch
    {
        "two-sided" => Alternative.TwoSided,
        "less" => Alternative.Less,
        "greater" => Alternative.Greater,
        _ => throw new InvalidDataException($"Unknown alternative '{value}'."),
    };

    [Fact]
    public void Every_case_matches_scipy()
    {
        using JsonDocument document = StatsCorpus.Load("stats_ttest.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            string call = c.GetProperty("call").GetString()!;
            JsonElement args = c.GetProperty("args");
            double[] a = StatsCorpus.Doubles(c.GetProperty("a"));
            double[] b = StatsCorpus.Doubles(c.GetProperty("b"));
            Alternative alternative = ParseAlternative(args.GetProperty("alternative").GetString()!);

            TTestResult result = call switch
            {
                "ttest_ind" => TTest.Independent(
                    a, b, alternative,
                    args.GetProperty("equal_var").GetBoolean() ? Variance.Equal : Variance.Welch),
                "ttest_rel" => TTest.Paired(a, b, alternative),
                "ttest_1samp" => TTest.OneSample(
                    a, args.GetProperty("popmean").GetDouble(), alternative),
                _ => throw new InvalidDataException($"Unknown call '{call}'."),
            };

            StatsOracleAsserts.Statistic(
                StatsCorpus.Number(c.GetProperty("statistic")), result.Statistic, name);
            StatsOracleAsserts.PValue(c.GetProperty("pvalue").GetDouble(), result.PValue, name);
            StatsOracleAsserts.Statistic(c.GetProperty("df").GetDouble(), result.Df, $"{name} df");

            (double low, double high) = result.ConfidenceInterval(0.95);
            StatsOracleAsserts.Statistic(
                StatsCorpus.Number(c.GetProperty("ci_low")), low, $"{name} ci low");
            StatsOracleAsserts.Statistic(
                StatsCorpus.Number(c.GetProperty("ci_high")), high, $"{name} ci high");

            replayed++;
        }

        // The corpus is not empty, and the loop did not skip it.
        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
```

`tests/Lodestar.Stats.Tests/TTestEdgeTests.cs`:

```csharp
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>What the t-test refuses, and the one default that is not scipy's.</summary>
public sealed class TTestEdgeTests
{
    [Fact]
    public void Independent_defaults_to_Welch_where_scipy_defaults_to_Student()
    {
        double[] a = [1.0, 2.0, 3.0, 4.0];
        double[] b = [2.0, 3.0, 8.0, 12.0, 15.0, 20.0];

        TTestResult chosen = TTest.Independent(a, b);
        TTestResult welch = TTest.Independent(a, b, Alternative.TwoSided, Variance.Welch);
        TTestResult student = TTest.Independent(a, b, Alternative.TwoSided, Variance.Equal);

        Assert.Equal(welch.Df, chosen.Df);
        Assert.NotEqual(student.Df, chosen.Df);
    }

    [Fact]
    public void Welch_degrees_of_freedom_need_not_be_whole()
    {
        double[] a = [1.0, 2.0, 3.0, 4.0];
        double[] b = [2.0, 3.0, 8.0, 12.0, 15.0, 20.0];

        double df = TTest.Independent(a, b, Alternative.TwoSided, Variance.Welch).Df;

        Assert.NotEqual(df, Math.Round(df));
    }

    [Fact]
    public void Independent_refuses_a_sample_of_fewer_than_two()
    {
        Assert.Throws<ArgumentException>(() => TTest.Independent([1.0], [1.0, 2.0]));
        Assert.Throws<ArgumentException>(() => TTest.Independent([1.0, 2.0], [1.0]));
    }

    [Fact]
    public void Paired_refuses_samples_of_different_length()
    {
        Assert.Throws<ArgumentException>(() => TTest.Paired([1.0, 2.0, 3.0], [1.0, 2.0]));
    }

    [Fact]
    public void OneSample_refuses_a_non_finite_population_mean()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TTest.OneSample([1.0, 2.0, 3.0], double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TTest.OneSample([1.0, 2.0, 3.0], double.PositiveInfinity));
    }

    [Fact]
    public void A_one_sided_interval_is_half_open_rather_than_narrower()
    {
        double[] a = [1.0, 2.0, 3.0, 4.0, 5.0];

        (double low, double high) = TTest.OneSample(a, 0.0, Alternative.Greater)
            .ConfidenceInterval(0.95);

        Assert.True(double.IsFinite(low));
        Assert.True(double.IsPositiveInfinity(high));
    }

    [Fact]
    public void ConfidenceInterval_refuses_a_level_outside_the_open_unit_interval()
    {
        TTestResult result = TTest.OneSample([1.0, 2.0, 3.0], 0.0);

        Assert.Throws<ArgumentOutOfRangeException>(() => result.ConfidenceInterval(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.ConfidenceInterval(1.0));
    }

    [Fact]
    public void A_NaN_in_the_sample_propagates_rather_than_being_dropped()
    {
        // The spec's ruling: nan_policy is not a parameter here, and the remarks
        // say a caller who wants scipy's 'omit' filters the array themselves.
        TTestResult result = TTest.OneSample([1.0, double.NaN, 3.0], 0.0);

        Assert.True(double.IsNaN(result.Statistic));
        Assert.True(double.IsNaN(result.PValue));
    }
}
```

- [ ] **Step 6: Run to verify both fail**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~TTest"`
Expected: FAIL — `The name 'TTest' does not exist in the current context`.

- [ ] **Step 7: Write `src/Lodestar.Stats/TTest.cs`**

```csharp
using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>Student's and Welch's t-tests: independent, paired and one-sample.</summary>
/// <remarks>
/// Arrays in, a statistic and a p-value out. Nothing is fitted and nothing is
/// held between two calls, so every entry point is static.
///
/// <b>The default differs from scipy's.</b> <see cref="Independent"/> defaults
/// to <see cref="Variance.Welch"/>, where <c>scipy.stats.ttest_ind</c> defaults
/// to <c>equal_var=True</c>, which is Student's test. Pooling the variances is
/// only correct when the two populations really share one, and a default that
/// is wrong in the common case costs more than a word at the call site.
/// </remarks>
public static class TTest
{
    /// <summary>The two-sample t-test on independent samples.</summary>
    /// <param name="a">The first sample; at least two values.</param>
    /// <param name="b">The second sample; at least two values.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <param name="variance">Whether to pool the two variances.</param>
    /// <returns>The statistic, the p-value and the degrees of freedom.</returns>
    /// <exception cref="ArgumentException">Either sample holds fewer than two values.</exception>
    public static TTestResult Independent(
        ReadOnlySpan<double> a,
        ReadOnlySpan<double> b,
        Alternative alternative = Alternative.TwoSided,
        Variance variance = Variance.Welch)
    {
        RequireAtLeastTwo(a, nameof(a));
        RequireAtLeastTwo(b, nameof(b));

        (double meanA, double varianceA) = MeanAndVariance(a);
        (double meanB, double varianceB) = MeanAndVariance(b);
        int n = a.Length;
        int m = b.Length;

        double standardError;
        double df;
        if (variance == Variance.Equal)
        {
            double pooled = (((n - 1) * varianceA) + ((m - 1) * varianceB)) / (n + m - 2);
            standardError = Math.Sqrt(pooled * ((1.0 / n) + (1.0 / m)));
            df = n + m - 2;
        }
        else
        {
            double termA = varianceA / n;
            double termB = varianceB / m;
            standardError = Math.Sqrt(termA + termB);

            // Welch-Satterthwaite. The denominator divides by n-1 and m-1, which
            // is why both samples must hold at least two values.
            double numerator = (termA + termB) * (termA + termB);
            df = numerator / ((termA * termA / (n - 1)) + (termB * termB / (m - 1)));
        }

        return Build(meanA - meanB, standardError, df, alternative);
    }

    /// <summary>The paired t-test: a one-sample test on the differences.</summary>
    /// <param name="a">The first measurement of each pair.</param>
    /// <param name="b">The second measurement of each pair, in the same order.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <returns>The statistic, the p-value and the degrees of freedom.</returns>
    /// <exception cref="ArgumentException">
    /// The samples differ in length, or hold fewer than two pairs.
    /// </exception>
    public static TTestResult Paired(
        ReadOnlySpan<double> a,
        ReadOnlySpan<double> b,
        Alternative alternative = Alternative.TwoSided)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException(
                $"A paired test needs the same number of values in both samples; got {a.Length} and {b.Length}.",
                nameof(b));
        }

        RequireAtLeastTwo(a, nameof(a));

        double[] differences = new double[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            differences[i] = a[i] - b[i];
        }

        return OneSample(differences, 0.0, alternative);
    }

    /// <summary>The one-sample t-test against a stated population mean.</summary>
    /// <param name="sample">The sample; at least two values.</param>
    /// <param name="populationMean">The mean the null hypothesis states.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <returns>The statistic, the p-value and the degrees of freedom.</returns>
    /// <exception cref="ArgumentException"><paramref name="sample"/> holds fewer than two values.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="populationMean"/> is NaN or infinite.
    /// </exception>
    public static TTestResult OneSample(
        ReadOnlySpan<double> sample,
        double populationMean,
        Alternative alternative = Alternative.TwoSided)
    {
        RequireAtLeastTwo(sample, nameof(sample));

        if (double.IsNaN(populationMean) || double.IsInfinity(populationMean))
        {
            throw new ArgumentOutOfRangeException(
                nameof(populationMean), populationMean, "The population mean must be finite.");
        }

        (double mean, double variance) = MeanAndVariance(sample);
        double standardError = Math.Sqrt(variance / sample.Length);

        return Build(mean - populationMean, standardError, sample.Length - 1, alternative);
    }

    private static void RequireAtLeastTwo(ReadOnlySpan<double> values, string name)
    {
        if (values.Length < 2)
        {
            throw new ArgumentException(
                $"A t-test needs at least two values; got {values.Length}.", name);
        }
    }

    // The sample variance, with the n-1 denominator. Two passes rather than the
    // sum-of-squares shortcut: the shortcut cancels catastrophically once the
    // mean is large compared with the spread, which is a real corpus case.
    private static (double Mean, double Variance) MeanAndVariance(ReadOnlySpan<double> values)
    {
        double sum = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }

        double mean = sum / values.Length;

        double squares = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            double deviation = values[i] - mean;
            squares += deviation * deviation;
        }

        return (mean, squares / (values.Length - 1));
    }

    private static TTestResult Build(
        double estimate, double standardError, double df, Alternative alternative)
    {
        double statistic = estimate / standardError;
        double pValue = PValue(statistic, df, alternative);

        return new TTestResult(statistic, pValue, df)
        {
            Estimate = estimate,
            StandardError = standardError,
            Alternative = alternative,
        };
    }

    internal static double PValue(double statistic, double df, Alternative alternative)
    {
        if (double.IsNaN(statistic))
        {
            return double.NaN;
        }

        return alternative switch
        {
            // Twice the tail beyond |t|. Evaluating at the absolute value rather
            // than at the signed one keeps the far tail accurate: 1 - Sf(-t)
            // would cancel to zero where Sf(|t|) is 1e-53.
            Alternative.TwoSided => 2.0 * Beta.StudentSf(Math.Abs(statistic), df),
            Alternative.Greater => Beta.StudentSf(statistic, df),
            Alternative.Less => Beta.StudentSf(-statistic, df),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };
    }
}
```

- [ ] **Step 8: Fill in `TTestResult.ConfidenceInterval`**

Replace the `throw new NotImplementedException();` body in
`src/Lodestar.Stats/TestResult.cs`:

```csharp
    public (double Low, double High) ConfidenceInterval(double level = 0.95)
    {
        if (double.IsNaN(level) || level <= 0.0 || level >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level), level, "The confidence level must lie strictly inside (0, 1).");
        }

        // A one-sided test spends its whole error budget on one side, so the tail
        // is 1 - level rather than half of it, and the other bound is infinite.
        double tail = Alternative == Alternative.TwoSided ? (1.0 - level) / 2.0 : 1.0 - level;
        double half = Internal.Beta.StudentQuantile(tail, Df) * StandardError;

        return Alternative switch
        {
            Alternative.TwoSided => (Estimate - half, Estimate + half),
            Alternative.Greater => (Estimate - half, double.PositiveInfinity),
            Alternative.Less => (double.NegativeInfinity, Estimate + half),
            _ => throw new ArgumentOutOfRangeException(nameof(Alternative), Alternative, null),
        };
    }
```

- [ ] **Step 9: Run to verify both pass**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~TTest"`
Expected: PASS. Read the count: **9 tests** (one oracle replay plus eight edge
facts). The oracle replay's own assertion on `replayed` is what proves the
corpus was not silently empty.

- [ ] **Step 10: Run the mirror too**

Run: `dotnet test tests/Lodestar.Stats.NetStandard.Tests -c Release`
Expected: PASS, with a count one higher than
`tests/Lodestar.Stats.Tests` — the extra is `NetStandardAssemblyGuardTests`.

- [ ] **Step 11: Run the gates and commit**

```bash
dotnet build Lodestar.slnx -c Release
dotnet format Lodestar.slnx --verify-no-changes
python3 tools/check_comment_length.py
python3 tools/check_repeated_literals.py --base origin/main
git add src/Lodestar.Stats tests/Lodestar.Stats.Tests
git commit -m "Lodestar.Stats: the four t-tests, and the quantile their interval needs

Refs #442. Independent defaults to Welch where scipy's ttest_ind defaults to
Student: pooling is only correct when the two populations share a variance, and
a default that is wrong in the common case costs more than a word at the call
site. The corpus covers both and equivalence.md gets the row.

The two-sided p-value is twice the tail at |t| rather than one minus the tail at
-t: the second cancels to zero where the first is 1e-53. StudentQuantile
bisects the tail rather than adding a second approximation to keep in agreement
with it, and its bracket is found by doubling, since a Cauchy tail at 1e-300
needs a bound near 1e300."
```

---

### Task 6: `MannWhitney` and `Wilcoxon`

**Files:**

- Create: `src/Lodestar.Stats/MannWhitney.cs`, `src/Lodestar.Stats/Wilcoxon.cs`
- Test: `tests/Lodestar.Stats.Tests/MannWhitneyOracleTests.cs`,
  `tests/Lodestar.Stats.Tests/WilcoxonOracleTests.cs`,
  `tests/Lodestar.Stats.Tests/RankTestEdgeTests.cs`

**Interfaces:**

- Consumes: `Ranks.Average`, `Ranks.TieCorrection`, `Ranks.HasTies`,
  `RankDistributions.MannWhitneyCounts`, `RankDistributions.SignedRankCounts`,
  `Normal.Sf`, the `Alternative`, `Continuity`, `ExactMethod` and `ZeroMethod` enums.
- Produces:
  - `TestResult MannWhitney.Test(ReadOnlySpan<double> x, ReadOnlySpan<double> y, Alternative alternative = Alternative.TwoSided, Continuity continuity = Continuity.Applied, ExactMethod method = ExactMethod.Auto)`
  - `TestResult Wilcoxon.Paired(ReadOnlySpan<double> x, ReadOnlySpan<double> y, ZeroMethod zeroMethod = ZeroMethod.Wilcox, Alternative alternative = Alternative.TwoSided, Continuity continuity = Continuity.None, ExactMethod method = ExactMethod.Auto)`
  - `TestResult Wilcoxon.OneSample(ReadOnlySpan<double> differences, ZeroMethod zeroMethod = ZeroMethod.Wilcox, Alternative alternative = Alternative.TwoSided, Continuity continuity = Continuity.None, ExactMethod method = ExactMethod.Auto)`

**The `Auto` rule, as scipy measures it.** `ExactMethod.Auto` takes the exact
branch when the sample is small **and** free of ties, and the asymptotic branch
otherwise — for Mann-Whitney the threshold is fewer than eight in either sample,
for Wilcoxon fifty or fewer non-zero differences. `ExactMethod.Exact` asked for
explicitly computes the exact distribution **even on tied data**: measured,
`mannwhitneyu(tied_a, tied_b, method="exact")` returns a number rather than
raising, so parity means returning one too, and the reference page says the
number is only approximate there.

- [ ] **Step 1: Write the failing oracle replays**

`tests/Lodestar.Stats.Tests/MannWhitneyOracleTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>Replays <c>tests/oracles/stats_mannwhitney.json</c>.</summary>
public sealed class MannWhitneyOracleTests
{
    [Fact]
    public void Every_case_matches_scipy()
    {
        using JsonDocument document = StatsCorpus.Load("stats_mannwhitney.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            JsonElement args = c.GetProperty("args");

            TestResult result = MannWhitney.Test(
                StatsCorpus.Doubles(c.GetProperty("a")),
                StatsCorpus.Doubles(c.GetProperty("b")),
                StatsCorpus.Alternative(args),
                args.GetProperty("use_continuity").GetBoolean()
                    ? Continuity.Applied
                    : Continuity.None,
                StatsCorpus.Method(args));

            StatsOracleAsserts.Statistic(
                c.GetProperty("statistic").GetDouble(), result.Statistic, name);
            StatsOracleAsserts.PValue(c.GetProperty("pvalue").GetDouble(), result.PValue, name);
            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
```

`tests/Lodestar.Stats.Tests/WilcoxonOracleTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>Replays <c>tests/oracles/stats_wilcoxon.json</c>.</summary>
public sealed class WilcoxonOracleTests
{
    [Fact]
    public void Every_case_matches_scipy()
    {
        using JsonDocument document = StatsCorpus.Load("stats_wilcoxon.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            JsonElement args = c.GetProperty("args");

            ZeroMethod zeroMethod = args.GetProperty("zero_method").GetString() switch
            {
                "wilcox" => ZeroMethod.Wilcox,
                "pratt" => ZeroMethod.Pratt,
                "zsplit" => ZeroMethod.ZSplit,
                var other => throw new InvalidDataException($"Unknown zero_method '{other}'."),
            };

            TestResult result = Wilcoxon.Paired(
                StatsCorpus.Doubles(c.GetProperty("x")),
                StatsCorpus.Doubles(c.GetProperty("y")),
                zeroMethod,
                StatsCorpus.Alternative(args),
                args.GetProperty("correction").GetBoolean() ? Continuity.Applied : Continuity.None,
                StatsCorpus.Method(args));

            StatsOracleAsserts.Statistic(
                c.GetProperty("statistic").GetDouble(), result.Statistic, name);
            StatsOracleAsserts.PValue(c.GetProperty("pvalue").GetDouble(), result.PValue, name);
            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
```

- [ ] **Step 2: Add the two shared parsers to `StatsCorpus`**

Append inside `StatsCorpus`:

```csharp
    /// <summary>The <c>alternative</c> a case was generated with.</summary>
    internal static Alternative Alternative(JsonElement args) =>
        args.GetProperty("alternative").GetString() switch
        {
            "two-sided" => Lodestar.Stats.Alternative.TwoSided,
            "less" => Lodestar.Stats.Alternative.Less,
            "greater" => Lodestar.Stats.Alternative.Greater,
            var other => throw new InvalidDataException($"Unknown alternative '{other}'."),
        };

    /// <summary>The <c>method</c> a case was generated with. scipy spells the KS one "asymp".</summary>
    internal static ExactMethod Method(JsonElement args) =>
        args.GetProperty("method").GetString() switch
        {
            "auto" => ExactMethod.Auto,
            "exact" => ExactMethod.Exact,
            "asymptotic" or "asymp" => ExactMethod.Asymptotic,
            var other => throw new InvalidDataException($"Unknown method '{other}'."),
        };
```

- [ ] **Step 3: Write the edge tests**

`tests/Lodestar.Stats.Tests/RankTestEdgeTests.cs`:

```csharp
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>What the two rank tests refuse, and where Auto changes branch.</summary>
public sealed class RankTestEdgeTests
{
    [Fact]
    public void MannWhitney_refuses_an_empty_sample()
    {
        Assert.Throws<ArgumentException>(() => MannWhitney.Test([], [1.0, 2.0]));
        Assert.Throws<ArgumentException>(() => MannWhitney.Test([1.0, 2.0], []));
    }

    [Fact]
    public void MannWhitney_auto_takes_the_exact_branch_only_when_small_and_untied()
    {
        double[] small = [1.0, 4.0, 7.0];
        double[] other = [2.0, 3.0, 8.0];

        Assert.Equal(
            MannWhitney.Test(small, other, method: ExactMethod.Exact).PValue,
            MannWhitney.Test(small, other, method: ExactMethod.Auto).PValue,
            1e-15);

        double[] tied = [1.0, 2.0, 2.0];
        double[] alsoTied = [2.0, 3.0, 3.0];

        Assert.Equal(
            MannWhitney.Test(tied, alsoTied, method: ExactMethod.Asymptotic).PValue,
            MannWhitney.Test(tied, alsoTied, method: ExactMethod.Auto).PValue,
            1e-15);
    }

    [Fact]
    public void MannWhitney_exact_still_answers_on_tied_data()
    {
        // Measured against scipy 1.18.0: mannwhitneyu(..., method="exact") on
        // tied samples returns a number rather than raising, so this does too.
        TestResult result = MannWhitney.Test(
            [1.0, 2.0, 2.0], [2.0, 3.0, 3.0], method: ExactMethod.Exact);

        Assert.True(result.PValue is > 0.0 and <= 1.0);
    }

    [Fact]
    public void Wilcoxon_refuses_samples_of_different_length()
    {
        Assert.Throws<ArgumentException>(() => Wilcoxon.Paired([1.0, 2.0, 3.0], [1.0, 2.0]));
    }

    [Fact]
    public void Wilcoxon_refuses_an_empty_sample()
    {
        Assert.Throws<ArgumentException>(() => Wilcoxon.OneSample([]));
    }

    [Fact]
    public void Wilcox_drops_the_zero_pairs_and_pratt_keeps_them()
    {
        double[] x = [1.0, 3.0, 5.0, 7.0, 9.0, 11.0];
        double[] y = [1.0, 3.5, 5.0, 8.0, 8.5, 13.0];

        double wilcox = Wilcoxon.Paired(x, y, ZeroMethod.Wilcox).Statistic;
        double pratt = Wilcoxon.Paired(x, y, ZeroMethod.Pratt).Statistic;

        // Two of the six differences are zero, so the two rules rank different
        // numbers of values and cannot agree.
        Assert.NotEqual(wilcox, pratt);
    }

    [Fact]
    public void Wilcoxon_of_all_zero_differences_is_a_statistic_of_zero_and_a_p_value_of_one()
    {
        TestResult result = Wilcoxon.OneSample([0.0, 0.0, 0.0]);

        Assert.Equal(0.0, result.Statistic);
        Assert.Equal(1.0, result.PValue);
    }
}
```

- [ ] **Step 4: Run to verify they fail**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~MannWhitney|FullyQualifiedName~Wilcoxon|FullyQualifiedName~RankTestEdge"`
Expected: FAIL — neither type exists.

- [ ] **Step 5: Write `src/Lodestar.Stats/MannWhitney.cs`**

```csharp
using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>The Mann-Whitney U test: do two independent samples come from one distribution?</summary>
/// <remarks>
/// The rank-based counterpart to <see cref="TTest.Independent"/>: it assumes
/// nothing about the shape of either distribution, only that a value from one
/// can be compared with a value from the other.
/// </remarks>
public static class MannWhitney
{
    // scipy takes the exact branch below eight in either sample; above that the
    // exact table is large and the normal approximation is already accurate.
    private const int AutoExactThreshold = 8;

    /// <summary>Compares two independent samples by their ranks.</summary>
    /// <param name="x">The first sample; at least one value.</param>
    /// <param name="y">The second sample; at least one value.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <param name="continuity">
    /// Whether the normal approximation gets the half-unit correction. Ignored
    /// on the exact branch, where there is nothing to approximate.
    /// </param>
    /// <param name="method">Exact, asymptotic, or chosen by sample size and ties.</param>
    /// <returns>U for the first sample, and the p-value.</returns>
    /// <exception cref="ArgumentException">Either sample is empty.</exception>
    public static TestResult Test(
        ReadOnlySpan<double> x,
        ReadOnlySpan<double> y,
        Alternative alternative = Alternative.TwoSided,
        Continuity continuity = Continuity.Applied,
        ExactMethod method = ExactMethod.Auto)
    {
        if (x.Length == 0)
        {
            throw new ArgumentException("The first sample is empty.", nameof(x));
        }
        if (y.Length == 0)
        {
            throw new ArgumentException("The second sample is empty.", nameof(y));
        }

        int n = x.Length;
        int m = y.Length;

        double[] pooled = new double[n + m];
        x.CopyTo(pooled);
        y.CopyTo(pooled.AsSpan(n));

        double[] ranks = Ranks.Average(pooled);
        double rankSumX = 0.0;
        for (int i = 0; i < n; i++)
        {
            rankSumX += ranks[i];
        }

        // U counts the pairs (xi, yj) with xi > yj, recovered from the rank sum
        // by subtracting the ranks x would hold if it sorted first.
        double u = rankSumX - (n * (n + 1) / 2.0);

        bool ties = Ranks.HasTies(pooled);
        bool exact = method switch
        {
            ExactMethod.Exact => true,
            ExactMethod.Asymptotic => false,
            _ => !ties && n < AutoExactThreshold && m < AutoExactThreshold,
        };

        double pValue = exact
            ? ExactPValue(u, n, m, alternative)
            : AsymptoticPValue(u, n, m, pooled, alternative, continuity);

        return new TestResult(u, pValue);
    }

    private static double ExactPValue(double u, int n, int m, Alternative alternative)
    {
        double[] counts = RankDistributions.MannWhitneyCounts(n, m);
        double total = 0.0;
        for (int i = 0; i < counts.Length; i++)
        {
            total += counts[i];
        }

        double atMost = CumulativeAtMost(counts, u);
        double atLeast = total - CumulativeAtMost(counts, u - 1);

        return alternative switch
        {
            Alternative.Less => atMost / total,
            Alternative.Greater => atLeast / total,
            // Twice the smaller tail, clamped: a discrete distribution's two
            // one-sided p-values can exceed one when doubled at the centre.
            Alternative.TwoSided => Math.Min(1.0, 2.0 * Math.Min(atMost, atLeast) / total),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };
    }

    private static double CumulativeAtMost(double[] counts, double u)
    {
        double sum = 0.0;
        for (int i = 0; i < counts.Length && i <= u; i++)
        {
            sum += counts[i];
        }

        return sum;
    }

    private static double AsymptoticPValue(
        double u,
        int n,
        int m,
        ReadOnlySpan<double> pooled,
        Alternative alternative,
        Continuity continuity)
    {
        double total = n + m;
        double mean = n * m / 2.0;

        // The tie correction shrinks the variance: tied values carry less
        // information about the ordering than distinct ones do.
        double tieTerm = Ranks.TieCorrection(pooled) / (total * (total - 1.0));
        double variance = n * m / 12.0 * (total + 1.0 - tieTerm);
        double deviation = u - mean;

        double correction = continuity == Continuity.Applied ? 0.5 : 0.0;
        double z = alternative switch
        {
            Alternative.Less => (deviation + correction) / Math.Sqrt(variance),
            Alternative.Greater => (deviation - correction) / Math.Sqrt(variance),
            Alternative.TwoSided => (Math.Abs(deviation) - correction) / Math.Sqrt(variance),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };

        return alternative switch
        {
            Alternative.Less => 1.0 - Normal.Sf(z),
            Alternative.Greater => Normal.Sf(z),
            Alternative.TwoSided => Math.Min(1.0, 2.0 * Normal.Sf(z)),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };
    }
}
```

- [ ] **Step 6: Write `src/Lodestar.Stats/Wilcoxon.cs`**

```csharp
using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>The Wilcoxon signed-rank test on paired measurements.</summary>
/// <remarks>
/// The rank-based counterpart to <see cref="TTest.Paired"/>. What it does with
/// a pair whose difference is exactly zero is not a detail but part of the
/// test's definition, which is why <see cref="ZeroMethod"/> is a parameter and
/// not a hidden convention.
/// </remarks>
public static class Wilcoxon
{
    // scipy takes the exact branch at fifty or fewer non-zero differences.
    private const int AutoExactThreshold = 50;

    /// <summary>Compares two paired samples by the ranks of their differences.</summary>
    /// <param name="x">The first measurement of each pair.</param>
    /// <param name="y">The second measurement of each pair, in the same order.</param>
    /// <param name="zeroMethod">What to do with pairs whose difference is zero.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <param name="continuity">Whether the normal approximation gets the half-unit correction.</param>
    /// <param name="method">Exact, asymptotic, or chosen by the number of non-zero differences.</param>
    /// <returns>The smaller signed-rank sum, and the p-value.</returns>
    /// <exception cref="ArgumentException">
    /// The samples differ in length, or are empty.
    /// </exception>
    public static TestResult Paired(
        ReadOnlySpan<double> x,
        ReadOnlySpan<double> y,
        ZeroMethod zeroMethod = ZeroMethod.Wilcox,
        Alternative alternative = Alternative.TwoSided,
        Continuity continuity = Continuity.None,
        ExactMethod method = ExactMethod.Auto)
    {
        if (x.Length != y.Length)
        {
            throw new ArgumentException(
                $"A paired test needs the same number of values in both samples; got {x.Length} and {y.Length}.",
                nameof(y));
        }

        double[] differences = new double[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            differences[i] = x[i] - y[i];
        }

        return OneSample(differences, zeroMethod, alternative, continuity, method);
    }

    /// <summary>Compares a sample of differences against a median of zero.</summary>
    /// <param name="differences">The differences; at least one value.</param>
    /// <param name="zeroMethod">What to do with differences that are exactly zero.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <param name="continuity">Whether the normal approximation gets the half-unit correction.</param>
    /// <param name="method">Exact, asymptotic, or chosen by the number of non-zero differences.</param>
    /// <returns>The smaller signed-rank sum, and the p-value.</returns>
    /// <exception cref="ArgumentException"><paramref name="differences"/> is empty.</exception>
    public static TestResult OneSample(
        ReadOnlySpan<double> differences,
        ZeroMethod zeroMethod = ZeroMethod.Wilcox,
        Alternative alternative = Alternative.TwoSided,
        Continuity continuity = Continuity.None,
        ExactMethod method = ExactMethod.Auto)
    {
        if (differences.Length == 0)
        {
            throw new ArgumentException("The sample is empty.", nameof(differences));
        }

        // Wilcox drops the zeros before ranking; the other two rank them and
        // differ only in what they do with the ranks afterwards.
        double[] ranked = zeroMethod == ZeroMethod.Wilcox
            ? [.. differences.ToArray().Where(d => d != 0.0)]
            : differences.ToArray();

        if (ranked.Length == 0)
        {
            // Every difference was zero: there is no evidence either way, and
            // scipy answers with a statistic of zero and a p-value of one.
            return new TestResult(0.0, 1.0);
        }

        double[] magnitudes = new double[ranked.Length];
        for (int i = 0; i < ranked.Length; i++)
        {
            magnitudes[i] = Math.Abs(ranked[i]);
        }

        double[] ranks = Ranks.Average(magnitudes);

        double positive = 0.0;
        double negative = 0.0;
        double zeroRankSum = 0.0;
        for (int i = 0; i < ranked.Length; i++)
        {
            if (ranked[i] > 0.0)
            {
                positive += ranks[i];
            }
            else if (ranked[i] < 0.0)
            {
                negative += ranks[i];
            }
            else
            {
                zeroRankSum += ranks[i];
            }
        }

        if (zeroMethod == ZeroMethod.ZSplit)
        {
            positive += zeroRankSum / 2.0;
            negative += zeroRankSum / 2.0;
        }

        double statistic = alternative == Alternative.TwoSided
            ? Math.Min(positive, negative)
            : positive;

        int effective = zeroMethod == ZeroMethod.Pratt
            ? ranked.Count(d => d != 0.0)
            : ranked.Length;

        bool exact = method switch
        {
            ExactMethod.Exact => true,
            ExactMethod.Asymptotic => false,
            _ => !Ranks.HasTies(magnitudes) && effective <= AutoExactThreshold,
        };

        double pValue = exact
            ? ExactPValue(positive, negative, effective, alternative)
            : AsymptoticPValue(positive, negative, ranks, alternative, continuity);

        return new TestResult(statistic, pValue);
    }

    private static double ExactPValue(
        double positive, double negative, int n, Alternative alternative)
    {
        double[] counts = RankDistributions.SignedRankCounts(n);
        double total = Math.Pow(2.0, n);

        double atMostPositive = 0.0;
        for (int w = 0; w < counts.Length && w <= positive; w++)
        {
            atMostPositive += counts[w];
        }

        double atMostNegative = 0.0;
        for (int w = 0; w < counts.Length && w <= negative; w++)
        {
            atMostNegative += counts[w];
        }

        return alternative switch
        {
            Alternative.Less => atMostPositive / total,
            Alternative.Greater => atMostNegative / total,
            Alternative.TwoSided => Math.Min(
                1.0, 2.0 * Math.Min(atMostPositive, atMostNegative) / total),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };
    }

    private static double AsymptoticPValue(
        double positive,
        double negative,
        double[] ranks,
        Alternative alternative,
        Continuity continuity)
    {
        double total = positive + negative;
        double mean = total / 2.0;

        double squares = 0.0;
        for (int i = 0; i < ranks.Length; i++)
        {
            squares += ranks[i] * ranks[i];
        }

        // The variance is the sum of squared ranks over four, which reduces to
        // n(n+1)(2n+1)/24 only when the ranks are untied.
        double variance = squares / 4.0;
        double deviation = positive - mean;
        double correction = continuity == Continuity.Applied ? 0.5 : 0.0;

        double z = alternative switch
        {
            Alternative.Less => (deviation + correction) / Math.Sqrt(variance),
            Alternative.Greater => (deviation - correction) / Math.Sqrt(variance),
            Alternative.TwoSided => (Math.Abs(deviation) - correction) / Math.Sqrt(variance),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };

        return alternative switch
        {
            Alternative.Less => 1.0 - Normal.Sf(z),
            Alternative.Greater => Normal.Sf(z),
            Alternative.TwoSided => Math.Min(1.0, 2.0 * Normal.Sf(z)),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };
    }
}
```

- [ ] **Step 7: Run, and expect to iterate**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~MannWhitney|FullyQualifiedName~Wilcoxon|FullyQualifiedName~RankTestEdge"`

These two families are where the tail conventions are fiddliest, so a first run
that fails on a subset of the corpus is expected rather than a sign the design
is wrong. When a case fails, the message names it, and the way to diagnose is to
reproduce that one case in Python:

```bash
REPO=$(git rev-parse --show-toplevel); cd "$(mktemp -d)"
PYTHONSAFEPATH=1 "$REPO/.venv-oracles/bin/python" -c "
from scipy import stats
r = stats.mannwhitneyu([1.,4.,7.,9.], [2.,3.,8.,12.,15.],
                       use_continuity=True, alternative='less', method='asymptotic')
print(r)
"
```

Reading scipy's source to understand **one** failing case is diagnosis and is
allowed; deriving the implementation from it is not (ADR 0003). Fix the
convention, not the corpus: a corpus regenerated to match a bug proves nothing.

Expected once converged: PASS, **9 tests**.

- [ ] **Step 8: Run the gates and commit**

```bash
dotnet build Lodestar.slnx -c Release
dotnet test tests/Lodestar.Stats.NetStandard.Tests -c Release
dotnet format Lodestar.slnx --verify-no-changes
python3 tools/check_comment_length.py
git add src/Lodestar.Stats tests/Lodestar.Stats.Tests
git commit -m "Lodestar.Stats: Mann-Whitney U and the Wilcoxon signed-rank test

Refs #442. Both carry an exact branch and an asymptotic one, and Auto picks
between them the way scipy measures: exact below eight in either sample for
Mann-Whitney and at fifty or fewer non-zero differences for Wilcoxon, and only
when the sample is untied.

ExactMethod.Exact asked for explicitly still answers on tied data, because
scipy does: measured, mannwhitneyu(..., method='exact') on ties returns a number
rather than raising. The reference page says the number is approximate there.

The two-sided p-value is twice the smaller tail clamped at one: on a discrete
null distribution the doubled tail exceeds one near the centre."
```

---

### Task 7: `ChiSquare` and `FisherExact`

**Files:**

- Create: `src/Lodestar.Stats/ChiSquare.cs`, `src/Lodestar.Stats/FisherExact.cs`
- Test: `tests/Lodestar.Stats.Tests/ChiSquareOracleTests.cs`,
  `tests/Lodestar.Stats.Tests/FisherExactOracleTests.cs`,
  `tests/Lodestar.Stats.Tests/TableTestEdgeTests.cs`

**Interfaces:**

- Consumes: `Gamma.RegularizedQ`, `Gamma.LogGamma`, `Chi2ContingencyResult`,
  the `Alternative` and `Continuity` enums, `StatsCorpus`, `StatsOracleAsserts`.
- Produces:
  - `TestResult ChiSquare.GoodnessOfFit(ReadOnlySpan<double> observed, ReadOnlySpan<double> expected = default)`
  - `Chi2ContingencyResult ChiSquare.Contingency(double[][] table, Continuity continuity = Continuity.Applied)`
  - `TestResult FisherExact.Test(int[][] table, Alternative alternative = Alternative.TwoSided)`

- [ ] **Step 1: Write the failing oracle replays and edge tests**

`tests/Lodestar.Stats.Tests/ChiSquareOracleTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>Replays <c>tests/oracles/stats_chisquare.json</c>, both calls.</summary>
public sealed class ChiSquareOracleTests
{
    [Fact]
    public void Every_case_matches_scipy()
    {
        using JsonDocument document = StatsCorpus.Load("stats_chisquare.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            double expectedStatistic = c.GetProperty("statistic").GetDouble();
            double expectedP = c.GetProperty("pvalue").GetDouble();

            if (c.GetProperty("call").GetString() == "chisquare")
            {
                double[] observed = StatsCorpus.Doubles(c.GetProperty("observed"));
                double[] expected = StatsCorpus.Doubles(c.GetProperty("expected_input"));

                TestResult result = expected.Length == 0
                    ? ChiSquare.GoodnessOfFit(observed)
                    : ChiSquare.GoodnessOfFit(observed, expected);

                StatsOracleAsserts.Statistic(expectedStatistic, result.Statistic, name);
                StatsOracleAsserts.PValue(expectedP, result.PValue, name);
            }
            else
            {
                Chi2ContingencyResult result = ChiSquare.Contingency(
                    StatsCorpus.Table(c.GetProperty("table")),
                    c.GetProperty("args").GetProperty("correction").GetBoolean()
                        ? Continuity.Applied
                        : Continuity.None);

                StatsOracleAsserts.Statistic(expectedStatistic, result.Statistic, name);
                StatsOracleAsserts.PValue(expectedP, result.PValue, name);
                Assert.Equal(c.GetProperty("dof").GetInt32(), result.Dof);

                double[][] expectedFreq = StatsCorpus.Table(c.GetProperty("expected_freq"));
                for (int i = 0; i < expectedFreq.Length; i++)
                {
                    for (int j = 0; j < expectedFreq[i].Length; j++)
                    {
                        StatsOracleAsserts.Statistic(
                            expectedFreq[i][j], result.ExpectedFrequencies[i][j],
                            $"{name} expected[{i}][{j}]");
                    }
                }
            }

            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
```

`tests/Lodestar.Stats.Tests/FisherExactOracleTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>Replays <c>tests/oracles/stats_fisher.json</c>.</summary>
public sealed class FisherExactOracleTests
{
    [Fact]
    public void Every_case_matches_scipy()
    {
        using JsonDocument document = StatsCorpus.Load("stats_fisher.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            int[][] table =
            [
                .. c.GetProperty("table").EnumerateArray()
                    .Select(row => row.EnumerateArray().Select(v => v.GetInt32()).ToArray()),
            ];

            TestResult result = FisherExact.Test(
                table, StatsCorpus.Alternative(c.GetProperty("args")));

            StatsOracleAsserts.Statistic(
                StatsCorpus.Number(c.GetProperty("statistic")), result.Statistic, name);
            StatsOracleAsserts.PValue(c.GetProperty("pvalue").GetDouble(), result.PValue, name);
            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
```

`tests/Lodestar.Stats.Tests/TableTestEdgeTests.cs`:

```csharp
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>What the two table tests refuse, and where Yates does and does not apply.</summary>
public sealed class TableTestEdgeTests
{
    [Fact]
    public void GoodnessOfFit_refuses_expectations_that_do_not_sum_to_the_observations()
    {
        Assert.Throws<ArgumentException>(
            () => ChiSquare.GoodnessOfFit([10.0, 10.0], [5.0, 6.0]));
    }

    [Fact]
    public void GoodnessOfFit_refuses_a_zero_expectation()
    {
        Assert.Throws<ArgumentException>(
            () => ChiSquare.GoodnessOfFit([10.0, 10.0], [20.0, 0.0]));
    }

    [Fact]
    public void GoodnessOfFit_refuses_mismatched_lengths_and_a_single_category()
    {
        Assert.Throws<ArgumentException>(
            () => ChiSquare.GoodnessOfFit([10.0, 10.0], [20.0]));
        Assert.Throws<ArgumentException>(() => ChiSquare.GoodnessOfFit([10.0]));
    }

    [Fact]
    public void Yates_applies_to_a_two_by_two_and_to_nothing_else()
    {
        double[][] twoByTwo = [[10.0, 20.0], [30.0, 40.0]];
        double[][] threeByTwo = [[10.0, 20.0], [30.0, 40.0], [15.0, 5.0]];

        Assert.NotEqual(
            ChiSquare.Contingency(twoByTwo, Continuity.Applied).Statistic,
            ChiSquare.Contingency(twoByTwo, Continuity.None).Statistic);

        // Above 2x2 the correction is not defined, so asking for it changes nothing.
        Assert.Equal(
            ChiSquare.Contingency(threeByTwo, Continuity.Applied).Statistic,
            ChiSquare.Contingency(threeByTwo, Continuity.None).Statistic,
            1e-15);
    }

    [Fact]
    public void Contingency_refuses_a_ragged_table_and_a_zero_marginal()
    {
        double[][] ragged = [[1.0, 2.0], [3.0]];
        double[][] emptyRow = [[0.0, 0.0], [3.0, 4.0]];

        Assert.Throws<ArgumentException>(() => ChiSquare.Contingency(ragged));
        Assert.Throws<ArgumentException>(() => ChiSquare.Contingency(emptyRow));
    }

    [Fact]
    public void FisherExact_refuses_a_table_that_is_not_two_by_two()
    {
        Assert.Throws<ArgumentException>(() => FisherExact.Test([[1, 2, 3], [4, 5, 6]]));
        Assert.Throws<ArgumentException>(() => FisherExact.Test([[1, 2]]));
    }

    [Fact]
    public void FisherExact_refuses_a_negative_count()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FisherExact.Test([[1, -2], [3, 4]]));
    }

    [Fact]
    public void FisherExact_odds_ratio_is_infinite_when_a_diagonal_is_zero()
    {
        TestResult result = FisherExact.Test([[5, 0], [0, 5]]);

        Assert.True(double.IsPositiveInfinity(result.Statistic));
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~ChiSquare|FullyQualifiedName~FisherExact|FullyQualifiedName~TableTestEdge"`
Expected: FAIL — neither type exists.

- [ ] **Step 3: Write `src/Lodestar.Stats/ChiSquare.cs`**

```csharp
using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>Pearson's chi-square: goodness of fit, and independence in a contingency table.</summary>
public static class ChiSquare
{
    /// <summary>Tests observed counts against an expected distribution.</summary>
    /// <param name="observed">The observed counts; at least two categories.</param>
    /// <param name="expected">
    /// The expected counts, which must sum to the observed total. Omit them for
    /// a uniform expectation, which is what <c>scipy.stats.chisquare</c> does
    /// with <c>f_exp=None</c>.
    /// </param>
    /// <returns>The statistic and the upper-tail p-value.</returns>
    /// <exception cref="ArgumentException">
    /// Fewer than two categories, mismatched lengths, a non-positive expectation,
    /// or expectations that do not sum to the observations.
    /// </exception>
    public static TestResult GoodnessOfFit(
        ReadOnlySpan<double> observed, ReadOnlySpan<double> expected = default)
    {
        if (observed.Length < 2)
        {
            throw new ArgumentException(
                $"A goodness-of-fit test needs at least two categories; got {observed.Length}.",
                nameof(observed));
        }

        double observedTotal = 0.0;
        for (int i = 0; i < observed.Length; i++)
        {
            observedTotal += observed[i];
        }

        double[] target = new double[observed.Length];
        if (expected.IsEmpty)
        {
            double uniform = observedTotal / observed.Length;
            for (int i = 0; i < target.Length; i++)
            {
                target[i] = uniform;
            }
        }
        else
        {
            if (expected.Length != observed.Length)
            {
                throw new ArgumentException(
                    $"There are {observed.Length} observations and {expected.Length} expectations.",
                    nameof(expected));
            }

            double expectedTotal = 0.0;
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] <= 0.0)
                {
                    throw new ArgumentException(
                        $"Expectation {i} is {expected[i]}; the statistic divides by it.",
                        nameof(expected));
                }

                target[i] = expected[i];
                expectedTotal += expected[i];
            }

            // scipy refuses the same way. An expectation summing elsewhere is not
            // a distribution over these categories, so the statistic would not be
            // chi-square distributed and the p-value would mean nothing.
            if (Math.Abs(expectedTotal - observedTotal) > 1e-8 * Math.Abs(observedTotal))
            {
                throw new ArgumentException(
                    $"The expectations sum to {expectedTotal} and the observations to {observedTotal}.",
                    nameof(expected));
            }
        }

        double statistic = 0.0;
        for (int i = 0; i < observed.Length; i++)
        {
            double deviation = observed[i] - target[i];
            statistic += deviation * deviation / target[i];
        }

        int dof = observed.Length - 1;
        return new TestResult(statistic, Gamma.RegularizedQ(dof / 2.0, statistic / 2.0));
    }

    /// <summary>Tests a contingency table for independence of its two factors.</summary>
    /// <param name="table">The observed counts, row-major and rectangular.</param>
    /// <param name="continuity">
    /// Whether to apply Yates's correction. It is defined for 2x2 tables only,
    /// so asking for it on any other shape changes nothing — the same rule
    /// <c>scipy.stats.chi2_contingency</c> follows with <c>correction=True</c>.
    /// </param>
    /// <returns>The statistic, the p-value, the degrees of freedom and the expected table.</returns>
    /// <exception cref="ArgumentException">
    /// The table is empty, ragged, holds a negative count, or has a zero row or column total.
    /// </exception>
    public static Chi2ContingencyResult Contingency(
        double[][] table, Continuity continuity = Continuity.Applied)
    {
        ArgumentNullException.ThrowIfNull(table);

        if (table.Length < 2 || table[0] is null || table[0].Length < 2)
        {
            throw new ArgumentException(
                "A contingency table needs at least two rows and two columns.", nameof(table));
        }

        int rows = table.Length;
        int columns = table[0].Length;

        double[] rowTotals = new double[rows];
        double[] columnTotals = new double[columns];
        double total = 0.0;

        for (int i = 0; i < rows; i++)
        {
            if (table[i] is null || table[i].Length != columns)
            {
                throw new ArgumentException($"Row {i} is not {columns} wide.", nameof(table));
            }

            for (int j = 0; j < columns; j++)
            {
                double value = table[i][j];
                if (value < 0.0 || double.IsNaN(value))
                {
                    throw new ArgumentException(
                        $"Cell [{i}][{j}] is {value}; counts must be non-negative.", nameof(table));
                }

                rowTotals[i] += value;
                columnTotals[j] += value;
                total += value;
            }
        }

        // A zero marginal makes the expectation zero, which the statistic divides
        // by: the factor has a level nothing was observed at, and the table needs
        // that level dropped before the test means anything.
        for (int i = 0; i < rows; i++)
        {
            if (rowTotals[i] == 0.0)
            {
                throw new ArgumentException($"Row {i} totals zero.", nameof(table));
            }
        }
        for (int j = 0; j < columns; j++)
        {
            if (columnTotals[j] == 0.0)
            {
                throw new ArgumentException($"Column {j} totals zero.", nameof(table));
            }
        }

        double[][] expected = new double[rows][];
        for (int i = 0; i < rows; i++)
        {
            expected[i] = new double[columns];
            for (int j = 0; j < columns; j++)
            {
                expected[i][j] = rowTotals[i] * columnTotals[j] / total;
            }
        }

        bool yates = continuity == Continuity.Applied && rows == 2 && columns == 2;

        double statistic = 0.0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                double deviation = Math.Abs(table[i][j] - expected[i][j]);

                // Yates moves the observation half a unit toward the expectation,
                // never past it: on a table whose cells already agree within half
                // a count, the correction would otherwise overshoot into a
                // negative contribution.
                if (yates)
                {
                    deviation = Math.Max(0.0, deviation - 0.5);
                }

                statistic += deviation * deviation / expected[i][j];
            }
        }

        int dof = (rows - 1) * (columns - 1);
        double pValue = Gamma.RegularizedQ(dof / 2.0, statistic / 2.0);

        return new Chi2ContingencyResult(statistic, pValue, dof, expected);
    }
}
```

- [ ] **Step 4: Write `src/Lodestar.Stats/FisherExact.cs`**

```csharp
using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>Fisher's exact test on a 2x2 contingency table.</summary>
/// <remarks>
/// Exact rather than asymptotic: the p-value is a sum of hypergeometric
/// probabilities over the tables with the same margins, so it is right at any
/// sample size, where the chi-square approximation needs the cells to be large.
/// </remarks>
public static class FisherExact
{
    // Two tables whose probabilities differ only in the last bits are the same
    // table for this purpose, and a bare <= would include or exclude one of them
    // by rounding. scipy guards the comparison the same way.
    private const double ProbabilityTolerance = 1e-7;

    /// <summary>Tests a 2x2 table for association.</summary>
    /// <param name="table">The counts, as two rows of two.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <returns>
    /// The conditional odds ratio — <c>PositiveInfinity</c> when the second
    /// diagonal is zero, <c>NaN</c> when both diagonals are — and the p-value.
    /// </returns>
    /// <exception cref="ArgumentException">The table is not 2x2.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A count is negative.</exception>
    public static TestResult Test(int[][] table, Alternative alternative = Alternative.TwoSided)
    {
        ArgumentNullException.ThrowIfNull(table);

        if (table.Length != 2 || table[0] is not { Length: 2 } || table[1] is not { Length: 2 })
        {
            throw new ArgumentException(
                "Fisher's exact test here is the 2x2 test; give it two rows of two.", nameof(table));
        }

        int a = table[0][0];
        int b = table[0][1];
        int c = table[1][0];
        int d = table[1][1];

        if (a < 0 || b < 0 || c < 0 || d < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(table), "Counts must be non-negative.");
        }

        double oddsRatio = b * c == 0
            ? (a * d == 0 ? double.NaN : double.PositiveInfinity)
            : (double)a * d / ((double)b * c);

        int rowOne = a + b;
        int columnOne = a + c;
        int total = a + b + c + d;

        // With the margins fixed, the whole table is determined by its top-left
        // cell, which ranges over the values that leave every other cell
        // non-negative.
        int lowest = Math.Max(0, columnOne - (total - rowOne));
        int highest = Math.Min(rowOne, columnOne);

        double observed = HypergeometricProbability(a, rowOne, columnOne, total);

        double pValue = 0.0;
        for (int k = lowest; k <= highest; k++)
        {
            double probability = HypergeometricProbability(k, rowOne, columnOne, total);

            bool include = alternative switch
            {
                Alternative.Less => k <= a,
                Alternative.Greater => k >= a,
                Alternative.TwoSided =>
                    probability <= observed * (1.0 + ProbabilityTolerance),
                _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
            };

            if (include)
            {
                pValue += probability;
            }
        }

        return new TestResult(oddsRatio, Math.Min(1.0, pValue));
    }

    // C(rowOne, k) C(total - rowOne, columnOne - k) / C(total, columnOne), through
    // log-gamma: the binomials overflow a double well before the counts do.
    private static double HypergeometricProbability(int k, int rowOne, int columnOne, int total)
    {
        double logProbability =
            LogChoose(rowOne, k) +
            LogChoose(total - rowOne, columnOne - k) -
            LogChoose(total, columnOne);

        return Math.Exp(logProbability);
    }

    private static double LogChoose(int n, int k)
    {
        if (k < 0 || k > n)
        {
            return double.NegativeInfinity;
        }

        return Gamma.LogGamma(n + 1) - Gamma.LogGamma(k + 1) - Gamma.LogGamma(n - k + 1);
    }
}
```

- [ ] **Step 5: Run and iterate**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~ChiSquare|FullyQualifiedName~FisherExact|FullyQualifiedName~TableTestEdge"`

Expected once converged: PASS, **10 tests**. The two-sided Fisher p-value is the
one likely to need a round: if a case fails by exactly one table's probability,
the inclusion tolerance is what to look at, and the "Fisher's tea tasting"
fixture (`[[3,1],[1,3]]`), whose two-sided answer is `0.4857142857142857`, is the
smallest case to reason about.

- [ ] **Step 6: Run the gates and commit**

```bash
dotnet build Lodestar.slnx -c Release
dotnet test tests/Lodestar.Stats.NetStandard.Tests -c Release
dotnet format Lodestar.slnx --verify-no-changes
python3 tools/check_comment_length.py
git add src/Lodestar.Stats tests/Lodestar.Stats.Tests
git commit -m "Lodestar.Stats: the two chi-square calls and Fisher's exact test

Refs #442. Yates's correction applies to 2x2 tables and to nothing else, which
is scipy's rule and not a simplification: asking for it on a 3x2 changes no
number. It moves the observation half a unit toward the expectation and never
past it, so a table already agreeing within half a count does not overshoot.

Fisher's hypergeometric terms go through log-gamma: the binomials overflow a
double long before the counts do. The two-sided tail includes a table whose
probability is within 1e-7 relative of the observed one, because two tables
differing in the last bits are the same table for this purpose."
```

---

### Task 8: `KolmogorovSmirnov`, `OneWayAnova` and `KruskalWallis`

**Files:**

- Create: `src/Lodestar.Stats/KolmogorovSmirnov.cs`,
  `src/Lodestar.Stats/OneWayAnova.cs`, `src/Lodestar.Stats/KruskalWallis.cs`
- Test: `tests/Lodestar.Stats.Tests/KolmogorovSmirnovOracleTests.cs`,
  `tests/Lodestar.Stats.Tests/GroupTestOracleTests.cs`,
  `tests/Lodestar.Stats.Tests/GroupTestEdgeTests.cs`

**Interfaces:**

- Consumes: `Kolmogorov.Sf`, `Beta.FisherSf`, `Gamma.RegularizedQ`,
  `Ranks.Average`, `Ranks.TieCorrection`, `KsResult`.
- Produces:
  - `KsResult KolmogorovSmirnov.TwoSample(ReadOnlySpan<double> a, ReadOnlySpan<double> b, Alternative alternative = Alternative.TwoSided, ExactMethod method = ExactMethod.Auto)`
  - `TestResult OneWayAnova.Test(params double[][] groups)`
  - `TestResult KruskalWallis.Test(params double[][] groups)`

- [ ] **Step 1: Write the failing tests**

`tests/Lodestar.Stats.Tests/KolmogorovSmirnovOracleTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>Replays <c>tests/oracles/stats_ks.json</c>.</summary>
public sealed class KolmogorovSmirnovOracleTests
{
    [Fact]
    public void Every_case_matches_scipy()
    {
        using JsonDocument document = StatsCorpus.Load("stats_ks.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            JsonElement args = c.GetProperty("args");

            KsResult result = KolmogorovSmirnov.TwoSample(
                StatsCorpus.Doubles(c.GetProperty("a")),
                StatsCorpus.Doubles(c.GetProperty("b")),
                StatsCorpus.Alternative(args),
                StatsCorpus.Method(args));

            StatsOracleAsserts.Statistic(
                c.GetProperty("statistic").GetDouble(), result.Statistic, name);
            StatsOracleAsserts.PValue(c.GetProperty("pvalue").GetDouble(), result.PValue, name);
            StatsOracleAsserts.Statistic(
                c.GetProperty("statistic_location").GetDouble(),
                result.StatisticLocation, $"{name} location");
            Assert.Equal(c.GetProperty("statistic_sign").GetInt32(), result.StatisticSign);
            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
```

`tests/Lodestar.Stats.Tests/GroupTestOracleTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>Replays the two k-sample corpora, which share a fixture shape.</summary>
public sealed class GroupTestOracleTests
{
    [Theory]
    [InlineData("stats_anova.json")]
    [InlineData("stats_kruskal.json")]
    public void Every_case_matches_scipy(string fileName)
    {
        using JsonDocument document = StatsCorpus.Load(fileName);
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = $"{fileName}: {c.GetProperty("name").GetString()}";
            double[][] groups = StatsCorpus.Table(c.GetProperty("groups"));

            TestResult result = c.GetProperty("call").GetString() == "f_oneway"
                ? OneWayAnova.Test(groups)
                : KruskalWallis.Test(groups);

            StatsOracleAsserts.Statistic(
                c.GetProperty("statistic").GetDouble(), result.Statistic, name);
            StatsOracleAsserts.PValue(c.GetProperty("pvalue").GetDouble(), result.PValue, name);
            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
```

`tests/Lodestar.Stats.Tests/GroupTestEdgeTests.cs`:

```csharp
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>What the three multi-sample tests refuse.</summary>
public sealed class GroupTestEdgeTests
{
    [Fact]
    public void Anova_refuses_fewer_than_two_groups()
    {
        Assert.Throws<ArgumentException>(() => OneWayAnova.Test([1.0, 2.0, 3.0]));
    }

    [Fact]
    public void Anova_refuses_an_empty_group()
    {
        Assert.Throws<ArgumentException>(
            () => OneWayAnova.Test([1.0, 2.0], [], [3.0, 4.0]));
    }

    [Fact]
    public void Anova_of_identical_groups_has_no_between_group_variance()
    {
        TestResult result = OneWayAnova.Test([1.0, 2.0, 3.0], [1.0, 2.0, 3.0]);

        Assert.Equal(0.0, result.Statistic, 1e-12);
        Assert.Equal(1.0, result.PValue, 1e-12);
    }

    [Fact]
    public void Kruskal_refuses_fewer_than_two_groups_and_an_empty_group()
    {
        Assert.Throws<ArgumentException>(() => KruskalWallis.Test([1.0, 2.0, 3.0]));
        Assert.Throws<ArgumentException>(
            () => KruskalWallis.Test([1.0, 2.0], [], [3.0, 4.0]));
    }

    [Fact]
    public void Ks_refuses_an_empty_sample()
    {
        Assert.Throws<ArgumentException>(() => KolmogorovSmirnov.TwoSample([], [1.0, 2.0]));
    }

    [Fact]
    public void Ks_of_a_sample_against_itself_is_a_distance_of_zero()
    {
        double[] sample = [1.0, 2.0, 3.0, 4.0];

        KsResult result = KolmogorovSmirnov.TwoSample(sample, sample);

        Assert.Equal(0.0, result.Statistic);
        Assert.Equal(1.0, result.PValue);
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~Kolmogorov|FullyQualifiedName~GroupTest"`
Expected: FAIL — none of the three types exists.

- [ ] **Step 3: Write `src/Lodestar.Stats/KolmogorovSmirnov.cs`**

```csharp
using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>The two-sample Kolmogorov-Smirnov test.</summary>
/// <remarks>
/// <b>Two-sample only.</b> The one-sample test compares a sample against a named
/// distribution, which means passing a cumulative distribution function; this
/// package has no distributions namespace to pass one from, and inventing one to
/// serve a single test is a second package's worth of surface.
/// </remarks>
public static class KolmogorovSmirnov
{
    // scipy takes the exact branch while the lattice stays small; above this the
    // table costs more than the asymptotic answer is worth.
    private const long AutoExactLimit = 10_000;

    /// <summary>Compares two samples by the largest gap between their empirical distributions.</summary>
    /// <param name="a">The first sample; at least one value.</param>
    /// <param name="b">The second sample; at least one value.</param>
    /// <param name="alternative">
    /// <see cref="Alternative.TwoSided"/> takes the largest gap in either
    /// direction; the one-sided values take the largest gap in one.
    /// </param>
    /// <param name="method">Exact, asymptotic, or chosen by the sample sizes.</param>
    /// <returns>The distance, the p-value, where the distance was reached and its sign.</returns>
    /// <exception cref="ArgumentException">Either sample is empty.</exception>
    public static KsResult TwoSample(
        ReadOnlySpan<double> a,
        ReadOnlySpan<double> b,
        Alternative alternative = Alternative.TwoSided,
        ExactMethod method = ExactMethod.Auto)
    {
        if (a.Length == 0)
        {
            throw new ArgumentException("The first sample is empty.", nameof(a));
        }
        if (b.Length == 0)
        {
            throw new ArgumentException("The second sample is empty.", nameof(b));
        }

        int n = a.Length;
        int m = b.Length;

        double[] sortedA = a.ToArray();
        double[] sortedB = b.ToArray();
        Array.Sort(sortedA);
        Array.Sort(sortedB);

        // Walk the merged order once, tracking both empirical distributions. The
        // supremum can only change at an observed value, so those are the only
        // points worth evaluating.
        double statistic = 0.0;
        double location = double.NaN;
        int sign = 0;

        int i = 0;
        int j = 0;
        while (i < n && j < m)
        {
            double value = Math.Min(sortedA[i], sortedB[j]);

            while (i < n && sortedA[i] == value)
            {
                i++;
            }
            while (j < m && sortedB[j] == value)
            {
                j++;
            }

            double difference = ((double)i / n) - ((double)j / m);
            double candidate = alternative switch
            {
                Alternative.Less => -difference,
                Alternative.Greater => difference,
                Alternative.TwoSided => Math.Abs(difference),
                _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
            };

            if (candidate > statistic)
            {
                statistic = candidate;
                location = value;
                sign = difference >= 0.0 ? 1 : -1;
            }
        }

        if (double.IsNaN(location))
        {
            // The two empirical distributions never separated, so no observation
            // attains the supremum; scipy reports the smallest value and a
            // positive sign there.
            location = Math.Min(sortedA[0], sortedB[0]);
            sign = 1;
        }

        bool exact = method switch
        {
            ExactMethod.Exact => true,
            ExactMethod.Asymptotic => false,
            _ => (long)n * m <= AutoExactLimit,
        };

        double pValue = exact
            ? ExactPValue(statistic, n, m, alternative)
            : AsymptoticPValue(statistic, n, m, alternative);

        return new KsResult(statistic, Math.Min(1.0, Math.Max(0.0, pValue)), location, sign);
    }

    private static double AsymptoticPValue(double d, int n, int m, Alternative alternative)
    {
        double effective = (double)n * m / (n + m);

        // Two-sided uses the Kolmogorov series; one-sided has the closed form
        // exp(-2 en d^2), which is the Smirnov limit rather than an approximation
        // of the series.
        return alternative == Alternative.TwoSided
            ? Kolmogorov.Sf(Math.Sqrt(effective) * d)
            : Math.Exp(-2.0 * effective * d * d);
    }

    // The exact tail counts the lattice paths from (0,0) to (n,m) that never let
    // the two empirical distributions separate by d or more, and divides by
    // C(n+m, n). Counting rather than enumerating: the paths number in the
    // billions where the table has (n+1)(m+1) entries.
    private static double ExactPValue(double d, int n, int m, Alternative alternative)
    {
        // Half a lattice step of slack, so a path exactly at the boundary counts
        // as inside: the statistic is attained, not exceeded.
        double bound = d - (0.5 / ((double)n * m));

        double[,] paths = new double[n + 1, m + 1];
        paths[0, 0] = 1.0;

        for (int i = 0; i <= n; i++)
        {
            for (int j = 0; j <= m; j++)
            {
                if (i == 0 && j == 0)
                {
                    continue;
                }

                double difference = ((double)i / n) - ((double)j / m);
                bool outside = alternative switch
                {
                    Alternative.Less => -difference >= bound,
                    Alternative.Greater => difference >= bound,
                    _ => Math.Abs(difference) >= bound,
                };

                if (outside)
                {
                    paths[i, j] = 0.0;
                    continue;
                }

                double fromLeft = i > 0 ? paths[i - 1, j] : 0.0;
                double fromBelow = j > 0 ? paths[i, j - 1] : 0.0;

                // Normalised as it goes: dividing each step by C(i+j, i) keeps
                // the value in [0, 1], where the raw counts reach C(n+m, n) and
                // overflow a double at a few hundred values each.
                paths[i, j] = ((fromLeft * i) + (fromBelow * j)) / (i + j);
            }
        }

        double inside = paths[n, m];
        return 1.0 - inside;
    }
}
```

**Note for the implementer.** The `ExactPValue` recursion above is the shape of
the answer, not necessarily its final form: the normalisation that keeps the
counts inside a `double` is the fiddly part, and the corpus's `method="exact"`
cases are what decide it. If the walk disagrees with scipy, reproduce one case
in Python and compare intermediate path counts —

```bash
REPO=$(git rev-parse --show-toplevel); cd "$(mktemp -d)"
PYTHONSAFEPATH=1 "$REPO/.venv-oracles/bin/python" -c "
from scipy import stats
print(stats.ks_2samp([1.,4.,7.,9.], [2.,3.,8.,12.,15.],
                     alternative='two-sided', method='exact'))
"
```

— and fix the recursion. Do not weaken the corpus to fit the walk.

- [ ] **Step 4: Write `src/Lodestar.Stats/OneWayAnova.cs`**

```csharp
using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>One-way analysis of variance: do several groups share one mean?</summary>
/// <remarks>
/// The k-sample generalisation of <see cref="TTest.Independent"/> with
/// <see cref="Variance.Equal"/>: on two groups the F statistic is the square of
/// Student's t, and the two p-values agree.
/// </remarks>
public static class OneWayAnova
{
    /// <summary>Compares the means of two or more groups.</summary>
    /// <param name="groups">The groups; at least two, each holding at least one value.</param>
    /// <returns>The F statistic and the upper-tail p-value.</returns>
    /// <exception cref="ArgumentException">
    /// Fewer than two groups, an empty group, or no group holding more than one value.
    /// </exception>
    public static TestResult Test(params double[][] groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        if (groups.Length < 2)
        {
            throw new ArgumentException(
                $"An analysis of variance needs at least two groups; got {groups.Length}.",
                nameof(groups));
        }

        int total = 0;
        double grandSum = 0.0;
        for (int g = 0; g < groups.Length; g++)
        {
            if (groups[g] is not { Length: > 0 })
            {
                throw new ArgumentException($"Group {g} is empty.", nameof(groups));
            }

            for (int i = 0; i < groups[g].Length; i++)
            {
                grandSum += groups[g][i];
                total++;
            }
        }

        if (total <= groups.Length)
        {
            throw new ArgumentException(
                "The within-group degrees of freedom are zero: every group holds one value.",
                nameof(groups));
        }

        double grandMean = grandSum / total;

        double between = 0.0;
        double within = 0.0;
        for (int g = 0; g < groups.Length; g++)
        {
            double sum = 0.0;
            for (int i = 0; i < groups[g].Length; i++)
            {
                sum += groups[g][i];
            }

            double mean = sum / groups[g].Length;
            double deviation = mean - grandMean;
            between += groups[g].Length * deviation * deviation;

            for (int i = 0; i < groups[g].Length; i++)
            {
                double residual = groups[g][i] - mean;
                within += residual * residual;
            }
        }

        double dfBetween = groups.Length - 1;
        double dfWithin = total - groups.Length;
        double statistic = (between / dfBetween) / (within / dfWithin);

        return new TestResult(statistic, Beta.FisherSf(statistic, dfBetween, dfWithin));
    }
}
```

- [ ] **Step 5: Write `src/Lodestar.Stats/KruskalWallis.cs`**

```csharp
using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>The Kruskal-Wallis H test: the rank-based k-sample comparison.</summary>
/// <remarks>
/// What <see cref="OneWayAnova"/> is to <see cref="TTest.Independent"/>, this is
/// to <see cref="MannWhitney"/>: on two groups it agrees with Mann-Whitney's
/// asymptotic two-sided p-value.
/// </remarks>
public static class KruskalWallis
{
    /// <summary>Compares two or more groups by their ranks in the pooled sample.</summary>
    /// <param name="groups">The groups; at least two, each holding at least one value.</param>
    /// <returns>The H statistic and the upper-tail p-value.</returns>
    /// <exception cref="ArgumentException">
    /// Fewer than two groups, an empty group, or a pooled sample in which every value is tied.
    /// </exception>
    public static TestResult Test(params double[][] groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        if (groups.Length < 2)
        {
            throw new ArgumentException(
                $"Kruskal-Wallis needs at least two groups; got {groups.Length}.", nameof(groups));
        }

        int total = 0;
        for (int g = 0; g < groups.Length; g++)
        {
            if (groups[g] is not { Length: > 0 })
            {
                throw new ArgumentException($"Group {g} is empty.", nameof(groups));
            }

            total += groups[g].Length;
        }

        double[] pooled = new double[total];
        int offset = 0;
        for (int g = 0; g < groups.Length; g++)
        {
            groups[g].CopyTo(pooled, offset);
            offset += groups[g].Length;
        }

        double[] ranks = Ranks.Average(pooled);

        double weighted = 0.0;
        offset = 0;
        for (int g = 0; g < groups.Length; g++)
        {
            double sum = 0.0;
            for (int i = 0; i < groups[g].Length; i++)
            {
                sum += ranks[offset + i];
            }

            weighted += sum * sum / groups[g].Length;
            offset += groups[g].Length;
        }

        double h = (12.0 / (total * (total + 1.0)) * weighted) - (3.0 * (total + 1.0));

        // Ties shrink the spread of the ranks, so the statistic is divided by
        // what is left of it. Every value tied leaves nothing, and the test has
        // no answer rather than a division by zero.
        double tieCorrection = 1.0 -
            (Ranks.TieCorrection(pooled) / (((double)total * total * total) - total));

        if (tieCorrection <= 0.0)
        {
            throw new ArgumentException(
                "Every value in the pooled sample is tied, so the ranks carry no information.",
                nameof(groups));
        }

        h /= tieCorrection;
        double dof = groups.Length - 1;

        return new TestResult(h, Gamma.RegularizedQ(dof / 2.0, h / 2.0));
    }
}
```

- [ ] **Step 6: Run and iterate**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~Kolmogorov|FullyQualifiedName~GroupTest"`
Expected once converged: PASS, **9 tests** (one KS replay, two group replays via
`InlineData`, six edge facts).

Cross-check the two identities the remarks claim, as a one-off probe rather than
a committed test: `OneWayAnova.Test(a, b).Statistic` must equal
`TTest.Independent(a, b, Alternative.TwoSided, Variance.Equal).Statistic` squared,
and their p-values must agree. If they do not, the ANOVA is wrong, not the t-test.

- [ ] **Step 7: Run the gates and commit**

```bash
dotnet build Lodestar.slnx -c Release
dotnet test tests/Lodestar.Stats.NetStandard.Tests -c Release
dotnet format Lodestar.slnx --verify-no-changes
python3 tools/check_comment_length.py
git add src/Lodestar.Stats tests/Lodestar.Stats.Tests
git commit -m "Lodestar.Stats: two-sample KS, one-way ANOVA and Kruskal-Wallis

Refs #442. KS is two-sample only: the one-sample test needs a cumulative
distribution function passed in, and this package has no distributions namespace
to pass one from. Its exact tail counts lattice paths and normalises as it goes,
because the raw counts reach C(n+m, n) and overflow a double at a few hundred
values each.

Kruskal-Wallis divides by what the ties leave of the rank spread, and refuses
rather than dividing by zero when every value is tied."
```

---

### Task 9: `ShapiroWilk` and `MultipleComparisons`

**Files:**

- Create: `src/Lodestar.Stats/ShapiroWilk.cs`, `src/Lodestar.Stats/MultipleComparisons.cs`
- Modify: `src/Lodestar.Stats/Internal/Normal.cs` — add `Quantile`
- Test: `tests/Lodestar.Stats.Tests/ShapiroWilkOracleTests.cs`,
  `tests/Lodestar.Stats.Tests/MultipleComparisonsOracleTests.cs`,
  `tests/Lodestar.Stats.Tests/ShapiroWilkEdgeTests.cs`,
  `tests/Lodestar.Stats.Tests/MultipleComparisonsEdgeTests.cs`

**Interfaces:**

- Consumes: `Normal.Sf`, `Ranks.Average`.
- Produces:
  - `double Normal.Quantile(double p)` — the `z` with `P(Z > z) = p`
  - `TestResult ShapiroWilk.Test(ReadOnlySpan<double> sample)`
  - `double[] MultipleComparisons.Bonferroni(ReadOnlySpan<double> pValues)`
  - `double[] MultipleComparisons.BenjaminiHochberg(ReadOnlySpan<double> pValues)`
  - `double[] MultipleComparisons.BenjaminiYekutieli(ReadOnlySpan<double> pValues)`

**Provenance for AS R94.** Royston's 1995 *Applied Statistics* 44:547-551
algorithm is a **published description with published constants**; writing it
from that description is what ADR 0003 permits. The constants below are the
paper's own polynomial coefficients, not a transcription of anyone's code.

- [ ] **Step 1: Write the failing test for `Normal.Quantile`**

Append to `tests/Lodestar.Stats.Tests/Internal/NormalTests.cs`:

```csharp
    [Theory]
    [InlineData(0.5, 0.0)]
    [InlineData(0.025, 1.959963984540054)]
    [InlineData(0.05, 1.6448536269514722)]
    [InlineData(0.975, -1.959963984540054)]
    [InlineData(1e-10, 6.361340902404056)]
    public void Quantile_inverts_the_upper_tail(double p, double expected)
    {
        Assert.Equal(expected, Normal.Quantile(p), 1e-9);
    }

    [Fact]
    public void Quantile_refuses_a_probability_outside_the_open_unit_interval()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Normal.Quantile(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Normal.Quantile(1.0));
    }
```

- [ ] **Step 2: Add `Quantile` to `src/Lodestar.Stats/Internal/Normal.cs`**

```csharp
    /// <summary>The z with <c>P(Z &gt; z) = p</c>: the inverse of <see cref="Sf"/>.</summary>
    /// <remarks>
    /// By bisection, for the reason <c>Beta.StudentQuantile</c> gives: one
    /// approximation to keep right instead of two that must agree.
    /// </remarks>
    internal static double Quantile(double p)
    {
        if (double.IsNaN(p) || p <= 0.0 || p >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(p), p, "The tail probability must lie strictly inside (0, 1).");
        }

        if (p == 0.5)
        {
            return 0.0;
        }

        double high = 1.0;
        while (Sf(high) > p && high < 1e10)
        {
            high *= 2.0;
        }

        double low = -high;
        for (int i = 0; i < 200; i++)
        {
            double middle = 0.5 * (low + high);
            if (middle == low || middle == high)
            {
                break;
            }

            if (Sf(middle) > p)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return 0.5 * (low + high);
    }
```

- [ ] **Step 3: Run to verify the quantile works**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~NormalTests"`
Expected: PASS, **17 tests** — the ten from Task 2 plus these seven.

- [ ] **Step 4: Write the failing oracle replays and edge tests**

`tests/Lodestar.Stats.Tests/ShapiroWilkOracleTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>Replays <c>tests/oracles/stats_shapiro.json</c>.</summary>
public sealed class ShapiroWilkOracleTests
{
    [Fact]
    public void Every_case_matches_scipy()
    {
        using JsonDocument document = StatsCorpus.Load("stats_shapiro.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;

            TestResult result = ShapiroWilk.Test(StatsCorpus.Doubles(c.GetProperty("x")));

            // The statistic is Royston's rational approximation on both sides, so
            // it agrees to the repository's absolute tolerance; the p-value is a
            // normal tail of a fitted transform, which is why it is relative.
            StatsOracleAsserts.Statistic(
                c.GetProperty("statistic").GetDouble(), result.Statistic, name);
            StatsOracleAsserts.PValue(c.GetProperty("pvalue").GetDouble(), result.PValue, name);
            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
```

`tests/Lodestar.Stats.Tests/MultipleComparisonsOracleTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Stats.Tests.Oracles;
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>
/// Replays <c>tests/oracles/stats_multiple_comparisons.json</c>: BH and BY
/// against scipy, Bonferroni against its own definition.
/// </summary>
public sealed class MultipleComparisonsOracleTests
{
    [Fact]
    public void Every_case_matches_scipy_and_the_bonferroni_definition()
    {
        using JsonDocument document = StatsCorpus.Load("stats_multiple_comparisons.json");
        int replayed = 0;

        foreach (JsonElement c in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            double[] p = StatsCorpus.Doubles(c.GetProperty("p"));

            StatsOracleAsserts.Vector(
                StatsCorpus.Doubles(c.GetProperty("bonferroni")),
                MultipleComparisons.Bonferroni(p), $"{name} bonferroni");
            StatsOracleAsserts.Vector(
                StatsCorpus.Doubles(c.GetProperty("bh")),
                MultipleComparisons.BenjaminiHochberg(p), $"{name} bh");
            StatsOracleAsserts.Vector(
                StatsCorpus.Doubles(c.GetProperty("by")),
                MultipleComparisons.BenjaminiYekutieli(p), $"{name} by");
            replayed++;
        }

        Assert.Equal(document.RootElement.GetProperty("metadata").GetProperty("count").GetInt32(),
                     replayed);
    }
}
```

`tests/Lodestar.Stats.Tests/ShapiroWilkEdgeTests.cs`:

```csharp
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>What Shapiro-Wilk refuses, and the range Royston's approximation covers.</summary>
public sealed class ShapiroWilkEdgeTests
{
    [Fact]
    public void Refuses_fewer_than_three_values()
    {
        Assert.Throws<ArgumentException>(() => ShapiroWilk.Test([1.0, 2.0]));
    }

    [Fact]
    public void Refuses_a_sample_with_no_spread()
    {
        // Every value identical: the statistic's denominator is zero, and there
        // is nothing to compare a normal shape against.
        Assert.Throws<ArgumentException>(() => ShapiroWilk.Test([2.0, 2.0, 2.0, 2.0]));
    }

    [Fact]
    public void Refuses_a_sample_above_five_thousand()
    {
        // Royston's normalising transform is fitted to n <= 5000; scipy warns and
        // answers anyway, which is a number nobody should read. Refusing says so.
        double[] tooMany = [.. Enumerable.Range(0, 5001).Select(i => (double)i)];

        Assert.Throws<ArgumentException>(() => ShapiroWilk.Test(tooMany));
    }

    [Fact]
    public void A_normal_looking_sample_is_not_rejected()
    {
        double[] sample =
        [
            -1.62, -1.10, -0.74, -0.47, -0.23, 0.0, 0.23, 0.47, 0.74, 1.10, 1.62,
        ];

        TestResult result = ShapiroWilk.Test(sample);

        Assert.True(result.Statistic is > 0.9 and <= 1.0);
        Assert.True(result.PValue > 0.05);
    }
}
```

`tests/Lodestar.Stats.Tests/MultipleComparisonsEdgeTests.cs`:

```csharp
using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>The three corrections' shared contract.</summary>
public sealed class MultipleComparisonsEdgeTests
{
    [Fact]
    public void All_three_refuse_an_empty_family()
    {
        Assert.Throws<ArgumentException>(() => MultipleComparisons.Bonferroni([]));
        Assert.Throws<ArgumentException>(() => MultipleComparisons.BenjaminiHochberg([]));
        Assert.Throws<ArgumentException>(() => MultipleComparisons.BenjaminiYekutieli([]));
    }

    [Fact]
    public void All_three_refuse_a_p_value_outside_the_unit_interval()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MultipleComparisons.Bonferroni([0.5, 1.5]));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MultipleComparisons.BenjaminiHochberg([-0.1, 0.5]));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MultipleComparisons.BenjaminiYekutieli([double.NaN]));
    }

    [Fact]
    public void All_three_return_the_adjusted_values_in_the_input_order()
    {
        double[] adjusted = MultipleComparisons.BenjaminiHochberg([0.3, 0.001, 0.02]);

        // The smallest input is at index 1, so the smallest adjusted value is too.
        Assert.True(adjusted[1] < adjusted[2]);
        Assert.True(adjusted[2] < adjusted[0]);
    }

    [Fact]
    public void Bonferroni_multiplies_by_the_family_size_and_clamps_at_one()
    {
        Assert.Equal([0.12, 0.6, 1.0], MultipleComparisons.Bonferroni([0.04, 0.2, 0.9]));
    }

    [Fact]
    public void Yekutieli_is_never_smaller_than_hochberg()
    {
        double[] p = [0.001, 0.008, 0.039, 0.041, 0.042];
        double[] bh = MultipleComparisons.BenjaminiHochberg(p);
        double[] by = MultipleComparisons.BenjaminiYekutieli(p);

        for (int i = 0; i < p.Length; i++)
        {
            Assert.True(by[i] >= bh[i] - 1e-15, $"by[{i}] = {by[i]} fell below bh[{i}] = {bh[i]}.");
        }
    }
}
```

- [ ] **Step 5: Run to verify they fail**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~ShapiroWilk|FullyQualifiedName~MultipleComparisons"`
Expected: FAIL — neither type exists.

- [ ] **Step 6: Write `src/Lodestar.Stats/ShapiroWilk.cs`**

```csharp
using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>The Shapiro-Wilk test for normality, by Royston's AS R94.</summary>
/// <remarks>
/// Written from Royston's 1995 published description and its published
/// polynomial constants (Applied Statistics 44:547-551), not from any
/// implementation of it (ADR 0003).
///
/// The transform that turns the statistic into a p-value is fitted for
/// <c>3 &lt;= n &lt;= 5000</c>. Outside that range there is no p-value to give,
/// so this refuses rather than extrapolating a number a reader would take at
/// face value; scipy warns and answers anyway.
/// </remarks>
public static class ShapiroWilk
{
    private const int MinimumSample = 3;
    private const int MaximumSample = 5000;

    // Royston's polynomial coefficients, ascending. The first two correct the
    // last two Blom weights; the rest carry the normalising transform.
    private static readonly double[] WeightCorrectionLast =
        [0.0, 0.221157, -0.147981, -2.071190, 4.434685, -2.706056];

    private static readonly double[] WeightCorrectionSecondLast =
        [0.0, 0.042981, -0.293762, -1.752461, 5.682633, -3.582633];

    private static readonly double[] SmallMu = [0.5440, -0.39978, 0.025054, -6.714e-4];
    private static readonly double[] SmallSigma = [1.3822, -0.77857, 0.062767, -0.0020322];
    private static readonly double[] LargeMu = [-1.5861, -0.31082, -0.083751, 0.0038915];
    private static readonly double[] LargeSigma = [-0.4803, -0.082676, 0.0030302];

    /// <summary>Tests whether a sample could have come from a normal distribution.</summary>
    /// <param name="sample">The sample; between 3 and 5000 values, not all equal.</param>
    /// <returns>Royston's W statistic and its p-value.</returns>
    /// <exception cref="ArgumentException">
    /// Fewer than 3 or more than 5000 values, or every value identical.
    /// </exception>
    public static TestResult Test(ReadOnlySpan<double> sample)
    {
        int n = sample.Length;
        if (n < MinimumSample || n > MaximumSample)
        {
            throw new ArgumentException(
                $"Royston's approximation covers {MinimumSample} to {MaximumSample} values; got {n}.",
                nameof(sample));
        }

        double[] sorted = sample.ToArray();
        Array.Sort(sorted);

        double[] weights = Weights(n);

        double mean = 0.0;
        for (int i = 0; i < n; i++)
        {
            mean += sorted[i];
        }
        mean /= n;

        double numerator = 0.0;
        double denominator = 0.0;
        for (int i = 0; i < n; i++)
        {
            numerator += weights[i] * sorted[i];
            double deviation = sorted[i] - mean;
            denominator += deviation * deviation;
        }

        if (denominator <= 0.0)
        {
            throw new ArgumentException(
                "Every value in the sample is identical, so there is no shape to test.",
                nameof(sample));
        }

        double w = numerator * numerator / denominator;

        return new TestResult(w, PValue(w, n));
    }

    // Royston's weights: the Blom scores rescaled to unit length, with the
    // largest one -- or two, above n = 5 -- replaced by his polynomial
    // corrections, and the rest rescaled so the vector stays unit length.
    private static double[] Weights(int n)
    {
        double[] blom = new double[n];
        double sumSquares = 0.0;
        for (int i = 0; i < n; i++)
        {
            // Blom's plotting position, through the *upper*-tail inverse, so the
            // sign is negated to put the smallest score first.
            double p = ((i + 1) - 0.375) / (n + 0.25);
            blom[i] = -Normal.Quantile(p);
            sumSquares += blom[i] * blom[i];
        }

        double norm = Math.Sqrt(sumSquares);
        double u = 1.0 / Math.Sqrt(n);

        double[] weights = new double[n];
        int corrected = n > 5 ? 2 : 1;

        double top = (blom[n - 1] / norm) + Polynomial(WeightCorrectionLast, u);
        weights[n - 1] = top;
        weights[0] = -top;

        double replacedRaw = blom[n - 1] * blom[n - 1];
        double replacedCorrected = top * top;

        if (corrected == 2)
        {
            double second = (blom[n - 2] / norm) + Polynomial(WeightCorrectionSecondLast, u);
            weights[n - 2] = second;
            weights[1] = -second;

            replacedRaw += blom[n - 2] * blom[n - 2];
            replacedCorrected += second * second;
        }

        // What the corrected weights left for the rest to share. Dividing by the
        // square root of it is what keeps the whole vector unit length after two
        // of its entries were replaced by values that do not come from Blom.
        double remaining =
            (sumSquares - (2.0 * replacedRaw)) / (1.0 - (2.0 * replacedCorrected));
        double scale = Math.Sqrt(remaining);

        for (int i = corrected; i < n - corrected; i++)
        {
            weights[i] = blom[i] / scale;
        }

        return weights;
    }

    private static double PValue(double w, int n)
    {
        if (n == 3)
        {
            // Royston gives the n = 3 case in closed form: the null distribution
            // of W is exactly known there, so no transform is fitted.
            double p = 1.909859 * (Math.Asin(Math.Sqrt(w)) - 1.047198);
            return Math.Min(1.0, Math.Max(0.0, p));
        }

        double logN = Math.Log(n);
        double y = Math.Log(1.0 - w);

        double mu;
        double sigma;
        if (n <= 11)
        {
            double gamma = -2.273 + (0.459 * n);
            mu = Polynomial(SmallMu, n);
            sigma = Math.Exp(Polynomial(SmallSigma, n));

            // Below twelve, W is transformed through gamma first; above it, the
            // transform is in log n instead.
            y = -Math.Log(gamma - y);
        }
        else
        {
            mu = Polynomial(LargeMu, logN);
            sigma = Math.Exp(Polynomial(LargeSigma, logN));
        }

        return Normal.Sf((y - mu) / sigma);
    }

    private static double Polynomial(double[] coefficients, double x)
    {
        double result = 0.0;
        for (int i = coefficients.Length - 1; i >= 0; i--)
        {
            result = (result * x) + coefficients[i];
        }

        return result;
    }
}
```

**Note for the implementer.** AS R94's weight correction is the fiddliest part of
this task, and the block above is the published rule rather than a finished
optimisation. The corpus's five samples — `n` of 7, 10, 20, 50 and 200 — are what
decide it, and the smallest is the one to debug against:

```bash
REPO=$(git rev-parse --show-toplevel); cd "$(mktemp -d)"
PYTHONSAFEPATH=1 "$REPO/.venv-oracles/bin/python" -c "
import numpy as np
from scipy import stats
x = np.array([1.,1.,2.,2.,3.,3.,4.,5.,9.,9.])
print(stats.shapiro(x))
"
```

If the statistic agrees and only the p-value does not, the bug is in the
transform; if the statistic does not, it is in the weights. Fix the code, never
the corpus.

- [ ] **Step 7: Write `src/Lodestar.Stats/MultipleComparisons.cs`**

```csharp
namespace Lodestar.Stats;

/// <summary>Adjusting a family of p-values for the number of tests in it.</summary>
/// <remarks>
/// Twenty tests at the five-percent level produce one significant result by
/// chance alone. These three rules answer that, and they answer different
/// questions: Bonferroni controls the chance of <i>any</i> false positive,
/// while the Benjamini rules control the expected <i>proportion</i> of false
/// positives among the results called significant.
///
/// Each returns adjusted p-values in the input's own order, so an adjusted
/// value can be compared against the level the caller already had in mind.
/// </remarks>
public static class MultipleComparisons
{
    /// <summary>Multiplies each p-value by the family size, clamped at one.</summary>
    /// <param name="pValues">The family; at least one value, each in <c>[0, 1]</c>.</param>
    /// <returns>The adjusted p-values, in the input's order.</returns>
    /// <exception cref="ArgumentException"><paramref name="pValues"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A value is NaN or outside <c>[0, 1]</c>.</exception>
    public static double[] Bonferroni(ReadOnlySpan<double> pValues)
    {
        Validate(pValues);

        double[] adjusted = new double[pValues.Length];
        for (int i = 0; i < pValues.Length; i++)
        {
            adjusted[i] = Math.Min(1.0, pValues[i] * pValues.Length);
        }

        return adjusted;
    }

    /// <summary>The Benjamini-Hochberg step-up procedure.</summary>
    /// <param name="pValues">The family; at least one value, each in <c>[0, 1]</c>.</param>
    /// <returns>The adjusted p-values, in the input's order.</returns>
    /// <exception cref="ArgumentException"><paramref name="pValues"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A value is NaN or outside <c>[0, 1]</c>.</exception>
    public static double[] BenjaminiHochberg(ReadOnlySpan<double> pValues) =>
        StepUp(pValues, factor: 1.0);

    /// <summary>The Benjamini-Yekutieli procedure, valid under any dependence.</summary>
    /// <remarks>
    /// Benjamini-Hochberg assumes the tests are independent or positively
    /// dependent. Yekutieli's correction drops that assumption at the price of a
    /// harmonic-sum factor, so its adjusted values are never smaller.
    /// </remarks>
    /// <param name="pValues">The family; at least one value, each in <c>[0, 1]</c>.</param>
    /// <returns>The adjusted p-values, in the input's order.</returns>
    /// <exception cref="ArgumentException"><paramref name="pValues"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A value is NaN or outside <c>[0, 1]</c>.</exception>
    public static double[] BenjaminiYekutieli(ReadOnlySpan<double> pValues)
    {
        Validate(pValues);

        double harmonic = 0.0;
        for (int i = 1; i <= pValues.Length; i++)
        {
            harmonic += 1.0 / i;
        }

        return StepUp(pValues, harmonic);
    }

    private static double[] StepUp(ReadOnlySpan<double> pValues, double factor)
    {
        Validate(pValues);

        int n = pValues.Length;
        int[] order = new int[n];
        double[] sorted = new double[n];
        for (int i = 0; i < n; i++)
        {
            order[i] = i;
            sorted[i] = pValues[i];
        }

        Array.Sort(sorted, order);

        double[] adjusted = new double[n];
        double running = 1.0;

        // Walking down from the largest and keeping the running minimum is what
        // makes the result monotone: without it a p-value could be adjusted below
        // a smaller one, and the ordering the caller reads would be a lie.
        for (int rank = n; rank >= 1; rank--)
        {
            double scaled = sorted[rank - 1] * n * factor / rank;
            running = Math.Min(running, scaled);
            adjusted[order[rank - 1]] = Math.Min(1.0, running);
        }

        return adjusted;
    }

    private static void Validate(ReadOnlySpan<double> pValues)
    {
        if (pValues.Length == 0)
        {
            throw new ArgumentException("The family of p-values is empty.", nameof(pValues));
        }

        for (int i = 0; i < pValues.Length; i++)
        {
            if (double.IsNaN(pValues[i]) || pValues[i] < 0.0 || pValues[i] > 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pValues), pValues[i], $"p-value {i} is not a probability.");
            }
        }
    }
}
```

- [ ] **Step 8: Run and iterate**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~ShapiroWilk|FullyQualifiedName~MultipleComparisons"`
Expected once converged: PASS, **11 tests**.

- [ ] **Step 9: Run the whole suite on both targets**

```bash
dotnet build Lodestar.slnx -c Release
dotnet test Lodestar.slnx -c Release
```

Expected: every assembly green. Read the two `Lodestar.Stats` counts: the mirror
must report exactly one more than the primary.

- [ ] **Step 10: Run the gates and commit**

```bash
dotnet format Lodestar.slnx --verify-no-changes
python3 tools/check_comment_length.py
python3 tools/check_repeated_literals.py --base origin/main
git add src/Lodestar.Stats tests/Lodestar.Stats.Tests
git commit -m "Lodestar.Stats: Shapiro-Wilk and the three multiple-comparison corrections

Refs #442. Shapiro-Wilk is Royston's AS R94, written from the 1995 published
description and its published constants. It refuses outside 3 <= n <= 5000,
where the normalising transform is not fitted: scipy warns and answers anyway,
which is a number nobody should read.

Bonferroni has no scipy oracle and none is added -- it is min(p * n, 1), a
definition, and its corpus states the definition. The two Benjamini rules share
one step-up walk that keeps a running minimum from the largest p-value down,
without which an adjusted value could fall below a smaller one's."
```

---

### Task 10: The fourteen samples, which are the packaging gate

**Files:**

- Create, under `samples/Lodestar.Sample/`: `TTestSample.cs`, `MannWhitneySample.cs`,
  `WilcoxonSample.cs`, `ChiSquareSample.cs`, `FisherExactSample.cs`,
  `KolmogorovSmirnovSample.cs`, `OneWayAnovaSample.cs`, `KruskalWallisSample.cs`,
  `ShapiroWilkSample.cs`, `MultipleComparisonsSample.cs`, `TestResultSample.cs`,
  `TTestResultSample.cs`, `Chi2ContingencyResultSample.cs`, `KsResultSample.cs`
- Modify: `samples/Lodestar.Sample/Program.cs`, `samples/Lodestar.Sample/Lodestar.Sample.csproj`
- Modify: `tools/check_sample_coverage.py:34`

**Interfaces:**

- Consumes: the whole public surface of `Lodestar.Stats`.
- Produces: nothing the library uses. The gate is that every public class and
  record is *referenced by a member* from the sample, which is what proves the
  packed package exposes it.

**Fourteen, not nineteen.** `tools/check_sample_coverage.py` follows decision
0041, which excludes an enum: it is demonstrated through the class whose
parameter it is. Five enums, fourteen classes and records.

- [ ] **Step 1: Add the package reference to the sample**

In `samples/Lodestar.Sample/Lodestar.Sample.csproj`, beside the other
`Lodestar.*` package references:

```xml
    <PackageReference Include="Lodestar.Stats" Version="0.1.0" />
```

The sample consumes the packages from `./artifacts` through
`samples/NuGet.config`, so this only resolves after a `dotnet pack`. That is the
point of the gate (ADR 0009).

- [ ] **Step 2: Write `samples/Lodestar.Sample/TTestSample.cs`**

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>
/// The t-tests — is the difference between these two groups more than noise?
/// </summary>
internal static class TTestSample
{
    // Response times in milliseconds, before and after a change, on two
    // independent sets of requests.
    private static readonly double[] Before = [102.0, 98.0, 110.0, 105.0, 99.0, 101.0, 108.0];
    private static readonly double[] After = [95.0, 92.0, 99.0, 91.0, 97.0, 90.0, 94.0, 96.0];

    // The same seven machines measured twice: paired, not independent.
    private static readonly double[] MachineBefore = [102.0, 98.0, 110.0, 105.0, 99.0, 101.0, 108.0];
    private static readonly double[] MachineAfter = [99.0, 96.0, 104.0, 103.0, 95.0, 99.0, 102.0];

    public static void Run()
    {
        Console.WriteLine("t-tests");

        TTestResult welch = TTest.Independent(Before, After);
        Console.WriteLine($"  Welch t               = {Inv.F3(welch.Statistic)}");
        Console.WriteLine($"  Welch p               = {Inv.E3(welch.PValue)}");
        Console.WriteLine($"  Welch df              = {Inv.F3(welch.Df)}");

        TTestResult student = TTest.Independent(
            Before, After, Alternative.TwoSided, Variance.Equal);
        Console.WriteLine($"  Student t             = {Inv.F3(student.Statistic)}");

        TTestResult paired = TTest.Paired(MachineBefore, MachineAfter);
        Console.WriteLine($"  paired t              = {Inv.F3(paired.Statistic)}");

        TTestResult oneSample = TTest.OneSample(After, populationMean: 100.0, Alternative.Less);
        Console.WriteLine($"  one-sample p (less)   = {Inv.E3(oneSample.PValue)}");
    }
}
```

- [ ] **Step 3: Write `samples/Lodestar.Sample/TTestResultSample.cs`**

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>
/// <see cref="TTestResult"/> — a t-test's statistic, p-value, degrees of freedom
/// and the confidence interval it can produce for the difference it measured.
/// </summary>
internal static class TTestResultSample
{
    private static readonly double[] Sample = [12.1, 9.4, 15.0, 11.2, 8.8, 13.9, 10.5];

    public static void Run()
    {
        Console.WriteLine("TTestResult");

        TTestResult result = TTest.OneSample(Sample, populationMean: 10.0);
        (double low, double high) = result.ConfidenceInterval(0.95);

        Console.WriteLine($"  statistic             = {Inv.F3(result.Statistic)}");
        Console.WriteLine($"  p-value               = {Inv.F3(result.PValue)}");
        Console.WriteLine($"  degrees of freedom    = {Inv.F3(result.Df)}");
        Console.WriteLine($"  95 % interval         = [{Inv.F3(low)}, {Inv.F3(high)}]");

        // A one-sided test spends its whole error budget on one side, so the
        // other bound is infinite rather than merely larger.
        (double oneLow, double oneHigh) = TTest
            .OneSample(Sample, populationMean: 10.0, Alternative.Greater)
            .ConfidenceInterval(0.95);
        Console.WriteLine($"  one-sided interval    = [{Inv.F3(oneLow)}, {oneHigh}]");
    }
}
```

- [ ] **Step 4: Write the remaining twelve samples on the same pattern**

Each is a `internal static class <Type>Sample` with a `Run()` that references the
type by a member and prints through `Inv` — the invariant-culture helper
`tools/check_sample_culture.py` requires, so a contributor in a comma-decimal
locale does not produce a different transcript.

`MannWhitneySample.cs`:

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>The Mann-Whitney U test — the rank-based two-sample comparison.</summary>
internal static class MannWhitneySample
{
    private static readonly double[] Control = [7.0, 3.0, 6.0, 2.0, 8.0, 5.0];
    private static readonly double[] Treated = [9.0, 12.0, 8.0, 11.0, 15.0, 10.0];

    public static void Run()
    {
        Console.WriteLine("Mann-Whitney U");

        TestResult exact = MannWhitney.Test(
            Control, Treated, Alternative.Less, Continuity.Applied, ExactMethod.Exact);
        TestResult asymptotic = MannWhitney.Test(
            Control, Treated, Alternative.Less, Continuity.Applied, ExactMethod.Asymptotic);

        Console.WriteLine($"  U                     = {Inv.F3(exact.Statistic)}");
        Console.WriteLine($"  exact p               = {Inv.E3(exact.PValue)}");
        Console.WriteLine($"  asymptotic p          = {Inv.E3(asymptotic.PValue)}");
    }
}
```

`WilcoxonSample.cs`:

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>The Wilcoxon signed-rank test, and what it does with a zero difference.</summary>
internal static class WilcoxonSample
{
    private static readonly double[] Before = [12.0, 9.0, 15.0, 11.0, 8.0, 14.0, 10.0];
    private static readonly double[] After = [10.0, 9.0, 12.0, 11.0, 6.0, 11.0, 7.0];

    public static void Run()
    {
        Console.WriteLine("Wilcoxon signed-rank");

        // Two of the seven pairs are unchanged, which is exactly what the three
        // zero methods disagree about.
        foreach (ZeroMethod zeroMethod in (ZeroMethod[])[ZeroMethod.Wilcox, ZeroMethod.Pratt, ZeroMethod.ZSplit])
        {
            TestResult result = Wilcoxon.Paired(Before, After, zeroMethod);
            Console.WriteLine(
                $"  {zeroMethod,-7} W = {Inv.F3(result.Statistic)}  p = {Inv.F3(result.PValue)}");
        }

        TestResult differences = Wilcoxon.OneSample([2.0, 0.0, 3.0, 0.0, 2.0, 3.0, 3.0]);
        Console.WriteLine($"  from differences  W   = {Inv.F3(differences.Statistic)}");
    }
}
```

`ChiSquareSample.cs`:

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>Pearson's chi-square: goodness of fit, and a contingency table.</summary>
internal static class ChiSquareSample
{
    private static readonly double[] Rolls = [16.0, 18.0, 16.0, 14.0, 12.0, 12.0];
    private static readonly double[][] Preference =
    [
        [30.0, 20.0],
        [15.0, 35.0],
    ];

    public static void Run()
    {
        Console.WriteLine("chi-square");

        TestResult fit = ChiSquare.GoodnessOfFit(Rolls);
        Console.WriteLine($"  fair-die statistic    = {Inv.F3(fit.Statistic)}");
        Console.WriteLine($"  fair-die p            = {Inv.F3(fit.PValue)}");

        Chi2ContingencyResult table = ChiSquare.Contingency(Preference);
        Console.WriteLine($"  contingency statistic = {Inv.F3(table.Statistic)}");
        Console.WriteLine($"  degrees of freedom    = {table.Dof}");
    }
}
```

`Chi2ContingencyResultSample.cs`:

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>
/// <see cref="Chi2ContingencyResult"/> — the statistic, the p-value, the degrees
/// of freedom and the table independence would have produced.
/// </summary>
internal static class Chi2ContingencyResultSample
{
    private static readonly double[][] Observed =
    [
        [30.0, 20.0],
        [15.0, 35.0],
    ];

    public static void Run()
    {
        Console.WriteLine("Chi2ContingencyResult");

        Chi2ContingencyResult result = ChiSquare.Contingency(Observed, Continuity.None);

        Console.WriteLine($"  statistic             = {Inv.F3(result.Statistic)}");
        Console.WriteLine($"  p-value               = {Inv.E3(result.PValue)}");
        Console.WriteLine($"  dof                   = {result.Dof}");
        Console.WriteLine($"  expected row 0        = {Inv.List(result.ExpectedFrequencies[0])}");
    }
}
```

`FisherExactSample.cs`:

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>Fisher's exact test — right at any sample size, where chi-square needs large cells.</summary>
internal static class FisherExactSample
{
    // Fisher's own tea-tasting table: four cups poured each way, and the taster
    // placed three of each correctly.
    private static readonly int[][] TeaTasting = [[3, 1], [1, 3]];

    public static void Run()
    {
        Console.WriteLine("Fisher's exact test");

        TestResult twoSided = FisherExact.Test(TeaTasting);
        TestResult greater = FisherExact.Test(TeaTasting, Alternative.Greater);

        Console.WriteLine($"  odds ratio            = {Inv.F3(twoSided.Statistic)}");
        Console.WriteLine($"  two-sided p           = {Inv.F3(twoSided.PValue)}");
        Console.WriteLine($"  one-sided p           = {Inv.F3(greater.PValue)}");
    }
}
```

`KolmogorovSmirnovSample.cs`:

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>The two-sample Kolmogorov-Smirnov test — do these two samples share a distribution?</summary>
internal static class KolmogorovSmirnovSample
{
    private static readonly double[] Baseline = [0.1, 0.4, 0.6, 0.9, 1.3, 1.7, 2.2, 2.8];
    private static readonly double[] Candidate = [0.5, 1.1, 1.4, 2.0, 2.6, 3.1, 3.9, 4.4];

    public static void Run()
    {
        Console.WriteLine("Kolmogorov-Smirnov");

        KsResult result = KolmogorovSmirnov.TwoSample(Baseline, Candidate);

        Console.WriteLine($"  D                     = {Inv.F3(result.Statistic)}");
        Console.WriteLine($"  p-value               = {Inv.F3(result.PValue)}");
        Console.WriteLine($"  reached at            = {Inv.F3(result.StatisticLocation)}");
    }
}
```

`KsResultSample.cs`:

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>
/// <see cref="KsResult"/> — the distance, the p-value, and where and in which
/// direction the two empirical distributions were furthest apart.
/// </summary>
internal static class KsResultSample
{
    private static readonly double[] Left = [1.0, 2.0, 3.0, 4.0, 5.0];
    private static readonly double[] Right = [3.0, 4.0, 5.0, 6.0, 7.0];

    public static void Run()
    {
        Console.WriteLine("KsResult");

        KsResult result = KolmogorovSmirnov.TwoSample(Left, Right);

        Console.WriteLine($"  statistic             = {Inv.F3(result.Statistic)}");
        Console.WriteLine($"  p-value               = {Inv.F3(result.PValue)}");
        Console.WriteLine($"  statistic location    = {Inv.F3(result.StatisticLocation)}");
        Console.WriteLine($"  statistic sign        = {result.StatisticSign}");
    }
}
```

`OneWayAnovaSample.cs`:

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>One-way ANOVA — do three groups share one mean?</summary>
internal static class OneWayAnovaSample
{
    private static readonly double[] Morning = [12.0, 14.0, 11.0, 13.0, 15.0];
    private static readonly double[] Afternoon = [16.0, 15.0, 18.0, 17.0, 14.0];
    private static readonly double[] Evening = [21.0, 19.0, 22.0, 20.0, 23.0];

    public static void Run()
    {
        Console.WriteLine("one-way ANOVA");

        TestResult result = OneWayAnova.Test(Morning, Afternoon, Evening);

        Console.WriteLine($"  F                     = {Inv.F3(result.Statistic)}");
        Console.WriteLine($"  p-value               = {Inv.E3(result.PValue)}");
    }
}
```

`KruskalWallisSample.cs`:

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>Kruskal-Wallis — the rank-based ANOVA, which assumes no shape at all.</summary>
internal static class KruskalWallisSample
{
    private static readonly double[] Morning = [12.0, 14.0, 11.0, 13.0, 15.0];
    private static readonly double[] Afternoon = [16.0, 15.0, 18.0, 17.0, 14.0];
    private static readonly double[] Evening = [21.0, 19.0, 22.0, 20.0, 23.0];

    public static void Run()
    {
        Console.WriteLine("Kruskal-Wallis");

        TestResult result = KruskalWallis.Test(Morning, Afternoon, Evening);

        Console.WriteLine($"  H                     = {Inv.F3(result.Statistic)}");
        Console.WriteLine($"  p-value               = {Inv.E3(result.PValue)}");
    }
}
```

`ShapiroWilkSample.cs`:

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>Shapiro-Wilk — could this sample have come from a normal distribution?</summary>
internal static class ShapiroWilkSample
{
    private static readonly double[] Symmetric =
        [-1.62, -1.10, -0.74, -0.47, -0.23, 0.0, 0.23, 0.47, 0.74, 1.10, 1.62];

    private static readonly double[] Skewed =
        [0.1, 0.2, 0.3, 0.4, 0.6, 0.9, 1.4, 2.2, 3.6, 6.1, 12.0];

    public static void Run()
    {
        Console.WriteLine("Shapiro-Wilk");

        TestResult symmetric = ShapiroWilk.Test(Symmetric);
        TestResult skewed = ShapiroWilk.Test(Skewed);

        Console.WriteLine($"  symmetric W           = {Inv.F3(symmetric.Statistic)}");
        Console.WriteLine($"  symmetric p           = {Inv.F3(symmetric.PValue)}");
        Console.WriteLine($"  skewed W              = {Inv.F3(skewed.Statistic)}");
        Console.WriteLine($"  skewed p              = {Inv.E3(skewed.PValue)}");
    }
}
```

`MultipleComparisonsSample.cs`:

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>
/// Adjusting a family of p-values — twenty tests at five percent produce one
/// significant result by chance alone.
/// </summary>
internal static class MultipleComparisonsSample
{
    private static readonly double[] Family = [0.001, 0.008, 0.039, 0.041, 0.042];

    public static void Run()
    {
        Console.WriteLine("multiple comparisons");

        Console.WriteLine($"  raw                   = {Inv.List(Family)}");
        Console.WriteLine($"  Bonferroni            = {Inv.List(MultipleComparisons.Bonferroni(Family))}");
        Console.WriteLine($"  Benjamini-Hochberg    = {Inv.List(MultipleComparisons.BenjaminiHochberg(Family))}");
        Console.WriteLine($"  Benjamini-Yekutieli   = {Inv.List(MultipleComparisons.BenjaminiYekutieli(Family))}");
    }
}
```

`TestResultSample.cs`:

```csharp
using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>
/// <see cref="TestResult"/> — the shape eight of the ten families return, and
/// the only two numbers most of them have to give.
/// </summary>
internal static class TestResultSample
{
    private static readonly double[] Left = [7.0, 3.0, 6.0, 2.0, 8.0, 5.0];
    private static readonly double[] Right = [9.0, 12.0, 8.0, 11.0, 15.0, 10.0];

    public static void Run()
    {
        Console.WriteLine("TestResult");

        TestResult result = MannWhitney.Test(Left, Right);

        Console.WriteLine($"  statistic             = {Inv.F3(result.Statistic)}");
        Console.WriteLine($"  p-value               = {Inv.F3(result.PValue)}");
    }
}
```

- [ ] **Step 5: Add an `E3` helper to `Inv` if it is not there**

`samples/Lodestar.Sample/Inv.cs` already carries `F3` and `List`. If it has no
scientific-notation formatter, add one — the p-values here reach `1e-8`, which
`F3` would print as `0.000`:

```csharp
    /// <summary>Three significant digits in scientific notation, invariant culture.</summary>
    /// <remarks>
    /// p-values span thirty orders of magnitude, so a fixed-point format prints
    /// most of them as zero. tools/check_sample_culture.py is why the culture is
    /// stated rather than inherited.
    /// </remarks>
    public static string E3(double value) =>
        value.ToString("E3", System.Globalization.CultureInfo.InvariantCulture);
```

- [ ] **Step 6: Call the fourteen from `Program.cs`**

Add after the existing sample calls:

```csharp
TTestSample.Run();
TTestResultSample.Run();
MannWhitneySample.Run();
WilcoxonSample.Run();
ChiSquareSample.Run();
Chi2ContingencyResultSample.Run();
FisherExactSample.Run();
KolmogorovSmirnovSample.Run();
KsResultSample.Run();
OneWayAnovaSample.Run();
KruskalWallisSample.Run();
ShapiroWilkSample.Run();
MultipleComparisonsSample.Run();
TestResultSample.Run();
```

- [ ] **Step 7: Add `Lodestar.Stats` to `tools/check_sample_coverage.py`**

Line 34 becomes:

```python
CONVERTED = ["Lodestar.Text", "Lodestar.Conformal", "Lodestar.Abstractions",
             "Lodestar.Decomposition", "Lodestar.Stats"]
```

- [ ] **Step 8: Run the coverage gate**

Run: `python3 tools/check_sample_coverage.py`
Expected: exit 0. A finding names the missing `<Type>Sample.cs`; a finding
naming an *enum* means line 34 was edited but decision 0041's exclusion was not
respected — re-read the script's header rather than adding a fifteenth file.

- [ ] **Step 9: Pack, and run the sample against the packed packages**

```bash
for p in src/Lodestar.Abstractions src/Lodestar.Text src/Lodestar.Embeddings src/Lodestar.Fuzzy \
         src/Lodestar.Metrics src/Lodestar.Conformal src/Lodestar.Decomposition \
         src/Lodestar.Onnx src/Lodestar.Stats; do
  dotnet pack "$p" -c Release -o ./artifacts
done
NUGET_PACKAGES=$(mktemp -d) dotnet run -c Release --project samples/Lodestar.Sample
```

Expected: the run prints the fourteen new blocks. The isolated `NUGET_PACKAGES`
is not optional — without it the sample resolves the *published* packages and
judges nuget.org instead of the working tree (ADR 0009).

- [ ] **Step 10: Run the packaging gate and commit**

```bash
python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
python3 tools/check_sample_culture.py
python3 tools/check_sample_coverage.py
dotnet format Lodestar.slnx --verify-no-changes
git add samples/Lodestar.Sample tools/check_sample_coverage.py
git commit -m "Lodestar.Stats: the fourteen samples, and the coverage gate that counts them

Refs #442. Fourteen, not nineteen: decision 0041 excludes an enum from
check_sample_coverage, because an enum is demonstrated through the class whose
parameter it is and a file exercising one alone would have to invent a use.

p-values print through E3 rather than F3: they span thirty orders of magnitude,
and a fixed-point format shows most of them as zero."
```

---

### Task 11: The reference pages, and the map that enforces them

**Files:**

- Create: `docs/reference/stats.md` and `docs/reference/stats/tests/*.md` — the
  nineteen type pages and seventeen method pages listed below
- Modify: `docs/wiki-map.json`

**Interfaces:**

- Consumes: the public surface, exactly as Tasks 1 and 5-9 shipped it.
- Produces: nothing in code. Two gates start enforcing: the **reference gate**
  (`ReferenceDocumentationTests`, which walks the assembly and demands an entry
  per public type and method) and the **doc-snippets gate** (every ` ```csharp `
  fence compiles, and a trailing `// =>` on a reference fence is executed and
  asserted).

**The thirty-six pages.** Nineteen types plus seventeen methods:

| page | subject |
| --- | --- |
| `ttest.md` | `TTest` |
| `ttest-independent.md`, `ttest-paired.md`, `ttest-onesample.md` | its three methods |
| `mannwhitney.md`, `mannwhitney-test.md` | `MannWhitney` |
| `wilcoxon.md`, `wilcoxon-paired.md`, `wilcoxon-onesample.md` | `Wilcoxon` |
| `chisquare.md`, `chisquare-goodnessoffit.md`, `chisquare-contingency.md` | `ChiSquare` |
| `fisherexact.md`, `fisherexact-test.md` | `FisherExact` |
| `kolmogorovsmirnov.md`, `kolmogorovsmirnov-twosample.md` | `KolmogorovSmirnov` |
| `onewayanova.md`, `onewayanova-test.md` | `OneWayAnova` |
| `kruskalwallis.md`, `kruskalwallis-test.md` | `KruskalWallis` |
| `shapirowilk.md`, `shapirowilk-test.md` | `ShapiroWilk` |
| `multiplecomparisons.md`, `multiplecomparisons-bonferroni.md`, `multiplecomparisons-benjaminihochberg.md`, `multiplecomparisons-benjaminiyekutieli.md` | `MultipleComparisons` |
| `testresult.md`, `ttestresult.md`, `ttestresult-confidenceinterval.md`, `chi2contingencyresult.md`, `ksresult.md` | the four result shapes |
| `alternative.md`, `variance.md`, `continuity.md`, `exactmethod.md`, `zeromethod.md` | the five enums |

- [ ] **Step 1: Add the package to `docs/wiki-map.json`**

Beside `Lodestar.Conformal`'s entry:

```json
  "Lodestar.Stats": {
   "wiki": "Stats",
   "pages": [
    "docs/guides/hypothesis-testing.md",
    "docs/reference/stats/*.md",
    "docs/reference/stats/*/*.md"
   ],
   "covered": {
    "Lodestar.Stats": "docs/reference/stats/tests"
   }
  }
```

The `Lodestar.Stats.Internal` namespace is **not** listed as covered: it is
internal, so the gate never walks it, and listing it would demand pages for a
log-gamma nobody can call.

- [ ] **Step 2: Write `docs/reference/stats.md`, the index**

```markdown
# `Lodestar.Stats`

Ten families of classical hypothesis test, at `scipy.stats` 1.18.0 parity.
Arrays in, a statistic and a p-value out; nothing is fitted, so every entry
point is static.

| test | what it asks | entry point |
| --- | --- | --- |
| Student / Welch *t* | do two samples have the same mean? | [`TTest`](stats/tests/ttest.md) |
| Mann-Whitney *U* | the same question, assuming no shape | [`MannWhitney`](stats/tests/mannwhitney.md) |
| Wilcoxon signed-rank | the same, on paired measurements | [`Wilcoxon`](stats/tests/wilcoxon.md) |
| χ² | do counts match an expected distribution, or are two factors independent? | [`ChiSquare`](stats/tests/chisquare.md) |
| Fisher exact | the same for a 2×2 table, at any sample size | [`FisherExact`](stats/tests/fisherexact.md) |
| Kolmogorov-Smirnov | do two samples share a distribution? | [`KolmogorovSmirnov`](stats/tests/kolmogorovsmirnov.md) |
| one-way ANOVA | do several groups share one mean? | [`OneWayAnova`](stats/tests/onewayanova.md) |
| Kruskal-Wallis | the same, assuming no shape | [`KruskalWallis`](stats/tests/kruskalwallis.md) |
| Shapiro-Wilk | could this sample be normal? | [`ShapiroWilk`](stats/tests/shapirowilk.md) |
| Bonferroni / BH / BY | how many of these results are chance? | [`MultipleComparisons`](stats/tests/multiplecomparisons.md) |

The [hypothesis-testing guide](../guides/hypothesis-testing.md) says which test
answers which question, and the [Python equivalence table](../equivalence.md)
maps each `scipy` call to its counterpart here.
```

- [ ] **Step 3: Write the thirty-six pages on the established shape**

Every page follows `docs/reference/conformal/prediction/splitconformal-quantile.md`:
a one-line summary, a `<!-- docs-declaration -->` marker, the signature, then
**Parameters**, **Returns**, **Exceptions**, **Example**, **Remarks**,
**Applies to** and **See also**. The example fence is executed, and a trailing
`// =>` on a value is an assertion.

`docs/reference/stats/tests/ttest-independent.md`, as the worked model the other
thirty-five follow:

````markdown
# TTest.Independent

Compares the means of two independent samples.

<!-- docs-declaration -->

```csharp
public static TTestResult Independent(
    ReadOnlySpan<double> a,
    ReadOnlySpan<double> b,
    Alternative alternative = Alternative.TwoSided,
    Variance variance = Variance.Welch)
```

**Parameters** — `a` and `b` are the two samples, each at least two values; both
spans are read, never modified. `alternative` says which tail the p-value covers.
`variance` says whether to pool the two sample variances.

**Returns** — `TTestResult`: the t statistic, the p-value, and the degrees of
freedom, which are fractional under `Variance.Welch`.

**Exceptions** — `ArgumentException` when either sample holds fewer than two
values.

**Example** — two samples with clearly different means.

```csharp
using Lodestar.Stats;

double[] before = [102.0, 98.0, 110.0, 105.0, 99.0];
double[] after = [95.0, 92.0, 99.0, 91.0, 97.0];

TTestResult result = TTest.Independent(before, after);

bool significant = result.PValue < 0.01;   // => True
```

**Remarks — the default is not scipy's.** This defaults to `Variance.Welch`;
`scipy.stats.ttest_ind` defaults to `equal_var=True`, which is Student's test.
Pooling is only correct when the two populations really share a variance, and a
default that is wrong in the common case costs more than a word at the call
site. Pass `Variance.Equal` for scipy's default. Both are covered by
`tests/oracles/stats_ttest.json`, and the divergence has a row in the
[equivalence table](../../../equivalence.md).

**A NaN propagates.** There is no `nan_policy` here: a NaN anywhere in either
sample makes the statistic and the p-value NaN. `scipy`'s three-valued policy is
a convenience for its array API rather than part of the test, and a caller who
wants `'omit'` filters the array in one line.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TTest.Paired`](ttest-paired.md),
[`TTest.OneSample`](ttest-onesample.md),
[`MannWhitney.Test`](mannwhitney-test.md) for the rank-based counterpart, the
[Python equivalence table](../../../equivalence.md).
````

- [ ] **Step 4: Extract and compile the snippets**

```bash
python3 tools/extract_doc_snippets.py
dotnet build samples/Lodestar.DocSnippets -c Release
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. A failure here means a page
promises a signature the assembly does not have — fix the page, not the code,
unless the code is what is wrong.

- [ ] **Step 5: Run the reference gate**

Run: `dotnet test tests/Lodestar.Stats.Tests -c Release --filter "FullyQualifiedName~ReferenceDocumentation"`
Expected: PASS, **2 tests**. A complaint names the undocumented member.

Note the gate walks `Type.GetMethods()`, which does **not** return constructors,
and compares the **Exceptions** block as a *set*: listing an exception the method
cannot throw fails as loudly as omitting one it can.

- [ ] **Step 6: Run the whole suite and the lint, then commit**

```bash
dotnet test Lodestar.slnx -c Release
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
git add docs/reference/stats.md docs/reference/stats docs/wiki-map.json
git commit -m "Lodestar.Stats: the thirty-six reference pages and the wiki map

Refs #442. Nineteen type pages and seventeen method pages, each with an executed
example, under a covered entry that starts the reference gate on this namespace.

Lodestar.Stats.Internal is deliberately not covered: it is internal, so the gate
never walks it, and listing it would demand a reference page for a log-gamma
nobody can call."
```

---

### Task 12: The guide, the equivalence table, the ADR and the changelog

**Files:**

- Create: `docs/guides/hypothesis-testing.md`
- Create: `docs/decisions/00NN-the-stats-numerical-layer-stays-internal.md` —
  **take the next free number**, and add its row to `docs/decisions/README.md`
- Modify: `docs/equivalence.md`, `CHANGELOG.md`, root `README.md`

**Interfaces:** none in code.

**Numbering the ADR.** Before writing it, run `ls docs/decisions/ | tail -3` and
take the next number after the highest that exists. Two branches claiming one
number collide on whichever merges second — that happened on #313, whose ADR had
to be renumbered from 0079 to 0080 after #540 and #542 both carried 0079. Then
run `python3 tools/check_comment_length.py` **after** the renumber if one is
needed, because a rename script that targets a fixed file list misses
`tools/generate_oracles.py`; that is exactly how #543 went red.

- [ ] **Step 1: Write the guide**

`docs/guides/hypothesis-testing.md`. It is not an API listing — the reference
pages are that. It answers *which test*, and it carries the honest sentence
about what a p-value is not:

````markdown
# Hypothesis testing

`Lodestar.Stats` answers one question in ten forms: **is this difference more
than noise?**

## Which test

| you have | and you assume | use |
| --- | --- | --- |
| two independent samples | roughly normal | [`TTest.Independent`](../reference/stats/tests/ttest-independent.md) |
| two independent samples | nothing about the shape | [`MannWhitney.Test`](../reference/stats/tests/mannwhitney-test.md) |
| the same subjects measured twice | roughly normal differences | [`TTest.Paired`](../reference/stats/tests/ttest-paired.md) |
| the same subjects measured twice | nothing about the shape | [`Wilcoxon.Paired`](../reference/stats/tests/wilcoxon-paired.md) |
| counts in categories | a stated expected distribution | [`ChiSquare.GoodnessOfFit`](../reference/stats/tests/chisquare-goodnessoffit.md) |
| a contingency table | cells large enough for the approximation | [`ChiSquare.Contingency`](../reference/stats/tests/chisquare-contingency.md) |
| a 2×2 table with small cells | nothing | [`FisherExact.Test`](../reference/stats/tests/fisherexact-test.md) |
| two samples, whole distributions | nothing | [`KolmogorovSmirnov.TwoSample`](../reference/stats/tests/kolmogorovsmirnov-twosample.md) |
| three or more groups | roughly normal, similar spread | [`OneWayAnova.Test`](../reference/stats/tests/onewayanova-test.md) |
| three or more groups | nothing about the shape | [`KruskalWallis.Test`](../reference/stats/tests/kruskalwallis-test.md) |
| one sample, and a normality assumption to check | nothing | [`ShapiroWilk.Test`](../reference/stats/tests/shapirowilk-test.md) |
| many p-values at once | nothing | [`MultipleComparisons`](../reference/stats/tests/multiplecomparisons.md) |

## What a p-value is, and is not

A p-value is the probability of seeing a difference at least this large **if the
null hypothesis is true**. It is not the probability that the null hypothesis is
true, and it is not the probability that your result is a fluke. A p-value of
0.03 does not mean there is a 3 % chance you are wrong.

Two consequences worth acting on:

- **`0.049` and `0.051` are the same evidence.** The threshold is a convention,
  not a discovery. Report the number.
- **Twenty tests at 5 % produce one significant result by chance.** That is what
  [`MultipleComparisons`](../reference/stats/tests/multiplecomparisons.md) is
  for, and it is not optional once you are testing more than a couple of things.

```csharp
using Lodestar.Stats;

double[] pValues = [0.001, 0.008, 0.039, 0.041, 0.042];

double[] adjusted = MultipleComparisons.BenjaminiHochberg(pValues);

bool stillSignificant = adjusted[0] < 0.05;   // => True
```

## One default that is not scipy's

[`TTest.Independent`](../reference/stats/tests/ttest-independent.md) defaults to
Welch's test; `scipy.stats.ttest_ind` defaults to Student's. Pooling the two
variances is only correct when the populations really share one, which is an
assumption most callers have not checked. Pass `Variance.Equal` for scipy's
default. Everything else in this package matches `scipy.stats` 1.18.0 exactly,
and the [equivalence table](../equivalence.md) is the row-by-row map.

## Exact and asymptotic

Three tests carry both an exact null distribution and a normal approximation to
it, selected by `ExactMethod`:

- `Auto` — exact for a small, untied sample; asymptotic otherwise. What `scipy`'s
  `method='auto'` does, and the same thresholds.
- `Exact` — always the exact distribution. On tied data the number is only
  approximate, because ties break the equal-probability argument the enumeration
  rests on; `scipy` computes there too rather than refusing, and so does this.
- `Asymptotic` — always the normal approximation, whatever the sample size.

The branch changes the number, not just the running time, which is why it is a
parameter and not a hidden optimisation.

## No incumbent to compare against

The one .NET library that carried these tests, `Accord.Statistics`, was last
published in **October 2017** and its framework was **archived in November
2020**. `MathNet.Numerics` ships the distributions a test needs and no tests.
ML.NET does prediction. `bench/Lodestar.Stats.Benchmarks` measures against
`Accord` anyway — see [performance](performance.md) for the numbers and the
machine.
````

- [ ] **Step 2: Add the rows to `docs/equivalence.md`**

One row per scipy call, in the package's own section. The `ttest_ind` row must
name the divergence rather than claim equality:

| Python | Lodestar | notes |
| --- | --- | --- |
| `scipy.stats.ttest_ind(a, b, equal_var=True)` | `TTest.Independent(a, b, Alternative.TwoSided, Variance.Equal)` | **the default differs**: `Variance.Welch` here, `equal_var=True` there |
| `scipy.stats.ttest_rel(a, b)` | `TTest.Paired(a, b)` | |
| `scipy.stats.ttest_1samp(a, m)` | `TTest.OneSample(a, m)` | |
| `scipy.stats.mannwhitneyu(x, y)` | `MannWhitney.Test(x, y)` | |
| `scipy.stats.wilcoxon(x, y)` | `Wilcoxon.Paired(x, y)` | |
| `scipy.stats.chisquare(f_obs, f_exp)` | `ChiSquare.GoodnessOfFit(observed, expected)` | |
| `scipy.stats.chi2_contingency(table)` | `ChiSquare.Contingency(table)` | |
| `scipy.stats.fisher_exact(table)` | `FisherExact.Test(table)` | |
| `scipy.stats.ks_2samp(a, b)` | `KolmogorovSmirnov.TwoSample(a, b)` | `ks_1samp` has no counterpart: it needs a CDF this package cannot supply |
| `scipy.stats.f_oneway(*groups)` | `OneWayAnova.Test(groups)` | |
| `scipy.stats.kruskal(*groups)` | `KruskalWallis.Test(groups)` | |
| `scipy.stats.shapiro(x)` | `ShapiroWilk.Test(x)` | refuses outside `3 <= n <= 5000`, where scipy warns and answers |
| `scipy.stats.false_discovery_control(p, method='bh')` | `MultipleComparisons.BenjaminiHochberg(p)` | |
| `scipy.stats.false_discovery_control(p, method='by')` | `MultipleComparisons.BenjaminiYekutieli(p)` | |
| — | `MultipleComparisons.Bonferroni(p)` | scipy has none; the corpus states the definition, `min(p × n, 1)` |
| `nan_policy=` | — | no counterpart: a NaN propagates, and a caller wanting `'omit'` filters the array |

- [ ] **Step 3: Write the ADR**

`docs/decisions/00NN-the-stats-numerical-layer-stays-internal.md`, on the
established shape — `# 00NN — …`, `**Status:** accepted`, then `## Context`,
`## Decision`, `## Consequences`, and the options refused with their reasons:

- **Refused: a public `Lodestar.Stats.Special` namespace.** Every function would
  become a parity promise with its own reference page and sample file, for a need
  #442 does not state. Publishing later stays possible; unpublishing does not.
- **Refused: putting it in `Lodestar.Abstractions`.** That buys a published
  floor between two packages for code one of them uses, and `Abstractions`'
  subject is the sparse matrix.
- **Recorded: `erfc` is `Q(1/2, x²)`, not a rational fit.** One approximation to
  keep accurate rather than two that must agree, and the far-tail accuracy comes
  free from the chi-square requirement.
- **Recorded: the p-value tolerance is relative.** With the measured numbers —
  `7.85e-26`, `2.38e-53` — and why `tools/compare_oracles.py` was not changed.

Then add its row to `docs/decisions/README.md`, and check the table still reads
in order:

```bash
grep -n '^| \[`00' docs/decisions/README.md | tail -6
```

- [ ] **Step 4: Add the `CHANGELOG.md` entry**

Under the unreleased section, in a **new** `### Lodestar.Stats` block placed in
the same package order the file already uses. **Check first that the package
does not already have a section** — a duplicate `###` heading giving one package
two `#### Added` blocks is a defect that reached `main` once already:

```bash
grep -n '^### Lodestar' CHANGELOG.md
```

The entry, one sentence plus the facts:

> - **`Lodestar.Stats` is a new package: ten families of classical hypothesis
>   test at `scipy.stats` 1.18.0 parity.** Student and Welch *t*, Mann-Whitney
>   *U*, Wilcoxon signed-rank, χ² goodness-of-fit and contingency, Fisher exact,
>   two-sample Kolmogorov-Smirnov, one-way ANOVA, Kruskal-Wallis, Shapiro-Wilk,
>   and the Bonferroni, Benjamini-Hochberg and Benjamini-Yekutieli corrections.
>   Core tier: the tail probabilities come from the package's own internal
>   log-gamma, incomplete beta and incomplete gamma rather than from a numerical
>   dependency. `TTest.Independent` defaults to Welch where `scipy` defaults to
>   Student, which is the one deliberate divergence and has its row in
>   [`docs/equivalence.md`](docs/equivalence.md). Ten frozen corpora, each case
>   carrying the full argument set it was generated with, and p-values compared
>   at `1e-9` **relative** — ordinary cases reach `2.38e-53`, where the
>   repository's absolute tolerance would accept a zero.
>   ([#442](https://github.com/CyrilB1531/lodestar/issues/442), decision 00NN)

- [ ] **Step 5: Add the package to the root `README.md` table**

The package table there lists each package and one line about it. Add
`Lodestar.Stats` in the same order the architecture table in `CLAUDE.md` uses.

- [ ] **Step 6: Lint and commit**

```bash
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 tools/extract_doc_snippets.py && dotnet build samples/Lodestar.DocSnippets -c Release
python3 tools/check_adr_immutable.py --base origin/main
python3 tools/check_comment_length.py
git add docs CHANGELOG.md README.md
git commit -m "Lodestar.Stats: the guide, the equivalence rows, the ADR and the changelog

Refs #442. The guide answers which test rather than listing the API, and says
what a p-value is not: 0.049 and 0.051 are the same evidence, and twenty tests
at five percent produce one significant result by chance.

The equivalence table names the two divergences rather than claiming equality --
Welch as the default where scipy takes Student, and a refusal outside Royston's
fitted range where scipy warns and answers."
```

---

### Task 13: The benchmark against `Accord.Statistics`

**Files:**

- Create: `bench/Lodestar.Stats.Benchmarks/Lodestar.Stats.Benchmarks.csproj`,
  `StatsBenchmarks.cs`, `Program.cs`
- Modify: `bench/bench-map.json`, `bench/README.md`, `Lodestar.slnx`,
  `docs/guides/performance.md`

**Interfaces:**

- Consumes: `TTest.Independent`, `MannWhitney.Test`, `ChiSquare.Contingency`.
- Produces: numbers for `docs/guides/performance.md`, and a second
  implementation to cross-check against.

**Why there is a benchmark at all.** `Lodestar.Conformal`'s spec recorded "no
.NET incumbent" as its benchmark section, and that was true. It is not true
here: `Accord.Statistics` 3.8.0 is archived but still installable, and `bench/`
already references six incumbents this way — `Fastenshtein`, `Quickenshtein`,
`F23.StringSimilarity`, `Raffinert.FuzzySharp`, `Microsoft.ML.Tokenizers`,
`Microsoft.ML`. The timing is the smaller half of the value; the larger half is
that where `Accord` and `scipy` disagree, the corpus says which one this package
follows, and the guide records it.

- [ ] **Step 1: Write the project**

`bench/Lodestar.Stats.Benchmarks/Lodestar.Stats.Benchmarks.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.14.0" />
    <!-- The incumbent: archived November 2020, last published October 2017, and
         still the only .NET library that carried these tests. -->
    <PackageReference Include="Accord.Statistics" Version="3.8.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Lodestar.Stats/Lodestar.Stats.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Write `bench/Lodestar.Stats.Benchmarks/StatsBenchmarks.cs`**

```csharp
using Accord.Statistics.Testing;
using BenchmarkDotNet.Attributes;
using Lodestar.Stats;

namespace Lodestar.Stats.Benchmarks;

/// <summary>
/// Three families against Accord.Statistics.Testing, the one .NET library that
/// carried them.
/// </summary>
/// <remarks>
/// The timing is the smaller half. The larger half is that a second
/// implementation is a second opinion: where Accord and scipy disagree, the
/// corpus says which one this package follows.
/// </remarks>
[MemoryDiagnoser]
public class StatsBenchmarks
{
    private double[] _a = [];
    private double[] _b = [];
    private double[][] _table = [];

    [Params(100, 10_000)]
    public int SampleSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(442);
        _a = [.. Enumerable.Range(0, SampleSize).Select(_ => random.NextDouble())];
        _b = [.. Enumerable.Range(0, SampleSize).Select(_ => random.NextDouble() + 0.1)];
        _table = [[30.0, 20.0], [15.0, 35.0]];
    }

    [Benchmark(Baseline = true)]
    public double LodestarWelchT() => TTest.Independent(_a, _b).PValue;

    [Benchmark]
    public double AccordWelchT() => new TwoSampleTTest(_a, _b, assumeEqualVariances: false).PValue;

    [Benchmark]
    public double LodestarMannWhitney() => MannWhitney.Test(_a, _b).PValue;

    [Benchmark]
    public double AccordMannWhitney() => new MannWhitneyWilcoxonTest(_a, _b).PValue;

    [Benchmark]
    public double LodestarChiSquare() => ChiSquare.Contingency(_table).PValue;

    [Benchmark]
    public double AccordChiSquare() =>
        new Accord.Statistics.Testing.ChiSquareTest(
            new Accord.Statistics.Analysis.ConfusionMatrix(30, 20, 15, 35)).PValue;
}
```

**Note for the implementer.** `Accord`'s API names are from a 2017 release and
may not be exactly as written above. Resolve them against the restored package
rather than guessing: `dotnet build` will name what does not exist, and the
`Accord.Statistics.Testing` namespace is small enough to read. If a family has no
`Accord` counterpart at all, drop that pair and record the absence in
`bench/README.md` — do not invent a comparison.

- [ ] **Step 3: Write `Program.cs`**

```csharp
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Lodestar.Stats.Benchmarks.StatsBenchmarks).Assembly)
    .Run(args);
```

- [ ] **Step 4: Register in `bench/bench-map.json` and `Lodestar.slnx`**

Add the project to the `/bench/` folder of `Lodestar.slnx`, and its entry to
`bench/bench-map.json` on the shape the existing entries use. Then:

Run: `python3 tools/check_bench_map.py`
Expected: exit 0.

- [ ] **Step 5: Run the benchmark and record the numbers**

```bash
dotnet run -c Release --project bench/Lodestar.Stats.Benchmarks -- --filter '*'
```

Record the result in `docs/guides/performance.md` **with the machine named**, as
that file's own convention requires, and the method in `bench/README.md`. A
number without its machine is not a measurement.

- [ ] **Step 6: Cross-check the two implementations disagree nowhere unexplained**

For each of the three families, run one corpus case through both and compare. If
they disagree, `scipy` is the reference this package follows and `Accord` is the
observation — say so in `bench/README.md` with the case that separates them,
and do not change the corpus.

- [ ] **Step 7: Commit**

```bash
dotnet build Lodestar.slnx -c Release
dotnet format Lodestar.slnx --verify-no-changes
python3 tools/check_bench_map.py
python3 tools/check_machine_paths.py --no-environment
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
git add bench Lodestar.slnx docs/guides/performance.md
git commit -m "Lodestar.Stats: the benchmark against Accord.Statistics

Refs #442. Unlike Lodestar.Conformal, this domain has a named .NET incumbent, so
#442's constraint is met literally rather than by recording an absence: Accord
is archived but installable, and bench/ already references six incumbents this
way.

The timing is the smaller half of the value. The larger half is a second
implementation to disagree with: where Accord and scipy part, the corpus says
which one this package follows."
```

---

## Before the pull request

Run the whole gate set once, in this order, from the repository root. Every one
of these has failed a branch in this repository before.

```bash
dotnet build Lodestar.slnx -c Release                       # 0 warnings, 0 errors
dotnet test Lodestar.slnx -c Release                        # read the counts, not the colour
dotnet format Lodestar.slnx --verify-no-changes
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"

python3 tools/check_version_floor.py
python3 tools/check_machine_paths.py --no-environment
python3 tools/check_sample_culture.py
python3 tools/check_comment_length.py
python3 tools/check_netstandard_guards.py
python3 tools/check_sample_coverage.py
python3 tools/check_bench_map.py
python3 tools/check_adr_immutable.py --base origin/main
python3 tools/check_repeated_literals.py --base origin/main

python3 tools/extract_doc_snippets.py
dotnet build samples/Lodestar.DocSnippets -c Release

for p in src/Lodestar.Abstractions src/Lodestar.Text src/Lodestar.Embeddings src/Lodestar.Fuzzy \
         src/Lodestar.Metrics src/Lodestar.Conformal src/Lodestar.Decomposition \
         src/Lodestar.Onnx src/Lodestar.Stats; do
  dotnet pack "$p" -c Release -o ./artifacts
done
python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
NUGET_PACKAGES=$(mktemp -d) dotnet run -c Release --project samples/Lodestar.Sample
```

Then the oracle fixed point, from a directory that is **not** an ancestor of the
checkout, reading the generator's **own** exit code:

```bash
REPO=$(git rev-parse --show-toplevel)
SCRATCH=$(mktemp -d)
cd "$SCRATCH" && PYTHONSAFEPATH=1 "$REPO/.venv-oracles/bin/python" \
  "$REPO/tools/generate_oracles.py" > "$SCRATCH/gen.log" 2>&1
echo "generator exit: $?"
git -C "$REPO" status --short tests/oracles
```

Expected: `generator exit: 0` and an **empty** `git status` — the committed
corpora are a fixed point of the generator. Anything listed means a corpus moved
and must be explained before it is committed.

Finally, clear Sonar: `AnalysisMode=All` findings block the merge, and a green
local build is not a clean Sonar. Per
`.github/instructions/sonarqube_mcp.instructions.md`, disable automatic analysis
at the start of the task, call `analyze_file_list` on what you changed at the
end, then re-enable it.

**Do not merge, tag, or open a pull request unless asked.**

## Self-Review

**1. Spec coverage.** Every section of
`docs/superpowers/specs/2026-09-05_0442_lodestar-stats-hypothesis-tests.md` maps
to a task:

| spec section | task |
| --- | --- |
| Problem — the incumbent survey | Task 12 (the guide records it) |
| Scope — the ten families | Tasks 5-9 |
| Scope — `ks_1samp`, `nan_policy` out | Task 8 (the class remark), Task 5 (the edge test) |
| Public surface — 19 types | Task 1 (results and enums), Tasks 5-9 (the ten families) |
| Public surface — 14 samples | Task 10 |
| The numerical layer | Tasks 2, 3, and the two quantiles added in Tasks 5 and 9 |
| Oracle discipline — full argument sets | Task 4 |
| Oracle discipline — relative p-values | Task 4 (`StatsOracleAsserts`) |
| Corpora — one per family, identity fact | Task 4 |
| Placement and wiring | Tasks 1, 10, 11 |
| Benchmarks | Task 13 |
| Testing | every task's own steps, plus **Before the pull request** |

**2. Placeholder scan.** Three places say "resolve this against the code rather
than the plan", and each names *how*: the Mann-Whitney and Wilcoxon tail
conventions (Task 6 step 7), the KS exact recursion's normalisation (Task 8
step 3), and `Accord`'s 2017 API names (Task 13 step 2). Each gives the exact
Python or `dotnet build` command that settles it, and each says which side to
change — the code, never the corpus. The ADR number in Task 12 is deliberately
`00NN` with the command that resolves it, because a number chosen when the plan
was written would collide the way #313's did.

**3. Type consistency.** `TestResult`, `TTestResult`, `Chi2ContingencyResult`
and `KsResult` are declared in Task 1 and used unchanged in Tasks 5-11.
`TTestResult.ConfidenceInterval` is declared in Task 1, filled in Task 5, and
documented in Task 11. `Alternative`, `Variance`, `Continuity`, `ExactMethod`
and `ZeroMethod` are declared in Task 1 and consumed by name thereafter.
`Gamma`, `Beta`, `Normal` and `Kolmogorov` are produced in Task 2 and consumed
in Tasks 5-9; `Ranks` and `RankDistributions` in Task 3, consumed in Tasks 6
and 8. `StatsCorpus` and `StatsOracleAsserts` are produced in Task 4, with
`StatsCorpus.Alternative` and `StatsCorpus.Method` added in Task 6 step 2 and
used by Tasks 7 and 8. `Beta.StudentQuantile` is added in Task 5 and
`Normal.Quantile` in Task 9, each in the task whose deliverable needs it.
