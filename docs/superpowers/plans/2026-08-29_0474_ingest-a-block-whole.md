# Ingest a block whole Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give `EmbeddingIndex` a way to take a contiguous block of vectors in one copy, so the sidecar route ADR 0055 decided stops being slower than the artifact it would replace.

**Architecture:** Two public static factories on the existing `partial class EmbeddingIndex`, sharing one private `Seed` that sets the fields. `FromBlock` copies the caller's span into a fresh array; `FromOwnedBlock` takes the caller's array and keeps it for the life of the index. A three-valued `BlockNormalization` argument decides whether the block is normalized on the way in, trusted as already normalized, or stored in a non-normalizing index. `Restore`, the JSON load path's tail, is rewired onto the same `Seed` so the two paths cannot drift.

**Tech Stack:** C# on `net10.0;netstandard2.0`, xunit, BenchmarkDotNet (this lot adds a plain-`Stopwatch` diagnostic row, not a BenchmarkDotNet one).

**Spec:** [`docs/superpowers/specs/2026-08-29_0474_embeddingindex-cannot-take-a-block-whole.md`](../specs/2026-08-29_0474_embeddingindex-cannot-take-a-block-whole.md)

**Branch:** `perf/474-ingest-a-block-whole` (already created and pushed; it carries the spec)

