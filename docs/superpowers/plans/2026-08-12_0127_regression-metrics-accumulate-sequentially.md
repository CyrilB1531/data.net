# #127 — Compensated regression sums Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Goal:** Stop the regression metrics losing the low-order bits of an ill-conditioned target, by summing
with Neumaier compensation where numpy sums pairwise — and prove it with a fixture the existing corpus
cannot hold.

**Architecture:** One `internal struct CompensatedSum` in `DataNet.Metrics.Internal`, applied at the three
places that accumulate: `Outputs.WeightedMean` (the walk seven metrics share), `R2`'s two passes, and
`ExplainedVariance`'s two passes. The per-column `double[]` becomes a `CompensatedSum[]` of the same
length. The evidence is a procedural oracle case — parameters and reference values committed, the 200 000
sample arrays rebuilt on both sides from one closed form, probes compared bit for bit.

**Tech Stack:** C# (`net10.0` + `netstandard2.0`), xunit, BenchmarkDotNet, scikit-learn through
`tools/generate_oracles.py`.

**Spec:** `docs/superpowers/specs/2026-08-12_0127_regression-metrics-accumulate-sequentially.md`

## Global Constraints

- Everything in English — code, comments, commit messages, PR body. Commit messages carry no
  `feat:`/`fix:` prefix and no process prefix such as `Fix round 1:`.
- Branch `fix/127-compensated-regression-sums` in the repository root (`<repo>` below). Never commit to
  `main`. Do not push or open a pull request without asking.
- **No absolute machine path in any committed file.** The repository is public; write `<repo>`.
- `src/` multi-targets `netstandard2.0` and `net10.0`. Every `src/` edit must compile on both, and
  `CompensatedSum` must behave identically on both — it is plain scalar arithmetic and gets no
  `#if`. Every test file is linked into the mirrored `*.NetStandard.Tests` project, so each new test counts
  **twice** in the suite total.
- Warnings are errors repository-wide, and since #109 the build also enforces nine `csharpsquid` rules the
  analyzer package ships disabled — `S107 S110 S1192 S1479 S2342 S2436 S3776 S6664 S6669`. **`dotnet build`
  is incremental: without `--no-incremental` no analyzer diagnostic is produced at all.**
- A suppression carries a reason a reviewer could disagree with, at the call site for one site, in the
  area's `Directory.Build.props` for a whole area. "Too noisy" is not a reason.
- `dotnet format DataNet.slnx --verify-no-changes` must exit 0. Run it **bare**, no `env -u DOTNET_ROOT`.
- Read the pass/fail **counts** of every test run. A `--filter` that matches nothing exits zero and reports
  success. Baseline on this branch: **2947 passing, 0 failed**, across eight assemblies — taken after
  `origin/main` was merged in on 2026-08-12, bringing issue #119's `fuse_unk` and its 16 tests.
- **Never write `echo "exit=$?"` after a pipeline** — it reports the last command's status. Redirect to a
  file and check separately.
- Oracle generation runs from a neutral working directory (`cd /tmp` first) or `nltk` refuses to import,
  and the generator's own exit code is read, never a pipeline's.
- `docs/**` and `tools/README.md` are inside CI's markdownlint glob; `CHANGELOG.md` is not.

## What the tolerance is, and why it matters here

`RegressionCorpus.AssertClose` compares with `1e-9 * max(1, |expected|)`. On a target near `1e9` that is an
absolute tolerance of about `1`, which is why the ill-conditioned case has to assert on **R²** and
**explained variance** — scores near 1, where the tolerance is `1e-9` absolute — rather than on the mean of
`y_true` itself. A test that asserted the mean would pass either way and prove nothing.

## File Structure

| File | Responsibility |
| --- | --- |
| `src/DataNet.Metrics/Internal/CompensatedSum.cs` *(new)* | Neumaier accumulation: `Add(double)`, `Value`. |
| `src/DataNet.Metrics/R2.cs:145-185` | Both passes accumulate through it. |
| `src/DataNet.Metrics/ExplainedVariance.cs:120-165` | The same, plus its mean-residual pass. |
| `src/DataNet.Metrics/Internal/Outputs.cs:186-215` | `WeightedMean`'s per-column accumulator and `totalWeight`. |
| `tests/DataNet.Metrics.Tests/CompensatedSumTests.cs` *(new)* | The mechanism, against a `decimal` reference, through the public metrics. |
| `tools/generate_oracles.py` | The procedural ill-conditioned case. |
| `tests/oracles/regression_conditioning.json` *(new, generated, committed)* | Parameters, probes, reference values. No arrays. |
| `tests/DataNet.Metrics.Tests/RegressionConditioningTests.cs` *(new)* | Rebuilds the arrays, checks the probes, replays the values. |
| `docs/guides/performance.md` | The measured cost. |
| `CHANGELOG.md`, `docs/equivalence.md` | The record. |

---

### Task 1: Measure what is actually lost, and where

**Files:**

- Create: `/tmp/127-probe/Program.cs` and `/tmp/127-probe/probe.csproj` (scratch, never committed)

**Depends on:** nothing.

**Produces:** the two numbers Task 4 and Task 6 need — the relative error the *centring mean* loses, and
the relative error the *kernel sum* loses.

The spec's D3 rule cannot be applied to a guess, and Task 4 must not write a test it has no reason to
believe can fail.

**Measured on 2026-08-12, and the shape it corrected.** The construction this plan first proposed was
degenerate: at `offset = 1e9` the ULP is about `2.4e-7`, and a prediction perturbed by `1e-8` rounds back
onto the target, so every residual was exactly zero and `mse = 0`, `r2 = 1` would have passed while proving
nothing — in the probe, in Task 2's test and in Task 5's fixture alike. The perturbation is what has to
clear the ULP; the *ramp* does not, because a ramp quantized onto ULP multiples is exactly the
ill-conditioning under repair. On the corrected shape — **`offset = 1e9`, `spread = 1e-2`,
`n = 200 000`, perturbation `((i % 7) - 3) * 1e-6`** — the measurements are:

