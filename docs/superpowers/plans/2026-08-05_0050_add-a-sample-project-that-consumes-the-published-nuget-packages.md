# #50 A sample that consumes the packages — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A console sample that installs the three packages the way a consumer would and exercises one thing per lot — so packaging defects fail CI instead of reaching nuget.org.

**Architecture:** `samples/DataNet.Sample` referencing `DataNet.Text`, `DataNet.Embeddings` and `DataNet.Fuzzy` by `PackageReference`, restored from a local folder fed by `dotnet pack` via `samples/NuGet.config`. Deliberately **outside** `DataNet.slnx`, because inside it `ProjectReference` resolution would satisfy the references and the gate would prove nothing. Built and run in CI, packing first.

**Tech Stack:** .NET console app, NuGet local feed, `NuGet.config`, GitHub Actions.

**Spec:** `2026-08-05_0050_add-a-sample-project-that-consumes-the-published-nuget-packages.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `feat/50-package-sample`. Never commit to `main`.
- **Never add the sample to `DataNet.slnx`.** This is the constraint the whole
  gate rests on, and breaking it produces a green, meaningless check.
- **`PackageReference` only.** A `ProjectReference` anywhere in the sample voids
  the exercise.
- **No library change.** If the sample exposes a packaging defect, that is the
  gate working — report it, fix it deliberately.
- **No model weights** (ADR 0003). The embeddings section uses the tokenizer only.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net
SCRATCH=/tmp/sample-gate

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

### Task 1: Prove the gap before building the gate

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the reason this is worth a project rather than a paragraph in the
README.

- [x] **Step 1: Confirm nothing consumes a package today**

```bash
grep -rn "PackageReference Include=\"DataNet" --include='*.csproj' . || echo "NOTHING CONSUMES THE PACKAGES"
```

- [x] **Step 2: Confirm packing is the only exercise packaging gets**

```bash
grep -rn "dotnet pack" .github/workflows/ci.yml
```

A package can pack cleanly and be broken for a consumer: an unreachable public
type, a missing `netstandard2.0` dependency group, an XML doc file that does not
ship. None of those fails anything today.

---

### Task 2: The project, restoring from a local feed

**Files:**

- Create: `samples/DataNet.Sample/DataNet.Sample.csproj`
- Create: `samples/NuGet.config`

**Depends on:** Task 1.

- [x] **Step 1: `NuGet.config` mapping `DataNet.*` to the local folder**

Everything else to nuget.org. The mapping is what guarantees the sample's restore
can only succeed from `./artifacts`.

- [x] **Step 2: Reference the three packages by version, bound to `$(Version)`**

The root `Directory.Build.props` applies to `samples/` too, so the reference
tracks what `pack` just produced rather than pinning a number that goes stale at
the next release.

- [x] **Step 3: Verify the sample is not in the solution**

```bash
grep -c "samples" DataNet.slnx || echo "NOT IN SOLUTION — correct"
```

Expected: no entry. Inside the solution, `ProjectReference` resolution would
quietly satisfy the references and the sample would prove nothing while appearing
to work.

- [x] **Step 4: Prove the restore actually comes from the feed**

```bash
pack_feed
rm -rf "$SCRATCH/sample"
NUGET_PACKAGES="$SCRATCH/sample" dotnet restore samples/DataNet.Sample -v normal 2>&1 | grep -i "artifacts\|nuget.org" | head
```

Expected: the `DataNet.*` packages resolving from `./artifacts`.

**Isolate `NUGET_PACKAGES`.** The global packages folder is consulted ahead of
every source, so a published package sitting in it wins over the one just built
whenever the two share a version — and the gate then validates the wrong assembly.

---

### Task 3: One thing per lot

**Files:**

- Create: `samples/DataNet.Sample/Program.cs`

**Depends on:** Task 2.

- [x] **Step 1: Print the resolved target framework first**

```text
DataNet.Text    : .NETCoreApp,Version=v10.0
```

This is what makes a resolution failure visible rather than something to infer
from a stack trace.

- [x] **Step 2: The five sections**

- distances — `Levenshtein`, `JaroWinkler`
- vectorization — `TfidfVectorizer` over a few documents, printing the shape and
  non-zero count
- stemming — all six Snowball languages
- fuzzy — `Process.ExtractOne`
- embeddings — **tokenizer only**

- [x] **Step 3: Say why embeddings stop at the tokenizer**

The ONNX path needs a model that is deliberately not committed. Print or comment
the reason; a silently skipped section reads as an oversight.

- [x] **Step 4: Run it**

```bash
pack_feed && run_sample
```

Expected: one line per lot and a final `OK`. A sample that prints nothing
meaningful cannot be reviewed.

---

### Task 4: CI, so it cannot rot

**Files:**

- Modify: `.github/workflows/ci.yml`

**Depends on:** Task 3.

- [x] **Step 1: A `Sample consumes the packages` job — pack, then run**

Packing first is the point: the gate tests what is *about* to ship, not what
already shipped.

- [x] **Step 2: Separate `NUGET_PACKAGES` for the two steps**

Packing `DataNet.Fuzzy` restores `DataNet.Text` from nuget.org, and that
extraction must not be visible to the sample. Separate folders make the two
restores independent.

- [x] **Step 3: Build *and* run**

Building resolves the packages as a consumer would, so a missing dependency group
or an unreachable public type fails the build. Running proves the code works once
resolved. Both are needed; neither is sufficient.

---

### Task 5: Record the decision, then gate

**Files:**

- Create: `docs/decisions/0009-sample-consumes-a-local-feed.md`
- Modify: `README.md`, `CHANGELOG.md`

**Depends on:** Task 4.

- [x] **Step 1: ADR 0009**

The local-feed choice and its trade-off: nuget.org would be more honest as
documentation and useless as a gate, because it can only fail once a broken
package is already public. Record the `NUGET_PACKAGES` isolation requirement —
it is the part that will be forgotten.

- [x] **Step 2: README links it from getting started**

The sample is the runnable version of the quickstart; say so where a reader is
looking for one.

- [x] **Step 3: State the limit**

The sample covers one thing per lot, **not every public type**. That is a real
gap; name it and open the follow-up rather than letting the gate look broader than
it is.

- [x] **Step 4: Full gate**

```bash
dotnet build -c Release && dotnet test -c Release 2>&1 | tail -3
pack_feed && run_sample
dotnet format --verify-no-changes
npx --yes markdownlint-cli2 "**/*.md" "#node_modules"
grep -c "samples" DataNet.slnx || echo "NOT IN SOLUTION — correct"
```

- [x] **Step 5: Commit**

```bash
git add samples/ docs/decisions/0009-sample-consumes-a-local-feed.md \
        .github/workflows/ci.yml README.md CHANGELOG.md
git commit -m "Add a sample that consumes the published packages"
```
