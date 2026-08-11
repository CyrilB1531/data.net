# Design — #1: multi-target `netstandard2.0` alongside `net10.0`

**Date:** 2026-08-04 · **Issue:** #1 · **Branch:** `feat/1-netstandard2.0-multitarget` ·
**Checkout:** `<repo>`

## Problem

The three libraries target `net10.0` only. That is the right default — every fast
path in the repository is written for it — but it excludes .NET Framework
4.6.1+, Mono, Xamarin and Unity, which is most of the installed base a text
library would otherwise be dropped into. A consumer on any of those cannot
reference `DataNet.Text` at all.

The naive fix is a second package with a reduced API. That is the thing to avoid:
two packages means two surfaces to document, two sets of oracles to argue about,
and a consumer who discovers the difference at the call site rather than at
install time.

## Constraints this change inherits

- **Warnings are errors repository-wide** (#6, merged before this branch). A
  `netstandard2.0` leg that compiles with warnings does not compile.
- **The provenance rule** (ADR 0003): every algorithm here is an original
  implementation. Nothing about this change may be copied from a polyfill library
  whose licence does not permit it.
- **The oracle corpora are the definition of correct.** Whatever the second target
  does, it must replay them identically — or the divergence must be recorded.

## Decisions

### D1 — One package, two frameworks, one public surface

`TargetFrameworks=net10.0;netstandard2.0` in each of the three libraries. A single
`dotnet pack` emits both `lib/` folders and the correct dependency groups.

**The public API is identical on both.** `netstandard2.0` reaches equivalent
behaviour through conditional compilation, never through a missing method. A
consumer reading the NuGet page sees one API; the framework they land on is an
implementation detail. This is the whole point of the change and every later
decision is subordinate to it.

### D2 — Gaps are closed in a fixed order of preference

1. **PolySharp compile-time polyfills** (`PrivateAssets="all"`) — records, `init`,
   `Index`/`Range`, the nullable attributes. Costs nothing at runtime and leaves no
   package dependency.
2. **`System.Memory` / `System.Numerics.Vectors`**, referenced *only* on the
   `netstandard2.0` target — `Span`, `Memory`, `ArrayPool`, `Vector<T>`.
3. **A hand-written fallback**, when neither of the above covers it.

The order matters because each step down adds either a dependency a consumer sees
or code the repository has to maintain.

### D3 — Shared helpers, not one `#if` per call site

`ArgumentNullException.ThrowIfNull` is net-only and appears at nearly every public
entry point. Writing `#if NET` around each one would put a directive in every file
in the repository and would trip CA1510 on the net10 leg.

Instead, `src/Shared/` gains two files compiled into all three libraries under
`DataNet.Internal`:

- **`Guard.NotNull`** — a single `#if` that delegates to
  `ArgumentNullException.ThrowIfNull` on net10 and checks by hand on
  `netstandard2.0`.
- **`StringCompat`** — the `string` char overloads (`StartsWith`, `EndsWith`,
  `Contains`) that exist only on net10.

`src/Shared/GlobalUsings.cs` makes both visible everywhere, so no call site needs
a directive. `src/Directory.Build.props` compiles the three files into each
library with `Link="Internal/…"`.

### D4 — `VectorMath.Dot` keeps its SIMD path, and says so

`Vector<T>`'s span-based constructor is net-only. `Dot` therefore keeps its
`Vector<T>` loop under `#if NET5_0_OR_GREATER` and falls back to a scalar loop on
`netstandard2.0`.

This is **the one deliberate behavioural split in the change** — same results,
different throughput — and it is the reason D6 exists.

### D5 — Central package management for `src/`

`src/Directory.Packages.props` pins `PolySharp`, `System.Memory` and
`System.Numerics.Vectors` in one place. `src/Directory.Build.props` chains to the
repository root explicitly, because MSBuild stops at the nearest
`Directory.Build.props` and the libraries would otherwise lose the root's
warnings-as-errors and package identity.

### D6 — The gap this leaves is stated, not papered over

**The `netstandard2.0` build is compile-verified, not behaviour-verified.** The
test projects target `net10.0`, so they exercise the net10 assemblies only. The
two builds differ by conditional compilation — the scalar `Dot` fallback most of
all — so "158 tests pass" must not be read as covering both targets.

This goes in ADR 0001 and in a follow-up issue. A claim of reach that no test
backs is worse than no claim.

### D7 — The package layout is verified against the produced nuspec

Not assumed. After `dotnet pack`, the nuspec must show both `lib/` folders, the
`System.Memory` / `System.Numerics.Vectors` dependencies under the
`.NETStandard2.0` group *only*, and **no PolySharp dependency at all** —
`PrivateAssets="all"` is worthless if it silently fails.

## Expected shape of the work

The second target will surface a long tail of compile errors, all mechanical:
range operators, `string.Join(char)`, `MathF`, `Array.Fill`, `CollectionsMarshal`,
`KeyValuePair` deconstruction, `.Order()`. Each becomes a portable equivalent.
None of them should change a single oracle value; if one does, the fallback is
wrong.

## Out of scope

- The SonarLint cleanup (#7), the CHANGELOG (#8) and the comparison benchmark
  suite (#10). They touch some of the same files and must not ride along — the
  diff is checked for stray `#pragma warning disable S…` before the PR.
- Running the test suite against the `netstandard2.0` assemblies. That is the
  follow-up D6 creates, not this branch.
- Any change to the net10 fast paths.
