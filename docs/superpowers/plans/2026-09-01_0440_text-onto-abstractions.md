# `Lodestar.Text` onto `Lodestar.Abstractions` (step B) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `Lodestar.Text` 0.5.0 stops declaring `CsrMatrix` and consumes `Lodestar.Abstractions` 0.1.0 instead — a breaking source change, taken deliberately — leaving exactly one `CsrMatrix` in the repository and unblocking step C.

**Architecture:** The second of the spec's three steps. Step A shipped `Lodestar.Abstractions` 0.1.0 and left three things temporary: two copies of the type, a `sonar.cpd.exclusions` line naming one of them, and a `covered` map that is empty. This step ends all three. Step C then adds `Lodestar.Decomposition` against the pair.

**Tech Stack:** C# / .NET (`net10.0;netstandard2.0`), xunit.

**Spec:** [`docs/superpowers/specs/2026-09-01_0440_decomposition-truncated-svd-and-nmf.md`](../specs/2026-09-01_0440_decomposition-truncated-svd-and-nmf.md)

**Branch:** `feat/440-text-onto-abstractions` (already created off `main`).

## Global Constraints

- **`Lodestar.Abstractions` 0.1.0 is on nuget.org and restores.** Proven, not assumed: a throwaway project with `nuget.org` as its only source and an empty `NUGET_PACKAGES` restored both `0.1.0` packages and resolved `lib/net10.0` and `lib/netstandard2.0`. Note the flat container's `index.json` still answered **404** at the time — the packages are unlisted/propagating, which hides them from search but not from a by-version restore. **Task 2 step 3 depends on that 404 being gone**; check before doing it.
- **The break is the point, and it is source-only in one direction.** Every `using Lodestar.Text.Vectorization;` that touches `CsrMatrix` or `SparseNorm` needs `using Lodestar.Abstractions;` as well or instead. No type-forward: decision 0071 refused it.
- **Two target frameworks, one public API.** `Lodestar.Text` keeps `net10.0;netstandard2.0` and keeps returning `CsrMatrix` from its vectorizers — only the assembly the type lives in changes.
- **`Lodestar.Text` 0.5.0.** Declared only in `src/Lodestar.Text/Version.props`. `Lodestar.Fuzzy`'s floor on `Lodestar.Text` stays **0.4.0**: Fuzzy needs no new API, and raising a floor without a reason asks consumers for an upgrade that buys them nothing.
- **`src/` references packages, never projects.** The new edge is a `PackageVersion` in `src/Directory.Packages.props` plus a `PackageReference` in `Lodestar.Text.csproj`. `LodestarUseProjectRefs` is a local loop; CI asserts the default path.
- **Warnings are errors**; `SonarAnalyzer.CSharp` at `AnalysisMode=All`, `AnalysisLevel` 10.0.
- **Comments:** two lines inline, eight of prose in XML documentation; `long-comment:` past that and it stays exceptional.
- Everything written in English. Samples print through `Inv.*`.

## What step A left for this step to close

| left behind | where | closed by |
| --- | --- | --- |
| a second `CsrMatrix` | `src/Lodestar.Text/Vectorization/CsrMatrix.cs` | task 1 |
| `sonar.cpd.exclusions` naming it | `.github/workflows/sonarcloud.yml` | task 2 |
| `Lodestar.Abstractions` in `check_sample_coverage`'s `WAITING` | `tools/check_sample_coverage.py` | task 2 |
| an empty `covered` map | `docs/wiki-map.json` | task 3 |
| seven reference pages under the wrong package | `docs/reference/text/vectorizers/` | task 3 |
| two members with no reference entry | `Multiply(block, columnCount)`, `TransposeMultiply` | task 3 |

---

### Task 1: Move the type, and make everything compile against its new home

**Files:**

