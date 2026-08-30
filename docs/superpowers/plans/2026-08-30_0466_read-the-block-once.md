# Read the block once Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Take `NpyFile.Read` from three copies of the vector block to one, and give the block a way to surrender its array so the route into an `EmbeddingIndex` reaches numpy's single copy.

**Architecture:** `Read(Stream)` becomes three staged reads — prefix, header, payload — with the payload read straight into the `float[]` the block will own, so no intermediate byte buffer exists. A new `Read(ReadOnlyMemory<byte>)` overload serves a caller that already holds the bytes by aliasing them through an internal `MemoryManager<float>`, copying nothing. `NpyBlock` gains an `OwnedArray` property that only the stream reader fills, which is what makes adoption by `EmbeddingIndex.FromOwnedBlock` safe.

**Tech Stack:** C# on `net10.0;netstandard2.0`, xunit, fixtures written by numpy 2.4.6 itself.

**Spec:** [`docs/superpowers/specs/2026-08-30_0466_the-npy-read-copies-the-block-three-times.md`](../specs/2026-08-30_0466_the-npy-read-copies-the-block-three-times.md)

**Branch:** `fix/466-npy-read-copies-the-block-twice` (already created; carries the `.ToArray()` removal and the spec)

**Issue:** [#466](https://github.com/CyrilB1531/lodestar/issues/466) — the pull request says `Closes #466` and closes nothing else.

## Global Constraints

- **Both target frameworks, one public API.** `net10.0;netstandard2.0`, equivalent behaviour through conditional compilation, never a reduced API.
- **Warnings are errors.** `SonarAnalyzer.CSharp` and the .NET code-quality rules at `AnalysisMode=All`, `AnalysisLevel=10.0`. A `#pragma warning disable` needs a reason a reviewer could disagree with — if you reach for one, stop and say so.
- **Comments say why, not what.** Two lines inline. The `long-comment:` marker is **exceptional** — one source file on `main` carries one — so cut the comment to length before reaching for it. XML documentation prose caps at eight lines.
- **`python3 tools/check_comment_length.py` sees untracked files** since #474, but stage or commit before trusting any guard.
- **A public member named in prose in a documented page must be linked** to its reference page, or `ReferenceDocumentationTests` fails. This has cost this repository four cycles, one of them on the branch that shipped #474.
- **Read the test count, not the colour**, and read whole result lines: a `--filter` matching nothing exits zero, and grepping for `Passed!` hides the assemblies that printed `Failed!`.
- **Timings come from `Benchmark (on demand)` only**, dispatched by the maintainer.
- **No fixture may change.** The `.npy` files under `tests/oracles/` were written by numpy itself, refusal cases included. Byte-for-byte output and every refusal message are the contract.
- Everything in English; commit messages carry no `feat:`/`fix:` prefix.

---

## The netstandard2.0 split, which the spec does not cover

`Stream.ReadExactly(Span<byte>)` is .NET 7 and later. `netstandard2.0` has neither it nor
`Stream.Read(Span<byte>)`, so filling a `float[]` viewed as bytes needs an intermediate array
there — **two copies on that framework, one on net10.0**.

This is the shape the repository already accepts for `VectorMath.Dot`: `Vector<T>` on net10,
scalar loop on netstandard2.0, one public API and one behaviour. The plan carries it as a
deliberate split rather than discovering it, and Task 5 amends the spec, which claims one copy
without qualification.

---

## File Structure

| file | responsibility |
| --- | --- |
| `src/Shared/Persistence/StreamFill.cs` | **create** — fill a span from a stream exactly, with the two-target split |
| `src/Lodestar.Embeddings/Persistence/NpyFile.cs` | **modify** — the staged stream read, the memory overload, two extracted header helpers |
| `src/Lodestar.Embeddings/Persistence/NpyPayloadManager.cs` | **create** — the `MemoryManager<float>` that aliases a byte payload |
| `tests/Lodestar.Embeddings.Tests/Persistence/NpyFileTests.cs` | **modify** — non-seekable, payload truncation, aliasing, adoption |
| `docs/decisions/0057-…md` | **create** — the two contracts, and the view-only design as its loser |
| `docs/reference/embeddings/persistence/npyfile-read.md` | **modify** — the new overload |
| `docs/reference/embeddings/persistence/npyblock.md` | **modify** — `OwnedArray` |
| `samples/Lodestar.Sample/Lot3Embeddings.cs` | **modify** — a use of the memory overload |
| `docs/equivalence.md`, `CHANGELOG.md`, `bench/README.md`, `docs/decisions/README.md` | **modify** |

---

### Task 1: Extract the two header helpers, changing nothing

`ReadHeader` decides the header-length width from the version and reads the declared length. The
staged stream read needs both facts before it knows how many bytes to ask for, and duplicating
either would put the version refusal message in two places.

**Files:**

- Modify: `src/Lodestar.Embeddings/Persistence/NpyFile.cs`

**Interfaces:**

- Produces: `private static int LengthSize(byte major, byte minor)` and `private static int HeaderTotal(ReadOnlySpan<byte> prefix)`, both consumed by Task 2.

- [ ] **Step 1: Run the existing tests and record the count**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~NpyFile"
```

Expected: PASS, **23 tests per assembly**, two assemblies. Write the number down; every later step compares against it.

- [ ] **Step 2: Extract `LengthSize`**

Add to `NpyFile`, and replace the `switch` inside `ReadHeader` with a call to it:

```csharp
    /// <summary>The width of a version's header-length field: 2 for 1.0, 4 for 2.0.</summary>
    /// <remarks>
    /// Shared with the staged stream read, which needs the width before it knows how many
    /// bytes the header occupies. 3.0 is UTF-8 and would likely read the same, but is
    /// refused rather than assumed: none has been seen.
    /// </remarks>
    private static int LengthSize(byte major, byte minor) => (major, minor) switch
    {
        (1, 0) => 2,
        (2, 0) => 4,
        _ => throw Malformed($"is version {major}.{minor}, which this does not read."),
    };
```

- [ ] **Step 3: Extract `HeaderTotal`**

Add to `NpyFile`:

```csharp
    /// <summary>How many bytes the header occupies, prefix included, from the first 12.</summary>
    /// <remarks>
    /// Twelve is the largest fixed prefix — six of magic, two of version, four of length —
    /// so one read of that size always carries the declared length whichever version it is.
    /// </remarks>
    private static int HeaderTotal(ReadOnlySpan<byte> prefix)
    {
        int size = LengthSize(prefix[VersionOffset], prefix[VersionOffset + 1]);
        int lengthOffset = VersionOffset + 2;
        int declared = size == 2
            ? BinaryPrimitives.ReadUInt16LittleEndian(prefix[lengthOffset..])
            : checked((int)BinaryPrimitives.ReadUInt32LittleEndian(prefix[lengthOffset..]));
        return checked(lengthOffset + size + declared);
    }
```

- [ ] **Step 4: Run the tests again**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~NpyFile"
```

Expected: PASS, **the same 23 per assembly**. A different count means the extraction changed behaviour and must be reverted, not accommodated.

- [ ] **Step 5: Commit**

```bash
git add src/Lodestar.Embeddings/Persistence/NpyFile.cs
git commit -m "Give the header's two facts a name each, for a reader that stages

The staged stream read needs the header-length width and the declared
length before it knows how many bytes to ask for. Extracted rather than
duplicated, so the version refusal keeps one message and one place."
```

---

### Task 2: Read the stream straight into the array

**Files:**

- Create: `src/Shared/Persistence/StreamFill.cs`
- Modify: `src/Lodestar.Embeddings/Persistence/NpyFile.cs`
- Test: `tests/Lodestar.Embeddings.Tests/Persistence/NpyFileTests.cs`

**Interfaces:**

- Consumes: `LengthSize`, `HeaderTotal` from Task 1.
- Produces: `NpyFile.Read(Stream, ArtifactLoadOptions?)` reading in one copy on net10.0; `StreamFill.Exactly(Stream, Span<byte>, string)`.

- [ ] **Step 1: Write the failing tests**

Append to `NpyFileTests`:

```csharp
    /// <summary>A stream that reports no length and no seeking, like a network body.</summary>
    private sealed class ForwardOnlyStream(byte[] bytes) : Stream
    {
        private readonly MemoryStream _inner = new(bytes);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            _inner.Read(buffer, offset, Math.Min(count, 7));

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public void A_forward_only_stream_reads_the_same_block()
    {
        byte[] npy = WrittenBlock([1f, 2f, 3f, 4f], 2, 2);

        NpyBlock block = NpyFile.Read(new ForwardOnlyStream(npy));

        Assert.Equal([1f, 2f, 3f, 4f], block.Values.ToArray());
        Assert.Equal([2, 2], block.Shape);
    }

    [Fact]
    public void A_stream_truncated_inside_its_payload_is_refused()
    {
        byte[] npy = WrittenBlock([1f, 2f, 3f, 4f], 2, 2);

        // Four bytes short: the header is whole and the block is not, which is the case
        // the staged read detects at the stream rather than on a complete buffer.
        var truncated = new MemoryStream(npy[..(npy.Length - 4)]);

        InvalidDataException e = Assert.Throws<InvalidDataException>(() => NpyFile.Read(truncated));
        Assert.Contains("bytes of data where its shape needs", e.Message, StringComparison.Ordinal);
    }

    /// <summary>A .npy of the given block, as NpyFile writes one.</summary>
    private static byte[] WrittenBlock(float[] values, params int[] shape)
    {
        using var stream = new MemoryStream();
        NpyFile.Write(stream, values, shape);
        return stream.ToArray();
    }
```

- [ ] **Step 2: Run them to verify they fail**

```bash
dotnet test tests/Lodestar.Embeddings.Tests -c Release --filter "FullyQualifiedName~NpyFile"
```

Expected: `A_forward_only_stream_reads_the_same_block` fails — the current path calls `JsonArtifact.ReadAllBytes`, which needs a length or grows a buffer, and the truncation message differs. Record which of the two fail and how; that is the reproduction.

- [ ] **Step 3: Write the fill helper**

Create `src/Shared/Persistence/StreamFill.cs`:

```csharp
namespace Lodestar.Internal.Persistence;

/// <summary>Fills a span from a stream, or refuses because the stream ended first.</summary>
internal static class StreamFill
{
    /// <summary>Reads exactly <paramref name="destination"/>.Length bytes, or throws.</summary>
    /// <remarks>
    /// The net10 path reads into the destination directly, which is the whole point: a
    /// block read this way is copied once. netstandard2.0 has no Stream.Read(Span), so it
    /// stages through a chunk and pays a second copy — the deliberate split VectorMath.Dot
    /// already makes, one API and one behaviour at two speeds.
    /// </remarks>
    /// <exception cref="InvalidDataException">The stream ended before the span was full.</exception>
    public static void Exactly(Stream stream, Span<byte> destination, string shortMessage)
    {
        int filled = 0;
#if NET7_0_OR_GREATER
        while (filled < destination.Length)
        {
            int read = stream.Read(destination[filled..]);
            if (read == 0)
            {
                throw new InvalidDataException(shortMessage);
            }
            filled += read;
        }
#else
        byte[] chunk = new byte[Math.Min(81_920, destination.Length)];
        while (filled < destination.Length)
        {
            int read = stream.Read(chunk, 0, Math.Min(chunk.Length, destination.Length - filled));
            if (read == 0)
            {
                throw new InvalidDataException(shortMessage);
            }
            chunk.AsSpan(0, read).CopyTo(destination[filled..]);
            filled += read;
        }
#endif
    }

    /// <summary>Reads up to <paramref name="destination"/>.Length bytes, returning how many.</summary>
    /// <remarks>Used where a short read is a refusal the caller words, not this one.</remarks>
    public static int UpTo(Stream stream, Span<byte> destination)
    {
        int filled = 0;
        while (filled < destination.Length)
        {
#if NET7_0_OR_GREATER
            int read = stream.Read(destination[filled..]);
#else
            byte[] chunk = new byte[destination.Length - filled];
            int read = stream.Read(chunk, 0, chunk.Length);
            chunk.AsSpan(0, read).CopyTo(destination[filled..]);
#endif
            if (read == 0)
            {
                break;
            }
            filled += read;
        }
        return filled;
    }
}
```

- [ ] **Step 4: Stage the stream read**

Replace `NpyFile.Read(Stream, ArtifactLoadOptions?)`'s body:

```csharp
    public static NpyBlock Read(Stream source, ArtifactLoadOptions? options = null)
    {
        Guard.NotNull(source);
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);

        // Twelve first, then the header it declares, then the payload straight into the
        // array the block keeps. No buffer holds the block on its way past (#466).
        Span<byte> prefix = stackalloc byte[MaxPrefix];
        int got = StreamFill.UpTo(source, prefix);
        if (got < MinPrefix)
        {
            return Read(prefix[..got], limits);
        }

        int total = HeaderTotal(prefix);
        byte[] head = new byte[total];
        prefix[..Math.Min(got, total)].CopyTo(head);
        if (total > got)
        {
            StreamFill.Exactly(source, head.AsSpan(got), Malformed("ends inside its header.").Message);
        }

        int dataStart = ReadHeader(head, out NpyHeader header);
        long elements = Elements(header, limits);
        float[] values = Buffers.AllocateUninitialized<float>((int)elements);
        StreamFill.Exactly(
            source,
            MemoryMarshal.AsBytes(values.AsSpan()),
            ShortPayload(elements * sizeof(float)));

        return new NpyBlock(values, header.Shape) { OwnedArray = values };
    }
```

with these members added to `NpyFile`:

```csharp
    /// <summary>Magic, version and the widest header length: the prefix one read always covers.</summary>
    private const int MaxPrefix = 12;

    /// <summary>Magic, version and the narrowest header length.</summary>
    private const int MinPrefix = 10;

    /// <summary>The element count a header declares, refused before anything is allocated.</summary>
    /// <remarks>
    /// The header is what announces the size on a staged read, so it is what has to be
    /// disbelieved. Divided rather than multiplied: two large dimensions overflow (#468).
    /// </remarks>
    private static long Elements(in NpyHeader header, in ArtifactLimits limits)
    {
        long elements = 1;
        foreach (int dimension in header.Shape)
        {
            elements *= dimension;
        }

        if (elements > limits.MaxTotalBytes / sizeof(float))
        {
            throw Malformed(
                $"declares {elements} elements, more than ArtifactLoadOptions.MaxTotalBytes "
                + $"({limits.MaxTotalBytes}) allows.");
        }
        return elements;
    }

    /// <summary>The message a payload shorter than its shape gets, on either read path.</summary>
    private static string ShortPayload(long expected) =>
        $"holds fewer than {expected} bytes of data where its shape needs {expected}.";
```

**`OwnedArray` does not exist yet** — Task 3 adds it. Until then, drop the `{ OwnedArray = values }` initializer from the return and put it back in Task 3; the two tests in this task pass without it.

- [ ] **Step 5: Run the tests**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~NpyFile"
```

Expected: PASS, **25 per assembly** — Task 1's 23 plus these two. Read whole result lines, not a count of `Passed!`.

- [ ] **Step 6: Confirm no fixture moved**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~Npy|FullyQualifiedName~EmbeddingIndex"
dotnet build Lodestar.slnx -c Release
```

Expected: PASS on both assemblies, 0 warnings. Every refusal fixture numpy wrote must still be refused with the same message; a changed message is a failure even when a test still passes, so read the diff of any message you touched.

- [ ] **Step 7: Commit**

```bash
git add src/Shared/Persistence/StreamFill.cs \
        src/Lodestar.Embeddings/Persistence/NpyFile.cs \
        tests/Lodestar.Embeddings.Tests/Persistence/NpyFileTests.cs
git commit -m "Read the payload into the array the block keeps

The header is read in two staged reads, then the block goes straight into
the float[] the caller receives. No intermediate buffer holds it, which is
the second of the three copies #466 counts against numpy's one.

netstandard2.0 has no Stream.Read(Span), so it stages through a chunk and
pays a second copy there -- the split VectorMath.Dot already makes.

Two cases the buffered read never had to handle: a forward-only stream,
and a truncation inside the payload rather than inside the header."
```

---

### Task 3: `OwnedArray`, so the block can be adopted

**Files:**

- Modify: `src/Lodestar.Embeddings/Persistence/NpyFile.cs`
- Test: `tests/Lodestar.Embeddings.Tests/Persistence/NpyFileTests.cs`

**Interfaces:**

- Consumes: `Read(Stream, ArtifactLoadOptions?)` from Task 2.
- Produces: `NpyBlock.OwnedArray` (`float[]?`, `init`-only).

- [ ] **Step 1: Write the failing tests**

```csharp
    [Fact]
    public void A_stream_read_block_owns_its_array()
    {
        NpyBlock block = NpyFile.Read(new MemoryStream(WrittenBlock([1f, 0f, 0f, 1f], 2, 2)));

        Assert.NotNull(block.OwnedArray);
        Assert.Same(block.OwnedArray, MemoryMarshal.TryGetArray<float>(block.Values, out var seg)
            ? seg.Array
            : null);
    }

    [Fact]
    public void A_hand_built_block_owns_nothing()
    {
        // The record's constructor is public, so a caller can build one around an array it
        // still holds. OwnedArray stays null there, which is what keeps ADR 0056's
        // invariant from being reached without the method that documents it.
        float[] mine = [1f, 2f];
        var block = new NpyBlock(mine, [2]);

        Assert.Null(block.OwnedArray);
    }

    [Fact]
    public void An_owned_block_is_adoptable_by_the_index()
    {
        NpyBlock block = NpyFile.Read(new MemoryStream(WrittenBlock([1f, 0f], 2)));

        EmbeddingIndex index = EmbeddingIndex.FromOwnedBlock(
            block.OwnedArray!, 2, BlockNormalization.AlreadyNormalized);

        Assert.Equal(1f, index.Search([1f, 0f], 1)[0].Score, 4);
    }
```

Add `using Lodestar.Embeddings.Search;` and `using System.Runtime.InteropServices;` to the test file if they are not there.

- [ ] **Step 2: Run to verify they fail**

```bash
dotnet test tests/Lodestar.Embeddings.Tests -c Release --filter "FullyQualifiedName~NpyFile"
```

Expected: the build fails — `NpyBlock` has no `OwnedArray`.

- [ ] **Step 3: Add the property**

In `NpyFile.cs`, replace the `NpyBlock` declaration:

```csharp
/// <summary>A float block read from a <c>.npy</c> file, with the shape it was stored under.</summary>
/// <param name="Values">The elements, in C order.</param>
/// <param name="Shape">The dimensions; one entry for a vector, two for a matrix.</param>
public readonly record struct NpyBlock(ReadOnlyMemory<float> Values, IReadOnlyList<int> Shape)
{
    /// <summary>The array this block owns, or <see langword="null"/> when it borrows.</summary>
    /// <remarks>
    /// Only <see cref="NpyFile.Read(Stream, ArtifactLoadOptions?)"/> fills it, because only
    /// that path allocates an array nobody else holds. A block over a caller's bytes leaves
    /// it null, and so does one built by hand — which is what stops decision 0056's
    /// ownership transfer being reached without the method that documents it.
    /// </remarks>
    public float[]? OwnedArray { get; init; }
}
```

and restore the initializer Task 2 deferred:

```csharp
        return new NpyBlock(values, header.Shape) { OwnedArray = values };
```

- [ ] **Step 4: Run the tests**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~NpyFile"
```

Expected: PASS, **28 per assembly**.

- [ ] **Step 5: Commit**

```bash
git add src/Lodestar.Embeddings/Persistence/NpyFile.cs \
        tests/Lodestar.Embeddings.Tests/Persistence/NpyFileTests.cs
git commit -m "Let a stream-read block surrender the array nobody else holds

FromOwnedBlock takes an array for the life of an index, and the only array
safe to give it is one the reader just allocated. OwnedArray is a property
the reader fills rather than an inference from MemoryMarshal.TryGetArray:
NpyBlock's constructor is public, so inference would report a caller's own
live array adoptable and reach ADR 0056's invariant through a side door."
```

---

### Task 4: The memory overload, which copies nothing

**Files:**

- Create: `src/Lodestar.Embeddings/Persistence/NpyPayloadManager.cs`
- Modify: `src/Lodestar.Embeddings/Persistence/NpyFile.cs`
- Test: `tests/Lodestar.Embeddings.Tests/Persistence/NpyFileTests.cs`

**Interfaces:**

- Consumes: `HeaderTotal`, `ReadHeader`, `Elements`, `ShortPayload`.
- Produces: `public static NpyBlock Read(ReadOnlyMemory<byte> npy, ArtifactLoadOptions? options = null)`.

- [ ] **Step 1: Write the failing tests**

```csharp
    [Fact]
    public void The_memory_overload_reads_the_same_block()
    {
        byte[] npy = WrittenBlock([1f, 2f, 3f, 4f], 2, 2);

        NpyBlock block = NpyFile.Read(npy.AsMemory());

        Assert.Equal([1f, 2f, 3f, 4f], block.Values.ToArray());
        Assert.Equal([2, 2], block.Shape);
    }

    [Fact]
    public void The_memory_overload_borrows_rather_than_copies()
    {
        byte[] npy = WrittenBlock([1f, 2f], 2);
        NpyBlock block = NpyFile.Read(npy.AsMemory());

        // The contract this asserts: Values aliases the caller's bytes, so changing them
        // changes what the block reports. Written down and raised by nothing, so tested.
        MemoryMarshal.AsBytes(new float[] { 9f }.AsSpan())
            .CopyTo(npy.AsSpan(npy.Length - (2 * sizeof(float))));

        Assert.Equal(9f, block.Values.Span[0]);
    }

    [Fact]
    public void A_borrowed_block_owns_nothing()
    {
        NpyBlock block = NpyFile.Read(WrittenBlock([1f, 2f], 2).AsMemory());

        Assert.Null(block.OwnedArray);
    }
```

- [ ] **Step 2: Run to verify they fail**

```bash
dotnet test tests/Lodestar.Embeddings.Tests -c Release --filter "FullyQualifiedName~NpyFile"
```

Expected: the build fails — no `Read(ReadOnlyMemory<byte>)` overload.

- [ ] **Step 3: Write the manager**

Create `src/Lodestar.Embeddings/Persistence/NpyPayloadManager.cs`:

```csharp
using System.Buffers;
using System.Runtime.InteropServices;

namespace Lodestar.Embeddings.Persistence;

/// <summary>Presents a byte payload's float block as a <see cref="Memory{T}"/> over the same bytes.</summary>
/// <remarks>
/// <see cref="MemoryMarshal.Cast{TFrom, TTo}(ReadOnlySpan{TFrom})"/> reinterprets a span and
/// there is no counterpart for <see cref="Memory{T}"/>, which is the whole reason this type
/// exists rather than a one-line cast. Nothing is copied and nothing is owned: the payload
/// belongs to the caller who passed it, for as long as the block is read.
/// </remarks>
internal sealed class NpyPayloadManager(ReadOnlyMemory<byte> payload) : MemoryManager<float>
{
    public override Span<float> GetSpan() =>
        MemoryMarshal.Cast<byte, float>(MemoryMarshal.AsMemory(payload).Span);

    /// <summary>Not supported: this manager borrows and has nothing to pin.</summary>
    public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();

    public override void Unpin() => throw new NotSupportedException();

    protected override void Dispose(bool disposing) { }
}
```

- [ ] **Step 4: Add the overload**

Add to `NpyFile`, directly below `Read(Stream, ArtifactLoadOptions?)`:

```csharp
    /// <summary>Reads a <c>.npy</c> from bytes already in memory, copying nothing.</summary>
    /// <remarks>
    /// For a caller holding the file already — a blob, a cache entry, an embedded resource.
    /// <b>The returned block aliases those bytes, so they must not change while it is read</b>,
    /// the contract <c>EmbeddingIndex.Load(ReadOnlyMemory)</c> states for the same reason.
    /// <see cref="NpyBlock.OwnedArray"/> is therefore null: a borrowed block has no array to
    /// hand over. Decision 0057 has the trade.
    /// </remarks>
    /// <param name="npy">The file's bytes, which outlive the block.</param>
    /// <param name="options">Bounds applied while reading, or <see langword="null"/> for the defaults.</param>
    /// <exception cref="InvalidDataException">As <see cref="Read(Stream, ArtifactLoadOptions?)"/>.</exception>
    public static NpyBlock Read(ReadOnlyMemory<byte> npy, ArtifactLoadOptions? options = null)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        int dataStart = ReadHeader(npy.Span, out NpyHeader header);
        long elements = Elements(header, limits);

        long expected = elements * sizeof(float);
        if (npy.Length - dataStart < expected)
        {
            throw Malformed(ShortPayload(expected));
        }

        var manager = new NpyPayloadManager(npy.Slice(dataStart, (int)expected));
        return new NpyBlock(manager.Memory, header.Shape);
    }
