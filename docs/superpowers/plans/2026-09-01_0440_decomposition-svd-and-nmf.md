# `Lodestar.Decomposition` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship `Lodestar.Decomposition` 0.1.0 — `TruncatedSvd` at
`sklearn.decomposition.TruncatedSVD(algorithm="randomized")` parity and `Nmf` at
`sklearn.decomposition.NMF(solver="mu")` parity — with one package edge, to
`Lodestar.Abstractions` 0.1.1.

**Architecture:** Randomized SVD never factorizes the sparse matrix: it multiplies it by a thin
dense block and factorizes *that*. `CsrMatrix.Multiply(block, columnCount)` and
`CsrMatrix.TransposeMultiply(block, columnCount)` already ship in `Lodestar.Abstractions` 0.1.1
(step A of this issue), so what is left is three dense kernels — thin Householder QR, LU with
partial pivoting, one-sided Jacobi SVD — and the two algorithms composed on top of them. Every
dense block in this package is a flat `double[]`, **row-major**, carried beside its width, which
is the shape `CsrMatrix`'s products already take and return.

**Tech Stack:** C# on `net10.0;netstandard2.0`, xunit, frozen JSON oracles generated from
scikit-learn 1.9.0 and scipy, BenchmarkDotNet.

**Spec:** [`docs/superpowers/specs/2026-09-01_0440_decomposition-truncated-svd-and-nmf.md`](../specs/2026-09-01_0440_decomposition-truncated-svd-and-nmf.md)

**Branch:** `feat/440-decomposition`

## Global Constraints

- **Target frameworks:** `net10.0;netstandard2.0`, one public API, never a reduced one. Anything
  the older target lacks is closed in the fixed order PolySharp → `System.Memory` /
  `System.Numerics.Vectors` referenced on that target only → hand-written fallback.
- **One package edge, and only one:** `Lodestar.Decomposition` → `Lodestar.Abstractions` **0.1.1**,
  as a `PackageReference` resolved from `src/Directory.Packages.props`. No `ProjectReference` from
  anything under `src/` — a CI job asserts this through evaluated MSBuild. No `Lodestar.Text` edge,
  no third-party dependency beyond the two `netstandard2.0` polyfills.
- **Version:** `LodestarDecompositionVersion` = `0.1.0`, declared in
  `src/Lodestar.Decomposition/Version.props` and nowhere else.
- **Working across two packages:** `Lodestar.Abstractions` 0.1.1 is already on nuget.org, so the
  restore resolves it with no pack step and `LodestarUseProjectRefs` must stay **unset** for the
  whole of this branch. If a restore cannot see 0.1.1, run
  `dotnet nuget locals http-cache --clear` — the flat container is ahead of the local cache.
- **Oracle discipline:** every number the C# produces is compared against a frozen corpus at
  `1e-9`. Generate from a working directory that is **not an ancestor of the checkout** (`/var/tmp`
  when the worktree is under `/tmp`), with `.venv-oracles`' interpreter, and read the generator's
  **own** exit code — never a pipeline's.
- **Every corpus payload carries `metadata`.** `main()` prints
  `payload['metadata']['count']` for every generator, so a payload without it raises `KeyError`
  and the generation exits non-zero. Return `{"metadata": {..., "count": len(cases)}, ...}` — do
  not loosen `main()`, which is shared by forty generators.
- **A factor-level comparison runs on full-rank fixtures only.** Past a vanished pivot a QR's
  or an LU's factors stop being basis-independent: measured, scipy's own `|diag(R)|` moves from
  2.124424 to 2.157438 under a 1e-14 perturbation of a duplicated column. Reconstruction and
  structural assertions run on every fixture, the rank-deficient one included — that is what
  proves the zero-pivot guard.
- **Analyzers gate the build.** `SonarAnalyzer.CSharp` plus the .NET code-quality rules at
  `AnalysisMode=All`, `AnalysisLevel=10.0`, warnings as errors. A rule an area trips by being that
  area goes in that area's `Directory.Build.props` with a comment naming each rule; a rule one call
  site disagrees with goes in a `#pragma warning disable` with the reason above it.
- **Comments** say why, not what: two lines inline, eight lines of prose in XML documentation
  (`<remarks>`/`<para>` tag lines count). `long-comment:` markers must stay exceptional.
- **Provenance:** scikit-learn is BSD-3-Clause, so reading it to confirm a constant or a branch
  order is fine, and is what the corpus arbitrates anyway. Write the C# from the algorithm, not by
  transcription. Never transcribe GPL-licensed code from anywhere.
- **Everything in English** — code, comments, ADRs, commit messages. Commit messages carry no
  `feat:`/`fix:` prefix and reference the issue with `Part of #440.`
- **Gates run once, before the pull request**, not inside each task. Each task's own step list ends
  at its tests and its commit.

---

## File Structure

| file | responsibility |
| --- | --- |
| `src/Lodestar.Decomposition/Version.props` | `LodestarDecompositionVersion`, 0.1.0 |
| `src/Lodestar.Decomposition/Lodestar.Decomposition.csproj` | package identity, the one edge, the two `InternalsVisibleTo` grants |
| `src/Lodestar.Decomposition/Internal/GaussianSampler.cs` | the package's own PRNG: SplitMix64 + Box–Muller, standard normals from an `int` seed |
| `src/Lodestar.Decomposition/Internal/HouseholderQr.cs` | thin (economic) QR of a tall row-major block |
| `src/Lodestar.Decomposition/Internal/PartialPivotLu.cs` | `PL` of `scipy.linalg.lu(permute_l=True)`, the `LU` power-iteration normalizer |
| `src/Lodestar.Decomposition/Internal/JacobiSvd.cs` | one-sided Jacobi SVD of a dense row-major block |
| `src/Lodestar.Decomposition/Internal/RandomizedRangeFinder.cs` | `Q` from `A`, `Ω`, the iteration count and the normalizer |
| `src/Lodestar.Decomposition/Internal/DenseBlock.cs` | the row-major helpers every kernel shares: transpose, column norms, sign flip |
| `src/Lodestar.Decomposition/PowerIterationNormalizer.cs` | `Auto`, `None`, `Qr`, `Lu` |
| `src/Lodestar.Decomposition/TruncatedSvd.cs` | `TruncatedSvdOptions` and the fitted `TruncatedSvd` |
| `src/Lodestar.Decomposition/NmfInitialization.cs` | `NndSvd`, `NndSvda` |
| `src/Lodestar.Decomposition/NmfBetaLoss.cs` | `Frobenius`, `KullbackLeibler` |
| `src/Lodestar.Decomposition/Internal/NndSvd.cs` | the NNDSVD initialisation family |
| `src/Lodestar.Decomposition/Nmf.cs` | `NmfOptions` and the fitted `Nmf` |
| `tests/Lodestar.Decomposition.Tests/` | the one suite, on the net10.0 assembly |
| `tests/Lodestar.Decomposition.NetStandard.Tests/` | the same sources, linked, on the netstandard2.0 assembly |
| `tests/oracles/decomposition_qr.json` | `scipy.linalg.qr(mode="economic")` |
| `tests/oracles/decomposition_lu.json` | `scipy.linalg.lu(permute_l=True)` |
| `tests/oracles/decomposition_svd.json` | `scipy.linalg.svd` (dense cases) and `sklearn.utils.extmath.randomized_svd` + `TruncatedSVD` (randomized cases) |
| `tests/oracles/decomposition_nmf.json` | `_initialize_nmf` and `NMF(solver="mu")` |
| `samples/Lodestar.Sample/DecompositionSamples.cs` + four `*Sample.cs` | decision 0041's per-class samples, and ADR 0009's packaging gate |
| `docs/reference/decomposition/` | the reference pages the gate enforces |
| `docs/guides/decomposition.md` | the guide |
| `bench/Lodestar.Decomposition.Benchmarks/` | the harness, against ML.NET's `ProjectToPrincipalComponents` |

**Row-major, always.** A block of `r` rows and `c` columns is a `double[r * c]` where element
`(i, j)` is at `i * c + j`. Every kernel below takes and returns that layout, and every signature
carries the width beside the array. There is no column-major array anywhere in this package.

---

## Task 1: The package, its wiring, and the PRNG

**Files:**

- Create: `src/Lodestar.Decomposition/Version.props`
- Create: `src/Lodestar.Decomposition/Lodestar.Decomposition.csproj`
- Create: `src/Lodestar.Decomposition/Internal/GaussianSampler.cs`
- Create: `tests/Lodestar.Decomposition.Tests/Lodestar.Decomposition.Tests.csproj`
- Create: `tests/Lodestar.Decomposition.Tests/OracleLoader.cs`
- Create: `tests/Lodestar.Decomposition.Tests/GaussianSamplerTests.cs`
- Create: `tests/Lodestar.Decomposition.NetStandard.Tests/Lodestar.Decomposition.NetStandard.Tests.csproj`
- Modify: `Lodestar.slnx`
- Modify: `docs/wiki-map.json`
- Modify: `tools/check_nuspec_dependencies.py`
- Modify: `tools/check_version_floor.py`
- Modify: `tools/check_sample_coverage.py`
- Modify: `.github/workflows/ci.yml` (four pack/ProjectReference loops)
- Modify: `.github/workflows/sonarcloud.yml` (one pack loop)
- Modify: `.github/workflows/wiki.yml` (the released loop)
- Modify: `.github/workflows/release.yml` (the allow-list `case`)
- Modify: `.github/workflows/release-nuget-org.yml` (the `package` choice list)
- Modify: `samples/Lodestar.Sample/Lodestar.Sample.csproj`
- Modify: `samples/Lodestar.DocSnippets/Lodestar.DocSnippets.csproj`

**Interfaces:**

- Consumes: `Lodestar.Abstractions.CsrMatrix` 0.1.1 — `RowCount`, `ColumnCount`, `Values`,
  `ColumnIndices`, `RowPointers`, `Multiply(ReadOnlySpan<double> block, int columnCount)`,
  `TransposeMultiply(ReadOnlySpan<double> block, int columnCount)`.
- Produces: `internal sealed class GaussianSampler` with
  `internal GaussianSampler(int seed)` and `internal double[] Normal(int rows, int columns)`
  returning a row-major `double[rows * columns]` of standard normals.

- [ ] **Step 1: Write the failing tests**

Create `tests/Lodestar.Decomposition.Tests/GaussianSamplerTests.cs`:

```csharp
using Lodestar.Decomposition.Internal;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// The package's own generator. It is deliberately not numpy's: a seed reproduces a run of
/// Lodestar, never scikit-learn's matrix, which is why the corpus freezes Ω as an input.
/// </summary>
public sealed class GaussianSamplerTests
{
    [Fact]
    public void The_same_seed_draws_the_same_block()
    {
        double[] first = new GaussianSampler(20260901).Normal(6, 4);
        double[] second = new GaussianSampler(20260901).Normal(6, 4);

        Assert.Equal(first, second);
    }

    [Fact]
    public void A_different_seed_draws_a_different_block()
    {
        double[] first = new GaussianSampler(1).Normal(6, 4);
        double[] second = new GaussianSampler(2).Normal(6, 4);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void The_block_has_the_shape_it_was_asked_for()
    {
        Assert.Equal(24, new GaussianSampler(7).Normal(6, 4).Length);
    }

    [Fact]
    public void The_draws_are_standard_normal_to_two_decimals()
    {
        double[] draws = new GaussianSampler(20260901).Normal(20_000, 5);

        double mean = 0;
        foreach (double draw in draws)
        {
            mean += draw;
        }
        mean /= draws.Length;

        double variance = 0;
        foreach (double draw in draws)
        {
            variance += (draw - mean) * (draw - mean);
        }
        variance /= draws.Length;

        Assert.Equal(0.0, mean, 2);
        Assert.Equal(1.0, variance, 2);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(4, 0)]
    [InlineData(-1, 4)]
    public void A_block_with_no_elements_is_refused(int rows, int columns)
    {
        GaussianSampler sampler = new(7);

        Assert.Throws<ArgumentOutOfRangeException>(() => sampler.Normal(rows, columns));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release`
Expected: the project does not exist yet, so this fails to restore. That is the failure being
fixed; the steps below create the project and the type together.

- [ ] **Step 3: Create the package**

`src/Lodestar.Decomposition/Version.props`:

```xml
<Project>

  <!--
    Lodestar.Decomposition owns its version here, independently of the other
    packages (see docs/decisions/0012-per-package-versioning.md).

    0.1.0 is this package's first release. It carries one inter-package edge, to
    Lodestar.Abstractions, whose floor lives in src/Directory.Packages.props and
    is deliberately decoupled from this number.
  -->
  <PropertyGroup>
    <LodestarDecompositionVersion>0.1.0</LodestarDecompositionVersion>
  </PropertyGroup>

</Project>
```

`src/Lodestar.Decomposition/Lodestar.Decomposition.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- This package's version, owned here rather than repository-wide. -->
  <Import Project="Version.props" />

  <PropertyGroup>
    <Version>$(LodestarDecompositionVersion)</Version>
    <TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>
    <RootNamespace>Lodestar.Decomposition</RootNamespace>

    <PackageId>Lodestar.Decomposition</PackageId>
    <Description>Truncated SVD and non-negative matrix factorization for .NET at scikit-learn parity, over a sparse matrix and without centring it: latent semantic analysis on a term-document matrix, with explained variance, and NMF for a parts-based decomposition. Randomized SVD with all three power-iteration normalizers; the dense kernels are written here, so the only dependency is Lodestar.Abstractions.</Description>
    <PackageTags>svd;truncated-svd;lsa;nmf;decomposition;dimensionality-reduction;topic-model;sparse;lodestar</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Lodestar.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Lodestar.Decomposition.Tests" />
    <!-- Same suite, replayed against the netstandard2.0 build. -->
    <InternalsVisibleTo Include="Lodestar.Decomposition.NetStandard.Tests" />
  </ItemGroup>

</Project>
```

`src/Lodestar.Decomposition/Internal/GaussianSampler.cs`:

```csharp
namespace Lodestar.Decomposition.Internal;

/// <summary>Standard normal draws from an <see cref="int"/> seed, reproducible everywhere.</summary>
/// <remarks>
/// <see cref="Random"/> is not the answer: its algorithm changed in .NET 6, so the same seed
/// gives different numbers on .NET Framework and on net10.0 — and this package ships to both.
/// A seed reproduces a run of <em>this</em> library and nothing else, which is why the oracle
/// corpora pass Ω explicitly rather than seeding.
/// </remarks>
internal sealed class GaussianSampler
{
    private ulong _state;

    internal GaussianSampler(int seed) => _state = unchecked((ulong)seed + 0x9E3779B97F4A7C15UL);

    /// <summary>Draws a row-major block of independent standard normals.</summary>
    internal double[] Normal(int rows, int columns)
    {
        if (rows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), rows, "A block has at least one row.");
        }
        if (columns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), columns, "A block has at least one column.");
        }

        double[] block = new double[checked(rows * columns)];
        for (int i = 0; i < block.Length; i += 2)
        {
            // Box–Muller consumes two uniforms and yields two normals; the second is dropped
            // only when the block has an odd length.
            (double first, double second) = NextPair();
            block[i] = first;
            if (i + 1 < block.Length)
            {
                block[i + 1] = second;
            }
        }
        return block;
    }

    private (double First, double Second) NextPair()
    {
        // Radius zero would send Log to -infinity, so the uniform is drawn on (0, 1].
        double radius = Math.Sqrt(-2.0 * Math.Log(NextUnitInterval()));
        double angle = 2.0 * Math.PI * NextUnitInterval();
        return (radius * Math.Cos(angle), radius * Math.Sin(angle));
    }

    /// <summary>A uniform on <c>(0, 1]</c> — the 53 significant bits of a double.</summary>
    private double NextUnitInterval() => ((NextState() >> 11) + 1) * (1.0 / 9007199254740992.0);

    /// <summary>SplitMix64, whose whole state is one addition and three mixes.</summary>
    private ulong NextState()
    {
        unchecked
        {
            _state += 0x9E3779B97F4A7C15UL;
            ulong z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
```

- [ ] **Step 4: Create both test projects**

`tests/Lodestar.Decomposition.Tests/Lodestar.Decomposition.Tests.csproj` — copy
`tests/Lodestar.Conformal.Tests/Lodestar.Conformal.Tests.csproj` verbatim and change exactly two
things: the `ProjectReference` path to `../../src/Lodestar.Decomposition/Lodestar.Decomposition.csproj`,
and the reference-page glob to `../../docs/reference/decomposition/**/*.md`. Leave the oracle,
`wiki-map.json`, `docs/**/*.md` and `../Shared/ReferenceDocumentation.cs` item groups exactly as
they are.

`tests/Lodestar.Decomposition.NetStandard.Tests/Lodestar.Decomposition.NetStandard.Tests.csproj` —
copy `tests/Lodestar.Conformal.NetStandard.Tests/Lodestar.Conformal.NetStandard.Tests.csproj`
verbatim and change: `<AssemblyName>Lodestar.Decomposition.NetStandard.Tests</AssemblyName>`,
`<RootNamespace>Lodestar.Decomposition.Tests</RootNamespace>`, the `ProjectReference` path (keeping
`SetTargetFramework="TargetFramework=netstandard2.0"`), the linked `Compile Include` glob to
`../Lodestar.Decomposition.Tests/**/*.cs`, and the reference-page glob as above.

`tests/Lodestar.Decomposition.Tests/OracleLoader.cs`:

```csharp
using System.Text.Json;

namespace Lodestar.Decomposition.Tests;

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

- [ ] **Step 5: Put the package into the solution and into every list that names the others**

`Lodestar.slnx` — one `<Project>` under `/src/` and two under `/tests/`, in the existing
alphabetical order:

```xml
    <Project Path="src/Lodestar.Decomposition/Lodestar.Decomposition.csproj" />
```

```xml
    <Project Path="tests/Lodestar.Decomposition.NetStandard.Tests/Lodestar.Decomposition.NetStandard.Tests.csproj" />
    <Project Path="tests/Lodestar.Decomposition.Tests/Lodestar.Decomposition.Tests.csproj" />
```

`.github/workflows/ci.yml` — four loops at lines 141, 152, 216 and 251 read
`for proj in src/Lodestar.Abstractions src/Lodestar.Text src/Lodestar.Embeddings src/Lodestar.Fuzzy src/Lodestar.Metrics src/Lodestar.Conformal; do`.
Append `src/Lodestar.Decomposition` to each of the four, separated by a space.

`.github/workflows/sonarcloud.yml` — the same append on the one pack loop.

`.github/workflows/wiki.yml` — append `Lodestar.Decomposition` to the released-versions loop.

`.github/workflows/release.yml` — the allow-list case becomes
`Lodestar.Abstractions|Lodestar.Text|Lodestar.Embeddings|Lodestar.Fuzzy|Lodestar.Metrics|Lodestar.Conformal|Lodestar.Decomposition) ;;`

`.github/workflows/release-nuget-org.yml` — add a `- Lodestar.Decomposition` entry to the
`package` choice list.

`samples/Lodestar.Sample/Lodestar.Sample.csproj` **and**
`samples/Lodestar.DocSnippets/Lodestar.DocSnippets.csproj` — each gains one import and one
reference:

```xml
  <Import Project="../../src/Lodestar.Decomposition/Version.props" />
```

```xml
    <PackageReference Include="Lodestar.Decomposition" Version="$(LodestarDecompositionVersion)" />
```

- [ ] **Step 6: Teach the three checkers about the new edge**

`tools/check_nuspec_dependencies.py` — add the id constant beside the others and the expected
graph beside `CONFORMAL`'s:

```python
DECOMPOSITION = "Lodestar.Decomposition"
```

```python
    DECOMPOSITION: {
        # The one edge of this package, and the reason Lodestar.Abstractions exists:
        # CsrMatrix and its two dense-block products, with no Lodestar.Text behind them.
        NET: {ABSTRACTIONS: ABSTRACTIONS_FLOOR},
        NETSTANDARD: {ABSTRACTIONS: ABSTRACTIONS_FLOOR, **POLYFILLS},
    },
```

Also extend the module docstring's sentence about the inter-package edges: there are now three,
not two — `Lodestar.Fuzzy` on `Lodestar.Text`, and both `Lodestar.Text` and
`Lodestar.Decomposition` on `Lodestar.Abstractions`.

`tools/check_version_floor.py` — `Floor` carries one `required_by`, and `Lodestar.Abstractions` is
now floored by two dependents. Widen the field rather than adding a duplicate row, so the message
names both:

```python
@dataclass(frozen=True)
class Floor:
    """One edge: a package, the dependents that floor it, and where each number lives."""

    package: str
    version_element: str
    floor_constant: str
    required_by: tuple[str, ...]

    @property
    def version_props(self) -> pathlib.Path:
        """Where the package declares what it is."""
        return ROOT / "src" / self.package / "Version.props"

    @property
    def dependents(self) -> str:
        """The dependents, for a message a reader can act on."""
        return " and ".join(self.required_by)