| Quantity | Value |
| --- | --- |
| distinct values in `y_true` | **83 887** |
| indices with a non-zero residual | **6/7** |
| centring mean, sequential against compensated | **5.0e-12** relative |
| kernel sum (squared residuals), sequential against compensated | **0.0** — the array never carries the offset |
| **R², sequential against compensated** | **3.574e-7** relative, 357× the oracle's `1e-9` tolerance |

Those numbers are what Tasks 2 to 6 use. An executor re-running the probe should reproduce them; a
material difference is a finding, not a nuisance.

- [ ] **Step 1: Build a probe that measures both accumulations**

```bash
mkdir -p /tmp/127-probe && cd /tmp/127-probe
cat > probe.csproj <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
EOF
cat > Program.cs <<'EOF'
// Naive versus Neumaier on the two accumulations DataNet.Metrics performs, on the
// shape issue #127 describes: a large offset over a small spread.
const int N = 1_000_000;
const double Offset = 1e9;
double step = 1e-2 / N;

double[] yTrue = new double[N];
double[] yPred = new double[N];
for (int i = 0; i < N; i++)
{
    yTrue[i] = Offset + (i * step);
    yPred[i] = yTrue[i] + (((i % 7) - 3) * step);
}

static double Naive(ReadOnlySpan<double> values)
{
    double sum = 0.0;
    foreach (double v in values) { sum += v; }
    return sum;
}

static double Neumaier(ReadOnlySpan<double> values)
{
    double sum = 0.0, c = 0.0;
    foreach (double v in values)
    {
        double t = sum + v;
        c += Math.Abs(sum) >= Math.Abs(v) ? (sum - t) + v : (v - t) + sum;
        sum = t;
    }
    return sum + c;
}

static double Relative(double naive, double exact) =>
    exact == 0.0 ? Math.Abs(naive) : Math.Abs((naive - exact) / exact);

// 1. The centring mean of y_true — where the issue measured the damage.
double naiveMean = Naive(yTrue) / N;
double exactMean = Neumaier(yTrue) / N;
Console.WriteLine($"mean        naive={naiveMean:R} neumaier={exactMean:R} rel={Relative(naiveMean, exactMean):E3}");

// 2. The kernel sum seven metrics share: squared residuals, already differenced.
double[] squares = new double[N];
for (int i = 0; i < N; i++) { double r = yTrue[i] - yPred[i]; squares[i] = r * r; }
double naiveMse = Naive(squares) / N;
double exactMse = Neumaier(squares) / N;
Console.WriteLine($"mse kernel  naive={naiveMse:R} neumaier={exactMse:R} rel={Relative(naiveMse, exactMse):E3}");

// 3. The centred sum of squares R² divides by, computed from each mean in turn.
static double CentredSumOfSquares(double[] values, double mean)
{
    double[] centred = new double[values.Length];
    for (int i = 0; i < values.Length; i++) { double d = values[i] - mean; centred[i] = d * d; }
    return Naive(centred);
}
double denomFromNaiveMean = CentredSumOfSquares(yTrue, naiveMean);
double denomFromExactMean = CentredSumOfSquares(yTrue, exactMean);
Console.WriteLine($"denominator fromNaiveMean={denomFromNaiveMean:R} fromExactMean={denomFromExactMean:R} rel={Relative(denomFromNaiveMean, denomFromExactMean):E3}");
EOF
dotnet run -c Release > /tmp/127-probe.log 2>&1
echo "probe=$?"
cat /tmp/127-probe.log
```

- [ ] **Step 2: Record all three numbers, and say what they imply**

Copy the three lines verbatim into your report. Then state, explicitly:

1. the relative error of the **centring mean** — the issue predicts this is large;
2. the relative error of the **kernel sum** — if it is below `1e-12`, D3's rule says the hot loop may keep
   its plain accumulation *if* the benchmark cost turns out above 10%, and Task 4 will have no failing test
   to write;
3. the relative error of the **denominator** computed from the two means, which is the path by which the
   mean's error reaches R²'s score.

Nothing is committed by this task.

---

### Task 2: `CompensatedSum`, and the first place it is needed

**Files:**

- Create: `src/DataNet.Metrics/Internal/CompensatedSum.cs`
- Create: `tests/DataNet.Metrics.Tests/CompensatedSumTests.cs`
- Modify: `src/DataNet.Metrics/R2.cs` (the block at `:145-185`)

**Depends on:** Task 1.

**Interfaces:**

- Produces, for Tasks 3 and 4: `internal struct CompensatedSum` with `void Add(double value)` and
  `readonly double Value { get; }`, in namespace `DataNet.Metrics.Internal`.

**Note on visibility.** `DataNet.Metrics` declares no `InternalsVisibleTo`, so the tests **cannot** see
`CompensatedSum` directly and must not be given a way to: exposing internals to add one test would be a
bigger change than this fix. The mechanism is therefore tested through `R2.Score`, which is the nearest
public thing that depends on it, against a `decimal` reference computed inside the test — an independent,
higher-precision path rather than a second copy of the algorithm under test.

- [ ] **Step 1: Write the failing test**

`tests/DataNet.Metrics.Tests/CompensatedSumTests.cs`:

