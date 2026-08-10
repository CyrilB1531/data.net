# Design — #64: per-package versioning, and package references inside `src/`

**Date:** 2026-08-05 · **Issue:** #64 · **Branch:** `feat/64-split-versioning-nuget-refs` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

The three libraries share one `<Version>` in `Directory.Build.props` and release
together from a single `v*` tag. A patch touching only `DataNet.Fuzzy` therefore
**republishes the other two at a number describing no change in them**.

The build graph and the release graph disagree: `DataNet.Fuzzy` reaches
`DataNet.Text` by `ProjectReference`, so nothing ever exercises the reference a
consumer actually gets.

## Decisions

### D1 — `src/<Package>/Version.props` per project, holding a **named** property

Chosen over Nerdbank.GitVersioning, MinVer and GitVersion because all three
*derive* a version from git topology, and nothing here wants a derived version:
**the number is a deliberate semantic statement**, and writing it down costs no
package at all.

Named rather than plain `<Version>` because three places need it for three
different reasons — the csproj (its own identity), the CPM floor (what
`DataNet.Fuzzy` requires), and the sample (the version just packed).

### D2 — `DataNet.Fuzzy → DataNet.Text` by `PackageReference`

So the build graph matches the release graph.

The floor in `src/Directory.Packages.props` is **written out in full** rather than
tracking `$(DataNetTextVersion)`: it answers a different question — the minimum a
consumer must take — and because it always names an already-published release,
`git clone && dotnet build` needs no pack step. No chicken-and-egg on a fresh
clone.

### D3 — An opt-in developer loop, asserted against by CI

`export DataNetUseProjectRefs=true` flips the reference back for a branch editing
both packages. The default is the shipped path; the build prints a
high-importance message when the property is on; **CI asserts the default**.

### D4 — The split is exercised, not merely enabled

`DataNet.Fuzzy` ships `0.2.1` while `DataNet.Text` and `DataNet.Embeddings` stay
at `0.2.0`, **and the sample builds and runs against that mix**. A capability that
has never been used is a capability that does not work.

### D5 — Per-package tags; the umbrella `v*` tag is retired

`DataNet.Fuzzy/v0.2.1`.

## Two things found by measurement, both silent

### F1 — `SetTargetFramework` does not cross a `PackageReference`

NuGet resolves package assets against the **consuming** project's framework. After
the migration, `DataNet.Fuzzy.NetStandard.Tests` was replaying the
`netstandard2.0` `DataNet.Fuzzy` against the **net10.0** `DataNet.Text` — half a
mirror, every test green, because `NetStandardAssemblyGuardTests` only ever
inspected `DataNet.Fuzzy`.

**Exactly the false confidence that guard exists to prevent.** The suite now pins
`DataNet.Text` itself and the guard covers it. Verified by removing the pin:
`Expected ".NETStandard,Version=v2.0" / Actual ".NETCoreApp,Version=v10.0"`.

### F2 — A direct `ProjectReference` silently outranks a `PackageReference` of the same id

With no warning. That is the only reason `bench/DataNet.NetStandard.Benchmarks`
still resolves `netstandard2.0` assemblies. Load-bearing in two places now, so it
is written down rather than left to be rediscovered.

## Two deliberate departures from the issue

### X1 — `-p:Version` is removed from every workflow, not made safe

The issue asked to keep passing the tag through the environment as data. This goes
further: the tag is **compared against `Version.props`** and the job refused on a
mismatch, so nothing derived from a tag or a dispatch input reaches `dotnet pack`
at all. The repository stays authoritative, and a `<Version>` silently diverging
from what was published becomes impossible.

### X2 — The grep acceptance criterion is replaced by an MSBuild check

`git grep '<ProjectReference' -- 'src/**/*.csproj'` **contradicts the issue's own
§3**, whose prescribed conditional `ItemGroup` contains that literal text. The
grep would fail on the sanctioned solution, and could not distinguish the shipped
path from the dev loop either way.

CI instead asks MSBuild what the project *resolved*:
`dotnet msbuild src/DataNet.Fuzzy -getItem:ProjectReference` must come back empty.
Verified to discriminate: `[]` by default, `['../DataNet.Text/DataNet.Text.csproj']`
with the property set.

Note also that the `.nuspec` check proves something different from what the issue
implies: **both paths emit the same `<dependency>`**, so it cannot tell you which
was taken. It guards an unexpected or vanished dependency instead.

## What "done" means

Per-package `Version.props`; the one `src/` cross-reference by package; the dev
loop opt-in and asserted against; the split exercised at `0.2.1`/`0.2.0`/`0.2.0`
with the sample green; ADR 0012 written and ADR 0009 amended.
