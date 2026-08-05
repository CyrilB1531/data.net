# 0012 — Each package versions and releases on its own

**Status:** accepted · **Date:** 2026-08-05

## Context

`Directory.Build.props` declared one `<Version>` for the repository, inherited by
`DataNet.Text`, `DataNet.Embeddings` and `DataNet.Fuzzy` alike, and both release
workflows packed all three from a single `v*` tag. A patch touching only
`DataNet.Fuzzy` therefore republished the other two at a number describing no
change in them, and each package's version stopped saying anything about that
package.

There was also exactly one inter-project reference under `src/`:
`DataNet.Fuzzy → DataNet.Text`, by `ProjectReference`.

Worth stating plainly, because it changes what this decision is *for*: a
`ProjectReference` between two packable projects already becomes a NuGet
`<dependency>` in the produced `.nuspec`. Consumers of `DataNet.Fuzzy` were
already pulling `DataNet.Text` as an ordinary package dependency. The shipped
graph was never wrong. What was wrong was that the *build* graph and the
*release* graph disagreed, and that disagreement is what forced the lockstep.

## Decision

**Each publishable project declares its own version** in a sibling
`Version.props`, and `DataNet.Fuzzy` reaches `DataNet.Text` through a
`PackageReference` on the published package.

The first exercise of the split ships with this change: `DataNet.Text` and
`DataNet.Fuzzy` both go to `0.3.0` — the first has gained stop-word lists, the
second has changed how it declares its dependency. `DataNet.Embeddings` followed
to `0.3.0` shortly after, on its own account, for the vocabulary loaders.

That all three ended up on the same number is not a dilution of the point.
Independent versioning is not a rule that numbers must differ; it is the removal
of the rule that they must match. What was previously impossible is a package
holding still while its neighbours move — and each of these three moved because
of something in it, which is the property the split buys.

### Why a per-project `Version.props` and not a versioning tool

Nerdbank.GitVersioning and MinVer both support what is needed here, and GitVersion
can be configured into it. All three were declined, for the same reason: they
derive a version from git height and tag topology, and nothing in this repository
wants a derived version. The number is a deliberate semantic statement about a
public API. Writing it down makes it reviewable in the diff that changes it,
costs no package — not even a build-time one — and leaves the decision open. The
day the release cadence genuinely diverges and hand-editing three files becomes
the bottleneck, MinVer is the smallest step from here.

The file holds a *named* property (`DataNetTextVersion`) rather than plain
`<Version>`, because three places need the number for three different reasons:
the csproj (its own identity), `src/Directory.Packages.props` (the floor
`DataNet.Fuzzy` depends on) and `samples/DataNet.Sample.csproj` (the version that
was just packed). One source of truth, imported where needed.

### The dependency floor is chosen, not tracked

`src/Directory.Packages.props` pins `DataNet.Text` to a version written out in
full, deliberately *not* `$(DataNetTextVersion)`. It answers a different question:
the minimum `DataNet.Text` a consumer of `DataNet.Fuzzy` must take. Raising it is
a semver decision, not a side effect of `DataNet.Text` moving.

That decoupling is also what keeps the build honest on a fresh clone. Had the
floor tracked `DataNet.Text`'s current version, every checkout where that version
is not yet published would fail to restore until someone ran `dotnet pack` into a
local feed first — the chicken-and-egg this arrangement is otherwise prone to.
Because the floor always names something already on nuget.org,
`git clone && dotnet build` works with no pack step at all.

The cost is real and is the point: when `DataNet.Fuzzy` needs new `DataNet.Text`
API, `DataNet.Text` must be released first, and only then can the floor rise.
Two packages that release independently cannot also be edited as one.

### The developer loop, and the trap it must not fall into

`DataNet.Fuzzy.csproj` carries both references under a condition:

```xml
<ItemGroup Condition="'$(DataNetUseProjectRefs)' == 'true'">
  <ProjectReference Include="../DataNet.Text/DataNet.Text.csproj" />
</ItemGroup>
<ItemGroup Condition="'$(DataNetUseProjectRefs)' != 'true'">
  <PackageReference Include="DataNet.Text" />
</ItemGroup>
```

**The default — property unset — is the path CI packs and ships.** ADR 0009 made
this point about the sample: inside the solution, `ProjectReference` resolution
"would quietly satisfy the references and the sample would prove nothing while
appearing to work." The stakes are higher here, because the quiet path is also
the convenient one.

Three things keep it from becoming the accidental default:

- The property is only ever set by a person, as an environment variable.
  MSBuild reads environment variables as properties, so one `export` covers
  `dotnet build`, `dotnet test` and the IDE — no flag to remember per command,
  which is what makes the loop survivable. A workflow requiring a
  pack-and-restore cycle per edit would be abandoned within a week.
- The build prints a high-importance message whenever it is on.
- CI asserts the shipped path directly, by evaluating MSBuild rather than
  grepping. A text search for `<ProjectReference` cannot distinguish the two
  paths — it matches the conditional group either way — so the check asks
  MSBuild what the project actually resolved:
  `dotnet msbuild src/DataNet.Fuzzy -getItem:ProjectReference` must come back
  empty.

