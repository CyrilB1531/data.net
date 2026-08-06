# EmbeddingIndex Persistence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Save and load a built `EmbeddingIndex` — vectors, per-vector ids and configuration — so a corpus is embedded once ([issue #62](https://github.com/CyrilB1531/data.net/issues/62)).

**Architecture:** One more artifact in the format ADR 0011 already defines: a JSON document opening with `$schema` and `version`, whose vector block is a base64 string of raw little-endian IEEE-754 bits — the shape the idf vector already uses. `EmbeddingIndex` becomes `partial`; the new members live in a sibling `.Persistence.cs`. Two internal helpers move to `src/Shared/Persistence/` so the reader-side guards are written once.

**Tech Stack:** C# on `net10.0` + `netstandard2.0`, `System.Text.Json`, xUnit, BenchmarkDotNet, Python 3 + numpy for the cross-language harness.

**Spec:** [`docs/superpowers/specs/2026-08-06-embedding-index-persistence-design.md`](../specs/2026-08-06-embedding-index-persistence-design.md)

## Global Constraints

- **Zero new dependencies.** `DataNet.Embeddings` carries ONNX Runtime, plus `System.Text.Json` on `netstandard2.0` only. Nothing else. `tools/check_nuspec_dependencies.py` asserts the dependency groups.
- **Both targets build:** `net10.0` and `netstandard2.0`. `BitConverter.SingleToInt32Bits`, `float.IsFinite`, `BinaryPrimitives.ReadSingleLittleEndian` and `MemoryMarshal.CreateSpan` **do not exist on `netstandard2.0`** — `MemoryMarshal.AsBytes`, `MemoryMarshal.Cast`, `float.IsNaN` and `float.IsInfinity` do.
- **Warnings are errors** repository-wide. XML documentation is generated, so every public member needs a `<summary>`.
- **Never `BinaryFormatter`**, no polymorphic deserialization. A loaded file is untrusted input: every count read from it is checked against `ArtifactLoadOptions` *before* it sizes a buffer, and exceeding a limit is `InvalidDataException`, never `OutOfMemoryException`.
- **English only** — code, comments, commit messages, docs.
- **Definition of done** (CONTRIBUTING.md §"Definition of done"), run from the repository root:

```bash
dotnet build DataNet.slnx -c Release && dotnet test DataNet.slnx -c Release && dotnet format DataNet.slnx --verify-no-changes
```

- Commit messages are a single imperative sentence saying what changed and why, with the trailer `Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>`.
- The branch is `feat/62-embedding-index-persistence`, already created, with the spec commit on it.

---

### Task 1: Share `ArtifactIo`, factor the base64 block

`ArtifactIo` is `internal` and lives in `DataNet.Text`. `DataNet.Embeddings` needs the same save/load skeleton, and the base64 bound-before-decode logic buried in `FeatureVocabularyJson` is the exact code the vector block needs — with a different element size. Both move to `src/Shared/Persistence/`, which `src/Directory.Build.props` compiles into every package that ships artifacts.

This task changes no behaviour. The existing suite is its test: it must be green before and after, and both packages must still build on both targets.

**Files:**

- Move: `src/DataNet.Text/Persistence/ArtifactIo.cs` → `src/Shared/Persistence/ArtifactIo.cs`
- Create: `src/Shared/Persistence/Base64Numbers.cs`
- Modify: `src/Directory.Build.props` (the `DataNetIncludesPersistence` item group)
- Modify: `src/DataNet.Text/Persistence/FeatureVocabularyJson.cs` (`WriteIdf`, `ReadIdf`)

**Interfaces:**

- Consumes: `ArtifactLimits`, `JsonArtifact` (already in `DataNet.Internal.Persistence`).
- Produces:
  - `DataNet.Internal.Persistence.ArtifactIo` — same members as today, new namespace.
  - `Base64Numbers.WriteDoubles(Utf8JsonWriter writer, string propertyName, IReadOnlyList<double> values)`
  - `Base64Numbers.ReadDoubles(ref Utf8JsonReader reader, string artifact, string propertyName, in ArtifactLimits limits) -> double[]`
  - `Base64Numbers.WriteSingles(Utf8JsonWriter writer, string propertyName, ReadOnlySpan<float> values)`
  - `Base64Numbers.ReadSingles(ref Utf8JsonReader reader, string artifact, string propertyName, in ArtifactLimits limits) -> float[]`

- [ ] **Step 1: Confirm the suite is green before touching anything**

```bash
dotnet test DataNet.slnx -c Release
```

Expected: PASS. If it is not green here, stop — this task's only proof is that the same suite is still green afterwards.

- [ ] **Step 2: Move `ArtifactIo` to the shared folder**

```bash
git mv src/DataNet.Text/Persistence/ArtifactIo.cs src/Shared/Persistence/ArtifactIo.cs
```

Then edit the moved file's header: delete the line `using DataNet.Internal.Persistence;` and change the namespace declaration.

```csharp
using System.Text.Json;

namespace DataNet.Internal.Persistence;
```

Nothing else in the file changes. The three call sites — `TfidfVectorizer.Persistence.cs`, `CountVectorizer.Persistence.cs`, `HashingVectorizer.Persistence.cs` — already carry `using DataNet.Internal.Persistence;`, so they need no edit.

- [ ] **Step 3: Compile it into both packages**

In `src/Directory.Build.props`, add one line to the item group guarded by `'$(DataNetIncludesPersistence)' == 'true'`, after the `JsonArtifact.cs` line:

```xml
    <Compile Include="$(MSBuildThisFileDirectory)Shared/Persistence/ArtifactIo.cs" Link="Internal/Persistence/ArtifactIo.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)Shared/Persistence/Base64Numbers.cs" Link="Internal/Persistence/Base64Numbers.cs" />
```

- [ ] **Step 4: Write `Base64Numbers`**

Create `src/Shared/Persistence/Base64Numbers.cs`:

```csharp
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace DataNet.Internal.Persistence;

/// <summary>
/// Reads and writes a numeric vector as one base64 string of raw little-endian
/// IEEE-754 bits — the encoding ADR 0011 chose for the parts of an artifact
/// nobody reads by eye.
/// </summary>
/// <remarks>
/// <para>
/// Encoding and bounds only: what a value <em>means</em> — whether a non-finite
/// entry is a broken model or the caller's own data — belongs to the artifact
/// that owns the vector, and is checked there.
/// </para>
/// <para>
/// Raw bits make the round trip exact by construction rather than by trusting a
/// decimal formatter, on any framework. Little-endian is written explicitly so a
/// file written on one architecture reads on another.
/// </para>
/// </remarks>
internal static class Base64Numbers
{
    /// <summary>Writes <paramref name="values"/> as a base64 property.</summary>
    public static void WriteDoubles(Utf8JsonWriter writer, string propertyName, IReadOnlyList<double> values)
    {
        byte[] raw = new byte[values.Count * sizeof(double)];
        for (int i = 0; i < values.Count; i++)
        {
            BinaryPrimitives.WriteInt64LittleEndian(
                raw.AsSpan(i * sizeof(double)),
                BitConverter.DoubleToInt64Bits(values[i]));
        }
        writer.WriteBase64String(propertyName, raw);
    }

    /// <summary>Reads a base64 property written by <see cref="WriteDoubles"/>.</summary>
    public static double[] ReadDoubles(
        ref Utf8JsonReader reader,
        string artifact,
        string propertyName,
        in ArtifactLimits limits)
    {
        byte[] raw = ReadRaw(ref reader, artifact, propertyName, limits, sizeof(double));
        var values = new double[raw.Length / sizeof(double)];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = BitConverter.Int64BitsToDouble(
                BinaryPrimitives.ReadInt64LittleEndian(raw.AsSpan(i * sizeof(double))));
        }
        return values;
    }

    /// <summary>Writes <paramref name="values"/> as a base64 property.</summary>
    /// <remarks>
    /// Bulk-copied rather than converted element by element the way
    /// <see cref="WriteDoubles"/> is. <c>BitConverter.SingleToInt32Bits</c> does not
    /// exist on <c>netstandard2.0</c>, and this block is the largest thing in any
    /// artifact — an embedding index is millions of floats where an idf vector is
    /// tens of thousands.
    /// </remarks>
    public static void WriteSingles(Utf8JsonWriter writer, string propertyName, ReadOnlySpan<float> values)
    {
        byte[] raw = new byte[values.Length * sizeof(float)];
        MemoryMarshal.AsBytes(values).CopyTo(raw);
        SwapIfBigEndian(raw);
        writer.WriteBase64String(propertyName, raw);
    }

    /// <summary>Reads a base64 property written by <see cref="WriteSingles"/>.</summary>
    public static float[] ReadSingles(
        ref Utf8JsonReader reader,
        string artifact,
        string propertyName,
        in ArtifactLimits limits)
    {
        byte[] raw = ReadRaw(ref reader, artifact, propertyName, limits, sizeof(float));
        SwapIfBigEndian(raw);
        var values = new float[raw.Length / sizeof(float)];
        MemoryMarshal.Cast<byte, float>(raw).CopyTo(values);
        return values;
    }

    /// <summary>
    /// Turns the buffer into little-endian, in place. A no-op on every platform
    /// .NET currently runs on — present so the format is defined by the file
    /// rather than by the architecture that happened to write it.
    /// </summary>
    private static void SwapIfBigEndian(byte[] raw)
    {
        if (BitConverter.IsLittleEndian)
        {
            return;
        }
        Span<int> words = MemoryMarshal.Cast<byte, int>(raw);
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = BinaryPrimitives.ReverseEndianness(words[i]);
        }
    }

    /// <summary>Decodes the property's base64 payload, bounded on both sides of the decode.</summary>
    /// <remarks>
    /// The encoded run is bounded <em>before</em> decoding: <c>TryGetBytesFromBase64</c>
    /// materialises the whole decoded buffer first, so checking only the decoded count
    /// would let the limit be satisfied by the allocation it exists to prevent. Four
    /// base64 characters carry three bytes.
    /// </remarks>
    private static byte[] ReadRaw(
        ref Utf8JsonReader reader,
        string artifact,
        string propertyName,
        in ArtifactLimits limits,
        int elementSize)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.String)
        {
            throw JsonArtifact.UnexpectedToken(artifact, propertyName, reader.TokenType);
        }

        long encodedLength = reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length;
        limits.CheckArrayLength(encodedLength * 3 / (4 * elementSize), propertyName);

        if (!reader.TryGetBytesFromBase64(out byte[]? raw))
        {
            throw JsonArtifact.Inconsistent(artifact, $"'{propertyName}' is not valid base64.");
        }
        if (raw.Length % elementSize != 0)
        {
            throw JsonArtifact.Inconsistent(
                artifact,
                $"'{propertyName}' does not hold a whole number of {elementSize * 8}-bit values ({raw.Length} bytes).");
        }

        limits.CheckArrayLength(raw.Length / elementSize, propertyName);
        return raw;
    }
}
```