- Delete: `src/Lodestar.Text/Vectorization/CsrMatrix.cs`
- Modify: `src/Lodestar.Text/Version.props`, `src/Lodestar.Text/Lodestar.Text.csproj`, `src/Directory.Packages.props`
- Modify: `src/Lodestar.Text/Vectorization/CountVectorizer.cs`, `HashingVectorizer.cs`, `TfidfTransformer.cs`, `TfidfVectorizer.cs`, `src/Lodestar.Text/Persistence/VectorizerOptionsJson.cs`
- Modify: `tests/Lodestar.Text.Tests/Vectorization/CountVectorizerOracleTests.cs`, `HashingVectorizerOracleTests.cs`, `TfidfVectorizerOracleTests.cs`, `CsrMatrixValidationTests.cs`, `tests/Lodestar.Text.Tests/Persistence/PersistenceOverloadTests.cs`, `VectorizerPersistenceTests.cs`
- Modify: `bench/Lodestar.Text.Benchmarks/StopWordBenchmarks.cs`, `VectorizerBenchmarks.cs`

**Interfaces:**

- Consumes: `Lodestar.Abstractions.CsrMatrix` and `Lodestar.Abstractions.SparseNorm`, 0.1.0, from nuget.org — including `internal static CsrMatrix.CreateUnchecked(...)`, reachable because `Lodestar.Abstractions` ships `InternalsVisibleTo("Lodestar.Text")`.
- Produces: a `Lodestar.Text` whose public surface is unchanged except that `CsrMatrix` and `SparseNorm` are now types of another assembly. `CsrMatrixValidationTests` moves out of this package in task 3's cleanup — see step 6.

- [ ] **Step 1: Take the new edge**

```bash
python3 - <<'PY'
import pathlib

p = pathlib.Path("src/Directory.Packages.props")
t = p.read_text()
old = '    <PackageVersion Include="Lodestar.Text" Version="0.4.0" />\n'
new = ('    <PackageVersion Include="Lodestar.Abstractions" Version="0.1.0" />\n'
       '    <PackageVersion Include="Lodestar.Text" Version="0.4.0" />\n')
assert old in t
p.write_text(t.replace(old, new, 1))

p = pathlib.Path("src/Lodestar.Text/Lodestar.Text.csproj")
t = p.read_text()
old = "  <ItemGroup>\n    <InternalsVisibleTo Include=\"Lodestar.Text.Tests\" />"
new = ('  <!-- CsrMatrix and SparseNorm live here since 0.5.0 (decision 0071). The floor is\n'
       '       pinned in src/Directory.Packages.props, like every other src/ reference. -->\n'
       '  <ItemGroup>\n'
       '    <PackageReference Include="Lodestar.Abstractions" />\n'
       '  </ItemGroup>\n\n'
       "  <ItemGroup>\n    <InternalsVisibleTo Include=\"Lodestar.Text.Tests\" />")
assert old in t
p.write_text(t.replace(old, new, 1))

p = pathlib.Path("src/Lodestar.Text/Version.props")
t = p.read_text()
assert "<LodestarTextVersion>0.4.0</LodestarTextVersion>" in t
p.write_text(t.replace("<LodestarTextVersion>0.4.0</LodestarTextVersion>",
                       "<LodestarTextVersion>0.5.0</LodestarTextVersion>", 1))
PY
```

Also update the comment block above `src/Directory.Packages.props`'s `ItemGroup`: it explains one floor ("`Lodestar.Fuzzy` depends on `Lodestar.Text`") and there are now two. Say that both floors follow the same rule, and that `Lodestar.Text → Lodestar.Abstractions` is the edge decision 0071 added.

- [ ] **Step 2: Delete the copy, and watch the build tell you where it was used**

```bash
git rm src/Lodestar.Text/Vectorization/CsrMatrix.cs
dotnet build src/Lodestar.Text/Lodestar.Text.csproj -c Release 2>&1 | grep -E "error CS" | sed 's/\[.*//' | sort -u
```

Expected: `CS0246`/`CS0103` naming `CsrMatrix` and `SparseNorm` in the four vectorizer files and `VectorizerOptionsJson.cs`. That list **is** the work of the next step; if a file you did not expect appears, read it before editing it.

- [ ] **Step 3: Add the using to every file the compiler named**

Each of these files gets `using Lodestar.Abstractions;` in its using block, in alphabetical position. They are file-scoped-namespace files whose namespace is `Lodestar.Text.Vectorization` or `Lodestar.Text.Persistence`, so the type no longer resolves implicitly.

