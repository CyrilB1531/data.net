# `Lodestar.Abstractions` (step A) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship `Lodestar.Abstractions` 0.1.0 — `CsrMatrix`, `SparseNorm` and the two sparse-dense matrix products `Lodestar.Decomposition` will need — as a standalone, green, publishable package, without touching `Lodestar.Text`.

**Architecture:** The first of three steps the spec lays out. `src/` references published packages and never projects, so `Lodestar.Text` cannot depend on `Lodestar.Abstractions` until it is on nuget.org; step A therefore creates the package and nothing else, step B moves `Lodestar.Text` onto it, step C adds the algorithms. Both `CsrMatrix` types exist between A and B, in different namespaces and different packages — deliberate, temporary, and the reason two gates are deferred below.

**Tech Stack:** C# / .NET (`net10.0;netstandard2.0`), xunit, scipy 1.18.0 for the matrix-product corpus.

**Spec:** [`docs/superpowers/specs/2026-09-01_0440_decomposition-truncated-svd-and-nmf.md`](../specs/2026-09-01_0440_decomposition-truncated-svd-and-nmf.md)

**Branch:** `feat/440-abstractions-csrmatrix` (already created off `main`; the spec is committed on it).

## Global Constraints

- **Two target frameworks, one public API.** `net10.0;netstandard2.0`, never a reduced surface.
- **Zero dependencies.** Nothing on `net10.0`; only `System.Memory` 4.6.3 and `System.Numerics.Vectors` 4.6.1 on `netstandard2.0` — `Lodestar.Metrics`' graph. **No `System.Text.Json`**: `LodestarIncludesPersistence` stays unset, because `CsrMatrix` itself serialises nothing.
- **Version 0.1.0**, declared only in `src/Lodestar.Abstractions/Version.props` as `$(LodestarAbstractionsVersion)`. Release tag would be `Lodestar.Abstractions/v0.1.0`.
- **Namespace is `Lodestar.Abstractions`.** Not `Lodestar.Text.Vectorization`: the spec refuses a type-forward that would leave this package declaring a type in another package's namespace forever.
- **Everything step B and step C will need ships in 0.1.0**, because each later change to this package costs another release. Concretely: `InternalsVisibleTo("Lodestar.Text")` — `CreateUnchecked` is `internal` and `CountVectorizer`, `TfidfTransformer` and `HashingVectorizer` call it — and both matrix products.
- **`Lodestar.Text` is not touched.** Its `CsrMatrix` stays exactly as it is; step B removes it.
- **No reference pages, and no `covered` entry, in this step.** Two pages documenting two `CsrMatrix.Multiply` cannot both satisfy the link rule from one guide, and `docs/wiki-map.json`'s `covered` is the only thing the reference gate enforces — CLAUDE.md's *reference gate* paragraph says the rest waits on the page nobody has written. Step B moves the pages and adds `covered`.
- **Warnings are errors.** `SonarAnalyzer.CSharp` at `AnalysisMode=All`, `AnalysisLevel` 10.0. Python touched here is linted the same way: `ruff check --select ARG` reproduces S1172, which is what #511 was caught by.
- **Comments:** why, not what; two lines inline, eight of prose in XML documentation; `long-comment:` past that, and it must stay exceptional — seven markers in 3586 blocks.
- **Oracle replay tolerance is `1e-9`.** Corpora are generated from a directory that is not an ancestor of the checkout, and the generator's **own** exit code is read.

---

### Task 1: The package, the type, and the decision that admits it

**Files:**

- Create: `src/Lodestar.Abstractions/Version.props`
- Create: `src/Lodestar.Abstractions/Lodestar.Abstractions.csproj`
- Create: `src/Lodestar.Abstractions/CsrMatrix.cs`
- Create: `tests/Lodestar.Abstractions.Tests/Lodestar.Abstractions.Tests.csproj`
- Create: `tests/Lodestar.Abstractions.NetStandard.Tests/Lodestar.Abstractions.NetStandard.Tests.csproj`
- Create: `tests/Lodestar.Abstractions.Tests/CsrMatrixTests.cs`
- Create: `docs/decisions/0071-csrmatrix-moves-to-an-abstractions-package.md`
- Modify: `docs/decisions/README.md`
- Modify: `Lodestar.slnx`

**Interfaces:**

- Consumes: nothing.
- Produces: `Lodestar.Abstractions.CsrMatrix` with the surface `Lodestar.Text.Vectorization.CsrMatrix` has today — `CsrMatrix(int, int, double[], int[], int[])`, `RowCount`, `ColumnCount`, `Values`, `ColumnIndices`, `RowPointers`, `NonZeroCount`, `ToDense()`, `RowL1Norm(int)`, `RowL2Norm(int)`, `NormalizeRows(SparseNorm)`, `Multiply(ReadOnlySpan<double>)`, and `internal static CreateUnchecked(...)` — plus `Lodestar.Abstractions.SparseNorm` with `L1` and `L2`. Task 2 adds two members to this type.

- [ ] **Step 1: Copy the type across, changing only its namespace**

The move is mechanical and must stay mechanical — a hand-retyped copy is a second implementation to keep in step with the first until step B deletes it.

