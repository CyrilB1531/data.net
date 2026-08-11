# #107 Analysis Parity — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `dotnet build` enforce every diagnostic the SonarCloud pull-request gate counts, in all four source areas, so a finding is a compile error on the machine that wrote the code.

**Architecture:** Two independent halves. `samples/` gains a `Directory.Build.props` carrying `SonarAnalyzer.CSharp`, the way `bench/` does. The root `Directory.Build.props` turns on the .NET code-quality analysers at `AnalysisMode=All`, pinned to `AnalysisLevel=10.0`. The 655 findings that follow are absorbed by seven area-wide `NoWarn` entries and 46 individual judgements. **Every commit leaves `dotnet build DataNet.slnx` green**: the 46 sites are fixed first, verified through a command-line `-p:AnalysisMode=All` override, and the switch is committed last.

**Tech Stack:** MSBuild `Directory.Build.props`, Microsoft.CodeAnalysis.NetAnalyzers (SDK 10.0.110), SonarAnalyzer.CSharp 10.20.0.135146, xunit, BenchmarkDotNet.

**Spec:** `2026-08-10-107-analysis-parity-design.md` (same directory).

## Global Constraints

- **Everything in English** — code comments, ADR, `CONTRIBUTING.md`, commit messages, PR body.
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `chore/107-analysis-parity` in `<repo>`. Never commit to `main`.
- `TreatWarningsAsErrors=true` is set repo-wide and must stay. Never verify with `-p:TreatWarningsAsErrors=false` in a final check — only while diagnosing.
- `src/` multi-targets **`netstandard2.0` and `net10.0`**. Every `src/` edit must compile on both. The test projects and `samples/` target `net10.0` only.
- The single analyzer version pin is `$(DataNetSonarAnalyzerVersion)` in the root `Directory.Build.props`. Do not duplicate the number.
- Every suppression carries a reason in the source, per `CONTRIBUTING.md` and ADR 0015. No bare `#pragma`, no bare `NoWarn`.
- `samples/` builds only against a local feed. Every samples build in this plan needs a fresh `pack` **and** an isolated `NUGET_PACKAGES`, or it judges the published packages instead of the working tree (ADR 0009).

### Correction to the spec, found while planning

The spec listed **CA1307 ×8 in `src/` as a real fix**. It cannot be: the messages ask for `string.IndexOf(char, StringComparison)` and `string.Replace(string, string?, StringComparison)`, and **neither overload exists on `netstandard2.0`**. `src/Shared/StringCompat.cs:21` is itself the netstandard2.0 polyfill. Those 8 sites move to file-scoped pragmas with that reason — the idiom `BpeTokenizer.cs:355` and `BatchEncoder.cs:215` already use for CA1845, word for word. The 1 CA1307 in `tests/` stays a real fix, because both test projects target `net10.0`.

Revised counts: **15 real fixes** (CA1305 ×9, CA1062 ×4, CA2251 ×1, CA1307 ×1 in tests) and **31 reasoned suppressions**.

### Reusable verification commands

```bash
cd <repo>
SCRATCH=/tmp/claude-49201103/-home-cyril-Documents-devs-data-net/c134d377-25c6-4da3-8dec-8ffcbffa021b/scratchpad

# The switch, as a command-line override. Used in Tasks 3-7, before it is committed.
ANALYSIS="-p:EnableNETAnalyzers=true -p:AnalysisLevel=10.0 -p:AnalysisMode=All"

# Refresh the local feed the samples consume (needed once before any samples build).
pack_feed() {
  rm -rf "$SCRATCH/pack-packages" ./artifacts
  NUGET_PACKAGES="$SCRATCH/pack-packages" bash -c '
    for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do
      dotnet pack "$p" -c Release -o ./artifacts || exit 1
    done'
  python3 tools/extract_doc_snippets.py
}

# Build the samples the way CI does.
build_samples() {   # extra args are passed through to dotnet build
  rm -rf "$SCRATCH/sample-packages"
  NUGET_PACKAGES="$SCRATCH/sample-packages" dotnet build samples/DataNet.Sample -c Release "$@" &&
  NUGET_PACKAGES="$SCRATCH/sample-packages" dotnet build samples/DataNet.DocSnippets -c Release "$@"
}
```

---

### Task 1: `samples/` gets the analyser

**Files:**

- Create: `samples/Directory.Build.props`

**Depends on:** nothing.
**Produces:** `samples/` inherits the root `Directory.Build.props` explicitly, and every later task's samples build is analysed.

This task adds **SonarAnalyzer only** — not the `AnalysisMode` switch, which lands in Task 8.

- [x] **Step 1: Prove the gap exists before closing it**

```bash
cd <repo>
cat >> samples/DataNet.Sample/Lot1Distances.cs <<'EOF'

internal static class Probe
{
    // int dead = 1;
}
EOF
pack_feed
build_samples 2>&1 | grep -c "S125"
```

Expected: `0`. Commented-out code, and nothing reports it. That is the bug.

- [x] **Step 2: Create `samples/Directory.Build.props`**

```xml
<Project>

  <!-- Chain to the repository-root Directory.Build.props (MSBuild otherwise stops
       at the nearest one). -->
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />

  <!-- The samples are the one area ADR 0009's packaging gate forces new code
       into, so they are the worst place to leave unanalysed. The version comes
       from the single pin in the root Directory.Build.props; it is named on the
       PackageReference because samples/, like bench/, has no Central Package
       Management. samples/NuGet.config maps only DataNet.* to the local feed, so
       this restores from nuget.org as usual. See
       docs/decisions/0019-the-net-analysers-run-in-the-build-too.md. -->
  <ItemGroup>
    <PackageReference Include="SonarAnalyzer.CSharp" Version="$(DataNetSonarAnalyzerVersion)" PrivateAssets="all" />
  </ItemGroup>

</Project>
```