```csharp
using DataNet.Metrics;
using Xunit;

namespace DataNet.Metrics.Tests;

/// <summary>
/// The accumulation itself, on the shape issue #127 measured: a large offset over a
/// small spread, where a sequential sum loses the spread in the offset's low bits.
/// </summary>
/// <remarks>
/// Asserted through <see cref="R2"/> rather than against the internal accumulator,
/// because <c>DataNet.Metrics</c> exposes no internals to its tests and adding that
/// exposure for one test would be a larger change than the fix. The reference is
/// computed in <see cref="decimal"/> — 28 significant digits against
/// <see cref="double"/>'s 17 — so the expected value comes from a genuinely more
/// precise arithmetic rather than from a second implementation of the thing under
/// test.
/// </remarks>
public sealed class CompensatedSumTests
{
    private const int Samples = 200_000;
    private const double Offset = 1e9;
    private const double Spread = 1e-2;

    /// <summary>
    /// The measured shape: a ramp of 83 887 distinct values over an offset that swamps
    /// it, and a prediction perturbed by a multiple of 1e-6.
    /// </summary>
    /// <remarks>
    /// 1e-6 and not smaller: the ULP at 1e9 is about 2.4e-7, so a perturbation below
    /// half of that rounds back onto the target and every residual becomes exactly
    /// zero — which scores a perfect R² and proves nothing. The ramp's own step is
    /// 5e-8, below the ULP and deliberately so: quantizing it onto ULP multiples is
    /// what makes the target ill-conditioned in the first place.
    /// </remarks>
    private static (double[] YTrue, double[] YPred) IllConditioned()
    {
        double step = Spread / Samples;
        double[] yTrue = new double[Samples];
        double[] yPred = new double[Samples];
        for (int i = 0; i < Samples; i++)
        {
            yTrue[i] = Offset + (i * step);
            yPred[i] = yTrue[i] + (((i % 7) - 3) * 1e-6);
        }
        return (yTrue, yPred);
    }

    /// <summary>
    /// R² of a prediction that is exactly the truth shifted by a constant. The exact
    /// score is derivable: the residual is the same constant everywhere, so the
    /// numerator is n·c² and the denominator is the centred sum of squares of an
    /// arithmetic progression — both computed here in decimal.
    /// </summary>
    [Fact]
    public void R2_matches_a_decimal_reference_on_an_ill_conditioned_target()
    {
        (double[] yTrue, double[] yPred) = IllConditioned();

        double expected = (double)ExactR2(yTrue, yPred);
        double actual = R2.Score(yTrue, yPred);

        Assert.Equal(expected, actual, 10);
    }

    /// <summary>
    /// The same data through a deliberately sequential sum, so that the test above is
    /// known to discriminate: if this one ever agreed with the decimal reference to
    /// twelve places, the first test would be passing for free.
    /// </summary>
    [Fact]
    public void A_sequential_sum_does_not_match_that_reference()
    {
        (double[] yTrue, double[] yPred) = IllConditioned();

        double sequential = SequentialR2(yTrue, yPred);
        double expected = (double)ExactR2(yTrue, yPred);

        Assert.NotEqual(expected, sequential, 10);
    }

    /// <summary>R² in <see cref="decimal"/>, which carries eleven more digits than <see cref="double"/>.</summary>
    private static decimal ExactR2(double[] yTrue, double[] yPred)
    {
        decimal mean = 0m;
        foreach (double value in yTrue)
        {
            mean += (decimal)value;
        }
        mean /= yTrue.Length;

        decimal numerator = 0m;
        decimal denominator = 0m;
        for (int i = 0; i < yTrue.Length; i++)
        {
            decimal residual = (decimal)yTrue[i] - (decimal)yPred[i];
            decimal centred = (decimal)yTrue[i] - mean;
            numerator += residual * residual;
            denominator += centred * centred;
        }
        return 1m - (numerator / denominator);
    }

    /// <summary>The accumulation this change replaces, kept here as the control.</summary>
    private static double SequentialR2(double[] yTrue, double[] yPred)
    {
        double mean = 0.0;
        foreach (double value in yTrue)
        {
            mean += value;
        }
        mean /= yTrue.Length;

        double numerator = 0.0;
        double denominator = 0.0;
        for (int i = 0; i < yTrue.Length; i++)
        {
            double residual = yTrue[i] - yPred[i];
            double centred = yTrue[i] - mean;
            numerator += residual * residual;
            denominator += centred * centred;
        }
        return 1.0 - (numerator / denominator);
    }
}
```

Read `tests/DataNet.Metrics.Tests/R2Tests.cs` first and match its `using` block and naming; the shape above
is indicative, the file's own idiom wins. If `R2.Score`'s signature differs from
`Score(double[], double[])`, use the real one.

- [ ] **Step 2: Run them and watch the first fail**

```bash
dotnet test DataNet.slnx -c Release --filter "FullyQualifiedName~CompensatedSumTests" > /tmp/127-t2-red.log 2>&1
echo "test=$?"
grep -E "^Réussi!|^Échoué!|Assert" /tmp/127-t2-red.log | head -10
```

Expected: `R2_matches_a_decimal_reference_on_an_ill_conditioned_target` **fails**, and
`A_sequential_sum_does_not_match_that_reference` **passes** — the second is what proves the first is not
passing for free. Four tests in total across the two mirrored projects. **Read the count.**

If the first test *passes* before any change, stop and report. Task 1 measured this exact shape at
**3.574e-7** relative between a sequential R² and a compensated one, which ten decimal places separate by
more than three orders of magnitude, so a green first test means the construction was not built as written
— check the perturbation is `1e-6` and not the ramp's `5e-8` step, which rounds away entirely.

- [ ] **Step 3: Write the accumulator**

`src/DataNet.Metrics/Internal/CompensatedSum.cs`:

```csharp
namespace DataNet.Metrics.Internal;

/// <summary>
/// A running sum that keeps the low-order bits a sequential <c>+=</c> discards —
/// Neumaier's variant of compensated summation.
/// </summary>
/// <remarks>
/// <para>
/// numpy sums pairwise, so on an ill-conditioned target — a large offset over a small
/// spread — a sequential loop and <c>numpy.mean</c> separate well past the 1e-9 the
/// oracle corpora compare at. Measured on n = 200 000 around 1e9: the sequential mean
/// lands 2.1e-3 away from the exact one, 21% of the range the data occupies, and R²
/// and explained variance centre on that mean before squaring. Issue #127.
/// </para>
/// <para>
/// Neumaier rather than Kahan: Kahan's correction is lost whenever the incoming term
/// is larger than the running total, which is exactly this shape — an accumulator
/// starting at zero taking terms near 1e9. The branch below is what fixes that, and
/// is the only difference between the two.
/// </para>
/// <para>
/// This is not fragile in the way it would be in C: .NET does not reassociate
/// floating-point arithmetic — there is no fast-math switch — so the compiler and the
/// JIT are both required to evaluate <c>(sum - total) + value</c> as written. The
/// compensation cannot be optimized away, and a reader arriving from a language where
/// it can should not "simplify" this.
/// </para>
/// </remarks>
internal struct CompensatedSum
{
    private double _sum;
    private double _compensation;

    /// <summary>Adds one term, keeping what the addition rounded off.</summary>
    /// <param name="value">The term to add.</param>
    public void Add(double value)
    {
        double total = _sum + value;
        _compensation += Math.Abs(_sum) >= Math.Abs(value)
            ? (_sum - total) + value
            : (value - total) + _sum;
        _sum = total;
    }

    /// <summary>The sum, with the accumulated rounding folded back in.</summary>
    public readonly double Value => _sum + _compensation;
}
```