- [ ] **Step 5: Delegate from `FeatureVocabularyJson`**

In `src/DataNet.Text/Persistence/FeatureVocabularyJson.cs`, keep both methods, their XML docs and their non-finite checks — those are idf semantics and stay here — and replace only the encoding halves.

`WriteIdf` becomes:

```csharp
    public static void WriteIdf(Utf8JsonWriter writer, IReadOnlyList<double> idf)
    {
        for (int i = 0; i < idf.Count; i++)
        {
            double value = idf[i];
            // Raw bits would carry these happily, where a JSON number could not. The
            // format's promise is that what it holds is a usable model, so the refusal
            // that WriteExactDouble applies to every other double applies here too.
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new InvalidDataException(
                    $"Cannot persist a non-finite idf weight at index {i}: the model is broken before it reaches the file.");
            }
        }
        Base64Numbers.WriteDoubles(writer, IdfProperty, idf);
    }
```

`ReadIdf` becomes:

```csharp
    public static double[] ReadIdf(ref Utf8JsonReader reader, string artifact, in ArtifactLimits limits)
    {
        double[] values = Base64Numbers.ReadDoubles(ref reader, artifact, IdfProperty, limits);
        for (int i = 0; i < values.Length; i++)
        {
            // Raw bits carry NaN and infinity perfectly well, where JSON numbers could
            // not. Left through, they turn every later Transform into NaN scores —
            // silently, and a long way from the file that caused it.
            if (double.IsNaN(values[i]) || double.IsInfinity(values[i]))
            {
                throw JsonArtifact.Inconsistent(
                    artifact,
                    $"'{IdfProperty}' holds a value that is not finite, at index {i}.");
            }
        }
        return values;
    }
```

Remove the now-unused `using System.Buffers.Binary;` from the top of the file if nothing else in it needs it.

- [ ] **Step 6: Verify nothing moved but the code**

```bash
dotnet build DataNet.slnx -c Release && dotnet test DataNet.slnx -c Release
```

Expected: PASS, same test count as Step 1. `ArtifactHardeningTests` asserts the messages `"is not valid base64"` and `"does not hold a whole number of 64-bit values"` — both are preserved verbatim by `ReadRaw` for `sizeof(double)`, so a failure there means the message string drifted.

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "$(cat <<'EOF'
Share the artifact skeleton the second package is about to need

ArtifactIo and the base64 block were internal to DataNet.Text. The index
artifact needs both, with a different element size, and the reader-side
guard — bound the encoded run before decoding, because TryGetBytesFromBase64
materialises the buffer the limit exists to prevent — is not code to write
a second time from memory.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Per-vector ids on the index

A persisted index whose items are anonymous integers is unusable: the caller's parallel array of documents dies with the process just as the vectors did. The index gains an optional id per vector, kept off the hot search path.

**Files:**

- Modify: `src/DataNet.Embeddings/Search/EmbeddingIndex.cs`
- Test: `tests/DataNet.Embeddings.Tests/EmbeddingIndexTests.cs`

**Interfaces:**

- Consumes: nothing from Task 1.
- Produces:
  - `public void Add(ReadOnlySpan<float> vector, string? id)`
  - `public string? GetId(int index)`
  - `public bool HasIds { get; }`
  - `private string? IdAt(int index)` and `private string?[]? _ids` — used by Tasks 3 and 4.

- [ ] **Step 1: Write the failing tests**

Append to `tests/DataNet.Embeddings.Tests/EmbeddingIndexTests.cs`, inside the existing test class:

```csharp
    [Fact]
    public void An_index_without_ids_reports_none()
    {
        var index = new EmbeddingIndex(dimension: 2);
        index.Add([1f, 0f]);

        Assert.False(index.HasIds);
        Assert.Null(index.GetId(0));
    }

    [Fact]
    public void An_id_is_recalled_by_position()
    {
        var index = new EmbeddingIndex(dimension: 2);
        index.Add([1f, 0f], "doc-1");
        index.Add([0f, 1f], "documento-café");

        Assert.True(index.HasIds);
        Assert.Equal("doc-1", index.GetId(0));
        Assert.Equal("documento-café", index.GetId(1));
    }

    [Fact]
    public void A_null_id_is_the_same_as_no_id_at_all()
    {
        var index = new EmbeddingIndex(dimension: 2);
        index.Add([1f, 0f], null);

        Assert.False(index.HasIds);
        Assert.Null(index.GetId(0));
    }

    [Fact]
    public void Ids_and_anonymous_vectors_mix_in_one_index()
    {
        var index = new EmbeddingIndex(dimension: 2);
        index.Add([1f, 0f]);
        index.Add([0f, 1f], "named");
        index.Add([1f, 1f]);

        Assert.True(index.HasIds);
        Assert.Null(index.GetId(0));
        Assert.Equal("named", index.GetId(1));
        Assert.Null(index.GetId(2));
    }

    [Fact]
    public void An_empty_id_is_kept_as_an_id()
    {
        var index = new EmbeddingIndex(dimension: 2);
        index.Add([1f, 0f], string.Empty);

        Assert.True(index.HasIds);
        Assert.Equal(string.Empty, index.GetId(0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void GetId_outside_the_index_is_rejected(int position)
    {
        var index = new EmbeddingIndex(dimension: 2);
        index.Add([1f, 0f], "only");

        Assert.Throws<ArgumentOutOfRangeException>(() => index.GetId(position));
    }
```

- [ ] **Step 2: Run them to verify they fail**

```bash
dotnet test tests/DataNet.Embeddings.Tests -c Release --filter "FullyQualifiedName~EmbeddingIndexTests"
```

Expected: FAIL to compile — `Add` has no two-argument overload, and neither `GetId` nor `HasIds` exists.

- [ ] **Step 3: Implement**

In `src/DataNet.Embeddings/Search/EmbeddingIndex.cs`, make the class partial and add the id state. The declaration becomes:

```csharp
public sealed partial class EmbeddingIndex
```

Add beside the existing fields:

```csharp
    private string?[]? _ids;
```

Add after the existing `Add`:

```csharp
    /// <summary>Adds a vector together with an opaque id the caller can recall after a reload.</summary>
    /// <param name="vector">The embedding, of length <see cref="Dimension"/>.</param>
    /// <param name="id">
    /// Anything identifying the document — a primary key, a URL, a path. Kept
    /// verbatim and never interpreted. <c>null</c> is exactly equivalent to
    /// <see cref="Add(ReadOnlySpan{float})"/>.
    /// </param>
    /// <remarks>
    /// A separate overload rather than an optional parameter on
    /// <see cref="Add(ReadOnlySpan{float})"/>: adding one would change that method's
    /// signature and break every already-compiled caller.
    /// </remarks>
    public void Add(ReadOnlySpan<float> vector, string? id)
    {
        Add(vector);
        if (id is null)
        {
            return;
        }

        // Allocated on the first id and no earlier: an index whose items are
        // anonymous pays nothing for a feature it does not use.
        _ids ??= new string?[_count];
        if (_ids.Length < _count)
        {
            Array.Resize(ref _ids, Math.Max(_count, _ids.Length * 2));
        }
        _ids[_count - 1] = id;
    }

    /// <summary>Whether any vector in this index carries an id.</summary>
    public bool HasIds => _ids is not null;

    /// <summary>The id of the item at <paramref name="index"/>, or <c>null</c> if it has none.</summary>
    /// <param name="index">A position in <c>[0, Count)</c> — a <see cref="SearchResult.Index"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the index.</exception>
    /// <remarks>
    /// The id is looked up here rather than carried on <see cref="SearchResult"/>.
    /// <see cref="Search"/> scores into a <c>SearchResult[Count]</c> and sorts it;
    /// the struct is 8 bytes the collector never has to look inside, and putting a
    /// reference in it would turn that hot array into one the GC must scan and the
    /// sort must move references through.
    /// </remarks>
    public string? GetId(int index)
    {
        if ((uint)index >= (uint)_count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"index must be in [0, {_count}).");
        }
        return IdAt(index);
    }

    /// <summary>The id at <paramref name="index"/>, unchecked, tolerating a short id buffer.</summary>
    /// <remarks>
    /// The buffer stops at the last item that was given an id, so positions past it
    /// are absent rather than null-filled.
    /// </remarks>
    private string? IdAt(int index) =>
        _ids is not null && index < _ids.Length ? _ids[index] : null;
```