- [x] **Step 3: Run the probe again — it must now fail the build**

```bash
build_samples 2>&1 | grep -E "error S125"
```

Expected: `error S125: Remove this commented out code.` in `Lot1Distances.cs`, and a non-zero exit. `TreatWarningsAsErrors` from the root props is what turns it into an error — its presence proves the `Import` on line 5 works.

- [x] **Step 4: Prove `Generated/*.g.cs` stays exempt**

```bash
git checkout samples/DataNet.Sample/Lot1Distances.cs
cat >> samples/DataNet.DocSnippets/Generated/Quickstart.g.cs <<'EOF'

internal static class Probe
{
    // int dead = 1;
}
EOF
build_samples 2>&1 | tail -3
```

Expected: build succeeds, 0 warnings. Roslyn's generated-code detection keys on the `.g.cs` suffix, which SonarAnalyzer honours — so the local build matches `sonar.exclusions` with no configuration. Record the exact output; ADR 0019 cites it.

- [x] **Step 5: Restore the tree and confirm it is clean**

```bash
python3 tools/extract_doc_snippets.py   # rewrites Generated/ from the Markdown
build_samples 2>&1 | tail -3
git status --porcelain
```

Expected: build green; `git status` shows only `samples/Directory.Build.props` as untracked.

- [x] **Step 6: Commit**

```bash
git add samples/Directory.Build.props
git commit -m "Run the Sonar rules over samples/, the one area a feature must touch"
```

---

### Task 2: The seven area-wide `NoWarn` entries

**Files:**

- Modify: `tests/Directory.Build.props`
- Modify: `bench/Directory.Build.props`
- Modify: `samples/Directory.Build.props`

**Depends on:** Task 1.
**Produces:** the `NoWarn` lists Task 8 relies on. They are **inert until Task 8**, because none of these rules is enabled at the SDK default — so this commit changes no build outcome.

- [x] **Step 1: Add the `tests/` list**

Insert this `PropertyGroup` into `tests/Directory.Build.props`, after the `Import` and before the `ItemGroup`:

```xml
  <!-- The .NET code-quality rules that a *test* trips by being a test. Each is
       off for this area only, and for a reason that does not apply to src/:
         CA1707 — Method_Case_Expected is the xunit naming convention.
         CA1515 — xunit requires a public test class to discover it.
         CA5394 — Random builds corpora here; there is nothing to keep secret.
         CA1062 — a test helper's arguments come from the suite, not a caller.
         CA1849 — the suite calls the synchronous Save on purpose, to compare it
                  against SaveAsync; that comparison is what the tests assert.
       None of the five is enabled at the SDK default, so SonarCloud reports none
       of them and turning them off here reopens no gap. See
       docs/decisions/0019-the-net-analysers-run-in-the-build-too.md. -->
  <PropertyGroup>
    <NoWarn>$(NoWarn);CA1707;CA1515;CA5394;CA1062;CA1849</NoWarn>
  </PropertyGroup>
```

- [x] **Step 2: Add the `bench/` list**

Insert into `bench/Directory.Build.props`, after the `Import`:

```xml
  <!-- The .NET code-quality rules a benchmark trips by being a benchmark:
         CA1707 — BenchmarkDotNet reports the method name as written.
         CA1515 — BenchmarkDotNet requires public types to discover them.
         CA5394 — Random builds corpora here; there is nothing to keep secret.
         CA1303 — a harness prints its own results to the console.
         CA1812 — BenchmarkDotNet instantiates its types by reflection, so an
                  "never instantiated" finding is always wrong here.
       See docs/decisions/0019-the-net-analysers-run-in-the-build-too.md. -->
  <PropertyGroup>
    <NoWarn>$(NoWarn);CA1707;CA1515;CA5394;CA1303;CA1812</NoWarn>
  </PropertyGroup>
```

- [x] **Step 3: Add the `samples/` line**

Insert into `samples/Directory.Build.props`, after the `Import` and before the `ItemGroup` added in Task 1:

```xml
  <!-- CA1303 (do not pass literals as localized parameters): printing to the
       console is what a sample does, and every string here is written to be read
       by a developer in the guides. See
       docs/decisions/0019-the-net-analysers-run-in-the-build-too.md. -->
  <PropertyGroup>
    <NoWarn>$(NoWarn);CA1303</NoWarn>
  </PropertyGroup>
```

- [x] **Step 4: Verify nothing changed**

```bash
dotnet build DataNet.slnx -c Release 2>&1 | tail -3
```

Expected: green, 0 warnings — a `NoWarn` for a rule that is not enabled is a no-op.

- [x] **Step 5: Commit**

```bash
git add tests/Directory.Build.props bench/Directory.Build.props samples/Directory.Build.props
git commit -m "Say per area which code-quality rules an area trips by being itself"
```

---

### Task 3: `bench/` — the one remaining finding

**Files:**

- Modify: `bench/DataNet.Text.Benchmarks/BatchEmbeddingBenchmarks.cs:34-35`

**Depends on:** Task 2.

`CA1001` says `BatchEmbeddingBenchmarks` holds a disposable `_embedder` without being `IDisposable`. It disposes it from `[GlobalCleanup]` at line 92-93 — BenchmarkDotNet owns the lifecycle, and making the class `IDisposable` would hand ownership to a caller that does not exist.