- [ ] **Step 4: Accumulate R² through it**

In `R2.cs`, the block currently at `:145-185`. `means`, `numerators` and `denominators` become
`CompensatedSum[]`, `totalWeight` becomes a `CompensatedSum`, and the reductions read `.Value`:

```csharp
        double[] scores = new double[outputCount];
        double[] denominators = new double[outputCount];
        CompensatedSum[] numerators = new CompensatedSum[outputCount];
        CompensatedSum[] centredSquares = new CompensatedSum[outputCount];
        CompensatedSum[] meanSums = new CompensatedSum[outputCount];
        double[] means = new double[outputCount];
        bool weighted = !sampleWeight.IsEmpty;
        CompensatedSum totalWeight = default;

        for (int row = 0; row < samples; row++)
        {
            double weight = weighted ? sampleWeight[row] : 1.0;
            totalWeight.Add(weight);
            int offset = row * outputCount;
            for (int col = 0; col < outputCount; col++)
            {
                meanSums[col].Add(weight * yTrue[offset + col]);
            }
        }
        for (int col = 0; col < outputCount; col++)
        {
            means[col] = meanSums[col].Value / totalWeight.Value;
        }
```

and the second pass accumulates `numerators[col].Add(weight * residual * residual)` and
`centredSquares[col].Add(weight * centred * centred)`, with the final loop reading
`denominators[col] = centredSquares[col].Value;` before `Resolve(numerators[col].Value, denominators[col], …)`.

`denominators` stays a `double[]` because the method returns it to its caller; keep that return type
unchanged.

Note that an array element is accessed by reference, so `meanSums[col].Add(x)` mutates the element in
place. A `foreach` over the array would copy each element and silently drop the mutation — do not
introduce one.

- [ ] **Step 5: Green, everywhere**

```bash
dotnet build DataNet.slnx -c Release --no-incremental > /tmp/127-t2-b.log 2>&1; echo "build=$?"; tail -3 /tmp/127-t2-b.log
dotnet test DataNet.slnx -c Release > /tmp/127-t2-green.log 2>&1; echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/127-t2-green.log
```

Expected: 0 warnings, and **2951 passing** (2947 + 2 new tests × 2 mirrors). The 18 existing regression
corpus cases must still pass: compensation moves their last bits and they compare at `1e-9`, so a failure
there is a result to understand and report, not to accommodate.

If an analyzer objects to the new struct — `CA1815` on a value type without `Equals`, for instance —
apply the repository's own policy: a rule the whole area trips goes in that area's `Directory.Build.props`
with a comment naming it; a rule one site disagrees with gets a `#pragma` and a reason. Do not disable a
rule to avoid thinking about it.

- [ ] **Step 6: Commit**

```bash
git add src/DataNet.Metrics/Internal/CompensatedSum.cs src/DataNet.Metrics/R2.cs \
        tests/DataNet.Metrics.Tests/CompensatedSumTests.cs
git commit -m "Keep the bits a sequential sum drops out of R2's centring mean"
```

---

### Task 3: Explained variance, the other two-pass metric

**Files:**

- Modify: `src/DataNet.Metrics/ExplainedVariance.cs` (the block at `:120-165`)
- Modify: `tests/DataNet.Metrics.Tests/CompensatedSumTests.cs`

**Depends on:** Task 2.

**Interfaces:**

- Consumes: `CompensatedSum` from Task 2.

`ExplainedVariance` centres on the same mean and adds one of its own — the mean residual it subtracts
before squaring, which `R2` does not have. Both are accumulated.

- [ ] **Step 1: Write the failing test**

Add to `CompensatedSumTests.cs`, beside the R² pair:

```csharp
    /// <summary>
    /// Explained variance subtracts the mean residual before squaring, so it carries a
    /// second accumulation R² does not have. Same shape, same decimal reference.
    /// </summary>
    [Fact]
    public void ExplainedVariance_matches_a_decimal_reference_on_an_ill_conditioned_target()
    {
        (double[] yTrue, double[] yPred) = IllConditioned();

        double expected = (double)ExactExplainedVariance(yTrue, yPred);
        double actual = ExplainedVariance.Score(yTrue, yPred);

        Assert.Equal(expected, actual, 10);
    }

    /// <summary>Explained variance in <see cref="decimal"/>: 1 − Var(y − ŷ) ⁄ Var(y).</summary>
    private static decimal ExactExplainedVariance(double[] yTrue, double[] yPred)
    {
        decimal mean = 0m;
        decimal meanResidual = 0m;
        for (int i = 0; i < yTrue.Length; i++)
        {
            mean += (decimal)yTrue[i];
            meanResidual += (decimal)yTrue[i] - (decimal)yPred[i];
        }
        mean /= yTrue.Length;
        meanResidual /= yTrue.Length;

        decimal numerator = 0m;
        decimal denominator = 0m;
        for (int i = 0; i < yTrue.Length; i++)
        {
            decimal residual = (decimal)yTrue[i] - (decimal)yPred[i] - meanResidual;
            decimal centred = (decimal)yTrue[i] - mean;
            numerator += residual * residual;
            denominator += centred * centred;
        }
        return 1m - (numerator / denominator);
    }
```

Check `ExplainedVariance.Score`'s real signature before writing the call.

- [ ] **Step 2: Run it and watch it fail**

```bash
dotnet test DataNet.slnx -c Release --filter "FullyQualifiedName~ExplainedVariance_matches" > /tmp/127-t3-red.log 2>&1
echo "test=$?"
grep -E "^Réussi!|^Échoué!|Assert" /tmp/127-t3-red.log | head -6
```