- [ ] **Step 4: Run the tests**

```bash
dotnet test tests/DataNet.Embeddings.Tests -c Release --filter "FullyQualifiedName~EmbeddingIndexTests"
```

Expected: PASS, including the six new ones.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "$(cat <<'EOF'
Let a vector carry the id its document is known by

Without it a reloaded index is a wall of anonymous integers whose meaning
died with the parallel array the caller kept beside it.

The id is recalled through the index rather than carried on SearchResult:
Search scores into a SearchResult[Count] and sorts it, and a reference in
that struct would double it and hand the collector an array to scan on the
one path an exhaustive index exists to make fast.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: `Save` — the artifact writer

**Files:**

- Create: `src/DataNet.Embeddings/Search/EmbeddingIndex.Persistence.cs`
- Test: `tests/DataNet.Embeddings.Tests/Persistence/EmbeddingIndexPersistenceTests.cs`

**Interfaces:**

- Consumes: `ArtifactIo.Save`, `ArtifactIo.SaveAsync`, `JsonArtifact.OpenWrite`, `Base64Numbers.WriteSingles` (Task 1); `_ids`, `IdAt` (Task 2).
- Produces:
  - `public void Save(Stream destination)`, `public void Save(string path)`, `public Task SaveAsync(Stream destination, CancellationToken cancellationToken = default)`
  - The artifact constants Task 4 reads back: `ArtifactName = "embedding-index"`, `ArtifactVersion = 1`, and the property names `"dimension"`, `"normalize"`, `"count"`, `"ids"`, `"vectors"`.

- [ ] **Step 1: Write the failing tests**

Create `tests/DataNet.Embeddings.Tests/Persistence/EmbeddingIndexPersistenceTests.cs`:

```csharp
using System.Text;
using DataNet.Embeddings.Search;
using Xunit;

namespace DataNet.Embeddings.Tests.Persistence;

/// <summary>
/// Proves the round trip a persisted index exists for: embed once, query for as
/// long as the file lasts.
/// </summary>
/// <remarks>
/// Score comparisons are bitwise, not tolerant. A tolerance would hide the one
/// failure that matters — vectors that came back almost right and now rank a
/// corpus almost correctly, forever.
/// </remarks>
public sealed class EmbeddingIndexPersistenceTests
{
    [Fact]
    public void The_artifact_declares_its_kind_and_version()
    {
        string json = SaveToString(Sample());

        Assert.StartsWith("{\"$schema\":\"datanet/embedding-index\",\"version\":1,", json, StringComparison.Ordinal);
    }

    [Fact]
    public void The_artifact_carries_the_configuration_and_the_count()
    {
        string json = SaveToString(Sample());

        Assert.Contains("\"dimension\":3", json, StringComparison.Ordinal);
        Assert.Contains("\"normalize\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"count\":2", json, StringComparison.Ordinal);
        Assert.Contains("\"vectors\":\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void An_index_without_ids_writes_no_ids_section()
    {
        Assert.DoesNotContain("\"ids\"", SaveToString(Sample()), StringComparison.Ordinal);
    }

    [Fact]
    public void An_index_with_ids_writes_one_entry_per_vector()
    {
        var index = new EmbeddingIndex(dimension: 3);
        index.Add([1f, 0f, 0f], "first");
        index.Add([0f, 1f, 0f]);

        Assert.Contains("\"ids\":[\"first\",null]", SaveToString(index), StringComparison.Ordinal);
    }

    [Fact]
    public void An_empty_index_still_writes_a_complete_artifact()
    {
        string json = SaveToString(new EmbeddingIndex(dimension: 3));

        Assert.Contains("\"count\":0", json, StringComparison.Ordinal);
        Assert.Contains("\"vectors\":\"\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void A_non_finite_component_is_refused_rather_than_written()
    {
        var index = new EmbeddingIndex(dimension: 2, normalize: false);
        index.Add([1f, float.NaN]);

        using var stream = new MemoryStream();
        InvalidDataException error = Assert.Throws<InvalidDataException>(() => index.Save(stream));

        Assert.Contains("item 0", error.Message, StringComparison.Ordinal);
        Assert.Contains("component 1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Saving_leaves_the_callers_stream_open()
    {
        using var stream = new MemoryStream();
        Sample().Save(stream);

        // Would throw ObjectDisposedException if Save had disposed it.
        Assert.True(stream.CanWrite);
    }

    [Fact]
    public async Task Saving_asynchronously_writes_the_same_bytes()
    {
        EmbeddingIndex index = Sample();
        using var synchronous = new MemoryStream();
        using var asynchronous = new MemoryStream();

        index.Save(synchronous);
        await index.SaveAsync(asynchronous);

        Assert.Equal(synchronous.ToArray(), asynchronous.ToArray());
    }

    [Fact]
    public void Saving_to_a_path_writes_the_same_bytes()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            EmbeddingIndex index = Sample();
            index.Save(path);

            using var stream = new MemoryStream();
            index.Save(stream);
            Assert.Equal(stream.ToArray(), File.ReadAllBytes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_refused_save_does_not_leave_a_truncated_file_behind()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var index = new EmbeddingIndex(dimension: 2, normalize: false);
        index.Add([1f, float.PositiveInfinity]);

        Assert.Throws<InvalidDataException>(() => index.Save(path));
        Assert.False(File.Exists(path));
    }

    /// <summary>Two vectors of three dimensions, normalized on insertion.</summary>
    private static EmbeddingIndex Sample()
    {
        var index = new EmbeddingIndex(dimension: 3);
        index.Add([1f, 0f, 0f]);
        index.Add([0.6f, 0.8f, 0f]);
        return index;
    }

    private static string SaveToString(EmbeddingIndex index)
    {
        using var stream = new MemoryStream();
        index.Save(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
```

- [ ] **Step 2: Run them to verify they fail**

```bash
dotnet test tests/DataNet.Embeddings.Tests -c Release --filter "FullyQualifiedName~EmbeddingIndexPersistenceTests"
```

Expected: FAIL to compile — `Save` does not exist.

- [ ] **Step 3: Implement the writer**

Create `src/DataNet.Embeddings/Search/EmbeddingIndex.Persistence.cs`:

```csharp
using System.Text.Json;
using DataNet.Internal.Persistence;

namespace DataNet.Embeddings.Search;

public sealed partial class EmbeddingIndex
{
    private const string ArtifactName = "embedding-index";
    private const int ArtifactVersion = 1;
    private const string DimensionProperty = "dimension";
    private const string NormalizeProperty = "normalize";
    private const string CountProperty = "count";
    private const string IdsProperty = "ids";
    private const string VectorsProperty = "vectors";

    /// <summary>
    /// Writes the index — configuration, ids and the vector block — to
    /// <paramref name="destination"/> as UTF-8 JSON.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Building an index runs an encoder over every document in the corpus. This is
    /// what makes that a one-off: the vectors are written as one base64 string of
    /// raw little-endian IEEE-754 bits, so a reloaded index scores bit for bit what
    /// the original scored.
    /// </para>
    /// <para>
    /// A non-finite component is refused here even though <see cref="Add"/> accepts
    /// one. An artifact is read back by callers who will never see the code that
    /// built it, and a score that is <c>NaN</c> forever is worse than a save that
    /// failed at the point the broken vector was still in reach.
    /// </para>
    /// </remarks>
    /// <param name="destination">The stream to write to. Flushed but never disposed — the caller owns it.</param>
    /// <exception cref="InvalidDataException">A vector holds a non-finite component.</exception>
    public void Save(Stream destination) =>
        ArtifactIo.Save(destination, ArtifactName, ArtifactVersion, WriteArtifactBody);

    /// <summary>Writes the index to <paramref name="path"/>, replacing any existing file.</summary>
    /// <param name="path">The file to write. UTF-8 without a byte-order mark.</param>
    /// <exception cref="InvalidDataException">A vector holds a non-finite component.</exception>
    public void Save(string path)
    {
        // Before opening: OpenWrite truncates, so a refused save would otherwise
        // destroy a good artifact and leave a header where it used to be.
        EnsureFinite();
        using FileStream file = JsonArtifact.OpenWrite(path);
        Save(file);
    }

    /// <summary>Asynchronous counterpart of <see cref="Save(Stream)"/>.</summary>
    /// <param name="destination">The stream to write to; never disposed by this method.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <exception cref="InvalidDataException">A vector holds a non-finite component.</exception>
    public Task SaveAsync(Stream destination, CancellationToken cancellationToken = default) =>
        ArtifactIo.SaveAsync(destination, ArtifactName, ArtifactVersion, WriteArtifactBody, cancellationToken);

    private void WriteArtifactBody(Utf8JsonWriter writer)
    {
        EnsureFinite();
        writer.WriteNumber(DimensionProperty, _dim);
        writer.WriteBoolean(NormalizeProperty, _normalize);

        // Written before the block it describes, so a reader sizes its buffer from a
        // value it has already bounded rather than from the file's appetite.
        writer.WriteNumber(CountProperty, _count);

        if (_ids is not null)
        {
            writer.WriteStartArray(IdsProperty);
            for (int i = 0; i < _count; i++)
            {
                string? id = IdAt(i);
                if (id is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    writer.WriteStringValue(id);
                }
            }
            writer.WriteEndArray();
        }

        Base64Numbers.WriteSingles(writer, VectorsProperty, _data.AsSpan(0, _length));
    }

    /// <summary>Throws unless every stored component is a finite number.</summary>
    private void EnsureFinite()
    {
        ReadOnlySpan<float> data = _data.AsSpan(0, _length);
        for (int i = 0; i < data.Length; i++)
        {
            float value = data[i];
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new InvalidDataException(
                    $"Cannot persist a non-finite value at item {i / _dim}, component {i % _dim}. "
                    + "Add accepts such a vector; the artifact does not, because it would score NaN "
                    + "for every query a reloaded index is ever given.");
            }
        }
    }
}
```

