# #140 — Branchless partitioning in the median's selection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Goal:** Take the mispredicted branch out of the loop that carries 65% of `MedianAbsoluteError`'s cost —
after proving the branch is the cost, and only if the result clears a bar written before the number.

**Architecture:** `WeightedPercentile.Partition` is a Lomuto scheme with one data-dependent comparison per
element. The branchless form swaps unconditionally and advances the store index by the comparison, which is
three lines and no intrinsics. The introselect around it, the `Array.Sort` fallbacks and the weighted path
do not change. The proof that nothing else changed is that not one oracle byte moves.

**Tech Stack:** C# (`net10.0` + `netstandard2.0`), xunit, the cross-language metrics harness
(`compare-metrics`).

**Spec:** `docs/superpowers/specs/2026-08-13_0140_the-medians-partitioning-branches-on-every-element.md`

## Global Constraints

- Everything in English — code, comments, commit messages, PR body. Commit messages carry no
  `feat:`/`fix:` prefix and no process prefix such as `Fix round 1:`.
- Branch `perf/140-branchless-partition`, based on `main` at `c457d98`. Never commit to `main`. Do not push
  or open a pull request without asking.
- **No absolute machine path in anything committed** — `tools/check_machine_paths.py` enforces it, and it
  now catches Windows and UNC shapes too.
- Warnings are errors repository-wide, and nine extra `csharpsquid` rules are enforced. **`dotnet build` is
  incremental: without `--no-incremental` no analyzer diagnostic is produced at all.**
- `src/` multi-targets `netstandard2.0` and `net10.0`. This change is ordinary arithmetic and must compile
  to the same source on both — **no `#if`**. Every test file is linked into the mirrored
  `*.NetStandard.Tests` project, so each new test counts **twice**.
- `dotnet format DataNet.slnx --verify-no-changes` runs **once**, in the final task.
- Read the pass/fail **counts** of every run. Baseline on this branch: **3 051 passing, 0 failed** across
  eight assemblies.