- [x] **Step 1: See the finding**

```bash
dotnet build bench/DataNet.Text.Benchmarks -c Release -p:TreatWarningsAsErrors=false $ANALYSIS 2>&1 | grep CA1001
```

Expected: one `warning CA1001` on `BatchEmbeddingBenchmarks`.

- [x] **Step 2: Add the pragma with its reason**

Above `[MemoryDiagnoser]` (currently line 33):

```csharp
// CA1001 (owns a disposable field but is not IDisposable): BenchmarkDotNet owns
// this type's lifecycle and calls [GlobalCleanup] below, which disposes
// _embedder. IDisposable would advertise an ownership no caller ever takes.
#pragma warning disable CA1001
[MemoryDiagnoser]
```

- [x] **Step 3: Verify `bench/` is clean under the switch**

```bash
dotnet build bench/DataNet.Text.Benchmarks bench/DataNet.NetStandard.Benchmarks -c Release $ANALYSIS 2>&1 | tail -3
```

Expected: green, 0 warnings, with `TreatWarningsAsErrors` left on.

- [x] **Step 4: Verify the committed build is still green**

```bash
dotnet build DataNet.slnx -c Release 2>&1 | tail -3
```

Expected: green.

- [x] **Step 5: Commit**

```bash
git add bench/DataNet.Text.Benchmarks/BatchEmbeddingBenchmarks.cs
git commit -m "Say why the batch-embedding benchmark is not IDisposable"
```

---

### Task 4: `samples/` — five CA1305

**Files:**

- Modify: `samples/DataNet.Sample/Lot3Embeddings.cs:1, 184, 185, 196`
- Modify: `samples/DataNet.Sample/Lot5Metrics.cs:1, 154, 198`

**Depends on:** Task 3.

`CA1305` wants an `IFormatProvider` on `ToString("F3")`. A reader should see the correct form, and a sample that prints `0,123` on a French machine and `0.123` on an English one is a sample that lies about what the library produced.

- [x] **Step 1: See the five**

```bash
pack_feed
build_samples -p:TreatWarningsAsErrors=false $ANALYSIS 2>&1 | grep CA1305
```

Expected: 5 warnings, at `Lot3Embeddings.cs:184,185,196` and `Lot5Metrics.cs:154,198`.

- [x] **Step 2: Add the using to both files**

`Lot3Embeddings.cs` line 1 becomes:

```csharp
using System.Globalization;
using System.Text;
```

`Lot5Metrics.cs` line 1 becomes:

```csharp
using System.Globalization;
using DataNet.Metrics;
```

- [x] **Step 3: Make the five calls invariant**

`Lot3Embeddings.cs:184-185`:

```csharp
        Console.WriteLine($"  MeanPool         : [{string.Join(", ", pooled.Select(v => v.ToString("F3", CultureInfo.InvariantCulture)))}]");
        Console.WriteLine($"  MeanPool+L2      : [{string.Join(", ", normalized.Select(v => v.ToString("F3", CultureInfo.InvariantCulture)))}]");
```

`Lot3Embeddings.cs:196`:

```csharp
            + string.Join(" | ", batchPooled.Select(v => $"[{string.Join(", ", v.Select(c => c.ToString("F3", CultureInfo.InvariantCulture)))}]")));
```

`Lot5Metrics.cs:154`:

```csharp
        Console.WriteLine($"    micro avg          = {report.MicroAverage?.F1.ToString("F3", CultureInfo.InvariantCulture) ?? "<absent: every label is covered>"}");
```

`Lot5Metrics.cs:198`:

```csharp
        "[" + string.Join(", ", values.Select(v => v.ToString("F3", CultureInfo.InvariantCulture))) + "]";
```

- [x] **Step 4: Verify `samples/` is clean under the switch**

```bash
build_samples $ANALYSIS 2>&1 | tail -3
```

Expected: both projects green, 0 warnings.

- [x] **Step 5: Verify the sample still runs and still prints the same numbers**

```bash
rm -rf "$SCRATCH/sample-packages"
NUGET_PACKAGES="$SCRATCH/sample-packages" dotnet run --project samples/DataNet.Sample -c Release 2>&1 | tail -20
```

Expected: exit 0, and the `MeanPool` / `micro avg` lines print `0.123`-style values. `InvariantCulture` is what the machine already produced, so no number changes.

- [x] **Step 6: Commit**

```bash
git add samples/DataNet.Sample/Lot3Embeddings.cs samples/DataNet.Sample/Lot5Metrics.cs
git commit -m "Format the sample's numbers invariantly, so it prints what it measured"
```

---

### Task 5: `tests/` — six findings

**Files:**

- Modify: `tests/DataNet.Text.Tests/Oracles/OracleAsserts.cs:88, 109, 131` (+ using)
- Modify: `tests/DataNet.Text.Tests/Distances/LevenshteinOracleTests.cs:130` (+ using)
- Modify: `tests/DataNet.Embeddings.Tests/ByteLevelBpeTests.cs:41`
- Modify: `tests/DataNet.Text.Tests/Vectorization/StopWordsTests.cs:51`

**Depends on:** Task 4.

Both test projects and both `*.NetStandard.Tests` mirrors target `net10.0`, so the `StringComparison` overload **is** available here — unlike `src/`. `StopWordsTests.cs:51` asserts that a stop word *is* lowercase, so `ToLowerInvariant` is the assertion itself.

