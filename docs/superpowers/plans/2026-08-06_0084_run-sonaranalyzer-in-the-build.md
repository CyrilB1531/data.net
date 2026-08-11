# #84 SonarAnalyzer in the build — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A Sonar finding becomes a compile error on the machine that wrote the code, in `src/`, `tests/` and `bench/` — with the scope decided by measurement and the gate demonstrated failing.

**Architecture:** `SonarAnalyzer.CSharp` as an analyzer-only `PackageReference` in the three areas, its version pinned once in the root `Directory.Build.props` and read from three places because the areas reference packages differently. `samples/` stays out, with the reasons recorded.

**Tech Stack:** SonarAnalyzer.CSharp on Roslyn, MSBuild Central Package Management, C# (net10.0 + netstandard2.0).

**Spec:** `2026-08-06_0084_run-sonaranalyzer-in-the-build.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `chore/84-sonaranalyzer-in-build`. Never commit to `main`.
- **`PrivateAssets="all"`.** The analyzer must reach no published package.
- **The version is pinned once.** Never duplicate the number.
- **Every commit leaves `dotnet build DataNet.slnx` green.** Fix the findings
  first; turn the switch on last.
- Every suppression carries a reason in the source.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build DataNet.slnx -c Release; }
test_all()  { dotnet test DataNet.slnx -c Release; }

pack_check() {
  rm -rf ./artifacts
  for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do
    dotnet pack "$p" -c Release -o ./artifacts || return 1
  done
  python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
}
```

---

### Task 1: Measure the cost per area, before choosing the scope

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the decision the issue asked to be made deliberately.

- [x] **Step 1: Add the analyzer temporarily and count, per area**

```bash
# With the reference added to one area at a time:
build_all 2>&1 | grep -oE "warning S[0-9]+" | sort | uniq -c | sort -rn
```

Expected:

| Area | Findings |
| --- | ---: |
| `src/` | 7 |
| `tests/` | 4 |
| `bench/` | 0 |

- [x] **Step 2: Read the numbers rather than debating the principle**

**Four findings is not an arbitration**, and `bench/` was already clean. The root
props has said "warnings are errors everywhere: src, tests and bench alike" since
the beginning, and SonarCloud already reports on all three.

Scoping the local build *more narrowly than the remote gate* would recreate the
round trip in miniature, for the code that is read most often.

- [x] **Step 3: Write down why `samples/` stays out**

Outside `DataNet.slnx`; restores from a local feed so a `pack` must come first;
`DocSnippets/Generated/` already excluded from SonarCloud's analysis.

State honestly that this leaves the samples analysed only by CI. An ADR that
hides its own weakness cannot be revisited later.

---

### Task 2: Fix the eleven findings first

**Files:**

- Modify: `src/DataNet.Embeddings/Persistence/SentencePieceModelLoader.cs`,
  `Pooling/Pooling.cs`, `Search/EmbeddingIndex.cs`,
  `Tokenization/SentencePieceTokenizer.cs`, and the four in `tests/`

**Depends on:** Task 1.
**Produces:** a tree that is already clean when the switch is thrown.

- [x] **Step 1: Work them with the analyzer applied as a command-line override**

So no commit leaves the build red for anyone else.

- [x] **Step 2: Fix where the rule is right; suppress with a reason where it is not**

Same discipline as #7 and #27. "Too noisy" is not a reason.

- [x] **Step 3: Confirm zero findings under the override**

```bash
build_all 2>&1 | grep -c "warning S"
```

Expected: `0`.

---

### Task 3: Turn it on, from one pin

**Files:**

- Modify: `Directory.Build.props`
- Modify: `src/Directory.Packages.props`, `tests/Directory.Packages.props`
- Modify: `bench/Directory.Build.props`

**Depends on:** Task 2.

- [x] **Step 1: `$(DataNetSonarAnalyzerVersion)` in the root props**

One number. Comment that raising it usually surfaces new rules and is therefore
its own change.

- [x] **Step 2: Reference it from the three areas**

`src/` and `tests/` through Central Package Management; **`bench/` has none**, so
it names the version on the `PackageReference` itself — from the same property.

- [x] **Step 3: `PrivateAssets="all"` everywhere**

- [x] **Step 4: Confirm it reaches no package**

```bash
pack_check
```

An analyzer leaking into a published dependency graph is a real consumer-facing
defect.

---

### Task 4: Prove the gate fails

**Depends on:** Task 3.

- [x] **Step 1: Introduce a deliberate violation in each of the three areas**

```bash
# e.g. append commented-out code (S125) to one file per area
build_all 2>&1 | grep -E "error S125"
```

Expected: a build **error** in each area, and a non-zero exit.

- [x] **Step 2: Remove them and confirm green**

```bash
build_all && test_all 2>&1 | tail -3
```

A gate nobody has seen fail is not known to work — the standard #10 and #17 set.

---

### Task 5: Record what this changes about the workflow

**Files:**

- Create: `docs/decisions/0015-sonar-rules-in-the-build.md`
- Modify: `CONTRIBUTING.md`

**Depends on:** Task 4.

- [x] **Step 1: ADR 0015**

The scope measurement, the single pin and why, the `samples/` exclusion and its
three reasons, and the demonstration.

- [x] **Step 2: `CONTRIBUTING.md` — the rule that changes**

**Sonar findings are cleared before a commit, not after.** This changes what "the
build is green" means, so it belongs where contributors read.

- [x] **Step 3: State what the local build still cannot see**

Duplication and coverage need the server. **A green local build is not a green
quality gate** — say it, or the new gate will be over-trusted.

- [x] **Step 4: Full gate**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
pack_check
```

- [x] **Step 5: Commit**

```bash
git add -A
git commit -m "Fail on a Sonar finding before the push instead of after it"
```