```bash
mkdir -p src/Lodestar.Abstractions
sed '1s|^namespace Lodestar\.Text\.Vectorization;$|namespace Lodestar.Abstractions;|' \
  src/Lodestar.Text/Vectorization/CsrMatrix.cs > src/Lodestar.Abstractions/CsrMatrix.cs
head -1 src/Lodestar.Abstractions/CsrMatrix.cs
diff <(tail -n +2 src/Lodestar.Text/Vectorization/CsrMatrix.cs) \
     <(tail -n +2 src/Lodestar.Abstractions/CsrMatrix.cs) && echo "identical below line 1"
```

Expected: the first line reads `namespace Lodestar.Abstractions;`, and the diff is empty. If the diff is not empty the `sed` matched something else — stop and look.

- [ ] **Step 2: Write the version and the project**

```bash
cat > src/Lodestar.Abstractions/Version.props <<'PROPS'
<Project>

  <!--
    Lodestar.Abstractions owns its version here, independently of the other
    packages (see docs/decisions/0012-per-package-versioning.md).

    0.1.0 is this package's first release, and it is the release Lodestar.Text
    0.5.0 depends on: src/ references published packages, so this number has to
    be on nuget.org before that branch can go green.
  -->
  <PropertyGroup>
    <LodestarAbstractionsVersion>0.1.0</LodestarAbstractionsVersion>
  </PropertyGroup>

</Project>
PROPS
cat > src/Lodestar.Abstractions/Lodestar.Abstractions.csproj <<'PROJ'
<Project Sdk="Microsoft.NET.Sdk">

  <!-- This package's version, owned here rather than repository-wide. -->
  <Import Project="Version.props" />

  <PropertyGroup>
    <Version>$(LodestarAbstractionsVersion)</Version>
    <TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>
    <RootNamespace>Lodestar.Abstractions</RootNamespace>

    <PackageId>Lodestar.Abstractions</PackageId>
    <Description>The primitive types Lodestar's packages share: a compressed sparse row matrix with the vector and dense-block products a decomposition needs. No dependencies, no I/O, nothing to configure.</Description>
    <PackageTags>sparse;csr;matrix;linear-algebra;lodestar</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <!-- CreateUnchecked is internal and CountVectorizer, TfidfTransformer and
         HashingVectorizer call it. The grant ships in 0.1.0 because adding it
         later would cost another release of this package. -->
    <InternalsVisibleTo Include="Lodestar.Text" />
    <InternalsVisibleTo Include="Lodestar.Abstractions.Tests" />
    <!-- Same suite, replayed against the netstandard2.0 build. -->
    <InternalsVisibleTo Include="Lodestar.Abstractions.NetStandard.Tests" />
  </ItemGroup>

</Project>
PROJ
dotnet build src/Lodestar.Abstractions/Lodestar.Abstractions.csproj -c Release 2>&1 | tail -4
```

Expected: `0 Warning(s)`, `0 Error(s)`, both target frameworks. A `System.Text.Json` error means `LodestarIncludesPersistence` leaked in — it must stay unset.

- [ ] **Step 3: Write the two test projects**

Both are the `Lodestar.Conformal` pair with the name changed, minus the reference-gate items, which this step does not earn:

```bash
mkdir -p tests/Lodestar.Abstractions.Tests tests/Lodestar.Abstractions.NetStandard.Tests
sed -e 's#Lodestar\.Conformal#Lodestar.Abstractions#g' \
    tests/Lodestar.Conformal.Tests/Lodestar.Conformal.Tests.csproj \
  > tests/Lodestar.Abstractions.Tests/Lodestar.Abstractions.Tests.csproj
sed -e 's#Lodestar\.Conformal#Lodestar.Abstractions#g' \
    tests/Lodestar.Conformal.NetStandard.Tests/Lodestar.Conformal.NetStandard.Tests.csproj \
  > tests/Lodestar.Abstractions.NetStandard.Tests/Lodestar.Abstractions.NetStandard.Tests.csproj
```

Then delete, from **both** files, the whole `<ItemGroup>` whose comment begins *"The gate's engine is shared by every package's suite"* — the `ReferenceDocumentation.cs` link, the `docs/reference/**` copy, the `wiki-map.json` copy and the `docs/**/*.md` copy. Without a `covered` entry that engine has nothing to check, and copying the tree into two more output directories for nothing is the kind of cost that never gets removed later.

Keep the `../oracles/**/*.json` item: task 2 needs it.

- [ ] **Step 4: Write the failing test**

