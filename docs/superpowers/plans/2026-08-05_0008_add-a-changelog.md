# #8 Changelog and 0.2.0 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `CHANGELOG.md` covering `0.1.0` (reconstructed) and `0.2.0` (written from the merged work), with `Version` moved to `0.2.0` — and **no tag pushed**.

**Architecture:** Two commits, two concerns. The changelog plus the version bump; then the shipped notices, corrected while cutting the release is the moment anyone reads them. Publication is deliberately left undone.

**Tech Stack:** Markdown (Keep a Changelog 1.1.0), SemVer, MSBuild `Directory.Build.props`, `dotnet pack`.

**Spec:** `2026-08-05_0008_add-a-changelog.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. **Do not tag** — that is
  this branch's defining constraint, not a formality: a `v0.2.0` tag publishes
  irreversibly to nuget.org.
- Branch `release/8-changelog-0.2.0`. Never commit to `main`.
- **No source change.** If writing an entry reveals a defect, open an issue; do
  not fix it here. A release branch that also changes behaviour describes a
  release that was never tested.
- Every claim in the changelog must be checkable against the history or the
  corpora. "Faster" without a number, or a number without its limit, does not go
  in.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
test_all()  { dotnet test -c Release; }
mdl()       { npx --yes markdownlint-cli2 "**/*.md" "#node_modules"; }
```

---

### Task 1: Reconstruct what 0.1.0 actually was

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the content of the `0.1.0` section, from evidence rather than memory.

- [ ] **Step 1: Find the tag and what preceded it**

```bash
git log --oneline v0.1.0 | tail -40
git log --oneline v0.1.0..HEAD | wc -l
```

Expected: the second number is the count of pull requests to summarise for
`0.2.0` — 23 at the time of writing.

- [ ] **Step 2: List the public surface at 0.1.0**

```bash
git grep -n "^public " v0.1.0 -- 'src/**/*.cs' | grep -oE "(class|record|enum|struct) \w+" | sort -u
```

`0.1.0`'s section describes what shipped, not what was worked on. This is the
list.

---

### Task 2: Write the changelog

**Files:**

- Create: `CHANGELOG.md`

**Depends on:** Task 1.

- [ ] **Step 1: Header and conventions**

Keep a Changelog + SemVer, and a line saying one version covers all three
packages, released together from the single `Version` in
`Directory.Build.props`. That is true today and stops being true at #64 — say
what is true now.

- [ ] **Step 2: The `0.2.0` section, from the merged work**

Grouped `Added` / `Changed` / `Fixed`. The highlights:

- `netstandard2.0` as a second target — .NET Framework 4.6.1+, Mono, Xamarin,
  Unity
- Spanish, Portuguese, Italian and German Snowball stemmers — 758 frozen
  reference words with English and French
- Blocked Myers: long-string `Levenshtein.Distance` 20–33× faster
- Script injection fixed in the release workflows; actions pinned to SHAs; hashed
  Python lock
- `coverlet.collector` was missing, so **coverage was never actually collected**

- [ ] **Step 3: The Regex timeout goes under `Changed`, not `Fixed`**

Input that previously hung the calling thread now raises
`RegexMatchTimeoutException`. Filing it under *Fixed* hides a behavioural change
from the one reader who needs it — someone whose input now throws.

- [ ] **Step 4: The Levenshtein number carries its limit, in the same sentence**

20–33× on long strings, **and** that the bit-parallel path needs a Latin-1
pattern, so CJK and emoji inputs still take the DP. Verify the claim before
quoting it:

```bash
grep -rn "Latin-1\|latin1\|0xFF" src/DataNet.Text/Distances/Myers.cs | head
```

- [ ] **Step 5: Verify every number**

```bash
python -c "
import json,glob
print(sum(len(json.load(open(f))['cases']) for f in glob.glob('tests/oracles/snowball_*.json')))
"
git log --oneline v0.1.0..HEAD | wc -l
```

Use what these print. A changelog is the one document nobody re-derives later.

---

### Task 3: Move the version

**Files:**

- Modify: `Directory.Build.props`
- Modify: `README.md`

**Depends on:** Task 2.

- [ ] **Step 1: `Version` to `0.2.0`**

- [ ] **Step 2: Justify minor rather than major, in the changelog**

Nothing public removed or renamed; stemmers and the second target are additive;
the performance work is behaviour-preserving and the corpora prove it. If any of
those is false, the version is wrong.

```bash
git diff v0.1.0..HEAD -- src | grep -E "^-\s*public" | head
```

Expected: empty. A removed or renamed public member here means 0.2.0 is the wrong
number.

- [ ] **Step 3: Pack all three at the new version**

```bash
rm -rf ./artifacts
for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy; do
  dotnet pack "$p" -c Release -o ./artifacts
done
ls ./artifacts/*.nupkg
for f in ./artifacts/*.nupkg; do echo "$f"; unzip -l "$f" | grep "lib/"; done
```

Expected: three packages at `0.2.0`, each carrying `lib/net10.0` **and**
`lib/netstandard2.0`.

---

### Task 4: The shipped notices, as a separate commit

**Files:**

- Modify: `LICENSE`
- Modify: `NOTICE`
- Modify: `THIRD-PARTY-NOTICES.md`

**Depends on:** Task 3.

- [ ] **Step 1: Correct the attribution**

These three go inside every package, so they are read by consumers and almost
never by the maintainer. Cutting a release is the moment they get looked at.

- [ ] **Step 2: Confirm each third-party entry still matches a real dependency**

```bash
grep -rn "PackageReference\|PackageVersion" src --include='*.props' --include='*.csproj' | grep -oE 'Include="[^"]+"' | sort -u
```

Every runtime dependency needs an entry; a `PrivateAssets="all"` analyzer or
polyfill does not ship and must not claim to.

- [ ] **Step 3: Separate commit**

Different concern from the changelog, and it reviews better alone.

---

### Task 5: Full gate, and the thing deliberately not done

**Depends on:** Task 4.

- [ ] **Step 1: Everything**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
mdl
```

Expected: clean on both frameworks, 168/168, markdownlint 0 issues across 27
files.

- [ ] **Step 2: Confirm no tag exists on this branch**

```bash
git tag --points-at HEAD
```

Expected: empty. `v0.2.0` triggers the release workflow and publishes to GitHub
Packages, and nuget.org publication is irreversible for a given version. The tag
is the maintainer's call after the merge.

- [ ] **Step 3: Say so in the PR body**

Under a "Not done here" heading, with the reason. A reviewer should not have to
wonder whether it was forgotten.

- [ ] **Step 4: Commit**

```bash
git commit -m "Add the changelog and cut version 0.2.0"
git commit -m "Attribute the project to Cyril BRUNET, and fix the shipped notices"
```