FLOORS = (
    Floor("Lodestar.Text", "LodestarTextVersion", "TEXT_FLOOR", ("Lodestar.Fuzzy",)),
    Floor("Lodestar.Abstractions", "LodestarAbstractionsVersion", "ABSTRACTIONS_FLOOR",
          ("Lodestar.Text", "Lodestar.Decomposition")),
)
```

Then replace the three `{floor.required_by}` interpolations in `check` and the one in `main`'s
`ok` line with `{floor.dependents}`.

`tools/check_sample_coverage.py` — `CONVERTED` gains the package, which is born under decision
0041 rather than exempt from it:

```python
CONVERTED = ["Lodestar.Text", "Lodestar.Conformal", "Lodestar.Abstractions", "Lodestar.Decomposition"]
```

`docs/wiki-map.json` — the package is described now, with an empty `covered`, because
`build_wiki.py` looks a tagged package up unconditionally and both keys are read. `covered` is
filled in Task 5, when there is a public namespace to cover, and
`docs/guides/decomposition.md` joins `pages` in Task 9, when it exists — `build_wiki.py`
hard-fails a literal path it cannot find, and `tools/tests/test_build_wiki.py` catches it:

```json
    "Lodestar.Decomposition": {
      "wiki": "Decomposition",
      "pages": [
        "docs/reference/decomposition/*.md",
        "docs/reference/decomposition/*/*.md"
      ],
      "covered": {}
    },
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release`
Expected: PASS, 7 tests. **Read the count, not the colour.**

Run: `dotnet test tests/Lodestar.Decomposition.NetStandard.Tests -c Release`
Expected: PASS, 7 tests.

Run: `dotnet build Lodestar.slnx -c Release`
Expected: no warnings (they are errors).

Run: `python3 tools/check_version_floor.py && python3 tools/check_sample_coverage.py`
Expected: both `ok`. The floor line now reads `Lodestar.Text and Lodestar.Decomposition floors at
0.1.1`.

- [ ] **Step 8: Commit**

```bash
git add src/Lodestar.Decomposition tests/Lodestar.Decomposition.Tests \
  tests/Lodestar.Decomposition.NetStandard.Tests Lodestar.slnx docs/wiki-map.json \
  tools/check_nuspec_dependencies.py tools/check_version_floor.py \
  tools/check_sample_coverage.py .github/workflows samples/Lodestar.Sample/Lodestar.Sample.csproj \
  samples/Lodestar.DocSnippets/Lodestar.DocSnippets.csproj
git commit -m "Open Lodestar.Decomposition on the one edge it is allowed

Eight lists name the packages one by one -- four loops in ci.yml, the sonarcloud
pack, the wiki's released versions, the release allow-list and the nuget.org
dispatch matrix. A package missing from any of them builds and is then not
shipped, checked or documented.

check_version_floor's Floor carried one dependent per edge, and
Lodestar.Abstractions now has two. Widening the field beats a second row naming
the same three numbers: a duplicate row would report the same drift twice and
let the two copies disagree.

The generator is SplitMix64 with Box-Muller rather than System.Random, whose
algorithm changed in .NET 6 -- the same seed gives different numbers on .NET
Framework, and this package ships to both.

Part of #440."
```

---

## Task 2: Thin Householder QR

**Files:**

- Create: `src/Lodestar.Decomposition/Internal/DenseBlock.cs`
- Create: `src/Lodestar.Decomposition/Internal/HouseholderQr.cs`
- Create: `tests/Lodestar.Decomposition.Tests/HouseholderQrTests.cs`
- Create: `tests/oracles/decomposition_qr.json`
- Modify: `tools/generate_oracles.py`

**Interfaces:**

- Consumes: nothing from earlier tasks.
- Produces:
  - `internal static class DenseBlock` with
    `internal static double[] Transpose(ReadOnlySpan<double> block, int rows, int columns)` and
    `internal static double ColumnNorm(ReadOnlySpan<double> block, int rows, int columns, int column)`.
  - `internal static class HouseholderQr` with
    `internal static (double[] Q, double[] R) Decompose(ReadOnlySpan<double> a, int rows, int columns)`
    — `a` is row-major `rows × columns` with `rows >= columns`; `Q` is row-major
    `rows × columns` with orthonormal columns, `R` is row-major `columns × columns` and upper
    triangular. This is `scipy.linalg.qr(a, mode="economic")`'s shape, though **not necessarily its
    signs**, which the composed algorithm is invariant to.

- [ ] **Step 1: Write the oracle generator**

In `tools/generate_oracles.py`, beside the other `generate_*` functions and after the sparse-matmul
block, add:

```python
# --- Dense kernels for Lodestar.Decomposition (#440) -----------------------

# Reused by three corpora below. S1192 counts a repeated JSON key like any other
# literal, and these are written once for that reason as much as for clarity.
MATRIX_KEY = "matrix"
ROWS_KEY = "rows"
COLUMNS_KEY = "columns"
FULL_RANK_KEY = "full_rank"


def _dense_fixtures() -> list[dict]:
    """Tall-and-skinny blocks, the shape a range finder actually produces."""
    rng = SeededRandom(SEED + 44300)
    shapes = [(6, 3), (12, 4), (25, 10), (40, 10), (9, 9), (5, 1)]
    fixtures = []
    for rows, columns in shapes:
        values = [rng.gauss(0.0, 1.0) for _ in range(rows * columns)]
        fixtures.append(
            {ROWS_KEY: rows, COLUMNS_KEY: columns, MATRIX_KEY: values, FULL_RANK_KEY: True})
    # Column 1 repeats column 0: past the vanished pivot the factors stop being
    # basis-independent, so full_rank is False and the factor comparisons skip it.
    rows, columns = 8, 3
    base = [rng.gauss(0.0, 1.0) for _ in range(rows)]
    tail = [rng.gauss(0.0, 1.0) for _ in range(rows)]
    deficient = []
    for i in range(rows):
        deficient.extend([base[i], base[i], tail[i]])
    fixtures.append(
        {ROWS_KEY: rows, COLUMNS_KEY: columns, MATRIX_KEY: deficient, FULL_RANK_KEY: False})
    return fixtures


def generate_decomposition_qr() -> dict:
    """Economic QR, against scipy (#440).

    The factors are unique only up to a per-column sign, so the corpus freezes what
    is actually invariant: that Q has orthonormal columns, that R is upper
    triangular, and that Q @ R reproduces the input. scipy's own factors ride along
    so a divergence can be looked at, never asserted on.
    """
    from scipy import linalg

    cases = []
    for fixture in _dense_fixtures():
        rows, columns = fixture[ROWS_KEY], fixture[COLUMNS_KEY]
        a = np.array(fixture[MATRIX_KEY]).reshape(rows, columns)
        q, r = linalg.qr(a, mode="economic")
        cases.append({
            **fixture,
            "scipy_q": q.ravel().tolist(),
            "scipy_r": r.ravel().tolist(),
        })
    return {"metadata": {"library": "scipy", "version": version("scipy"),
                         "reference_calls": ["scipy.linalg.qr"],
                         "seed": SEED, "count": len(cases), "tolerance": 1e-9},
            "cases": cases}
```

Register it in `main()`'s dispatch table, beside `"conformal.json"`:

```python
        "decomposition_qr.json": generate_decomposition_qr,
```

- [ ] **Step 2: Generate the corpus and read the generator's own exit code**

```bash
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
echo "exit: $?"
```

Expected: `exit: 0`, and `tests/oracles/decomposition_qr.json` exists. If the checkout is **not**
under `/tmp`, `/tmp` serves as the neutral directory instead. Never pipe this into `tail`.

- [ ] **Step 3: Write the failing test**

`tests/Lodestar.Decomposition.Tests/HouseholderQrTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Decomposition.Internal;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// The economic QR, against scipy. A QR is unique only up to a per-column sign, so what is
/// asserted is what the composed algorithm actually relies on: orthonormal columns, an upper
/// triangular R, and a product that reproduces the input.
/// </summary>
public sealed class HouseholderQrTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("decomposition_qr.json");

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

    private static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_product_reproduces_the_input(int index)
    {
        JsonElement c = Cases[index];
        int rows = c.GetProperty("rows").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();
        double[] a = Doubles(c, "matrix");

        (double[] q, double[] r) = HouseholderQr.Decompose(a, rows, columns);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                double acc = 0;
                for (int k = 0; k < columns; k++)
                {
                    acc += q[(i * columns) + k] * r[(k * columns) + j];
                }
                Assert.Equal(a[(i * columns) + j], acc, Tolerance);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_columns_of_q_are_orthonormal(int index)
    {
        JsonElement c = Cases[index];
        int rows = c.GetProperty("rows").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();

        (double[] q, _) = HouseholderQr.Decompose(Doubles(c, "matrix"), rows, columns);

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                double acc = 0;
                for (int k = 0; k < rows; k++)
                {
                    acc += q[(k * columns) + i] * q[(k * columns) + j];
                }
                Assert.Equal(i == j ? 1.0 : 0.0, acc, Tolerance);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void R_is_upper_triangular(int index)
    {
        JsonElement c = Cases[index];
        int rows = c.GetProperty("rows").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();

        (_, double[] r) = HouseholderQr.Decompose(Doubles(c, "matrix"), rows, columns);

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < i; j++)
            {
                Assert.Equal(0.0, r[(i * columns) + j], Tolerance);
            }
        }
    }

    /// <summary>
    /// The fixtures whose factors are basis-independent. Past a vanished pivot they are not:
    /// the reflector is built from rounding noise, and scipy's own |diag(R)| moves by 0.03
    /// under a 1e-14 perturbation of the duplicate column. The three theories above still
    /// cover the rank-deficient block, which is what proves the zero-column guard.
    /// </summary>
    public static TheoryData<int> FullRankIndices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            if (Cases[i].GetProperty("full_rank").GetBoolean())
            {
                data.Add(i);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(FullRankIndices))]
    public void The_singular_values_of_r_match_scipy(int index)
    {
        // R differs from scipy's by a per-column sign; |diag| does not, and it is the
        // cheapest thing that would catch a pivot or a scaling error.
        JsonElement c = Cases[index];
        int columns = c.GetProperty("columns").GetInt32();
        double[] expected = Doubles(c, "scipy_r");

        (_, double[] r) = HouseholderQr.Decompose(
            Doubles(c, "matrix"), c.GetProperty("rows").GetInt32(), columns);

        for (int i = 0; i < columns; i++)
        {
            Assert.Equal(
                Math.Abs(expected[(i * columns) + i]), Math.Abs(r[(i * columns) + i]), Tolerance);
        }
    }

    [Fact]
    public void A_wide_block_is_refused()
    {
        Assert.Throws<ArgumentException>(() => HouseholderQr.Decompose(new double[6], 2, 3));
    }
}
```

- [ ] **Step 4: Run the test to verify it fails**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release --filter "FullyQualifiedName~HouseholderQr"`
Expected: FAIL — `HouseholderQr` does not exist.

- [ ] **Step 5: Write the implementation**

`src/Lodestar.Decomposition/Internal/DenseBlock.cs`:

```csharp
namespace Lodestar.Decomposition.Internal;

/// <summary>The row-major helpers every kernel in this package shares.</summary>
/// <remarks>
/// A block of <c>r</c> rows and <c>c</c> columns is a <c>double[r * c]</c> where element
/// <c>(i, j)</c> lives at <c>i * c + j</c> — the layout <c>CsrMatrix</c>'s dense-block products
/// already take and return, so nothing in this package ever transposes to talk to it.
/// </remarks>
internal static class DenseBlock
{
    /// <summary>Transposes a row-major block into another row-major block.</summary>
    internal static double[] Transpose(ReadOnlySpan<double> block, int rows, int columns)
    {
        double[] result = new double[block.Length];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                result[(j * rows) + i] = block[(i * columns) + j];
            }
        }
        return result;
    }

    /// <summary>The Euclidean norm of one column.</summary>
    internal static double ColumnNorm(ReadOnlySpan<double> block, int rows, int columns, int column)
    {
        double sum = 0;
        for (int i = 0; i < rows; i++)
        {
            double value = block[(i * columns) + column];
            sum += value * value;
        }
        return Math.Sqrt(sum);
    }
}
```

`src/Lodestar.Decomposition/Internal/HouseholderQr.cs`:

```csharp
namespace Lodestar.Decomposition.Internal;

/// <summary>The economic QR of a tall block, by Householder reflections.</summary>
/// <remarks>
/// <para>
/// Gram–Schmidt is shorter and loses orthogonality on exactly the blocks this package produces —
/// a range finder's columns are nearly parallel by construction, which is the case classical
/// Gram–Schmidt is famously bad at. Householder is unconditionally stable and costs the same.
/// </para>
/// <para>
/// The signs of the factors are not LAPACK's, and do not need to be: a per-column sign flip
/// <c>Q → QD</c> leaves <c>B = QᵀA</c> as <c>DB</c>, whose SVD returns <c>DŨ</c>, and
/// <c>QD · DŨ = QŨ</c>. What comes out of the composed algorithm is invariant.
/// </para>
/// </remarks>
internal static class HouseholderQr
{
    /// <summary>Factors a row-major <c>rows × columns</c> block with <c>rows >= columns</c>.</summary>
    internal static (double[] Q, double[] R) Decompose(ReadOnlySpan<double> a, int rows, int columns)
    {
        if (rows < columns)
        {
            throw new ArgumentException(
                $"An economic QR needs at least as many rows as columns; got {rows} × {columns}.",
                nameof(a));
        }
        if (a.Length != checked(rows * columns))
        {
            throw new ArgumentException(
                $"Block length {a.Length} != {rows} × {columns}.", nameof(a));
        }

        // Work in place on a copy: the reflectors are applied to it, and what is left
        // above the diagonal is R.
        double[] work = a.ToArray();
        double[][] reflectors = new double[columns][];

        for (int k = 0; k < columns; k++)
        {
            double[] v = new double[rows - k];
            double norm = 0;
            for (int i = k; i < rows; i++)
            {
                double value = work[(i * columns) + k];
                v[i - k] = value;
                norm += value * value;
            }
            norm = Math.Sqrt(norm);

            // A zero column is already reduced. Skipping it keeps a rank-deficient block
            // finite instead of dividing by zero and filling R with NaN.
            if (norm == 0)
            {
                reflectors[k] = v;
                continue;
            }

            double alpha = v[0] >= 0 ? -norm : norm;
            v[0] -= alpha;
            double vNorm = 0;
            foreach (double value in v)
            {
                vNorm += value * value;
            }
            if (vNorm == 0)
            {
                reflectors[k] = v;
                continue;
            }

            reflectors[k] = v;
            ApplyLeft(work, rows, columns, k, v, vNorm);
        }

        double[] r = new double[columns * columns];
        for (int i = 0; i < columns; i++)
        {
            for (int j = i; j < columns; j++)
            {
                r[(i * columns) + j] = work[(i * columns) + j];
            }
        }

        // Q is the reflectors applied, in reverse, to the first `columns` columns of the
        // identity — never formed as a rows × rows matrix, which is the whole point of "thin".
        double[] q = new double[rows * columns];
        for (int j = 0; j < columns; j++)
        {
            q[(j * columns) + j] = 1.0;
        }
        for (int k = columns - 1; k >= 0; k--)
        {
            double[] v = reflectors[k];
            double vNorm = 0;
            foreach (double value in v)
            {
                vNorm += value * value;
            }
            if (vNorm != 0)
            {
                ApplyLeft(q, rows, columns, k, v, vNorm);
            }
        }
        return (q, r);
    }

    /// <summary>Applies <c>I - 2vvᵀ/vᵀv</c> to the trailing rows of every column.</summary>
    private static void ApplyLeft(
        double[] block, int rows, int columns, int from, double[] v, double vNorm)
    {
        for (int j = 0; j < columns; j++)
        {
            double dot = 0;
            for (int i = from; i < rows; i++)
            {
                dot += v[i - from] * block[(i * columns) + j];
            }
            double scale = 2.0 * dot / vNorm;
            for (int i = from; i < rows; i++)
            {
                block[(i * columns) + j] -= scale * v[i - from];
            }
        }
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release --filter "FullyQualifiedName~HouseholderQr"`
Expected: PASS, 28 tests — 7 cases × 3 basis-independent theories, 6 full-rank cases × the
diagonal theory, plus the wide-block fact. **Read the count.**

Run: `dotnet test tests/Lodestar.Decomposition.NetStandard.Tests -c Release --filter "FullyQualifiedName~HouseholderQr"`
Expected: the same count.

- [ ] **Step 7: Commit**

```bash
git add src/Lodestar.Decomposition/Internal tests/Lodestar.Decomposition.Tests \
  tests/oracles/decomposition_qr.json tools/generate_oracles.py
git commit -m "Factor a tall block by Householder reflections

Gram-Schmidt is shorter and loses orthogonality on exactly the blocks a range
finder produces -- nearly parallel columns are the case it is famously bad at.

The corpus asserts what the composed algorithm relies on rather than scipy's
factors: orthonormal columns, an upper triangular R, and a product that
reproduces the input. A QR is unique only up to a per-column sign, so asserting
the factors would freeze a convention nothing depends on. scipy's own factors
ride along so a divergence can be looked at.

A zero column is left alone instead of dividing by its norm, which is what keeps
a rank-deficient block finite; the corpus carries one.

Part of #440."
```

---

## Task 3: LU with partial pivoting

**Files:**

- Create: `src/Lodestar.Decomposition/Internal/PartialPivotLu.cs`
- Create: `tests/Lodestar.Decomposition.Tests/PartialPivotLuTests.cs`
- Create: `tests/oracles/decomposition_lu.json`
- Modify: `tools/generate_oracles.py`

**Interfaces:**

- Consumes: `_dense_fixtures()` from Task 2's generator block.
- Produces: `internal static class PartialPivotLu` with
  `internal static double[] PermutedLower(ReadOnlySpan<double> a, int rows, int columns)` —
  `a` is row-major `rows × columns` with `rows >= columns`; the return is the row-major
  `rows × columns` block `scipy.linalg.lu(a, permute_l=True)` returns as its first factor, which is
  `P L`. That is the whole of what `power_iteration_normalizer="LU"` uses: scikit-learn writes
  `Q, _ = linalg.lu(A @ Q, permute_l=True)` and discards `U`.

- [ ] **Step 1: Write the oracle generator**

Append to the dense-kernel block in `tools/generate_oracles.py`:

```python
def generate_decomposition_lu() -> dict:
    """LU with partial pivoting, against scipy (#440).

    ``permute_l=True`` is the form scikit-learn's power iteration uses: it asks for
    ``P @ L`` and throws ``U`` away. That product is unique for a full-rank block,
    so unlike the QR corpus this one asserts the factor itself as well as the
    reconstruction.
    """
    from scipy import linalg

    cases = []
    for fixture in _dense_fixtures():
        rows, columns = fixture[ROWS_KEY], fixture[COLUMNS_KEY]
        a = np.array(fixture[MATRIX_KEY]).reshape(rows, columns)
        pl, u = linalg.lu(a, permute_l=True)
        cases.append({
            **fixture,
            "permuted_lower": pl.ravel().tolist(),
            "upper": u.ravel().tolist(),
        })
    return {"metadata": {"library": "scipy", "version": version("scipy"),
                         "reference_calls": ["scipy.linalg.lu"],
                         "seed": SEED, "count": len(cases), "tolerance": 1e-9},
            "cases": cases}
```

Register it in `main()`:

```python
        "decomposition_lu.json": generate_decomposition_lu,
```

- [ ] **Step 2: Generate the corpus and read the generator's own exit code**

```bash
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
echo "exit: $?"
```

Expected: `exit: 0`, and `tests/oracles/decomposition_lu.json` exists.

- [ ] **Step 3: Write the failing test**

`tests/Lodestar.Decomposition.Tests/PartialPivotLuTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Decomposition.Internal;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// <c>scipy.linalg.lu(permute_l=True)</c>'s first factor, which is the whole of what the
/// <c>LU</c> power-iteration normalizer uses.
/// </summary>
public sealed class PartialPivotLuTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("decomposition_lu.json");

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

    private static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    /// <summary>Only the full-rank fixtures: partial pivoting has a tie to break on the other,
    /// and which way it falls is not a property either implementation owes the other.</summary>
    public static TheoryData<int> FullRankIndices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            if (Cases[i].GetProperty("full_rank").GetBoolean())
            {
                data.Add(i);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(FullRankIndices))]
    public void The_permuted_lower_factor_matches_scipy(int index)
    {
        JsonElement c = Cases[index];

        double[] pl = PartialPivotLu.PermutedLower(
            Doubles(c, "matrix"), c.GetProperty("rows").GetInt32(),
            c.GetProperty("columns").GetInt32());

        double[] expected = Doubles(c, "permuted_lower");
        Assert.Equal(expected.Length, pl.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], pl[i], Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_product_with_scipys_upper_reproduces_the_input(int index)
    {
        // Independent of the factor assertion above: it would still catch a P L that is
        // self-consistent and wrong, because U comes from scipy rather than from here.
        JsonElement c = Cases[index];
        int rows = c.GetProperty("rows").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();
        double[] a = Doubles(c, "matrix");
        double[] u = Doubles(c, "upper");

        double[] pl = PartialPivotLu.PermutedLower(a, rows, columns);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                double acc = 0;
                for (int k = 0; k < columns; k++)
                {
                    acc += pl[(i * columns) + k] * u[(k * columns) + j];
                }
                Assert.Equal(a[(i * columns) + j], acc, Tolerance);
            }
        }
    }

    [Fact]
    public void A_wide_block_is_refused()
    {
        Assert.Throws<ArgumentException>(() => PartialPivotLu.PermutedLower(new double[6], 2, 3));
    }
}
```