```csharp
using Xunit;

namespace Lodestar.Abstractions.Tests;

/// <summary>The surface as it moved: same behaviour, new namespace.</summary>
public sealed class CsrMatrixTests
{
    // [[1, 0, 2], [0, 3, 0]] — one row with a gap, one with a single entry.
    private static CsrMatrix Sample() =>
        new(2, 3, [1.0, 2.0, 3.0], [0, 2, 1], [0, 2, 3]);

    [Fact]
    public void The_dense_form_puts_every_value_back_where_it_came_from()
    {
        double[,] dense = Sample().ToDense();

        Assert.Equal(1.0, dense[0, 0]);
        Assert.Equal(0.0, dense[0, 1]);
        Assert.Equal(2.0, dense[0, 2]);
        Assert.Equal(3.0, dense[1, 1]);
    }

    /// <summary>
    /// The three arrays are public and have been since 0.1.0, so they are part of the
    /// contract that moved rather than an implementation detail behind it.
    /// </summary>
    [Fact]
    public void The_raw_arrays_are_the_ones_they_were_built_from()
    {
        CsrMatrix matrix = Sample();

        Assert.Equal([1.0, 2.0, 3.0], matrix.Values);
        Assert.Equal([0, 2, 1], matrix.ColumnIndices);
        Assert.Equal([0, 2, 3], matrix.RowPointers);
        Assert.Equal(3, matrix.NonZeroCount);
        Assert.Equal(2, matrix.RowCount);
        Assert.Equal(3, matrix.ColumnCount);
    }

    [Fact]
    public void Row_norms_read_only_the_row_they_name()
    {
        CsrMatrix matrix = Sample();

        Assert.Equal(3.0, matrix.RowL1Norm(0));
        Assert.Equal(Math.Sqrt(5.0), matrix.RowL2Norm(0), 1e-12);
        Assert.Equal(3.0, matrix.RowL2Norm(1));
    }

    [Fact]
    public void Normalizing_by_L2_leaves_every_row_of_unit_length()
    {
        CsrMatrix matrix = Sample();

        matrix.NormalizeRows(SparseNorm.L2);

        Assert.Equal(1.0, matrix.RowL2Norm(0), 1e-12);
        Assert.Equal(1.0, matrix.RowL2Norm(1), 1e-12);
    }

    [Fact]
    public void The_vector_product_skips_the_zeros() =>
        Assert.Equal([7.0, 6.0], Sample().Multiply([1.0, 2.0, 3.0]));

    [Fact]
    public void A_vector_of_the_wrong_length_is_refused() =>
        Assert.Throws<ArgumentException>(() => Sample().Multiply([1.0, 2.0]));

    [Fact]
    public void Arrays_that_do_not_describe_a_matrix_are_refused() =>
        Assert.Throws<ArgumentException>(
            () => new CsrMatrix(2, 3, [1.0], [0, 2], [0, 2, 3]));

    /// <summary>
    /// The unchecked factory is what the vectorizers use, and it stays internal after
    /// the move: <c>InternalsVisibleTo</c> is what keeps step B compiling.
    /// </summary>
    [Fact]
    public void The_unchecked_factory_is_reachable_from_a_friend_assembly() =>
        Assert.Equal(3, CsrMatrix.CreateUnchecked(2, 3, [1.0, 2.0, 3.0], [0, 2, 1], [0, 2, 3])
                                 .NonZeroCount);
}
```

- [ ] **Step 5: Put the three projects in the solution**

Edit `Lodestar.slnx`: `<Project Path="src/Lodestar.Abstractions/Lodestar.Abstractions.csproj" />` in the `/src/` folder, and both test projects in `/tests/`, keeping each folder's existing ordering.

