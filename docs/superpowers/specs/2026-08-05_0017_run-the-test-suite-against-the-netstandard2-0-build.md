# Design — #17: run the test suite against the `netstandard2.0` build

**Date:** 2026-08-05 · **Issue:** #17 · **Branch:** `test/17-netstandard-runtime-validation` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

ADR 0001 recorded this gap when #1 shipped, and 0.2.0 is about to be published to
nuget.org with it still open.

Multi-targeting produced `netstandard2.0` assemblies that are **compiled but never
executed**. The suite targets `net10.0`, so it exercises the net10 build only —
while the two differ by conditional compilation in exactly the places most likely
to carry a transcription error:

- the scalar `VectorMath.Dot` fallback, with no `Vector<T>` SIMD
- `Substring` instead of span slicing throughout the stemmers
- `Guard`, and portable stand-ins for `CollectionsMarshal`, `Array.Fill`,
  `.Order()`, `KeyValuePair` deconstruction

A mistake in any of those reaches .NET Framework, Mono and Unity consumers **with
a green build behind it** — and broad reach is 0.2.0's headline feature. Shipping
the claim before the evidence is the thing to avoid.

## Decisions

### D1 — Mirror projects that link the sources, never copy them

Three new test projects reference the libraries with
`SetTargetFramework=netstandard2.0` and `<Compile Include>` the existing test
sources.

One suite, two builds. A copied suite drifts the first time someone adds a test to
one and not the other, and the drift is silent. A linked suite cannot: any test
added later is picked up by both automatically.

### D2 — `netstandard2.0` is a contract, not a runtime

The tests cannot run *on* it. They run on `net10.0` — identical host — and only
the assembly under test changes. This is worth stating in the project file itself,
because the arrangement looks like a mistake to anyone who has not thought about
it.

### D3 — A `TargetFrameworkAttribute` guard in each mirror, and it is not ceremony

**#10 has already produced entirely plausible numbers while measuring the wrong
build.** The identical failure here would leave every test passing while proving
nothing — a strictly worse outcome, because a green suite is more persuasive than
a benchmark table.

So each mirror asserts the `TargetFrameworkAttribute` of the assembly under test.
That guard is the `+1` test in each project's count.

### D4 — The guard is verified by breaking the isolation on purpose

Not assumed to work. Removing `SetTargetFramework` must produce:

```text
Assert.Equal() Failure: Strings differ
Expected: ".NETStandard,Version=v2.0"
Actual:   ".NETCoreApp,Version=v10.0"
```

A gate nobody has seen fail is not known to work.

### D5 — Update the two documents that state the old limitation

ADR 0001 and `CHANGELOG.md` both say the `netstandard2.0` build is compile-verified
only. That stops being true here, and a stale limitation is worse than none — it
tells a reader to distrust something that is now proven.

### D6 — No workflow change

`dotnet test DataNet.slnx` covers the solution, so CI picks the mirrors up as soon
as they are in it. Adding a job would be a second place to keep in step.

## Expected shape

| Suite | net10.0 | netstandard2.0 |
| --- | ---: | ---: |
| Text | 147 | 148 |
| Embeddings | 11 | 12 |
| Fuzzy | 10 | 11 |
| **Total** | **168** | **171** |

The `+1` in each mirror is D3's guard. **All must pass** — the portable fallbacks
agree with the same frozen oracle corpora as the net10 paths, or one of them is
wrong.

`DataNet.Text` needs a second `InternalsVisibleTo`, since the mirror assembly has
its own name.

## Out of scope

- Optimising the `netstandard2.0` paths.
- Multi-targeting the test projects themselves, which would run them on a runtime
  nobody ships to.

## What "done" means

Three mirrors green; each guard verified by deliberate breakage; ADR 0001 and the
changelog corrected; no workflow change needed.
