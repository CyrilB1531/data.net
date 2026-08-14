# #155 — Wiring the comment budget Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Goal:** Clear the 15 findings #134's four zones never covered, then make CI run the guard that would have
caught them.

**Architecture:** Three tasks. Clear the areas nobody owned, fix the one-day-old regression, then add the
guard beside its two siblings in the two CI jobs that already run them.

**Tech Stack:** C# XML documentation and inline comments, GitHub Actions YAML,
`tools/check_comment_length.py`.

**Spec:** `docs/superpowers/specs/2026-08-14_0155_wire-the-comment-budget.md`

## Global Constraints

- Branch `chore/155-wire-the-comment-budget`, rebased on `main` at `347df50` — #154 merged, so this is no
  longer stacked. Do not push, do not open a pull request without asking.
- **No behaviour changes.** The suite stays at **3 197 passing, 0 failed** across eight assemblies, and
  `git status --porcelain tests/oracles/` stays empty.
- **Every `dotnet` invocation goes through `./.dotnet-guarded`**, never bare `dotnet`.
- `dotnet build` gives no analyzer diagnostics without `--no-incremental`. Warnings are errors.
- Budgets: two lines inline, eight lines of prose in XML documentation. **The 9 existing `long-comment:`
  markers are out of scope** — each carries a reason a review accepted, and re-litigating them is not this
  lot's business.
- **Write no ADR and take no ADR number.**
- `dotnet format DataNet.slnx --verify-no-changes` runs once, in the final task.
- Never let a Markdown line begin with `#` followed by a digit — markdownlint reads it as a heading (MD018).
- English everywhere. Commit messages carry no `feat:`/`fix:` prefix and no process prefix.

## The 15 findings

```bash
python3 tools/check_comment_length.py
```

| file | findings |
| --- | ---: |
| `src/Shared/Persistence/JsonArtifact.cs` | 5 |
| `src/Shared/Persistence/Base64Numbers.cs` | 3 |
| `src/Shared/Persistence/ArtifactHeader.cs` | 1 |
| `src/Shared/Persistence/ArtifactIo.cs` | 1 |
| `src/Shared/GlobalUsings.cs`, `src/Shared/RegexDefaults.cs` | 2 |
| `src/DataNet.Fuzzy/Fuzz.cs`, `src/DataNet.Fuzzy/Deduplicator.cs` | 2 |
| `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs` | 1 — the regression |

---

### Task 1: `src/Shared/` — 12 findings

**Files:** `src/Shared/Persistence/` (10), `src/Shared/GlobalUsings.cs`, `src/Shared/RegexDefaults.cs`.

**Depends on:** nothing.

- [ ] **Step 1: This code is compiled into all four packages, which is why it was nobody's**

`src/Shared/` has no package of its own — it is linked into `DataNet.Text`, `DataNet.Embeddings`,
`DataNet.Fuzzy` and `DataNet.Metrics` under `DataNet.Internal`, with a global using. That is why the four
zone issues each swept "their" package and none of them reached it.

- [ ] **Step 2: The persistence blocks are claims about a format**

`JsonArtifact` and `Base64Numbers` document the artifact format's reader and writer: why the encoder is
relaxed, why raw bits rather than JSON numbers, what a non-finite value does. Those are checkable against the
round-trip tests in `tests/DataNet.Text.Tests/Persistence/` and against **ADR 0011**, which is the format's
decision record. Cite the ADR **after opening it** — #152 found a suppression citing decision 0013 for a
claim 0013 never makes.

A claim that raw bits carry NaN past a non-finite check is exactly the shape `ArtifactHardeningTests` exists
to pin: name that test rather than restating the reasoning.

- [ ] **Step 3: Triage, verify, commit**

Same five rules as the four merged sweeps: cite what answers it, run it once and cite the output, or cut it
as the opinion it is; what survives and does not fit moves to the type's own documentation, and the block
keeps one line naming where it went.

```bash
./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental   # 0 warnings
./.dotnet-guarded dotnet test DataNet.slnx -c Release                      # 3 197 passing
git commit -m "Sweep the shared persistence, which belonged to no package's zone"
```

---

### Task 2: `DataNet.Fuzzy` and the regression — 3 findings

**Files:** `src/DataNet.Fuzzy/Fuzz.cs`, `src/DataNet.Fuzzy/Deduplicator.cs`,
`src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs`.

