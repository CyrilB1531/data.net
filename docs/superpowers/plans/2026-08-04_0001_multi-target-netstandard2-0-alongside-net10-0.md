# #1 Multi-target `netstandard2.0` — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship one NuGet package per library carrying both `net10.0` and `netstandard2.0`, with an identical public surface on each, so the packages install on .NET Framework 4.6.1+, Mono, Xamarin and Unity without a consumer noticing which leg they landed on.

**Architecture:** `TargetFrameworks` gains a second entry in all three `.csproj`. The gaps it opens are closed in a fixed order — PolySharp, then target-conditional `System.Memory`/`System.Numerics.Vectors`, then hand-written fallbacks. Two shared files under `src/Shared/` (`Guard`, `StringCompat`) absorb the two gaps that would otherwise scatter `#if` across every file. `VectorMath.Dot` is the single deliberate behavioural split: SIMD on net10, scalar on netstandard2.0.

**Tech Stack:** MSBuild multi-targeting, PolySharp 1.15.0, System.Memory 4.6.0, System.Numerics.Vectors 4.6.0, xunit.

**Spec:** `2026-08-04_0001_multi-target-netstandard2-0-alongside-net10-0.md` (in `../specs/`).

## Global Constraints

- **Everything in English** — code comments, ADR, commit messages, PR body.
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `feat/1-netstandard2.0-multitarget`. Never commit to `main`.
- `TreatWarningsAsErrors=true` is repo-wide (#6) and must stay. Never verify with
  `-p:TreatWarningsAsErrors=false` in a final check — only while diagnosing.
- **The public API must be identical on both targets.** If a fix is reachable only
  by removing or narrowing a member, the fix is wrong.
- **No oracle value may move.** `tests/oracles/*.json` is untouched by this branch.
  A diff there means a fallback changed behaviour.
- Per ADR 0003, nothing is copied from an existing polyfill package. PolySharp is
  *referenced*, not vendored.
- Stay inside the multi-targeting concern. #7, #8 and #10 touch the same files and
  are excluded on purpose.

### Reusable verification commands

```bash
cd <repo>

# Both frameworks, warnings as errors.
build_all() { dotnet build -c Release; }

# One framework at a time — the netstandard leg is where the errors are.
build_ns()  { dotnet build -c Release -f netstandard2.0; }
build_net() { dotnet build -c Release -f net10.0; }

# The suite still targets net10.0 only; that limitation is D6, not a defect.
test_all()  { dotnet test -c Release; }
```

---

### Task 1: Turn on the second target and see the true size of the problem

**Files:**

- Modify: `src/DataNet.Text/DataNet.Text.csproj`
- Modify: `src/DataNet.Embeddings/DataNet.Embeddings.csproj`
- Modify: `src/DataNet.Fuzzy/DataNet.Fuzzy.csproj`

**Depends on:** nothing.
**Produces:** a complete, counted list of what `netstandard2.0` actually rejects — the input to every later task.

Do not fix anything in this task. The point is to measure before deciding.

- [x] **Step 1: Add the second target framework**

In each of the three `.csproj`, replace `<TargetFramework>net10.0</TargetFramework>` with:

```xml
<TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>
```

- [x] **Step 2: Record the failure list**

```bash
dotnet build -c Release -f netstandard2.0 2>&1 | grep -E "error CS" | sed 's/.*error /error /' | sort | uniq -c | sort -rn
```

Expected: a long tail dominated by missing `Span`/`Memory` types, then range
operators, `string.Join(char)`, `MathF`, `Array.Fill`, `CollectionsMarshal`,
`KeyValuePair` deconstruction, `.Order()` and
`ArgumentNullException.ThrowIfNull`.

Keep this list. Task 5 is finished when it is empty, and no other measure of
"done" is accepted.

- [x] **Step 3: Confirm net10 is untouched**

```bash
build_net && test_all
```

Expected: green, 158/158. Adding a target framework must not disturb the existing
one; if it does, stop and find out why before continuing.

---

### Task 2: The two shared helpers

**Files:**

- Create: `src/Shared/Guard.cs`
- Create: `src/Shared/StringCompat.cs`
- Create: `src/Shared/GlobalUsings.cs`
- Create: `src/Directory.Build.props`
- Create: `src/Directory.Packages.props`

**Depends on:** Task 1.
**Produces:** the mechanism that keeps `#if` out of every other file in the repository.

- [x] **Step 1: `src/Directory.Packages.props`**

Central package management for the shipped libraries, pinning the three packages
this change introduces:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="PolySharp" Version="1.15.0" />
    <PackageVersion Include="System.Memory" Version="4.6.0" />
    <PackageVersion Include="System.Numerics.Vectors" Version="4.6.0" />
  </ItemGroup>
</Project>
```

- [x] **Step 2: `src/Directory.Build.props`**

It must **import the repository root explicitly**. MSBuild stops at the nearest
`Directory.Build.props`, so without the import the libraries silently lose
warnings-as-errors and their package identity — a failure that shows up as a
*successful* build, which is the worst kind.

```xml
<Project>

  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />

  <!-- Compile-time polyfills (records/init, Index/Range, nullable attrs…) so the
       netstandard2.0 target builds. PrivateAssets=all: no runtime dependency. -->
  <ItemGroup>
    <PackageReference Include="PolySharp" PrivateAssets="all" />
  </ItemGroup>

  <!-- Shared internal helpers compiled into every library. -->
  <ItemGroup>
    <Compile Include="$(MSBuildThisFileDirectory)Shared/GlobalUsings.cs" Link="Internal/GlobalUsings.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)Shared/Guard.cs" Link="Internal/Guard.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)Shared/StringCompat.cs" Link="Internal/StringCompat.cs" />
  </ItemGroup>

  <!-- Span, Memory, ArrayPool and Vector<T> are in-box on net10 but need these
       packages on netstandard2.0. -->
  <ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.0'">
    <PackageReference Include="System.Memory" />
    <PackageReference Include="System.Numerics.Vectors" />
  </ItemGroup>

</Project>
```

- [x] **Step 3: `Guard.NotNull`, one `#if` for the whole repository**

`namespace DataNet.Internal`, `internal static class Guard`. On net10 it delegates
to `ArgumentNullException.ThrowIfNull`; on netstandard2.0 it throws by hand. The
delegation is not decoration — it is what keeps CA1510 quiet on the net10 leg,
where the analyser insists on the built-in.

- [x] **Step 4: `StringCompat`, the char overloads**

`StartsWith(char)`, `EndsWith(char)`, `Contains(char)` as extension-shaped statics
in `DataNet.Internal`. These are the three that appear across the stemmers and
tokenizers.

- [x] **Step 5: `GlobalUsings.cs`**

`global using DataNet.Internal;` so no call site needs a directive.

- [x] **Step 6: Prove the helpers are reachable from all three libraries**

```bash
build_ns 2>&1 | grep -cE "error CS0103.*Guard|error CS0103.*StringCompat"
```

Expected: `0`. Any hit means the `Compile Include` glob does not reach that
project.

---

### Task 3: Replace `ArgumentNullException.ThrowIfNull` everywhere

**Files:**

- Modify: every `src/` file with a public entry point (`Fuzz.cs`, `Process.cs`,
  `Deduplicator.cs`, `CountVectorizer.cs`, `HashingVectorizer.cs`, `CsrMatrix.cs`,
  `TextAnalyzer.cs`, `EmbeddingIndex.cs`, `OnnxTextEmbedder.cs`, the tokenizers)

**Depends on:** Task 2.
**Produces:** the largest single class of error from Task 1's list, gone.

- [x] **Step 1: Find every call site**

```bash
grep -rn "ArgumentNullException.ThrowIfNull" src --include='*.cs' | wc -l
```

- [x] **Step 2: Replace with `Guard.NotNull`, mechanically**

Same argument, same parameter name, same exception type and message. This is a
substitution, not a redesign — argument validation semantics do not change on
either target.

- [x] **Step 3: Verify both legs**

```bash
build_net && build_ns 2>&1 | grep -c "ThrowIfNull"
```

Expected: net10 green; `0` remaining references.

---

### Task 4: The mechanical long tail

**Files:**

- Modify: `src/DataNet.Text/Similarity/SetSimilarity.cs`,
  `Stemming/PorterStemmer.cs`, `Stemming/EnglishSnowballStemmer.cs`,
  `Stemming/FrenchSnowballStemmer.cs`, `Vectorization/CountVectorizer.cs`,
  `Vectorization/CsrMatrix.cs`, `Vectorization/HashingVectorizer.cs`,
  `Vectorization/TextAnalyzer.cs`
- Modify: `src/DataNet.Embeddings/Tokenization/WordPieceTokenizer.cs`,
  `Tokenization/SentencePieceTokenizer.cs`, `Search/EmbeddingIndex.cs`,
  `Onnx/OnnxTextEmbedder.cs`
- Modify: `src/DataNet.Fuzzy/Fuzz.cs`, `Process.cs`, `Deduplicator.cs`

**Depends on:** Task 3.
**Produces:** an empty error list from Task 1, Step 2.

Work one library at a time — `DataNet.Text`, then `DataNet.Embeddings`, then
`DataNet.Fuzzy` — and rebuild after each. A portable equivalent per construct:

- [x] **Step 1: Range and index operators** → explicit `Substring` / index
      arithmetic. Note that `s[^1]` inside an interpolation is easy to miss;
      the compiler finds them all, so trust the error list rather than a grep.
- [x] **Step 2: `string.Join(char, …)`** → `string.Join(separator.ToString(), …)`.
- [x] **Step 3: `MathF.*`** → `(float)Math.*`, keeping the cast explicit so the
      float arithmetic is visible to a reader.
- [x] **Step 4: `Array.Fill`** → an explicit loop.
- [x] **Step 5: `CollectionsMarshal.GetValueRefOrAddDefault`** → `TryGetValue` +
      assignment. This costs a second hash lookup on the netstandard leg only;
      the net10 path keeps the marshal under `#if`.
- [x] **Step 6: `KeyValuePair` deconstruction** → `.Key` / `.Value`.
- [x] **Step 7: `.Order()`** → `.OrderBy(x => x)`.

- [x] **Step 8: The error list must now be empty**

```bash
build_ns 2>&1 | grep -c "error CS"
```

Expected: `0`.

---

### Task 5: `VectorMath.Dot` — the one deliberate split

**Files:**

- Modify: `src/DataNet.Embeddings/Search/VectorMath.cs`

**Depends on:** Task 4.
**Produces:** the behavioural difference the ADR has to declare.

- [x] **Step 1: Guard the SIMD path**

Wrap the `Vector<T>` loop in `#if NET5_0_OR_GREATER` and add a scalar loop for the
other leg. The span-based `Vector<T>` constructor is net-only; `System.Numerics.Vectors`
supplies the type but not that constructor, which is exactly the trap here — the
reference resolves, so the failure is a compile error at the *constructor*, not a
missing type.

- [x] **Step 2: Comment why, in the source**

One sentence naming the constructor. A future reader will otherwise try to delete
the `#if`.

- [x] **Step 3: Both legs build; net10 results unchanged**

```bash
build_all && test_all
```

Expected: green, 158/158, and `git diff --stat tests/oracles/` empty.

---

### Task 6: Verify the package, against the nuspec rather than against hope

**Files:** none modified.

**Depends on:** Task 5.
**Produces:** evidence for the claim the README will make.

- [x] **Step 1: Pack**

```bash
rm -rf ./artifacts
for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy; do
  dotnet pack "$p" -c Release -o ./artifacts || break
done
```

- [x] **Step 2: Read what was actually produced**

```bash
cd /tmp && rm -rf nuspec-check && mkdir nuspec-check && cd nuspec-check
unzip -o <repo>/artifacts/DataNet.Text.*.nupkg > /dev/null
unzip -l <repo>/artifacts/DataNet.Text.*.nupkg | grep "lib/"
cat DataNet.Text.nuspec | sed -n '/<dependencies>/,/<\/dependencies>/p'
```

Expected, and each point is a separate pass/fail:

1. `lib/net10.0/DataNet.Text.dll` **and** `lib/netstandard2.0/DataNet.Text.dll`.
2. `<group targetFramework="net10.0" />` — empty.
3. `<group targetFramework=".NETStandard2.0">` carrying `System.Memory` and
   `System.Numerics.Vectors`.
4. **No `PolySharp` dependency anywhere.** If it appears, `PrivateAssets="all"`
   did not take effect and consumers would inherit it.

- [x] **Step 3: Repeat for `DataNet.Embeddings` and `DataNet.Fuzzy`**

`DataNet.Embeddings` additionally carries `Microsoft.ML.OnnxRuntime` in **both**
groups — it is a real runtime dependency, not a polyfill.

---

### Task 7: Record the decision and the gap it leaves

**Files:**

- Create: `docs/decisions/0001-target-framework.md`
- Modify: `README.md`

**Depends on:** Task 6.
**Produces:** the honest version of the reach claim.

- [x] **Step 1: ADR 0001**

Context, the order of preference from D2, the `Dot` split, and — in its own
section — **the verification gap**: the suite targets `net10.0`, so the
`netstandard2.0` assemblies are compile-verified only. Say plainly that "158 tests
pass" does not cover both targets.

- [x] **Step 2: README**

The targets, the single-package claim, and a pointer to ADR 0001. Do not write a
sentence the tests do not support.

- [x] **Step 3: Open the follow-up issue**

"Run the test suite against the netstandard2.0 build" — the work D6 defers. Link
it from the ADR so the gap has an owner rather than a paragraph.

---

### Task 8: Final gate

**Depends on:** Task 7.

- [x] **Step 1: Everything, from clean**

```bash
dotnet clean -c Release && build_all && test_all
dotnet format --verify-no-changes
npx --yes markdownlint-cli2 "**/*.md" "#node_modules"
```

Expected: both frameworks with 0 warnings and 0 errors under warnings-as-errors;
158/158; `dotnet format` clean; markdownlint 0 issues.

- [x] **Step 2: Prove the branch stayed in its lane**

```bash
git diff main --stat -- tests/oracles/          # must be empty
git diff main -- src | grep -c "pragma warning disable S"   # must be 0
```

The second check is what keeps #7's Sonar cleanup out of this diff. An empty
oracle diff is what proves no fallback changed behaviour.

- [x] **Step 3: Commit**

```bash
git add -A
git commit -m "Multi-target netstandard2.0 alongside net10.0"
```