- [ ] **Step 4: Run the test to verify it fails**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release --filter "FullyQualifiedName~PartialPivotLu"`
Expected: FAIL — `PartialPivotLu` does not exist.

- [ ] **Step 5: Write the implementation**

`src/Lodestar.Decomposition/Internal/PartialPivotLu.cs`:

```csharp
namespace Lodestar.Decomposition.Internal;

/// <summary>Gaussian elimination with partial pivoting, keeping only <c>P L</c>.</summary>
/// <remarks>
/// <para>
/// This is the <c>LU</c> power-iteration normalizer, and it is scikit-learn's default: the
/// <c>auto</c> rule resolves to <c>LU</c> whenever there are more than two power iterations, and
/// <c>TruncatedSVD</c> asks for five. It is not a normalizer in the orthogonal sense — the columns
/// of <c>P L</c> are not orthonormal — but it is cheaper than a QR and enough to stop the power
/// iteration collapsing onto the leading singular vector.
/// </para>
/// <para>
/// <c>U</c> is computed and dropped: forming it costs nothing beyond what the elimination already
/// wrote, and returning it would invite a caller to use a factorization this package never needs.
/// </para>
/// </remarks>
internal static class PartialPivotLu
{
    /// <summary>Returns <c>P L</c> for a row-major <c>rows × columns</c> block, <c>rows >= columns</c>.</summary>
    internal static double[] PermutedLower(ReadOnlySpan<double> a, int rows, int columns)
    {
        if (rows < columns)
        {
            throw new ArgumentException(
                $"This factorization needs at least as many rows as columns; got {rows} × {columns}.",
                nameof(a));
        }
        if (a.Length != checked(rows * columns))
        {
            throw new ArgumentException(
                $"Block length {a.Length} != {rows} × {columns}.", nameof(a));
        }

        double[] work = a.ToArray();
        int[] permutation = new int[rows];
        for (int i = 0; i < rows; i++)
        {
            permutation[i] = i;
        }

        for (int k = 0; k < columns; k++)
        {
            int pivot = k;
            double best = Math.Abs(work[(k * columns) + k]);
            for (int i = k + 1; i < rows; i++)
            {
                double candidate = Math.Abs(work[(i * columns) + k]);
                if (candidate > best)
                {
                    best = candidate;
                    pivot = i;
                }
            }

            if (pivot != k)
            {
                SwapRows(work, columns, k, pivot);
                (permutation[k], permutation[pivot]) = (permutation[pivot], permutation[k]);
            }

            double head = work[(k * columns) + k];
            // A zero pivot means the column is already eliminated; dividing by it would
            // fill the factor with NaN on a rank-deficient block, which the corpus carries.
            if (head == 0)
            {
                continue;
            }

            for (int i = k + 1; i < rows; i++)
            {
                double factor = work[(i * columns) + k] / head;
                work[(i * columns) + k] = factor;
                for (int j = k + 1; j < columns; j++)
                {
                    work[(i * columns) + j] -= factor * work[(k * columns) + j];
                }
            }
        }

        // L is unit lower triangular in the eliminated block; P L puts each row back where
        // the pivoting took it from, which is what scipy's permute_l=True returns.
        double[] result = new double[rows * columns];
        for (int i = 0; i < rows; i++)
        {
            int target = permutation[i] * columns;
            for (int j = 0; j < columns && j < i; j++)
            {
                result[target + j] = work[(i * columns) + j];
            }
            if (i < columns)
            {
                result[target + i] = 1.0;
            }
        }
        return result;
    }

    private static void SwapRows(double[] block, int columns, int first, int second)
    {
        for (int j = 0; j < columns; j++)
        {
            (block[(first * columns) + j], block[(second * columns) + j]) =
                (block[(second * columns) + j], block[(first * columns) + j]);
        }
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release --filter "FullyQualifiedName~PartialPivotLu"`
Expected: PASS, 14 tests — 6 full-rank cases × the factor theory, 7 cases × the reconstruction
theory, plus the wide-block fact.

The reconstruction theory runs on the rank-deficient fixture too, and must: it is what proves the
zero-pivot guard leaves the factor finite rather than filling it with NaN. Only the factor-level
comparison skips it, for the reason Task 2 measured — past a vanished pivot the factors stop being
basis-independent, and scipy's own answer moves under a perturbation far below the tolerance. Do
not weaken either assertion to get green.

Run: `dotnet test tests/Lodestar.Decomposition.NetStandard.Tests -c Release --filter "FullyQualifiedName~PartialPivotLu"`
Expected: the same count.

- [ ] **Step 7: Commit**

```bash
git add src/Lodestar.Decomposition/Internal/PartialPivotLu.cs \
  tests/Lodestar.Decomposition.Tests/PartialPivotLuTests.cs \
  tests/oracles/decomposition_lu.json tools/generate_oracles.py
git commit -m "Normalize a power iteration the way scikit-learn's default does

TruncatedSVD asks for five power iterations and the auto rule resolves anything
above two to LU, so this is the kernel the default call actually goes through --
shipping only QR would mean the default disagrees with scikit-learn's default.

permute_l=True is the form the iteration uses: it takes P L and throws U away.
That product is unique for a full-rank block, so the corpus asserts the factor
itself, and separately multiplies it by scipy's U -- a P L that is
self-consistent and wrong fails the second check and not the first.

Part of #440."
```

---

## Task 4: One-sided Jacobi SVD

**Files:**

- Create: `src/Lodestar.Decomposition/Internal/JacobiSvd.cs`
- Create: `tests/Lodestar.Decomposition.Tests/JacobiSvdTests.cs`
- Create: `tests/oracles/decomposition_svd.json` (the `dense` half; Task 5 adds `randomized`)
- Modify: `tools/generate_oracles.py`

**Interfaces:**

- Consumes: `DenseBlock.Transpose`, `DenseBlock.ColumnNorm` from Task 2.
- Produces: `internal static class JacobiSvd` with
  `internal static (double[] U, double[] S, double[] Vt) Decompose(ReadOnlySpan<double> a, int rows, int columns)`
  — `a` is row-major `rows × columns` of any shape. With `k = min(rows, columns)`: `U` is row-major
  `rows × k`, `S` is `double[k]` in **descending** order, `Vt` is row-major `k × columns`. This is
  `scipy.linalg.svd(a, full_matrices=False)`'s shape, with the same singular values and, up to a
  per-triplet sign, the same vectors.

- [ ] **Step 1: Write the oracle generator**

Append to the dense-kernel block in `tools/generate_oracles.py`:

```python
def _dense_svd_fixtures() -> list[dict]:
    """Both orientations, plus the wide-and-short shape B actually has."""
    rng = SeededRandom(SEED + 44400)
    shapes = [(6, 3), (3, 6), (10, 10), (14, 4), (4, 14), (1, 5), (5, 1)]
    fixtures = []
    for rows, columns in shapes:
        values = [rng.gauss(0.0, 1.0) for _ in range(rows * columns)]
        fixtures.append({ROWS_KEY: rows, COLUMNS_KEY: columns, MATRIX_KEY: values})
    return fixtures


def _dense_svd_cases() -> list[dict]:
    """The dense factorization on its own, so a failure in the composed algorithm
    has somewhere smaller to land."""
    from scipy import linalg

    cases = []
    for fixture in _dense_svd_fixtures():
        rows, columns = fixture[ROWS_KEY], fixture[COLUMNS_KEY]
        a = np.array(fixture[MATRIX_KEY]).reshape(rows, columns)
        u, s, vt = linalg.svd(a, full_matrices=False)
        cases.append({
            **fixture,
            "singular_values": s.tolist(),
            "scipy_u": u.ravel().tolist(),
            "scipy_vt": vt.ravel().tolist(),
        })
    return cases


def generate_decomposition_svd() -> dict:
    """The dense SVD, and (from Task 5) randomized_svd on top of it (#440)."""
    dense = _dense_svd_cases()
    return {"metadata": {"library": "scipy", "version": version("scipy"),
                         "reference_calls": ["scipy.linalg.svd"],
                         "seed": SEED, "count": len(dense), "tolerance": 1e-9},
            "dense": dense}
```

Register it in `main()`:

```python
        "decomposition_svd.json": generate_decomposition_svd,
```

- [ ] **Step 2: Generate the corpus and read the generator's own exit code**

```bash
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
echo "exit: $?"
```

Expected: `exit: 0`, and `tests/oracles/decomposition_svd.json` holds a `dense` array of 7 cases.

- [ ] **Step 3: Write the failing test**

`tests/Lodestar.Decomposition.Tests/JacobiSvdTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Decomposition.Internal;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// The dense factorization the whole method exists to reach, on its own and against scipy.
/// Singular values are unique and asserted directly; the vectors are unique only up to a
/// per-triplet sign, so what is asserted of them is the reconstruction and orthonormality.
/// </summary>
public sealed class JacobiSvdTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("decomposition_svd.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("dense").EnumerateArray()];

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }
        return data;
    }