```

- [ ] **Step 5: Delete the private span overload**

`Read(ReadOnlySpan<byte>, in ArtifactLimits)` now has no caller except the short-prefix branch in Task 2's `Read(Stream)`. Replace that branch's call with `ReadHeader(prefix[..got], out _)`, which raises the same refusal, then delete the private overload. Build to confirm nothing else referenced it.

- [ ] **Step 6: Run the tests**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~NpyFile"
dotnet build Lodestar.slnx -c Release
```

Expected: PASS, **31 per assembly**; build 0 warnings.

- [ ] **Step 7: Commit**

```bash
git add src/Lodestar.Embeddings/Persistence/NpyPayloadManager.cs \
        src/Lodestar.Embeddings/Persistence/NpyFile.cs \
        tests/Lodestar.Embeddings.Tests/Persistence/NpyFileTests.cs
git commit -m "Serve a caller who already holds the bytes without copying them

MemoryMarshal.Cast reinterprets a span and has no Memory counterpart, so
the block is presented through a MemoryManager that borrows rather than
owns. The aliasing is the contract Load(ReadOnlyMemory) already states,
and it is asserted rather than only documented: the test mutates the
caller's bytes and watches the block report the new value."
```

---

### Task 5: The decision, the pages, and the gates

**Files:** `docs/decisions/0057-…md`, `docs/decisions/README.md`, `docs/reference/embeddings/persistence/npyfile-read.md`, `docs/reference/embeddings/persistence/npyblock.md`, `samples/Lodestar.Sample/Lot3Embeddings.cs`, `docs/equivalence.md`, `CHANGELOG.md`, `bench/README.md`, the spec.