- [x] **Step 1: See the six**

```bash
dotnet build DataNet.slnx -c Release -p:TreatWarningsAsErrors=false $ANALYSIS 2>&1 | grep -E "CA1305|CA1307|CA1308" | grep tests/
```

Expected: 4 × CA1305, 1 × CA1307, 1 × CA1308.

- [x] **Step 2: Make the four `StringBuilder` appends invariant**

Add `using System.Globalization;` as the first using of `OracleAsserts.cs` and of `LevenshteinOracleTests.cs`, then:

`OracleAsserts.cs:88`, `:109`, `:131` — pass the provider to the interpolated-string overload:

```csharp
            failures.Append(CultureInfo.InvariantCulture, $"  {describe(c)}: expected \"{e}\", got \"{a}\"\n");
```

```csharp
            failures.Append(CultureInfo.InvariantCulture, $"  {describe(c)}: expected {e}, got {a}\n");
```

```csharp
            failures.Append(CultureInfo.InvariantCulture, $"  {describe(c)}: expected {e:R}, got {a:R}\n");
```

`LevenshteinOracleTests.cs:130`:

```csharp
            sb.Append(CultureInfo.InvariantCulture, $"  [#{c.Id} {c.Category}] a={Escape(c.A)} b={Escape(c.B)}: {message}\n");
```

- [x] **Step 3: Make the `IndexOf` explicit**

`ByteLevelBpeTests.cs:41`:

```csharp
            int space = line.IndexOf(' ', StringComparison.Ordinal);
```

Add `using System;` only if the file lacks it — `ImplicitUsings` is on repo-wide, so it almost certainly does not need one.

- [x] **Step 4: Say why the assertion lowercases**

`StopWordsTests.cs:51` — the line reads `Assert.Equal(word.ToLowerInvariant(), word);`. Put a file-scoped pragma above the class, matching the idiom in `src/DataNet.Text/Stemming/`:

```csharp
// CA1308 (normalize to uppercase): the assertion below is precisely that every
// shipped stop word is already lowercase, which is the invariant the lists are
// built on. Uppercasing would assert the opposite of what is meant.
#pragma warning disable CA1308
```

- [x] **Step 5: Verify `tests/` is clean under the switch and still passes**

```bash
dotnet build DataNet.slnx -c Release $ANALYSIS 2>&1 | grep -E "tests/.*(warning|error)" ; echo "---"
dotnet test DataNet.slnx -c Release 2>&1 | tail -8
```

Expected: no `tests/` diagnostics (the `src/` ones remain until Tasks 6-7); the whole suite passes. **Read the pass/fail counts, not just the exit code** — a suite that ran 0 tests is not a green suite.

- [x] **Step 6: Commit**

```bash
git add tests/
git commit -m "Pin the culture and the comparison the oracle assertions already assumed"
```

---

### Task 6: `src/` — where the analyser is right

**Files:**

- Modify: `src/DataNet.Fuzzy/Fuzz.cs:91-93, 103-105`
- Modify: `src/DataNet.Text/Vectorization/TfidfTransformer.cs:42-44, 64-70`
- Modify: `src/DataNet.Text/Persistence/FeatureVocabularyJson.cs:184`

**Depends on:** Task 5.

Five genuine defects. `Ratio`, `PartialRatio` and `WRatio` in `Fuzz.cs` all guard both arguments; the four `Token*Ratio` methods do not, so `TokenSortRatio(null, "x")` throws `NullReferenceException` from `string.Split` instead of `ArgumentNullException`. `Guard` is in `DataNet.Internal` and reaches every `src/` file through `src/Shared/GlobalUsings.cs` — `Fuzz.cs` already calls it unqualified.

- [x] **Step 1: See the five**

```bash
dotnet build DataNet.slnx -c Release -p:TreatWarningsAsErrors=false $ANALYSIS 2>&1 | grep -E "CA1062|CA2251" | grep src/
```

Expected: 4 × CA1062 (`Fuzz.cs:93`, `:105`, `TfidfTransformer.cs:44`, `:70`), 1 × CA2251 (`FeatureVocabularyJson.cs:184`).

- [x] **Step 2: Guard the two flagged `Fuzz` methods**

```csharp
    /// <summary><see cref="Ratio"/> after splitting, sorting and rejoining the tokens of each string.</summary>
    public static double TokenSortRatio(string a, string b)
    {
        Guard.NotNull(a);
        Guard.NotNull(b);
        return Ratio(SortedJoin(Tokenize(a)), SortedJoin(Tokenize(b)));
    }
```

```csharp
    /// <summary><see cref="PartialRatio"/> on sorted-token strings.</summary>
    public static double PartialTokenSortRatio(string a, string b)
    {
        Guard.NotNull(a);
        Guard.NotNull(b);
        return PartialRatio(SortedJoin(Tokenize(a)), SortedJoin(Tokenize(b)));
    }
```

Then read `TokenSet` (called by `TokenSetRatio` and `PartialTokenSetRatio`, the two siblings the analyser did **not** flag). If it already guards, nothing to do. If it does not, **do not widen this task** — note it for the PR body as a pre-existing inconsistency the analyser did not catch.

- [x] **Step 3: Guard the two `TfidfTransformer` entry points**

```csharp
    public TfidfTransformer Fit(CsrMatrix counts)
    {
        Guard.NotNull(counts);
        int n = counts.RowCount;
```

```csharp
    public CsrMatrix Transform(CsrMatrix counts)
    {
        Guard.NotNull(counts);
        if (_options.UseIdf && _idf is null)
```