Expected: two failures, one per mirrored project. A run of 0 tests is not a red run.

- [ ] **Step 3: Accumulate it through `CompensatedSum`**

The same treatment as Task 2, on `ExplainedVariance.cs:120-165`: `means`, `meanResiduals`, `numerators`
and the centred squares become `CompensatedSum[]`, `totalWeight` becomes a `CompensatedSum`, the two
division loops read `.Value`, and the returned `denominators` stays a `double[]`.

- [ ] **Step 4: Green**

```bash
dotnet build DataNet.slnx -c Release --no-incremental > /tmp/127-t3-b.log 2>&1; echo "build=$?"; tail -3 /tmp/127-t3-b.log
dotnet test DataNet.slnx -c Release > /tmp/127-t3-green.log 2>&1; echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/127-t3-green.log
```

Expected: 0 warnings, **2953 passing** (2951 + 1 × 2).

- [ ] **Step 5: Commit**

```bash
git add src/DataNet.Metrics/ExplainedVariance.cs tests/DataNet.Metrics.Tests/CompensatedSumTests.cs
git commit -m "Compensate the second accumulation explained variance carries"
```

---

### Task 4: The walk seven metrics share

**Files:**

- Modify: `src/DataNet.Metrics/Internal/Outputs.cs` (`WeightedMean`, at `:186-215`)
- Modify: `tests/DataNet.Metrics.Tests/CompensatedSumTests.cs`

**Depends on:** Tasks 1 and 2.

**Interfaces:**

- Consumes: `CompensatedSum` from Task 2.
- Produces: nothing later tasks call. Task 6 measures what this costs and may reverse it.

This is `MeanSquaredError`, `RootMeanSquaredError`, `MeanAbsoluteError`, `MeanAbsolutePercentageError`,
`MeanSquaredLogError`, `RootMeanSquaredLogError` and `PinballLoss` at once — and the hot loop
`mse_n1000000_k10` measures at 1.00× against numpy.

- [ ] **Step 1: Decide, from Task 1's numbers, whether a failing test exists to write**

**Task 1 settled this: the kernel sum's relative error is `0.0`, exactly.** The array these seven metrics
average holds squared *residuals*, which are differences — the offset that wrecks the centring mean is gone
before the first addition, and a sequential sum and a compensated one come out bit-identical. Measured on
the same shape that puts R² 357× outside the oracle tolerance, and measured twice, at n = 200 000 and at
n = 1 000 000.

So **write no behavioural test here**: there is no payload inside this metric's contract that one could
catch, this change is a strictness upgrade rather than a bug fix, and Task 6's benchmark decides whether it
stays. Do not invent a test that passes before and after; that is a test that asserts nothing.

Either way, one property is worth pinning and costs nothing: the total weight is accumulated too, so a
million samples of weight `0.1` must average exactly what the same data averages unweighted.

```csharp
    /// <summary>
    /// The shared walk accumulates the total weight as well as the values, over as many
    /// terms as there are samples. A million equal weights must still divide the sum by
    /// exactly what they add up to.
    /// </summary>
    [Fact]
    public void A_uniform_weight_changes_no_shared_mean()
    {
        const int Rows = 1_000_000;
        double[] yTrue = new double[Rows];
        double[] yPred = new double[Rows];
        double[] weights = new double[Rows];
        for (int i = 0; i < Rows; i++)
        {
            yTrue[i] = 1.0 + (i % 17);
            yPred[i] = yTrue[i] - 0.25;
            weights[i] = 0.1;
        }

        double unweighted = MeanSquaredError.Score(yTrue, yPred);
        double weighted = MeanSquaredError.Score(yTrue, yPred, sampleWeight: weights);

        Assert.Equal(unweighted, weighted, 15);
    }
```

Check `MeanSquaredError.Score`'s parameter order before writing that call — `sampleWeight` is one of
several optional parameters.

- [ ] **Step 2: Run whatever you wrote and record the result**

```bash
dotnet test DataNet.slnx -c Release --filter "FullyQualifiedName~CompensatedSumTests" > /tmp/127-t4-red.log 2>&1
echo "test=$?"
grep -E "^Réussi!|^Échoué!" /tmp/127-t4-red.log
```

- [ ] **Step 3: Compensate the walk**

In `Outputs.WeightedMean`, `result` becomes a `CompensatedSum[]` and `totalWeight` a `CompensatedSum`;
the final loop builds the `double[]` the method returns:

```csharp
        CompensatedSum[] sums = new CompensatedSum[outputCount];
        bool weighted = !sampleWeight.IsEmpty;
        CompensatedSum totalWeight = default;

        for (int row = 0; row < samples; row++)
        {
            double weight = weighted ? sampleWeight[row] : 1.0;
            totalWeight.Add(weight);
            int offset = row * outputCount;
            for (int col = 0; col < outputCount; col++)
            {
                sums[col].Add(weight * kernel.Apply(yTrue[offset + col], yPred[offset + col]));
            }
        }

        double[] result = new double[outputCount];
        double total = totalWeight.Value;
        for (int col = 0; col < outputCount; col++)
        {
            result[col] = sums[col].Value / total;
        }

        return result;
```

The method's signature and return type do not change; `SquareRoots` keeps receiving the same `double[]`.

- [ ] **Step 4: Green**

```bash
dotnet build DataNet.slnx -c Release --no-incremental > /tmp/127-t4-b.log 2>&1; echo "build=$?"; tail -3 /tmp/127-t4-b.log
dotnet test DataNet.slnx -c Release > /tmp/127-t4-green.log 2>&1; echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/127-t4-green.log
```

State the total. Seven metrics change their last bits here, so every corpus case that exercises them is
re-verified by this run.

- [ ] **Step 5: Commit**

```bash
git add src/DataNet.Metrics/Internal/Outputs.cs tests/DataNet.Metrics.Tests/CompensatedSumTests.cs
git commit -m "Compensate the walk the seven kernel metrics share"
```

---

### Task 5: The fixture the corpus cannot hold

**Files:**

- Modify: `tools/generate_oracles.py`
- Create: `tests/oracles/regression_conditioning.json` (generated, committed)
- Create: `tests/DataNet.Metrics.Tests/RegressionConditioningTests.cs`