```bash
for f in src/Lodestar.Text/Vectorization/CountVectorizer.cs \
         src/Lodestar.Text/Vectorization/HashingVectorizer.cs \
         src/Lodestar.Text/Vectorization/TfidfTransformer.cs \
         src/Lodestar.Text/Vectorization/TfidfVectorizer.cs \
         src/Lodestar.Text/Persistence/VectorizerOptionsJson.cs; do
  grep -q "^using Lodestar.Abstractions;" "$f" || \
    python3 - "$f" <<'PY'
import pathlib, sys
p = pathlib.Path(sys.argv[1])
lines = p.read_text().split("\n")
usings = [i for i, l in enumerate(lines) if l.startswith("using ")]
directive = "using Lodestar.Abstractions;"
if usings:
    at = next((i for i in usings if lines[i] > directive), usings[-1] + 1)
else:
    at = 0
lines.insert(at, directive)
p.write_text("\n".join(lines))
PY
done
dotnet build src/Lodestar.Text/Lodestar.Text.csproj -c Release 2>&1 | tail -4
```

Expected: `0 Warning(s)`, `0 Error(s)`, both targets. A `CS0122` on `CreateUnchecked` means the `InternalsVisibleTo` did not take — check that the restored package is 0.1.0 and not a stale cache entry.

- [ ] **Step 4: Do the same for tests and benchmarks**

```bash
dotnet build Lodestar.slnx -c Release 2>&1 | grep -E "error CS" | sed 's/(.*//' | sort -u
```

Add `using Lodestar.Abstractions;` to each file named, the same way. The expected list is the six test files and the two benchmark files in this task's **Files** block; anything else, read first.

- [ ] **Step 5: Run everything**

```bash
dotnet test Lodestar.slnx -c Release 2>&1 | grep -E "Passed!|Failed!" | sort -u
```

Expected: twelve assemblies, every one green, and the counts unchanged from `main` except where task 3 moves tests. Read the **count**, not the colour.

- [ ] **Step 6: Move `CsrMatrixValidationTests` to the package that owns the type**

`tests/Lodestar.Text.Tests/Vectorization/CsrMatrixValidationTests.cs` tests a constructor that is no longer `Lodestar.Text`'s. Move the file to `tests/Lodestar.Abstractions.Tests/`, change its namespace to `Lodestar.Abstractions.Tests`, and drop any `using Lodestar.Text.Vectorization;` it carries. Then check for overlap with the `CsrMatrixTests` step A wrote — `Arrays_that_do_not_describe_a_matrix_are_refused` covers one case the validation suite covers in full, so delete that one test from `CsrMatrixTests.cs` rather than keeping two spellings of it.

```bash
git mv tests/Lodestar.Text.Tests/Vectorization/CsrMatrixValidationTests.cs \
       tests/Lodestar.Abstractions.Tests/CsrMatrixValidationTests.cs
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~CsrMatrix" 2>&1 | grep -E "Passed!|Failed!"
```

- [ ] **Step 7: Commit**

```bash
git add -A src tests bench
git commit -m "Move Lodestar.Text onto Lodestar.Abstractions' CsrMatrix"
```

---

### Task 2: The graph, the floors, and the three temporary things step A left

**Files:**

- Modify: `tools/check_nuspec_dependencies.py`
- Modify: `tools/check_version_floor.py`
- Modify: `tools/check_sample_coverage.py`
- Modify: `.github/workflows/sonarcloud.yml`
- Modify: `samples/Lodestar.Sample/CsrMatrixSample.cs`, `samples/Lodestar.Sample/Program.cs`, `samples/Lodestar.Sample/PackagingGate.cs`, and the nine other sample files naming the type
- Delete: `samples/Lodestar.Sample/Lot7Abstractions.cs`

**Interfaces:**

- Consumes: the built packages from task 1.
- Produces: `check_version_floor.py` covering **two** floors rather than one, through a table rather than module-level constants.

- [ ] **Step 1: Declare the new edge in the expected graph**

In `tools/check_nuspec_dependencies.py`, beside `TEXT_FLOOR`:

```python
# Must equal Directory.Packages.props' PackageVersion, for the edge decision 0071
# added: Lodestar.Text stopped declaring CsrMatrix and consumes it from here.
ABSTRACTIONS_FLOOR = "0.1.0"
```

and give `TEXT` the edge on both targets:

```python
    TEXT: {
        NET: {ABSTRACTIONS: ABSTRACTIONS_FLOOR},
        NETSTANDARD: {ABSTRACTIONS: ABSTRACTIONS_FLOOR, **POLYFILLS, **PERSISTENCE},
    },
```

Leave `FUZZY`'s `TEXT_FLOOR` at `0.4.0`. Update the module docstring, which says `Lodestar.Fuzzy → Lodestar.Text` is "the only inter-package edge that exists" — there are two now, and the second is the one this release adds.

- [ ] **Step 2: Generalise the floor check to both packages**

`tools/check_version_floor.py` hardcodes one package in five places — `VERSION_PROPS`, `PACKAGE`, the `LodestarTextVersion` element name, the `TEXT_FLOOR` regex, and the messages. Replace those with a table and loop the existing two rules over it:

```python
@dataclass(frozen=True)
class Floor:
    """One package whose declared version, floor and asserted floor must agree."""

    package: str
    version_element: str
    floor_constant: str
    required_by: str


FLOORS = (
    Floor("Lodestar.Text", "LodestarTextVersion", "TEXT_FLOOR", "Lodestar.Fuzzy"),
    Floor("Lodestar.Abstractions", "LodestarAbstractionsVersion", "ABSTRACTIONS_FLOOR",
          "Lodestar.Text"),
)
```

`declared_version`, `floor_version`, `asserted_floor` and `published_versions` each take a `Floor` and use `ROOT / "src" / floor.package / "Version.props"`, `floor.version_element`, and `rf'^{floor.floor_constant} = "([^"]+)"'`. `main` loops, collecting failures across both rather than stopping at the first. The docstring's three bullets become one sentence naming the table.

- [ ] **Step 3: Prove `--check-feed` passes before pushing**

```bash
python3 tools/check_version_floor.py --check-feed
```

This is the step with an external dependency. `published_versions` reads
`https://api.nuget.org/v3-flatcontainer/lodestar.abstractions/index.json`, which answered **404** while the package was still propagating even though `dotnet restore` already resolved it. If it still 404s, the check reports the floor as unpublished and **CI will fail the same way** — that is a real blocker to state, not to work around by weakening the check. Confirm with:

```bash
curl -sS https://api.nuget.org/v3-flatcontainer/lodestar.abstractions/index.json
```

- [ ] **Step 4: Retire step A's three temporary things**

```bash
python3 - <<'PY'
import pathlib, re

# The CPD exclusion and the comment that named its removal condition.
p = pathlib.Path(".github/workflows/sonarcloud.yml")
t = p.read_text()
t = t.replace(' \\\n            /d:sonar.cpd.exclusions="src/Lodestar.Abstractions/CsrMatrix.cs"', "", 1)
t = re.sub(r"      # sonar\.cpd\.exclusions holds exactly one file.*?\n(?=      - name: Build the solution\n)",
           "", t, count=1, flags=re.S)
assert "cpd.exclusions" not in t
p.write_text(t)

# The package is converted now: the name CsrMatrixSample.cs is free.
p = pathlib.Path("tools/check_sample_coverage.py")
t = p.read_text()
t = t.replace('CONVERTED = ["Lodestar.Text", "Lodestar.Conformal"]',
              'CONVERTED = ["Lodestar.Text", "Lodestar.Conformal", "Lodestar.Abstractions"]', 1)
t = re.sub(r"# Lodestar\.Abstractions waits one release.*?\n# CsrMatrixSample\.cs.*?\n", "", t, count=1)
t = t.replace(', "Lodestar.Abstractions"]\nWAITING', "]\nWAITING")
t = t.replace('WAITING = ["Lodestar.Fuzzy", "Lodestar.Embeddings", "Lodestar.Metrics", "Lodestar.Abstractions"]',
              'WAITING = ["Lodestar.Fuzzy", "Lodestar.Embeddings", "Lodestar.Metrics"]', 1)
p.write_text(t)
PY
grep -n "cpd.exclusions" .github/workflows/sonarcloud.yml; echo "(no output = gone)"
grep -n "CONVERTED\|^WAITING" tools/check_sample_coverage.py
python3 -c "import yaml,pathlib; yaml.safe_load(pathlib.Path('.github/workflows/sonarcloud.yml').read_text()); print('sonarcloud.yml parses')"
```