`ArtifactLoadOptions` lives in `DataNet.Embeddings.Persistence`, but nothing here uses it yet — its `using` arrives with `Load` in Task 4. Adding it now fails the build, because `TreatWarningsAsErrors` promotes the unused-using warning.

- [ ] **Step 4: Run the tests**

```bash
dotnet test tests/DataNet.Embeddings.Tests -c Release --filter "FullyQualifiedName~EmbeddingIndexPersistenceTests"
```

Expected: PASS, all ten.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "$(cat <<'EOF'
Write the index out as the artifact ADR 0011 already defined

Configuration and ids stay readable JSON; the vector block is one base64
string of raw little-endian bits, the same asymmetry the idf vector uses
and for the same reason — nobody reads a million floats by eye.

count precedes the block it describes so a reader sizes from a value it
has already bounded, and Save(string) checks the vectors before it opens
the file rather than truncating a good artifact to discover they are bad.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: `Load` — the reader and the round trip

**Files:**

- Modify: `src/DataNet.Embeddings/Search/EmbeddingIndex.Persistence.cs`
- Test: `tests/DataNet.Embeddings.Tests/Persistence/EmbeddingIndexPersistenceTests.cs`

**Interfaces:**

- Consumes: everything Task 3 produced, plus `ArtifactHeader`, `ArtifactIo.CreateReader`, `ArtifactIo.EnsureEndOfDocument`, `ArtifactIo.Malformed`, `JsonArtifact.ReadAllBytes`, `JsonArtifact.ReadAllBytesAsync`, `JsonArtifact.OpenRead`, `Base64Numbers.ReadSingles`, `ArtifactLoadOptions.LimitsOf`.
- Produces: `public static EmbeddingIndex Load(Stream, ArtifactLoadOptions?)`, `public static EmbeddingIndex Load(string, ArtifactLoadOptions?)`, `public static Task<EmbeddingIndex> LoadAsync(Stream, ArtifactLoadOptions?, CancellationToken)`.

- [ ] **Step 1: Write the failing tests**

Append to `tests/DataNet.Embeddings.Tests/Persistence/EmbeddingIndexPersistenceTests.cs`:

```csharp
    [Fact]
    public void A_reloaded_index_scores_bit_for_bit_what_the_original_scored()
    {
        EmbeddingIndex original = Corpus(normalize: true);
        float[] query = [0.3f, -0.7f, 0.2f, 0.9f];

        EmbeddingIndex reloaded = RoundTrip(original);

        IReadOnlyList<SearchResult> before = original.Search(query, k: 4);
        IReadOnlyList<SearchResult> after = reloaded.Search(query, k: 4);
        Assert.Equal(before.Count, after.Count);
        for (int i = 0; i < before.Count; i++)
        {
            Assert.Equal(before[i].Index, after[i].Index);
            Assert.Equal(
                BitConverter.SingleToInt32Bits(before[i].Score),
                BitConverter.SingleToInt32Bits(after[i].Score));
        }
    }

    [Fact]
    public void The_configuration_survives_the_round_trip()
    {
        EmbeddingIndex reloaded = RoundTrip(Corpus(normalize: true));

        Assert.Equal(4, reloaded.Dimension);
        Assert.Equal(3, reloaded.Count);
    }

    [Fact]
    public void An_index_saved_unnormalized_comes_back_unnormalized()
    {
        // A vector of norm 5. If loading renormalized it — or if the flag were lost
        // and the query were normalized — this score could not be 25.
        var original = new EmbeddingIndex(dimension: 2, normalize: false);
        original.Add([3f, 4f]);

        EmbeddingIndex reloaded = RoundTrip(original);

        Assert.Equal(
            BitConverter.SingleToInt32Bits(25f),
            BitConverter.SingleToInt32Bits(reloaded.Search([3f, 4f], k: 1)[0].Score));
    }

    [Fact]
    public void The_same_vectors_saved_under_each_flag_load_differently()
    {
        // [2, 0] rather than [3, 4]: both its normalized form and its self-dot are
        // exactly representable, so the assertion can be bitwise without depending
        // on how the accumulation happened to round.
        var normalized = new EmbeddingIndex(dimension: 2, normalize: true);
        normalized.Add([2f, 0f]);
        var raw = new EmbeddingIndex(dimension: 2, normalize: false);
        raw.Add([2f, 0f]);

        float normalizedScore = RoundTrip(normalized).Search([2f, 0f], k: 1)[0].Score;
        float rawScore = RoundTrip(raw).Search([2f, 0f], k: 1)[0].Score;

        Assert.Equal(BitConverter.SingleToInt32Bits(1f), BitConverter.SingleToInt32Bits(normalizedScore));
        Assert.Equal(BitConverter.SingleToInt32Bits(4f), BitConverter.SingleToInt32Bits(rawScore));
    }

    [Fact]
    public void An_empty_index_round_trips()
    {
        EmbeddingIndex reloaded = RoundTrip(new EmbeddingIndex(dimension: 7));

        Assert.Equal(0, reloaded.Count);
        Assert.Equal(7, reloaded.Dimension);
        Assert.False(reloaded.HasIds);
    }

    [Fact]
    public void Ids_round_trip_including_the_awkward_ones()
    {
        var original = new EmbeddingIndex(dimension: 2);
        original.Add([1f, 0f], "documento-café");
        original.Add([0f, 1f], string.Empty);
        original.Add([1f, 1f]);
        original.Add([0f, 0.5f], "日本語");

        EmbeddingIndex reloaded = RoundTrip(original);

        Assert.True(reloaded.HasIds);
        Assert.Equal("documento-café", reloaded.GetId(0));
        Assert.Equal(string.Empty, reloaded.GetId(1));
        Assert.Null(reloaded.GetId(2));
        Assert.Equal("日本語", reloaded.GetId(3));
    }

    [Fact]
    public void An_index_without_ids_reloads_without_them()
    {
        Assert.False(RoundTrip(Sample()).HasIds);
    }

    [Fact]
    public void More_vectors_can_be_added_to_a_reloaded_index()
    {
        EmbeddingIndex reloaded = RoundTrip(Sample());
        reloaded.Add([0f, 0f, 1f], "added-later");

        Assert.Equal(3, reloaded.Count);
        Assert.Equal("added-later", reloaded.GetId(2));
    }

    [Fact]
    public void Loading_leaves_the_callers_stream_open()
    {
        using var stream = new MemoryStream();
        Sample().Save(stream);
        stream.Position = 0;

        EmbeddingIndex.Load(stream);

        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task Loading_asynchronously_produces_the_same_index()
    {
        using var stream = new MemoryStream();
        Sample().Save(stream);
        stream.Position = 0;

        EmbeddingIndex reloaded = await EmbeddingIndex.LoadAsync(stream);

        Assert.Equal(2, reloaded.Count);
        Assert.Equal(3, reloaded.Dimension);
    }

    [Fact]
    public void Loading_from_a_path_produces_the_same_index()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Sample().Save(path);
            EmbeddingIndex reloaded = EmbeddingIndex.Load(path);

            Assert.Equal(2, reloaded.Count);
            Assert.Equal(3, reloaded.Dimension);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Three vectors of four dimensions, deliberately not unit-length.</summary>
    private static EmbeddingIndex Corpus(bool normalize)
    {
        var index = new EmbeddingIndex(dimension: 4, normalize);
        index.Add([0.1f, 0.2f, 0.3f, 0.4f]);
        index.Add([-0.9f, 0.4f, 0.05f, 0.7f]);
        index.Add([0.33f, 0.33f, 0.33f, 0.33f]);
        return index;
    }

    private static EmbeddingIndex RoundTrip(EmbeddingIndex index)
    {
        using var stream = new MemoryStream();
        index.Save(stream);
        stream.Position = 0;
        return EmbeddingIndex.Load(stream);
    }
```