- [ ] **Step 6: Run them**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~Abstractions" 2>&1 | grep -E "Passed!|Failed!"
```

Expected: two assemblies, the same non-zero count in each. Read the count, not the colour: a `--filter` that matches nothing exits zero.

- [ ] **Step 7: Write the decision that admits the package**

`docs/decisions/0071-csrmatrix-moves-to-an-abstractions-package.md`, with `**Status:** accepted · **Date:** 2026-09-01` and `**Amends:** [0069](0069-the-package-layout-as-built-and-what-enforces-it.md)` on the status line. It must carry:

- **Context** — 0069 recorded `Lodestar.Abstractions` as *decided against*, on the ground that the duplication and cycles #427 predicted never happened, and left "whether a second, third and fourth edge into `Lodestar.Text` stays acceptable" to whoever opened the first Phase 2 lot. This is that lot.
- **Decision** — `CsrMatrix` and `SparseNorm` move to `Lodestar.Abstractions`, namespace and all. `Lodestar.Decomposition` depends on `Abstractions` alone, and `Lodestar.Text` becomes its second consumer rather than its owner.
- **Why not the edge into `Lodestar.Text`** — it would make every future consumer of a shared primitive carry the distances, the stemmers, the tokenizers and the vectorizers, and `System.Text.Json` with them on `netstandard2.0`. 0069's rule 1 reads the other way here: the dependency profile genuinely differs.
- **What it costs, stated rather than softened** — a breaking source change for `Lodestar.Text` 0.5.0, three PRs separated by two releases nobody can automate, and an `InternalsVisibleTo` from `Abstractions` to `Text` so that `CreateUnchecked` can stay internal. That attribute names a package in the opposite direction to the dependency; it is inert at run time and it is the price of not making a documented footgun public.
- **Options refused** — the type-forward that keeps `Lodestar.Text.Vectorization` as the namespace (buys source compatibility, costs a permanently wrong namespace in a pre-1.0 library); a namespace inside `Lodestar.Text` (cheapest, refused for the reason above); making `CreateUnchecked` public.
- **Consequences** — 0069's "the satellite tier is empty" and "no `Abstractions`" paragraphs are the parts this amends; its rules 2 and 3 are untouched and now cover six packages.

Then add the row to `docs/decisions/README.md`'s table, raise **both** spellings of the count — `grep -n "seventy\|sixty-nine" docs/decisions/README.md` finds them — and add the `Amended by [0071]` back-reference to 0069's own row, which is the shape the index already uses for 0057/0058.

- [ ] **Step 8: Check the gates this task can fail**

```bash
python3 tools/check_adr_immutable.py --base origin/main
npx markdownlint-cli2 "docs/**/*.md"
python3 tools/check_comment_length.py
dotnet format Lodestar.slnx --verify-no-changes
```

- [ ] **Step 9: Commit**

```bash
git add src/Lodestar.Abstractions tests/Lodestar.Abstractions.Tests tests/Lodestar.Abstractions.NetStandard.Tests Lodestar.slnx docs/decisions
git commit -m "Create Lodestar.Abstractions, and record what it amends"
```

---

### Task 2: The two matrix products

**Files:**

- Modify: `src/Lodestar.Abstractions/CsrMatrix.cs`
- Modify: `tools/generate_oracles.py`
- Create: `tests/oracles/sparse_matmul.json`
- Create: `tests/Lodestar.Abstractions.Tests/OracleLoader.cs`
- Create: `tests/Lodestar.Abstractions.Tests/SparseProductTests.cs`

**Interfaces:**

- Consumes: `Lodestar.Abstractions.CsrMatrix` from task 1.
- Produces: two public members on `CsrMatrix` —
  `public double[] Multiply(ReadOnlySpan<double> dense, int columnCount)` returning `RowCount × columnCount` row-major, and
  `public double[] TransposeMultiply(ReadOnlySpan<double> dense, int columnCount)` returning `ColumnCount × columnCount` row-major.
  Step C's power iterations are `A Ω` and `Aᵀ Q`, which is exactly this pair.
- Produces: `tests/oracles/sparse_matmul.json`, shaped
  `{"metadata": {...}, "cases": [{"name", "rows", "columns", "values", "column_indices", "row_pointers", "block_columns", "block", "product", "transpose_block", "transpose_product"}]}`,
  every matrix and block row-major.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Text.Json;
using Xunit;

namespace Lodestar.Abstractions.Tests;

/// <summary>The sparse-dense products, against scipy.</summary>
public sealed class SparseProductTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("sparse_matmul.json");

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

    private static int[] Ints(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetInt32())];

    private static CsrMatrix Matrix(JsonElement c) => new(
        c.GetProperty("rows").GetInt32(),
        c.GetProperty("columns").GetInt32(),
        Doubles(c, "values"),
        Ints(c, "column_indices"),
        Ints(c, "row_pointers"));

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_dense_block_product_matches_scipy(int index)
    {
        JsonElement c = Cases[index];
        int width = c.GetProperty("block_columns").GetInt32();

        double[] product = Matrix(c).Multiply(Doubles(c, "block"), width);

        double[] expected = Doubles(c, "product");
        Assert.Equal(expected.Length, product.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], product[i], Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_transposed_product_matches_scipy(int index)
    {
        JsonElement c = Cases[index];
        int width = c.GetProperty("block_columns").GetInt32();

        double[] product = Matrix(c).TransposeMultiply(Doubles(c, "transpose_block"), width);

        double[] expected = Doubles(c, "transpose_product");
        Assert.Equal(expected.Length, product.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], product[i], Tolerance);
        }
    }

    [Fact]
    public void A_block_whose_length_does_not_fit_the_matrix_is_refused()
    {
        CsrMatrix matrix = new(2, 3, [1.0], [0], [0, 1, 1]);

        Assert.Throws<ArgumentException>(() => matrix.Multiply([1.0, 2.0, 3.0, 4.0], 2));
        Assert.Throws<ArgumentException>(() => matrix.TransposeMultiply([1.0, 2.0, 3.0], 2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_block_width_that_is_not_positive_is_refused(int width)
    {
        CsrMatrix matrix = new(2, 3, [1.0], [0], [0, 1, 1]);

        Assert.Throws<ArgumentOutOfRangeException>(() => matrix.Multiply([], width));
        Assert.Throws<ArgumentOutOfRangeException>(() => matrix.TransposeMultiply([], width));
    }

    /// <summary>
    /// A one-column block is the vector product, which is what makes the wider one
    /// worth having rather than a loop the caller writes.
    /// </summary>
    [Fact]
    public void One_column_agrees_with_the_vector_overload()
    {
        CsrMatrix matrix = new(2, 3, [1.0, 2.0, 3.0], [0, 2, 1], [0, 2, 3]);

        Assert.Equal(matrix.Multiply([1.0, 2.0, 3.0]), matrix.Multiply([1.0, 2.0, 3.0], 1));
    }
}
```

Copy `tests/Lodestar.Conformal.Tests/OracleLoader.cs` into `tests/Lodestar.Abstractions.Tests/`, changing only its namespace to `Lodestar.Abstractions.Tests`.

- [ ] **Step 2: Run it to watch it fail**