- [ ] **Step 5: Fold `Lot7Abstractions` back into the per-class sample**

`Lot7Abstractions.cs` existed because `CsrMatrixSample.cs` was taken by `Lodestar.Text`'s copy. It is not any more. Move the two calls only `Lot7Abstractions` makes — `Multiply(block, columnCount)` and `TransposeMultiply(block, columnCount)` — into `CsrMatrixSample.cs`, keeping that file's existing shape and its `Inv.*` formatting; delete `Lot7Abstractions.cs` and its `Run()` call in `Program.cs`. Keep the `Lodestar.Abstractions : {FrameworkOf(typeof(CsrMatrix))}` line and the `typeof(CsrMatrix).Assembly` entry in `PackagingGate` — the assembly is still packaged, and now it is the only place the type lives.

Then add `using Lodestar.Abstractions;` to the sample files the build names, exactly as task 1 step 3 did.

- [ ] **Step 6: Pack, and check the graph and the gates**

```bash
rm -rf ./artifacts .nuget-sample
for p in src/Lodestar.Abstractions src/Lodestar.Text src/Lodestar.Embeddings src/Lodestar.Fuzzy src/Lodestar.Metrics src/Lodestar.Conformal; do
  dotnet pack "$p" -c Release -o ./artifacts || break
done
python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
NUGET_PACKAGES="$PWD/.nuget-sample" dotnet run -c Release --project samples/Lodestar.Sample
python3 tools/check_sample_coverage.py
python3 tools/check_sample_culture.py
```

Expected: `Lodestar.Text 0.5.0` in the list with its new edge accepted; the sample ending `OK`; the coverage checker naming `Lodestar.Text, Lodestar.Conformal, Lodestar.Abstractions` as converted with none waiting on a `CsrMatrixSample.cs`.

**The sample restores `Lodestar.Text` from `./artifacts`, so it consumes the 0.5.0 you just packed and the 0.1.0 `Lodestar.Abstractions` from nuget.org.** An `NU1605` downgrade error here means the local `Lodestar.Text` and the feed disagree — repack rather than pinning around it.

- [ ] **Step 7: Commit**

```bash
git add tools .github/workflows samples
git commit -m "Take the new edge into the graph, and retire step A's three stopgaps"
```

---

### Task 3: Move the documentation to the package that owns the type

**Files:**

- Move: `docs/reference/text/vectorizers/csrmatrix.md`, `csrmatrix-multiply.md`, `csrmatrix-normalizerows.md`, `csrmatrix-rowl1norm.md`, `csrmatrix-rowl2norm.md`, `csrmatrix-todense.md`, `sparsenorm.md` → `docs/reference/abstractions/sparse/`
- Create: `docs/reference/abstractions/sparse.md`, `docs/reference/abstractions/sparse/csrmatrix-transposemultiply.md`
- Modify: `docs/wiki-map.json`, `docs/reference/text/vectorizers.md`, the twelve `docs/reference/text/vectorizers/*.md` pages that name the type, `docs/guides/vectorization.md`, `docs/equivalence.md`, `README.md`, `docs/migration/pandas.md`
- Modify: `tests/Lodestar.Abstractions.Tests/*.csproj` and its `.NetStandard` twin, `CHANGELOG.md`

**Interfaces:**

- Consumes: the assembly from task 1 — the pages are checked against it, so a signature that drifted fails here.
- Produces: nothing later tasks read. This is the task that makes the branch green.

- [ ] **Step 1: Move the seven pages and repoint their relative links**

```bash
mkdir -p docs/reference/abstractions/sparse
for f in csrmatrix csrmatrix-multiply csrmatrix-normalizerows csrmatrix-rowl1norm \
         csrmatrix-rowl2norm csrmatrix-todense sparsenorm; do
  git mv "docs/reference/text/vectorizers/$f.md" "docs/reference/abstractions/sparse/$f.md"
done
grep -rn "](\.\./\|](docs/" docs/reference/abstractions/sparse/ | head -30
```

Every page sat three directories deep under `docs/` and still does, so `../../../equivalence.md` and `../../../decisions/…` still resolve. What does **not** is a link to a sibling that stayed behind — a `[CountVectorizer](countvectorizer.md)` now needs `../../text/vectorizers/countvectorizer.md`. The grep above lists every relative link in the moved pages; fix each, then verify none is broken:

```bash
python3 - <<'PY'
import pathlib, re
bad = []
for page in sorted(pathlib.Path("docs/reference/abstractions/sparse").glob("*.md")):
    for target in re.findall(r"\]\((?!https?:)([^)#]+)", page.read_text()):
        if not (page.parent / target).resolve().exists():
            bad.append(f"{page}: {target}")
print("\n".join(bad) or "every relative link resolves")
PY
```

- [ ] **Step 2: Write the index and the two missing entries**

`docs/reference/abstractions/sparse.md` is the index the directory needs — the reference gate loads `<directory>.md` beside the directory it is given. Model it on `docs/reference/conformal/prediction.md`: what the package is for, why a sparse matrix rather than a dense one, and a member table linking every entry.

`csrmatrix-transposemultiply.md` is a new entry, in the layout of `docs/reference/metrics/regression/maxerror-score.md`: an H1 naming the member, one sentence, `<!-- docs-declaration -->`, the signature fence, then **Parameters**, **Returns**, **Exceptions**, **Example**, **Remarks**, **Applies to**, **See also**. Its example fence is **executed**, so its `// =>` values must be what the code prints — the sample already prints `transposed · 2x2 = [1.000, 0.500, 6.000, 4.500, 2.000, 1.000]` for the matrix `[[1,0,2],[0,3,0]]` and the block `[1.0, 0.5, 2.0, 1.5]`, which is a verified pair to build the entry on.

`csrmatrix-multiply.md` gains the second overload. **The declaration block must list exactly the overloads reflection reports**, and the gate groups by method *name*, so one fence with both:

```csharp
public static double[] Multiply(ReadOnlySpan<double> vector)
public static double[] Multiply(ReadOnlySpan<double> block, int columnCount)
```

(without `static` — these are instance methods; copy the exact spelling the gate prints when it disagrees.) The page then documents both: the vector product it already described, and the block product, with the sentence that the block form is what a power iteration multiplies by.

- [ ] **Step 3: Declare the pages in the map**

Replace `Lodestar.Abstractions`'s empty entry in `docs/wiki-map.json`:

```json
    "Lodestar.Abstractions": {
      "wiki": "Abstractions",
      "pages": [
        "docs/reference/abstractions/*.md",
        "docs/reference/abstractions/*/*.md"
      ],
      "covered": {
        "Lodestar.Abstractions": "docs/reference/abstractions/sparse"
      }
    }
```

and give the two test projects the copy items they were built without — the `ReferenceDocumentation.cs` link, `docs/reference/abstractions/**/*.md`, `wiki-map.json` and `docs/**/*.md`, copied verbatim from `tests/Lodestar.Conformal.Tests/Lodestar.Conformal.Tests.csproj`'s last `ItemGroup` with the path changed. Then add `tests/Lodestar.Abstractions.Tests/Documentation/ReferenceDocumentationTests.cs`, modelled on Conformal's, with `typeof(CsrMatrix).Assembly` and `"Lodestar.Abstractions"`.

- [ ] **Step 4: Repoint every link that pointed at the old location**

```bash
grep -rn "vectorizers/csrmatrix\|vectorizers/sparsenorm" --include=*.md docs README.md | grep -v superpowers
```

Each hit is one edit. `docs/reference/text/vectorizers.md` needs more than a path change: its lines 61, 66, 93 and 96 describe `CsrMatrix` and `SparseNorm` as members of *this* page's package. Rewrite them to say the vectorizers return a type that now ships in `Lodestar.Abstractions`, link across, and remove the two rows from the member table — a table row on that page is a claim the reference gate checks against `Lodestar.Text`'s assembly, and the type is no longer in it.

