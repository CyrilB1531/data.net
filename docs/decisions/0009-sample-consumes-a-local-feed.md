# 0009 — The sample restores from a local feed, not nuget.org

**Status:** accepted · **Date:** 2026-08-05

## Context

Nothing in the repository consumed the published packages. Everything built
through `ProjectReference`, so packaging was only ever exercised by
`dotnet pack` — never by installing and using the result.

That hides a whole class of defect. A package can pack cleanly and still be
broken for a consumer: a missing dependency in the `netstandard2.0` group so
`System.Memory` is absent at run time; an XML documentation file that fails to
ship; a type that is `public` in source but unreachable from outside the
assembly. None of those would have failed CI.

## Decision

`samples/DataNet.Sample` consumes the three packages by **`PackageReference`**,
restoring from a **local folder fed by `dotnet pack`** rather than from nuget.org.

The version is bound to `$(Version)` from the repository's root
`Directory.Build.props`, which applies to the sample too, so it tracks whatever
`dotnet pack` just produced instead of pinning a number that goes stale.

> **Amended by [0012](0012-per-package-versioning.md).** The three packages now
> version independently, so there is no repository-wide `$(Version)` left to bind
> to: the sample imports each project's `Version.props` and uses one property per
> package. The guarantee is unchanged — it still tracks what `dotnet pack` just
> produced — it just reads three sources instead of one.

## Why not nuget.org

Restoring from nuget.org would test the **last published** version. That is more
honest as documentation — it is what a reader would actually get — but useless as
a gate: it can only fail after a broken package is already public, and it cannot
run at all before the first publish of a new version. At the time this landed,
`0.2.0` was not yet on nuget.org, and the sample already worked against it.

A local feed inverts that: the gate runs on what is *about* to ship.

## Consequences

- **The packages must be packed before the sample restores.** The CI job does
  this; running it by hand requires the same:

  ```bash
  for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy; do
    dotnet pack "$p" -c Release -o ./artifacts
  done
  dotnet run --project samples/DataNet.Sample -c Release
  ```

- **The sample is deliberately outside `DataNet.slnx`.** Inside the solution,
  `ProjectReference` resolution would quietly satisfy the references and the
  sample would prove nothing while appearing to work.
- **It runs in CI**, because a sample that is never built rots into documentation
  that lies.
- **It covers `lib/net10.0` only.** The `netstandard2.0` package assets are not
  consumed by anything: the netstandard2.0 *assemblies* are covered by the mirror
  test projects, but the *package* dependency group for that target is not.
  Adding a `net8.0` target to the sample would close this, since net8.0 resolves
  `lib/netstandard2.0`; it needs the 8.0 runtime in CI.
- ONNX inference is not exercised. Model weights are deliberately not committed,
  so the sample uses the tokenizer and says so, rather than failing on a missing
  file.