```bash
dotnet test tests/Lodestar.Abstractions.Tests -c Release --filter "FullyQualifiedName~SparseProduct" 2>&1 | tail -5
```

Expected: a compile error naming `Multiply` and `TransposeMultiply` — the overloads do not exist yet.

- [ ] **Step 3: Implement both products**

Append to `CsrMatrix`, after the existing `Multiply(ReadOnlySpan<double>)`:

```csharp
    /// <summary>Computes the matrix-block product <c>this · block</c>.</summary>
    /// <remarks>
    /// The dense operand is row-major and <paramref name="columnCount"/> wide, so its length is
    /// <see cref="ColumnCount"/> × that; the result is <see cref="RowCount"/> rows of the same
    /// width. Written as one pass over the non-zeros rather than <paramref name="columnCount"/>
    /// passes: the column indices are read once and the inner loop walks contiguous memory on
    /// both sides.
    /// </remarks>
    /// <param name="block">The dense right operand, row-major.</param>
    /// <param name="columnCount">How many columns <paramref name="block"/> holds.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="columnCount"/> is not positive.</exception>
    /// <exception cref="ArgumentException"><paramref name="block"/> is not <see cref="ColumnCount"/> rows of that width.</exception>
    public double[] Multiply(ReadOnlySpan<double> block, int columnCount)
    {
        GuardBlock(block, ColumnCount, columnCount);

        double[] result = new double[(long)RowCount * columnCount <= int.MaxValue
            ? RowCount * columnCount
            : throw new ArgumentOutOfRangeException(nameof(columnCount), columnCount,
                "The product would not fit in a single array.")];
        for (int row = 0; row < RowCount; row++)
        {
            int target = row * columnCount;
            for (int k = RowPointers[row]; k < RowPointers[row + 1]; k++)
            {
                double value = Values[k];
                int source = ColumnIndices[k] * columnCount;
                for (int column = 0; column < columnCount; column++)
                {
                    result[target + column] += value * block[source + column];
                }
            }
        }
        return result;
    }

    /// <summary>Computes the transposed product <c>thisᵀ · block</c>, without transposing.</summary>
    /// <remarks>
    /// The dense operand is <see cref="RowCount"/> rows of <paramref name="columnCount"/>, and the
    /// result is <see cref="ColumnCount"/> of them. Materialising the transpose would cost a second
    /// matrix; scattering into the result instead reads each non-zero once, which is the same work.
    /// </remarks>
    /// <param name="block">The dense right operand, row-major.</param>
    /// <param name="columnCount">How many columns <paramref name="block"/> holds.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="columnCount"/> is not positive.</exception>
    /// <exception cref="ArgumentException"><paramref name="block"/> is not <see cref="RowCount"/> rows of that width.</exception>
    public double[] TransposeMultiply(ReadOnlySpan<double> block, int columnCount)
    {
        GuardBlock(block, RowCount, columnCount);

        double[] result = new double[(long)ColumnCount * columnCount <= int.MaxValue
            ? ColumnCount * columnCount
            : throw new ArgumentOutOfRangeException(nameof(columnCount), columnCount,
                "The product would not fit in a single array.")];
        for (int row = 0; row < RowCount; row++)
        {
            int source = row * columnCount;
            for (int k = RowPointers[row]; k < RowPointers[row + 1]; k++)
            {
                double value = Values[k];
                int target = ColumnIndices[k] * columnCount;
                for (int column = 0; column < columnCount; column++)
                {
                    result[target + column] += value * block[source + column];
                }
            }
        }
        return result;
    }

    /// <summary>Refuses a dense operand whose shape does not fit the side it multiplies.</summary>
    private static void GuardBlock(ReadOnlySpan<double> block, int expectedRows, int columnCount)
    {
        if (columnCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(columnCount), columnCount, "A dense block has at least one column.");
        }
        if (block.Length != (long)expectedRows * columnCount)
        {
            throw new ArgumentException(
                $"A block of {expectedRows} rows and {columnCount} columns holds "
                    + $"{(long)expectedRows * columnCount} values, not {block.Length}.",
                nameof(block));
        }
    }
```

- [ ] **Step 4: Add the corpus generator**

`scipy.sparse.csr_matrix @ dense` is the reference. Insert into `tools/generate_oracles.py`, before `_internal_validity_fixtures`:

```python
# --- Sparse-dense products (#440) -----------------------------------------


def _sparse_matmul_fixtures() -> list[dict]:
    """CSR matrices paired with a dense block, including the shapes that hide bugs."""
    rng = SeededRandom(SEED + 440)
    cases = [
        # Row-major by hand, so the layout the C# reads is visible in the fixture.
        {"name": "a gap in the first row", "rows": 2, "columns": 3, "width": 2,
         "dense": [[1.0, 0.0, 2.0], [0.0, 3.0, 0.0]]},
        # An empty row: row_pointers repeat, and the result row must stay zero.
        {"name": "an empty row", "rows": 3, "columns": 3, "width": 4,
         "dense": [[1.0, 2.0, 0.0], [0.0, 0.0, 0.0], [0.0, 0.0, 5.0]]},
        # An all-zero column: the transposed product must still produce its row.
        {"name": "a column nothing touches", "rows": 2, "columns": 4, "width": 3,
         "dense": [[1.0, 0.0, 0.0, 2.0], [0.0, 0.0, 3.0, 0.0]]},
        {"name": "one column of block", "rows": 3, "columns": 3, "width": 1,
         "dense": [[1.0, 0.0, 2.0], [0.0, 3.0, 0.0], [4.0, 0.0, 5.0]]},
        {"name": "no non-zeros at all", "rows": 2, "columns": 2, "width": 2,
         "dense": [[0.0, 0.0], [0.0, 0.0]]},
    ]
    # One larger, denser case, so the small hand-written ones are not the whole corpus.
    rows, columns = 12, 9
    dense = [[round(rng.uniform(-4.0, 4.0), 6) if rng.random() < 0.35 else 0.0
              for _ in range(columns)] for _ in range(rows)]
    cases.append({"name": "twelve by nine at a third dense", "rows": rows,
                  "columns": columns, "width": 5, "dense": dense})
    return cases


def generate_sparse_matmul() -> dict:
    """The two sparse-dense products, against scipy (#440)."""
    from scipy import sparse

    rng = SeededRandom(SEED + 4400)
    cases = []
    for fx in _sparse_matmul_fixtures():
        matrix = sparse.csr_matrix(np.array(fx["dense"], dtype=float))
        width = fx["width"]
        block = np.array([[round(rng.uniform(-3.0, 3.0), 6) for _ in range(width)]
                          for _ in range(fx["columns"])])
        transpose_block = np.array([[round(rng.uniform(-3.0, 3.0), 6) for _ in range(width)]
                                    for _ in range(fx["rows"])])
        cases.append({
            "name": fx["name"],
            "rows": fx["rows"], "columns": fx["columns"],
            "values": [float(v) for v in matrix.data],
            "column_indices": [int(i) for i in matrix.indices],
            "row_pointers": [int(i) for i in matrix.indptr],
            "block_columns": width,
            "block": [stable(v) for v in block.ravel()],
            "product": [stable(v) for v in (matrix @ block).ravel()],
            "transpose_block": [stable(v) for v in transpose_block.ravel()],
            "transpose_product": [stable(v) for v in (matrix.T @ transpose_block).ravel()],
        })

    return {"metadata": {"library": "scipy", "version": version("scipy"),
                         "count": len(cases)},
            "cases": cases}
```

and register it in `main()`'s `generators` dict, after `"conformal.json"`:

```python
        "sparse_matmul.json": generate_sparse_matmul,
```

- [ ] **Step 5: Generate, reading the generator's own exit code**

```bash
REPO=$(git rev-parse --show-toplevel)
cd /tmp && PYTHONSAFEPATH=1 "$REPO/.venv-oracles/bin/python" "$REPO/tools/generate_oracles.py"
echo "generator exit: $?"
```

Expected: `sparse_matmul.json: 6 cases -> …`, exit 0, and `git status --short tests/oracles` naming one file. Never pipe this into `tail`: the shell would report `tail`'s status.

- [ ] **Step 6: Run the tests**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~Abstractions" 2>&1 | grep -E "Passed!|Failed!"
```

Expected: both assemblies green, and the count risen by the theory rows the six cases produce.

- [ ] **Step 7: Lint the Python the way SonarCloud will**

```bash
.venv-oracles/bin/ruff check --isolated --select ARG,PLR0912,PLR0913,PLR0915,C901 --no-cache --output-format concise tools/generate_oracles.py
python3 tools/check_repeated_literals.py --base origin/main
python3 tools/check_comment_length.py
```

Expected: nothing reported in the new block. `ruff`'s `ARG002` is `S1172`'s analogue and is what #511's quality gate failed on; the local build says nothing about Python, so this is the only check that runs before the push.

- [ ] **Step 8: Commit**

```bash
git add src/Lodestar.Abstractions tests/Lodestar.Abstractions.Tests tools/generate_oracles.py tests/oracles/sparse_matmul.json
git commit -m "Give CsrMatrix the two products a decomposition needs"
```

---

### Task 3: Packaging, CI, the sample and the changelog

**Files:**

- Modify: `tools/check_nuspec_dependencies.py`
- Modify: `tools/check_sample_coverage.py`
- Modify: `.github/workflows/ci.yml`, `sonarcloud.yml`, `release.yml`, `release-nuget-org.yml`
- Modify: `samples/Lodestar.Sample/Lodestar.Sample.csproj`
- Modify: `samples/Lodestar.DocSnippets/Lodestar.DocSnippets.csproj`
- Create: `samples/Lodestar.Sample/Lot7Abstractions.cs`
- Modify: `samples/Lodestar.Sample/Program.cs`, `samples/Lodestar.Sample/PackagingGate.cs`
- Modify: `CHANGELOG.md`

**Interfaces:**

- Consumes: `Lodestar.Abstractions` 0.1.0 packed to `./artifacts` by the loops this task edits.
- Produces: nothing later tasks read. This is the task that makes the branch green.

- [ ] **Step 1: Declare the intended dependency graph**

In `tools/check_nuspec_dependencies.py`, after `METRICS = "Lodestar.Metrics"`:

```python
ABSTRACTIONS = "Lodestar.Abstractions"
```

and in `EXPECTED`, before the `TEXT` entry so the table reads bottom-up:

```python
    ABSTRACTIONS: {
        # Nothing on net10.0, only the polyfills on netstandard2.0: a sparse matrix
        # and two products, with no I/O to serialise. Deliberately not
        # System.Text.Json — persistence stays in the packages that persist.
        NET: {},
        NETSTANDARD: {**POLYFILLS},
    },
