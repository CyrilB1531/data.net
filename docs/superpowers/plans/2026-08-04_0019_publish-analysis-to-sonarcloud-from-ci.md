# #19 Publish analysis to SonarQube Cloud — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish analysis and coverage to SonarQube Cloud on pushes to `main` and on pull requests, with the coverage path actually working — and with everything verifiable locally verified before the first CI run.

**Architecture:** A `SonarQube Cloud` workflow wrapping the build as `begin` → `build` → `end`, because the .NET path analyses by observing the compilation. Three departures from the generated template: the .NET SDK, `ubuntu-latest`, and a fork-pull-request guard. Separately, `coverlet.collector` is referenced in the three test projects and coverage is collected in OpenCover format — it has never actually been collected.

**Tech Stack:** GitHub Actions, `dotnet-sonarscanner`, coverlet (OpenCover format), SonarQube Cloud project `CyrilB1531_data.net` / organization `cyrilb1531`.

**Spec:** `2026-08-04_0019_publish-analysis-to-sonarcloud-from-ci.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `ci/19-sonarcloud-analysis`. Never commit to `main`.
- **Do not make the quality gate a required check in this branch.** The baseline
  is unknown; requiring it now would block every pull request on pre-existing
  findings.
- **No source change.** The only non-workflow edits are the three test project
  files and the central package versions.
- Everything verifiable locally is verified locally. A workflow whose first run is
  also its first test has a feedback loop measured in minutes.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }

# Exactly what the workflow will run.
test_cov() {
  dotnet test -c Release --collect:"XPlat Code Coverage" \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
}
```

---

### Task 1: Prove coverage is broken before fixing it

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the finding that would otherwise fail every pull request from day
one.

- [ ] **Step 1: Run the collection the suite already claims to do**

```bash
dotnet test -c Release --collect:"XPlat Code Coverage" 2>&1 | grep -i "collector\|coverage"
```

Expected:

```text
Data collector 'XPlat Code Coverage' not found
```

The step **warns and the job passes** — it has always been a silent no-op.

- [ ] **Step 2: Confirm the package was never referenced**

```bash
grep -rn "coverlet" tests/ --include='*.csproj' --include='*.props'
```

Expected: nothing.

- [ ] **Step 3: Understand why this matters before the workflow exists**

The default quality gate requires coverage on new code. Publishing with no
coverage fails every pull request from the first run — and the failure would look
like a code-quality problem rather than a missing package.

---

### Task 2: Make coverage real

**Files:**

- Modify: `tests/Directory.Packages.props`
- Modify: `tests/DataNet.Text.Tests/DataNet.Text.Tests.csproj`
- Modify: `tests/DataNet.Embeddings.Tests/DataNet.Embeddings.Tests.csproj`
- Modify: `tests/DataNet.Fuzzy.Tests/DataNet.Fuzzy.Tests.csproj`

**Depends on:** Task 1.

- [ ] **Step 1: Reference `coverlet.collector` in the three test projects**

With `PrivateAssets="all"` — it is tooling, not a dependency.

- [ ] **Step 2: Collect in OpenCover, not the default**

The .NET path of SonarQube Cloud reads **OpenCover**. The default Cobertura output
is ignored and displays as 0 %, which is indistinguishable from having no tests —
so a wrong format here produces a confident, wrong number rather than an error.

- [ ] **Step 3: Prove three reports are produced**

```bash
find . -name "coverage.opencover.xml" -newermt "-5 minutes" | sort
```

Expected: exactly **three**, one per test project. That is what
`sonar.cs.opencover.reportsPaths` globs; two would mean a project is silently
uncovered.

---

### Task 3: The workflow

**Files:**

- Create: `.github/workflows/sonarcloud.yml`

**Depends on:** Task 2.

- [ ] **Step 1: Start from the generated template, then change three things**

- [ ] **Step 2: Add `actions/setup-dotnet`**

The libraries target `net10.0` and the scanner analyses whatever the build
compiles. Without the SDK the build fails, or — worse — succeeds having analysed
nothing.

- [ ] **Step 3: `ubuntu-latest`, with paths and shell adjusted**

Matching the rest of CI. Two runner families means two sets of path bugs.

- [ ] **Step 4: `begin` → `build` → `end`, in that order**

Not the generic scan action. The .NET path works by installing the Roslyn
analyzers during `begin` and observing the compilation between the two — a scan
without a build in the middle produces a green job and an empty analysis.

- [ ] **Step 5: Guard fork pull requests**

They are not given `SONAR_TOKEN`, and would **fail** rather than skip. Skip
explicitly on a missing secret.

- [ ] **Step 6: Wire the coverage glob**

`sonar.cs.opencover.reportsPaths` matching the three paths found in Task 2.

---

### Task 4: Verify what can be verified without CI

**Files:** none modified.

**Depends on:** Task 3.

- [ ] **Step 1: The YAML parses, and has the steps and triggers you think**

```bash
python3 -c "
import yaml,sys
d=yaml.safe_load(open('.github/workflows/sonarcloud.yml'))
job=list(d['jobs'].values())[0]
print('triggers:', list(d[True].keys()) if True in d else list(d['on'].keys()))
print('steps:', len(job['steps']))
for s in job['steps']: print('  -', s.get('name') or s.get('uses'))
"
```

Expected: both triggers, seven steps.

- [ ] **Step 2: Run the workflow's exact build and test commands by hand**

```bash
build_all && test_cov 2>&1 | tail -3
```

Expected: clean on both frameworks, 158/158.

- [ ] **Step 3: Documentation**

```bash
# CONTRIBUTING.md and README.md gain the job and the badge.
grep -n "SonarQube\|SonarCloud" CONTRIBUTING.md README.md
```

The job name must be quoted **exactly** — required checks are configured by name,
and a paraphrase becomes a check that never matches.

---

### Task 5: Record what only the first run can answer

**Files:** the pull request body.

**Depends on:** Task 4.

- [ ] **Step 1: Write the three unknowns down, with what each would change**

- **Do the `#pragma warning disable S…` suppressions from #7 carry over?** They
  should — the scanner runs the same SonarAnalyzer rules through Roslyn — but
  check rather than assume, and record the fallback.
- **Does multi-targeting double-count issues?** Every file now compiles twice.
- **Should the quality gate become a required check?** Not yet. Wait for a few
  runs to show the baseline is clean.

Naming an unknown is not hedging: each of these decides what the next branch does.

- [ ] **Step 2: Full gate**

```bash
build_all && dotnet test -c Release 2>&1 | tail -3
dotnet format --verify-no-changes
npx --yes markdownlint-cli2 "**/*.md" "#node_modules"
```

Expected: clean, 158/158, markdownlint 0 issues across 25 files.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "Publish analysis to SonarQube Cloud from CI"
```

- [ ] **Step 4: Read the first run, and act on it in a follow-up**

A green build is not a clean Sonar. Read the published analysis before calling
this done, and open issues for what it surfaces rather than widening this branch.