`FitTransform` at line 112 delegates to both, so it needs nothing.

- [x] **Step 4: Replace the `CompareOrdinal == 0`**

`FeatureVocabularyJson.cs:184` currently reads `string.CompareOrdinal(previous, current) == 0`. Replace with:

```csharp
        string.Equals(previous, current, StringComparison.Ordinal)
```

Leave line 92's `string.CompareOrdinal(previous, name) >= 0` alone — it is an ordering test, and CA2251 does not flag it.

- [x] **Step 5: Verify the five are gone and nothing regressed**

```bash
dotnet build DataNet.slnx -c Release -p:TreatWarningsAsErrors=false $ANALYSIS 2>&1 | grep -E "CA1062|CA2251"
dotnet test DataNet.slnx -c Release 2>&1 | tail -8
```

Expected: no output from the first command; the suite green with its counts read.

- [x] **Step 6: Write the test that pins the new behaviour**

`TokenSortRatio(null!, "x")` now throws `ArgumentNullException` where it threw `NullReferenceException`. That is a behaviour change and needs a test. Add to `tests/DataNet.Fuzzy.Tests/FuzzTests.cs` (match the file's existing naming and `[Fact]` style):

```csharp
    [Fact]
    public void TokenSortRatio_NullArgument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Fuzz.TokenSortRatio(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => Fuzz.TokenSortRatio("x", null!));
    }

    [Fact]
    public void PartialTokenSortRatio_NullArgument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Fuzz.PartialTokenSortRatio(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => Fuzz.PartialTokenSortRatio("x", null!));
    }
```

- [x] **Step 7: Run those two tests and confirm they exercise the new guards**

```bash
dotnet test tests/DataNet.Fuzzy.Tests -c Release --filter "FullyQualifiedName~TokenSortRatio_Null|FullyQualifiedName~PartialTokenSortRatio_Null" 2>&1 | tail -6
```

Expected: `Passed! - Failed: 0, Passed: 2`. **If it reports `Passed: 0`, the filter matched nothing and nothing was verified** — fix the filter before believing the result.

- [x] **Step 8: Commit**

```bash
git add src/DataNet.Fuzzy/Fuzz.cs src/DataNet.Text/Vectorization/TfidfTransformer.cs \
        src/DataNet.Text/Persistence/FeatureVocabularyJson.cs tests/DataNet.Fuzzy.Tests/FuzzTests.cs
git commit -m "Throw ArgumentNullException where the token ratios threw NullReference"
```

---

### Task 7: `src/` — where the code is right

**Files:**

- Modify: `src/Shared/StringCompat.cs`, `src/Shared/Persistence/ArtifactIo.cs:44`, `src/Shared/Persistence/JsonArtifact.cs:172`
- Modify: `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs`, `src/DataNet.Embeddings/Tokenization/WordPieceTokenizer.cs`, `src/DataNet.Embeddings/Tokenization/SentencePieceVocabulary.cs`
- Modify: `src/DataNet.Metrics/Internal/ReportText.cs`, `src/DataNet.Metrics/ConfusionMatrix.cs`
- Modify: `src/DataNet.Text/Stemming/` — `EnglishSnowballStemmer.cs`, `FrenchSnowballStemmer.cs`, `GermanSnowballStemmer.cs`, `ItalianSnowballStemmer.cs`, `PorterStemmer.cs`, `PortugueseSnowballStemmer.cs`, `SpanishSnowballStemmer.cs`
- Modify: `src/DataNet.Text/Vectorization/TextAnalyzer.cs`, `src/DataNet.Text/Vectorization/CsrMatrix.cs`

**Depends on:** Task 6.

29 reasoned suppressions. Use **file-scoped** pragmas near the top of the file, without `restore`, which is the idiom these very files already use (`SpanishSnowballStemmer.cs:9-10`, `Lcs.cs:7`). Use a `disable`/`restore` pair only where an existing pair is already in place.

- [x] **Step 1: See all 26**

```bash
dotnet build DataNet.slnx -c Release -p:TreatWarningsAsErrors=false $ANALYSIS 2>&1 \
  | grep -oE "src/[^(]*\([0-9]+,[0-9]+\): warning CA[0-9]+" | sort -u
```

Expected: CA1307 ×8, CA1308 ×10, CA1814 ×4, CA1819 ×3, CA1849 ×2, CA1008 ×1, CA1720 ×1 — note CA1814 and CA1819 report at more than one line per member.

- [x] **Step 2: CA1307 ×8 — the overload does not exist on netstandard2.0**

Files: `StringCompat.cs`, `TokenizerJsonLoader.cs`, `ReportText.cs`, `GermanSnowballStemmer.cs`, `PortugueseSnowballStemmer.cs`. Add near the top of each, above the type:

```csharp
// CA1307 (specify StringComparison): the overload it asks for —
// string.IndexOf(char, StringComparison) / string.Replace(string, string?,
// StringComparison) — does not exist on netstandard2.0, which this assembly
// targets. Both calls are ordinal on every runtime that has them, so the
// suggestion would change nothing but the compilation. Same reason as the CA1845
// pragmas in DataNet.Embeddings/Tokenization/.
#pragma warning disable CA1307
```

`PortugueseSnowballStemmer.cs` and `GermanSnowballStemmer.cs` already carry pragma lines at the top; add `CA1307` to that block rather than opening a new one.

- [x] **Step 3: CA1308 ×10 — lowercase is the algorithm**

Files: `WordPieceTokenizer.cs`, `EnglishSnowballStemmer.cs`, `FrenchSnowballStemmer.cs`, `GermanSnowballStemmer.cs`, `ItalianSnowballStemmer.cs`, `PorterStemmer.cs` (2 sites), `PortugueseSnowballStemmer.cs`, `SpanishSnowballStemmer.cs`, `TextAnalyzer.cs`. Add to each file's pragma block:

```csharp
// CA1308 (normalize to uppercase): Snowball, Porter and WordPiece are *defined*
// on lowercase input — the published algorithms, the reference implementations
// and the oracle corpora this suite is checked against all lowercase first.
// ToUpperInvariant would return different stems, which is a wrong answer rather
// than a differently-cased one.
#pragma warning disable CA1308
```

Where a file already has a pragma line, extend it (e.g. `SpanishSnowballStemmer.cs:10` becomes `#pragma warning disable S3776, S3267, CA1308`) and put the comment above it.

- [x] **Step 4: CA1814 ×4 — the dense form is the point**

`ConfusionMatrix.cs` above `ToArray` (line 128) and `CsrMatrix.cs` above `ToDense` (line 137):

```csharp
    // CA1814 (prefer jagged arrays): a confusion matrix and a densified CSR
    // matrix are rectangular by construction, and double[,] is the shape every
    // consumer expects to interop with. A jagged array would cost one allocation
    // per row and let a caller build a ragged one.
#pragma warning disable CA1814
```

- [x] **Step 5: CA1819 ×3 — the three arrays are the format**

`CsrMatrix.cs` above `Values` (line 125):

```csharp
    // CA1819 (properties should not return arrays): Values, ColumnIndices and
    // RowPointers *are* the compressed-sparse-row format — a consumer indexes
    // them directly, which is the reason to expose CSR at all. They have been
    // public since 0.1.0, so wrapping them is a breaking change for a rule about
    // defensive copies this type deliberately does not make.
#pragma warning disable CA1819
```

- [x] **Step 6: CA1849 ×2 — extend the pragmas already there**

`ArtifactIo.cs:44` and `JsonArtifact.cs:172` already read `#pragma warning disable S6966` with a comment explaining that the destination is a `MemoryStream` whose async path performs no I/O. CA1849 says the same thing S6966 says, so extend both lines and their matching `restore`:

```csharp
#pragma warning disable S6966, CA1849
```

```csharp
#pragma warning restore S6966, CA1849
```

Prefix the existing comment's `SonarLint S6966:` with `CA1849 /` so the reason names both rules.

- [x] **Step 7: CA1008 ×1 and CA1720 ×1**

`SentencePieceVocabulary.cs` above `public enum SentencePieceType` (line 13):

```csharp
// CA1008 (enums should have a zero value): the members mirror the piece types of
// SentencePiece's own ModelProto, which are numbered from 1. A synthetic None = 0
// would be a value no model file can carry. See
// docs/decisions/0013-sentencepiece-parity-scope.md.
#pragma warning disable CA1008
```

`TextAnalyzer.cs` above `public enum AnalyzerKind` (line 12), added to the pragma block already at line 11:

```csharp
// CA1720 (identifier contains type name): AnalyzerKind.Char mirrors
// scikit-learn's analyzer='char', which is the name a reader arrives with, and it
// has been public since 0.1.0 — renaming it breaks consumers for a naming rule.
#pragma warning disable CA1720
```

- [x] **Step 8: The whole solution is now clean under the switch**

```bash
dotnet build DataNet.slnx -c Release $ANALYSIS 2>&1 | tail -3
```

Expected: green, **0 warnings**, with `TreatWarningsAsErrors` on. This is the moment the sweep is finished.

- [x] **Step 9: The suite still passes, and the netstandard2.0 leg still compiles**

```bash
dotnet test DataNet.slnx -c Release 2>&1 | tail -8
```

Expected: green, counts read. The `*.NetStandard.Tests` projects are what prove the `netstandard2.0` assemblies still work — if their counts are 0, nothing was verified.

- [x] **Step 10: Commit**

```bash
git add src/
git commit -m "Say in each file why the code-quality rule is wrong about it"
```

---

### Task 8: Turn the switch on

**Files:**

- Modify: `Directory.Build.props` (root)

**Depends on:** Task 7. Everything the switch would report is already fixed, so this commit is green from the first build.

- [x] **Step 1: Add the three properties**

In the root `Directory.Build.props`, immediately after the `TreatWarningsAsErrors` line and before the `DataNetSonarAnalyzerVersion` block:

```xml
    <!-- The .NET code-quality rules, on and enforced. Two of them are the reason
         this exists: CA1845 and CA1859 are `note` severity by default, so they
         never appear in build output — while the compiler still writes them to
         the SARIF error log the Sonar scanner reads, and the quality gate counts
         them against a threshold of zero. AnalysisMode=All raises every rule to
         warning, which TreatWarningsAsErrors above turns into a build failure.

         EnableNETAnalyzers is not decorative: the SDK enables these analysers
         only for net5.0 and later, so without it the netstandard2.0 leg of every
         multi-targeted project goes unanalysed.

         AnalysisLevel is pinned rather than `latest` for the reason
         docs/decisions/0015-sonar-rules-in-the-build.md gives about the
         SonarAnalyzer pin: adding rules to a build where warnings are errors
         breaks it, so the bump must be an edit somebody makes on purpose rather
         than a side effect of CI resolving a newer 10.0.x SDK.

         What each area switches back off, and why, is in that area's
         Directory.Build.props. See
         docs/decisions/0019-the-net-analysers-run-in-the-build-too.md. -->
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>10.0</AnalysisLevel>
    <AnalysisMode>All</AnalysisMode>
```

- [x] **Step 2: The plain build — no overrides — is green**

```bash
dotnet build DataNet.slnx -c Release 2>&1 | tail -3
```

Expected: green, 0 warnings. This is the command a contributor runs, and it now enforces what the gate enforces.

- [x] **Step 3: The samples build is green too**

```bash
pack_feed
build_samples 2>&1 | tail -3
```

Expected: both projects green. The properties reach `samples/` through the `Import` added in Task 1 — which is the half of this issue that did not exist before.

- [x] **Step 4: Commit**

```bash
git add Directory.Build.props
git commit -m "Enforce the .NET code-quality rules the pull-request gate counts"
```

---

### Task 9: Prove the gate bites, in all four areas

**Files:** none committed. Every edit in this task is reverted.

**Depends on:** Task 8. ADR 0015 proved its gate by making it fail on purpose; this reproduces that for the wider rule set and for the area that had no gate at all.

- [x] **Step 1: `src/`**

```bash
printf '\ninternal static class Probe107 { public static string Up(string s) => s.ToLowerInvariant(); }\n' >> src/DataNet.Text/Distances/Hamming.cs
dotnet build src/DataNet.Text -c Release 2>&1 | grep -E "error CA1308"
git checkout src/DataNet.Text/Distances/Hamming.cs
```

Expected: `error CA1308`. Record the exact line for the ADR.

- [x] **Step 2: `tests/`**

```bash
printf '\ninternal static class Probe107 { public static int Find(string s) => s.IndexOf("x"); }\n' >> tests/DataNet.Text.Tests/Distances/LevenshteinOracleTests.cs
dotnet build tests/DataNet.Text.Tests -c Release 2>&1 | grep -E "error CA1307"
git checkout tests/DataNet.Text.Tests/Distances/LevenshteinOracleTests.cs
```

Expected: `error CA1307`. CA1307 is *not* in the `tests/` `NoWarn` list, which is what this checks.

- [x] **Step 3: `bench/`**

```bash
printf '\ninternal static class Probe107 { public static string Low(string s) => s.ToLowerInvariant(); }\n' >> bench/DataNet.Text.Benchmarks/VectorizerBenchmarks.cs
dotnet build bench/DataNet.Text.Benchmarks -c Release 2>&1 | grep -E "error CA1308"
git checkout bench/DataNet.Text.Benchmarks/VectorizerBenchmarks.cs
```

Expected: `error CA1308`.

- [x] **Step 4: `samples/` — the one that is new**

```bash
printf '\ninternal static class Probe107 { public static int Find(string s) => s.IndexOf("x"); }\n' >> samples/DataNet.Sample/Lot1Distances.cs
build_samples 2>&1 | grep -E "error CA1307|error S"
git checkout samples/DataNet.Sample/Lot1Distances.cs
```

Expected: an `error`. Before this branch, the same line produced nothing at all.

- [x] **Step 5: Confirm the tree is clean and green again**

```bash
git status --porcelain
dotnet build DataNet.slnx -c Release 2>&1 | tail -3
```

Expected: `git status` empty; build green. **Nothing is committed in this task** — if `git status` is not empty, a probe survived.

---

### Task 10: The documentation

**Files:**

- Create: `docs/decisions/0019-the-net-analysers-run-in-the-build-too.md`
- Modify: `docs/decisions/0015-sonar-rules-in-the-build.md` (the "Why `samples/` stays out" section)
- Modify: `CONTRIBUTING.md`
- Modify: `samples/Directory.Build.props`, `tests/Directory.Build.props`, `bench/Directory.Build.props` — fix the four dead ADR references (see Step 0)

**Depends on:** Task 9 — the ADR quotes its output.

- [x] **Step 0: Fix the four dead ADR references**

`main` moved while this branch was in flight: pull request #108 landed
`docs/decisions/0018-multiclass-roc-auc-parallelism-is-opt-in.md`, so **0018 is
taken and this ADR is 0019**. Tasks 1 and 2 wrote the old number into three
files, four times:

```bash
grep -rn "0018-the-net-analysers" --include=*.props .
```

Expected: two hits in `samples/Directory.Build.props`, one in
`tests/Directory.Build.props`, one in `bench/Directory.Build.props`. Change each
to `0019-the-net-analysers-run-in-the-build-too.md`. Do this in **this** commit,
so the reference and the file it points at land together and the branch never
contains a live pointer to a missing ADR.

- [x] **Step 1: Write ADR 0019**

Follow the shape of `0015`: `# 0019 — …`, `**Status:** accepted · **Date:** 2026-08-10`, then Context / Decision / Consequences. It must contain:

- The two causes from issue #107, and that `samples/` is the area ADR 0009's packaging gate forces new code into.
- The measured table: `Default` 0 findings, `Minimum` 0, `Recommended` 524, `All` 655 — and that `AnalysisMode=Minimum` already contains CA1845 and CA1859, so **the gap is closed by any mode above `Default`; `All` is a deliberate strictness upgrade on top**. Do not let the finding count imply the 655 were gate failures.
- That **none of the 16 rules behind the 655 is enabled at the SDK default**, so SonarCloud reports none of them and no area-wide `NoWarn` here reopens the gap — with the note that this has to be re-checked whenever the lists grow.
- The `.g.cs` measurement from Task 1 Step 4, quoted: the probe in `Generated/Quickstart.g.cs` produces 0 warnings, the identical probe in `SnippetContext.cs` produces S125, S101 and S1186. This is what **amends ADR 0015's "Why `samples/` stays out"**, whose stated reason is measured false.
- Why `AnalysisLevel` is pinned to `10.0`.
- The Task 9 demonstration, one line per area.
- The honest limitation: `samples/` builds only after a `pack`, so its gate lives in the two samples CI jobs, not in `dotnet build DataNet.slnx`.

- [x] **Step 2: Point ADR 0015 at it**

In `0015`, at the top of "Why `samples/` stays out", add:

```markdown
> **Amended by [0019](0019-the-net-analysers-run-in-the-build-too.md) (2026-08-10).**
> `samples/` now carries the analyser. The reason given below — that
> `Generated/` would light up prose — is measured false: Roslyn skips `.g.cs`
> files as generated code, and SonarAnalyzer honours that.
```

Leave the original text intact below it. An ADR records what was decided when.

- [x] **Step 3: Write the suppression policy into `CONTRIBUTING.md`**

Find the section that already describes `#pragma warning disable` with a reason and add the area-wide half:

```markdown
A rule that a whole area trips *by being that area* — xunit's underscored test
names, BenchmarkDotNet's reflection-instantiated types, a sample printing to the
console — goes in that area's `Directory.Build.props` as a `NoWarn` entry, with a
comment naming each rule and why it does not apply there. A rule that one call
site disagrees with stays a `#pragma warning disable` in the source, with its
reason above it. Never add either without the reason.
```

- [x] **Step 4: Re-read what the new ADR falsifies**

```bash
grep -rn "0015\|0017\|decisions/" --include=*.md docs README.md CONTRIBUTING.md | grep -v "^docs/decisions/0019"
ls docs/decisions/
```

Counts, enumerations and "see X" pointers go stale silently. Check any place that lists the ADRs or states how many there are, and any statement that `samples/` is not analysed.

- [x] **Step 5: Verify the guides still compile**

```bash
pack_feed
build_samples 2>&1 | tail -3
```

Expected: green — `CONTRIBUTING.md` has no C# fences, but `tools/extract_doc_snippets.py` reads the guides and this is the cheap way to be sure nothing was disturbed.

- [x] **Step 6: Commit**

```bash
git add docs/decisions/0019-the-net-analysers-run-in-the-build-too.md \
        docs/decisions/0015-sonar-rules-in-the-build.md CONTRIBUTING.md
git commit -m "Record why the code-quality analysers run here, and what each area switches off"
```

---

### Task 11: Push, watch the gate, open the pull request

**Depends on:** Task 10.

- [x] **Step 1: Final local verification, all of it**

```bash
git status --porcelain                                  # must be empty
dotnet build DataNet.slnx -c Release 2>&1 | tail -3     # green, 0 warnings
dotnet test DataNet.slnx -c Release 2>&1 | tail -8      # green, counts read
pack_feed && build_samples 2>&1 | tail -3               # green
env -u DOTNET_ROOT dotnet format DataNet.slnx --verify-no-changes ; echo "format exit=$?"
```

`format exit=0` is the only thing that means formatting is verified. If it times out on a `NamedPipeClientStream`, check `DOTNET_ROOT` against `which dotnet` before anything else.

- [x] **Step 2: Rebase onto `main` if it moved**

```bash
git fetch origin
git log --oneline HEAD..origin/main
```

If anything is listed, rebase — a long review is exactly when `main` moves, and the silent conflicts cost more than the reported ones.

- [x] **Step 3: Push**

```bash
git push -u origin chore/107-analysis-parity
```

- [x] **Step 4: Wait for CI, then read SonarCloud — a green build is not a clean Sonar**

```bash
gh run list --branch chore/107-analysis-parity --limit 5
```

Then read the findings SonarCloud raised on this branch, passing `resolved=false` — without it the count never drops, because the API returns closed issues too. The oracle-drift gate is flaky: if it is the only red one, re-run it before believing it.

- [x] **Step 5: Open the pull request**

```bash
gh pr create --base main --title "Close the gap between the local build's analysis and the PR gate" --body "$(cat <<'EOF'
Closes #107.

<body: the two causes, the measured table, the seven area-wide NoWarn lists and
why each is safe, the 46 individual judgements split into 15 fixes and 31
reasoned suppressions, the .g.cs measurement, the four-area demonstration, and
the CA1307/netstandard2.0 correction to the original issue's "set an explicit
AnalysisMode" plan.>

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

Then stop. **The user reviews and merges.**

---

## Self-review

**Spec coverage.** D1 → Task 8. D2 → Task 1. D3 → Tasks 2-7 (every one of the 16 rules appears in exactly one task). D4 → Task 10. D5 → Task 9, plus the per-task verification steps and Task 11.

**Corrections carried in.** CA1307 in `src/` moved from "fix" to "reasoned suppression" — the overload does not exist on `netstandard2.0`. The spec's `NoWarn` table is otherwise unchanged; the spec should be read alongside this plan's "Correction to the spec" section.

**Ordering.** The 46 fixes precede the switch, so no commit is red. Task 2's `NoWarn` entries are inert until Task 8 and are placed early only so Tasks 3-7 verify against the final rule set.

**Known soft spots, called out where they occur.** Task 3 Step 1, Task 5 Step 1, Task 6 Step 1 and Task 7 Step 1 all re-derive their finding list from a live build rather than trusting this plan's line numbers, because an earlier task's edit shifts them.
