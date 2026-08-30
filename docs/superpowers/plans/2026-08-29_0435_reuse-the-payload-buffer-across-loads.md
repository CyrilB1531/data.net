# Reuse the payload buffer across loads — implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** decide, on measurement, whether the transient artifact payload is rented from `ArrayPool<byte>.Shared` rather than allocated — trading 20.5 MB allocated per load against 33.5 MB held by the pool for the life of the process.

**Architecture:** the payload only. `JsonArtifact.ReadAllBytes` gains a form whose buffer has an owner, so the call site that consumes it also returns it; the vector block is untouched, because it becomes the index's backing store and outlives the call. An exact-length slice is not an implementation detail here but the thing being implemented: a rented buffer's tail may hold the previous index.

**Tech Stack:** C# on `net10.0` and `netstandard2.0`, `System.Buffers` (already referenced on the older target), xunit.

**Spec:** [`../specs/2026-08-29_0435_reuse-the-payload-buffer-across-loads.md`](../specs/2026-08-29_0435_reuse-the-payload-buffer-across-loads.md)

**Issue:** [#435](https://github.com/CyrilB1531/lodestar/issues/435), part of [#429](https://github.com/CyrilB1531/lodestar/issues/429) · **Branch:** `perf/435-reuse-the-payload-buffer`

**Blocked on:** [#433](https://github.com/CyrilB1531/lodestar/issues/433). Its lot measures the warm-heap subsidy directly and that number is the ceiling on what this can return. **Do not start Task 4 before it lands.**

## Global Constraints

- English everywhere; no `feat:`/`fix:` prefix on a commit subject; closing keywords in the pull-request body only.
- Warnings are errors on **both** target frameworks, SonarAnalyzer in the build.
- Comment budgets: two lines inline, eight of prose in XML documentation. No `long-comment:` marker on this branch.
- `netstandard2.0` reaches equivalent behaviour through conditional compilation, **never a reduced API**. The `*.NetStandard.Tests` projects link the same sources, so a new test file is picked up by both automatically.
- The exposure invariant in Task 2 is not a review preference. **Treat a failure of it as a security defect**: the value reaches the caller.
- Interleave the before/after; publish medians and spread; name the machine and record `uptime`.
- `git add -N` a new file before running the guards. Never `git checkout --` a file `git status` shows as `A` — the intent-to-add blob is empty and the checkout truncates it.
- Run every lint-job guard on this branch.

---

### Task 1: prove the payload escapes nowhere

**Files:**

- Modify: none. This task reads and writes a finding.
- Test: none yet.

**Interfaces:**

- Consumes: `JsonArtifact.ReadAllBytes(Stream, in ArtifactLimits)` → `ReadOnlyMemory<byte>`, and every caller of it.
- Produces: a yes/no that Task 2 depends on entirely.

- [ ] **Step 1: Enumerate the callers**

```bash
grep -rn "ReadAllBytes\|ReadAllBytesAsync" src/ --include=*.cs
```

- [ ] **Step 2: Read each consumer for retention**

For each, answer in one line whether anything it returns holds a slice of the input. The expected answers, to be confirmed rather than assumed:

- ids arrive from `Utf8JsonReader` as new `string` instances — no slice;
- floats are decoded by `Base64Numbers.ReadSingles` into their own array — no slice.

- [ ] **Step 3: Think hardest about the memory overload**

`EmbeddingIndex.Load(ReadOnlyMemory<byte>)` parses **the caller's** memory in place — its own documentation says *"the bytes must not change while it runs"*. It must keep doing exactly that, and **must never return a caller's buffer to a pool**. Confirm the two paths do not share a helper that would pool on both.

- [ ] **Step 4: If any consumer retains a slice, stop**

Write up what retains what and end the lot here. Pooling that path would hand one caller another's bytes, and the lot is then a different lot.

- [ ] **Step 5: Commit the finding**

```bash
git commit --allow-empty -m "Record that the payload escapes nowhere, which is what makes it poolable"
```

---

### Task 2: the ownership shape, and the invariant that has teeth

**Files:**

- Modify: `src/Shared/Persistence/Buffers.cs` — the rent/return owner
- Modify: `src/Shared/Persistence/JsonArtifact.cs:92` — `ReadAllBytes` and its async twin
- Modify: `src/Lodestar.Embeddings/Search/EmbeddingIndex.Persistence.cs:130` — return in a `finally`
- Test: `tests/Lodestar.Embeddings.Tests/Persistence/PooledPayloadTests.cs`

**Interfaces:**

- Consumes: Task 1's finding.
- Produces: `Buffers.RentedPayload`, a `readonly struct` with `ReadOnlyMemory<byte> Memory { get; }` and `void Dispose()`; `JsonArtifact.ReadAllBytesPooled(Stream, in ArtifactLimits)` → `Buffers.RentedPayload`.

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void A_second_load_observes_nothing_of_the_first()
{
    // B is deliberately the shorter artifact: that is the case where a slice taken from
    // the buffer rather than from the byte count exposes A's tail.
    EmbeddingIndex a = Build(count: 400, dimension: 16, seed: 1);
    EmbeddingIndex b = Build(count: 40, dimension: 16, seed: 2);

    using var first = new MemoryStream();
    a.Save(first);
    first.Position = 0;
    EmbeddingIndex loadedA = EmbeddingIndex.Load(first);

    using var second = new MemoryStream();
    b.Save(second);
    second.Position = 0;
    EmbeddingIndex loadedB = EmbeddingIndex.Load(second);

    Assert.Equal(b.Count, loadedB.Count);
    for (int i = 0; i < loadedB.Count; i++)
    {
        Assert.Equal(b.GetId(i), loadedB.GetId(i));
    }

    // The tail A left behind would show up as B's search answering with A's neighbours.
    Assert.Equal(b.Search(Query(16), k: 3), loadedB.Search(Query(16), k: 3));
    GC.KeepAlive(loadedA);
}
```

- [ ] **Step 2: Run it and watch it pass — then break it deliberately**

```bash
dotnet test tests/Lodestar.Embeddings.Tests -c Release --filter "FullyQualifiedName~PooledPayload"
```

It passes today, because nothing is pooled yet. **That is not evidence.** After Step 3 introduces the pool, change the slice to hand out the whole rented `buffer` instead of `buffer.AsMemory(0, filled)` and confirm this test **fails**. A test that has never failed proves nothing. Restore the correct slice.

- [ ] **Step 3: The owner, in `Buffers.cs`**

```csharp
/// <summary>A rented payload buffer and the exact bytes that were read into it.</summary>
/// <remarks>
/// The rent and the return live together so no call site can hold one without the other, the
/// way <c>ArtifactIo.SaveWithBlock</c> owns the writer sequence (ADR 0051). <see cref="Memory"/>
/// is sliced to what was read: a rented array is at least as long as asked and its tail may
/// hold the previous artifact.
/// </remarks>
internal readonly struct RentedPayload : IDisposable
{
    private readonly byte[] _buffer;

    public RentedPayload(byte[] buffer, int filled)
    {
        _buffer = buffer;
        Memory = new ReadOnlyMemory<byte>(buffer, 0, filled);
    }

    public ReadOnlyMemory<byte> Memory { get; }

    public void Dispose() => ArrayPool<byte>.Shared.Return(_buffer);
}
```

- [ ] **Step 4: Use it from the index's stream load**

```csharp
using Buffers.RentedPayload payload = JsonArtifact.ReadAllBytesPooled(source, limits);
return FromPayload(payload.Memory, limits);
```

- [ ] **Step 5: Run the full suite on both frameworks**

```bash
dotnet test Lodestar.slnx -c Release
```

Expected: every project passes, `Lodestar.Embeddings.NetStandard.Tests` included — read the counts.

- [ ] **Step 6: Commit**

```bash
git add src/ tests/
git commit -m "Rent the payload, and slice it to what was actually read"
```

---

### Task 3: the residency cost, stated before the timing

**Files:**

- Modify: none. This task produces numbers.

**Interfaces:**

- Consumes: Task 2 shipped.
- Produces: the two columns Task 4 decides on.

- [ ] **Step 1: Confirm what the pool actually rents**

```csharp
byte[] rented = ArrayPool<byte>.Shared.Rent(20_589_008);
Console.WriteLine($"{rented.Length:N0}");   // 33,554,432 on .NET 10
```

The shared pool does serve a 20 MB rent, contrary to the common belief that it caps at 1 MiB — but it rounds to the next power of two. **Record the actual figure on the bench machine**; the spec's 33 554 432 was measured on a container.

- [ ] **Step 2: Measure the first load and the second separately**

The first load pays the page commits either way. If only the second gains, a caller who loads one index gains nothing and pays the residency — which is the ordinary case for an embedding index and therefore decides the lot.

- [ ] **Step 3: Commit the numbers into the guide**

```bash
git add docs/guides/performance.md
git commit -m "Publish what the pool costs beside what it saves"
```

---

### Task 4: decide, and be willing to refuse

**Files:**

- Create: `docs/decisions/00NN-<slug>.md`, either way
- Modify: `src/`, reverted if the trade is refused

**Interfaces:**

- Consumes: #433 landed, Task 3 complete.
- Produces: the answer #435 closes on.

- [ ] **Step 1: Apply the bar**

The saving must be worth 33.5 MB held for the life of the process **for a caller who loads once**.

- [ ] **Step 2a: If it clears** — ship, with the residency stated in `docs/guides/performance.md` and the embeddings guide, and an ADR for the trade.

- [ ] **Step 2b: If it does not** — `git revert` the code, **keep the tests and the measurement**, and write the ADR anyway. ADR 0052 is the model: a refusal with its measurement attached is a result and it stops the fourth proposal.

- [ ] **Step 3: Say that no bench row was added**

Either way this adds no row to `bench/README.md`. Say so rather than leaving a reader to wonder whether one was forgotten.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Decide the pool on the residency, not on the millisecond"
```

---

### Task 5: the gates, then the pull request

- [ ] **Step 1: Build and test**

```bash
dotnet build Lodestar.slnx -c Release
dotnet test Lodestar.slnx -c Release
```

- [ ] **Step 2: Format and markdown**

```bash
dotnet format Lodestar.slnx --verify-no-changes
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
```

- [ ] **Step 3: Every lint-job guard, on this branch**

```bash
python tools/check_comment_length.py
python tools/check_no_console_writeline.py
python tools/check_bench_map.py
python tools/check_sample_coverage.py
python tools/check_machine_paths.py --no-environment
python tools/check_version_floor.py
python tools/check_sample_culture.py
python tools/check_adr_immutable.py --base main
python -m pytest tools/tests -q
```

- [ ] **Step 4: A code review before the pull request exists, reading the slice arithmetic specifically**

Every `AsMemory`, `AsSpan` and `Slice` on the pooled path, against the byte count actually read. This is the review that finds the defect the gates cannot.

- [ ] **Step 5: Open the pull request**, body carrying both columns and `Closes #435`.

## What this plan does not do

- **It does not pool the vector block.** That array becomes `_data` and outlives the call; returning it is a use-after-free the runtime will not complain about. #436 is where the block's lifetime is reconsidered, and it is blocked on a format decision.
- **It does not touch `Load(ReadOnlyMemory<byte>)`.** That overload parses the caller's memory and must keep doing so.
- **It does not pool the tf-idf or vectorizer artifacts** unless Task 3 says they gain. They sit below the large-object-heap threshold, where none of this applies.
- **It does not assume the trade is worth taking.**

## Execution log — Task 1, run 2026-08-29

**Answer: the payload escapes nowhere, so it is poolable — with two constraints the plan did not
have and one it half-anticipated.**

**The plan's Interfaces line was wrong about the scope.** It reads as though `ReadAllBytes` served
the embedding path; it has **ten call sites across two packages** — the three vectorizers,
`EmbeddingIndex`, `VocabTxtLoader`, `BpeFilesLoader`, `SentencePieceModelLoader`,
`TokenizerJsonLoader` (three of them) and `NpyFile`. Pooling is per-call-site, because the site
that consumes the buffer is the site that must return it, so this is the size of Task 2 rather
than a detail.

**Retention: none.** Every consumer takes `ReadOnlyMemory<byte>` as a *parameter* and none stores
it in a field. `EmbeddingIndex.Parse` takes `payload.Span` and hands it to a `Utf8JsonReader`;
ids come back from `reader.GetString()` as new strings and vectors from `Base64Numbers.ReadSingles`
as its own `float[]`. The vectorizers, the vocabulary loaders and the SentencePiece loader return
dictionaries, lists and strings that are allocated, not sliced.

**Constraint 1 — `JsonDocument` does not copy.** `JsonDocument.Parse(ReadOnlyMemory<byte>, …)`
retains the memory for the document's lifetime rather than copying it. Seven sites do this:
six in `TokenizerJsonLoader`, one in `BpeFilesLoader.ParseVocab`. All are `using`-scoped, so the
retention ends inside the method and nothing escapes — but **a pooled buffer must be returned
after the document is disposed**, not merely before the method returns, and three of those sites
pass `JsonArtifact.ReadAllBytes(...)` inline with no variable to return. Those three need
restructuring before they can pool at all.

**Constraint 2 — the shared helper the plan warned about is real.** Step 3 asked whether the two
paths share a helper that would pool on both. They do: `EmbeddingIndex.Load(ReadOnlyMemory<byte>)`,
which parses **the caller's** memory in place, and `Load(Stream)`, which parses ours, both call
`FromPayload`. **Pooling inside `FromPayload` would return a caller's buffer to `ArrayPool.Shared`**
— the exposure defect this plan's Global Constraints call a security defect, reachable from public
API in one step. The rent and the return therefore belong at the `Load(Stream)` call site, above
`FromPayload`, and no pooling may go inside it or inside `Parse`.

**An unrelated finding, recorded rather than fixed here.** `NpyFile.Read(Stream)` calls
`JsonArtifact.ReadAllBytes(source, limits).ToArray()`. `ReadAllBytes`'s own documentation names
that as the thing not to do — *"reaching for the array behind it, or for `.ToArray()`, both
reintroduce the copy this return type exists to remove"* — so every `.npy` read pays a full extra
copy of the block, about 15 MB at the corpus size. It came in with #450 and wants its own issue,
not this branch.

**The ceiling is known now, which is why this lot could start.** [#433](https://github.com/CyrilB1531/lodestar/issues/433)
measured the warm-heap subsidy at **8.1%**, with the mechanism being one fewer garbage collection
per load window rather than anything mysterious. That is the most this lot can return in time.
Whether it is worth 33.5 MB held by the pool for the life of the process is Task 4's decision,
and Task 4 now has its number.
