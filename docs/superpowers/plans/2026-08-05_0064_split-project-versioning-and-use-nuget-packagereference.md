# #64 Per-package versioning and package references — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Each package declares its own version and releases on its own cadence, with the one cross-reference under `src/` going through NuGet — so the build graph matches the release graph, and a `DataNet.Fuzzy` patch stops republishing two unchanged packages.

**Architecture:** `src/<Package>/Version.props` per project holding a named property, read from three places for three different reasons. `DataNet.Fuzzy → DataNet.Text` by `PackageReference` against a published floor, with an opt-in `DataNetUseProjectRefs` dev loop that CI asserts against. Per-package tags, compared against `Version.props` before publishing.

**Tech Stack:** MSBuild, Central Package Management, NuGet, GitHub Actions, Python check scripts.

**Spec:** `2026-08-05_0064_split-project-versioning-and-use-nuget-packagereference.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `feat/64-split-versioning-nuget-refs`. Never commit to `main`.
- **`git clone && dotnet build` must work with no pack step.** The floor always
  names an already-published release.
- **CI never sets `DataNetUseProjectRefs`** and asserts the default path.
- **Nothing derived from a tag or a dispatch input reaches `dotnet pack`.** The
  repository is authoritative about its own version.
- Both frameworks build; both mirror suites pass.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build DataNet.slnx -c Release; }
test_all()  { dotnet test DataNet.slnx -c Release; }

# What the project actually resolved — not what the file says.
resolved_project_refs() {
  dotnet msbuild "$1" -getItem:ProjectReference -p:TargetFramework=net10.0 \
    | python3 -c 'import sys,json; print([i["Identity"] for i in json.load(sys.stdin)["Items"]["ProjectReference"]])'
}
```

---

### Task 1: Per-package versions

**Files:**

- Create: `src/DataNet.Text/Version.props`, `src/DataNet.Embeddings/Version.props`,
  `src/DataNet.Fuzzy/Version.props`
- Modify: the three `.csproj`, `Directory.Build.props`

**Depends on:** nothing.

- [x] **Step 1: Remove the solution-wide `<Version>`**

Keep everything genuinely shared in the root props. `<Version>` is not shared —
that is the whole issue.

- [x] **Step 2: A named property per package, not a bare `<Version>`**

Three places need the number for three different reasons: the csproj (identity),
`src/Directory.Packages.props` (the floor `DataNet.Fuzzy` depends on), and the
sample (the version just packed). One source of truth, no tooling.

- [x] **Step 3: Record why no version tool was adopted**

Nerdbank.GitVersioning, MinVer and GitVersion all **derive** a version from git
topology. Nothing here wants a derived version: the number is a deliberate
semantic statement. Put this in ADR 0012 — otherwise it gets proposed again.

- [x] **Step 4: Comment the trap in the file itself**

Never leave a declared version equal to one already on the feed. A package's
identity is id+version, so a collision makes two different assemblies answer to
the same number: restore serves whichever it has cached, and a duplicate push is
absorbed by the feed while the job reports success.

---

### Task 2: The cross-reference becomes a package reference

**Files:**

- Modify: `src/DataNet.Fuzzy/DataNet.Fuzzy.csproj`,
  `src/Directory.Packages.props`

**Depends on:** Task 1.

- [x] **Step 1: `PackageReference` on a published floor, written out in full**

Not tracking `$(DataNetTextVersion)`. It answers a different question — the
minimum a consumer must take — and naming an already-published release is what
keeps a fresh clone buildable with no pack step.

- [x] **Step 2: The opt-in dev loop**

`DataNetUseProjectRefs=true` flips the reference back, through a conditional
`ItemGroup`. MSBuild reads environment variables as properties, so one `export`
covers `build`, `test` and the IDE.

- [x] **Step 3: Print a high-importance message when it is on**

A build silently using a graph that will never ship is how a benchmark or a
packaging check ends up describing nothing real.

- [x] **Step 4: Prove a clean clone builds**

```bash
git clone . /tmp/cleanclone && cd /tmp/cleanclone && dotnet build DataNet.slnx -c Release
```

---

### Task 3: The check that CI can actually run

**Files:**

- Modify: `.github/workflows/ci.yml`
- Modify: `tools/check_nuspec_dependencies.py`

**Depends on:** Task 2.
**Produces:** a gate that discriminates, rather than one that matches text.

- [x] **Step 1: Reject the grep the issue asked for, and say why**