- [ ] **Step 5: Run the gate, the snippets and the wiki**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~ReferenceDocumentation" 2>&1 | grep -E "Passed!|Failed!" | sort -u
python3 tools/extract_doc_snippets.py && NUGET_PACKAGES="$PWD/.nuget-sample" dotnet run -c Release --project samples/Lodestar.DocSnippets
python3 tools/build_wiki.py --repo . --out "$(mktemp -d)" --released Lodestar.Abstractions=0.1.0 --released Lodestar.Text=0.5.0
```

Expected: fourteen documentation assemblies green; the snippets running with a count risen by the new entries' fences; the wiki building and the index row for `Lodestar.Abstractions` reading a channel link rather than "no pages yet".

Two failures are routine and both are the pages' fault: a declaration that does not match what reflection reports, and a member named on a non-reference page without a link to its entry anywhere on that page. The second is the one this move creates — `docs/guides/vectorization.md`, `docs/equivalence.md` and `README.md` name the type, and the link rule now judges them against `Lodestar.Abstractions`'s pages.

- [ ] **Step 6: Changelog, with the migration line**

Under `## [Unreleased]`, a `### Lodestar.Text` `#### Changed` entry: one sentence saying `CsrMatrix` and `SparseNorm` moved to `Lodestar.Abstractions`, that consuming code adds `using Lodestar.Abstractions;`, the issue and the commit. Add the matching `### Lodestar.Abstractions` `#### Added` line for the two products' reference entries only if they were not already covered by step A's entry — they were, so do not restate them.

- [ ] **Step 7: The whole sweep, then commit**

```bash
dotnet format Lodestar.slnx --verify-no-changes
dotnet build Lodestar.slnx -c Release
dotnet test Lodestar.slnx -c Release
for c in check_version_floor check_machine_paths check_sample_culture check_sample_coverage \
         check_comment_length check_bench_map check_no_console_writeline; do
  python3 "tools/$c.py" || echo "FAILED: $c"
done
python3 tools/check_repeated_literals.py --base origin/main
python3 tools/check_adr_immutable.py --base origin/main
python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
.venv-oracles/bin/python -m pytest tools/tests -q
git add -A && git commit -m "Document CsrMatrix where it now lives"
```

---

## Before the pull request

- [ ] The sweep above, clean, and the **test count** read rather than the colour.
- [ ] `grep -rn "Lodestar.Text.Vectorization" --include=*.cs src tests bench samples | grep -i csrmatrix` returns nothing.
- [ ] `git ls-files | grep -c "Vectorization/CsrMatrix.cs"` is `0`.
- [ ] The PR body carries `Part of #440`, **not** `Closes` — step C remains.
- [ ] The body states the break in its first paragraph, and what step C needs: `Lodestar.Text/v0.5.0` tagged and dispatched to nuget.org.

## Self-Review

**1. Spec coverage.** The spec's step B row asks for four things: drop `CsrMatrix`, take the `PackageReference`, move the seven reference pages and fill `covered`, and delete the `sonar.cpd.exclusions` line. Task 1 does the first two, task 3 the third, task 2 the fourth. The spec's *Placement* section's breaking-change paragraph is task 1; its blast-radius sentence ("about two dozen source files, the sample, the executed snippets, three ADRs and the README") is covered except the ADRs, which need nothing — verified, not assumed: no `CsrMatrix.<Member>` mention exists outside `docs/reference/`, so the link rule leaves the immutable records alone, and the bare type name is not what it checks.

**2. Placeholders.** Task 3 steps 2 and 4 describe pages by their required shape and by the exact lines to rewrite rather than reproducing seven pages of prose; the layout is named by an existing file to copy, and the one new entry has a verified example value. Task 2 step 2 gives the dataclass and the table but not the five mechanical substitutions that follow from them — those are named individually in the step. Every command is runnable as written.

**3. Type consistency.** `Lodestar.Abstractions.CsrMatrix`, `Lodestar.Abstractions.SparseNorm`, `CsrMatrix.CreateUnchecked`, `Multiply(ReadOnlySpan<double>)`, `Multiply(ReadOnlySpan<double>, int)`, `TransposeMultiply(ReadOnlySpan<double>, int)` — the same names task 1 compiles against, task 2 exercises from the sample and task 3 documents. `ABSTRACTIONS_FLOOR`, `TEXT_FLOOR` and the `Floor` table's `floor_constant` values agree.

**4. The one thing this plan cannot prove in advance.** Task 2 step 3 depends on nuget.org's flat container listing `lodestar.abstractions`, which was still 404 when this plan was written even though `dotnet restore` resolved the package. The step says how to check and says the honest answer if it is still 404 — report the blocker — rather than pretending a workaround exists.