    private static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_singular_values_match_scipy(int index)
    {
        JsonElement c = Cases[index];

        (_, double[] s, _) = JacobiSvd.Decompose(
            Doubles(c, "matrix"), c.GetProperty("rows").GetInt32(),
            c.GetProperty("columns").GetInt32());

        double[] expected = Doubles(c, "singular_values");
        Assert.Equal(expected.Length, s.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], s[i], Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_factors_reproduce_the_input(int index)
    {
        JsonElement c = Cases[index];
        int rows = c.GetProperty("rows").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();
        double[] a = Doubles(c, "matrix");
        int rank = Math.Min(rows, columns);

        (double[] u, double[] s, double[] vt) = JacobiSvd.Decompose(a, rows, columns);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                double acc = 0;
                for (int k = 0; k < rank; k++)
                {
                    acc += u[(i * rank) + k] * s[k] * vt[(k * columns) + j];
                }
                Assert.Equal(a[(i * columns) + j], acc, Tolerance);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_singular_values_come_out_in_descending_order(int index)
    {
        JsonElement c = Cases[index];

        (_, double[] s, _) = JacobiSvd.Decompose(
            Doubles(c, "matrix"), c.GetProperty("rows").GetInt32(),
            c.GetProperty("columns").GetInt32());

        for (int i = 1; i < s.Length; i++)
        {
            Assert.True(s[i - 1] >= s[i], $"s[{i - 1}] = {s[i - 1]} < s[{i}] = {s[i]}");
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_rows_of_vt_are_orthonormal(int index)
    {
        JsonElement c = Cases[index];
        int rows = c.GetProperty("rows").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();
        int rank = Math.Min(rows, columns);

        (_, _, double[] vt) = JacobiSvd.Decompose(Doubles(c, "matrix"), rows, columns);

        for (int i = 0; i < rank; i++)
        {
            for (int j = 0; j < rank; j++)
            {
                double acc = 0;
                for (int k = 0; k < columns; k++)
                {
                    acc += vt[(i * columns) + k] * vt[(j * columns) + k];
                }
                Assert.Equal(i == j ? 1.0 : 0.0, acc, Tolerance);
            }
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it fails**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release --filter "FullyQualifiedName~JacobiSvd"`
Expected: FAIL — `JacobiSvd` does not exist.

- [ ] **Step 5: Write the implementation**

`src/Lodestar.Decomposition/Internal/JacobiSvd.cs`:

```csharp
namespace Lodestar.Decomposition.Internal;

/// <summary>The SVD of a dense block, by one-sided Jacobi rotations.</summary>
/// <remarks>
/// <para>
/// One-sided Jacobi orthogonalizes the columns of a tall block in place by plane rotations; the
/// column norms it converges to are the singular values, the normalized columns are <c>U</c>, and
/// the accumulated rotations are <c>V</c>. It is the accurate method for exactly this shape, and
/// it needs no bidiagonalization, no shifts and no deflation logic.
/// </para>
/// <para>
/// A wide block is factored through its transpose, which swaps the roles of <c>U</c> and
/// <c>V</c> — the block this package actually reaches here is <c>B = QᵀA</c>, wide and short.
/// </para>
/// </remarks>
internal static class JacobiSvd
{
    // Rotating a pair whose off-diagonal is already at the rounding floor changes nothing
    // and costs a sweep, so the sweep stops when every pair is below it.
    private const double Threshold = 1e-15;
    private const int MaximumSweeps = 60;

    /// <summary>Factors a row-major <c>rows × columns</c> block of any shape.</summary>
    internal static (double[] U, double[] S, double[] Vt) Decompose(
        ReadOnlySpan<double> a, int rows, int columns)
    {
        if (a.Length != checked(rows * columns))
        {
            throw new ArgumentException(
                $"Block length {a.Length} != {rows} × {columns}.", nameof(a));
        }

        if (rows < columns)
        {
            // Aᵀ = U₁ Σ V₁ᵀ gives A = V₁ Σ U₁ᵀ: the two factors trade places.
            (double[] wideU, double[] wideS, double[] wideVt) =
                Decompose(DenseBlock.Transpose(a, rows, columns), columns, rows);
            return (DenseBlock.Transpose(wideVt, wideS.Length, rows),
                    wideS,
                    DenseBlock.Transpose(wideU, columns, wideS.Length));
        }

        double[] work = a.ToArray();
        double[] v = new double[columns * columns];
        for (int i = 0; i < columns; i++)
        {
            v[(i * columns) + i] = 1.0;
        }

        for (int sweep = 0; sweep < MaximumSweeps; sweep++)
        {
            bool rotated = false;
            for (int p = 0; p < columns - 1; p++)
            {
                for (int q = p + 1; q < columns; q++)
                {
                    rotated |= RotatePair(work, v, rows, columns, p, q);
                }
            }
            if (!rotated)
            {
                break;
            }
        }

        return Finish(work, v, rows, columns);
    }

    /// <summary>Orthogonalizes one pair of columns, and reports whether it had to.</summary>
    private static bool RotatePair(
        double[] work, double[] v, int rows, int columns, int p, int q)
    {
        double alpha = 0;
        double beta = 0;
        double gamma = 0;
        for (int i = 0; i < rows; i++)
        {
            double left = work[(i * columns) + p];
            double right = work[(i * columns) + q];
            alpha += left * left;
            beta += right * right;
            gamma += left * right;
        }

        if (gamma == 0 || Math.Abs(gamma) <= Threshold * Math.Sqrt(alpha * beta))
        {
            return false;
        }

        double zeta = (beta - alpha) / (2.0 * gamma);
        double t = Math.Sign(zeta) / (Math.Abs(zeta) + Math.Sqrt(1.0 + (zeta * zeta)));
        if (zeta == 0)
        {
            t = 1.0;
        }
        double cosine = 1.0 / Math.Sqrt(1.0 + (t * t));
        double sine = cosine * t;

        Rotate(work, rows, columns, p, q, cosine, sine);
        Rotate(v, columns, columns, p, q, cosine, sine);
        return true;
    }

    private static void Rotate(
        double[] block, int rows, int columns, int p, int q, double cosine, double sine)
    {
        for (int i = 0; i < rows; i++)
        {
            double left = block[(i * columns) + p];
            double right = block[(i * columns) + q];
            block[(i * columns) + p] = (cosine * left) - (sine * right);
            block[(i * columns) + q] = (sine * left) + (cosine * right);
        }
    }

    /// <summary>Reads the norms off the orthogonalized columns and sorts the triplets.</summary>
    private static (double[] U, double[] S, double[] Vt) Finish(
        double[] work, double[] v, int rows, int columns)
    {
        double[] norms = new double[columns];
        for (int j = 0; j < columns; j++)
        {
            norms[j] = DenseBlock.ColumnNorm(work, rows, columns, j);
        }

        int[] order = new int[columns];
        for (int j = 0; j < columns; j++)
        {
            order[j] = j;
        }
        Array.Sort(order, (left, right) => norms[right].CompareTo(norms[left]));

        double[] u = new double[rows * columns];
        double[] s = new double[columns];
        double[] vt = new double[columns * columns];
        for (int j = 0; j < columns; j++)
        {
            int source = order[j];
            double norm = norms[source];
            s[j] = norm;
            // A numerically zero column carries no direction; leaving U's column at zero is
            // what scipy's own factor does for a rank-deficient block, and dividing would not.
            double scale = norm == 0 ? 0 : 1.0 / norm;
            for (int i = 0; i < rows; i++)
            {
                u[(i * columns) + j] = work[(i * columns) + source] * scale;
            }
            for (int i = 0; i < columns; i++)
            {
                vt[(j * columns) + i] = v[(i * columns) + source];
            }
        }
        return (u, s, vt);
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release --filter "FullyQualifiedName~JacobiSvd"`
Expected: PASS, 28 tests (7 cases × 4 theories).

Run: `dotnet test tests/Lodestar.Decomposition.NetStandard.Tests -c Release --filter "FullyQualifiedName~JacobiSvd"`
Expected: the same count.

- [ ] **Step 7: Commit**

```bash
git add src/Lodestar.Decomposition/Internal/JacobiSvd.cs \
  tests/Lodestar.Decomposition.Tests/JacobiSvdTests.cs \
  tests/oracles/decomposition_svd.json tools/generate_oracles.py
git commit -m "Factor the small block the whole method exists to reach

One-sided Jacobi orthogonalizes the columns in place: the norms it converges to
are the singular values, the normalized columns are U, and the accumulated
rotations are V. No bidiagonalization, no shifts, no deflation -- and it is the
accurate method for a block this shape.

B = QtA is wide and short, so it is factored through its transpose, which trades
U and V. Both orientations are in the corpus, including the degenerate 1 x n and
n x 1.

Singular values are unique and asserted against scipy directly. The vectors are
unique only up to a per-triplet sign, so of them the corpus asserts the
reconstruction and orthonormality; scipy's own factors ride along.

Part of #440."
```

---

## Task 5: `TruncatedSvd`

**Files:**

- Create: `src/Lodestar.Decomposition/PowerIterationNormalizer.cs`
- Create: `src/Lodestar.Decomposition/Internal/RandomizedRangeFinder.cs`
- Create: `src/Lodestar.Decomposition/TruncatedSvd.cs`
- Create: `tests/Lodestar.Decomposition.Tests/TruncatedSvdTests.cs`
- Create: `docs/reference/decomposition/factorization.md`
- Create: `docs/reference/decomposition/factorization/truncatedsvd.md`
- Create: `docs/reference/decomposition/factorization/truncatedsvd-fit.md`
- Create: `docs/reference/decomposition/factorization/truncatedsvd-transform.md`
- Create: `docs/reference/decomposition/factorization/truncatedsvdoptions.md`
- Create: `docs/reference/decomposition/factorization/poweriterationnormalizer.md`
- Create: `tests/Lodestar.Decomposition.Tests/Documentation/ReferenceDocumentationTests.cs`
- Create: `samples/Lodestar.Sample/DecompositionSamples.cs`
- Create: `samples/Lodestar.Sample/TruncatedSvdSample.cs`
- Create: `samples/Lodestar.Sample/TruncatedSvdOptionsSample.cs`
- Modify: `samples/Lodestar.Sample/Program.cs`
- Modify: `tools/generate_oracles.py`
- Modify: `tests/oracles/decomposition_svd.json` (regenerated, gains `randomized`)
- Modify: `docs/wiki-map.json` (fills `covered`)
- Modify: `docs/equivalence.md`

**Interfaces:**

- Consumes: `HouseholderQr.Decompose`, `PartialPivotLu.PermutedLower`, `JacobiSvd.Decompose`,
  `DenseBlock.Transpose`, `GaussianSampler.Normal`, and `CsrMatrix`'s two dense-block products.
- Produces:
  - `public enum PowerIterationNormalizer { Auto, None, Qr, Lu }`
  - `public sealed class TruncatedSvdOptions` with `int Oversampling { get; init; } = 10`,
    `int PowerIterations { get; init; } = 5`,
    `PowerIterationNormalizer Normalizer { get; init; } = PowerIterationNormalizer.Auto`,
    `int Seed { get; init; }`, `double[]? RandomMatrix { get; init; }`
  - `public sealed class TruncatedSvd` with
    `public static TruncatedSvd Fit(CsrMatrix matrix, int componentCount, TruncatedSvdOptions? options = null)`,
    `public double[] Transform(CsrMatrix matrix)`, and the read-only properties
    `int ComponentCount`, `int FeatureCount`, `IReadOnlyList<double> Components`,
    `IReadOnlyList<double> SingularValues`, `IReadOnlyList<double> ExplainedVariance`,
    `IReadOnlyList<double> ExplainedVarianceRatio`.
  - `internal static class RandomizedRangeFinder` with
    `internal static double[] Find(CsrMatrix matrix, ReadOnlySpan<double> omega, int size, int powerIterations, PowerIterationNormalizer normalizer)`
  - `internal static class SignFlip` with
    `internal static void Apply(double[] u, int rows, int columns, double[] vt, int vtColumns)`

**There is no unfitted state.** `Fit` is the only way to get a `TruncatedSvd`, so no property has
to throw and CA1065 never comes up. `FitTransform` is deliberately absent: scikit-learn's computes
`U · Σ` during the fit while `transform` computes `X · Componentsᵀ`, and shipping both would mean
two numbers under one promise.

- [ ] **Step 1: Write the oracle generator**

Append to the dense-kernel block in `tools/generate_oracles.py`:

```python
# The randomized corpus's own keys, written once for S1192 and for the two
# readers who have to agree on them -- this generator and the C# theory.
OMEGA_KEY = "omega"
COMPONENT_COUNT_KEY = "component_count"


def _sparse_fixture(rng: SeededRandom, rows: int, columns: int, density: float) -> dict:
    """A CSR fixture, in the field names the C# side already reads elsewhere."""
    values, column_indices, row_pointers = [], [], [0]
    for _ in range(rows):
        for column in range(columns):
            if rng.random() < density:
                values.append(rng.uniform(0.1, 4.0))
                column_indices.append(column)
        row_pointers.append(len(values))
    return {
        ROWS_KEY: rows,
        COLUMNS_KEY: columns,
        "values": values,
        "column_indices": column_indices,
        "row_pointers": row_pointers,
    }


def _randomized_settings() -> list[tuple[int, int, float, int, int, int, str]]:
    """rows, columns, density, k, oversampling, power iterations, normalizer.

    Every matrix is at least as tall as it is wide, because TruncatedSVD's own
    ``transpose="auto"`` resolves to False exactly there -- and transpose is the one
    knob this package does not offer, so a wide fixture would compare two different
    factorizations. One case per normalizer, and one where k + p reaches the rank so
    the randomized answer is the exact one.
    """
    return [
        (40, 25, 0.30, 4, 6, 3, "QR"),
        (40, 25, 0.30, 4, 6, 1, "none"),
        (40, 25, 0.30, 4, 6, 5, "LU"),
        (60, 30, 0.20, 8, 10, 5, "auto"),
        (30, 12, 0.50, 3, 10, 4, "auto"),
        (25, 8, 0.60, 2, 10, 7, "QR"),
    ]


def _randomized_cases() -> list[dict]:
    """randomized_svd and TruncatedSVD over a frozen Omega.

    Omega is drawn from ``np.random.RandomState(seed)`` and the *same* seed is handed
    to scikit-learn, so the matrix stored here is bit-for-bit the one it draws first:
    ``_randomized_range_finder``'s opening call is
    ``random_state.normal(size=(n_features, k + p))``. Nothing is monkey-patched, and
    the C# side starts from the same Omega instead of reproducing MT19937.
    """
    from scipy.sparse import csr_matrix
    from sklearn.decomposition import TruncatedSVD
    from sklearn.utils.extmath import randomized_svd

    rng = SeededRandom(SEED + 44500)
    cases = []
    for index, (rows, columns, density, k, p, iterations, normalizer) in enumerate(
            _randomized_settings()):
        fixture = _sparse_fixture(rng, rows, columns, density)
        a = csr_matrix(
            (fixture["values"], fixture["column_indices"], fixture["row_pointers"]),
            shape=(rows, columns))

        seed = SEED + 44600 + index
        omega = np.random.RandomState(seed).normal(size=(columns, k + p))

        u, s, vt = randomized_svd(
            a, n_components=k, n_oversamples=p, n_iter=iterations,
            power_iteration_normalizer=normalizer, transpose=False, random_state=seed)

        svd = TruncatedSVD(
            n_components=k, algorithm="randomized", n_oversamples=p, n_iter=iterations,
            power_iteration_normalizer=normalizer, random_state=seed)
        svd.fit(a)

        cases.append({
            **fixture,
            COMPONENT_COUNT_KEY: k,
            "oversampling": p,
            "power_iterations": iterations,
            "normalizer": normalizer,
            OMEGA_KEY: omega.ravel().tolist(),
            "left_singular_vectors": u.ravel().tolist(),
            "singular_values": s.tolist(),
            "components": vt.ravel().tolist(),
            "explained_variance": svd.explained_variance_.tolist(),
            "explained_variance_ratio": svd.explained_variance_ratio_.tolist(),
            "transform": svd.transform(a).ravel().tolist(),
        })
    return cases
```

and extend `generate_decomposition_svd` to carry both halves:

```python
def generate_decomposition_svd() -> dict:
    """The dense SVD on its own, and randomized_svd composed on top of it (#440)."""
    dense, randomized = _dense_svd_cases(), _randomized_cases()
    return {"metadata": {"library": "scipy and scikit-learn",
                         "version": version("scipy"),
                         "sklearn_version": version("scikit-learn"),
                         "reference_calls": ["scipy.linalg.svd",
                                             "sklearn.utils.extmath.randomized_svd",
                                             "sklearn.decomposition.TruncatedSVD"],
                         "seed": SEED, "count": len(dense) + len(randomized),
                         "tolerance": 1e-9},
            "dense": dense,
            "randomized": randomized}
```

- [ ] **Step 2: Regenerate and read the generator's own exit code**

```bash
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
echo "exit: $?"
```

Expected: `exit: 0`; `decomposition_svd.json` now holds `dense` (7) and `randomized` (6). The
`dense` half must be byte-identical to Task 4's — `git diff` it and confirm only `randomized`,
`generator` and the version fields moved.

- [ ] **Step 3: Write the failing tests**

`tests/Lodestar.Decomposition.Tests/TruncatedSvdTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Abstractions;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// Randomized SVD against scikit-learn 1.9.0, over the Ω the corpus freezes. Ω is an input on
/// both sides, so this is an ordinary parity comparison and not a subspace one.
/// </summary>
public sealed class TruncatedSvdTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("decomposition_svd.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("randomized").EnumerateArray()];

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }
        return data;
    }

    private static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    private static int[] Ints(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetInt32())];

    private static CsrMatrix Matrix(JsonElement c) => new(
        c.GetProperty("rows").GetInt32(),
        c.GetProperty("columns").GetInt32(),
        Doubles(c, "values"),
        Ints(c, "column_indices"),
        Ints(c, "row_pointers"));

    private static PowerIterationNormalizer Normalizer(JsonElement c) =>
        c.GetProperty("normalizer").GetString() switch
        {
            "QR" => PowerIterationNormalizer.Qr,
            "LU" => PowerIterationNormalizer.Lu,
            "none" => PowerIterationNormalizer.None,
            _ => PowerIterationNormalizer.Auto,
        };

    private static TruncatedSvd Fit(JsonElement c) => TruncatedSvd.Fit(
        Matrix(c),
        c.GetProperty("component_count").GetInt32(),
        new TruncatedSvdOptions
        {
            Oversampling = c.GetProperty("oversampling").GetInt32(),
            PowerIterations = c.GetProperty("power_iterations").GetInt32(),
            Normalizer = Normalizer(c),
            RandomMatrix = Doubles(c, "omega"),
        });

    private static void AssertSame(double[] expected, IReadOnlyList<double> actual)
    {
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_singular_values_match_scikit_learn(int index)
    {
        JsonElement c = Cases[index];
        int k = c.GetProperty("component_count").GetInt32();

        AssertSame([.. Doubles(c, "singular_values").Take(k)], Fit(c).SingularValues);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_components_match_scikit_learn(int index)
    {
        JsonElement c = Cases[index];
        int k = c.GetProperty("component_count").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();

        AssertSame([.. Doubles(c, "components").Take(k * columns)], Fit(c).Components);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_explained_variance_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "explained_variance"), Fit(c).ExplainedVariance);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_explained_variance_ratio_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "explained_variance_ratio"), Fit(c).ExplainedVarianceRatio);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_transform_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "transform"), Fit(c).Transform(Matrix(c)));
    }

    [Fact]
    public void A_seed_draws_the_matrix_when_none_is_given()
    {
        JsonElement c = Cases[0];
        TruncatedSvdOptions options = new() { Seed = 20260901 };

        TruncatedSvd first = TruncatedSvd.Fit(Matrix(c), 3, options);
        TruncatedSvd second = TruncatedSvd.Fit(Matrix(c), 3, options);

        Assert.Equal(first.SingularValues, second.SingularValues);
    }

    [Fact]
    public void A_component_count_at_or_above_the_feature_count_is_refused()
    {
        CsrMatrix matrix = Matrix(Cases[0]);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TruncatedSvd.Fit(matrix, matrix.ColumnCount));
    }

    [Fact]
    public void A_random_matrix_of_the_wrong_shape_is_refused()
    {
        CsrMatrix matrix = Matrix(Cases[0]);

        Assert.Throws<ArgumentException>(() => TruncatedSvd.Fit(
            matrix, 4, new TruncatedSvdOptions { RandomMatrix = new double[7] }));
    }

    [Fact]
    public void Transforming_a_matrix_of_another_width_is_refused()
    {
        JsonElement c = Cases[0];
        TruncatedSvd fitted = Fit(c);
        CsrMatrix narrower = new(2, 3, [1.0], [0], [0, 1, 1]);

        Assert.Throws<ArgumentException>(() => fitted.Transform(narrower));
    }
}
```

`tests/Lodestar.Decomposition.Tests/Documentation/ReferenceDocumentationTests.cs` — copy
`tests/Lodestar.Abstractions.Tests/Documentation/ReferenceDocumentationTests.cs` and change the
namespace to `Lodestar.Decomposition.Tests.Documentation`, the probe type to
`typeof(TruncatedSvd)`, and the package name in both calls to `"Lodestar.Decomposition"`.

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release --filter "FullyQualifiedName~TruncatedSvd"`
Expected: FAIL — `TruncatedSvd` does not exist.

- [ ] **Step 5: Write the implementation**

`src/Lodestar.Decomposition/PowerIterationNormalizer.cs`:

```csharp
namespace Lodestar.Decomposition;

/// <summary>What a power iteration does to its block between the two products.</summary>
/// <remarks>
/// A power iteration sharpens the spectrum and, left alone, collapses every column onto the
/// leading singular vector — in double precision, within a handful of iterations. The normalizer
/// is what stops that, and which one is used changes the answer, so it is frozen in the corpus
/// rather than chosen by the implementation.
/// </remarks>
public enum PowerIterationNormalizer
{
    /// <summary><see cref="None"/> below three power iterations, <see cref="Lu"/> at or above — scikit-learn's rule.</summary>
    Auto = 0,

    /// <summary>Nothing between the products. Cheapest, and adequate only for one or two iterations.</summary>
    None = 1,

    /// <summary>An economic QR. The most accurate, and the most expensive.</summary>
    Qr = 2,

    /// <summary>LU with partial pivoting. What <see cref="Auto"/> resolves to at scikit-learn's own default of five iterations.</summary>
    Lu = 3,
}
```

`src/Lodestar.Decomposition/Internal/RandomizedRangeFinder.cs`:

```csharp
using Lodestar.Abstractions;

namespace Lodestar.Decomposition.Internal;

/// <summary>An orthonormal basis for the range of <c>A</c>, found through a thin random block.</summary>
/// <remarks>
/// This is the only place the sparse matrix is read. Everything after it works on a block of
/// <c>k + p</c> columns, which is why the rank asked for — and not the size of the matrix —
/// decides the cost of the whole method.
/// </remarks>
internal static class RandomizedRangeFinder
{
    /// <summary>Returns <c>Q</c>, row-major <c>matrix.RowCount × size</c> with orthonormal columns.</summary>
    internal static double[] Find(
        CsrMatrix matrix,
        ReadOnlySpan<double> omega,
        int size,
        int powerIterations,
        PowerIterationNormalizer normalizer)
    {
        PowerIterationNormalizer resolved = Resolve(normalizer, powerIterations);

        double[] q = matrix.Multiply(omega, size);
        for (int iteration = 0; iteration < powerIterations; iteration++)
        {
            q = Normalize(q, matrix.RowCount, size, resolved);
            q = matrix.TransposeMultiply(q, size);
            q = Normalize(q, matrix.ColumnCount, size, resolved);
            q = matrix.Multiply(q, size);
        }

        (double[] basis, _) = HouseholderQr.Decompose(q, matrix.RowCount, size);
        return basis;
    }

    /// <summary>scikit-learn's <c>auto</c>: no normalizer below three iterations, LU above.</summary>
    internal static PowerIterationNormalizer Resolve(
        PowerIterationNormalizer normalizer, int powerIterations) =>
        normalizer != PowerIterationNormalizer.Auto ? normalizer
            : powerIterations <= 2 ? PowerIterationNormalizer.None
            : PowerIterationNormalizer.Lu;

    private static double[] Normalize(
        double[] block, int rows, int columns, PowerIterationNormalizer normalizer)
    {
        switch (normalizer)
        {
            case PowerIterationNormalizer.Qr:
                (double[] basis, _) = HouseholderQr.Decompose(block, rows, columns);
                return basis;
            case PowerIterationNormalizer.Lu:
                return PartialPivotLu.PermutedLower(block, rows, columns);
            default:
                return block;
        }
    }
}
```

> **The loop shape matters and is easy to get subtly wrong.** scikit-learn's is
> `Q = A Ω`, then `n_iter` times `{ Q ← normalize(A Q) ; Q ← normalize(Aᵀ Q) }`, then
> `Q, _ = qr(A Q)`. Written that way the products alternate starting from `A Ω`, which is what
> the code above does by normalizing *before* each product rather than after. If a case fails on
> `none` but passes on `QR`, this ordering is the first thing to re-derive against the corpus.

`src/Lodestar.Decomposition/Internal/SignFlip.cs`:

```csharp
namespace Lodestar.Decomposition.Internal;

/// <summary>scikit-learn's <c>svd_flip</c>, so two runs agree on more than a subspace.</summary>
/// <remarks>
/// An SVD is unique only up to flipping the sign of a matched pair of vectors. scikit-learn pins
/// it by making the largest-magnitude entry of each left vector positive, and every number this
/// package reports downstream — components, transforms, NNDSVD's initialisation — inherits that
/// convention.
/// </remarks>
internal static class SignFlip
{
    /// <summary>Flips each column of <paramref name="u"/> and the matching row of <paramref name="vt"/>.</summary>
    internal static void Apply(double[] u, int rows, int columns, double[] vt, int vtColumns)
    {
        for (int j = 0; j < columns; j++)
        {
            int largest = 0;
            double best = -1;
            for (int i = 0; i < rows; i++)
            {
                double magnitude = Math.Abs(u[(i * columns) + j]);
                if (magnitude > best)
                {
                    best = magnitude;
                    largest = i;
                }
            }

            double sign = Math.Sign(u[(largest * columns) + j]);
            if (sign >= 0)
            {
                continue;
            }

            for (int i = 0; i < rows; i++)
            {
                u[(i * columns) + j] = -u[(i * columns) + j];
            }
            for (int i = 0; i < vtColumns; i++)
            {
                vt[(j * vtColumns) + i] = -vt[(j * vtColumns) + i];
            }
        }
    }
}
```

`src/Lodestar.Decomposition/TruncatedSvd.cs`:

```csharp
using Lodestar.Abstractions;
using Lodestar.Decomposition.Internal;

namespace Lodestar.Decomposition;

/// <summary>What <see cref="TruncatedSvd.Fit"/> is allowed to vary.</summary>
public sealed class TruncatedSvdOptions
{
    /// <summary>Extra columns drawn beyond the rank asked for. scikit-learn's default is 10.</summary>
    public int Oversampling { get; init; } = 10;

    /// <summary>Power iterations. scikit-learn's <c>TruncatedSVD</c> default is 5.</summary>
    public int PowerIterations { get; init; } = 5;

    /// <summary>What happens to the block between the two products.</summary>
    public PowerIterationNormalizer Normalizer { get; init; } = PowerIterationNormalizer.Auto;

    /// <summary>Seeds this package's own generator when <see cref="RandomMatrix"/> is null.</summary>
    /// <remarks>
    /// It reproduces a run of Lodestar, not a run of scikit-learn: the two draw from different
    /// generators. Pass <see cref="RandomMatrix"/> to compare against Python.
    /// </remarks>
    public int Seed { get; init; }

    /// <summary>Ω itself, row-major and <c>features × (components + oversampling)</c>, or null to draw one.</summary>
    public double[]? RandomMatrix { get; init; }
}

/// <summary>A fitted truncated SVD — latent semantic analysis, with nothing centred.</summary>
public sealed class TruncatedSvd
{
    private readonly double[] _components;
    private readonly double[] _singularValues;
    private readonly double[] _explainedVariance;
    private readonly double[] _explainedVarianceRatio;

    private TruncatedSvd(
        int featureCount,
        double[] components,
        double[] singularValues,
        double[] explainedVariance,
        double[] explainedVarianceRatio)
    {
        FeatureCount = featureCount;
        _components = components;
        _singularValues = singularValues;
        _explainedVariance = explainedVariance;
        _explainedVarianceRatio = explainedVarianceRatio;
    }

    /// <summary>How many components were kept.</summary>
    public int ComponentCount => _singularValues.Length;

    /// <summary>How many columns the fitted matrix had, and every matrix passed to <see cref="Transform"/> must have.</summary>
    public int FeatureCount { get; }

    /// <summary>The right singular vectors, row-major <see cref="ComponentCount"/> × <see cref="FeatureCount"/>.</summary>
    public IReadOnlyList<double> Components => _components;

    /// <summary>The singular values kept, largest first.</summary>
    public IReadOnlyList<double> SingularValues => _singularValues;

    /// <summary>The variance of each transformed column.</summary>
    public IReadOnlyList<double> ExplainedVariance => _explainedVariance;

    /// <summary>Each component's share of the input's total column variance.</summary>
    /// <remarks>
    /// The denominator is the whole matrix's variance, not the kept components', which is why
    /// these sum to less than one — and why the sum is the number that says whether the rank is
    /// enough.
    /// </remarks>
    public IReadOnlyList<double> ExplainedVarianceRatio => _explainedVarianceRatio;

    /// <summary>Fits a truncated SVD of <paramref name="matrix"/> at rank <paramref name="componentCount"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="matrix"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="componentCount"/> is not in <c>[1, matrix.ColumnCount)</c>, or an option is out of range.</exception>
    /// <exception cref="ArgumentException"><see cref="TruncatedSvdOptions.RandomMatrix"/> is not <c>matrix.ColumnCount × (componentCount + oversampling)</c>.</exception>
    public static TruncatedSvd Fit(
        CsrMatrix matrix, int componentCount, TruncatedSvdOptions? options = null)
    {
        if (matrix is null)
        {
            throw new ArgumentNullException(nameof(matrix));
        }
        TruncatedSvdOptions settings = options ?? new TruncatedSvdOptions();
        Validate(matrix, componentCount, settings);

        int features = matrix.ColumnCount;
        int size = componentCount + settings.Oversampling;
        double[] omega = settings.RandomMatrix ?? new GaussianSampler(settings.Seed).Normal(features, size);
        if (omega.Length != (long)features * size)
        {
            throw new ArgumentException(
                $"Ω is {omega.Length} long, not {features} × {size}.", nameof(options));
        }

        double[] q = RandomizedRangeFinder.Find(
            matrix, omega, size, settings.PowerIterations, settings.Normalizer);

        // B = Qᵀ A, reached as (Aᵀ Q)ᵀ so the sparse matrix is never transposed.
        double[] b = DenseBlock.Transpose(matrix.TransposeMultiply(q, size), features, size);
        (double[] uHat, double[] s, double[] vt) = JacobiSvd.Decompose(b, size, features);
        int rank = s.Length;

        double[] u = Product(q, matrix.RowCount, size, uHat, rank);
        SignFlip.Apply(u, matrix.RowCount, rank, vt, features);

        double[] components = new double[componentCount * features];
        Array.Copy(vt, components, components.Length);
        double[] singularValues = new double[componentCount];
        Array.Copy(s, singularValues, componentCount);

        double[] variance = TransformedVariance(u, matrix.RowCount, rank, singularValues);
        double total = TotalVariance(matrix);
        double[] ratio = new double[componentCount];
        for (int j = 0; j < componentCount; j++)
        {
            ratio[j] = variance[j] / total;
        }

        return new TruncatedSvd(features, components, singularValues, variance, ratio);
    }

    /// <summary>Projects <paramref name="matrix"/> onto the components, row-major and <see cref="ComponentCount"/> wide.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="matrix"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="matrix"/> does not have <see cref="FeatureCount"/> columns.</exception>
    public double[] Transform(CsrMatrix matrix)
    {
        if (matrix is null)
        {
            throw new ArgumentNullException(nameof(matrix));
        }
        if (matrix.ColumnCount != FeatureCount)
        {
            throw new ArgumentException(
                $"This fit has {FeatureCount} features; the matrix has {matrix.ColumnCount}.",
                nameof(matrix));
        }

        // X · Componentsᵀ, one row at a time over the non-zeros.
        int k = ComponentCount;
        double[] result = new double[checked(matrix.RowCount * k)];
        for (int row = 0; row < matrix.RowCount; row++)
        {
            int target = row * k;
            for (int index = matrix.RowPointers[row]; index < matrix.RowPointers[row + 1]; index++)
            {
                double value = matrix.Values[index];
                int feature = matrix.ColumnIndices[index];
                for (int component = 0; component < k; component++)
                {
                    result[target + component] +=
                        value * _components[(component * FeatureCount) + feature];
                }
            }
        }
        return result;
    }

    private static void Validate(CsrMatrix matrix, int componentCount, TruncatedSvdOptions settings)
    {
        if (componentCount < 1 || componentCount >= matrix.ColumnCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(componentCount), componentCount,
                $"A truncated SVD keeps between 1 and {matrix.ColumnCount - 1} components.");
        }
        if (settings.Oversampling < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings), settings.Oversampling, "Oversampling is not negative.");
        }
        if (settings.PowerIterations < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings), settings.PowerIterations, "PowerIterations is not negative.");
        }
    }

    /// <summary>Q · Û, keeping the first <paramref name="rank"/> columns of the result.</summary>
    private static double[] Product(double[] q, int rows, int size, double[] uHat, int rank)
    {
        double[] result = new double[checked(rows * rank)];
        for (int i = 0; i < rows; i++)
        {
            for (int k = 0; k < size; k++)
            {
                double value = q[(i * size) + k];
                for (int j = 0; j < rank; j++)
                {
                    result[(i * rank) + j] += value * uHat[(k * rank) + j];
                }
            }
        }
        return result;
    }

    /// <summary>The per-column variance of <c>U Σ</c>, which is what scikit-learn reports.</summary>
    private static double[] TransformedVariance(
        double[] u, int rows, int rank, double[] singularValues)
    {
        double[] variance = new double[singularValues.Length];
        for (int j = 0; j < singularValues.Length; j++)
        {
            double mean = 0;
            for (int i = 0; i < rows; i++)
            {
                mean += u[(i * rank) + j] * singularValues[j];
            }
            mean /= rows;

            double sum = 0;
            for (int i = 0; i < rows; i++)
            {
                double centred = (u[(i * rank) + j] * singularValues[j]) - mean;
                sum += centred * centred;
            }
            variance[j] = sum / rows;
        }
        return variance;
    }

    /// <summary>The input's total column variance — the denominator of the ratio.</summary>
    /// <remarks>
    /// Computed from the sums of a sparse column and of its squares, which is what
    /// <c>mean_variance_axis</c> does: nothing is densified to reach it, and the zeros count
    /// towards the mean exactly as they must.
    /// </remarks>
    private static double TotalVariance(CsrMatrix matrix)
    {
        double[] sums = new double[matrix.ColumnCount];
        double[] squares = new double[matrix.ColumnCount];
        for (int index = 0; index < matrix.Values.Length; index++)
        {
            double value = matrix.Values[index];
            int column = matrix.ColumnIndices[index];
            sums[column] += value;
            squares[column] += value * value;
        }

        double total = 0;
        for (int column = 0; column < matrix.ColumnCount; column++)
        {
            double mean = sums[column] / matrix.RowCount;
            total += (squares[column] / matrix.RowCount) - (mean * mean);
        }
        return total;
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release --filter "FullyQualifiedName~TruncatedSvdTests"`
Expected: PASS, 34 tests (6 cases × 5 theories, plus 4 facts).

If `The_explained_variance_matches_scikit_learn` is the only failure, the variance is being taken
over `Transform(X)` rather than over `U Σ`. They agree mathematically and not in the last bits;
scikit-learn takes it over `U Σ`, and so must this.

Run: `dotnet test tests/Lodestar.Decomposition.NetStandard.Tests -c Release --filter "FullyQualifiedName~TruncatedSvdTests"`
Expected: the same count.

- [ ] **Step 7: Write the reference pages and fill `covered`**

`docs/wiki-map.json` — `Lodestar.Decomposition`'s `covered` stops being empty:

```json
      "covered": {
        "Lodestar.Decomposition": "docs/reference/decomposition/factorization"
      }
```

Create the five pages below. Follow `docs/reference/conformal/prediction/splitconformal-quantile.md`
for the section order — declaration fence, **Parameters**, **Returns**, **Exceptions**,
**Example**, **Remarks**, **Applies to**, **See also**. Two rules the gate enforces and a reviewer
cannot: the fence after `<!-- docs-declaration -->` must list **only** what reflection reports for
that member name (all overloads, nothing else), and any member named on a non-reference page must
be linked to its entry at least once on that page.

`docs/reference/decomposition/factorization.md` — the index. Lead with what the package is for
(uncentred LSA over a sparse matrix), then a members table linking
[`TruncatedSvd`](factorization/truncatedsvd.md), [`TruncatedSvdOptions`](factorization/truncatedsvdoptions.md)
and [`PowerIterationNormalizer`](factorization/poweriterationnormalizer.md). Say in one sentence,
here, that Ω is an input rather than a seed, and link
[ADR 0072](../../decisions/0072-omega-is-an-input-not-a-seed.md) — Task 9 writes it, so this link
is dead until then; the whole-tree link check runs in the pre-PR gates, after Task 9.

`docs/reference/decomposition/factorization/truncatedsvd.md` — the type page: one paragraph, a
**Properties** table (`ComponentCount`, `FeatureCount`, `Components`, `SingularValues`,
`ExplainedVariance`, `ExplainedVarianceRatio` — what each holds and its shape), then a **Members**
table linking `truncatedsvd-fit.md` and `truncatedsvd-transform.md`.

`.../truncatedsvd-fit.md`:

```csharp
public static TruncatedSvd Fit(CsrMatrix matrix, int componentCount, TruncatedSvdOptions options)
```

Its **Example** fence is executed, so it ends in a `// =>` on a value this page promises. Use a
small matrix built by hand and assert `fitted.ComponentCount`.

`.../truncatedsvd-transform.md`:

```csharp
public double[] Transform(CsrMatrix matrix)
```

`.../truncatedsvdoptions.md` and `.../poweriterationnormalizer.md` — type pages, no declaration
fence; the enum page's table gives one row per member, and says that `Auto` resolves to `None`
below three power iterations and to `Lu` at or above, which is what makes the default path LU.

- [ ] **Step 8: Write the samples**

`samples/Lodestar.Sample/TruncatedSvdSample.cs` — build a small term-document `CsrMatrix` by hand,
fit at rank 2, print the singular values, the explained-variance ratio and its sum, and the two
transformed rows. `samples/Lodestar.Sample/TruncatedSvdOptionsSample.cs` — fit the same matrix
twice, once with `Normalizer = PowerIterationNormalizer.Qr` and once with `Lu`, and print both
sets of singular values to show the normalizer is part of the answer. Both print through
`Inv.F3` / `Inv.List` so the run reads the same in every culture.

`samples/Lodestar.Sample/DecompositionSamples.cs` — an aggregator in the shape of
`TextSamples.cs`, calling `TruncatedSvdSample.Run()` then `TruncatedSvdOptionsSample.Run()`. The
filename must **not** end in `Sample.cs`, or `check_sample_coverage.py` will read it as a class's
sample.

`samples/Lodestar.Sample/Program.cs` — add `using Lodestar.Decomposition;`, a
`Console.WriteLine($"Lodestar.Decomposition: {FrameworkOf(typeof(TruncatedSvd))}");` line beside
the others, and `DecompositionSamples.Run();` after `SplitConformalSample.Run();`.

- [ ] **Step 9: Add the equivalence rows**

`docs/equivalence.md` — a new section after `## Lodestar.Conformal`, because a row lands in the
same commit as the function:

```markdown
## Lodestar.Decomposition — truncated SVD

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
| `TruncatedSVD(n_components=k, algorithm="randomized").fit(X)` | scikit-learn | [`TruncatedSvd.Fit(matrix, k)`](reference/decomposition/factorization/truncatedsvd-fit.md) | Ω is an **input**, not a seed: pass `RandomMatrix` to reproduce a Python run, since a `Seed` drives Lodestar's own generator ([decision 0072](decisions/0072-omega-is-an-input-not-a-seed.md)). `transpose="auto"` is not offered, so a matrix with fewer rows than columns is factorized as written where scikit-learn would swap the products. |
| `svd.transform(X)` | scikit-learn | [`TruncatedSvd.Transform(matrix)`](reference/decomposition/factorization/truncatedsvd-transform.md) | `X · componentsᵀ`, identical. `fit_transform`'s `U · Σ` is deliberately absent — same value, different last bits. |
| `svd.components_`, `svd.singular_values_` | scikit-learn | `Components`, `SingularValues` | Row-major `k × n_features` and `k`. Signs pinned by `svd_flip`, as scikit-learn does. |
| `svd.explained_variance_`, `svd.explained_variance_ratio_` | scikit-learn | `ExplainedVariance`, `ExplainedVarianceRatio` | Per-component variance of `U · Σ` (`ddof=0`), over the input's total column variance. Identical. |
| `power_iteration_normalizer=...` | scikit-learn | [`PowerIterationNormalizer`](reference/decomposition/factorization/poweriterationnormalizer.md) | All four values, including `Auto`'s rule — `None` below three iterations, `Lu` at or above. |
```

- [ ] **Step 10: Run the tests and the sample-coverage check**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release`
Expected: PASS. `Every_covered_namespace_is_documented` and
`Every_documented_member_named_in_the_docs_links_to_its_entry` are now live.

Run: `python3 tools/check_sample_coverage.py`
Expected: `ok`, and the count names `Lodestar.Decomposition`.

- [ ] **Step 11: Commit**

```bash
git add src/Lodestar.Decomposition tests/Lodestar.Decomposition.Tests \
  tests/oracles/decomposition_svd.json tools/generate_oracles.py docs/wiki-map.json \
  docs/reference/decomposition docs/equivalence.md samples/Lodestar.Sample
git commit -m "Fit a truncated SVD at scikit-learn parity, over a frozen Omega

Omega is drawn from np.random.RandomState(seed) in the generator and the same
seed is handed to scikit-learn, so the matrix the corpus stores is bit-for-bit
the one it draws first. Nothing is monkey-patched, and the C# starts from the
same block instead of reimplementing MT19937 -- which is what turns a randomized
algorithm into an ordinary parity target.

Every TruncatedSVD fixture is at least as tall as it is wide, because that is
where its own transpose='auto' resolves to False. transpose is the one knob this
package does not offer, so a wide fixture would compare two different
factorizations and call the disagreement a bug.

Explained variance is taken over U*Sigma, not over Transform(X): they agree
mathematically and not in the last bits, and scikit-learn takes the first.

There is no unfitted state -- Fit is the only constructor -- so no property has
to throw. FitTransform is absent on purpose: two numbers under one promise.

Part of #440."
```

---

## Task 6: The NNDSVD initialisation family

**Files:**

- Create: `src/Lodestar.Decomposition/Internal/RandomizedSvd.cs`
- Create: `src/Lodestar.Decomposition/NmfInitialization.cs`
- Create: `src/Lodestar.Decomposition/Internal/NndSvd.cs`
- Create: `tests/Lodestar.Decomposition.Tests/NndSvdTests.cs`
- Create: `tests/oracles/decomposition_nmf.json` (the `initialization` half; Task 7 adds `updates`)
- Modify: `src/Lodestar.Decomposition/TruncatedSvd.cs` (calls the extracted kernel)
- Modify: `tools/generate_oracles.py`

**Interfaces:**

- Consumes: everything Task 5 produced.
- Produces:
  - `public enum NmfInitialization { NndSvd, NndSvda }`
  - `internal static class RandomizedSvd` with
    `internal static (double[] U, double[] S, double[] Vt, int Rank) Compute(CsrMatrix matrix, int componentCount, int oversampling, int powerIterations, PowerIterationNormalizer normalizer, ReadOnlySpan<double> omega)`
    — `U` is row-major `matrix.RowCount × Rank`, `S` is `double[Rank]`, `Vt` is row-major
    `Rank × matrix.ColumnCount`, all sign-flipped, all **untruncated**: the caller keeps what it
    needs.
  - `internal static class NndSvd` with
    `internal static (double[] W, double[] H) Initialize(CsrMatrix matrix, int componentCount, NmfInitialization initialization, int seed, double[]? randomMatrix)`
    — `W` is row-major `matrix.RowCount × componentCount`, `H` is row-major
    `componentCount × matrix.ColumnCount`.

**`NndSvdar` is not shipped.** It fills the zeros with draws from numpy's uniform stream, which
[the spec rejects reproducing](../specs/2026-09-01_0440_decomposition-truncated-svd-and-nmf.md#rejected)
along with `RandomState.normal`. An initialisation that cannot be checked against the reference is
an initialisation nobody can trust, so the enum has two members and the reference page says why.

- [ ] **Step 1: Extract the triplet out of `TruncatedSvd.Fit`**

`Fit` currently computes `U`, `S` and `Vt` inline and keeps three of the four. NNDSVD needs all of
them, so move the computation into `src/Lodestar.Decomposition/Internal/RandomizedSvd.cs`:

```csharp
using Lodestar.Abstractions;

namespace Lodestar.Decomposition.Internal;

/// <summary>scikit-learn's <c>randomized_svd</c>, with Ω supplied rather than seeded.</summary>
/// <remarks>
/// The range finder reads the sparse matrix; everything after it is dense and thin. Sign
/// conventions are pinned here, once, so <c>TruncatedSvd</c> and the NNDSVD initialisation cannot
/// drift apart on them.
/// </remarks>
internal static class RandomizedSvd
{
    internal static (double[] U, double[] S, double[] Vt, int Rank) Compute(
        CsrMatrix matrix,
        int componentCount,
        int oversampling,
        int powerIterations,
        PowerIterationNormalizer normalizer,
        ReadOnlySpan<double> omega)
    {
        int features = matrix.ColumnCount;
        int size = componentCount + oversampling;

        double[] q = RandomizedRangeFinder.Find(matrix, omega, size, powerIterations, normalizer);

        // B = Qᵀ A, reached as (Aᵀ Q)ᵀ so the sparse matrix is never transposed.
        double[] b = DenseBlock.Transpose(matrix.TransposeMultiply(q, size), features, size);
        (double[] uHat, double[] s, double[] vt) = JacobiSvd.Decompose(b, size, features);
        int rank = s.Length;

        double[] u = Product(q, matrix.RowCount, size, uHat, rank);
        SignFlip.Apply(u, matrix.RowCount, rank, vt, features);
        return (u, s, vt, rank);
    }