- [ ] **Step 1: Write ADR 0057**

`docs/decisions/0057-the-npy-read-serves-a-stream-and-a-buffer-differently.md`, header in 0056's shape (`**Status:** accepted · **Date:** 2026-08-30`). It carries:

- **Context**: the measured row — 0.21–0.23× against numpy on the same bytes, three copies to its one — and that copy 2 alone moved it to 0.34–0.36×.
- **Decision**: two entry points, one contract each; `OwnedArray` filled only by the stream reader.
- **The loser, stated in full**, because it was this issue's own first proposal:

> **What was refused** is a view on every path — `NpyBlock.Values` aliasing the payload whether the bytes came from a stream or from the caller. It removes the byte-to-float copy without restructuring the read, which is why it looked cheapest. It caps the chain at two copies, because a view cannot be adopted and so forecloses removing the copy into the index, and it charges an aliasing contract to every caller including the one who only passed a `FileStream`. Reading into the array dominates it: one copy rather than two, and no contract at all on the path most callers take.

- **The reversal condition**: a caller found holding a `NpyBlock` past the lifetime of the bytes it borrowed, which would make the memory overload's contract the wrong default and argue for a copying one beside it.
- **The netstandard2.0 split**, named as a consequence rather than discovered: one copy on net10.0, two there, one API and one behaviour.