```

- [ ] **Step 2: Add the package to every loop that names the others**

```bash
grep -rn "Lodestar.Metrics" .github/workflows/*.yml
```

Each hit is one edit, appending `src/Lodestar.Abstractions` (or `Lodestar.Abstractions`) the way `Lodestar.Conformal` was appended:

- `.github/workflows/ci.yml` — four `for proj in …` loops
- `.github/workflows/sonarcloud.yml` — one loop
- `.github/workflows/release.yml` — the package allow-list
- `.github/workflows/release-nuget-org.yml` — the matrix

**Not `wiki.yml`.** This package has no pages until step B, and adding it to that loop would ask `build_wiki.py` for a package `docs/wiki-map.json` does not describe.

- [ ] **Step 3: Keep the sample-coverage gate off this package for now**

In `tools/check_sample_coverage.py`:

```python
CONVERTED = ["Lodestar.Text", "Lodestar.Conformal"]
WAITING = ["Lodestar.Fuzzy", "Lodestar.Embeddings", "Lodestar.Metrics", "Lodestar.Abstractions"]
```

`WAITING`, not `CONVERTED`: a per-class file would have to be called `CsrMatrixSample.cs`, and that name is taken by the one exercising `Lodestar.Text`'s copy until step B deletes it. Step B moves this entry across in the same commit that frees the name.

- [ ] **Step 4: Exercise the package from the sample**

The packaging gate counts **member** references, so every public member of the new assembly needs a call.

```csharp
using Lodestar.Abstractions;

namespace Lodestar.Sample;

/// <summary>
/// Lot 7 — Lodestar.Abstractions, the sparse primitive the other packages share.
/// </summary>
/// <remarks>
/// Named for a lot rather than for its class because `CsrMatrixSample.cs` still
/// demonstrates `Lodestar.Text`'s copy; the two exist together until that copy goes.
/// </remarks>
internal static class Lot7Abstractions
{
    // [[1, 0, 2], [0, 3, 0]] — three non-zeros, one gap, in CSR.
    private static readonly double[] Values = [1.0, 2.0, 3.0];
    private static readonly int[] Columns = [0, 2, 1];
    private static readonly int[] RowPointers = [0, 2, 3];

    // A 3 x 2 dense block, row-major: the shape a power iteration multiplies by.
    private static readonly double[] Block = [1.0, 0.5, 2.0, 1.5, 3.0, 2.5];

    public static void Run()
    {
        Console.WriteLine("lot 7 — the shared sparse primitive");

        CsrMatrix matrix = new(2, 3, Values, Columns, RowPointers);
        Console.WriteLine($"  shape                 = {Inv.F0(matrix.RowCount)} x {Inv.F0(matrix.ColumnCount)}, "
            + $"{Inv.F0(matrix.NonZeroCount)} non-zeros");
        Console.WriteLine($"  row 0 norms           = L1 {Inv.F3(matrix.RowL1Norm(0))}, L2 {Inv.F3(matrix.RowL2Norm(0))}");
        Console.WriteLine($"  values / indices      = {Inv.List(matrix.Values)} / [{string.Join(", ", matrix.ColumnIndices)}]");
        Console.WriteLine($"  row pointers          = [{string.Join(", ", matrix.RowPointers)}]");

        double[,] dense = matrix.ToDense();
        Console.WriteLine($"  dense row 0           = [{Inv.F1(dense[0, 0])}, {Inv.F1(dense[0, 1])}, {Inv.F1(dense[0, 2])}]");
        Console.WriteLine($"  · vector              = {Inv.List(matrix.Multiply([1.0, 2.0, 3.0]))}");
        Console.WriteLine($"  · 3x2 block           = {Inv.List(matrix.Multiply(Block, 2))}");
        Console.WriteLine($"  transposed · 2x2      = {Inv.List(matrix.TransposeMultiply([1.0, 0.5, 2.0, 1.5], 2))}");

        // NormalizeRows mutates, so it runs last and on a copy of the arrays.
        CsrMatrix scaled = new(2, 3, [.. Values], [.. Columns], [.. RowPointers]);
        scaled.NormalizeRows(SparseNorm.L2);
        Console.WriteLine($"  L2-normalized values  = {Inv.List(scaled.Values)}");
        Console.WriteLine();
    }
}
```

Then: add `using Lodestar.Abstractions;` and the `Lodestar.Abstractions : {FrameworkOf(typeof(CsrMatrix))}` line to `Program.cs`, call `Lot7Abstractions.Run();`, and add `typeof(CsrMatrix).Assembly,` to `PackagingGate.Verify`'s `packaged` array.

`Program.cs` and `PackagingGate.cs` both already import `Lodestar.Text.Vectorization` transitively; if `CsrMatrix` becomes ambiguous in either file, alias the new one — `using AbstractionsCsr = Lodestar.Abstractions.CsrMatrix;` — rather than removing the other import, and delete the alias in step B.

Add the `Version.props` import and the `PackageReference` to **both** `samples/Lodestar.Sample/Lodestar.Sample.csproj` and `samples/Lodestar.DocSnippets/Lodestar.DocSnippets.csproj`, beside the `Lodestar.Conformal` pair.

- [ ] **Step 5: Pack and check**

```bash
rm -rf ./artifacts
for p in src/Lodestar.Abstractions src/Lodestar.Text src/Lodestar.Embeddings src/Lodestar.Fuzzy src/Lodestar.Metrics src/Lodestar.Conformal; do
  dotnet pack "$p" -c Release -o ./artifacts || break
done
python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
NUGET_PACKAGES="$PWD/.nuget-sample" dotnet run -c Release --project samples/Lodestar.Sample
python3 tools/check_sample_coverage.py
python3 tools/check_sample_culture.py
```

Expected: six packages; the checker silent; the sample printing `Lodestar.Abstractions : .NETCoreApp,Version=v10.0` and ending `OK`. `PackagingGate` naming an uncovered member means that member has no call in `Lot7Abstractions` — add one, never suppress.

- [ ] **Step 6: Add the changelog entry**

Under `CHANGELOG.md`'s `## [Unreleased]`, a `### Lodestar.Abstractions` section with `#### Added`, one sentence, the issue and the commit — the shape `CONTRIBUTING.md`'s *Releasing* fixes, with no rationale and no caveat. Also raise the count in the file's header paragraph, which currently reads "The five packages".

- [ ] **Step 7: Run the whole gate sweep**

```bash
dotnet format Lodestar.slnx --verify-no-changes
dotnet build Lodestar.slnx -c Release
dotnet test Lodestar.slnx -c Release
python3 tools/check_version_floor.py
python3 tools/check_machine_paths.py --no-environment
python3 tools/check_sample_culture.py
python3 tools/check_sample_coverage.py
python3 tools/check_comment_length.py
python3 tools/check_repeated_literals.py --base origin/main
python3 tools/check_adr_immutable.py --base origin/main
python3 tools/check_bench_map.py
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 tools/extract_doc_snippets.py && NUGET_PACKAGES="$PWD/.nuget-sample" dotnet run -c Release --project samples/Lodestar.DocSnippets
.venv-oracles/bin/python -m pytest tools/tests -q
```

Read the **test count**, not the colour.

- [ ] **Step 8: Commit**

```bash
git add tools .github/workflows samples CHANGELOG.md
git commit -m "Pack, release and exercise Lodestar.Abstractions"
```

---

## Before the pull request

- [ ] The sweep in task 3 step 7, clean.
- [ ] `git ls-files .nuget-sample | wc -l` is `0` — the package cache is gitignored on this branch, and a `git add -A` that swept it in is what GitHub's 100 MB limit rejected on #511.
- [ ] The PR body carries `Part of #440`, **not** `Closes` — the lot needs steps B and C.
- [ ] The body says what step B needs from the maintainer: `Lodestar.Abstractions/v0.1.0` tagged and published before that branch can restore.

## Self-Review

**1. Spec coverage.** *Placement* step A → tasks 1 and 3. *The dense kernels* row "`A Ω` and `Aᵀ Q`" → task 2; the three factorizations belong to step C. *Testing*, the `1e-9` replay → task 2's corpus; the SVD, QR, LU and NMF corpora are step C's. *Rejected: Math.NET* → enforced by task 3's `EXPECTED` entry, which fails on any dependency at all. *Each step is its own plan* → this plan is step A alone. Nothing in the spec's scope for step A is unassigned.

**2. Placeholders.** Task 1 step 1 hands a `sed` command and a `diff` proving it, rather than 223 pasted lines: the point of that step is that the copy is *mechanical*, and a paste would be a second implementation. Task 1 step 7 specifies the ADR by the six things it must carry rather than by its prose, which is the one place a plan cannot write the deliverable for its author. Every code step carries code.

**3. Type consistency.** `CsrMatrix(int, int, double[], int[], int[])`, `Multiply(ReadOnlySpan<double>)`, `Multiply(ReadOnlySpan<double>, int)`, `TransposeMultiply(ReadOnlySpan<double>, int)`, `CreateUnchecked(int, int, double[], int[], int[])`, `SparseNorm.L1`/`L2` — used identically in tasks 1, 2 and 3. The corpus keys in task 2's *Produces* block are the keys its test reads: `rows`, `columns`, `values`, `column_indices`, `row_pointers`, `block_columns`, `block`, `product`, `transpose_block`, `transpose_product`.

**4. One thing this plan cannot prove.** `Multiply(ReadOnlySpan<double>)` and `Multiply(ReadOnlySpan<double>, int)` are an overload pair, and C# resolves `matrix.Multiply(array, 1)` unambiguously. Task 2 step 1's last test exists to catch it if a future `params` or default argument makes them collide.