    /// <summary>Q · Û.</summary>
    private static double[] Product(double[] q, int rows, int size, double[] uHat, int rank)
    {
        double[] result = new double[checked(rows * rank)];
        for (int i = 0; i < rows; i++)
        {
            for (int k = 0; k < size; k++)
            {
                double value = q[(i * size) + k];
                for (int j = 0; j < rank; j++)
                {
                    result[(i * rank) + j] += value * uHat[(k * rank) + j];
                }
            }
        }
        return result;
    }
}
```

`TruncatedSvd.Fit` then loses its `Product` helper and its middle block, and reads:

```csharp
        (double[] u, double[] s, double[] vt, int rank) = RandomizedSvd.Compute(
            matrix, componentCount, settings.Oversampling, settings.PowerIterations,
            settings.Normalizer, omega);
```

with everything from `double[] components = ...` onward unchanged. `TruncatedSvd.Product` and its
`using`s go; `rank` is still what `TransformedVariance` strides by.

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release --filter "FullyQualifiedName~TruncatedSvdTests"`
Expected: PASS, still 34. A pure extraction that moves a number is not a pure extraction.

- [ ] **Step 2: Write the oracle generator**

Append to `tools/generate_oracles.py`:

```python
# --- NMF for Lodestar.Decomposition (#440) ---------------------------------

INITIAL_W_KEY = "initial_w"
INITIAL_H_KEY = "initial_h"


def _nmf_settings() -> list[tuple[int, int, float, int, str]]:
    """rows, columns, density, k, init. Tall again, for transpose='auto'."""
    return [
        (30, 12, 0.45, 3, "nndsvd"),
        (30, 12, 0.45, 3, "nndsvda"),
        (48, 20, 0.30, 5, "nndsvd"),
        (48, 20, 0.30, 5, "nndsvda"),
        (16, 6, 0.70, 2, "nndsvd"),
    ]


def _nmf_initialization_cases() -> list[dict]:
    """_initialize_nmf over a frozen Omega.

    It calls randomized_svd internally, so W0 and H0 depend on the seed -- measured,
    seeds 7 and 99 give different matrices. Freezing Omega is what decouples the
    initialisation from the update loop, and lets each fail on its own.
    """
    from scipy.sparse import csr_matrix
    from sklearn.decomposition._nmf import _initialize_nmf

    rng = SeededRandom(SEED + 44700)
    cases = []
    for index, (rows, columns, density, k, init) in enumerate(_nmf_settings()):
        fixture = _sparse_fixture(rng, rows, columns, density)
        a = csr_matrix(
            (fixture["values"], fixture["column_indices"], fixture["row_pointers"]),
            shape=(rows, columns))

        seed = SEED + 44800 + index
        # _initialize_nmf's own randomized_svd call takes n_oversamples=10 and
        # n_iter='auto'; the first draw off this RandomState is the same Omega.
        omega = np.random.RandomState(seed).normal(size=(columns, k + 10))
        w, h = _initialize_nmf(a, k, init=init, random_state=seed)

        cases.append({
            **fixture,
            COMPONENT_COUNT_KEY: k,
            "initialization": init,
            OMEGA_KEY: omega.ravel().tolist(),
            INITIAL_W_KEY: w.ravel().tolist(),
            INITIAL_H_KEY: h.ravel().tolist(),
        })
    return cases


def generate_decomposition_nmf() -> dict:
    """NNDSVD, and (from Task 7) the multiplicative updates on top of it (#440)."""
    initialization = _nmf_initialization_cases()
    return {"metadata": {"library": "scikit-learn", "version": version("scikit-learn"),
                         "reference_calls": ["sklearn.decomposition._nmf._initialize_nmf"],
                         "seed": SEED, "count": len(initialization), "tolerance": 1e-9},
            "initialization": initialization}
```

Register it in `main()`:

```python
        "decomposition_nmf.json": generate_decomposition_nmf,
```

- [ ] **Step 3: Generate the corpus and read the generator's own exit code**

```bash
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
echo "exit: $?"
```

Expected: `exit: 0`, and `decomposition_nmf.json` holds an `initialization` array of 5 cases.

- [ ] **Step 4: Confirm the two constants against the installed scikit-learn**

`_initialize_nmf` has an `eps` default and a fill value for `nndsvda`, and both change every
number in the corpus. Read them rather than recall them — scikit-learn is BSD-3-Clause, so
consulting it to confirm a constant is fine; write the C# from the algorithm, not by
transcription:

```bash
sed -n '/^def _initialize_nmf/,/^def /p' \
  <repo>/.venv-oracles/lib/python3.*/site-packages/sklearn/decomposition/_nmf.py
```

Note the `eps` default, what `W[W < eps]` and `H[H < eps]` are set to, and what `nndsvda` fills a
zero with. Those three facts are the ones the implementation below depends on.

- [ ] **Step 5: Write the failing test**

`tests/Lodestar.Decomposition.Tests/NndSvdTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Abstractions;
using Lodestar.Decomposition.Internal;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// <c>_initialize_nmf</c>'s NNDSVD family, over the same frozen Ω its internal
/// <c>randomized_svd</c> would have drawn.
/// </summary>
public sealed class NndSvdTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("decomposition_nmf.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("initialization").EnumerateArray()];

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }
        return data;
    }

    private static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    private static int[] Ints(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetInt32())];

    private static CsrMatrix Matrix(JsonElement c) => new(
        c.GetProperty("rows").GetInt32(),
        c.GetProperty("columns").GetInt32(),
        Doubles(c, "values"),
        Ints(c, "column_indices"),
        Ints(c, "row_pointers"));

    private static (double[] W, double[] H) Initialize(JsonElement c) => NndSvd.Initialize(
        Matrix(c),
        c.GetProperty("component_count").GetInt32(),
        c.GetProperty("initialization").GetString() == "nndsvda"
            ? NmfInitialization.NndSvda
            : NmfInitialization.NndSvd,
        seed: 0,
        randomMatrix: Doubles(c, "omega"));

    private static void AssertSame(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void W_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "initial_w"), Initialize(c).W);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void H_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "initial_h"), Initialize(c).H);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Neither_factor_holds_a_negative_number(int index)
    {
        JsonElement c = Cases[index];

        (double[] w, double[] h) = Initialize(c);

        Assert.All(w, value => Assert.True(value >= 0, $"W holds {value}"));
        Assert.All(h, value => Assert.True(value >= 0, $"H holds {value}"));
    }
}
```

- [ ] **Step 6: Run the test to verify it fails**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release --filter "FullyQualifiedName~NndSvd"`
Expected: FAIL — `NndSvd` does not exist.

- [ ] **Step 7: Write the implementation**

`src/Lodestar.Decomposition/NmfInitialization.cs`:

```csharp
namespace Lodestar.Decomposition;

/// <summary>Where <see cref="Nmf.Fit(Lodestar.Abstractions.CsrMatrix, int, NmfOptions)"/> starts from.</summary>
/// <remarks>
/// Multiplicative updates never introduce a non-zero where the initialisation put a zero, so the
/// initialisation decides the sparsity of the answer as much as the data does.
/// scikit-learn's <c>nndsvdar</c> is not offered: it fills its zeros from numpy's uniform stream,
/// which nothing here reproduces, so it could not be checked against the reference.
/// </remarks>
public enum NmfInitialization
{
    /// <summary>Non-negative double SVD. Leaves zeros in place, which keeps the factors sparse.</summary>
    NndSvd = 0,