- [ ] **Step 2: Amend the spec**

The spec claims one copy without qualification. Add a dated amendment block at its end — **do not rewrite the body** — saying that `Stream.ReadExactly` and `Stream.Read(Span<byte>)` are .NET 7 and later, that `netstandard2.0` therefore stages through a chunk and pays two copies, and that this is the split `VectorMath.Dot` already makes.

- [ ] **Step 3: Update the two reference pages**

`npyfile-read.md` gains the memory overload in its `<!-- docs-declaration -->` fence and a **Remarks** paragraph on the aliasing contract; `npyblock.md` gains `OwnedArray`. Follow `embeddingindex-add.md`'s shape. **Every public member named in prose must be a link** — `EmbeddingIndex.FromOwnedBlock` and `EmbeddingIndex.Load` both have pages, so link them.

The example fences are compiled **and executed**, and a trailing `// =>` asserts the value. Produce every value by running it:

```csharp
using Lodestar.Embeddings.Persistence;

byte[] npy = File.ReadAllBytes(path);
NpyBlock block = NpyFile.Read(npy.AsMemory());

int rank = block.Shape.Count;  // => 2
bool borrowed = block.OwnedArray is null;  // => True
```

- [ ] **Step 4: Reference the overload from the sample**

In `samples/Lodestar.Sample/Lot3Embeddings.cs`, beside the existing `.npy` usage, add a call to `NpyFile.Read(ReadOnlyMemory<byte>)` and print the shape. `samples/` is exempt from the `console-print:` marker; confirm by running the guard, not by trusting this line.

