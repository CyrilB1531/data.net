# #100 Fewer copies on the index load path — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Take `EmbeddingIndex.Load` from five passes and 90 MB down to three and 35 MB, without changing a byte of the artifact, a public signature, or a single line of the hardening suites.

**Architecture:** Three independent changes, each measured before it is made. One buffer on the read path, sized from the stream's length with a correct fallback when the stream lies. Base64 decoding straight into the destination array, with one `Decode<T>` replacing two readers and keeping every exception identical. A vectorized non-finite scan on net10 that detects only — the scalar loop still locates, so the message still names the item.

**Tech Stack:** C# (net10.0 + netstandard2.0), `System.Buffers.Text.Base64`, `System.Numerics.Vector`, BenchmarkDotNet `[MemoryDiagnoser]`.

**Spec:** `2026-08-07_0100_loading-an-index-copies-the-vector-block-five-times.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `perf/100-index-load-copies`. Never commit to `main`.
- **No byte of the artifact changes.** No format change, no public signature moved.
- **The hardening suites are not modified.** If one needs adjusting, the change
  altered behaviour and is out of scope.
- **Same exception types and messages on every path**, including the fallbacks.
- A `perf/` pull request carries before/after numbers and names the machine.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all()  { dotnet build -c Release; }
test_all()   { dotnet test -c Release; }
test_hard()  { dotnet test -c Release --filter "FullyQualifiedName~Persistence|FullyQualifiedName~Hardening"; }
bench_load() { dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*Persistence*' --inProcess; }
```

---

### Task 1: Count the passes and the bytes

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the baseline, and the share of each candidate — so no optimisation
is applied blind.

- [ ] **Step 1: Measure `Load` with `[MemoryDiagnoser]`**

```bash
bench_load 2>&1 | tail -20
```

Expected: ~90 MB allocated for a 15 MB payload.

- [ ] **Step 2: Attribute the passes**

Read → `.ToArray()` → base64 decode → copy into `float[]` → non-finite scan.
Expected: five.

- [ ] **Step 3: Measure the non-finite scan's share specifically**

Expected: **18 %** of the load figure. Task 4 exists because of that number; if it
comes out at 2 %, Task 4 does not happen.

---

### Task 2: One buffer on the read path

**Files:**

- Modify: `src/Shared/Persistence/JsonArtifact.cs` and the ten `internal` loaders
  that follow the type change

**Depends on:** Task 1.

- [ ] **Step 1: Return `ReadOnlyMemory<byte>` over a buffer sized from the stream**

Instead of accumulating into a growable `MemoryStream` and calling `.ToArray()`.

- [ ] **Step 2: The fallback, which is the correctness argument**

A stream that will not say how long it is — **or that says it wrong** — falls back
to the growable path, **with its position put back first**, so nothing is silently
truncated.

- [ ] **Step 3: A test for a stream that lies about its length**

Both directions: reports too short, reports too long. This is the case that
produces a silently truncated index, which is far worse than a slow one.

- [ ] **Step 4: Confirm nothing public moved**

```bash
git diff main -- src | grep -E "^[+-]\s*public" | head
```

Expected: empty. All ten loaders are `internal`.

---

### Task 3: Decode into the destination

**Files:**

- Modify: the artifact readers (`ReadSingles`, `ReadDoubles`)

**Depends on:** Task 2.

- [ ] **Step 1: Size the array from the token's encoded length**

- [ ] **Step 2: `Base64.DecodeFromUtf8` straight into it**

- [ ] **Step 3: One `Decode<T>` replacing `ReadBoundedRaw` and `ReadUnboundedRaw`**

Falling through to the old `TryGetBytesFromBase64` path for any token that is
**not canonical**.

- [ ] **Step 4: Prove the exception surface is unchanged**

```bash
test_hard 2>&1 | tail -3
```

Same types, same messages, on **every** path — the fast one and the fallback.
Keeping this identical is what makes the change a performance change rather than a
behavioural one.

- [ ] **Step 5: Confirm the size bound still applies before allocation**

`MaxTotalBytes` caps the payload before anything is allocated, which is what
bounds the vector block now that it has no element-count limit (#62).

The argument gets **stronger**: the destination is sized from a length that gate
has already bounded, rather than from a count discovered after decoding. Say so.

---

### Task 4: Vectorize the non-finite scan — detect only

**Files:**

- Modify: `src/DataNet.Embeddings/Search/EmbeddingIndex.Persistence.cs`

**Depends on:** Task 3, and on Task 1 Step 3 having measured 18 %.

- [ ] **Step 1: A vector pass on `net10.0` that only detects**

- [ ] **Step 2: Keep the scalar loop to locate**

The exception message must still name the **exact item and component**. Speed must
not cost diagnosability — a "vector contains NaN somewhere" message turns a
five-minute fix into an afternoon.

- [ ] **Step 3: `netstandard2.0` keeps the scalar path**

- [ ] **Step 4: Both targets, and the hardening suites unmodified**

```bash
build_all && test_all 2>&1 | tail -3
git diff --stat main -- tests/   # the hardening suites must not appear
```

---

### Task 5: Publish what it is worth

**Files:**

- Modify: `bench/README.md`, `docs/decisions/0011-persistence-format.md`,
  `CHANGELOG.md`

**Depends on:** Task 4.

- [ ] **Step 1: Re-measure, `--inProcess` on both columns**

Per #87 and #88 — otherwise the comparison mixes the framework with the harness.

Expected:

| | before | after |
| --- | ---: | ---: |
| Passes | 5 | 3 |
| Allocated | 90 MB | 35 MB |

- [ ] **Step 2: Compare against the run the old figure came from**

A ratio against a number taken under a different harness is not a ratio. State
which run each column comes from.

- [ ] **Step 3: Record it in ADR 0011**

The format decision document is where a future reader will look for what the load
path costs.

- [ ] **Step 4: Commit**

```bash
git commit -m "Read an artifact into one buffer sized before it is filled"
git commit -m "Decode a base64 vector block into the array that keeps it"
git commit -m "Scan a restored vector block for non-finite values by vector"
git commit -m "Publish what the read-path change is worth"
```
