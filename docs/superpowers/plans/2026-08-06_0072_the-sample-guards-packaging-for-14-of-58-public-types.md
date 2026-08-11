# #72 The sample covers every public type — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make ADR 0009's claim true — every exported public type reachable from the sample by a **member** reference, with a gate that fails the run when one stops being.

**Architecture:** The calls are split by lot across four files. `PackagingGate.cs` enumerates the exported types of the three assemblies **as NuGet resolved them for the sample**, inspects the compiled sample's `MemberReference` table, and fails on any type with no member referenced. Exclusions are named, reasoned, and themselves validated.

**Tech Stack:** `System.Reflection.Metadata` / `MetadataLoadContext`, .NET console sample, NuGet local feed.

**Spec:** `2026-08-06_0072_the-sample-guards-packaging-for-14-of-58-public-types.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `feat/72-sample-covers-every-public-type`. Never commit to `main`.
- **The gate reads the packaged assemblies**, never the `src/` project outputs.
  Reading the project outputs makes it pass on exactly the defects it exists to
  catch.
- **A member reference, never `typeof`.** A type reference proves metadata
  presence and nothing else.
- **No model weights** (ADR 0003).
- The sample builds only after a `pack`, with an isolated `NUGET_PACKAGES`.

### Reusable verification commands

```bash
cd <repo>
SCRATCH=/tmp/gate72

pack_feed() {
  rm -rf ./artifacts "$SCRATCH/pack"
  NUGET_PACKAGES="$SCRATCH/pack" bash -c '
    for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy; do
      dotnet pack "$p" -c Release -o ./artifacts || exit 1
    done'
}

run_sample() {
  rm -rf "$SCRATCH/sample"
  NUGET_PACKAGES="$SCRATCH/sample" dotnet run --project samples/DataNet.Sample -c Release
}
```

---

### Task 1: Measure the real coverage

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the numbers the pull request rests on, and the list of 41.

- [x] **Step 1: Enumerate the exported types of the packaged assemblies**

Use `MetadataLoadContext` over the assemblies in `./artifacts`, not over
`src/**/bin`.

Expected: **58** exported public types across the three packages.

- [x] **Step 2: Read the sample's `MemberReference` table**

```bash
pack_feed
dotnet build samples/DataNet.Sample -c Release
```

Then inspect `DataNet.Sample.dll`'s `MemberReference` table and map each entry to
its declaring type.

- [x] **Step 3: Classify all 58**

Expected:

| | Before |
| --- | ---: |
| With a member referenced | 14 |
| Merely named (`typeof`) | 3 |
| Not mentioned at all | 41 |

- [x] **Step 4: Note the three `typeof`-only cases explicitly**

They are the reason D1 exists: a type reference proves the type is in metadata,
not that a member is callable, that its signature resolves, or that its parameter
types shipped.

---

### Task 2: Exercise the surface, split by lot

**Files:**

- Create: `samples/DataNet.Sample/Lot1Distances.cs`, `Lot2Vectorization.cs`,
  `Lot3Embeddings.cs`, `Lot4Fuzzy.cs`
- Modify: `samples/DataNet.Sample/Program.cs`

**Depends on:** Task 1.

- [x] **Step 1: One file per lot**

58 types will not fit readably in one file, and the sample doubles as
documentation — a wall of calls stops being either.

- [x] **Step 2: `Program.cs` keeps the framework banner and four calls**

The banner is what makes a resolution failure visible rather than inferred.

- [x] **Step 3: A real member call per type, not a `typeof`**

Where a type is a record or options bag, constructing it and reading a property
counts; naming it does not.

- [x] **Step 4: Run it**

```bash
pack_feed && run_sample
```

---

### Task 3: The gate

**Files:**

- Create: `samples/DataNet.Sample/PackagingGate.cs`

**Depends on:** Task 2.

- [x] **Step 1: Enumerate exported types from the resolved assemblies**

The ones NuGet gave the sample. This is the distinction the whole gate rests on.

- [x] **Step 2: Fail the run on any type with no member referenced**

Not a warning, not a printed report. A gate that continues is a report.

- [x] **Step 3: The one exclusion, with its reason in the code**

`OnnxTextEmbedder` — constructing it loads an ONNX model, and weights are never
committed.

- [x] **Step 4: Make the exclusion list self-validating**

**An exclusion naming a type that no longer exists fails the gate.** Otherwise the
list becomes where coverage quietly goes to die.

- [x] **Step 5: Prove the gate fails**

```bash
# Remove one member call from Lot4Fuzzy.cs, then:
pack_feed && run_sample; echo "exit: $?"
```

Expected: non-zero, naming the type. Restore afterwards. A gate never seen to fail
is not known to work.

- [x] **Step 6: Prove the exclusion check fails too**

Rename the excluded type in the exclusion list to something that does not exist
and confirm the run fails.

---

### Task 4: Correct the ADR, then gate

**Files:**

- Modify: `docs/decisions/0009-sample-consumes-a-local-feed.md`

**Depends on:** Task 3.

- [x] **Step 1: Amend ADR 0009**

Its text implied a guarantee the repository did not have. Record what the gate now
enforces, and that it enforces reachability rather than correctness — the oracles
do the latter.

- [x] **Step 2: State the ongoing cost, plainly**

**Every new public type must now be exercised in `Lot*.cs`**, member reference and
all, or the sample build fails. That is a real cost on every feature branch, and
it is the point.

- [x] **Step 3: Re-measure**

Expected:

| | Before | After |
| --- | ---: | ---: |
| With a member referenced | 14 | 57 |
| Merely named | 3 | 0 |
| Documented exclusions | 0 | 1 |
| Not mentioned at all | 41 | 0 |

- [x] **Step 4: Full gate**

```bash
dotnet build DataNet.slnx -c Release && dotnet test DataNet.slnx -c Release 2>&1 | tail -3
pack_feed && run_sample
dotnet format --verify-no-changes
```

- [x] **Step 5: Commit**

```bash
git commit -m "Exercise the whole public surface, not the fraction that fit in one file"
git commit -m "Fail the sample when a public type stops being reachable"
```