- [ ] **Step 5: Add the `docs/equivalence.md` row**

`numpy.load` → `NpyFile.Read`, which #450 shipped without and which this lot owes because it changes the reader's public surface.

- [ ] **Step 6: CHANGELOG and bench/README**

One `### Lodestar.Embeddings` / `#### Changed` sentence in the established shape with the issue and the commit. In `bench/README.md` §7, the paragraph on `embedding_index_ingest_npy` says the copy count changed; leave every published figure to Task 6.

- [ ] **Step 7: Run every gate**

```bash
python3 tools/check_comment_length.py
python3 tools/check_no_console_writeline.py
python3 tools/check_machine_paths.py --no-environment
python3 tools/check_sample_culture.py
python3 tools/check_sample_coverage.py
python3 tools/check_bench_map.py
python3 tools/check_version_floor.py
git fetch origin main && python3 tools/check_adr_immutable.py --base origin/main
dotnet format Lodestar.slnx --verify-no-changes
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 tools/extract_doc_snippets.py && dotnet build samples/Lodestar.DocSnippets -c Release
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~ReferenceDocumentation"
```

Expected: all clean; the reference filter reports **8 assemblies, 0 failed**. Read whole result lines — grepping for `Passed!` hides an assembly that printed `Failed!`, which cost a cycle on #474.