The `.nuspec` check is a second, independent net under the same failure. Both
paths do produce a `<dependency>` element on `DataNet.Text`, so the *id* cannot
tell them apart — but the version can, and they differ: a `PackageReference`
emits the floor from `src/Directory.Packages.props`, while a `ProjectReference`
emits `DataNet.Text`'s own current version, which the floor is deliberately
decoupled from. `tools/check_nuspec_dependencies.py` therefore asserts ranges as
well as ids, and a package built with the escape hatch left on fails it —
verified by packing with `DataNetUseProjectRefs=true`, which emits
`DataNet.Text 0.3.0` where the floor says `0.2.0`.

Note its one blind spot, so nobody mistakes it for the primary guard: the two
paths are distinguishable only while the floor and `DataNet.Text`'s own version
differ. They are equal on the release commit that raises the floor to a
just-published version, and on that commit this check would pass either way. The
MSBuild-evaluated check above has no such window, which is why it, and not this
one, is the guard that matters.

### `SetTargetFramework` does not cross a `PackageReference`

This one was found by measurement, not by reading, and it is the sharpest edge in
the whole change.

The `*.NetStandard.Tests` mirrors replay a suite against the `netstandard2.0`
build by pinning `ProjectReference … SetTargetFramework="TargetFramework=netstandard2.0"`.
That pin does not travel through a package: NuGet resolves package assets against
the *consuming* project's framework, which for these test projects is `net10.0`.
After the migration, `DataNet.Fuzzy.NetStandard.Tests` was running the
netstandard2.0 `DataNet.Fuzzy` against the **net10.0** `DataNet.Text` — half a
mirror — and every test stayed green, because `NetStandardAssemblyGuardTests` only
ever inspected the `DataNet.Fuzzy` assembly. Precisely the false confidence that
guard exists to prevent.

The fix: `DataNet.Fuzzy.NetStandard.Tests` names `DataNet.Text` in an explicit
pinned `ProjectReference` of its own (tests → src references are unaffected by
this ADR and stay `ProjectReference`), and the guard gained a second assertion
covering it. Removing the pin now fails that assertion with
`Expected ".NETStandard,Version=v2.0" / Actual ".NETCoreApp,Version=v10.0"` —
verified, not assumed.

`bench/DataNet.NetStandard.Benchmarks` was already immune, and by luck rather
than design: it names all three projects directly, and **a direct
`ProjectReference` silently takes precedence over a `PackageReference` of the
same id**. No warning is emitted. That precedence is load-bearing in two places
now, so it is written down here rather than left to be rediscovered.

### Tags and release order

Tags become `<PackageId>/v<Version>` — `DataNet.Fuzzy/v0.3.0`. `release.yml`
triggers on `DataNet.*/v*`, parses both halves out of `GITHUB_REF_NAME`, and packs
and pushes that package alone.

**The umbrella `v*` tag is retired.** There is no repository-wide version left for
it to designate, and "release everything at its current version" is a batch
operation better expressed as three tags than as one ambiguous name.

The tag no longer *sets* the version; it says which declared version to release.
The workflow compares it against `src/<Package>/Version.props` and refuses the job
on a mismatch. This strengthens the existing discipline rather than relaxing it:
the previous workflows kept the tag in the environment because a ref name is
untrusted input to a job that can push packages. Now nothing derived from the tag
reaches `dotnet pack` at all — `-p:Version` is gone from every workflow.

Release order follows from the floor being a published version: packing
`DataNet.Fuzzy` only ever needs a `DataNet.Text` that already exists, so no job
sequencing is required. It matters only when raising the floor, and there the
rule is the ordinary one — publish `DataNet.Text`, then raise the floor, then
release `DataNet.Fuzzy`.

## Consequences

- **A change to `DataNet.Text` is not seen by `DataNet.Fuzzy` until it is
  published.** `DataNet.Fuzzy.Tests` therefore exercises `DataNet.Fuzzy` compiled
  against the released `DataNet.Text`, not the one in the working tree. That is
  the intended semantics — it is what a consumer gets — but it means a
  cross-package change is two pull requests and a release, not one branch.
  `DataNetUseProjectRefs=true` covers the editing, never the merging.
- **`CHANGELOG.md` stays one file**, with per-package version headings. The
  repository remains a monorepo; only the release cadence decouples, and a single
  chronological file still reads better than three.
- **The sample needs one version property per package**, and its feed had to be
  pinned by identity to keep working. Reading three `Version.props` instead of one
  `$(Version)` was the easy half. The half that was nearly missed: packing
  `DataNet.Fuzzy` now restores `DataNet.Text` from nuget.org, and the global
  packages folder is consulted ahead of every source — so once a package is
  declared at a version that is also published, the sample resolves the *released*
  assembly and ADR 0009's guarantee inverts silently, validating what already
  shipped while appearing to validate what is about to. Per-package versioning
  makes this the normal case rather than a fluke, because a package that did not
  change keeps its published version indefinitely instead of being swept along by
  a repository-wide bump. Two things restore the guarantee: `packageSourceMapping`
  in `samples/NuGet.config` confines `DataNet.*` to the local feed, and the CI job
  gives the pack and the sample their own `NUGET_PACKAGES` directories, because no
  mapping can override the global folder. Keeping a declared version off the feed
  is the third, and `tools/check_version_floor.py` is where that is enforced.
- **`dotnet pack` output must be checked, not assumed.** A package's dependency
  graph is a build output nobody writes down. `tools/check_nuspec_dependencies.py`
  is where it is now written down, and it runs in CI and in both release jobs.
- Splitting the repository remains out of scope. This buys decoupled cadence and
  a build graph that matches the release graph — nothing more.