- [ ] **Step 2: Run them to verify they fail**

```bash
dotnet test tests/DataNet.Embeddings.Tests -c Release --filter "FullyQualifiedName~EmbeddingIndexPersistenceTests"
```

Expected: FAIL to compile — `EmbeddingIndex.Load` does not exist.

- [ ] **Step 3: Implement the reader**

Add `using DataNet.Embeddings.Persistence;` to the top of `src/DataNet.Embeddings/Search/EmbeddingIndex.Persistence.cs` — `ArtifactLoadOptions` lives there — and append:

```csharp
    /// <summary>
    /// Reads an index previously written by <see cref="Save(Stream)"/>, ready to
    /// <see cref="Search"/> without embedding the corpus again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Vectors are restored exactly as they were stored, never replayed through
    /// <see cref="Add"/>: they were normalized on insertion if the index normalized
    /// at all, and normalizing them a second time would move their bits.
    /// </para>
    /// <para>
    /// The normalization flag comes from the file and cannot be supplied by the
    /// caller. Reloading vectors under the other setting would produce scores that
    /// are quietly wrong rather than obviously so.
    /// </para>
    /// </remarks>
    /// <param name="source">The stream to read from; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The artifact is malformed, of the wrong kind, of an unsupported version, internally inconsistent, or exceeds a limit.</exception>
    public static EmbeddingIndex Load(Stream source, ArtifactLoadOptions? options = null)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        return FromPayload(JsonArtifact.ReadAllBytes(source, limits), limits);
    }

    /// <summary>Reads an index from <paramref name="path"/>.</summary>
    /// <param name="path">The artifact file, as written by <see cref="Save(string)"/>.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The artifact is malformed, of the wrong kind, of an unsupported version, internally inconsistent, or exceeds a limit.</exception>
    public static EmbeddingIndex Load(string path, ArtifactLoadOptions? options = null)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return Load(file, options);
    }

    /// <summary>Asynchronous counterpart of <see cref="Load(Stream, ArtifactLoadOptions?)"/>.</summary>
    /// <param name="source">The stream to read from; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <exception cref="InvalidDataException">The artifact is malformed, of the wrong kind, of an unsupported version, internally inconsistent, or exceeds a limit.</exception>
    public static async Task<EmbeddingIndex> LoadAsync(
        Stream source,
        ArtifactLoadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        byte[] payload = await JsonArtifact.ReadAllBytesAsync(source, limits, cancellationToken).ConfigureAwait(false);
        return FromPayload(payload, limits);
    }

    private static EmbeddingIndex FromPayload(byte[] payload, in ArtifactLimits limits)
    {
        try
        {
            return Parse(payload, limits);
        }
        catch (JsonException e)
        {
            throw ArtifactIo.Malformed(ArtifactName, e);
        }
    }

    private static EmbeddingIndex Parse(byte[] payload, in ArtifactLimits limits)
    {
        Utf8JsonReader reader = ArtifactIo.CreateReader(payload, ArtifactName, limits);
        var header = new ArtifactHeader(ArtifactName, ArtifactVersion);

        int? dimension = null;
        int? count = null;
        bool? normalize = null;
        string?[]? ids = null;
        float[]? vectors = null;

        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            string name = reader.GetString()!;
            if (header.TryConsume(ref reader, name))
            {
                continue;
            }
            switch (name)
            {
                case DimensionProperty:
                    dimension = JsonArtifact.ReadInt32(ref reader, ArtifactName, DimensionProperty);
                    break;
                case NormalizeProperty:
                    normalize = JsonArtifact.ReadBoolean(ref reader, ArtifactName, NormalizeProperty);
                    break;
                case CountProperty:
                    count = ReadCount(ref reader, limits);
                    break;
                case IdsProperty:
                    ids = ReadIds(ref reader, limits, count);
                    break;
                case VectorsProperty:
                    vectors = Base64Numbers.ReadSingles(ref reader, ArtifactName, VectorsProperty, limits);
                    break;
                default:
                    throw JsonArtifact.UnknownProperty(ArtifactName, name);
            }
        }

        ArtifactIo.EnsureEndOfDocument(ref reader, ArtifactName);
        header.EnsureComplete();
        return Restore(dimension, count, normalize, ids, vectors);
    }

    private static int ReadCount(ref Utf8JsonReader reader, in ArtifactLimits limits)
    {
        int count = JsonArtifact.ReadInt32(ref reader, ArtifactName, CountProperty);
        if (count < 0)
        {
            throw JsonArtifact.Inconsistent(ArtifactName, $"'{CountProperty}' is negative ({count}).");
        }
        limits.CheckArrayLength(count, CountProperty);
        return count;
    }

    /// <summary>Reads the id array, sized from the declared count when it arrived first.</summary>
    /// <remarks>
    /// The reader accepts reordered properties, so <c>ids</c> can precede the
    /// <c>count</c> that would have sized this buffer. The ceiling keeps a declared
    /// count from sizing the allocation on its own: the file has to actually deliver
    /// the entries before the buffer grows past it.
    /// </remarks>
    private static string?[] ReadIds(ref Utf8JsonReader reader, in ArtifactLimits limits, int? declaredCount)
    {
        JsonArtifact.ReadStartArray(ref reader, ArtifactName, IdsProperty);

        string?[] ids = new string?[
            declaredCount is int declared && declared > 0 ? Math.Min(declared, MaxPreallocatedIds) : 0];
        int read = 0;
        while (reader.Read() && reader.TokenType is JsonTokenType.String or JsonTokenType.Null)
        {
            string? id = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
            if (id is not null)
            {
                limits.CheckTokenLength(id.Length);
            }
            if (read == ids.Length)
            {
                Array.Resize(ref ids, ids.Length == 0 ? 4 : ids.Length * 2);
            }
            ids[read++] = id;
            limits.CheckArrayLength(read, IdsProperty);
        }
        if (reader.TokenType != JsonTokenType.EndArray)
        {
            throw JsonArtifact.UnexpectedToken(ArtifactName, IdsProperty, reader.TokenType);
        }

        if (read != ids.Length)
        {
            Array.Resize(ref ids, read);
        }
        return ids;
    }

    private const int MaxPreallocatedIds = 65_536;

    private static EmbeddingIndex Restore(
        int? dimension,
        int? count,
        bool? normalize,
        string?[]? ids,
        float[]? vectors)
    {
        if (dimension is not int dim)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, DimensionProperty);
        }
        if (normalize is not bool normalizeFlag)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, NormalizeProperty);
        }
        if (count is not int itemCount)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, CountProperty);
        }
        if (vectors is null)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, VectorsProperty);
        }
        if (dim < 1)
        {
            throw JsonArtifact.Inconsistent(
                ArtifactName,
                $"'{DimensionProperty}' must be at least 1, but the file declares {dim}.");
        }

        long expected = (long)itemCount * dim;
        if (vectors.LongLength != expected)
        {
            throw JsonArtifact.Inconsistent(
                ArtifactName,
                $"'{CountProperty}' is {itemCount} and '{DimensionProperty}' is {dim}, "
                + $"which needs {expected} values, but '{VectorsProperty}' holds {vectors.LongLength}.");
        }
        if (ids is not null && ids.Length != itemCount)
        {
            throw JsonArtifact.Inconsistent(
                ArtifactName,
                $"'{CountProperty}' is {itemCount} but '{IdsProperty}' holds {ids.Length} entries.");
        }
        EnsureFinite(vectors, dim);

        var index = new EmbeddingIndex(dim, normalizeFlag);
        index._data = vectors;
        index._length = vectors.Length;
        index._count = itemCount;
        index._ids = ids;
        return index;
    }
```

Then refactor `EnsureFinite` from Task 3 so both sides share one loop and one message. Replace the instance method with:

```csharp
    /// <summary>Throws unless every stored component is a finite number.</summary>
    private void EnsureFinite() => EnsureFinite(_data.AsSpan(0, _length), _dim);

    private static void EnsureFinite(ReadOnlySpan<float> data, int dimension)
    {
        for (int i = 0; i < data.Length; i++)
        {
            float value = data[i];
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new InvalidDataException(
                    $"Cannot persist a non-finite value at item {i / dimension}, component {i % dimension}. "
                    + "Add accepts such a vector; the artifact does not, because it would score NaN "
                    + "for every query a reloaded index is ever given.");
            }
        }
    }
```

The read side calls `EnsureFinite(vectors, dim)`, which reaches the same message — a file whose bits decode to `NaN` is refused exactly as writing one is.

- [ ] **Step 4: Run the tests**

```bash
dotnet test tests/DataNet.Embeddings.Tests -c Release --filter "FullyQualifiedName~EmbeddingIndexPersistenceTests"
```

Expected: PASS, all twenty-one.

- [ ] **Step 5: Check both targets still build**

```bash
dotnet build DataNet.slnx -c Release
```