- [ ] **Step 8: Pack and run the sample**

```bash
for p in src/Lodestar.Text src/Lodestar.Embeddings src/Lodestar.Fuzzy src/Lodestar.Metrics; do
  dotnet pack "$p" -c Release -o ./artifacts
done
NUGET_PACKAGES=$(mktemp -d) dotnet run --project samples/Lodestar.Sample -c Release
```

- [ ] **Step 9: Commit**

```bash
git add docs/ samples/ CHANGELOG.md bench/README.md
git commit -m "Record the two contracts, and give the new surface its pages

0057 carries the loser this issue proposed first: a view on every path,
which caps the chain at two copies by foreclosing adoption and charges an
aliasing contract to a caller who only passed a FileStream."
```

---

### Task 6: The measurement

**This task stops and asks.** `Benchmark (on demand)` is dispatched by hand; the GitHub App is refused with a 403.

- [ ] **Step 1: Ask the maintainer to dispatch `compare-persistence` on this branch**

- [ ] **Step 2: Read `embedding_index_ingest_npy` from all three rounds**

Record wall, cpu and both ratios per round, and the runner's load average at each round's start.

- [ ] **Step 3: Judge it against the spec's gate**

| | wall | cpu |
| --- | ---: | ---: |
| `main`, three copies | 0.21–0.23× | 0.19× |
| copy 2 removed (`f9bfef7`) | 0.34–0.36× | 0.29–0.30× |
| this lot | **expect ≈0.6× wall** | |

