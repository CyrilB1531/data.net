# #17 Replay the suite against the `netstandard2.0` build — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Execute the existing test suite against the `netstandard2.0` assemblies, so the build shipped to .NET Framework, Mono and Unity consumers is proven rather than compiled — closing the gap ADR 0001 recorded, before 0.2.0 reaches nuget.org.

**Architecture:** Three mirror test projects that **link** the existing test sources and pin `SetTargetFramework=netstandard2.0` on the project reference. Each carries one extra test asserting the `TargetFrameworkAttribute` of the assembly under test, and that guard is verified by breaking the isolation on purpose. No workflow change: `dotnet test DataNet.slnx` covers the solution.

**Tech Stack:** xunit, MSBuild `SetTargetFramework` and linked `Compile` items.

**Spec:** `2026-08-05_0017_run-the-test-suite-against-the-netstandard2-0-build.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `test/17-netstandard-runtime-validation`. Never commit to `main`.
- **Link the test sources. Never copy them.** A copied suite drifts silently the
  first time a test is added to one side only.
- **No library change.** If a portable fallback turns out to be wrong, that is a
  finding — report it and open an issue; fixing it here would mean the branch that
  proves correctness also changed the thing it proves.
- The mirrors target `net10.0` and reference the `netstandard2.0` assemblies.
  Targeting `netstandard2.0` in the test project is wrong — it is a contract, not
  a runtime.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
test_all()  { dotnet test -c Release; }
test_ns()   { dotnet test -c Release --filter "FullyQualifiedName~NetStandard"; }
```

---

### Task 1: One mirror, proven, before the other two

**Files:**

- Create: `tests/DataNet.Text.NetStandard.Tests/DataNet.Text.NetStandard.Tests.csproj`
- Create: `tests/DataNet.Text.NetStandard.Tests/NetStandardAssemblyGuardTests.cs`
- Modify: `src/DataNet.Text/DataNet.Text.csproj`
- Modify: `DataNet.slnx`

**Depends on:** nothing.
**Produces:** the pattern the other two copy — and the guard proven to work before
it is trusted three times.

- [x] **Step 1: The project file**

`TargetFramework=net10.0`, `IsPackable=false`, its own `AssemblyName`, and
`RootNamespace=DataNet.Text.Tests` so the linked sources compile unchanged.

Reference the library with:

```xml
<ProjectReference Include="../../src/DataNet.Text/DataNet.Text.csproj"
                  SetTargetFramework="TargetFramework=netstandard2.0" />
```

Link the sources and the oracle corpora:

```xml
<Compile Include="../DataNet.Text.Tests/**/*.cs"
         Exclude="../DataNet.Text.Tests/bin/**;../DataNet.Text.Tests/obj/**"
         Link="%(RecursiveDir)%(Filename)%(Extension)" />
<None Include="../oracles/**/*.json" CopyToOutputDirectory="PreserveNewest" LinkBase="oracles" />
```

- [x] **Step 2: Comment the arrangement in the project file itself**

`netstandard2.0` is a contract, not a runtime — the tests run on net10.0, an
identical host, and only the assembly under test changes. Without that comment the
setup reads as a mistake, and someone will "fix" it.

- [x] **Step 3: `InternalsVisibleTo` for the new assembly name**

```bash
grep -n "InternalsVisibleTo" src/DataNet.Text/DataNet.Text.csproj
```

The mirror has its own name, so it needs its own entry. This will surface as a
wall of accessibility errors if forgotten.

- [x] **Step 4: The guard test**

Assert the `TargetFrameworkAttribute` of the assembly under test equals
`.NETStandard,Version=v2.0`.

- [x] **Step 5: Prove the guard fails when isolation breaks**

```bash
# temporarily remove SetTargetFramework from the ProjectReference, then:
test_ns 2>&1 | grep -A3 "Assert.Equal"
```

Expected, and this is the step that earns the guard its place:

```text
Assert.Equal() Failure: Strings differ
Expected: ".NETStandard,Version=v2.0"
Actual:   ".NETCoreApp,Version=v10.0"
```

Restore `SetTargetFramework` afterwards and confirm it passes. #10 already shipped
plausible numbers from the wrong build; the identical failure here would leave
every test green while proving nothing.

- [x] **Step 6: Run the mirror in full**

```bash
dotnet test tests/DataNet.Text.NetStandard.Tests -c Release 2>&1 | tail -3
```

Expected: 148 — 147 linked tests plus the guard. **If any oracle test fails, stop
and report it**: a portable fallback disagrees with the corpus, which is exactly
what this branch exists to find, and it is a separate fix.

---

### Task 2: The other two mirrors

**Files:**

- Create: `tests/DataNet.Embeddings.NetStandard.Tests/` (csproj + guard)
- Create: `tests/DataNet.Fuzzy.NetStandard.Tests/` (csproj + guard)
- Modify: `DataNet.slnx`

**Depends on:** Task 1.

- [x] **Step 1: Same shape, three times**

Copy the pattern exactly. Divergence between the three mirrors is pure cost.

- [x] **Step 2: Add both to `DataNet.slnx`**

- [x] **Step 3: Verify each guard the same way**

Do not skip this for mirrors two and three on the grounds that the first one
worked. `SetTargetFramework` is easy to typo and the symptom is a green suite.

- [x] **Step 4: Full solution**

```bash
build_all && test_all 2>&1 | tail -5
```

Expected:

| Suite | net10.0 | netstandard2.0 |
| --- | ---: | ---: |
| Text | 147 | 148 |
| Embeddings | 11 | 12 |
| Fuzzy | 10 | 11 |
| **Total** | **168** | **171** |

- [x] **Step 5: Confirm the scalar `Dot` path is genuinely exercised**

```bash
dotnet test -c Release --filter "FullyQualifiedName~NetStandard&FullyQualifiedName~Vector" 2>&1 | tail -3
```

`VectorMath.Dot` is the one deliberate behavioural split in the library. A mirror
suite that does not reach it has not tested the thing most likely to be wrong.

---

### Task 3: Correct the documents that state the old limitation

**Files:**

- Modify: `docs/decisions/0001-target-framework.md`
- Modify: `CHANGELOG.md`

**Depends on:** Task 2.

- [x] **Step 1: ADR 0001**

It says the `netstandard2.0` build is compile-verified but not behaviour-verified.
That is no longer true. Amend the section rather than deleting it — the history of
the gap is worth keeping, and an ADR that silently changes its own claim is not a
record.

- [x] **Step 2: `CHANGELOG.md`**

Same correction. A stale limitation is worse than none: it tells a reader to
distrust something now proven.

- [x] **Step 3: Confirm no workflow change is needed**

```bash
grep -n "dotnet test" .github/workflows/ci.yml
```

Expected: `dotnet test` against the solution, which now includes the mirrors. If a
job names projects individually, that is where the change would be — check rather
than assume.

- [x] **Step 4: Full gate**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
npx --yes markdownlint-cli2 "**/*.md" "#node_modules"
```

- [x] **Step 5: Commit**

```bash
git add -A
git commit -m "Replay the test suite against the netstandard2.0 build"
```