- Never write `echo "exit=$?"` after a pipeline — redirect to a file and check separately.
- **A comment asserting what the hardware, the JIT or an algorithm does is a claim, and a false one is a
  defect** (#134). The invariant behind the unconditional swap must be stated so a reader can check it, not
  asserted.
- A `perf/` change carries before/after numbers and names the machine. This one is measured on an
  **Intel i7-4770S, four physical cores**, on a desktop that is never idle — hence the control and the
  interleaving.

## What is already measured, and must not be re-derived

| Fact | Value |
| --- | --- |
| `MedianAbsoluteError` phases at n = 1 000 000, unweighted | alloc **0.6 ms**, fill **2.6 ms**, `QuickSelect` **10.8 ms**, rest ~2 ms |
| `median_ae` in the harness, same shape | **15.4 ms**, against numpy's 15.7 |
| Control stability across four interleaved campaigns | **2.4%** |
| Parallelism, measured and rejected | 1.56× at 4 workers, 1.65× at 8, and negative without `unsafe` |

## File Structure

| File | Responsibility |
| --- | --- |
| `src/DataNet.Metrics/Internal/WeightedPercentile.cs` | `Partition`'s inner loop becomes branchless; nothing else in the file moves. |
| `tests/DataNet.Metrics.Tests/WeightedPercentileMedianTests.cs` | The shapes a partition scheme gets wrong, which the suite does not currently state. |
| `docs/guides/performance.md` | The phase decomposition and the before/after. |

---

### Task 1: Prove the branch is the cost, without the experiment that flatters itself

**Files:**

- Create: `/tmp/140-diag/` (scratch, never committed)

**Depends on:** nothing.

**Produces:** the number that decides whether Tasks 2-4 happen at all.

**The trap this task exists to avoid.** The natural experiment — run `QuickSelect` on random data, then on
sorted data — is **confounded**. On sorted input, median-of-three lands on the true median, so one
partition pass suffices where random data needs several. Total time would collapse because the *number of
passes* collapsed, and the branch would take credit it had not earned.

So count **element touches** and compare **nanoseconds per touch**.

- [ ] **Step 1: Copy the algorithm into a scratch project, with a counter**

`WeightedPercentile` is `internal`, so a scratch project cannot call it. Copy `QuickSelect`, `Partition`,
`Swap` and `FloorLog2` verbatim from `src/DataNet.Metrics/Internal/WeightedPercentile.cs` into
`/tmp/140-diag/Program.cs`, and add one `static long touches;` incremented once per iteration of
`Partition`'s inner loop. Copying rather than paraphrasing matters: a diagnosis of code you rewrote is a
diagnosis of the rewrite.

```bash
mkdir -p /tmp/140-diag && cd /tmp/140-diag
cat > diag.csproj <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <Optimize>true</Optimize>
  </PropertyGroup>
</Project>
EOF
```

- [ ] **Step 2: Measure three distributions, per touch**

Three arrays of 1 000 000 doubles, each selected at `k = n/2`, each repeated enough times to be stable
(five repeats, report the median):

1. **random** — `new Random(20260813)` doubles, the shape the corpus has;
2. **sorted ascending** — where the comparison is predictable;
3. **two distinct values** — `i % 2 == 0 ? 0.0 : 1.0`, where the comparison is unpredictable *and* the
   pivot is degenerate.

Rebuild the array before every run: `QuickSelect` permutes in place, so a second run on the same array
measures different data.

Report, for each: total ms, `touches`, and **ns per touch**. The third case matters because if the branch
is the cost, its per-touch figure should sit with random rather than with sorted.

- [ ] **Step 3: Decide, and say which way**

- **Per-touch cost on sorted is far below random** (the spec expects roughly half or better) → the branch
  is the cost, and Tasks 2-4 proceed.
- **Per-touch cost is similar across all three** → the hypothesis is wrong. **Stop.** Report the numbers,
  and this lot closes on the finding — the issue gets the measurement and the code keeps its readable
  partition.

Nothing is committed by this task. Remove `/tmp/140-diag` when done.

---

### Task 2: The shapes a partition scheme gets wrong

**Files:**

- Modify: `tests/DataNet.Metrics.Tests/WeightedPercentileMedianTests.cs`

**Depends on:** Task 1 (a positive diagnosis).

**Interfaces:**

- Consumes nothing. Produces the tests Task 3 must not break.

These go in **before** the change, on the current code, and must pass on it. They are not a red-green
cycle: they are the net that catches an off-by-one in the new index arithmetic, and a net woven after the
fall catches nothing.

- [ ] **Step 1: Write them**

Read the file first and follow its idiom — how it reaches the metric, whether it calls
`MedianAbsoluteError.Score` or the internal helper, and how it names cases. The shapes:

```csharp
    /// <summary>
    /// The inputs a partition scheme gets wrong, on a size large enough to pass the
    /// insertion cutoff and exercise the introselect loop rather than the sort
    /// fallback. Written against the branchy partition and expected to pass there:
    /// they exist to catch what a rewrite of the index arithmetic would break, and
    /// a test added after a change cannot do that.
    /// </summary>
    [Theory]
    [InlineData("all equal")]
    [InlineData("already sorted")]
    [InlineData("reverse sorted")]
    [InlineData("two distinct values")]
    [InlineData("organ pipe")]
    public void The_median_is_right_on_the_shapes_that_break_a_partition(string shape)
    {
        const int Samples = 5_000;
        double[] yTrue = new double[Samples];
        double[] yPred = new double[Samples];
        for (int i = 0; i < Samples; i++)
        {
            double residual = shape switch
            {
                "all equal" => 3.0,
                "already sorted" => i,
                "reverse sorted" => Samples - i,
                "two distinct values" => i % 2 == 0 ? 0.0 : 1.0,
                "organ pipe" => i < Samples / 2 ? i : Samples - i,
                _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "no such shape"),
            };
            yTrue[i] = residual;
            yPred[i] = 0.0;
        }

        double actual = MedianAbsoluteError.Score(yTrue, yPred);

        Assert.Equal(ExpectedMedian(yTrue), actual, 12);
    }

    /// <summary>
    /// The median by the definition, computed by sorting a copy — independent of the
    /// selection under test, which is the point.
    /// </summary>
    private static double ExpectedMedian(double[] residuals)
    {
        double[] sorted = (double[])residuals.Clone();
        Array.Sort(sorted);
        int n = sorted.Length;
        return n % 2 == 1 ? sorted[n / 2] : (sorted[(n / 2) - 1] + sorted[n / 2]) / 2.0;
    }
```

`ExpectedMedian` sorts rather than selects on purpose: a reference that used the same selection would agree
with a broken partition.

- [ ] **Step 2: Run them against the unchanged code**

```bash
dotnet test DataNet.slnx -c Release --filter "FullyQualifiedName~The_median_is_right_on_the_shapes" > /tmp/140-t2.log 2>&1
echo "test=$?"
grep -E "^Réussi!|^Échoué!" /tmp/140-t2.log
```

Expected: **10 passing** — five shapes × two mirrored projects — on code nobody has touched yet. A failure
here is a defect in the *current* partition and a much bigger finding than this lot expected: stop and
report it.

- [ ] **Step 3: Commit**

```bash
git add tests/DataNet.Metrics.Tests/WeightedPercentileMedianTests.cs
git commit -m "Pin the median on the shapes a partition scheme gets wrong"
```

---

### Task 3: The branchless partition

**Files:**

- Modify: `src/DataNet.Metrics/Internal/WeightedPercentile.cs` (`Partition`'s inner loop only)

**Depends on:** Tasks 1 and 2.

- [ ] **Step 1: Replace the loop, and nothing around it**

The current inner loop:

```csharp
        for (int i = from; i < to; i++)
        {
            if (values[i] < pivot)
            {
                Swap(values, i, storeIndex);
                storeIndex++;
            }
        }
```

becomes:

```csharp
        for (int i = from; i < to; i++)
        {
            // Unconditional swap, conditional advance. This looks wrong and is not:
            // storeIndex always points at the first slot not yet known to hold a
            // value below the pivot, so when values[i] is not below it either, the
            // two positions hold interchangeable values and the swap is a no-op in
            // meaning if not in memory. The comparison then advances the index by
            // one or zero, which the JIT emits as a setcc rather than a branch --
            // and that branch is the point: it is taken about half the time on the
            // data this metric sees, which is the worst case for a predictor.
            double value = values[i];
            values[i] = values[storeIndex];
            values[storeIndex] = value;
            storeIndex += value < pivot ? 1 : 0;
        }
```

The median-of-three above it, the final `Swap(values, storeIndex, to)`, the introselect budget, the
`InsertionCutoff` fallback and the weighted path all stay exactly as they are. If the diff shows anything
else, narrow it.

- [ ] **Step 2: The whole suite, and the corpora**

```bash
dotnet build DataNet.slnx -c Release --no-incremental > /tmp/140-t3-b.log 2>&1; echo "build=$?"; tail -3 /tmp/140-t3-b.log
dotnet test DataNet.slnx -c Release > /tmp/140-t3-t.log 2>&1; echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/140-t3-t.log
```

Expected: 0 warnings and **3 061 passing** (3 051 + Task 2's ten). Every median in every regression corpus
is already pinned against scikit-learn, so this run is the real check: a partition that no longer
partitions moves one of them.

- [ ] **Step 3: Commit**

```bash
git add src/DataNet.Metrics/Internal/WeightedPercentile.cs
git commit -m "Swap unconditionally, advance conditionally, and stop mispredicting"
```

---

### Task 4: Measure it, and apply the bar

**Files:**

- Modify: `docs/guides/performance.md`
- Possibly revert: `src/DataNet.Metrics/Internal/WeightedPercentile.cs`

**Depends on:** Task 3.

**The bar, from the spec and fixed before the number: 20% on `median_ae` at n = 1 000 000, or the change is
reverted.**

- [ ] **Step 1: Measure, interleaved, in one window**

Build a baseline worktree at the merge base, copy this branch's corpus into it so both sides measure the
same data, and alternate:

```bash
cd <repo>
git worktree add /tmp/140-baseline $(git merge-base origin/main HEAD)
cp -r bench/corpus/metrics/. /tmp/140-baseline/bench/corpus/metrics/
dotnet build /tmp/140-baseline/bench/DataNet.Text.Benchmarks -c Release > /tmp/140-basebuild.log 2>&1
uptime
for round in 1 2; do
  dotnet run -c Release --project bench/DataNet.Text.Benchmarks --no-build -- compare-metrics > /tmp/140-after-$round.log 2>&1
  cp bench/results/csharp-metrics.json /tmp/140-after-$round.json
  dotnet run -c Release --project /tmp/140-baseline/bench/DataNet.Text.Benchmarks --no-build -- compare-metrics > /tmp/140-before-$round.log 2>&1
  cp /tmp/140-baseline/bench/results/csharp-metrics.json /tmp/140-before-$round.json
done
uptime
```

**Copy the result file after every campaign** — the next run overwrites it, and that is how an earlier
measurement on this repository lost three campaigns.

`mse` and `mae` are the control here: this change cannot touch them, so if they move more than a couple of
percent between the two sides, the window is contaminated and the round is void.

- [ ] **Step 2: Apply the bar, and say which way you went**

Compute `median_ae` at n = 1 000 000, after against before, as the median of the two rounds.

- **≥ 20% faster** → keep it, and write the guide.
- **< 20%** → `git revert` Task 3's commit, keep Task 2's tests (they are worth having either way), and
  amend the spec's D3 with the measured figure. Reverting is not a failure: the spec says what the change
  costs in readability, and a change that does not pay for that is one this repository does not take —
  #127 reverted its branchless 2Sum on exactly this rule.

- [ ] **Step 3: Write the guide section**

Extend the regression-metrics section rather than starting a rival one. It must carry the phase
decomposition — alloc 0.6 ms, fill 2.6 ms, select 10.8 ms — because that is what explains why this lever
and not another, the before and after per round, the machine, and the load at both ends. Say plainly that
`mse` and `mae` served as controls and by how much they moved.

- [ ] **Step 4: Remove the worktree and commit**

```bash
git worktree remove /tmp/140-baseline --force
git add docs/guides/performance.md src/DataNet.Metrics/Internal/WeightedPercentile.cs
git commit -m "Publish what the branchless partition bought"
```

---

### Task 5: Final verification

**Depends on:** Tasks 1-4. Nothing is committed here unless a gate fails and is fixed.

- [ ] **Step 1: Every gate**

```bash
cd <repo>
git status --porcelain                                                       # empty
dotnet build DataNet.slnx -c Release --no-incremental > /tmp/140-fv-b.log 2>&1; echo "build=$?"; tail -3 /tmp/140-fv-b.log
dotnet format DataNet.slnx --verify-no-changes > /tmp/140-fv-f.log 2>&1;      echo "format=$?"
dotnet test DataNet.slnx -c Release > /tmp/140-fv-t.log 2>&1;                 echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/140-fv-t.log
python3 tools/check_version_floor.py > /tmp/140-fv-v.log 2>&1;                echo "floor=$?"
python3 tools/check_machine_paths.py > /tmp/140-fv-p.log 2>&1;                echo "paths=$?"
.venv-oracles/bin/python -m pytest tools/tests -q > /tmp/140-fv-py.log 2>&1;  echo "pytest=$?"; tail -2 /tmp/140-fv-py.log
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" \
  "tools/README.md" "bench/README.md" > /tmp/140-fv-md.log 2>&1;              echo "markdownlint=$?"
```

All 0, 0 warnings, and the eight per-assembly counts read and stated.

- [ ] **Step 2: The oracle drift gate**

```bash
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py > /tmp/140-fv-gen.log 2>&1
echo "generate=$?"
cd <repo> && git status --porcelain tests/oracles/
```

Expected: empty. This branch changes an algorithm that feeds no corpus, so anything here is the known
flakiness — regenerate once before reporting it.

- [ ] **Step 3: Stop and report**

Do not push and do not open a pull request. Report Task 1's per-touch figures, Task 4's before/after with
its controls, which way the bar went, and let the user decide.

---

## Self-Review

**Spec coverage.** D1 → Task 1, including the confound it exists to avoid. D2 → Task 3. D3 → Task 4 Step 2,
where the bar is applied in both directions. D4 → Task 2 for the shapes, and Task 3 Step 2 plus Task 5 Step
2 for "not one byte moves". Documentation → Task 4 Step 3. Out of scope — parallelism, the weighted sort,
`ArrayPool` — has no task, deliberately.

**Placeholders.** Task 4 Step 2 branches on a number Task 1 and Task 4 measure, and states what to do in
both directions. `<repo>` in Tasks 4 and 5 is a path only the executing session knows, and writing the real
one into a file is what `tools/check_machine_paths.py` refuses.

**Type consistency.** `Partition`, `QuickSelect`, `Swap`, `FloorLog2` and `InsertionCutoff` are the names in
`WeightedPercentile.cs` today. `MedianAbsoluteError.Score(double[], double[])` is what Task 2 calls, and
Task 2 says to check the file's own idiom before writing the call. `ExpectedMedian` is defined in Task 2 and
used only there.