One `memcpy` of this block reads as about 1.8 ms on that runner, so removing copy 3 should put the row near 2.4 ms against numpy's ~1.47.

**A result far from that is the finding, not a reason to re-run.** If removing a whole copy of 15.36 MB does not move the row by roughly a memcpy, the cost is somewhere this analysis did not look, and the pull request says so rather than shipping on the arithmetic.

- [ ] **Step 4: Publish**

Every figure with its machine and window in `docs/guides/performance.md`; ratios and meaning only in `bench/README.md` §7.

- [ ] **Step 5: Open the pull request**

Body carries the before/after table, names the machine, and says `Closes #466`. A `perf/`-shaped lot without both is incomplete.

---

## Self-Review

**Spec coverage.** Both entry points → Tasks 2 and 4. `OwnedArray` and its anti-inference reasoning → Task 3. The staged read's limit ordering and truncation message → Task 2 Step 4. The view refused as loser → Task 5 Step 1. The three new tests the spec names — non-seekable, payload truncation, aliasing observed — → Tasks 2 and 4. The gate → Task 6. The four-route copy table → ADR 0057.

**One spec gap, found while planning and carried rather than hidden.** The spec claims one copy without qualification; `netstandard2.0` has neither `Stream.ReadExactly` nor `Stream.Read(Span<byte>)` and pays two. Task 5 Step 2 amends the spec rather than rewriting it.

**Type consistency.** `OwnedArray` is `float[]?` everywhere. `StreamFill.Exactly(Stream, Span<byte>, string)` and `StreamFill.UpTo(Stream, Span<byte>)` keep their signatures across Tasks 2 and 4. `Elements(in NpyHeader, in ArtifactLimits)` and `ShortPayload(long)` are defined in Task 2 and consumed in Task 4.

**What this plan cannot verify.** Every `dotnet` command was written from the sources it extends, not run against this tree — the expected test counts are counted from the plan's own fences and want checking by the first executor. A count that disagrees is the plan's error, not the suite's.