    /// <summary>NNDSVD with the zeros filled by the matrix's mean. Denser, and it converges faster.</summary>
    NndSvda = 1,
}
```

`src/Lodestar.Decomposition/Internal/NndSvd.cs`:

```csharp
using Lodestar.Abstractions;

namespace Lodestar.Decomposition.Internal;

/// <summary>Non-negative double SVD: a deterministic starting point for NMF.</summary>
/// <remarks>
/// Each singular triplet after the first is split into its positive and its negative part, and
/// whichever carries more energy becomes the component. The leading triplet needs no such choice —
/// Perron–Frobenius makes it non-negative already for a non-negative matrix.
/// </remarks>
internal static class NndSvd
{
    // _initialize_nmf's eps default: anything below it is snapped to zero, which is what
    // keeps NndSvd sparse instead of dusted with rounding noise.
    private const double Epsilon = 1e-6;

    // _initialize_nmf calls randomized_svd with its own defaults, not TruncatedSVD's.
    private const int Oversampling = 10;

    internal static (double[] W, double[] H) Initialize(
        CsrMatrix matrix,
        int componentCount,
        NmfInitialization initialization,
        int seed,
        double[]? randomMatrix)
    {
        int rows = matrix.RowCount;
        int features = matrix.ColumnCount;
        int size = componentCount + Oversampling;
        double[] omega = randomMatrix ?? new GaussianSampler(seed).Normal(features, size);

        (double[] u, double[] s, double[] vt, int rank) = RandomizedSvd.Compute(
            matrix, componentCount, Oversampling, PowerIterations(matrix, componentCount),
            PowerIterationNormalizer.Auto, omega);

        double[] w = new double[checked(rows * componentCount)];
        double[] h = new double[checked(componentCount * features)];

        double leading = Math.Sqrt(s[0]);
        for (int i = 0; i < rows; i++)
        {
            w[i * componentCount] = leading * Math.Abs(u[i * rank]);
        }
        for (int j = 0; j < features; j++)
        {
            h[j] = leading * Math.Abs(vt[j]);
        }

        for (int component = 1; component < componentCount; component++)
        {
            double[] left = Column(u, rows, rank, component);
            double[] right = Row(vt, features, component);

            (double[] leftPart, double leftNorm, double[] rightPart, double rightNorm) =
                Dominant(left, right);

            double sigma = Math.Sqrt(s[component] * leftNorm * rightNorm);
            for (int i = 0; i < rows; i++)
            {
                w[(i * componentCount) + component] = sigma * leftPart[i] / leftNorm;
            }
            for (int j = 0; j < features; j++)
            {
                h[(component * features) + j] = sigma * rightPart[j] / rightNorm;
            }
        }

        Snap(w);
        Snap(h);

        if (initialization == NmfInitialization.NndSvda)
        {
            double average = Average(matrix);
            Fill(w, average);
            Fill(h, average);
        }
        return (w, h);
    }

    /// <summary>scikit-learn's <c>n_iter="auto"</c>: 7 for a small rank, 4 otherwise.</summary>
    private static int PowerIterations(CsrMatrix matrix, int componentCount) =>
        componentCount < 0.1 * Math.Min(matrix.RowCount, matrix.ColumnCount) ? 7 : 4;

    /// <summary>The heavier of the positive and the negative part of a matched pair.</summary>
    private static (double[] Left, double LeftNorm, double[] Right, double RightNorm) Dominant(
        double[] left, double[] right)
    {
        (double[] leftPositive, double leftPositiveNorm) = Positive(left);
        (double[] rightPositive, double rightPositiveNorm) = Positive(right);
        (double[] leftNegative, double leftNegativeNorm) = Negative(left);
        (double[] rightNegative, double rightNegativeNorm) = Negative(right);

        return leftPositiveNorm * rightPositiveNorm > leftNegativeNorm * rightNegativeNorm
            ? (leftPositive, leftPositiveNorm, rightPositive, rightPositiveNorm)
            : (leftNegative, leftNegativeNorm, rightNegative, rightNegativeNorm);
    }

    private static (double[] Part, double Norm) Positive(double[] vector)
    {
        double[] part = new double[vector.Length];
        double sum = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            part[i] = Math.Max(vector[i], 0);
            sum += part[i] * part[i];
        }
        return (part, Math.Sqrt(sum));
    }

    private static (double[] Part, double Norm) Negative(double[] vector)
    {
        double[] part = new double[vector.Length];
        double sum = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            part[i] = Math.Abs(Math.Min(vector[i], 0));
            sum += part[i] * part[i];
        }
        return (part, Math.Sqrt(sum));
    }

    private static double[] Column(double[] block, int rows, int columns, int column)
    {
        double[] result = new double[rows];
        for (int i = 0; i < rows; i++)
        {
            result[i] = block[(i * columns) + column];
        }
        return result;
    }

    private static double[] Row(double[] block, int columns, int row)
    {
        double[] result = new double[columns];
        Array.Copy(block, row * columns, result, 0, columns);
        return result;
    }

    private static void Snap(double[] block)
    {
        for (int i = 0; i < block.Length; i++)
        {
            if (block[i] < Epsilon)
            {
                block[i] = 0;
            }
        }
    }

    private static void Fill(double[] block, double value)
    {
        for (int i = 0; i < block.Length; i++)
        {
            if (block[i] == 0)
            {
                block[i] = value;
            }
        }
    }

    /// <summary>The mean over every cell, zeros included — <c>X.mean()</c>, not the non-zeros'.</summary>
    private static double Average(CsrMatrix matrix)
    {
        double sum = 0;
        foreach (double value in matrix.Values)
        {
            sum += value;
        }
        return sum / ((double)matrix.RowCount * matrix.ColumnCount);
    }
}
```

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release --filter "FullyQualifiedName~NndSvd"`
Expected: PASS, 15 tests (5 cases × 3 theories).

If `W_matches_scikit_learn` fails on the `nndsvda` cases only, the fill value is wrong — Step 4's
reading of `_initialize_nmf` is where to check, not the algorithm. If it fails on every case at
the same magnitude, the power-iteration count resolved differently: print
`PowerIterations(matrix, k)` for each fixture and compare against
`7 if k < 0.1 * min(shape) else 4`.

Run: `dotnet test tests/Lodestar.Decomposition.NetStandard.Tests -c Release --filter "FullyQualifiedName~NndSvd"`
Expected: the same count.

- [ ] **Step 9: Commit**

```bash
git add src/Lodestar.Decomposition tests/Lodestar.Decomposition.Tests/NndSvdTests.cs \
  tests/oracles/decomposition_nmf.json tools/generate_oracles.py
git commit -m "Start NMF from a deterministic factorization

_initialize_nmf calls randomized_svd internally, so W0 and H0 depend on the seed
-- measured, seeds 7 and 99 give different matrices. Freezing Omega decouples the
initialisation from the update loop and lets each fail on its own.

Its randomized_svd call takes its own defaults, not TruncatedSVD's: ten
oversamples and n_iter='auto', which is 7 for a small rank and 4 otherwise. The
triplet is extracted out of TruncatedSvd.Fit rather than recomputed, so the two
cannot drift apart on the sign convention.

nndsvdar is not shipped. It fills its zeros from numpy's uniform stream, which
nothing here reproduces, so it could not be checked against the reference -- and
an initialisation nobody can check is one nobody can trust.

Part of #440."
```

---

## Task 7: `Nmf` — the multiplicative updates

**Files:**

- Create: `src/Lodestar.Decomposition/NmfBetaLoss.cs`
- Create: `src/Lodestar.Decomposition/Nmf.cs`
- Create: `src/Lodestar.Decomposition/Internal/BetaDivergence.cs`
- Create: `tests/Lodestar.Decomposition.Tests/NmfTests.cs`
- Create: `docs/reference/decomposition/factorization/nmf.md`
- Create: `docs/reference/decomposition/factorization/nmf-fit.md`
- Create: `docs/reference/decomposition/factorization/nmfoptions.md`
- Create: `docs/reference/decomposition/factorization/nmfbetaloss.md`
- Create: `docs/reference/decomposition/factorization/nmfinitialization.md`
- Create: `samples/Lodestar.Sample/NmfSample.cs`
- Create: `samples/Lodestar.Sample/NmfOptionsSample.cs`
- Modify: `samples/Lodestar.Sample/DecompositionSamples.cs`
- Modify: `docs/reference/decomposition/factorization.md` (the members table)
- Modify: `tools/generate_oracles.py`
- Modify: `tests/oracles/decomposition_nmf.json` (regenerated, gains `updates`)
- Modify: `docs/equivalence.md`

**Interfaces:**

- Consumes: `NndSvd.Initialize`, `NmfInitialization`.
- Produces:
  - `public enum NmfBetaLoss { Frobenius, KullbackLeibler }`
  - `public sealed class NmfOptions` with `NmfBetaLoss BetaLoss { get; init; } = NmfBetaLoss.Frobenius`,
    `NmfInitialization Initialization { get; init; } = NmfInitialization.NndSvd`,
    `int MaxIterations { get; init; } = 200`, `double Tolerance { get; init; } = 1e-4`,
    `int Seed { get; init; }`, `double[]? RandomMatrix { get; init; }`
  - `public sealed class Nmf` with
    `public static Nmf Fit(CsrMatrix matrix, int componentCount, NmfOptions? options = null)`,
    `public static Nmf Fit(CsrMatrix matrix, double[] initialWeights, double[] initialComponents, NmfOptions? options = null)`,
    and the properties `int ComponentCount`, `int FeatureCount`, `int Iterations`,
    `double ReconstructionError`, `IReadOnlyList<double> Weights` (**W**, row-major
    `rows × componentCount`), `IReadOnlyList<double> Components` (**H**, row-major
    `componentCount × features`).
  - `internal static class BetaDivergence` with
    `internal static double Compute(CsrMatrix matrix, double[] w, double[] h, int componentCount, NmfBetaLoss loss)`
    — `_beta_divergence(..., square_root=True)`.

**Out of scope for 0.1.0, and named so the absence is a decision:** `alpha_W`/`alpha_H`
regularization (`l1_ratio` with them), `solver="cd"`, and `NMF.transform` on unseen data — which
is itself an NMF solve, not a projection.

- [ ] **Step 1: Write the oracle generator**

Append to the NMF block in `tools/generate_oracles.py`:

```python
def _nmf_update_settings() -> list[tuple[int, int, float, int, str, int, float]]:
    """rows, columns, density, k, beta loss, max_iter, tol.

    tol=0.0 disables the early stop, which makes the iteration count an input rather
    than a result -- measured, NMF then reports n_iter_ = max_iter and returns the
    identical W on two runs. One case keeps scikit-learn's default tol so the
    stopping rule itself is compared, not just the arithmetic.
    """
    return [
        (30, 12, 0.45, 3, "frobenius", 60, 0.0),
        (30, 12, 0.45, 3, "kullback-leibler", 60, 0.0),
        (48, 20, 0.30, 5, "frobenius", 40, 0.0),
        (48, 20, 0.30, 5, "kullback-leibler", 40, 0.0),
        (16, 6, 0.70, 2, "frobenius", 200, 1e-4),
    ]


def _nmf_update_cases() -> list[dict]:
    """The multiplicative updates, from a frozen W0 and H0.

    The initialisation is passed in as ``init="custom"`` so this half and the
    initialisation half fail independently: a wrong W0 breaks one corpus, not both.
    """
    from scipy.sparse import csr_matrix
    from sklearn.decomposition import NMF
    from sklearn.decomposition._nmf import _initialize_nmf

    rng = SeededRandom(SEED + 44900)
    cases = []
    for index, (rows, columns, density, k, loss, iterations, tol) in enumerate(
            _nmf_update_settings()):
        fixture = _sparse_fixture(rng, rows, columns, density)
        a = csr_matrix(
            (fixture["values"], fixture["column_indices"], fixture["row_pointers"]),
            shape=(rows, columns))

        seed = SEED + 45000 + index
        w0, h0 = _initialize_nmf(a, k, init="nndsvda", random_state=seed)

        model = NMF(n_components=k, init="custom", solver="mu", beta_loss=loss,
                    tol=tol, max_iter=iterations, random_state=seed)
        w = model.fit_transform(a, W=w0.copy(), H=h0.copy())

        cases.append({
            **fixture,
            COMPONENT_COUNT_KEY: k,
            "beta_loss": loss,
            "max_iterations": iterations,
            "tolerance": tol,
            INITIAL_W_KEY: w0.ravel().tolist(),
            INITIAL_H_KEY: h0.ravel().tolist(),
            "weights": w.ravel().tolist(),
            "components": model.components_.ravel().tolist(),
            "iterations": int(model.n_iter_),
            "reconstruction_error": float(model.reconstruction_err_),
        })
    return cases
```

and extend `generate_decomposition_nmf`:

```python
def generate_decomposition_nmf() -> dict:
    """NNDSVD, and the multiplicative updates on top of it (#440)."""
    initialization, updates = _nmf_initialization_cases(), _nmf_update_cases()
    return {"metadata": {"library": "scikit-learn", "version": version("scikit-learn"),
                         "reference_calls": ["sklearn.decomposition._nmf._initialize_nmf",
                                             "sklearn.decomposition.NMF"],
                         "seed": SEED, "count": len(initialization) + len(updates),
                         "tolerance": 1e-9},
            "initialization": initialization,
            "updates": updates}
```

- [ ] **Step 2: Regenerate and read the generator's own exit code**

```bash
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
echo "exit: $?"
```

Expected: `exit: 0`; `decomposition_nmf.json` holds `initialization` (5) and `updates` (5), and
the `initialization` half is byte-identical to Task 6's — `git diff` and confirm.

- [ ] **Step 3: Write the failing test**

`tests/Lodestar.Decomposition.Tests/NmfTests.cs`:

```csharp
using System.Text.Json;
using Lodestar.Abstractions;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// <c>NMF(solver="mu")</c> against scikit-learn 1.9.0, from the W₀ and H₀ the corpus freezes.
/// </summary>
public sealed class NmfTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("decomposition_nmf.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("updates").EnumerateArray()];

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }
        return data;
    }

    private static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    private static int[] Ints(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetInt32())];

    private static CsrMatrix Matrix(JsonElement c) => new(
        c.GetProperty("rows").GetInt32(),
        c.GetProperty("columns").GetInt32(),
        Doubles(c, "values"),
        Ints(c, "column_indices"),
        Ints(c, "row_pointers"));

    private static Nmf Fit(JsonElement c) => Nmf.Fit(
        Matrix(c),
        Doubles(c, "initial_w"),
        Doubles(c, "initial_h"),
        new NmfOptions
        {
            BetaLoss = c.GetProperty("beta_loss").GetString() == "kullback-leibler"
                ? NmfBetaLoss.KullbackLeibler
                : NmfBetaLoss.Frobenius,
            MaxIterations = c.GetProperty("max_iterations").GetInt32(),
            Tolerance = c.GetProperty("tolerance").GetDouble(),
        });

    private static void AssertSame(double[] expected, IReadOnlyList<double> actual)
    {
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_weights_match_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "weights"), Fit(c).Weights);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_components_match_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "components"), Fit(c).Components);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_iteration_count_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        Assert.Equal(c.GetProperty("iterations").GetInt32(), Fit(c).Iterations);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_reconstruction_error_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        Assert.Equal(
            c.GetProperty("reconstruction_error").GetDouble(), Fit(c).ReconstructionError, Tolerance);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Neither_factor_holds_a_negative_number(int index)
    {
        Nmf fitted = Fit(Cases[index]);

        Assert.All(fitted.Weights, value => Assert.True(value >= 0, $"W holds {value}"));
        Assert.All(fitted.Components, value => Assert.True(value >= 0, $"H holds {value}"));
    }

    [Fact]
    public void The_initialising_overload_reaches_the_same_answer()
    {
        // Fit(matrix, k) is Fit(matrix, W0, H0) with NNDSVD in front of it, and nothing else.
        JsonElement c = Cases[0];
        CsrMatrix matrix = Matrix(c);
        NmfOptions options = new()
        {
            Initialization = NmfInitialization.NndSvda,
            MaxIterations = 20,
            Tolerance = 0.0,
            Seed = 20260901,
        };

        Nmf composed = Nmf.Fit(matrix, c.GetProperty("component_count").GetInt32(), options);

        Assert.Equal(20, composed.Iterations);
        Assert.All(composed.Components, value => Assert.True(value >= 0));
    }

    [Fact]
    public void An_initialisation_of_the_wrong_shape_is_refused()
    {
        CsrMatrix matrix = Matrix(Cases[0]);

        Assert.Throws<ArgumentException>(
            () => Nmf.Fit(matrix, new double[3], new double[matrix.ColumnCount]));
    }

    [Fact]
    public void A_negative_initialisation_is_refused()
    {
        JsonElement c = Cases[0];
        CsrMatrix matrix = Matrix(c);
        double[] w = Doubles(c, "initial_w");
        w[0] = -1.0;

        Assert.Throws<ArgumentException>(() => Nmf.Fit(matrix, w, Doubles(c, "initial_h")));
    }
}
```

- [ ] **Step 4: Run the test to verify it fails**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release --filter "FullyQualifiedName~NmfTests"`
Expected: FAIL — `Nmf` does not exist.

- [ ] **Step 5: Write the implementation**

`src/Lodestar.Decomposition/NmfBetaLoss.cs`:

```csharp
namespace Lodestar.Decomposition;

/// <summary>What the factorization is asked to minimise.</summary>
/// <remarks>
/// The two are not interchangeable. Frobenius fits a Gaussian noise model and is what a
/// continuous matrix wants; Kullback–Leibler fits a Poisson one and is what counts want — a
/// term-document matrix included, which is why it is here at all.
/// </remarks>
public enum NmfBetaLoss
{
    /// <summary>Squared Frobenius norm, <c>β = 2</c>.</summary>
    Frobenius = 0,

    /// <summary>Generalised Kullback–Leibler divergence, <c>β = 1</c>.</summary>
    KullbackLeibler = 1,
}
```

`src/Lodestar.Decomposition/Internal/BetaDivergence.cs`:

```csharp
using Lodestar.Abstractions;

namespace Lodestar.Decomposition.Internal;

/// <summary>scikit-learn's <c>_beta_divergence(..., square_root=True)</c>.</summary>
/// <remarks>
/// Both branches avoid densifying <c>W H</c>: the Frobenius one expands the squared norm into
/// three traces, and the Kullback–Leibler one needs <c>W H</c> only where the matrix is non-zero
/// plus one rank-one correction for everywhere else.
/// </remarks>
internal static class BetaDivergence
{
    /// <summary><c>double.Epsilon</c> is not this: it is numpy's <c>finfo(float64).eps</c>.</summary>
    internal const double MachineEpsilon = 2.220446049250313e-16;

    internal static double Compute(
        CsrMatrix matrix, double[] w, double[] h, int componentCount, NmfBetaLoss loss)
    {
        double residual = loss == NmfBetaLoss.Frobenius
            ? Frobenius(matrix, w, h, componentCount)
            : KullbackLeibler(matrix, w, h, componentCount);

        // Rounding can push the residual just below zero on a near-perfect fit.
        return Math.Sqrt(2.0 * Math.Max(residual, 0));
    }