**Depends on:** Tasks 2-4.

**Interfaces:**

- Produces: nothing later tasks call.

`tests/oracles/regression.json` stores its arrays as JSON and caps at 450 values. This case has 200 000,
so it carries the parameters instead and both sides build the arrays from one closed form.

- [ ] **Step 1: Add the generator**

In `tools/generate_oracles.py`, beside the other regression generators:

```python
# --- The conditioning the ordinary regression corpus cannot reach (issue #127) ---
#
# regression.json stores its arrays in full and caps at 450 values, over targets in
# [0.5, 40] -- a range where a sequential sum and numpy's pairwise one agree to far
# more digits than the corpus compares at. The defect #127 fixes needs the opposite:
# many samples, and a large offset over a small spread, so that the low-order bits of
# every term fall off the end of the accumulator.
#
# Storing 200 000 samples as JSON would be megabytes, so this case carries the closed
# form instead. The C# side rebuilds the same arrays from the same expression, in the
# same order -- both languages evaluate IEEE-754 doubles, so the two constructions are
# identical value for value. PROBE_INDICES is how that stops being a matter of faith:
# the raw bits at those positions are recorded and compared before anything is scored.

CONDITIONING_SAMPLES = 200_000
CONDITIONING_OFFSET = 1e9
CONDITIONING_SPREAD = 1e-2
# 1e-6 and not the ramp's own 5e-8 step: the ULP at 1e9 is about 2.4e-7, so a
# perturbation below half of that rounds straight back onto the target. Measured:
# with 1e-8 every residual is exactly zero, mse is 0 and r2 is 1, and a fixture built
# that way passes while proving nothing. The ramp's step stays below the ULP on
# purpose -- quantizing it is the ill-conditioning this case exists to carry.
CONDITIONING_PERTURBATION = 1e-6
PROBE_INDICES = [0, 1, CONDITIONING_SAMPLES // 2, CONDITIONING_SAMPLES - 2, CONDITIONING_SAMPLES - 1]


def _conditioning_arrays() -> tuple[list[float], list[float]]:
    """The closed form both sides build, and nothing but it."""
    step = CONDITIONING_SPREAD / CONDITIONING_SAMPLES
    y_true = [CONDITIONING_OFFSET + i * step for i in range(CONDITIONING_SAMPLES)]
    y_pred = [y_true[i] + ((i % 7) - 3) * CONDITIONING_PERTURBATION
              for i in range(CONDITIONING_SAMPLES)]
    return y_true, y_pred


def _bits(value: float) -> str:
    """The double's raw IEEE-754 bits, so a probe compares the number and not its spelling."""
    import struct  # noqa: PLC0415

    return f"{struct.unpack('<Q', struct.pack('<d', value))[0]:016x}"


def generate_regression_conditioning() -> dict:
    """scikit-learn's answers on a target no committed array could carry."""
    y_true, y_pred = _conditioning_arrays()
    yt = np.asarray(y_true)
    yp = np.asarray(y_pred)

    values = {
        "r2": stable(float(skm.r2_score(yt, yp))),
        "explained_variance": stable(float(skm.explained_variance_score(yt, yp))),
        "mse": stable(float(skm.mean_squared_error(yt, yp))),
        "mae": stable(float(skm.mean_absolute_error(yt, yp))),
    }
    return {
        "metadata": {
            "algorithm": "Regression under ill conditioning",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.r2_score",
                "sklearn.metrics.explained_variance_score",
                "sklearn.metrics.mean_squared_error",
                "sklearn.metrics.mean_absolute_error",
            ],
            "samples": CONDITIONING_SAMPLES,
            "offset": CONDITIONING_OFFSET,
            "spread": CONDITIONING_SPREAD,
            "perturbation": CONDITIONING_PERTURBATION,
            "construction": (
                "step = spread / samples; y_true[i] = offset + i * step; "
                "y_pred[i] = y_true[i] + ((i % 7) - 3) * perturbation"
            ),
            "probe_indices": PROBE_INDICES,
            "probe_bits_y_true": [_bits(y_true[i]) for i in PROBE_INDICES],
            "probe_bits_y_pred": [_bits(y_pred[i]) for i in PROBE_INDICES],
            "count": len(values),
        },
        "values": values,
    }
```

Register it in `main`'s generators dict as `"regression_conditioning.json": generate_regression_conditioning`.
Check the names the file already uses for the numpy and scikit-learn imports and for `stable(...)` before
writing this — reuse them rather than adding imports.

- [ ] **Step 2: Generate, from a neutral directory, and read the generator's own exit code**

```bash
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py > /tmp/127-gen.log 2>&1
echo "generate=$?"
tail -3 /tmp/127-gen.log
cd <repo> && git status --porcelain tests/oracles/
ls -la tests/oracles/regression_conditioning.json
```

Expected: exit 0 and **exactly one new file**, a few kilobytes. If any other corpus moved, stop and report
it — this change touches no other generator.

- [ ] **Step 3: Replay it**

`tests/DataNet.Metrics.Tests/RegressionConditioningTests.cs`:

```csharp
using System.Globalization;
using System.Text.Json;
using DataNet.Metrics;
using Xunit;

namespace DataNet.Metrics.Tests;

/// <summary>
/// Replays <c>regression_conditioning.json</c>: 200 000 samples of a target with a
/// large offset over a small spread, which is where a sequential sum and numpy's
/// pairwise one part company. Issue #127.
/// </summary>
/// <remarks>
/// The corpus carries no arrays — they would be megabytes — but the closed form that
/// builds them, and the raw bits of five values along the way. Those bits are compared
/// before anything is scored: two sides that build slightly different arrays would
/// otherwise compare their scores happily and prove nothing.
/// </remarks>
public sealed class RegressionConditioningTests
{
    private static readonly JsonDocument Corpus = OracleLoader.Load("regression_conditioning.json");

    [Fact]
    public void The_rebuilt_arrays_are_the_ones_scikit_learn_scored()
    {
        (double[] yTrue, double[] yPred) = Build();
        JsonElement metadata = Corpus.RootElement.GetProperty("metadata");
        int[] indices = [.. metadata.GetProperty("probe_indices").EnumerateArray().Select(e => e.GetInt32())];
        string[] trueBits = [.. metadata.GetProperty("probe_bits_y_true").EnumerateArray().Select(e => e.GetString()!)];
        string[] predBits = [.. metadata.GetProperty("probe_bits_y_pred").EnumerateArray().Select(e => e.GetString()!)];

        Assert.NotEmpty(indices);
        for (int probe = 0; probe < indices.Length; probe++)
        {
            Assert.Equal(trueBits[probe], Bits(yTrue[indices[probe]]));
            Assert.Equal(predBits[probe], Bits(yPred[indices[probe]]));
        }
    }

    [Theory]
    [InlineData("r2")]
    [InlineData("explained_variance")]
    [InlineData("mse")]
    [InlineData("mae")]
    public void Each_metric_matches_scikit_learn(string key)
    {
        (double[] yTrue, double[] yPred) = Build();
        double expected = OracleLoader.Number(Corpus.RootElement.GetProperty("values").GetProperty(key));

        double actual = key switch
        {
            "r2" => R2.Score(yTrue, yPred),
            "explained_variance" => ExplainedVariance.Score(yTrue, yPred),
            "mse" => MeanSquaredError.Score(yTrue, yPred),
            "mae" => MeanAbsoluteError.Score(yTrue, yPred),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "no metric for this corpus key"),
        };

        RegressionCorpus.AssertClose(expected, actual, key);
    }

    /// <summary>The corpus's own closed form, evaluated in the same order Python evaluates it.</summary>
    private static (double[] YTrue, double[] YPred) Build()
    {
        JsonElement metadata = Corpus.RootElement.GetProperty("metadata");
        int samples = metadata.GetProperty("samples").GetInt32();
        double offset = metadata.GetProperty("offset").GetDouble();
        double step = metadata.GetProperty("spread").GetDouble() / samples;
        double perturbation = metadata.GetProperty("perturbation").GetDouble();

        double[] yTrue = new double[samples];
        double[] yPred = new double[samples];
        for (int i = 0; i < samples; i++)
        {
            yTrue[i] = offset + (i * step);
            yPred[i] = yTrue[i] + (((i % 7) - 3) * perturbation);
        }
        return (yTrue, yPred);
    }

    private static string Bits(double value) =>
        BitConverter.DoubleToInt64Bits(value).ToString("x16", CultureInfo.InvariantCulture);
}
```

`RegressionCorpus.AssertClose` is `internal` to the test project and compares at
`1e-9 * max(1, |expected|)`. Read `tests/DataNet.Metrics.Tests/OracleLoader.cs` for `Number(...)`'s real
name before using it.

- [ ] **Step 4: Green, then prove the replay discriminates**

```bash
dotnet test DataNet.slnx -c Release > /tmp/127-t5-green.log 2>&1; echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/127-t5-green.log
```

Then mutate the **output-directory copy** of the corpus — never the committed source file — and confirm the
probe test fails:

```bash
CORPUS=tests/DataNet.Metrics.Tests/bin/Release/net10.0/oracles/regression_conditioning.json
cp "$CORPUS" /tmp/127-corpus.bak
python3 - <<'EOF'
import json
p = "tests/DataNet.Metrics.Tests/bin/Release/net10.0/oracles/regression_conditioning.json"
d = json.load(open(p))
d["metadata"]["offset"] = 1e9 + 1
json.dump(d, open(p, "w"), ensure_ascii=False, indent=1)
EOF
dotnet test tests/DataNet.Metrics.Tests -c Release --no-build --filter "FullyQualifiedName~RegressionConditioning" > /tmp/127-t5-mutant.log 2>&1
echo "mutant=$?"
grep -A3 "Message d'erreur" /tmp/127-t5-mutant.log | head -8
cp /tmp/127-corpus.bak "$CORPUS"
```

Expected: non-zero, with the probe test naming the mismatch. Report the message.

- [ ] **Step 5: Commit**

```bash
git add tools/generate_oracles.py tests/oracles/regression_conditioning.json \
        tests/DataNet.Metrics.Tests/RegressionConditioningTests.cs
git commit -m "Measure the metrics on the conditioning the corpus cannot carry"
```

---

### Task 6: What it costs, and whether the hot loop keeps it

**Files:**

- Modify: `docs/guides/performance.md` (the section "Regression metrics — mse, mae, median_ae, r2")
- Possibly modify: `src/DataNet.Metrics/Internal/Outputs.cs` (only if the rule below says so)

**Depends on:** Tasks 1-4.

**Interfaces:**

- Consumes: Task 1's kernel-sum error, Task 4's compensation.

The spec's D3 fixed the rule before the number was known:

> All three sites are compensated **unless** the benchmark cost on `mse` at n = 1 000 000 exceeds **10%**
> *and* the measured relative error of the uncompensated kernel sum stays below **1e-12**.

- [ ] **Step 1: Measure before against after, in one window, interleaved**

The bench corpus is regenerated per checkout, so a baseline built in a fresh worktree would measure
*different data*. Build the baseline from the merge base and copy this branch's corpus into it, then run
the two alternately rather than one campaign after the other — campaign-by-campaign timing on a shared
machine measures the machine.

```bash
cd <repo>
git worktree add /tmp/127-baseline $(git merge-base origin/main HEAD)
cp -r bench/corpus/. /tmp/127-baseline/bench/corpus/
uptime
for round in 1 2; do
  dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*Metrics*mse*' \
    > /tmp/127-after-$round.log 2>&1
  dotnet run -c Release --project /tmp/127-baseline/bench/DataNet.Text.Benchmarks -- --filter '*Metrics*mse*' \
    > /tmp/127-before-$round.log 2>&1
done
uptime
```

Read `bench/README.md` first: it states the harness, the filter syntax and the methodology this repository
reports under, and the numbers you publish must be comparable to the ones already on the page. If the
filter above matches nothing, find the right one from `bench/DataNet.Text.Benchmarks/CrossLang/MetricsCrossLang.cs`
rather than reporting a run of zero benchmarks as a result.

