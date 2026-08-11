# #10 net10 versus netstandard2.0 benchmarks — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Measure what the `netstandard2.0` build actually costs against the net10 one, with the isolation **proven** rather than intended — a run that silently measures the wrong assemblies must fail, not produce a table.

**Architecture:** A second benchmark project links the same sources and swaps only the referenced assemblies. Two changes make the claim true: an in-process toolchain, so no generated project can re-resolve the reference; and a pre-flight assertion on the loaded assemblies' `TargetFrameworkAttribute` that exits non-zero on a mismatch. `VectorMathBenchmarks` is added because `Dot` and `L2Norm` are where the builds deliberately differ.

**Tech Stack:** BenchmarkDotNet (in-process toolchain), MSBuild `SetTargetFramework`, C# (net10.0 + netstandard2.0).

**Spec:** `2026-08-04_0010_benchmark-the-net10-build-against-the-netstandard2-0-build.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `feat/10-netstandard-benchmark`. Never commit to `main`.
- **No library change.** This branch measures. If it finds something worth fixing,
  open an issue.
- **No number is quoted before Task 3 passes.** Task 3 is the isolation proof, and
  every figure produced before it is meaningless.
- Numbers are reported with the machine that produced them, per `CONTRIBUTING.md`.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
run_net()   { dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter "$1"; }
run_ns()    { dotnet run -c Release --project bench/DataNet.NetStandard.Benchmarks -- --filter "$1"; }
```

---

### Task 1: Reproduce the wrong answer first

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the failure this branch exists to prevent, seen rather than
described.

Do not skip this. The end of the branch is a number, and a number is only
trustworthy if you have seen what the untrustworthy version looks like.

- [x] **Step 1: Create the second project with `SetTargetFramework` alone**

A minimal `bench/DataNet.NetStandard.Benchmarks` that links the existing sources
and references the libraries with
`SetTargetFramework="TargetFramework=netstandard2.0"`. Default toolchain, no
assertion.

- [x] **Step 2: Run both suites and compare**

```bash
run_net '*Dot*' 2>&1 | tail -20
run_ns  '*Dot*' 2>&1 | tail -20
```

Expected: a difference of a few percent. **That is the bug.** BenchmarkDotNet's
default toolchain generates its own project, re-resolves the reference and
restores the net10 build, so both runs measure the same assemblies.

- [x] **Step 3: Write the wrong number down**

It goes in the pull request body as the thing that was nearly shipped. A 4 %
result is small, correctly signed, and looks like a JIT difference — nobody
questions it.

---

### Task 2: In-process toolchain

**Files:**

- Create: `bench/DataNet.NetStandard.Benchmarks/DataNet.NetStandard.Benchmarks.csproj`
- Create: `bench/DataNet.NetStandard.Benchmarks/Program.cs`
- Modify: `DataNet.slnx`

**Depends on:** Task 1.

- [x] **Step 1: Link the sources, do not copy them**

`<Compile Include="../DataNet.Text.Benchmarks/**/*.cs" Exclude="…/bin/**;…/obj/**" Link="…" />`.
One suite, two builds — the same device the mirror test projects use. A copy
drifts; a link cannot.

- [x] **Step 2: Configure the in-process toolchain**

So no generated project exists to re-resolve anything. This is the actual fix; the
assertion in Task 3 is what proves it worked.

- [x] **Step 3: Add both projects to `DataNet.slnx`**

- [x] **Step 4: Build**

```bash
build_all
```

---

### Task 3: Prove the isolation, and make a failure loud

**Files:**

- Modify: `bench/DataNet.NetStandard.Benchmarks/Program.cs`

**Depends on:** Task 2.
**Produces:** the difference between a measurement and a guess.

- [x] **Step 1: Read `TargetFrameworkAttribute` off the loaded assemblies**

Before any benchmark runs, print one line per library under test:

```text
// DataNet.Text: .NETStandard,Version=v2.0
// DataNet.Embeddings: .NETStandard,Version=v2.0
```

- [x] **Step 2: Exit non-zero on a mismatch**

Not a warning. An isolation failure is invisible in the numbers unless you already
know what to expect, so the run must stop rather than print a plausible table.

- [x] **Step 3: Verify the assertion fires**

```bash
# temporarily drop SetTargetFramework from the csproj, then:
run_ns '*Dot*' ; echo "exit: $?"
```

Expected: the assertion reports `.NETCoreApp` and a **non-zero exit**. Restore
`SetTargetFramework` afterwards.

A gate that has never been seen to fail is not known to work — the same argument
ADR 0015 later makes about the analyzer gate.

- [x] **Step 4: Confirm the real run prints the right thing**

```bash
run_ns '*Dot*' 2>&1 | grep "Version=v2.0"
```

---

### Task 4: Benchmark what actually differs

**Files:**

- Create: `bench/DataNet.Text.Benchmarks/VectorMathBenchmarks.cs`
- Modify: `bench/DataNet.Text.Benchmarks/DataNet.Text.Benchmarks.csproj`

**Depends on:** Task 3.

- [x] **Step 1: `Dot` and `L2Norm`, at 384 / 768 / 1024**

These are the embedding dimensions that matter, and `Dot` is the one deliberate
behavioural split from ADR 0001. Benchmarking code that is byte-identical on both
targets would average the difference away.

- [x] **Step 2: Run both suites**

```bash
run_net '*VectorMath*' 2>&1 | tail -20
run_ns  '*VectorMath*' 2>&1 | tail -20
```

- [x] **Step 3: Sanity-check the result against physics before believing it**

Expected shape, on an Intel i7-4770S with .NET 10.0.110:

| Dimension | net10 | netstandard2.0 | cost |
| --- | --- | --- | --- |
| 384 | 73.2 ns | 338.5 ns | 4.6× |
| 768 | 130.9 ns | 679.8 ns | 5.2× |
| 1024 | 163.6 ns | 912.1 ns | 5.6× |

Then check the number is achievable by the code you think ran: 912 ns for 1024
floats is ~0.89 ns per element, which is a latency-bound scalar accumulator. The
earlier 173 ns was **never physically plausible** for scalar code — that is what
gave the bug away.

Do this arithmetic explicitly. It is the check that catches the next isolation
failure, whatever form it takes.

- [x] **Step 4: Handle S2245 if it fires**

The new benchmarks seed an RNG to build vectors. Suppress with the reason:
determinism is the requirement here, not a risk. Separate commit — it is a
different concern from the measurement.

---

### Task 5: Record it so the failure is recognisable next time

**Files:**

- Modify: `bench/README.md`

**Depends on:** Task 4.

- [x] **Step 1: The numbers, and the machine that produced them**

- [x] **Step 2: How to tell if isolation breaks again**

The assertion output to expect, and the explicit warning that **a few percent is
the failure mode, not a result**. A future reader must be able to recognise 4 %
for what it is.

- [x] **Step 3: Full gate**

```bash
build_all && dotnet test -c Release 2>&1 | tail -3
dotnet format --verify-no-changes
npx --yes markdownlint-cli2 "**/*.md" "#node_modules"
```

Expected: clean on both frameworks, 168/168, format and markdownlint clean.

- [x] **Step 4: Commit**

```bash
git commit -m "Measure the netstandard2.0 build against the net10 one"
git commit -m "Suppress S2245 in the new VectorMath benchmarks"
```