Expected: clean. `netstandard2.0` is where `MemoryMarshal` and the absent `SingleToInt32Bits` would show up.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "$(cat <<'EOF'
Read the index back, verbatim and bit for bit

Vectors are restored straight into the buffer instead of being replayed
through Add: they were normalized on insertion, and normalizing them again
would move the very bits this artifact exists to preserve.

The normalization flag comes from the file and cannot be overridden by the
caller, because an index reloaded under the other setting scores wrongly
without ever looking wrong.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: Hardening — one refusal at a time

A persisted index is a file, and a file can come from anywhere. Every case here must fail with `InvalidDataException` and a message a caller can act on — never an unhandled parser error, an `OutOfMemoryException`, or worst of all a silent misread.

**Files:**

- Create: `tests/DataNet.Embeddings.Tests/Persistence/EmbeddingIndexHardeningTests.cs`
- Modify (only if a test exposes a gap): `src/DataNet.Embeddings/Search/EmbeddingIndex.Persistence.cs`

**Interfaces:**

- Consumes: `EmbeddingIndex.Save`, `EmbeddingIndex.Load`, `ArtifactLoadOptions` (Tasks 3 and 4).
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Write the failing tests**

Create `tests/DataNet.Embeddings.Tests/Persistence/EmbeddingIndexHardeningTests.cs`:

```csharp
using System.Text;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Search;
using Xunit;

namespace DataNet.Embeddings.Tests.Persistence;

/// <summary>
/// One test per way a saved index can arrive broken. The baseline is a real
/// artifact, mutated in the one place each case is about, so a test that stops
/// exercising its case fails rather than passing vacuously.
/// </summary>
public sealed class EmbeddingIndexHardeningTests
{
    [Fact]
    public void Input_that_is_not_json_is_rejected()
    {
        Assert.Contains("not well-formed JSON", Load("this is not json").Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Input_that_is_not_an_object_is_rejected()
    {
        Assert.Contains("must be a JSON object", Load("[1, 2, 3]").Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_truncated_artifact_is_rejected()
    {
        string json = Baseline();
        Assert.Contains(
            "not well-formed JSON",
            Load(json.Substring(0, json.Length / 2)).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Trailing_content_is_rejected()
    {
        Assert.Contains("Trailing content", Load(Baseline() + "{}").Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Another_artifact_kind_is_rejected()
    {
        string json = Baseline().Replace("datanet/embedding-index", "datanet/tfidf-vectorizer", StringComparison.Ordinal);
        Assert.Contains("datanet/embedding-index", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_format_version_is_rejected()
    {
        string json = Baseline().Replace("\"version\":1", "\"version\":2", StringComparison.Ordinal);
        Assert.Contains("Unsupported", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_header_is_rejected()
    {
        string json = Baseline().Replace("\"$schema\":\"datanet/embedding-index\",", "", StringComparison.Ordinal);
        Assert.Contains("$schema", Load(json).Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("\"dimension\":0")]
    [InlineData("\"dimension\":-3")]
    public void A_dimension_below_one_is_rejected(string replacement)
    {
        string json = Baseline().Replace("\"dimension\":3", replacement, StringComparison.Ordinal);
        Assert.Contains("dimension", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_negative_count_is_rejected()
    {
        string json = Baseline().Replace("\"count\":2", "\"count\":-1", StringComparison.Ordinal);
        Assert.Contains("negative", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_count_the_vector_block_cannot_support_is_rejected()
    {
        // The block holds 2 x 3 floats; claiming a thousand items must not allocate
        // for a thousand items.
        string json = Baseline().Replace("\"count\":2", "\"count\":1000", StringComparison.Ordinal);
        InvalidDataException error = Load(json);

        Assert.Contains("1000", error.Message, StringComparison.Ordinal);
        Assert.Contains("vectors", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_property_is_named()
    {
        string json = Baseline().Replace("\"normalize\":true,", "", StringComparison.Ordinal);
        Assert.Contains("normalize", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_property_is_rejected_rather_than_ignored()
    {
        string json = Baseline().Replace("\"count\":2", "\"shards\":4,\"count\":2", StringComparison.Ordinal);
        Assert.Contains("shards", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_vector_block_that_is_not_base64_is_rejected()
    {
        string json = ReplaceVectors(Baseline(), "not base64 at all!!");
        Assert.Contains("not valid base64", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_vector_block_that_is_not_whole_floats_is_rejected()
    {
        // Five bytes: one float and a quarter.
        string json = ReplaceVectors(Baseline(), Convert.ToBase64String(new byte[5]));
        Assert.Contains("32-bit values", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_vector_block_holding_NaN_is_rejected()
    {
        byte[] raw = new byte[2 * 3 * sizeof(float)];
        BitConverter.GetBytes(float.NaN).CopyTo(raw, 0);
        string json = ReplaceVectors(Baseline(), Convert.ToBase64String(raw));

        Assert.Contains("non-finite", Load(json).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_ids_section_of_the_wrong_length_is_rejected()
    {
        string json = WithIds().Replace("[\"a\",\"b\"]", "[\"a\"]", StringComparison.Ordinal);
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(new MemoryStream(Encoding.UTF8.GetBytes(json))));

        Assert.Contains("ids", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_ids_entry_of_the_wrong_type_is_rejected()
    {
        string json = WithIds().Replace("[\"a\",\"b\"]", "[\"a\",7]", StringComparison.Ordinal);
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(new MemoryStream(Encoding.UTF8.GetBytes(json))));

        Assert.Contains("ids", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_id_longer_than_the_limit_is_rejected()
    {
        var index = new EmbeddingIndex(dimension: 3);
        index.Add([1f, 0f, 0f], new string('x', 64));
        using var stream = new MemoryStream();
        index.Save(stream);
        stream.Position = 0;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(stream, new ArtifactLoadOptions { MaxTokenLength = 16 }));

        Assert.Contains("MaxTokenLength", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_artifact_larger_than_the_byte_limit_is_rejected()
    {
        using var stream = new MemoryStream();
        Sample().Save(stream);
        stream.Position = 0;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(stream, new ArtifactLoadOptions { MaxTotalBytes = 8 }));

        Assert.Contains("MaxTotalBytes", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_vector_block_over_the_array_limit_is_rejected_before_it_is_decoded()
    {
        using var stream = new MemoryStream();
        Sample().Save(stream);
        stream.Position = 0;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(stream, new ArtifactLoadOptions { MaxArrayLength = 2 }));

        Assert.Contains("MaxArrayLength", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_artifact_whose_properties_were_reordered_still_loads()
    {
        // A hand-edited file is a supported input: the writer's order is for
        // reproducibility, not for the reader's convenience.
        string json = "{\"vectors\":" + Extract(Baseline(), "\"vectors\":")
            + ",\"count\":2,\"normalize\":true,\"dimension\":3,"
            + "\"version\":1,\"$schema\":\"datanet/embedding-index\"}";

        EmbeddingIndex index = EmbeddingIndex.Load(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        Assert.Equal(2, index.Count);
        Assert.Equal(3, index.Dimension);
    }

    /// <summary>Two vectors of three dimensions, no ids.</summary>
    private static EmbeddingIndex Sample()
    {
        var index = new EmbeddingIndex(dimension: 3);
        index.Add([1f, 0f, 0f]);
        index.Add([0.6f, 0.8f, 0f]);
        return index;
    }

    private static string Baseline() => Save(Sample());

    private static string WithIds()
    {
        var index = new EmbeddingIndex(dimension: 3);
        index.Add([1f, 0f, 0f], "a");
        index.Add([0.6f, 0.8f, 0f], "b");
        return Save(index);
    }

    private static string Save(EmbeddingIndex index)
    {
        using var stream = new MemoryStream();
        index.Save(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Swaps the base64 payload of the vector block for <paramref name="payload"/>.</summary>
    private static string ReplaceVectors(string json, string payload)
    {
        int start = json.IndexOf("\"vectors\":\"", StringComparison.Ordinal) + "\"vectors\":\"".Length;
        int end = json.IndexOf('"', start);
        return json.Substring(0, start) + payload + json.Substring(end);
    }

    /// <summary>The JSON value following <paramref name="property"/>, to the closing brace.</summary>
    private static string Extract(string json, string property)
    {
        int start = json.IndexOf(property, StringComparison.Ordinal) + property.Length;
        return json.Substring(start, json.Length - start - 1);
    }

    private static InvalidDataException Load(string json) =>
        Assert.Throws<InvalidDataException>(
            () => EmbeddingIndex.Load(new MemoryStream(Encoding.UTF8.GetBytes(json))));
}
```

- [ ] **Step 2: Run them and read every failure**

```bash
dotnet test tests/DataNet.Embeddings.Tests -c Release --filter "FullyQualifiedName~EmbeddingIndexHardeningTests"
```

Expected: most pass on Task 4's code. Any that fail are a real gap — fix `EmbeddingIndex.Persistence.cs`, do not weaken the assertion. Two to watch:

- `A_vector_block_over_the_array_limit_is_rejected_before_it_is_decoded` — `MaxArrayLength = 2` must trip in `Base64Numbers.ReadRaw` on the *encoded* length, before `TryGetBytesFromBase64` allocates. If it only trips after, the bound is in the wrong place.
- `An_ids_entry_of_the_wrong_type_is_rejected` — a number inside `ids` ends the `while` loop, leaving the reader on a token that is not `EndArray`, which is what the explicit check after the loop is for.

- [ ] **Step 3: Re-run until green**

```bash
dotnet test tests/DataNet.Embeddings.Tests -c Release --filter "FullyQualifiedName~EmbeddingIndexHardeningTests"
```

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "$(cat <<'EOF'
Prove the loader refuses every shape of broken index file

An artifact is untrusted input, so each way it can arrive wrong gets a
test and a message naming what was wrong: a count the vector block cannot
support, an ids section of the wrong length, a block that is not whole
floats, a limit exceeded. The block limit is asserted on the encoded run
specifically, because decoding first would perform the allocation the
limit exists to prevent.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 6: Documentation, sample and changelog

**Files:**

- Modify: `docs/guides/embeddings.md` (§"Index a corpus and query it")
- Modify: `samples/DataNet.DocSnippets/SnippetContext.cs`
- Modify: `docs/equivalence.md` (new section after §"DataNet.Embeddings — vocabulary loaders", which ends at line 111)
- Modify: `CHANGELOG.md` (§"DataNet.Embeddings — 0.3.0", under `#### Added`)
- Modify: `samples/DataNet.Sample/Lot3Embeddings.cs`

**Interfaces:**

- Consumes: the public API of Tasks 2–4.
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Add the guide snippet**

In `docs/guides/embeddings.md`, after the existing fence in *Index a corpus and query it* and before the paragraph beginning "The search is an **exhaustive SIMD-vectorized**", insert:

````markdown
Embedding a corpus is the expensive half, and it only has to happen once. Save
the built index and reload it in the process that queries it:

```csharp
var index = new EmbeddingIndex(dimension: vector.Length);
foreach ((float[] v, string id) in corpusWithIds) index.Add(v, id);
index.Save("corpus.index.json");

// …later, in another process
EmbeddingIndex reloaded = EmbeddingIndex.Load("corpus.index.json");
SearchResult best = reloaded.Search(queryVector, k: 1)[0];
Console.WriteLine($"{reloaded.GetId(best.Index)}  score={best.Score:F3}");
```

The vectors are stored as raw IEEE-754 bits, so a reloaded index scores bit for
bit what the original scored — and the normalization flag travels in the file
rather than being supplied again on load, because an index reloaded under the
other setting would rank a corpus wrongly without ever looking wrong. The reader
bounds every count it reads against `ArtifactLoadOptions` before that count sizes
a buffer.
````

- [ ] **Step 2: Give the snippet its scaffolding**

Every C# fence in the guide is extracted and compiled. Add the two symbols the new fence uses to the `Embeddings` partial in `samples/DataNet.DocSnippets/SnippetContext.cs`, beside the existing `corpusVectors`:

```csharp
    /// <summary>The embedded corpus an index is filled from, with the ids it is queried by.</summary>
    public readonly (float[] Vector, string Id)[] corpusWithIds = [(new float[384], "doc-1")];
```

- [ ] **Step 3: Verify the snippet actually compiles**

```bash
python3 tools/extract_doc_snippets.py && dotnet build samples/DataNet.DocSnippets -c Release
```

Expected: clean build. A failure here is a snippet that would not have compiled for a reader either.

- [ ] **Step 4: Add the equivalence rows**

In `docs/equivalence.md`, insert a section after the *vocabulary loaders* table (before `## DataNet.Fuzzy — applied fuzzy matching`):

```markdown
## DataNet.Embeddings — index persistence

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
| `numpy.save(path, matrix)` | numpy | `index.Save(path)` / `index.Save(stream)` | Versioned JSON whose vector block is base64-encoded raw little-endian IEEE-754 bits, not a `.npy` memory dump. Carries the dimension, the normalization flag and an optional id per vector, none of which `.npy` has anywhere to put. |
| `numpy.load(path)` | numpy | `EmbeddingIndex.Load(path, options?)` | Static, not a constructor. Returns a queryable index rather than an array, and bounds every count against `ArtifactLoadOptions` before it sizes a buffer. |
| `faiss.write_index(idx, path)` / `faiss.read_index(path)` | faiss | `index.Save(path)` / `EmbeddingIndex.Load(path)` | Comparable in purpose, not in structure: DataNet's index is exhaustive (`IndexFlatIP`-shaped), so there is no graph or quantizer to serialize. An approximate index is a separate decision, not made. |
| — (a parallel `list[str]` the caller keeps) | — | `index.Add(vector, id)` / `index.GetId(i)` | Deliberate addition: without ids in the file, a reloaded index is a wall of anonymous integers. |
```

- [ ] **Step 5: Add the changelog entry**

In `CHANGELOG.md`, under `### DataNet.Embeddings — 0.3.0` → `#### Added`, append:

```markdown
- **`EmbeddingIndex.Save` / `EmbeddingIndex.Load`**, so a corpus is embedded
  once. Building an index runs an encoder over every document — seconds for a
  demo, hours for anything real — and that work used to die with the process.
  The artifact is the versioned JSON of
  [decision 0011](docs/decisions/0011-persistence-format.md) with the vector
  block as base64 raw IEEE-754 bits: a reloaded index scores bit for bit what
  the original scored. The normalization flag travels in the file rather than
  being supplied again on load, because an index reloaded under the other
  setting ranks a corpus wrongly without ever looking wrong.
- **`EmbeddingIndex.Add(vector, id)`, `GetId` and `HasIds`** — an opaque id per
  vector, kept off `SearchResult` so the array `Search` scores into stays eight
  bytes per hit and free of references for the collector to chase.
```

- [ ] **Step 6: Exercise the round trip from the sample**

In `samples/DataNet.Sample/Lot3Embeddings.cs`, replace the block that builds and queries the index with one that also persists it:

```csharp
        // Nearest-neighbour search over those vectors, with the ids a reloaded
        // index is queried by.
        var index = new EmbeddingIndex(dimension: 3, normalize: true);
        index.Add([1f, 0f, 0f], "east");
        index.Add([0f, 1f, 0f], "north");
        index.Add([0.9f, 0.1f, 0f], "east-north-east");
        IReadOnlyList<SearchResult> hits = index.Search([1f, 0f, 0f], k: 2);
        Console.WriteLine($"  EmbeddingIndex   : {index.Count} vectors of {index.Dimension} dims");
        foreach (SearchResult hit in hits)
        {
            Console.WriteLine($"    #{hit.Index} {index.GetId(hit.Index)} score={hit.Score:F4}");
        }

        // Embed once, query for as long as the artifact lasts.
        using var artifact = new MemoryStream();
        index.Save(artifact);
        artifact.Position = 0;
        EmbeddingIndex reloaded = EmbeddingIndex.Load(artifact);
        SearchResult best = reloaded.Search([1f, 0f, 0f], k: 1)[0];
        Console.WriteLine($"  Reloaded index   : {reloaded.Count} vectors, "
            + $"best '{reloaded.GetId(best.Index)}' score={best.Score:F4}");
        Console.WriteLine();
```

- [ ] **Step 7: Run the full definition of done**

```bash
dotnet build DataNet.slnx -c Release && dotnet test DataNet.slnx -c Release && dotnet format DataNet.slnx --verify-no-changes
```

Expected: all three clean. `dotnet format` works locally — trust its exit code.

- [ ] **Step 8: Commit**

```bash
git add -A && git commit -m "$(cat <<'EOF'
Show the round trip where a reader will actually meet it

The guide's search section stopped at a query, which is the cheap half;
the expensive half is the corpus behind it, and nothing told a reader it
need only be paid once. The snippet is extracted and compiled, so it is
checked rather than merely proofread.

equivalence.md names numpy.save as the nearest counterpart and says where
the two part company, and the sample now saves and reloads under the CI
job that runs against the packaged assemblies.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 7: Benchmark — what the format choice costs

The design chose JSON + base64 over a dedicated binary format knowing it costs 33% in size and buffers the payload before decoding. A `.npy` file is a short header followed by the raw little-endian block: it **is** what the binary alternative would have produced, so this measures the decision rather than describing it.

**Files:**

- Modify: `bench/DataNet.Text.Benchmarks/PersistenceBenchmarks.cs`
- Modify: `bench/DataNet.Text.Benchmarks/CrossLang/PersistenceCrossLang.cs`
- Modify: `bench/python/bench_persistence.py`
- Modify: `bench/README.md` (new §5)

**Interfaces:**

- Consumes: `EmbeddingIndex.Save`, `EmbeddingIndex.Load` (Tasks 3 and 4).
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Add the BenchmarkDotNet pair**

In `bench/DataNet.Text.Benchmarks/PersistenceBenchmarks.cs`, add `using DataNet.Embeddings.Search;` at the top, then the fields, the setup and the two benchmarks:

```csharp
    private EmbeddingIndex _index = null!;
    private byte[] _indexArtifact = [];
```

Inside `Setup()`, after the TF-IDF block:

```csharp
        _index = BuildIndex();
        using var indexStream = new MemoryStream();
        _index.Save(indexStream);
        _indexArtifact = indexStream.ToArray();