**Depends on:** Task 1.

- [ ] **Step 1: `DataNet.Fuzzy`'s two blocks name rapidfuzz**

`Fuzz.cs`'s 11-line header and `Deduplicator.cs`'s 9 document what each ratio matches in `fuzz.*`. The
corpora that prove it are in `tests/oracles/`, and `docs/guides/migrating-from-rapidfuzz.md` is the
user-facing home #153 used for exactly this subject. Cite; do not restate.

- [ ] **Step 2: The regression is one line over, and its comment is good**

`BpeTokenizer.cs:132` says a merge pair listed twice keeps its **last** occurrence, "which is what the
reference does", and names `tests/oracles/bpe_duplicate_merge.json`, model `duplicate`. That citation is
what the whole sweep asked for — it arrived in `708982f`, the fix for issue #160, after #152 swept the file.

**Keep the claim and the citation; lose one line.** Do not cut it: the corpus case it names is the only
thing separating "last wins" from "first wins", and #160 exists because nothing recorded that.

- [ ] **Step 3: Verify and commit**

```bash
./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental
./.dotnet-guarded dotnet test DataNet.slnx -c Release
python3 tools/check_comment_length.py    # must print nothing at all
git commit -m "Sweep DataNet.Fuzzy, and bring the day-old regression inside the budget"
```

---

### Task 3: Wire it, and say that CI runs it

**Files:** `.github/workflows/ci.yml`, `CONTRIBUTING.md`, `tools/README.md`.

**Depends on:** Tasks 1-2. **The guard must print nothing before this task adds it**, or the job it adds
fails on its own first run — the outcome #150 avoided by shipping the tool unwired.

- [ ] **Step 1: Add it beside its siblings, in both jobs**

`ci.yml` runs `check_machine_paths.py` and `check_version_floor.py` in the `Lint` job and again in the
Windows job. Add the guard in both, immediately after the machine-path step, matching each site's own
interpreter — `python3` where its neighbours use `python3`, `python` where they use `python`.

```yaml
      - name: Comment budgets
        run: python3 tools/check_comment_length.py
```

Neither job installs dependencies; this guard imports nothing beyond the standard library, which is why it
belongs with those two and not with the jobs that need a venv.

- [ ] **Step 2: Print the marker count, and do not fail on it**

Add `--report` as a second step, or extend the same one, so the log carries the number. **Measured baseline:
9 markers.** It must not fail the job when that number moves — a marker is a judgment held to a
`#pragma warning disable`'s bar, and failing on the count would turn it into a quota.

- [ ] **Step 3: One sentence each in `CONTRIBUTING.md` and `tools/README.md`**

Both already describe the guard. What changes is that it is enforced, so each gains a sentence saying CI runs
it. **No new section**, and no restating what the other says — #156 has just been through these two files
for exactly that.

- [ ] **Step 4: Final verification**

```bash
cd <repo>
git status --porcelain                                                    # empty
python3 tools/check_comment_length.py; echo "guard=$?"                    # nothing, 0
python3 tools/check_comment_length.py --report | tail -3
./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental
./.dotnet-guarded dotnet format DataNet.slnx --verify-no-changes
./.dotnet-guarded dotnet test DataNet.slnx -c Release                     # 3 197 passing
python3 tools/check_version_floor.py; python3 tools/check_machine_paths.py
.venv-oracles/bin/python -m pytest tools/tests -q | tail -1
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md" > /dev/null
```

Then check the YAML parses as GitHub will read it — `python3 -c "import yaml,sys; yaml.safe_load(open('.github/workflows/ci.yml'))"` — and report the two step names as they will appear in the log.

- [ ] **Step 5: Stop and report.** Do not push, do not open a pull request.

---

## Self-Review

**Spec coverage.** D1 → Tasks 1 and 2. D2 → Task 3 Step 1. D3 → Task 3 Step 2. D4 → nothing, deliberately:
the spec records that the markdownlint glob stays as it is and why. D5 is satisfied by history rather than by
a task — #154 merged, so the branch is no longer stacked.

**Placeholders.** Each task names what is specific to it: the code that belonged to no package, the two
files that name rapidfuzz, the regression whose citation is the thing to keep. `<repo>` stands for a path
that must not be written into a committed file.

**Type consistency.** No code changes. The 15 findings and the 9 markers were measured with
`check_comment_length.py` on this branch, rebased on `main` at `347df50`.
