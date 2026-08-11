# Design — #72: the sample guards packaging for 14 of 58 public types

**Date:** 2026-08-06 · **Issue:** #72 · **Branch:** `feat/72-sample-covers-every-public-type` ·
**Checkout:** `<repo>`

## Problem

ADR 0009 says the sample proves the packages work for a consumer. Measured, it
proves that for **14 of 58 exported public types**.

| | Before |
| --- | ---: |
| Exported public types | 58 |
| With a member referenced | 14 |
| Merely named (`typeof`) | 3 |
| Not mentioned at all | **41** |

The gap is not the sample being small — it is ADR 0009's text implying a guarantee
the repository does not have. A gate covering a quarter of the surface, described
as covering the surface, is worse than a smaller claim honestly stated.

## Decisions

### D1 — A member reference, not a type reference

`typeof(T)` proves the type exists in metadata. It does **not** prove a member is
callable, that its signature resolves, or that its parameter types shipped.

Three types were in that state already. The gate counts a **member** reference or
nothing.

### D2 — The gate reads the assemblies NuGet resolved for the sample

Not the `src/` project outputs. The packaged assemblies are the artefact under
test; reading the project outputs would make the gate pass on exactly the defects
it exists to catch.

### D3 — The gate lives in the sample and fails the run

`PackagingGate.cs` enumerates the exported types of the three assemblies, checks
the `MemberReference` table of the compiled `DataNet.Sample.dll`, and **fails when
one has no member referenced**.

A gate that reports and continues is a report.

### D4 — Exclusions are named, reasoned, and validated

One: **`OnnxTextEmbedder`**. Constructing it loads an ONNX model, and weights are
never committed (ADR 0003).

It carries its reason in the code, and **an exclusion naming a type that no longer
exists fails the gate** — otherwise the exclusion list becomes the place coverage
quietly goes to die.

### D5 — The calls are split by lot, not piled into `Program.cs`

`Lot1Distances.cs`, `Lot2Vectorization.cs`, `Lot3Embeddings.cs`,
`Lot4Fuzzy.cs`. `Program.cs` keeps the framework banner and four calls.

58 types will not fit readably in one file, and the sample doubles as
documentation.

### D6 — ADR 0009 is corrected, not merely extended

Its text implied the guarantee. The amendment records what the gate now actually
enforces.

## Expected result

| | Before | After |
| --- | ---: | ---: |
| With a member referenced | 14 | **57** |
| Merely named (`typeof`) | 3 | 0 |
| Documented exclusions | 0 | 1 |
| Not mentioned at all | 41 | **0** |

## A consequence to accept deliberately

**Every new public type now has to be exercised in `Lot*.cs`**, member reference
and all, or the sample build fails. That is a real ongoing cost on every feature
branch, and it is the point: it is what makes the ADR's claim true.

## Out of scope

- Testing behaviour. The gate proves reachability from a packaged assembly, not
  correctness — the oracles do that.

## What "done" means

57 of 58 types with a member referenced; one documented exclusion; the gate
failing when a type stops being reachable; ADR 0009 corrected.
