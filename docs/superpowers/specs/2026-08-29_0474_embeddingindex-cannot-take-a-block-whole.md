# 0474 — EmbeddingIndex cannot take a block whole

**Issue:** [#474](https://github.com/CyrilB1531/lodestar/issues/474) ·
**Status:** proposed · **Date:** 2026-08-29

## Problem

[ADR 0055](../../decisions/0055-the-artifact-gets-a-binary-sidecar-once-a-block-can-be-ingested-whole.md)
found the time argument for a binary sidecar that
[0011](../../decisions/0011-persistence-format.md) had looked for in the wrong place: reading a
`.npy` block is **2.02× faster** than loading the JSON artifact. Then it found the reason it cannot
be taken. `sidecar` on a hosted runner, 10 000 × 384, medians of nine:

| | median |
| --- | ---: |
| `EmbeddingIndex.Load`, payload pooled | 11.834 ms |
| `NpyFile.Read` | 5.236 ms |
| floor — the read plus one copy into a backing store | 5.847 ms |
| rebuild through `Add`, per vector | 17.973 ms |

**`load / rebuild` = 0.66×.** `Add` copies one vector, normalizes it, and grows the store when it
is full, ten thousand times over — none of it wrong for the API it is, all of it waste for a caller
already holding the whole block contiguous in the right layout. The sidecar route that exists today
is slower than the artifact it would replace, so the format is not the bottleneck and a sidecar
shipped against this surface would be a format change that made loading slower.

The ingest is the bottleneck. This lot removes it.

## What is settled and out of scope

- **The sidecar format itself.** 0055 decided it and named this its precondition. Nothing here
  writes or reads a sidecar; `EmbeddingIndex.Save` is untouched.
- **Memory-mapping.** [#436](https://github.com/CyrilB1531/lodestar/issues/436)'s original ask, now
  two lots downstream. It stays worth having because it removes the copy the floor still pays, and
  this lot must not pre-empt it.
- **`Add`.** It is the right API for appending one embedding and keeps its behaviour exactly. This
  lot adds a path beside it, not in front of it.
- **Non-finite components.** `Add` accepts them and only `Save` refuses them. A bulk ingest accepts
  them for the same reason, so the two paths cannot disagree about what an index may hold.

## The shape

Two public factories on `EmbeddingIndex`, and one public enum:

```csharp
public enum BlockNormalization
{
    Normalize,          // the index normalizes; the block is normalized in place once taken
    AlreadyNormalized,  // the index normalizes; the block already is, and is stored bit for bit
    Off,                // the index does not normalize, on insertion or on query
}

public static EmbeddingIndex FromBlock(
    ReadOnlySpan<float> block, int dimension,
    BlockNormalization normalization, IReadOnlyList<string?>? ids = null);

public static EmbeddingIndex FromOwnedBlock(
    float[] block, int dimension,
    BlockNormalization normalization, IReadOnlyList<string?>? ids = null);
```

`count` is derived as `block.Length / dimension` and the division must be exact. The issue asked for
"a dimension and a count"; a count passed alongside a length is one more inconsistency to refuse and
buys nothing until a caller holds a block longer than its contents.

### Why normalization is one enum and not two booleans

The index's `normalize` flag governs the **query** as well as the store — `Search` normalizes the
query when it is on. So the three reachable configurations are not two independent choices:

| the block | `_normalize` | what happens to the block |
| --- | --- | --- |
| raw | `true` | normalized in place after it is taken |
| already normalized | `true` | stored bit for bit |
| raw | `false` | stored bit for bit, queries unnormalized |

A `bool normalize` beside a `bool alreadyNormalized` makes a fourth combination representable —
`normalize: false, alreadyNormalized: true` — which means nothing and would have to be refused at
run time. One enum deletes it at compile time. `Normalize` is member 0, so an accidental
`default(BlockNormalization)` yields the correct-but-slower behaviour and never a silently wrong
score.

`AlreadyNormalized` is a promise the caller can break. It is a required argument rather than an
optional parameter precisely so it is written at the call site, where a review can see it.

### Why both factories are public, and what that costs

`FromBlock` copies: one copy, which is the floor exactly. `FromOwnedBlock` adopts, which beats it.

Adoption is not new to this type — `Restore` already sets `_data` to the array the JSON parse
produced, and `Load(ReadOnlyMemory<byte>)` already documents that the bytes must not change while
it runs. What is new is the **duration**: `Load`'s invariant lasts the call, and
`FromOwnedBlock`'s lasts the life of the index. A caller who keeps the reference and writes to it
changes the index's vectors, and every subsequent score, with nothing raised. A caller who hands
over an array rented from `ArrayPool<float>.Shared` and later returns it serves the next renter's
bytes as embeddings. That second one is exactly the trap
[0053](../../decisions/0053-the-payload-buffer-is-not-pooled-because-residency-outlives-the-load.md)
named, from the other side of the boundary.

The decision is that the invariant is the caller's to keep, stated in the method's name, its
documentation, and a test that asserts adoption is observable. It gets its own ADR, because it is a
decision with a loser: the alternative — the copying factory public and the adopting one an
internal seam — keeps the invariant out of every caller's hands and was refused for the reach it
costs.

The index exposes no vector accessor, so the exposure is one-directional: a caller can reach into
the index's store only through an array it handed over itself.

### The finding: the sidecar route cannot reach `FromOwnedBlock` yet

`NpyFile.Read` allocates a fresh, exactly-sized `float[]` that nobody else holds — the ideal
candidate for adoption. It returns it as `NpyBlock.Values`, a `ReadOnlyMemory<float>`, and there is
no supported way back to the array.

So the sidecar route reaches `FromBlock`, pays the copy, and lands **at** the floor rather than
below it. This is deliberate and it is 0055's work to change, by giving the `.npy` read a path that
yields an owned array. The two alternatives were weighed and refused here:

- `FromOwnedBlock` taking a `ReadOnlyMemory<float>` and adopting through
  `MemoryMarshal.TryGetArray`, throwing when the memory is not a whole array — a public contract
  that depends on the provenance of its argument.
- `NpyBlock` gaining a way to surrender its array — a change to the type
  [#450](https://github.com/CyrilB1531/lodestar/issues/450) shipped, and a second issue.

Neither can be taken inside an issue that must close exactly one.

## The gate

Two rows added to `SidecarBench`, interleaved with the four it already has, on the same corpus:

- **`ingest copy`** — `NpyFile.Read` then `FromBlock`. The sidecar route as it will exist, and the
  row the gate is read off.
- **`ingest only`** — `FromBlock` on a block already in hand. It separates the ingest's own cost
  from the read's, so a later regression can be attributed to one or the other.

**There is no `ingest owned` row, and its absence is the finding.** `FromOwnedBlock` assigns four
fields; with `AlreadyNormalized` it is constant time whatever the block's size, so a row for it
would publish noise. The ceiling adoption reaches is the `read npy block` row that already exists —
5.236 ms, the read and nothing else. `bench/README.md` says that in a sentence instead.

**`ingest copy` must approach 5.847 ms and not 17.973**, and `load / ingest` must clear 1.0 while
aiming at the 2.02× that `load / floor` showed. A bulk ingest landing near the rebuild has not
earned the sidecar its precondition, and 0055 should not be built on it.

Measured on a hosted runner through `Benchmark (on demand)`, interleaved, medians and range
published with the machine named. **Not on the session container**, which put `load / floor` at
0.73× — the opposite conclusion — with that row spread over 12–43 ms against the runner's 4.0–8.5.
It is the second time in this roadmap the container inverted a result;
[#433](https://github.com/CyrilB1531/lodestar/issues/433) was the first.

## What this does not claim

- **Not that the sidecar is now worth shipping.** This lot clears the precondition and measures it.
  0055 decides on the numbers this produces, and may still decide against.
- **Not that adoption is free.** It moves a cost from the library to the caller's discipline. The
  ADR takes that as a price rather than discovering it is absent.
- **Not that `ingest owned` is a route anything can take today.** It is a ceiling, published as one.

## What ships

Implementation, two factories and the enum; `Restore` rewired onto the same private seed so the
JSON load path and the block path cannot drift. Tests: bit-for-bit equality with an index built by
`Add`; adoption observed through the caller's array; the `Off` path; a block whose length is not a
multiple of the dimension; ids of the wrong length. Both target frameworks pick them up through the
linked `*.NetStandard.Tests` sources.

Documentation: reference pages for the three new members and `embeddingindex.md` updated, for the
reference gate; two calls in `samples/Lodestar.Sample/Lot3Embeddings.cs` referencing all three
members between them, so the packaged API is exercised from outside the assembly; the
`bench/README.md` row descriptions; the ADR; and the `CHANGELOG.md` entry item 7 of the definition
of done requires.

No `*Sample.cs` file is due. `check_sample_coverage.py` enforces one sample file per public class
only for the packages in its `CONVERTED` list, and `Lodestar.Embeddings` is still in `WAITING`
([#280](https://github.com/CyrilB1531/lodestar/issues/280) closes when that half empties). ADR 0041's
first rule gives an enum no file of its own in any case: it is demonstrated through the class whose
parameter it is.
