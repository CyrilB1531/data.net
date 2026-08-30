# 0466 — The .npy read copies the block three times, where numpy copies it once

**Issue:** [#466](https://github.com/CyrilB1531/lodestar/issues/466) ·
**Status:** proposed, amended in part — see the amendment at the end · **Date:** 2026-08-30

## Problem

[#474](https://github.com/CyrilB1531/lodestar/issues/474) built the cross-language row this
repository had never had: `embedding_index_ingest_npy`, where **both sides read the same `.npy`
and return something searchable** — `np.load` against `NpyFile.Read` plus
`EmbeddingIndex.FromBlock`. Every other index row compares our JSON artifact against numpy's raw
block, which prices a format and reports it as a speed.

On a hosted runner, three rounds, both sides on 15 360 128 bytes:

| | Lodestar wall | numpy wall | wall | **cpu** |
| --- | ---: | ---: | ---: | ---: |
| before this lot | 6.106 / 5.645 / 6.035 ms | 1.410 / 1.233 / 1.280 ms | 0.21–0.23× | **0.19×** |

**numpy is four to five times faster on the same bytes.** Taking the format advantage away made
the gap *wider* than `embedding_index_load`'s 0.24–0.27×, because that row let numpy be compared
against five megabytes less work.

The reason is not the language. **We copy the 15.36 MB block three times before an index holds
it; numpy copies it once.**

| # | copy | where |
| --- | --- | --- |
| 1 | stream → exact `byte[]` | `JsonArtifact.ReadAllBytes` |
| 2 | `byte[]` → `byte[]` | `.ToArray()` in `NpyFile.Read(Stream)` |
| 3 | `byte[]` → `float[]` | `payload.Slice(…).CopyTo(MemoryMarshal.AsBytes(values))` |
| 4 | `float[]` → the index's store | `EmbeddingIndex.FromBlock` |

Copy 2 is already gone, in this branch's first commit: `ReadAllBytes` returns a `ReadOnlyMemory`
bounded to the bytes actually read, and its own documentation says that reaching for `.ToArray()`
reintroduces the copy the return type exists to remove. Measured on the same runner, that alone
moved the row to **4.039 / 4.317 ms, 0.34–0.36× wall and 0.29–0.30× cpu** — about 1.8 ms, the
shape of one `memcpy` of this block.

This lot takes copies 3 and 4.

## What is settled and out of scope

- **The artifact format.** Nothing here reads or writes a Lodestar artifact; `EmbeddingIndex.Save`
  and the JSON load path are untouched.
- **The refusals `.npy` already makes.** Big-endian, `float64`, `fortran_order`, a scalar shape,
  past two dimensions, and `'|O'` — numpy's pickle-backed dtype, refused by name on the header
  before the payload is touched — all keep their behaviour and their messages exactly.
- **Non-finite values.** A `.npy` is somebody else's data; `NpyFile` carries a `NaN` through and
  `EmbeddingIndex.Save` is what refuses to persist one. Unchanged.
- **`EmbeddingIndex`'s storage.** The index keeps a `float[]`. Making it hold something else is
  the only route below one copy and is not proposed.

## The shape

Two entry points, because a caller holding a stream and a caller holding bytes can be served
differently — the pattern `EmbeddingIndex.Load(Stream)` and `Load(ReadOnlyMemory<byte>)` already
set here.

```csharp
// One copy: the stream is read straight into the array the block will own.
public static NpyBlock Read(Stream source, ArtifactLoadOptions? options = null);

// Zero copies: Values aliases the caller's bytes, which must not change while the block lives.
public static NpyBlock Read(ReadOnlyMemory<byte> npy, ArtifactLoadOptions? options = null);

public readonly record struct NpyBlock(ReadOnlyMemory<float> Values, IReadOnlyList<int> Shape)
{
    /// <summary>The array this block owns, or null when it borrows the caller's bytes.</summary>
    public float[]? OwnedArray { get; init; }
}
```

### Why `OwnedArray` is a property the reader fills and not an inference

`MemoryMarshal.TryGetArray` answers true for a stream-read block's array and false for a
memory-manager view, so the distinction appears to fall out for free. **It does not, and taking
it would open a hole in [ADR 0056](../../decisions/0056-a-block-may-be-adopted-and-the-invariant-is-the-callers-to-keep.md).**

`NpyBlock` is a `readonly record struct` with a public positional constructor. Anyone can build
one around a `float[]` they still hold; inference would report it adoptable, and
`EmbeddingIndex.FromOwnedBlock` would take an array its caller is still writing to. That is 0056's
invariant reached through a side door rather than through the method that documents it.

A property only `NpyFile.Read(Stream)` sets cannot be reached that way: a hand-built block leaves
it null, and a view leaves it null because there is no array to give.

### What the two overloads cost the chain

| route | copies |
| --- | ---: |
| `Read(Stream)` → `FromBlock` | 2 |
| `Read(Stream)` → `FromOwnedBlock(block.OwnedArray)` | **1** |
| `Read(ReadOnlyMemory<byte>)` → `FromBlock` | **1** |
| `Read(ReadOnlyMemory<byte>)` → `FromOwnedBlock` | not available, and correctly so |

numpy's `np.load` is one. **Two of the four routes reach parity, and the fourth is refused rather
than made to look available**: a view has no array to surrender, so `OwnedArray` is null and the
adopting factory cannot be called with it.

### The alternative that was refused

**A view alone**, `NpyBlock.Values` aliasing the payload on every path including the stream one,
was the first proposal — mine, in [this issue's own analysis](https://github.com/CyrilB1531/lodestar/issues/466#issuecomment-5467051624).
It removes copy 3 without touching the stream read, which is why it looked cheapest.

It caps the chain at two copies. A view cannot be adopted, so it **forecloses** removing copy 4,
and buys that with an aliasing contract on every read rather than on the one overload whose
caller asked for it. Reading the stream into the array directly dominates it on both axes: one
copy instead of two, and no contract at all on the path most callers take.

The view survives, scoped to the overload where the caller already holds the bytes and has
therefore already accepted their lifetime.

## The read path

`Read(Stream)` becomes three steps where it was one:

1. read the 12-byte prefix — magic, version, header length — and refuse an unreadable one;
2. read `headerLength` bytes, parse the shape, and **check the limits before allocating anything**;
3. allocate `float[elements]` uninitialized and `ReadExactly` the payload into
   `MemoryMarshal.AsBytes(values)`.

**Step 2's order is the part that has to be right.** Today `JsonArtifact.ReadAllBytes` bounds the
whole file before anything is allocated. Reading in stages, it is the *header* that announces the
size, so the header is what must be disbelieved: `elements > MaxTotalBytes / sizeof(float)` is
checked on the declared shape, before the allocation, which is where the current code's check
moves to rather than a check it loses.

**Truncation changes how it is detected**, and must not change what it says. There is no complete
buffer to compare against any more, so a short payload surfaces as `ReadExactly` throwing at end
of stream instead of `available < expected`. The refusal message stays equivalent, or #450's
truncation fixtures start meaning something else.

`Read(ReadOnlyMemory<byte>)` parses the same header, then builds `Values` on an internal
`MemoryManager<float>` aliasing the payload slice — `MemoryMarshal.Cast` applies to spans, not to
`Memory`, which is what makes the manager necessary rather than decorative. It carries the
alignment note: numpy pads the header so the payload starts on a 64-byte boundary, and
`NpyFile.Write` reproduces that padding.

The private `Read(ReadOnlySpan<byte>)` overload goes: its one caller becomes the memory overload.

## What proves it

**The fixtures numpy wrote are the witness**, and none of them may move. #450 committed files
numpy itself produced, including every one that must be refused, precisely so that a
hand-approximated big-endian or pickled file could not prove anything. Byte-for-byte output and
every refusal message are the contract.

Three tests the current suite does not have:

- a **non-seekable** stream, which the staged read must handle and the buffered one never had to;
- a stream truncated **inside the payload** rather than inside the header, which is the case the
  detection moves;
- **aliasing observed** on the memory overload: mutating the caller's bytes after `Read` changes
  what `Values` reports. Asserted rather than documented, the way `FromOwnedBlock`'s adoption is —
  an invariant that raises nothing when broken needs a test that does.

## The gate

`compare-persistence` on a hosted runner, `embedding_index_ingest_npy`, against this branch's own
first commit rather than against `main`:

| | wall | cpu |
| --- | ---: | ---: |
| `main`, three copies | 0.21–0.23× | 0.19× |
| copy 2 removed (`f9bfef7`) | 0.34–0.36× | 0.29–0.30× |
| **this lot** | **to measure** | **to measure** |

The arithmetic that motivates it, and which the run is there to confirm or refuse: one `memcpy` of
this block reads as about 1.8 ms on that runner, so removing copy 3 should put the row near
2.4 ms against numpy's 1.47 — roughly **0.6×**. Reaching parity needs copy 4 as well, which is
what `OwnedArray` makes possible and what the sidecar route will take.

**A result far from that is the finding, not a reason to re-run.** If removing a whole copy of
15.36 MB does not move the row by something near a memcpy, then the cost is somewhere this
analysis has not looked, and the lot should say so rather than ship on the arithmetic.

## What this does not claim

- **Not that the project is fast at this.** Even at parity on copies, `np.load` is a thin wrapper
  over a read into an array, and matching it is the ceiling here rather than a win.
- **Not that the other rows change.** `embedding_index_load` reads the JSON artifact and is
  untouched; its 0.24–0.27× is a statement about the format, as `bench/README.md` §7 now says.
- **Not that `NpyBlock` was wrong as shipped.** #450 built it for a reader that copied; the member
  this lot adds is what a reader that does not copy needs, and the change is additive.

## Amendment — 2026-08-30: the stream read copies once on `net10.0` and twice on `netstandard2.0`

*The body above is left as written.* Its shape says "one copy: the stream is read straight into
the array the block will own", with no target framework beside it. That is true on `net10.0` and
not on `netstandard2.0`, and the difference is a framework's API surface rather than a choice made
here.

Reading a payload into an array the caller already holds needs `Stream.Read(Span<byte>)`, and
`Stream.ReadExactly` with it; both are .NET 7 and later. The shared `StreamFill.Exactly` therefore
reads into the destination directly under `#if NET7_0_OR_GREATER` and stages through an 80 KB
chunk otherwise, so `Read(Stream)` costs **one copy on `net10.0` and two on `netstandard2.0`** —
one public API and one behaviour at two speeds, the split `VectorMath.Dot` already makes and which
`StreamFill`'s own remarks name as its precedent.

Two consequences for what the body claims. *What the two overloads cost the chain* counts
`net10.0`'s copies: on `netstandard2.0` every route through `Read(Stream)` carries one more, and
the two routes that reach numpy's single copy do so on `net10.0` alone. *The gate* is measured on
`net10.0`, as every row in `bench/README.md` is, so the figure it produces says nothing about the
other target.

`Read(ReadOnlyMemory<byte>)` is unaffected and copies nothing on either target: nothing on that
path depends on a `Stream` API.
[Decision 0057](../../decisions/0057-the-npy-read-serves-a-stream-and-a-buffer-differently.md)
records the split as a consequence of the two-entry-point shape rather than as a discovery under
it.

## Amendment — 2026-08-30: the gate passed, and not for the reason this spec argued

*The body above is left as written, including the arithmetic this measurement refutes.*

**The gate.** *The gate* expected about **0.6×** of numpy's wall, from removing copy 3 alone. The
row measured **1.21–1.25× wall and 1.00–1.13× cpu** — ahead rather than behind. The figures, the
runner and the anchors are in
[the performance guide](../../guides/performance.md#the-same-row-once-the-block-is-adopted-issue-466).

**The reasoning was wrong in both directions, which is why this is recorded rather than
celebrated.** Two dispatches separated the causes:

- **Copy 3, which this spec is named after, was worth nothing.** Against the untouched
  `embedding_index_load_memory`, the row read 0.88 / 0.94 / 0.96 before and 0.90 / 0.97 / 0.99
  after. *The gate* said in advance that such a result is the finding rather than a reason to
  re-run, and it is taken that way: [#480](https://github.com/CyrilB1531/lodestar/issues/480)
  carries it.
- **Copy 4, which this spec put out of scope**, was worth all of it — the same ratio moved to
  0.31 / 0.28 / 0.28. *What the two overloads cost the chain* counted it as one copy among four.
  It is not one copy among four: `FromBlock` allocates a second 15.36 MB store on the large object
  heap to copy into, and adopting removes the allocation with the copy. The row fell by about
  3.1 ms where one `memcpy` of this block costs about 1.2.

**And the benchmark was measuring the wrong chain.** The row called `FromBlock`, so it charged
this side a copy `np.load` never pays — numpy returns the array it has just filled. That was found
while judging the gate rather than while looking for a better number, and it is a defect in the row
independent of what the result had been. Fixed in `ac6881f`.

**What the body still gets right.** Two entry points, one contract each; `OwnedArray` filled only
by the stream reader, and the anti-inference argument for it. Without `OwnedArray` there is no
adopting route to measure, so the member this spec argued for is what the result rests on — by a
mechanism it did not name.