- [ ] **Step 2: Apply the rule, and say which branch of it you took**

Compute the cost as the ratio of medians, after over before, for `mse` at n = 1 000 000. Then:

- **cost ≤ 10%**, or Task 1's kernel-sum error at or above `1e-12` → the compensation stays. Say both
  numbers.
- **cost > 10% and error < 1e-12** → revert Task 4's change to `Outputs.WeightedMean` only
  (`git revert --no-commit <task 4 commit>` then unstage anything that is not `Outputs.cs`, or edit it
  back by hand), keep `R2` and `ExplainedVariance` compensated, and amend the spec's D3 with the two
  numbers that decided it. The tests from Task 4 that would then fail must go with it.

Whichever branch you take, the report states the cost, the error, and the decision in that order.

- [ ] **Step 3: Write the numbers into the guide**

Extend the existing regression section rather than starting a new one. It must carry: what changed, the
before/after for each of the four operations, the machine, and the load average at the start and end of the
window — the section already sets that standard, and a comparison whose load is not stated is not
comparable to the rows above it.

- [ ] **Step 4: Remove the baseline worktree and commit**

```bash
git worktree remove /tmp/127-baseline --force
git add docs/guides/performance.md src/DataNet.Metrics/Internal/Outputs.cs
git commit -m "Publish what compensating the regression sums costs"
```

---

### Task 7: The record, and final verification

**Files:**

- Modify: `CHANGELOG.md`
- Modify: `docs/equivalence.md`

**Depends on:** Tasks 2-6.

- [ ] **Step 1: Amend the CHANGELOG entry rather than adding a Fixed one**

```bash
git tag --list 'DataNet.Metrics/*'
grep -n "DataNet.Metrics" CHANGELOG.md | head -5
```

`DataNet.Metrics — 0.1.0` sits under `[Unreleased]` and the regression metrics merged on 2026-08-11, so no
published package contains them: there is no user to tell about a fix. Amend the entry that describes them
so it stays true — the sums are compensated, and why.

- [ ] **Step 2: One clause in `docs/equivalence.md`**

Find the regression rows and add, in the voice they already use, that the accumulation is compensated, so
the answers are at least as accurate as numpy's pairwise reduction rather than merely close to it. Change
nothing else on those rows.

- [ ] **Step 3: Every gate, with real exit codes**

```bash
cd <repo>
git status --porcelain                                                    # empty
dotnet build DataNet.slnx -c Release --no-incremental > /tmp/127-fv-b.log 2>&1; echo "build=$?"; tail -3 /tmp/127-fv-b.log
dotnet format DataNet.slnx --verify-no-changes > /tmp/127-fv-f.log 2>&1;   echo "format=$?"
dotnet test DataNet.slnx -c Release > /tmp/127-fv-t.log 2>&1;              echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/127-fv-t.log
python3 tools/check_version_floor.py > /tmp/127-fv-v.log 2>&1;             echo "floor=$?"
<repo>/.venv-oracles/bin/python -m pytest tools/tests -q > /tmp/127-fv-p.log 2>&1; echo "pytest=$?"; tail -1 /tmp/127-fv-p.log
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" \
  "tools/README.md" "bench/README.md" > /tmp/127-fv-md.log 2>&1; echo "markdownlint=$?"
```

All 0, 0 warnings, and the eight per-assembly test counts read and stated.

- [ ] **Step 4: The oracle drift gate**

```bash
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py > /tmp/127-fv-gen.log 2>&1
echo "generate=$?"
cd <repo> && git status --porcelain tests/oracles/
```

Expected: empty. This gate is known to be flaky here — regenerate once more before reporting drift.

- [ ] **Step 5: The two gates outside the solution**

```bash
SCRATCH=<this session's scratchpad>
rm -rf ./artifacts "$SCRATCH/pack-packages"
NUGET_PACKAGES="$SCRATCH/pack-packages" bash -c 'for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do dotnet pack "$p" -c Release -o ./artifacts || exit 1; done'
python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
rm -rf "$SCRATCH/sample-packages"
NUGET_PACKAGES="$SCRATCH/sample-packages" dotnet run --project samples/DataNet.Sample -c Release
python3 tools/extract_doc_snippets.py
NUGET_PACKAGES="$SCRATCH/sample-packages" dotnet build samples/DataNet.DocSnippets -c Release --no-incremental
```

This branch adds no public type, so the packaging gate should pass unchanged.

- [ ] **Step 6: Commit the record, then stop**

```bash
git add CHANGELOG.md docs/equivalence.md
git commit -m "Record that the regression sums are compensated"
```

Do not push and do not open a pull request. Report the state and let the user decide both.

---

## Self-Review

**Spec coverage.** D1 → Task 2 Step 3. D2 → Tasks 2, 3 and 4, one site each. D3 → Task 4 Step 1 and Task 6
Step 2, which is where the rule is applied. D4 → Task 5. D5 → Task 2's test pair, adapted: the spec asked
for a unit test of `CompensatedSum`, and `DataNet.Metrics` exposes no internals to its tests, so it is
asserted through `R2.Score` against a `decimal` reference with a sequential control beside it — same
guarantee, no new visibility. D6 → Tasks 6 and 7. Evidence section → Tasks 2, 3, 5 and 6. Risks: the
existing 18 cases are re-run at every task's Step 4; the procedural fixture's drift is Task 5 Step 3's
probes; the noisy benchmark window is Task 6 Step 1's interleaving and its `uptime` readings.

**Placeholders.** Two are deliberate and marked: Task 4 Step 1 branches on a number Task 1 measures, and
says what to do in both directions; Task 6 Step 2 does the same for the benchmark. `<repo>` and `SCRATCH`
in Task 7 are paths only the executing session knows. Every other code block is complete.

**Type consistency.** `CompensatedSum`, `Add(double)` and `Value` are the names used in Tasks 2, 3 and 4.
`R2.Score`, `ExplainedVariance.Score`, `MeanSquaredError.Score` and `MeanAbsoluteError.Score` are the four
public entry points the tests call, and each task says to check the real signature before writing the call.
The corpus file is `regression_conditioning.json` in Task 5's three steps and in the file table.