```

Then, after the existing benchmarks:

```csharp
    [Benchmark]
    public long EmbeddingIndexSave()
    {
        using var stream = new MemoryStream(_indexArtifact.Length);
        _index.Save(stream);
        return stream.Length;
    }

    [Benchmark]
    public EmbeddingIndex EmbeddingIndexLoad()
    {
        using var stream = new MemoryStream(_indexArtifact);
        return EmbeddingIndex.Load(stream);
    }

    /// <summary>
    /// Ten thousand vectors of 384 dimensions — the shape a sentence-transformer
    /// corpus actually has, and 15 MB of floats.
    /// </summary>
    /// <remarks>
    /// Generated from a fixed seed rather than read from the corpus directory: the
    /// cost of writing a float block depends on how many floats there are, not on
    /// what they are, and both language sides reproduce the same array from the same
    /// arithmetic without a committed fixture.
    /// </remarks>
    internal static EmbeddingIndex BuildIndex()
    {
        const int count = 10_000;
        const int dimension = 384;
        var index = new EmbeddingIndex(dimension, normalize: true);
        var vector = new float[dimension];
        uint state = 12_345;
        for (int item = 0; item < count; item++)
        {
            for (int i = 0; i < dimension; i++)
            {
                // xorshift32: the same three shifts are trivial to reproduce in Python.
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                vector[i] = (state & 0xFFFFFF) / (float)0xFFFFFF - 0.5f;
            }
            index.Add(vector, $"doc-{item}");
        }
        return index;
    }
```

- [ ] **Step 2: Run it on both targets**

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks        -- --filter '*EmbeddingIndex*'
dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter '*EmbeddingIndex*'
```

Expected: four rows with time and allocation figures. `PersistenceBenchmarks.cs` is already linked into the netstandard project, so nothing needs adding there. Record the numbers — they go into the README in Step 6.

- [ ] **Step 3: Add the cross-language C# side**

In `bench/DataNet.Text.Benchmarks/CrossLang/PersistenceCrossLang.cs`, add `using DataNet.Embeddings.Search;`, then inside `Run()` before the `results` list:

```csharp
        EmbeddingIndex index = PersistenceBenchmarks.BuildIndex();
        byte[] indexArtifact;
        using (var stream = new MemoryStream())
        {
            index.Save(stream);
            indexArtifact = stream.ToArray();
        }
```

and two entries at the end of the `results` list:

```csharp
            Measure("embedding_index_save", () =>
            {
                using var stream = new MemoryStream(indexArtifact.Length);
                index.Save(stream);
                return stream.Length;
            }),
            Measure("embedding_index_load", () =>
            {
                using var stream = new MemoryStream(indexArtifact);
                return EmbeddingIndex.Load(stream);
            }),
```

- [ ] **Step 4: Add the Python side**

In `bench/python/bench_persistence.py`, add `import io` and `import numpy as np` to the imports, then before `results`:

```python
    vectors = build_vectors()
    buffer = io.BytesIO()
    np.save(buffer, vectors)
    npy_bytes = buffer.getvalue()
```

two entries at the end of the `results` list:

```python
        measure("embedding_index_save", lambda: np.save(io.BytesIO(), vectors)),
        measure("embedding_index_load", lambda: np.load(io.BytesIO(npy_bytes))),
```

and the generator, beside `measure`:

```python
def build_vectors() -> "np.ndarray":
    """A 10 000 x 384 block, from the same xorshift32 seed the C# side uses.

    The two blocks are the same size and come from the same generator; they are not
    bit-identical, and do not need to be -- DataNet normalizes on insertion, and what
    is being timed is how many floats there are rather than which ones.

    A .npy file is a short header followed by the raw little-endian block, so this
    row is the binary floor DataNet's JSON + base64 artifact is measured against --
    not a competitor doing the same job, a lower bound on the job itself.
    """
    count, dimension = 10_000, 384
    out = np.empty((count, dimension), dtype=np.float32)
    state = 12345
    for item in range(count):
        for i in range(dimension):
            # Plain ints masked to 32 bits, which is what C#'s uint does anyway --
            # numpy's uint32 raises on the overflow these shifts depend on.
            state = (state ^ (state << 13)) & 0xFFFFFFFF
            state ^= state >> 17
            state = (state ^ (state << 5)) & 0xFFFFFFFF
            out[item, i] = (state & 0xFFFFFF) / 0xFFFFFF - 0.5
    return out
```

Also add `"numpy": version("numpy")` to the `libraries` dictionary in the metadata payload.

- [ ] **Step 5: Run both sides back to back**

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- compare-persistence
python bench/python/bench_persistence.py
python bench/compare.py persistence
```

Expected: a table with the two new rows. Run them on an idle machine, back to back, and use one run for both — picking per-row winners from different runs would flatter whichever side was measured last.

If the Python generator is too slow to sit in the harness, precompute it once and cache it in `bench/corpus/vectors_10k_384.npy` — but only if measured, and say so in the README.

- [ ] **Step 6: Write §5 of `bench/README.md`**

Add a section after §4, following its conventions — the exact machine and runtime versions, both the wall and cpu columns, and a paragraph reading the numbers. The skeleton, with the figures from Steps 2 and 5 filled in:

```markdown
## 5. Persisting an embedding index (issue #62)

`EmbeddingIndex.Save` and `Load` on 10 000 vectors of 384 dimensions — 15 MB of
floats, the shape a sentence-transformer corpus has. The array is generated from
a fixed xorshift32 seed on both sides rather than committed: what a float block
costs to write depends on how many floats it holds, not on what they are.

### net10 vs netstandard2.0

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks        -- --filter '*EmbeddingIndex*'
dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter '*EmbeddingIndex*'
```

| Operation | net10 | netstandard2.0 | net10 alloc | ns2.0 alloc |
| --- | --- | --- | --- | --- |
| `EmbeddingIndexSave` | … | … | … | … |
| `EmbeddingIndexLoad` | … | … | … | … |

### vs numpy — what the format choice costs

`numpy.save` writes a short header followed by the raw little-endian block. That
is precisely what a dedicated binary format for this artifact would have
produced, so this comparison measures the decision recorded in
[0011](../docs/decisions/0011-persistence-format.md) rather than illustrating it.

| Operation | DataNet | numpy | wall | DataNet cpu | numpy cpu | **cpu** |
| --- | --- | --- | --- | --- | --- | --- |
| `embedding_index_save` | … | … | … | … | … | … |
| `embedding_index_load` | … | … | … | … | … | … |

| | DataNet artifact | `.npy` |
| --- | --- | --- |
| bytes on disk | … | … |

### Reading the numbers

[Fill in from the measurement: the size ratio should be close to 4/3, the base64
encode and decode is the difference in time, and the load path allocates the
payload buffer and the decoded array where numpy allocates one block. Say plainly
whether the cost is what the design predicted — and if it is not, say that
instead.]
```

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "$(cat <<'EOF'
Measure what the JSON artifact costs against the binary floor

The design took base64 inside JSON over a dedicated binary format, on the
grounds that the ADR already made that call and that the extra 33% and the
buffered decode were worth one format instead of two. numpy.save writes a
header and the raw block, which is exactly what the alternative would have
written, so the comparison prices the decision rather than restating it.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 8: Open the pull request

- [ ] **Step 1: Run the definition of done one last time**

```bash
dotnet build DataNet.slnx -c Release && dotnet test DataNet.slnx -c Release && dotnet format DataNet.slnx --verify-no-changes
```

Expected: all three clean.

- [ ] **Step 2: Confirm the dependency edges did not move**

```bash
python3 tools/check_nuspec_dependencies.py
```

Expected: PASS. This work adds no package reference; a failure means one crept in.

- [ ] **Step 3: Push and open the PR**

```bash
git push -u origin feat/62-embedding-index-persistence
```

Then open a pull request titled after what the branch does, whose body states: the artifact shape and why it is JSON rather than binary (ADR 0011, which named this artifact), the id decision and why the id is not on `SearchResult`, the non-finite refusal and the wart it accepts, the benchmark result against `.npy`, and `Closes #62`. The maintainer merges — do not merge.

---

## Self-review

**Spec coverage.** Format and ADR reasoning → Tasks 1, 3, 4. Public API including the six overloads → Tasks 2–4. Internal moves → Task 1. Artifact shape, load semantics, refusal table, non-finite rule → Tasks 3–5. Tests → Tasks 2–5. Documentation, equivalence, changelog, sample, no-new-ADR → Task 6. Benchmark → Task 7. Out-of-scope HNSW → not implemented anywhere, as required.

**Acceptance criteria of #62.** Bit-exact round trip (Task 4, `A_reloaded_index_scores_bit_for_bit_what_the_original_scored`); `Count`/`Dimension`/normalize asserted (Task 4); metadata including non-ASCII and empty id (Tasks 2 and 4); the two normalization flags distinguishable and no silent renormalization (Task 4); malformed input — truncated, unknown version, dimension 0 or negative, count inconsistent with the block, oversized (Task 5); empty index (Tasks 3 and 4); guide snippet (Task 6); equivalence rows (Task 6); XML documentation on every public member (Tasks 2–4).