    private static double Frobenius(CsrMatrix matrix, double[] w, double[] h, int k)
    {
        // ||X - WH||² = ||X||² + tr(HᵀWᵀWH) - 2 tr(WᵀXHᵀ), which never forms WH.
        double normX = 0;
        foreach (double value in matrix.Values)
        {
            normX += value * value;
        }

        double[] wtw = new double[k * k];
        for (int i = 0; i < matrix.RowCount; i++)
        {
            for (int a = 0; a < k; a++)
            {
                double left = w[(i * k) + a];
                for (int b = 0; b < k; b++)
                {
                    wtw[(a * k) + b] += left * w[(i * k) + b];
                }
            }
        }

        double normWh = 0;
        int features = matrix.ColumnCount;
        for (int a = 0; a < k; a++)
        {
            for (int b = 0; b < k; b++)
            {
                double factor = wtw[(a * k) + b];
                double inner = 0;
                for (int j = 0; j < features; j++)
                {
                    inner += h[(a * features) + j] * h[(b * features) + j];
                }
                normWh += factor * inner;
            }
        }

        double cross = 0;
        for (int row = 0; row < matrix.RowCount; row++)
        {
            for (int index = matrix.RowPointers[row]; index < matrix.RowPointers[row + 1]; index++)
            {
                double value = matrix.Values[index];
                int column = matrix.ColumnIndices[index];
                for (int a = 0; a < k; a++)
                {
                    cross += value * w[(row * k) + a] * h[(a * features) + column];
                }
            }
        }

        return (normX + normWh - (2.0 * cross)) / 2.0;
    }

    private static double KullbackLeibler(CsrMatrix matrix, double[] w, double[] h, int k)
    {
        int features = matrix.ColumnCount;

        double residual = 0;
        double dataSum = 0;
        for (int row = 0; row < matrix.RowCount; row++)
        {
            for (int index = matrix.RowPointers[row]; index < matrix.RowPointers[row + 1]; index++)
            {
                double value = matrix.Values[index];
                // A zero entry contributes nothing: 0 · log(0/x) is defined as 0 here, which
                // is what skipping it means.
                if (value <= MachineEpsilon)
                {
                    continue;
                }
                int column = matrix.ColumnIndices[index];
                double product = 0;
                for (int a = 0; a < k; a++)
                {
                    product += w[(row * k) + a] * h[(a * features) + column];
                }
                residual += value * Math.Log(value / Math.Max(product, MachineEpsilon));
                dataSum += value;
            }
        }

        // Σ WH over every cell, as (Σ columns of W) · (Σ rows of H) — a rank-one identity,
        // so the zeros cost nothing.
        double sumWh = 0;
        for (int a = 0; a < k; a++)
        {
            double columnSum = 0;
            for (int i = 0; i < matrix.RowCount; i++)
            {
                columnSum += w[(i * k) + a];
            }
            double rowSum = 0;
            for (int j = 0; j < features; j++)
            {
                rowSum += h[(a * features) + j];
            }
            sumWh += columnSum * rowSum;
        }

        return residual + sumWh - dataSum;
    }
}
```

`src/Lodestar.Decomposition/Nmf.cs`:

```csharp
using Lodestar.Abstractions;
using Lodestar.Decomposition.Internal;

namespace Lodestar.Decomposition;

/// <summary>What <see cref="Nmf.Fit(CsrMatrix, int, NmfOptions)"/> is allowed to vary.</summary>
public sealed class NmfOptions
{
    /// <summary>What the factorization minimises.</summary>
    public NmfBetaLoss BetaLoss { get; init; } = NmfBetaLoss.Frobenius;

    /// <summary>Where the iteration starts. Ignored by the overload that is handed W and H.</summary>
    public NmfInitialization Initialization { get; init; } = NmfInitialization.NndSvd;

    /// <summary>The iteration cap. scikit-learn's default is 200.</summary>
    public int MaxIterations { get; init; } = 200;

    /// <summary>The relative improvement below which the iteration stops, checked every ten.</summary>
    /// <remarks>
    /// Zero disables the stop, which turns <see cref="MaxIterations"/> into an input rather than
    /// a cap — that is what the oracle corpus does, so an iteration count cannot silently differ.
    /// </remarks>
    public double Tolerance { get; init; } = 1e-4;

    /// <summary>Seeds the initialisation's own generator when <see cref="RandomMatrix"/> is null.</summary>
    public int Seed { get; init; }

    /// <summary>Ω for the initialisation, row-major <c>features × (components + 10)</c>.</summary>
    public double[]? RandomMatrix { get; init; }
}

/// <summary>A fitted non-negative matrix factorization, <c>X ≈ W H</c>.</summary>
public sealed class Nmf
{
    private readonly double[] _weights;
    private readonly double[] _components;

    private Nmf(int featureCount, int componentCount, double[] weights, double[] components,
                int iterations, double reconstructionError)
    {
        FeatureCount = featureCount;
        ComponentCount = componentCount;
        _weights = weights;
        _components = components;
        Iterations = iterations;
        ReconstructionError = reconstructionError;
    }

    /// <summary>How many components were asked for.</summary>
    public int ComponentCount { get; }

    /// <summary>How many columns the factorized matrix had.</summary>
    public int FeatureCount { get; }

    /// <summary>How many multiplicative updates ran — scikit-learn's <c>n_iter_</c>.</summary>
    public int Iterations { get; }

    /// <summary>The beta divergence at the end, square-rooted — scikit-learn's <c>reconstruction_err_</c>.</summary>
    public double ReconstructionError { get; }

    /// <summary><c>W</c>, row-major rows × <see cref="ComponentCount"/>: each row's mix of components.</summary>
    public IReadOnlyList<double> Weights => _weights;

    /// <summary><c>H</c>, row-major <see cref="ComponentCount"/> × <see cref="FeatureCount"/> — scikit-learn's <c>components_</c>.</summary>
    public IReadOnlyList<double> Components => _components;

    /// <summary>Factorizes <paramref name="matrix"/>, initialising it with the NNDSVD family.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="matrix"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="componentCount"/> is not in <c>[1, matrix.ColumnCount)</c>, or an option is out of range.</exception>
    public static Nmf Fit(CsrMatrix matrix, int componentCount, NmfOptions? options = null)
    {
        if (matrix is null)
        {
            throw new ArgumentNullException(nameof(matrix));
        }
        NmfOptions settings = options ?? new NmfOptions();
        if (componentCount < 1 || componentCount >= matrix.ColumnCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(componentCount), componentCount,
                $"A factorization keeps between 1 and {matrix.ColumnCount - 1} components.");
        }

        (double[] w, double[] h) = NndSvd.Initialize(
            matrix, componentCount, settings.Initialization, settings.Seed, settings.RandomMatrix);
        return Fit(matrix, w, h, settings);
    }

    /// <summary>Factorizes <paramref name="matrix"/> from an initialisation you supply.</summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">The two blocks do not agree on a component count, do not fit the matrix, or hold a negative number.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An option is out of range.</exception>
    public static Nmf Fit(
        CsrMatrix matrix, double[] initialWeights, double[] initialComponents,
        NmfOptions? options = null)
    {
        if (matrix is null)
        {
            throw new ArgumentNullException(nameof(matrix));
        }
        if (initialWeights is null)
        {
            throw new ArgumentNullException(nameof(initialWeights));
        }
        if (initialComponents is null)
        {
            throw new ArgumentNullException(nameof(initialComponents));
        }
        NmfOptions settings = options ?? new NmfOptions();
        if (settings.MaxIterations < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options), settings.MaxIterations, "MaxIterations is at least one.");
        }
        if (settings.Tolerance < 0 || double.IsNaN(settings.Tolerance))
        {
            throw new ArgumentOutOfRangeException(
                nameof(options), settings.Tolerance, "Tolerance is not negative.");
        }

        int features = matrix.ColumnCount;
        int componentCount = ComponentCountOf(matrix, initialWeights, initialComponents, features);
        RequireNonNegative(initialWeights, nameof(initialWeights));
        RequireNonNegative(initialComponents, nameof(initialComponents));

        double[] w = (double[])initialWeights.Clone();
        double[] h = (double[])initialComponents.Clone();

        double initial = BetaDivergence.Compute(matrix, w, h, componentCount, settings.BetaLoss);
        double previous = initial;
        int iteration = 0;
        while (iteration < settings.MaxIterations)
        {
            iteration++;
            MultiplicativeUpdates.UpdateWeights(matrix, w, h, componentCount, settings.BetaLoss);
            MultiplicativeUpdates.UpdateComponents(matrix, w, h, componentCount, settings.BetaLoss);

            // scikit-learn checks every tenth iteration, never on the others: checking more
            // often would stop earlier, on the same data, for no reason a caller can see.
            if (settings.Tolerance > 0 && iteration % 10 == 0)
            {
                double error = BetaDivergence.Compute(
                    matrix, w, h, componentCount, settings.BetaLoss);
                if ((previous - error) / initial < settings.Tolerance)
                {
                    break;
                }
                previous = error;
            }
        }

        double final = BetaDivergence.Compute(matrix, w, h, componentCount, settings.BetaLoss);
        return new Nmf(features, componentCount, w, h, iteration, final);
    }

    private static int ComponentCountOf(
        CsrMatrix matrix, double[] weights, double[] components, int features)
    {
        if (weights.Length % matrix.RowCount != 0)
        {
            throw new ArgumentException(
                $"W is {weights.Length} long, which is not a multiple of {matrix.RowCount} rows.",
                nameof(weights));
        }
        int componentCount = weights.Length / matrix.RowCount;
        if (componentCount < 1 || components.Length != (long)componentCount * features)
        {
            throw new ArgumentException(
                $"W implies {componentCount} components, so H must be {componentCount} × {features}; " +
                $"it is {components.Length} long.",
                nameof(components));
        }
        return componentCount;
    }

    private static void RequireNonNegative(double[] block, string name)
    {
        foreach (double value in block)
        {
            if (!(value >= 0))
            {
                throw new ArgumentException(
                    $"A non-negative factorization cannot start from {value}.", name);
            }
        }
    }
}
```

`src/Lodestar.Decomposition/Internal/MultiplicativeUpdates.cs`:

```csharp
using Lodestar.Abstractions;

namespace Lodestar.Decomposition.Internal;

/// <summary>Lee and Seung's multiplicative updates, in scikit-learn's <c>solver="mu"</c> form.</summary>
/// <remarks>
/// <para>
/// Each factor is scaled by a ratio rather than moved by a step, which is what keeps it
/// non-negative with no projection and no line search — and what makes a zero permanent, so the
/// initialisation decides the sparsity of the answer.
/// </para>
/// <para>
/// W is updated first and H second, against the already-updated W. Doing both against the old
/// pair is a different algorithm and converges more slowly.
/// </para>
/// </remarks>
internal static class MultiplicativeUpdates
{
    internal static void UpdateWeights(
        CsrMatrix matrix, double[] w, double[] h, int k, NmfBetaLoss loss)
    {
        int features = matrix.ColumnCount;
        double[] numerator;
        double[] denominator;

        if (loss == NmfBetaLoss.Frobenius)
        {
            numerator = MatrixTimesTranspose(matrix, h, k);          // X Hᵀ
            double[] hht = Gram(h, k, features);                     // H Hᵀ
            denominator = DenseProduct(w, matrix.RowCount, k, hht, k);
        }
        else
        {
            // WH is needed only where X is non-zero, and the ratio X/WH replaces it there.
            double[] ratio = SparseRatio(matrix, w, h, k);
            numerator = SparsePatternTimesTranspose(matrix, ratio, h, k);
            double[] rowSums = RowSums(h, k, features);
            denominator = new double[w.Length];
            for (int i = 0; i < matrix.RowCount; i++)
            {
                for (int a = 0; a < k; a++)
                {
                    denominator[(i * k) + a] = rowSums[a];
                }
            }
        }

        Scale(w, numerator, denominator);
    }

    internal static void UpdateComponents(
        CsrMatrix matrix, double[] w, double[] h, int k, NmfBetaLoss loss)
    {
        int features = matrix.ColumnCount;
        double[] numerator;
        double[] denominator;

        if (loss == NmfBetaLoss.Frobenius)
        {
            numerator = TransposeTimesMatrix(matrix, w, k);           // Wᵀ X
            double[] wtw = Gram(DenseBlock.Transpose(w, matrix.RowCount, k), k, matrix.RowCount);
            denominator = DenseProduct(wtw, k, k, h, features);
        }
        else
        {
            double[] ratio = SparseRatio(matrix, w, h, k);
            numerator = TransposeTimesSparsePattern(matrix, ratio, w, k);
            double[] columnSums = ColumnSums(w, matrix.RowCount, k);
            denominator = new double[h.Length];
            for (int a = 0; a < k; a++)
            {
                for (int j = 0; j < features; j++)
                {
                    denominator[(a * features) + j] = columnSums[a];
                }
            }
        }

        Scale(h, numerator, denominator);

        // scikit-learn snaps H below machine epsilon to zero for β ≤ 1, and only there.
        if (loss == NmfBetaLoss.KullbackLeibler)
        {
            for (int i = 0; i < h.Length; i++)
            {
                if (h[i] < BetaDivergence.MachineEpsilon)
                {
                    h[i] = 0;
                }
            }
        }
    }

    /// <summary><c>X / (W H)</c> at X's non-zeros, floored so the division cannot blow up.</summary>
    private static double[] SparseRatio(CsrMatrix matrix, double[] w, double[] h, int k)
    {
        int features = matrix.ColumnCount;
        double[] ratio = new double[matrix.Values.Length];
        for (int row = 0; row < matrix.RowCount; row++)
        {
            for (int index = matrix.RowPointers[row]; index < matrix.RowPointers[row + 1]; index++)
            {
                int column = matrix.ColumnIndices[index];
                double product = 0;
                for (int a = 0; a < k; a++)
                {
                    product += w[(row * k) + a] * h[(a * features) + column];
                }
                ratio[index] = matrix.Values[index] / Math.Max(product, BetaDivergence.MachineEpsilon);
            }
        }
        return ratio;
    }

    private static double[] MatrixTimesTranspose(CsrMatrix matrix, double[] h, int k) =>
        SparsePatternTimesTranspose(matrix, matrix.Values, h, k);

    /// <summary><c>S Hᵀ</c> where S shares the matrix's sparsity and carries <paramref name="data"/>.</summary>
    private static double[] SparsePatternTimesTranspose(
        CsrMatrix matrix, double[] data, double[] h, int k)
    {
        int features = matrix.ColumnCount;
        double[] result = new double[checked(matrix.RowCount * k)];
        for (int row = 0; row < matrix.RowCount; row++)
        {
            for (int index = matrix.RowPointers[row]; index < matrix.RowPointers[row + 1]; index++)
            {
                double value = data[index];
                int column = matrix.ColumnIndices[index];
                for (int a = 0; a < k; a++)
                {
                    result[(row * k) + a] += value * h[(a * features) + column];
                }
            }
        }
        return result;
    }

    private static double[] TransposeTimesMatrix(CsrMatrix matrix, double[] w, int k) =>
        TransposeTimesSparsePattern(matrix, matrix.Values, w, k);

    /// <summary><c>Wᵀ S</c> where S shares the matrix's sparsity and carries <paramref name="data"/>.</summary>
    private static double[] TransposeTimesSparsePattern(
        CsrMatrix matrix, double[] data, double[] w, int k)
    {
        int features = matrix.ColumnCount;
        double[] result = new double[checked(k * features)];
        for (int row = 0; row < matrix.RowCount; row++)
        {
            for (int index = matrix.RowPointers[row]; index < matrix.RowPointers[row + 1]; index++)
            {
                double value = data[index];
                int column = matrix.ColumnIndices[index];
                for (int a = 0; a < k; a++)
                {
                    result[(a * features) + column] += w[(row * k) + a] * value;
                }
            }
        }
        return result;
    }

    /// <summary><c>B Bᵀ</c> for a row-major <c>rows × columns</c> block.</summary>
    private static double[] Gram(double[] block, int rows, int columns)
    {
        double[] result = new double[rows * rows];
        for (int a = 0; a < rows; a++)
        {
            for (int b = 0; b < rows; b++)
            {
                double sum = 0;
                for (int j = 0; j < columns; j++)
                {
                    sum += block[(a * columns) + j] * block[(b * columns) + j];
                }
                result[(a * rows) + b] = sum;
            }
        }
        return result;
    }

    private static double[] DenseProduct(
        double[] left, int leftRows, int inner, double[] right, int rightColumns)
    {
        double[] result = new double[checked(leftRows * rightColumns)];
        for (int i = 0; i < leftRows; i++)
        {
            for (int t = 0; t < inner; t++)
            {
                double value = left[(i * inner) + t];
                for (int j = 0; j < rightColumns; j++)
                {
                    result[(i * rightColumns) + j] += value * right[(t * rightColumns) + j];
                }
            }
        }
        return result;
    }

    private static double[] RowSums(double[] block, int rows, int columns)
    {
        double[] sums = new double[rows];
        for (int i = 0; i < rows; i++)
        {
            double sum = 0;
            for (int j = 0; j < columns; j++)
            {
                sum += block[(i * columns) + j];
            }
            sums[i] = sum;
        }
        return sums;
    }