**Issue:** [#474](https://github.com/CyrilB1531/lodestar/issues/474) — the pull request says `Closes #474` and closes nothing else.

## Global Constraints

- **Both target frameworks, one public API.** Everything ships `net10.0;netstandard2.0`; `netstandard2.0` reaches equivalent behaviour through conditional compilation, never a reduced API. Nothing in this lot needs an `#if`.
- **Warnings are errors.** `dotnet build Lodestar.slnx -c Release` must report 0 warnings. `SonarAnalyzer.CSharp` and the .NET code-quality rules run at `AnalysisMode=All`, `AnalysisLevel=10.0`.
- **Clear Sonar findings before committing, not after.**
- **No `ProjectReference` between `src/` projects.** This lot touches one package (`Lodestar.Embeddings`) and one bench project, so `LodestarUseProjectRefs` is not needed and must not be set.
- **Everything in English** — code, comments, commit messages, PR body. Commit messages carry no `feat:`/`fix:` prefix.
- **Comment rules:** say why not what; two lines inline, or a `long-comment:` marker on the **first** line of the block. `Console.WriteLine` needs a `console-print:` marker **directly** above the call. `python3 tools/check_comment_length.py` and `python3 tools/check_no_console_writeline.py` enforce both.
- **A public member named in prose in a documentation page must be linked** to its reference page, or `ReferenceDocumentationTests` fails.
- **Timings come from `Benchmark (on demand)` only**, dispatched by the maintainer. The session container has inverted this exact comparison once already.
- **Definition of done item 7:** any change to shipped behaviour carries its `CHANGELOG.md` entry.

---

## File Structure

| file | responsibility |
| --- | --- |
| `src/Lodestar.Embeddings/Search/BlockNormalization.cs` | **create** — the enum, and the remark saying why it is not two booleans |
| `src/Lodestar.Embeddings/Search/EmbeddingIndex.Block.cs` | **create** — `FromBlock`, `FromOwnedBlock`, `Seed`, `CheckBlock`, `CopyIds` |
| `src/Lodestar.Embeddings/Search/EmbeddingIndex.Persistence.cs` | **modify** — `Restore`'s tail calls `Seed` |
| `tests/Lodestar.Embeddings.Tests/EmbeddingIndexBlockTests.cs` | **create** — the whole behaviour of both factories |
| `bench/Lodestar.Text.Benchmarks/CrossLang/SidecarBench.cs` | **modify** — two rows and one ratio |
| `docs/decisions/0056-a-block-may-be-adopted-and-the-invariant-is-the-callers-to-keep.md` | **create** — the adoption decision and its loser |
| `docs/reference/embeddings/search/embeddingindex-fromblock.md` | **create** |
| `docs/reference/embeddings/search/embeddingindex-fromownedblock.md` | **create** |
| `docs/reference/embeddings/search/blocknormalization.md` | **create** |
| `docs/reference/embeddings/search/embeddingindex.md` | **modify** — link the two new methods from the member table |
| `samples/Lodestar.Sample/Lot3Embeddings.cs` | **modify** — two calls referencing all three new members |
| `bench/README.md` | **modify** — §12 gains the two rows and the sentence about adoption |
| `docs/guides/performance.md` | **modify** — the measured numbers, once they exist |
| `CHANGELOG.md` | **modify** — one `### Lodestar.Embeddings` / `#### Added` entry |
| `docs/decisions/README.md` | **modify** — the 0056 index row |

The enum gets its own file rather than living beside the class: `check_sample_coverage.py`'s
`DECLARATION` regex reads one public type per declaration and does not care, but ADR 0041's
reasoning about findability applies to reading source too.

---

### Task 1: The enum, the seed, and the copying factory

**Files:**

- Create: `src/Lodestar.Embeddings/Search/BlockNormalization.cs`
- Create: `src/Lodestar.Embeddings/Search/EmbeddingIndex.Block.cs`
- Modify: `src/Lodestar.Embeddings/Search/EmbeddingIndex.Persistence.cs` (the last statement of `Restore`)
- Test: `tests/Lodestar.Embeddings.Tests/EmbeddingIndexBlockTests.cs`

**Interfaces:**

- Consumes: `EmbeddingIndex`'s private fields `_data`, `_length`, `_count`, `_ids`, its private instance method `NormalizeStored(int start)`, and `Buffers.AllocateUninitialized<T>(int)` from `Lodestar.Internal.Persistence`.
- Produces: `public enum BlockNormalization { Normalize, AlreadyNormalized, Off }`; `public static EmbeddingIndex FromBlock(ReadOnlySpan<float> block, int dimension, BlockNormalization normalization, IReadOnlyList<string?>? ids = null)`; `private static EmbeddingIndex Seed(float[] data, int dimension, int count, BlockNormalization normalization, string?[]? ids)`; `private static int CheckBlock(int length, int dimension, BlockNormalization normalization, IReadOnlyList<string?>? ids)`; `private static string?[]? CopyIds(IReadOnlyList<string?>? ids)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/Lodestar.Embeddings.Tests/EmbeddingIndexBlockTests.cs`:

```csharp
using Lodestar.Embeddings.Search;
using Xunit;

namespace Lodestar.Embeddings.Tests;

public sealed class EmbeddingIndexBlockTests
{
    /// <summary>Scores are compared to four places; the index stores float, not double.</summary>
    private const int Places = 4;

    [Fact]
    public void FromBlock_scores_exactly_as_an_index_built_by_Add()
    {
        float[] block = [3f, 4f, 0f, 1f, 2f, 2f];

        var added = new EmbeddingIndex(dimension: 2);
        for (int item = 0; item < 3; item++)
        {
            added.Add(block.AsSpan(item * 2, 2));
        }

        EmbeddingIndex bulk = EmbeddingIndex.FromBlock(block, 2, BlockNormalization.Normalize);

        // The index exposes no vector accessor, so equal scores for a query is what
        // "the same bits" is observable as. Both paths call NormalizeStored, so this is
        // exact equality rather than a tolerance.
        Assert.Equal(added.Search([1f, 1f], 3), bulk.Search([1f, 1f], 3));
    }

    [Fact]
    public void AlreadyNormalized_stores_the_block_untouched()
    {
        // A block that is not normalized, taken as though it were. The query is normalized
        // to (0.6, 0.8) and the stored vector is not, so the score is |(3,4)| = 5.
        EmbeddingIndex index = EmbeddingIndex.FromBlock(
            [3f, 4f], 2, BlockNormalization.AlreadyNormalized);

        Assert.Equal(5f, index.Search([3f, 4f], 1)[0].Score, Places);
    }

    [Fact]
    public void Off_normalizes_neither_the_block_nor_the_query()
    {
        EmbeddingIndex index = EmbeddingIndex.FromBlock([3f, 4f], 2, BlockNormalization.Off);

        Assert.Equal(25f, index.Search([3f, 4f], 1)[0].Score, Places);
    }

    [Fact]
    public void FromBlock_copies_so_the_caller_can_reuse_its_buffer()
    {
        float[] block = [1f, 0f];
        EmbeddingIndex index = EmbeddingIndex.FromBlock(
            block, 2, BlockNormalization.AlreadyNormalized);

        block[0] = 0f;
        block[1] = 1f;

        Assert.Equal(1f, index.Search([1f, 0f], 1)[0].Score, Places);
    }

    [Fact]
    public void Ids_travel_with_the_block()
    {
        EmbeddingIndex index = EmbeddingIndex.FromBlock(
            [1f, 0f, 0f, 1f], 2, BlockNormalization.AlreadyNormalized, ["east", null]);

        Assert.True(index.HasIds);
        Assert.Equal("east", index.GetId(0));
        Assert.Null(index.GetId(1));
    }

    [Fact]
    public void An_empty_block_makes_an_empty_index()
    {
        EmbeddingIndex index = EmbeddingIndex.FromBlock([], 2, BlockNormalization.Normalize);

        Assert.Equal(0, index.Count);
        Assert.Equal(2, index.Dimension);
    }

    [Fact]
    public void A_block_that_is_not_a_multiple_of_the_dimension_is_refused()
    {
        ArgumentException e = Assert.Throws<ArgumentException>(
            () => EmbeddingIndex.FromBlock([1f, 2f, 3f], 2, BlockNormalization.Off));

        Assert.Contains("not a multiple", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Ids_of_the_wrong_length_are_refused()
    {
        ArgumentException e = Assert.Throws<ArgumentException>(
            () => EmbeddingIndex.FromBlock([1f, 0f], 2, BlockNormalization.Off, ["a", "b"]));

        Assert.Contains("2 entries for 1", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_dimension_below_one_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => EmbeddingIndex.FromBlock([1f, 0f], 0, BlockNormalization.Off));
    }

    [Fact]
    public void A_normalization_outside_the_enum_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => EmbeddingIndex.FromBlock([1f, 0f], 2, (BlockNormalization)99));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet test tests/Lodestar.Embeddings.Tests -c Release --filter "FullyQualifiedName~EmbeddingIndexBlockTests"
```

Expected: the build fails — `BlockNormalization` and `FromBlock` do not exist.

**Read the test count, not the colour.** A `--filter` that matches nothing exits zero and reports
success; this filter must report 10 tests once they compile.

- [ ] **Step 3: Write the enum**

Create `src/Lodestar.Embeddings/Search/BlockNormalization.cs`:

```csharp
namespace Lodestar.Embeddings.Search;

/// <summary>How a block handed to a bulk ingest relates to the index's normalization.</summary>
/// <remarks>
/// One argument rather than a <c>normalize</c> flag beside an <c>alreadyNormalized</c> one: the
/// index's flag governs the query as well as the store, so a pair would make a fourth combination
/// representable that means nothing. <see cref="Normalize"/> is the zero value, so an accidental
/// <c>default</c> yields the correct-but-slower behaviour rather than a silently wrong score.
/// </remarks>
public enum BlockNormalization
{
    /// <summary>The index normalizes, and the block is normalized in place once it is taken.</summary>
    Normalize,

    /// <summary>
    /// The index normalizes and the block already is, so it is stored bit for bit. This is a
    /// promise the caller keeps: an unnormalized block taken this way scores wrong and raises
    /// nothing.
    /// </summary>
    AlreadyNormalized,

    /// <summary>The index does not normalize, on insertion or on query.</summary>
    Off,
}
```

- [ ] **Step 4: Write the copying factory and the seed**

Create `src/Lodestar.Embeddings/Search/EmbeddingIndex.Block.cs`:

```csharp
using Lodestar.Internal.Persistence;

namespace Lodestar.Embeddings.Search;

public sealed partial class EmbeddingIndex
{
    /// <summary>Builds an index from a contiguous block of vectors, in one copy.</summary>
    /// <remarks>
    /// For a caller that already holds a whole corpus laid out row after row — a <c>.npy</c>
    /// block, a model's output, a column read out of a store. Replaying it through
    /// <see cref="Add(ReadOnlySpan{float})"/> costs three times the read that produced it
    /// (issue #474); this copies once.
    /// </remarks>
    /// <param name="block">The vectors, row after row, in C order.</param>
    /// <param name="dimension">The embedding dimension; <paramref name="block"/>'s length must be a multiple of it.</param>
    /// <param name="normalization">What is to be done about normalization, and what the index's own flag becomes.</param>
    /// <param name="ids">One id per vector, or <see langword="null"/> for an anonymous index. Copied, never retained.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dimension"/> is below 1, or <paramref name="normalization"/> is not one of the enum's values.</exception>
    /// <exception cref="ArgumentException"><paramref name="block"/>'s length is not a multiple of <paramref name="dimension"/>, or <paramref name="ids"/> holds a number of entries other than the vector count.</exception>
    public static EmbeddingIndex FromBlock(
        ReadOnlySpan<float> block,
        int dimension,
        BlockNormalization normalization,
        IReadOnlyList<string?>? ids = null)
    {
        int count = CheckBlock(block.Length, dimension, normalization, ids);

        // Uninitialized: every element is written by the copy that follows.
        float[] data = Buffers.AllocateUninitialized<float>(block.Length);
        block.CopyTo(data);
        return Seed(data, dimension, count, normalization, CopyIds(ids));
    }

    /// <summary>Validates the three arguments that can disagree, and returns the vector count.</summary>
    private static int CheckBlock(
        int length,
        int dimension,
        BlockNormalization normalization,
        IReadOnlyList<string?>? ids)
    {
        Guard.NotLessThan(dimension, 1);

        // A cast reaches a value the enum does not name, and every branch below would then
        // read it as AlreadyNormalized — a wrong score rather than a refusal.
        if (normalization is not (BlockNormalization.Normalize
            or BlockNormalization.AlreadyNormalized
            or BlockNormalization.Off))
        {
            throw new ArgumentOutOfRangeException(
                nameof(normalization), normalization, "is not a BlockNormalization value.");
        }

        if (length % dimension != 0)
        {
            // "block" as a literal, not nameof: the parameter belongs to the two factories
            // that call this, and theirs is the name a caller has to read.
            throw new ArgumentException(
                $"block length {length} is not a multiple of dimension {dimension}.", "block");
        }

        int count = length / dimension;
        if (ids is not null && ids.Count != count)
        {
            throw new ArgumentException(
                $"ids holds {ids.Count} entries for {count} vectors.", nameof(ids));
        }
        return count;
    }

    /// <summary>The ids as an array the index can keep, or null for an anonymous index.</summary>
    /// <remarks>
    /// Copied by both factories, adopted by neither. The block is where the bytes are; an id
    /// list is the head, and an <c>IReadOnlyList</c> is not an array to take in the first place.
    /// </remarks>
    private static string?[]? CopyIds(IReadOnlyList<string?>? ids)
    {
        if (ids is null)
        {
            return null;
        }

        var copy = new string?[ids.Count];
        for (int i = 0; i < ids.Count; i++)
        {
            copy[i] = ids[i];
        }
        return copy;
    }

    /// <summary>Puts a validated block into a new index, normalizing it only when asked.</summary>
    /// <remarks>
    /// The one place the fields are set from a block, reached by both factories and by
    /// <c>Restore</c>, so the JSON load path and the block path cannot drift apart.
    /// </remarks>
    private static EmbeddingIndex Seed(
        float[] data,
        int dimension,
        int count,
        BlockNormalization normalization,
        string?[]? ids)
    {
        var index = new EmbeddingIndex(dimension, normalization != BlockNormalization.Off)
        {
            _data = data,
            _length = count * dimension,
            _count = count,
            _ids = ids,
        };

        if (normalization == BlockNormalization.Normalize)
        {
            for (int item = 0; item < count; item++)
            {
                index.NormalizeStored(item * dimension);
            }
        }
        return index;
    }
}
```

- [ ] **Step 5: Rewire `Restore` onto the seed**

In `src/Lodestar.Embeddings/Search/EmbeddingIndex.Persistence.cs`, replace the five statements at
the end of `Restore`:

```csharp
        var index = new EmbeddingIndex(dim, normalizeFlag);
        index._data = vectors;
        index._length = vectors.Length;
        index._count = itemCount;
        index._ids = ids;
        return index;
```

with:

```csharp
        // AlreadyNormalized, never Normalize: a stored vector is restored exactly as it was
        // written, and normalizing a second time would move its bits.
        return Seed(
            vectors,
            dim,
            itemCount,
            normalizeFlag ? BlockNormalization.AlreadyNormalized : BlockNormalization.Off,
            ids);
```

`Restore` has already checked that `vectors.LongLength == (long)itemCount * dim`, so `Seed`'s
`count * dimension` is the `vectors.Length` the old code assigned. No behaviour changes.

- [ ] **Step 6: Run the tests to verify they pass**

```bash
dotnet test tests/Lodestar.Embeddings.Tests -c Release --filter "FullyQualifiedName~EmbeddingIndexBlockTests"
```

Expected: PASS, **10 tests**.

- [ ] **Step 7: Run the persistence suite, which `Restore` must not have moved**

```bash
dotnet test tests/Lodestar.Embeddings.Tests -c Release --filter "FullyQualifiedName~Persistence"
```

Expected: PASS, and the same count as before the change. If any test here fails, `Seed` is not
equivalent to the code it replaced — fix `Seed`, never the test.

- [ ] **Step 8: Build both frameworks with warnings as errors**

```bash
dotnet build Lodestar.slnx -c Release
```

Expected: 0 warnings, 0 errors.

- [ ] **Step 9: Commit**

```bash
git add src/Lodestar.Embeddings/Search/BlockNormalization.cs \
        src/Lodestar.Embeddings/Search/EmbeddingIndex.Block.cs \
        src/Lodestar.Embeddings/Search/EmbeddingIndex.Persistence.cs \
        tests/Lodestar.Embeddings.Tests/EmbeddingIndexBlockTests.cs
git commit -m "Take a block in one copy, and give the load path the same seed

Add copies one vector at a time and costs three times the read it follows,
which is what put the sidecar route at 0.66x. FromBlock copies once.

Restore's tail was already this code with the normalization decided the
other way, so it calls the same seed now: AlreadyNormalized, because a
stored vector is restored as it was written and normalizing again would
move its bits."
```

---

### Task 2: The adopting factory

**Files:**

- Modify: `src/Lodestar.Embeddings/Search/EmbeddingIndex.Block.cs`
- Test: `tests/Lodestar.Embeddings.Tests/EmbeddingIndexBlockTests.cs`

**Interfaces:**

- Consumes: `CheckBlock`, `CopyIds` and `Seed` from Task 1, with the signatures listed there.
- Produces: `public static EmbeddingIndex FromOwnedBlock(float[] block, int dimension, BlockNormalization normalization, IReadOnlyList<string?>? ids = null)`.

- [ ] **Step 1: Write the failing tests**

Append to `tests/Lodestar.Embeddings.Tests/EmbeddingIndexBlockTests.cs`, inside the class:

```csharp
    [Fact]
    public void FromOwnedBlock_takes_the_array_rather_than_copying_it()
    {
        float[] block = [1f, 0f];
        EmbeddingIndex index = EmbeddingIndex.FromOwnedBlock(
            block, 2, BlockNormalization.AlreadyNormalized);

        Assert.Equal(1f, index.Search([1f, 0f], 1)[0].Score, Places);

        // long-comment: the invariant FromOwnedBlock documents, asserted rather than only
        // written down. Ownership transferred, so writing to the array afterwards changes
        // what the index scores — and the next reader who breaks that learns it from a
        // failure instead of from a wrong search result in production.
        block[0] = 0f;
        block[1] = 1f;

        Assert.Equal(0f, index.Search([1f, 0f], 1)[0].Score, Places);
    }

    [Fact]
    public void FromOwnedBlock_normalizes_the_callers_array_in_place()
    {
        float[] block = [3f, 4f];
        EmbeddingIndex index = EmbeddingIndex.FromOwnedBlock(
            block, 2, BlockNormalization.Normalize);

        Assert.Equal(0.6f, block[0], Places);
        Assert.Equal(0.8f, block[1], Places);
        Assert.Equal(1f, index.Search([3f, 4f], 1)[0].Score, Places);
    }

    [Fact]
    public void FromOwnedBlock_scores_exactly_as_FromBlock()
    {
        float[] copied = [3f, 4f, 0f, 1f];
        float[] owned = [3f, 4f, 0f, 1f];

        EmbeddingIndex a = EmbeddingIndex.FromBlock(copied, 2, BlockNormalization.Normalize);
        EmbeddingIndex b = EmbeddingIndex.FromOwnedBlock(owned, 2, BlockNormalization.Normalize);

        Assert.Equal(a.Search([1f, 1f], 2), b.Search([1f, 1f], 2));
    }

    [Fact]
    public void FromOwnedBlock_refuses_a_null_block()
    {
        Assert.Throws<ArgumentNullException>(
            () => EmbeddingIndex.FromOwnedBlock(null!, 2, BlockNormalization.Off));
    }

    [Fact]
    public void FromOwnedBlock_refuses_a_block_that_is_not_a_multiple_of_the_dimension()
    {
        ArgumentException e = Assert.Throws<ArgumentException>(
            () => EmbeddingIndex.FromOwnedBlock([1f, 2f, 3f], 2, BlockNormalization.Off));

        Assert.Contains("not a multiple", e.Message, StringComparison.Ordinal);
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet test tests/Lodestar.Embeddings.Tests -c Release --filter "FullyQualifiedName~EmbeddingIndexBlockTests"
```

Expected: the build fails — `FromOwnedBlock` does not exist.

- [ ] **Step 3: Write the adopting factory**

Add to `src/Lodestar.Embeddings/Search/EmbeddingIndex.Block.cs`, directly below `FromBlock`:

```csharp
    /// <summary>Builds an index that <b>takes</b> <paramref name="block"/>, without copying it.</summary>
    /// <remarks>
    /// <b>Ownership transfers.</b> The index reads this array for as long as it lives, so the
    /// caller must not write to it, and must not return it to an <c>ArrayPool</c> — a later
    /// renter's bytes would become this index's embeddings, and no exception marks the moment.
    /// With <see cref="BlockNormalization.Normalize"/> the array is normalized <b>in place</b>,
    /// so the caller's own values change.
    /// <para>
    /// <see cref="FromBlock"/> is the one to reach for unless the copy has been measured and
    /// matters: it costs one pass and asks nothing of the caller. Decision 0056 has the trade.
    /// </para>
    /// </remarks>
    /// <param name="block">The vectors, row after row, in C order. Handed over, not borrowed.</param>
    /// <param name="dimension">The embedding dimension; <paramref name="block"/>'s length must be a multiple of it.</param>
    /// <param name="normalization">What is to be done about normalization, and what the index's own flag becomes.</param>
    /// <param name="ids">One id per vector, or <see langword="null"/> for an anonymous index. Copied, never retained.</param>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dimension"/> is below 1, or <paramref name="normalization"/> is not one of the enum's values.</exception>
    /// <exception cref="ArgumentException"><paramref name="block"/>'s length is not a multiple of <paramref name="dimension"/>, or <paramref name="ids"/> holds a number of entries other than the vector count.</exception>
    public static EmbeddingIndex FromOwnedBlock(
        float[] block,
        int dimension,
        BlockNormalization normalization,
        IReadOnlyList<string?>? ids = null)
    {
        Guard.NotNull(block);
        int count = CheckBlock(block.Length, dimension, normalization, ids);
        return Seed(block, dimension, count, normalization, CopyIds(ids));
    }
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet test tests/Lodestar.Embeddings.Tests -c Release --filter "FullyQualifiedName~EmbeddingIndexBlockTests"
```

Expected: PASS, **15 tests**.

- [ ] **Step 5: Run the whole Embeddings suite on both frameworks**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~Lodestar.Embeddings"
```

Expected: PASS, and the netstandard2.0 assembly reports the same 15 new tests — the
`*.NetStandard.Tests` project links the same sources, so no project file changes.

- [ ] **Step 6: Run the comment guard**

```bash
python3 tools/check_comment_length.py
```

Expected: clean. The one block over two lines carries `long-comment:` on its **first** line.

- [ ] **Step 7: Commit**

```bash
git add src/Lodestar.Embeddings/Search/EmbeddingIndex.Block.cs \
        tests/Lodestar.Embeddings.Tests/EmbeddingIndexBlockTests.cs
git commit -m "Let a caller hand the block over instead of lending it

FromOwnedBlock keeps the array for the life of the index, which is a
longer invariant than Load(ReadOnlyMemory)'s and the one 0053 named as
the trap. It is asserted rather than only documented: the test writes to
the array afterwards and watches the score move."
```

---

### Task 3: The bench rows

**Files:**

- Modify: `bench/Lodestar.Text.Benchmarks/CrossLang/SidecarBench.cs`

**Interfaces:**

- Consumes: `FromBlock` from Task 1. `Rounds.Interleave(rows, repeats, warmups)` and `Rounds.Median(double[])`, which already exist in the bench project. `SidecarBench`'s existing private `Unbounded`, `Block` and `Floor`.
- Produces: nothing another task consumes; this task's output is the `sidecar` subcommand's report.

The row names are padded to **14 characters** so the report's columns line up with the four rows
already there (`"load artifact "`, `"read npy block"`, `"rebuild index "`, `"sidecar floor "`).

- [ ] **Step 1: Add the two rows**

In `SidecarBench.Run`, replace the `rows` collection expression:

```csharp
        (string Name, Action Work)[] rows =
        [
            ("load artifact ", () => GC.KeepAlive(EmbeddingIndex.Load(new MemoryStream(artifact)))),
            ("read npy block", () => GC.KeepAlive(NpyFile.Read(new MemoryStream(npy), Unbounded))),
            ("rebuild index ", () => Rebuild(block, count, dimension)),
            ("sidecar floor ", () => Floor(npy, count, dimension)),
        ];
```

with:

```csharp
        (string Name, Action Work)[] rows =
        [
            ("load artifact ", () => GC.KeepAlive(EmbeddingIndex.Load(new MemoryStream(artifact)))),
            ("read npy block", () => GC.KeepAlive(NpyFile.Read(new MemoryStream(npy), Unbounded))),
            ("rebuild index ", () => Rebuild(block, count, dimension)),
            ("sidecar floor ", () => Floor(npy, count, dimension)),
            ("ingest copy   ", () => IngestCopy(npy, dimension)),
            ("ingest only   ", () => IngestOnly(block, dimension)),
        ];
```

- [ ] **Step 2: Add the two methods**

Append to `SidecarBench`, below `Floor`:

```csharp
    /// <summary>The sidecar route as it will exist: the block read, then the bulk ingest.</summary>
    /// <remarks>
    /// This is the row issue #474's gate is read off. It is what <c>sidecar floor</c> bounds —
    /// the floor pays a read and one copy, and this pays a read and one copy into an index —
    /// so the two landing together is the finding, and this landing near
    /// <c>rebuild index</c> instead is the refusal.
    /// </remarks>
    private static void IngestCopy(byte[] npy, int dimension)
    {
        NpyBlock read = NpyFile.Read(new MemoryStream(npy), Unbounded);
        GC.KeepAlive(EmbeddingIndex.FromBlock(
            read.Values.Span, dimension, BlockNormalization.AlreadyNormalized));
    }

    /// <summary>The ingest alone, on a block already in hand.</summary>
    /// <remarks>
    /// Separates the ingest's cost from the read's, so a later regression in either can be
    /// attributed. There is deliberately no row for <c>FromOwnedBlock</c>: it assigns four
    /// fields and is constant time whatever the block's size, so its ceiling is the
    /// <c>read npy block</c> row and a row of its own would publish noise.
    /// </remarks>
    private static void IngestOnly(float[] block, int dimension)
    {
        GC.KeepAlive(EmbeddingIndex.FromBlock(
            block, dimension, BlockNormalization.AlreadyNormalized));
    }
```

- [ ] **Step 3: Add the ratio to the report**

Replace the report's last term:

```csharp
            $"{Environment.NewLine}load / floor    {medians[0] / medians[3]:F2}x   " +
            $"load / rebuild  {medians[0] / medians[2]:F2}x";
```

with:

```csharp
            $"{Environment.NewLine}load / floor    {medians[0] / medians[3]:F2}x   " +
            $"load / rebuild  {medians[0] / medians[2]:F2}x   " +
            $"load / ingest   {medians[0] / medians[4]:F2}x   " +
            $"ingest / floor  {medians[4] / medians[3]:F2}x";
```

- [ ] **Step 4: Add the `using` the new code needs**

`SidecarBench.cs` already has `using Lodestar.Embeddings.Search;`, which is where
`BlockNormalization` lives. No new using is needed; confirm by building rather than by reading.

- [ ] **Step 5: Build the bench project**

```bash
dotnet build bench/Lodestar.Text.Benchmarks -c Release
```

Expected: 0 warnings, 0 errors.

- [ ] **Step 6: Run the subcommand once, for shape only**

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- sidecar
```

Expected: six named rows and four ratios printed, and **exit code 0**. Check it with
`echo $?` — an unknown subcommand used to print a menu and exit 0, which is why #470 wasted a
run; it exits 2 now, and confirming that is cheap.

**The numbers this prints are not the lot's numbers.** The container has put `load / floor` at
0.73× where the runner puts it at 2.02×. This step proves the code runs, nothing else.

- [ ] **Step 7: Run the bench-map guard**

```bash
python3 tools/check_bench_map.py
```

Expected: clean. `SidecarBench` is already in `bench/bench-map.json`; adding methods to an
existing class does not change the map.

- [ ] **Step 8: Commit**

```bash
git add bench/Lodestar.Text.Benchmarks/CrossLang/SidecarBench.cs
git commit -m "Measure the route the ingest makes possible, and the ingest alone

Two rows beside the floor they are bounded by. No row for FromOwnedBlock:
adoption assigns four fields, so its ceiling is the block read that
already has one and a row of its own would publish noise."
```

---

### Task 4: The decision, the reference pages, and the gates

**Files:**

- Create: `docs/decisions/0056-a-block-may-be-adopted-and-the-invariant-is-the-callers-to-keep.md`
- Modify: `docs/decisions/README.md`
- Create: `docs/reference/embeddings/search/embeddingindex-fromblock.md`
- Create: `docs/reference/embeddings/search/embeddingindex-fromownedblock.md`
- Create: `docs/reference/embeddings/search/blocknormalization.md`
- Modify: `docs/reference/embeddings/search/embeddingindex.md`
- Modify: `samples/Lodestar.Sample/Lot3Embeddings.cs`
- Modify: `bench/README.md`
- Modify: `CHANGELOG.md`

**Interfaces:**

- Consumes: the three public members from Tasks 1 and 2, with the exact signatures given there.
- Produces: nothing another task consumes.

- [ ] **Step 1: Write ADR 0056**

Create `docs/decisions/0056-a-block-may-be-adopted-and-the-invariant-is-the-callers-to-keep.md`.
It must carry, in this order: a `**Status:** accepted · **Date:** 2026-08-29 · **Refines:**
[`0053`](0053-the-payload-buffer-is-not-pooled-because-residency-outlives-the-load.md)` line; a
**Context** section quoting 0053's exposure invariant and stating that `FromOwnedBlock` holds the
mirror image of it — the library reading a caller's array rather than a caller reading the
library's; a **Decision** section saying both factories ship public and the invariant is the
caller's to keep; and a **Consequences** section carrying the loser.

The loser is stated in full, because an ADR without one is a note:

> **What was refused** is the copying factory public with the adopting one an internal seam used
> only by the load path. It keeps the permanent invariant out of every caller's hands, and
> `NpyFile.Read`'s freshly allocated array — which nobody else holds — is exactly the case it
> would serve. It was refused for reach: a caller holding a block from anywhere else (a model's
> output, a memory-mapped file, a column store) would have no way to avoid a copy the library can
> see is unnecessary, and would have no way to ask for one.

And the condition that would reverse it:

> **What would change this decision** is evidence that the invariant is broken in practice — an
> issue reporting scores that drift, or a caller found returning an adopted array to a pool. The
> reversal is to make `FromOwnedBlock` internal, which is a source-breaking change to a published
> package and therefore a major version, not a patch.

- [ ] **Step 2: Add the 0056 row to the decision index**

In `docs/decisions/README.md`, add the row for 0056 in the same shape as 0055's, immediately
after it.

- [ ] **Step 3: Write the three reference pages**

Each follows the shape of
`docs/reference/embeddings/search/embeddingindex-add.md`: an H1 naming the member, one sentence,
a `<!-- docs-declaration -->` marker, a ```csharp fence holding the declaration, then
**Parameters**, **Returns**, **Exceptions**, **Example**, **Remarks**, **Applies to** — net10.0,
netstandard2.0 — and **See also**.

**The example fences are executed, and a trailing `// =>` is an assertion on the value.** Write
values that are actually produced, not values that look right. For
`embeddingindex-fromblock.md`:

```csharp
using Lodestar.Embeddings.Search;

float[] block = { 1f, 0f, 0f, 1f };
var index = EmbeddingIndex.FromBlock(block, dimension: 2, BlockNormalization.AlreadyNormalized, ["east", "north"]);

int count = index.Count;  // => 2
string? first = index.GetId(0);  // => east
```

For `embeddingindex-fromownedblock.md`, the example must show the invariant rather than hide it:

```csharp
using Lodestar.Embeddings.Search;

float[] block = { 3f, 4f };
var index = EmbeddingIndex.FromOwnedBlock(block, dimension: 2, BlockNormalization.Normalize);

// The index took the array, so normalization happened in the caller's own values.
float first = block[0];  // => 0.6
int count = index.Count;  // => 1
```

For `blocknormalization.md`, the example demonstrates the enum through a call that takes it,
which is the only way ADR 0041 accepts an enum being shown:

```csharp
using Lodestar.Embeddings.Search;

float[] raw = { 3f, 4f };
var normalizing = EmbeddingIndex.FromBlock(raw, dimension: 2, BlockNormalization.Normalize);
var verbatim = EmbeddingIndex.FromBlock(raw, dimension: 2, BlockNormalization.Off);

float scored = normalizing.Search(new float[] { 3f, 4f }, 1)[0].Score;  // => 1
float unscored = verbatim.Search(new float[] { 3f, 4f }, 1)[0].Score;  // => 25
```

**Every public member named in prose on these pages must be a link** to its own page, or
`ReferenceDocumentationTests` fails. That is the trap that has cost this repository three cycles.

- [ ] **Step 4: Link the new members from the index page**

In `docs/reference/embeddings/search/embeddingindex.md`, add
`[`EmbeddingIndex.FromBlock`](embeddingindex-fromblock.md)` and
`[`EmbeddingIndex.FromOwnedBlock`](embeddingindex-fromownedblock.md)` to the member table, in
the same shape as the `Add` row.

- [ ] **Step 5: Reference the new members from the sample**

In `samples/Lodestar.Sample/Lot3Embeddings.cs`, beside the existing `EmbeddingIndex` block that
starts at the `new EmbeddingIndex(dimension: 3, normalize: true)` line, add:

```csharp
        // The bulk ingest, which is what a caller holding a whole corpus reaches for. Both
        // factories appear because the packaging gate is a reference from outside the assembly.
        float[] corpus = [1f, 0f, 0f, 0f, 1f, 0f];
        EmbeddingIndex bulk = EmbeddingIndex.FromBlock(corpus, 3, BlockNormalization.Normalize);
        EmbeddingIndex owned = EmbeddingIndex.FromOwnedBlock(
            [0f, 0f, 1f], 3, BlockNormalization.AlreadyNormalized);

        Console.WriteLine($"  Bulk ingest      : {bulk.Count} + {owned.Count} vectors");
```

The `Console.WriteLine` needs no `console-print:` marker: `check_no_console_writeline.py` exempts
`samples/`, which is what the samples are for. Confirm that by running the guard rather than by
trusting this line.

- [ ] **Step 6: Extend `bench/README.md` §12**

Under `## 12. What a binary sidecar would buy (issue #436)`, change "Four rows" to "Six rows",
describe `ingest copy` and `ingest only`, and add the sentence that keeps the missing row from
looking like an oversight:

> There is no row for `FromOwnedBlock`. It assigns four fields and is constant time whatever the
> block's size, so its ceiling is the `read npy block` row and a row of its own would publish
> noise.

Leave the published numbers alone; they belong to `docs/guides/performance.md` and to Task 5.

- [ ] **Step 7: Write the CHANGELOG entry**

In `CHANGELOG.md`, under `## [Unreleased]`, in a `### Lodestar.Embeddings` / `#### Added`
subsection (creating either if it is not there), add one sentence in the established shape —
the sentence, the issue link, and the commit link:

```markdown
- `EmbeddingIndex.FromBlock` and `EmbeddingIndex.FromOwnedBlock` build an index from a contiguous block of vectors in one copy or none, where replaying the block through `Add` cost three times the read that produced it. ([#474](https://github.com/CyrilB1531/lodestar/issues/474), [`<sha>`](https://github.com/CyrilB1531/lodestar/commit/<sha>))
```

Fill `<sha>` with the short hash of Task 1's commit.

- [ ] **Step 8: Run every guard**

```bash
python3 tools/check_comment_length.py
python3 tools/check_no_console_writeline.py
python3 tools/check_bench_map.py
python3 tools/check_machine_paths.py --no-environment
python3 tools/check_sample_culture.py
python3 tools/check_version_floor.py
python3 tools/check_sample_coverage.py
git fetch origin main && python3 tools/check_adr_immutable.py --base origin/main
dotnet format Lodestar.slnx --verify-no-changes
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
```

Expected: every one clean. `check_adr_immutable.py` passes because 0056 is **new**; if it
complains, an accepted ADR was edited and the fix is a new ADR, never an edit.

- [ ] **Step 9: Compile and execute the documentation snippets**

```bash
python3 tools/extract_doc_snippets.py
dotnet build samples/Lodestar.DocSnippets -c Release
```

Expected: 0 errors. This is where a `// =>` value that was guessed rather than produced fails.

- [ ] **Step 10: Run the reference gate**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~ReferenceDocumentation"
```

Expected: PASS. A failure naming a member means either a signature drifted from its page or a
member was named in prose without a link.

- [ ] **Step 11: Pack and run the sample against the packages**

```bash
for p in src/Lodestar.Text src/Lodestar.Embeddings src/Lodestar.Fuzzy src/Lodestar.Metrics; do
  dotnet pack "$p" -c Release -o ./artifacts
done
NUGET_PACKAGES=$(mktemp -d) dotnet run --project samples/Lodestar.Sample -c Release
```

Expected: the sample runs and prints the bulk-ingest line. The isolated `NUGET_PACKAGES` is
required by ADR 0009 — without it the sample judges the published packages instead of the
working tree.

- [ ] **Step 12: Commit**

```bash
git add docs/ samples/ bench/README.md CHANGELOG.md
git commit -m "Record the adoption decision, and give the three members their pages

0056 carries the loser 0053's reasoning argues for: the adopting factory
internal, serving NpyFile.Read's own array and nobody else's. It was
refused for reach, and the condition that reverses it is a caller found
returning an adopted array to a pool."
```

---

### Task 5: The measurement, and the gate it decides

**Files:**

- Modify: `docs/guides/performance.md`
- Modify: `bench/README.md` (§12's numbers only)
- Modify: `docs/superpowers/specs/2026-08-29_0474_embeddingindex-cannot-take-a-block-whole.md` (a dated amendment block, if the measurement contradicts it)

**Interfaces:**

- Consumes: the `sidecar` subcommand from Task 3.
- Produces: the numbers ADR 0055's next lot argues from.

**This task cannot be completed by an agent alone.** The `Benchmark (on demand)` workflow is
dispatched by hand by the maintainer; the GitHub App is refused with a 403. Stop here, ask, and
resume when the run's output is available.

- [ ] **Step 1: Ask the maintainer to dispatch `Benchmark (on demand)` on this branch, with the `sidecar` subcommand**

- [ ] **Step 2: Read the six rows out of the run's log**

Record, for each of the six rows, the **median, the minimum and the maximum**, and the four
ratios. Name the runner. Medians alone hide the spread that told #470's story.

- [ ] **Step 3: Judge the gate**

`ingest copy` approaching **5.847 ms** and `load / ingest` clearing **1.0** — ideally near
**2.02×** — clears the precondition ADR 0055 set, and 0055's sidecar becomes buildable.

`ingest copy` landing near the rebuild's **17.973 ms** does not, and **the honest outcome is then
to say so and stop**: the pull request keeps the ingest and the measurement, the CHANGELOG entry
stands because the API shipped, and a follow-up issue carries what the ingest costs that the
floor does not. A refusal published is this repository's normal outcome — 0052, 0053 and #432 are
the model — and is not a failed lot.

- [ ] **Step 4: Publish the numbers where they belong**

Every number goes in `docs/guides/performance.md`, with the machine and the window named, because
that is the document whose subject is *what was measured*. `bench/README.md` §12 keeps only the
ratios and the sentence saying what they mean, because its subject is *how to measure*.

- [ ] **Step 5: Amend the spec if the measurement contradicts it**

The spec claims `ingest copy` will approach the floor. If it does not, the spec is superseded by
its own measurement: add a **dated amendment block** at its end saying what was measured and what
that changes. **Do not rewrite the body** — a spec records what was believed when the work
started, and this repository amends rather than rewrites.

- [ ] **Step 6: Commit and open the pull request**

```bash
git add docs/guides/performance.md bench/README.md docs/superpowers/specs/
git commit -m "Publish what the ingest measured, on the runner that measured it"
git push -u origin perf/474-ingest-a-block-whole
```

The pull request body carries the before/after numbers, names the machine, and says
`Closes #474`. A `perf/` pull request without both is incomplete.

---

## Self-Review

**Spec coverage.** Every section of the spec maps to a task: the shape and the enum's rationale to
Task 1, the two factories to Tasks 1 and 2, the `Restore` rewiring to Task 1 Step 5, the adoption
invariant and its ADR to Tasks 2 and 4, the corrected bench rows to Task 3, the reference,
packaging, changelog and guard obligations to Task 4, and the gate to Task 5. The spec's finding —
that `NpyBlock.Values` is a `ReadOnlyMemory<float>` and so the sidecar route reaches `FromBlock`
rather than `FromOwnedBlock` — is carried in Task 3's `IngestCopy` remark and needs no code.

**Type consistency.** `BlockNormalization` has the same three members everywhere. `Seed`,
`CheckBlock` and `CopyIds` keep the signatures Task 1 declares wherever Task 2 calls them.
`FromBlock` takes `ReadOnlySpan<float>` in every occurrence and `FromOwnedBlock` takes `float[]`
in every occurrence — the difference is the point, not a slip.

**Placeholder scan.** No step says "add validation", "handle edge cases" or "write tests for the
above"; every code step carries the code, and Task 5's two steps that cannot be executed by an
agent say so at the top of the task rather than pretending otherwise.

**What this plan cannot verify.** It was written in a session with no .NET SDK — the egress policy
denies `builds.dotnet.microsoft.com` — so none of its `dotnet` commands were run against this
tree. The code is read off the sources it extends and the expected test counts are counted from
the plan's own fences; both want checking by the first executor with a working toolchain, and a
count that disagrees is the plan's error, not the suite's.