`git grep '<ProjectReference' -- 'src/**/*.csproj'` **contradicts the issue's own
§3**: the prescribed conditional `ItemGroup` contains that literal text, so the
grep fails on the sanctioned solution — and it cannot tell the shipped path from
the dev loop either way.

- [x] **Step 2: Ask MSBuild what was resolved**

```bash
resolved_project_refs src/DataNet.Fuzzy
DataNetUseProjectRefs=true resolved_project_refs src/DataNet.Fuzzy
```

Expected: `[]` by default, `['../DataNet.Text/DataNet.Text.csproj']` with the
property set. **Verify it discriminates** — a check that returns the same answer
both ways is not a check.

- [x] **Step 3: Note what the `.nuspec` check does *not* prove**

Both paths emit the same `<dependency>`, so it cannot tell you which was taken. It
guards an unexpected or vanished dependency instead. Say so, or it will be cited
for the wrong guarantee.

- [x] **Step 4: Split it into parse, check and report**

---

### Task 4: The mirror that was only half a mirror

**Files:**

- Modify: `tests/DataNet.Fuzzy.NetStandard.Tests/*`

**Depends on:** Task 2.
**Produces:** the fix for the silent regression this migration introduced.

- [x] **Step 1: Check what the mirror actually resolved**

NuGet resolves package assets against the **consuming** project's framework, and
`SetTargetFramework` does **not** cross a `PackageReference`.

Expected finding: after the migration, `DataNet.Fuzzy.NetStandard.Tests` replays
the `netstandard2.0` `DataNet.Fuzzy` against the **net10.0** `DataNet.Text` — half
a mirror, every test green, because the guard only inspected `DataNet.Fuzzy`.

This is exactly the false confidence that guard exists to prevent.

- [x] **Step 2: Pin `DataNet.Text` in the mirror, and widen the guard to cover it**

- [x] **Step 3: Verify by removing the pin**

```text
Expected ".NETStandard,Version=v2.0" / Actual ".NETCoreApp,Version=v10.0"
```

- [x] **Step 4: Record why the benchmark mirror still works**

**A direct `ProjectReference` silently outranks a `PackageReference` of the same
id**, with no warning. That is the only reason
`bench/DataNet.NetStandard.Benchmarks` still resolves `netstandard2.0` assemblies.
Load-bearing in two places now — write it down.

---

### Task 5: Release, with the repository authoritative

**Files:**

- Modify: `.github/workflows/release.yml`,
  `.github/workflows/release-nuget-org.yml`
- Create: `tools/check_version_floor.py`

**Depends on:** Task 3.

- [x] **Step 1: Remove `-p:Version` from every workflow**

Further than the issue asked. The tag chooses *which* release to cut; it does not
set the number.

- [x] **Step 2: Compare the tag against `Version.props` and refuse a mismatch**

```bash
# Guard check: a bogus tag against the declared version must fail the job.
```

Expected: `9.9.9` refused against a declared `0.2.1`.

- [x] **Step 3: Per-package tags; retire the umbrella `v*`**

`DataNet.Fuzzy/v0.2.1`.

- [x] **Step 4: `check_version_floor.py`**

Three files hold a `DataNet.Text` version number for three different reasons, and
**MSBuild is happy when they disagree**. Offline and instant; CI adds
`--check-feed` to prove the floor is actually published.

---

### Task 6: Exercise the split, do not merely enable it

**Files:**

- Modify: `samples/DataNet.Sample/DataNet.Sample.csproj`, `samples/NuGet.config`
- Modify: `docs/decisions/0009-sample-consumes-a-local-feed.md`
- Create: `docs/decisions/0012-per-package-versioning.md`
- Modify: `CONTRIBUTING.md`, `README.md`, `CHANGELOG.md`

**Depends on:** Task 5.

- [x] **Step 1: Ship a genuine mix**

`DataNet.Fuzzy 0.2.1`, `DataNet.Text 0.2.0`, `DataNet.Embeddings 0.2.0` — and the
sample builds and runs against it. A capability never used is a capability that
does not work.

- [x] **Step 2: The sample references the versions just packed**

Through the named properties, so it tracks `pack` rather than pinning numbers that
go stale.

- [x] **Step 3: ADR 0012, and an amendment note on ADR 0009**

- [x] **Step 4: `CONTRIBUTING.md` — the two-package working loop**

Including the two things to keep straight: it is a local loop and not a merge
strategy, and it must be unset before measuring anything.

- [x] **Step 5: Full gate**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
python3 tools/check_version_floor.py
resolved_project_refs src/DataNet.Fuzzy
```

Expected: 0 warnings; 386 passed; format clean; floor coherent; `[]`.