    private static double[] ColumnSums(double[] block, int rows, int columns)
    {
        double[] sums = new double[columns];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                sums[j] += block[(i * columns) + j];
            }
        }
        return sums;
    }

    /// <summary><c>factor *= numerator / denominator</c>, with a zero denominator floored.</summary>
    private static void Scale(double[] factor, double[] numerator, double[] denominator)
    {
        for (int i = 0; i < factor.Length; i++)
        {
            double bottom = denominator[i] == 0 ? BetaDivergence.MachineEpsilon : denominator[i];
            factor[i] *= numerator[i] / bottom;
        }
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release --filter "FullyQualifiedName~NmfTests"`
Expected: PASS, 28 tests (5 cases × 5 theories, plus 3 facts).

Two failures have a known cause and neither is fixed by loosening the tolerance:

- **The Frobenius cases pass and the Kullback–Leibler ones do not.** The KL denominator is a
  broadcast row (`H`'s row sums for W, `W`'s column sums for H), not a matrix product. Re-read
  `_multiplicative_update_w` for `beta_loss == 1`.
- **Every case is close and drifts with the iteration count.** W is being updated against a stale
  H, or H against a stale W. scikit-learn updates W first and H second, against the new W.

Run: `dotnet test tests/Lodestar.Decomposition.NetStandard.Tests -c Release --filter "FullyQualifiedName~NmfTests"`
Expected: the same count.

- [ ] **Step 7: Write the reference pages, the samples and the equivalence rows**

Five pages under `docs/reference/decomposition/factorization/`, in the shape Task 5 established.
`nmf-fit.md` declares **both overloads in one fence**, because the gate groups by method name:

```csharp
public static Nmf Fit(CsrMatrix matrix, int componentCount, NmfOptions options)
public static Nmf Fit(CsrMatrix matrix, double[] initialWeights, double[] initialComponents, NmfOptions options)
```

`nmf.md` is the type page — a **Properties** table for `ComponentCount`, `FeatureCount`,
`Iterations`, `ReconstructionError`, `Weights` (W) and `Components` (H), each with its shape and
its scikit-learn name, then a **Members** table pointing at `nmf-fit.md`.
`nmfinitialization.md` says in one sentence why `nndsvdar` is absent, and links
[ADR 0072](../../decisions/0072-omega-is-an-input-not-a-seed.md).
`docs/reference/decomposition/factorization.md` gains the three new types in its table.

`samples/Lodestar.Sample/NmfSample.cs` — factorize a small non-negative `CsrMatrix` at rank 2 and
print `Iterations`, `ReconstructionError` and the two components. `NmfOptionsSample.cs` — the same
matrix under both `NmfBetaLoss` values, printing both reconstruction errors, which is the point
the enum's documentation makes. `DecompositionSamples.Run()` calls both after the SVD pair.

`docs/equivalence.md` — extend the `Lodestar.Decomposition` section:

```markdown
## Lodestar.Decomposition — non-negative matrix factorization

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
| `NMF(n_components=k, solver="mu", init="nndsvd").fit(X)` | scikit-learn | [`Nmf.Fit(matrix, k)`](reference/decomposition/factorization/nmf-fit.md) | Ω is an input, as for the SVD. `nndsvdar` is not offered — it draws from numpy's uniform stream, so it could not be checked ([decision 0072](decisions/0072-omega-is-an-input-not-a-seed.md)). `solver="cd"` and the `alpha_W`/`alpha_H` regularization are out of scope for 0.1.0. |
| `NMF(init="custom").fit_transform(X, W=W0, H=H0)` | scikit-learn | [`Nmf.Fit(matrix, initialWeights, initialComponents)`](reference/decomposition/factorization/nmf-fit.md) | Identical. `tol = 0` makes the iteration count an input, which is what the corpus freezes. |
| `nmf.components_`, `nmf.n_iter_`, `nmf.reconstruction_err_` | scikit-learn | `Components`, `Iterations`, `ReconstructionError` | Identical. `Weights` is what `fit_transform` returns. |
| `beta_loss="frobenius" \| "kullback-leibler"` | scikit-learn | [`NmfBetaLoss`](reference/decomposition/factorization/nmfbetaloss.md) | Both, at `solver="mu"`. Other β values are not offered. |
```

- [ ] **Step 8: Run the whole suite and the sample-coverage check**

Run: `dotnet test tests/Lodestar.Decomposition.Tests -c Release`
Expected: PASS, and the documentation gate is green on the new pages.

Run: `python3 tools/check_sample_coverage.py`
Expected: `ok`. Every public class of `Lodestar.Decomposition` now has its own `*Sample.cs`;
`NmfBetaLoss`, `NmfInitialization` and `PowerIterationNormalizer` are enums and are exempt.

- [ ] **Step 9: Commit**

```bash
git add src/Lodestar.Decomposition tests/Lodestar.Decomposition.Tests/NmfTests.cs \
  tests/oracles/decomposition_nmf.json tools/generate_oracles.py \
  docs/reference/decomposition docs/equivalence.md samples/Lodestar.Sample
git commit -m "Factorize without a sign, at scikit-learn parity on both losses

The initialisation is passed to scikit-learn as init='custom' so the two halves
of the corpus fail independently: a wrong W0 breaks one and not both.

tol=0 disables the early stop, which turns the iteration count into an input --
measured, NMF then reports n_iter_ = max_iter and returns the identical W twice.
One case keeps the default tol so the stopping rule is compared too, not only the
arithmetic.

Neither loss densifies W H. Frobenius expands the squared norm into three traces;
Kullback-Leibler needs the product only where the matrix is non-zero, plus one
rank-one correction for the sum over everywhere else.

W is updated first and H second, against the already-updated W. Updating both
against the old pair is a different algorithm.

Part of #440."
```

---

## Task 8: The benchmark, against the incumbent V4 restated

**Files:**

- Create: `bench/Lodestar.Text.Benchmarks/DecompositionBenchmarks.cs`
- Modify: `bench/Lodestar.Text.Benchmarks/Lodestar.Text.Benchmarks.csproj`
- Modify: `bench/bench-map.json`
- Modify: `bench/README.md`

**Interfaces:**

- Consumes: `TruncatedSvd.Fit`, `Nmf.Fit`, `CsrMatrix`.
- Produces: a BenchmarkDotNet class `DecompositionBenchmarks` with the methods
  `TruncatedSvd_Rank20`, `Nmf_Rank20` and `MlNet_ProjectToPrincipalComponents_Rank20`.

`bench/Lodestar.Text.Benchmarks` is where every non-netstandard benchmark lives regardless of the
package it measures — `BatchEmbeddingBenchmarks` and `MetricsCrossLang` are already there — and it
already references `Microsoft.ML` 5.0.0. A new project would buy a name and cost a solution entry,
a CI wiring and a second BenchmarkDotNet configuration.

**The comparison is not like-for-like, and the section says so before it reports a ratio.** ML.NET
computes **centred** PCA at a **fixed rank of 20** over an `IDataView`; this computes **uncentred**
truncated SVD at a rank the caller names, over a `CsrMatrix`. Agreement cannot be checked between
two different decompositions, so what is checked instead is that each side reconstructs its own
input to its own stated error.

**No numbers are published from a container.** Per
[decision 0051](../../decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md), a run
on a shared cloud container is not the machine `docs/guides/performance.md` reports; this task
ships the harness and the section that says how to run it, and
`docs/guides/performance.md` gets its row when the numbers are taken on a named machine. Leaving
the row out is the honest state, not an omission.

- [ ] **Step 1: Reference the package from the benchmark project**

`bench/Lodestar.Text.Benchmarks/Lodestar.Text.Benchmarks.csproj` — one line in the
`ProjectReference` group:

```xml
    <ProjectReference Include="../../src/Lodestar.Decomposition/Lodestar.Decomposition.csproj" />
```

- [ ] **Step 2: Write the benchmark**

`bench/Lodestar.Text.Benchmarks/DecompositionBenchmarks.cs`, following the existing classes'
shape (a `[GlobalSetup]` that builds the corpus once, `[Benchmark]` methods that do the work and
nothing else). Build one sparse term-document matrix in `GlobalSetup` — 2 000 rows × 500 columns
at 2 % density, from a fixed seed so two runs measure the same matrix — and its dense `float[][]`
twin for ML.NET, which cannot take a sparse one. Then:

```csharp
    [Benchmark(Baseline = true)]
    public int TruncatedSvd_Rank20() =>
        TruncatedSvd.Fit(_matrix, 20, new TruncatedSvdOptions { Seed = 20260901 }).ComponentCount;

    [Benchmark]
    public int Nmf_Rank20() =>
        Nmf.Fit(_matrix, 20, new NmfOptions { Seed = 20260901, MaxIterations = 50 }).Iterations;

    [Benchmark]
    public int MlNet_ProjectToPrincipalComponents_Rank20() { /* the ML.NET pipeline, fit once */ }
```

Add `#pragma warning disable CA1707` only if the benchmark-area `NoWarn` in
`bench/Directory.Build.props` does not already cover the underscored names — it does; check rather
than add.

- [ ] **Step 3: Name it in `bench/bench-map.json`**

`tools/check_bench_map.py` refuses a BenchmarkDotNet class this file does not name. Add to
`benchmarks`, keeping the object's alphabetical order:

```json
    "DecompositionBenchmarks": ["src/Lodestar.Decomposition/**"],
```

- [ ] **Step 4: Run the benchmark once, to prove the harness works**

Run: `dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- --filter '*Decomposition*'`
Expected: three rows, no crash. **Do not copy these numbers into any document** — this is a
correctness run of the harness, not a measurement.

Run: `python3 tools/check_bench_map.py`
Expected: `ok`.

- [ ] **Step 5: Write the `bench/README.md` section**

A new numbered section at the end, in the shape of the existing ones: what is measured, the exact
command, the corpus, and — before anything else — the paragraph above on why the comparison is not
like-for-like and what is checked instead of agreement. Say explicitly that
`docs/guides/performance.md` carries no row for it yet, and that a row lands with the machine it
was taken on.

- [ ] **Step 6: Commit**

```bash
git add bench/ && git commit -m "Measure the decomposition against the incumbent, and say what differs

ML.NET's ProjectToPrincipalComponents centres, fixes the rank at 20 and takes an
IDataView; this is uncentred, takes the rank as a parameter and reads a CsrMatrix.
Agreement between two different decompositions cannot be checked, so what is
checked is that each side reconstructs its own input to its own stated error --
the shape #438's harness already uses for FeaturizeText.

The harness ships; the numbers do not. A shared container is not the machine
docs/guides/performance.md reports (decision 0051), so the row lands with the
machine it was taken on.

Part of #440."
```

---

## Task 9: The guide, the decision record, and the release notes

**Files:**

- Create: `docs/guides/decomposition.md`
- Create: `docs/decisions/0072-omega-is-an-input-not-a-seed.md`
- Modify: `docs/decisions/README.md`
- Modify: `docs/wiki-map.json`
- Modify: `CHANGELOG.md`
- Modify: `README.md`
- Modify: `docs/migration/numpy.md`

**Interfaces:**

- Consumes: every public member Tasks 5 and 7 produced.
- Produces: no code.

- [ ] **Step 1: Write ADR 0072**

`docs/decisions/0072-omega-is-an-input-not-a-seed.md`, in the shape of
`0070-k-greater-than-n-returns-an-infinite-interval.md`: `**Status:** accepted · **Date:** 2026-09-01`,
a **Context**, an **Options** section with the loser named, a **Decision**, and **Consequences**.

It records three divergences from scikit-learn that share one cause — the random matrix:

1. **Ω is a parameter, and a `Seed` drives Lodestar's own generator.** Reproducing numpy's
   `RandomState.normal` would mean MT19937 and numpy's cached-polar Gaussian. The measurement that
   makes this affordable: over the *same* Ω, with `power_iteration_normalizer="QR"` and
   `transpose=False`, a step-by-step reimplementation reproduces `randomized_svd`'s `U`, `s` and
   `Vᵀ` to **exactly 0.0** — not to a tolerance — on a 40×25 fixture at `k=4, p=6, n_iter=3`. Ω
   being an input is what turns a randomized algorithm into an ordinary parity target.
2. **`transpose="auto"` is not offered.** It swaps the two products when there are fewer rows than
   columns, which a term-document matrix routinely has, so a flag that silently changes which
   factorization runs is a parity claim with two shapes.
3. **`nndsvdar` is not offered.** It fills its zeros from numpy's uniform stream, so it inherits
   (1)'s cost with none of (1)'s escape hatch — there is no "pass the noise in" that a caller
   would ever use.

The runner-up, named and refused: **reproducing MT19937**, which would make a seed portable
between the two ecosystems. Refused because the API already accepts Ω explicitly, that is what the
corpus passes, and a bug in a hand-written MT19937 would surface as a wrong factorization rather
than as a wrong random number.

- [ ] **Step 2: Update the decisions index**

`docs/decisions/README.md` — a row for 0072 in the table, and **both** spellings of the count:
line 89's *All seventy-one carry `accepted`* becomes *seventy-two*, and line 92's *the other
seventy* becomes *seventy-one*. Grep for both words before committing:

```bash
grep -n "seventy" docs/decisions/README.md
```

Expected: exactly the two lines, both updated. ADRs are immutable once accepted and on `main`, so
0069 and 0071 are **not** edited — 0072 stands beside them.

Run: `python3 tools/check_adr_immutable.py --base origin/main`
Expected: `ok`.

- [ ] **Step 3: Write the guide**

`docs/wiki-map.json` — the guide now exists, so it joins `Lodestar.Decomposition`'s `pages`
above the two reference globs. Task 1 deliberately left it out: `build_wiki.py` hard-fails a
literal path that is not in the tree, and `tools/tests/test_build_wiki.py` goes red on it.

```json
        "docs/guides/decomposition.md",
```

`docs/guides/decomposition.md`, in the shape of `docs/guides/conformal.md`. Its ` ```csharp `
fences are compiled by the doc-snippets gate, so every one must build against the packed packages.
Cover, in this order:

1. **What this is for** — uncentred LSA over a term-document matrix, and one sentence on why
   centring is the thing it deliberately does not do: it destroys the sparsity that made the
   matrix worth storing.
2. **From a `CountVectorizer` to a rank-20 projection** — the end-to-end snippet. It needs
   `Lodestar.Text`, which the doc-snippets project already references.
3. **Reading the explained-variance ratio** — that the ratios sum to less than one by
   construction, and that the sum is how a caller chooses the rank.
4. **`## Reproducing a scikit-learn run`** — pass `RandomMatrix`, not `Seed`, and why; link
   [ADR 0072](../decisions/0072-omega-is-an-input-not-a-seed.md).
5. **NMF, and which loss** — Frobenius for a continuous matrix, Kullback–Leibler for counts.
6. **What is not here in 0.1.0** — PCA, `solver="cd"`, regularization, persistence of a fitted
   model, `nndsvdar`.

Every member the guide names must be linked to its reference entry at least once on the page —
that is the link rule the gate enforces, and it is the one thing a reviewer will not catch.

Run: `python3 tools/extract_doc_snippets.py && dotnet build samples/Lodestar.DocSnippets -c Release`
Expected: builds clean. A snippet that ends on an unread local is fine (`CS0219` is suppressed
there); an API that moved is not.

- [ ] **Step 4: Update `CHANGELOG.md`, `README.md` and `docs/migration/numpy.md`**

`CHANGELOG.md` — under `## [Unreleased]`, a **new** `### Lodestar.Decomposition` section with an
`#### Added` heading. MD024 refuses a duplicate heading at the same level, so add the section once
and put both entries under it rather than repeating the package heading:

```markdown
### Lodestar.Decomposition

#### Added

- **`TruncatedSvd` — `sklearn.decomposition.TruncatedSVD(algorithm="randomized")` at parity, over a `CsrMatrix` and without centring it.** Fit, transform, components, singular values, explained variance and its ratio; all three power-iteration normalizers, including `Auto`'s rule. Ω is an input rather than a seed, which is what makes a randomized algorithm an ordinary parity target — [decision 0072](docs/decisions/0072-omega-is-an-input-not-a-seed.md) has the measurement and what it refuses. ([#440](https://github.com/CyrilB1531/lodestar/issues/440))

- **`Nmf` — `sklearn.decomposition.NMF(solver="mu")` at parity, on both β losses, from the NNDSVD family.** The dense kernels it needs — thin Householder QR, LU with partial pivoting, one-sided Jacobi SVD — are written here, so the package's only dependency is `Lodestar.Abstractions`. ([#440](https://github.com/CyrilB1531/lodestar/issues/440))
```

Also extend the preamble's package list: it says *six packages*; there are now **seven**.

`README.md` — a row in the package table for `Lodestar.Decomposition`, and the intro sentence's
package count if it names one.

`docs/migration/numpy.md` — its row says decompositions are delegated to Math.NET Numerics. That
is still true of a general dense linear-algebra need and is **not** rewritten; add a sentence
beside it saying that truncated SVD and NMF over a sparse matrix now ship in
`Lodestar.Decomposition`, with why Math.NET was refused for them (5.0.0, April 2022, nothing but a
beta in four years; its sparse SVD request open since 2013).

- [ ] **Step 5: Verify the whole documentation surface**

Run: `dotnet test Lodestar.slnx -c Release`
Expected: PASS. The reference and link gates run in both `Lodestar.Decomposition` test projects,
and `tools/tests/test_build_wiki.py::test_no_link_in_the_published_wiki_names_a_page_it_does_not_hold`
covers the whole tree — including the pages that did **not** move, which is where the last page
move's broken links hid.

Run: `npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"`
Expected: clean.

- [ ] **Step 6: Commit**

```bash
git add docs CHANGELOG.md README.md
git commit -m "Lead with the assumption, then the API

Three divergences from scikit-learn share one cause -- the random matrix -- so
they get one decision record rather than three: Omega is a parameter, transpose
is not offered, and nndsvdar is not shipped. The measurement that makes the first
affordable is that over the same Omega the two implementations agree to exactly
0.0, not to a tolerance.

Reproducing MT19937 is named as the runner-up and refused: the API already takes
Omega, and a bug in a hand-written Mersenne Twister would surface as a wrong
factorization rather than as a wrong random number.

The guide leads with what centring would cost a sparse matrix, because that is
the reason to reach for this rather than for PCA.

Part of #440."
```

---

## Before the pull request

These run **once**, on the finished branch, not inside each task.

- [ ] `dotnet build Lodestar.slnx -c Release` — both frameworks, warnings are errors.
- [ ] `dotnet test Lodestar.slnx -c Release` — the whole suite, twice over. Read the counts.
- [ ] `dotnet format Lodestar.slnx --verify-no-changes`
- [ ] `python3 tools/check_version_floor.py --check-feed` — `Lodestar.Abstractions` 0.1.1 must
      resolve from nuget.org for both dependents.
- [ ] `python3 tools/check_machine_paths.py`
- [ ] `python3 tools/check_sample_culture.py`
- [ ] `python3 tools/check_sample_coverage.py`
- [ ] `python3 tools/check_bench_map.py`
- [ ] `python3 tools/check_adr_immutable.py --base origin/main`
- [ ] `python3 tools/check_repeated_literals.py --base origin/main` — four occurrences of a literal
      trip S1192, and this branch adds a lot of row-major index arithmetic and a lot of JSON keys.
- [ ] `python3 tools/check_comment_length.py` — two lines inline, eight of prose in XML
      documentation; `<remarks>` and `<para>` tag lines count.
- [ ] `python3 -m pytest tools/tests -q`
- [ ] `npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"`
- [ ] `python3 tools/extract_doc_snippets.py && dotnet build samples/Lodestar.DocSnippets -c Release`
- [ ] Pack and run the sample against the packed feed, with an isolated `NUGET_PACKAGES`, or it
      judges the published packages instead of the working tree (ADR 0009):

```bash
rm -rf ./artifacts
for p in src/Lodestar.Abstractions src/Lodestar.Text src/Lodestar.Embeddings \
         src/Lodestar.Fuzzy src/Lodestar.Metrics src/Lodestar.Conformal \
         src/Lodestar.Decomposition; do
  dotnet pack "$p" -c Release -o ./artifacts
done
python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
NUGET_PACKAGES=$(mktemp -d) dotnet run -c Release --project samples/Lodestar.Sample
```

- [ ] Regenerate the corpora one last time from a neutral directory and confirm **no drift**:

```bash
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
echo "exit: $?"
cd <repo> && git status --short tests/oracles
```

Expected: `exit: 0` and no modified file. The `Oracles are reproducible` CI job is occasionally
flaky — the same commit has gone red then green — so re-run before believing a failure, and use
the artefact it uploads to compare off the runner.

- [ ] Sonar: `toggle_automatic_analysis` off at the start of the pass, `analyze_file_list` on every
      file this branch created or modified, then re-enable. Clear the findings **before**
      committing the fix for them, not after. Do not try to confirm a fix through
      `search_sonar_issues_in_projects`; the server will not reflect it yet.
- [ ] `Closes #440` belongs in the pull-request body only if this lot closes the issue. It does
      not — lots 1, 2 and 4 are still blocked on restated gaps — so the body says `Part of #440`
      and names what remains.

**Release.** `Lodestar.Decomposition` 0.1.0 is tagged `Lodestar.Decomposition/v0.1.0` and published
through the manual `release-nuget-org.yml` dispatch. Neither is this branch's work: tagging and
publishing are the maintainer's, and nothing in this plan waits on them — the branch builds
against `Lodestar.Abstractions` 0.1.1, which is already on nuget.org.

---

## Self-Review

**Spec coverage.** Every section of the spec maps to a task:

| spec section | task |
| --- | --- |
| `TruncatedSvd`, with explained variance and its ratio | 5 |
| `transpose="auto"` not offered | 5 (fixtures), 9 (ADR 0072) |
| `Nmf`, `solver="mu"`, both β losses, NNDSVD family | 6, 7 |
| The five dense kernels (two shipped in step A) | 2, 3, 4 |
| All three normalizers, matching `auto` | 5 |
| Ω frozen as an input; a seed drives Lodestar's own generator | 1 (the generator), 5, 9 (ADR 0072) |
| `W₀, H₀` frozen as inputs | 6, 7 |
| Core tier, one edge, `net10.0;netstandard2.0` | 1 |
| `decomposition_qr.json`, `decomposition_lu.json`, `decomposition_svd.json`, `decomposition_nmf.json` | 2, 3, 4/5, 6/7 |
| Both target frameworks run the same suite | 1, and every task after it |
| Benchmark against ML.NET, stated as not like-for-like | 8 |
| Rejected options recorded | 9 (ADR 0072); Math.NET, CSparse and the namespace-inside-`Lodestar.Text` option stay in the spec, which is their record |

Two spec claims are narrowed here, and both are narrowings the spec's own *Rejected* section
implies rather than contradicts:

- **The NNDSVD "family" ships as two members, not three.** `nndsvdar` draws from numpy's uniform
  stream, which the spec refuses to reproduce. Recorded in ADR 0072 and on the enum's page.
- **`decomposition_svd.json` carries a `dense` half** the spec's corpus list does not name, so the
  one-sided Jacobi SVD has a test of its own. The spec asks for exactly this property — "a failure
  in the composed algorithm has somewhere smaller to land" — of the QR and LU corpora; extending
  it to the third kernel is the same argument.

**Placeholder scan.** No "TBD", no "add appropriate error handling", no "similar to Task N". Three
steps describe a document rather than transcribe it — Task 5's reference pages, Task 8's
`bench/README.md` section and Task 9's guide — and each names its model file, its required
sections in order, and the gate that will reject it. Task 8's `MlNet_ProjectToPrincipalComponents_Rank20`
body is the one code block left to the implementer: the ML.NET pipeline is API the implementer
must read from the installed 5.0.0, and a made-up one here would be worse than none.

**Type consistency.**

- `PowerIterationNormalizer` is `Auto/None/Qr/Lu` everywhere, including the corpus's
  `"auto"/"none"/"QR"/"LU"` mapping in Task 5's test.
- `RandomizedSvd.Compute` is introduced in Task 6 by extraction from Task 5's `Fit`, and Task 6
  states the resulting `Fit` body. `TruncatedSvd.Product` moves with it and does not survive in two
  places.
- `NndSvd.Initialize` takes `(matrix, componentCount, initialization, seed, randomMatrix)` in both
  Task 6's test and Task 7's `Nmf.Fit`.
- `BetaDivergence.MachineEpsilon` is used by `BetaDivergence`, `MultiplicativeUpdates` and nothing
  else, and is numpy's `finfo(float64).eps`, not `double.Epsilon`.
- Row-major everywhere, width beside the array, `(i, j)` at `i * width + j`. `Components` is
  `k × features` in both `TruncatedSvd` and `Nmf`; `Weights` is `rows × k`.
- The oracle key names are shared constants in the generator (`ROWS_KEY`, `COLUMNS_KEY`,
  `MATRIX_KEY`, `OMEGA_KEY`, `COMPONENT_COUNT_KEY`, `INITIAL_W_KEY`, `INITIAL_H_KEY`) and string
  literals in the C# theories, where each appears at most three times — below S1192's threshold of
  four.

---

## Execution Handoff

Plan complete and saved to
`docs/superpowers/plans/2026-09-01_0440_decomposition-svd-and-nmf.md`. Two execution options:

**1. Subagent-Driven (recommended)** — a fresh subagent per task, a review between tasks, fast
iteration.

**2. Inline Execution** — the tasks run in this session under `executing-plans`, with checkpoints.

Which approach?
