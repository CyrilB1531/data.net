# #152 — Sweeping DataNet.Embeddings' comments Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Goal:** Bring `src/DataNet.Embeddings`' 124 over-budget comment blocks inside the rule that now counts
them, moving what deserves to survive into the guide or `docs/equivalence.md` and citing it from one line.

**Architecture:** One task per file group, descending by concentration. Each block gets one of three
outcomes — cite a corpus, run it once and cite the output, or cut it as the opinion it is — and every block
that loses a fact keeps a line naming where the fact went.

**Tech Stack:** C# XML documentation and inline comments, Markdown, `tools/check_comment_length.py`.

**Spec:** `docs/superpowers/specs/2026-08-14_0152_sweep-the-embeddings-comments.md`

## Global Constraints

- Branch `docs/152-sweep-embeddings-comments`, based on `main` at `b8d8109`. Do not push, do not open a
  pull request without asking.
- **No behaviour changes.** Comments, `docs/guides/embeddings.md` and `docs/equivalence.md` only. The suite
  is **3 147 passing, 0 failed** across eight assemblies before and after every task, and no byte of
  `tests/oracles/` moves. A different count is a bug this lot introduced.
- **Every `dotnet` invocation goes through `./.dotnet-guarded`**, never bare `dotnet` — another session
  benchmarks on this machine. It blocks with no deadline; let it wait.
- `dotnet build` gives no analyzer diagnostics without `--no-incremental`. Warnings are errors.
- Budgets: **two lines** for an inline comment, **eight lines of prose** for XML documentation
  (`<param>`, `<exception>`, `<summary>` tags do not spend it). `long-comment: <reason>` as the first line
  is allowed where a block earns it, held to a `#pragma warning disable`'s bar.
- **Write no ADR.** An ADR records a choice with a loser; this lot moves findings. A block that turns out to
  hold a real undocumented decision is **reported**, not improvised into a document.
  **`docs/decisions/` numbering is contested right now** — another session has `0023`-`0026` in flight while
  `main` already carries `0023`. Do not add a number to that pile.
- **One fact, one home.** Before moving a fact into the guide or `equivalence.md`, grep both, plus
  `docs/decisions/`, for it already being there. Cite what exists rather than restating it — #156 exists to
  clean up the paragraph that lives twice.
- `dotnet format DataNet.slnx --verify-no-changes` runs **once**, in the final task.
- English everywhere. Commit messages carry no `feat:`/`fix:` prefix and no process prefix.

## How to triage one block

The count is not the job; this is. For each block the counter names:

1. **Read what it claims.** A claim names the reference — HuggingFace, `tokenizers`, scikit-learn, numpy —
   or asserts something about this code ("this can never be null", "the caller has validated").
2. **Ask what would check it.** A corpus case, a `file:line`, a command. If `tests/oracles/` answers it,
   cite the corpus and the case: that is the cheap tier and should be the common one here.
3. **If it is executable and nothing frozen answers it**, run it once and cite the output in the comment, or
   add the corpus case and cite that where the answer deserves freezing.
4. **If nothing reasonable checks it**, it is an opinion. Cut it, or rewrite it as an opinion. Do not
   reformat it into a shorter unverifiable claim.
5. **Then fit the budget.** What survives and does not fit goes to `docs/guides/embeddings.md` (if it
   answers "will this load my file?") or `docs/equivalence.md` (if it says reproduced/refused and how it
   diverges), and the block keeps **one line naming where it went**.

A block cut as tier 3 keeps no pointer — there is nowhere for an opinion to go — and each of those is named
in the task's report so a reviewer can disagree.

## Per-task shape

Every task below has the same five steps. They are written once here rather than repeated four times:

1. **List the blocks**: `python3 tools/check_comment_length.py | grep '<the file or prefix>'`.
2. **Triage and edit**, block by block, by the five rules above.
3. **Verify**: `./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental` (0 warnings), then
   `./.dotnet-guarded dotnet test DataNet.slnx -c Release` — **3 147 passing**, and
   `git status --porcelain tests/oracles/` empty.
4. **Confirm the zone shrank by what you fixed**: the same `grep` prints nothing for the files in scope.
   If markdown changed, run
   `npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"`
   and `python3 tools/extract_doc_snippets.py`.
5. **Commit** the files touched, with a message naming what moved and where, and listing any claim found
   **false** — the issue warns to expect claims that were true of an earlier design.

---

### Task 1: `Persistence/TokenizerJsonLoader.cs` — 26 blocks

**Files:** modify `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs`,
`docs/guides/embeddings.md`, `docs/equivalence.md`.

**Depends on:** nothing. First because it holds the most, and the longest block in the tree.

- [ ] **Step 1: The 61-line type header at `:8` is the one to do first and carefully**

It carries at least one fact written nowhere else: a stock HuggingFace BERT `tokenizer.json` —
`BertPreTokenizer` plus a full `BertNormalizer` — **is refused**, and `VocabTxtLoader` is the route instead.
Verified while writing the spec: `BertPreTokenizer` appears in no ADR, no guide, and no row of
`docs/equivalence.md`.

That paragraph moves to `docs/guides/embeddings.md`, beside what the guide already says about refused
files. The other paragraphs of that block get the same treatment one at a time: the graph being *checked
rather than ignored*, the `decoder` section being accepted unchecked for WordPiece and Unigram but not for
BPE, and unrecognized top-level properties being accepted in silence while an artifact DataNet wrote would
reject them. That last one is an asymmetry with a reason — it belongs in `equivalence.md`'s loader row, not
in the bin.

The `<summary>` and the `<example>` stay where they are.

- [ ] **Step 2: The remaining 25 blocks**, by the triage above.

- [ ] **Step 3-5: Verify, confirm, commit** per the per-task shape.

```bash
git commit -m "Sweep TokenizerJsonLoader's comments, and move what only they recorded"
```

---

### Task 2: `Tokenization/BpeTokenizer.cs` — 20 blocks

**Files:** modify `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs`, and the two documents as needed.

**Depends on:** Task 1 — not technically, but the guide sections it creates are where this task's facts will
want to go, and two tasks inventing two homes for the same subject is what D4 forbids.

- [ ] **Step 1: Expect the highest share of stale claims here**

Eight lots have edited this file — #118, #119, #120, #130, #143, #145, #121, #149 — and the issue warns to
expect claims that were true of an earlier design. Two are already known and fixed, so they are the shape to
look for, not the whole set: a comment said HuggingFace tolerates a merge naming an absent token (it raises
``Token `x` out of vocabulary``), and `Decode`'s remarks scoped a throw to hand-assembled ids when
`Encode`'s own output reached it.

**A claim you find false is fixed and named in the commit message**, not reformatted.

- [ ] **Step 2: The three long blocks are the budget's real subject**

`:584` and `:651` run 33 lines each and `:889` runs 26. Whatever survives them and does not fit belongs in
`equivalence.md`'s BPE rows, which already carry that subject.

- [ ] **Step 3-5: Verify, confirm, commit** per the per-task shape.

```bash
git commit -m "Sweep BpeTokenizer's comments, eight lots deep"
```

---

### Task 3: The three other tokenizers — 23 blocks

**Files:** `Tokenization/SentencePieceTokenizer.cs` (8), `Tokenization/BpePreTokenizer.cs` (8),
`Tokenization/WordPieceTokenizer.cs` (7).

**Depends on:** Tasks 1-2 for the guide's shape.

- [ ] **Step 1: `WordPieceTokenizer.cs:168` is a claim with a live dependent**

It states that normalizing the whole input once and indexing it with raw positions is sound "only because
`ToLowerInvariant` maps char to char and so preserves length — an assumption about the scripts in scope, not
a fact of Unicode". #121 relied on that sentence being true and did **not** extend the mechanism. Keep the
claim; give it its citation.

- [ ] **Step 2: The rest**, by the triage above.

- [ ] **Step 3-5: Verify, confirm, commit** per the per-task shape.

```bash
git commit -m "Sweep the SentencePiece, BPE pre-tokenizer and WordPiece comments"
```

---

### Task 4: The small tokenization types — 26 blocks

**Files:** `Tokenization/` — `BpeVocabulary.cs` (4), `AddedToken.cs` (4), `EncodingOptions.cs` (3),
`SentencePieceVocabulary.cs` (2), `ByteLevelAlphabet.cs` (2), `BatchEncoder.cs` (2),
`AddedTokenScanner.cs` (2), and one each in `SplitBehavior.cs`, `SpecialTokenTemplate.cs`,
`PrecompiledNormalizer.cs`, `ISubwordTokenizer.cs`, `EncodedBatch.cs`, `BpeSplitStep.cs`, `BpePatterns.cs`.

**Depends on:** Tasks 1-3.

- [ ] **Step 1: These are public API documentation, and the budget counts prose only**

A `<param>` per parameter and an `<exception>` per throw do not spend it, so several of these are over
budget on `<remarks>` alone. `AddedToken.cs:71` runs 34 lines and `BpeVocabulary.cs:115` runs 31: both
document flags whose behaviour ADR 0022 already settled, which makes them the cheap tier — cite the ADR
section and cut the retelling.

- [ ] **Step 2-5: Triage, verify, confirm, commit** per the per-task shape.

```bash
git commit -m "Sweep the tokenization types' documentation to prose, not retelling"
```

---

### Task 5: Persistence, search, pooling and ONNX — 29 blocks

**Files:** `Persistence/BpeFilesLoader.cs` (6), `Persistence/SentencePieceModelLoader.cs` (5),
`Persistence/VocabTxtLoader.cs` (4), `Persistence/ProtobufReader.cs` (2),
`Persistence/ArtifactLoadOptions.cs` (1), `Search/EmbeddingIndex.Persistence.cs` (5),
`Search/EmbeddingIndex.cs` (2), `Pooling/Pooling.cs` (2), `Onnx/OnnxTextEmbedder.cs` (2).

**Depends on:** Tasks 1-4.

- [ ] **Step 1: This group's claims are about formats and runtimes, not tokenization**

`OnnxTextEmbedder.cs` names ONNX Runtime eight times and cites nothing; `ProtobufReader.cs` claims wire
formats. Those are tier 2 more often than tier 1 — executable, but with no frozen answer. Run one and cite
the output rather than deleting the claim.

- [ ] **Step 2-5: Triage, verify, confirm, commit** per the per-task shape.

```bash
git commit -m "Sweep the loaders, the index, pooling and the ONNX embedder"
```

---

### Task 6: Final verification

**Depends on:** Tasks 1-5. Nothing is committed unless a gate fails and is fixed.

- [ ] **Step 1: The issue's own "done when"**

```bash
cd <repo>
python3 tools/check_comment_length.py | grep '^src/DataNet.Embeddings/'   # prints nothing
python3 tools/check_comment_length.py | wc -l                            # 632 - 124 = 508
```

- [ ] **Step 2: Every gate**

```bash
git status --porcelain                                                                     # empty
./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental > /tmp/152-fv-b.log 2>&1; echo "build=$?"; grep -E "Avertissement\(s\)|Erreur\(s\)" /tmp/152-fv-b.log
./.dotnet-guarded dotnet format DataNet.slnx --verify-no-changes > /tmp/152-fv-f.log 2>&1;  echo "format=$?"
./.dotnet-guarded dotnet test DataNet.slnx -c Release > /tmp/152-fv-t.log 2>&1;             echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/152-fv-t.log
python3 tools/check_version_floor.py; python3 tools/check_machine_paths.py; echo "floor+paths=$?"
.venv-oracles/bin/python -m pytest tools/tests -q | tail -1
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md" > /dev/null; echo "markdownlint=$?"
python3 tools/extract_doc_snippets.py | tail -2
```

- [ ] **Step 3: The evidence D5 asks for**

Not the counter. Produce the list of **every fact that moved, with its new home**, and the list of **every
block cut as tier 3**, from the task reports. That list is what a reviewer reads; the counter only says the
prose got shorter.

- [ ] **Step 4: Stop and report**

Do not push, do not open a pull request. Report the two lists, the per-assembly test counts, any claim found
false and fixed in passing, and any block that turned out to hold a real undocumented decision — which is a
finding for a new issue, not an ADR written here.

---

## Self-Review

**Spec coverage.** D1 → Tasks 1-5, in the spec's order of concentration. D2 → "How to triage one block",
written once and referenced by every task. D3 → Task 1 Step 1 for the guide, Task 2 Step 2 for
`equivalence.md`, and the Global Constraints for the no-ADR rule and the live numbering collision. D4 → the
Global Constraints' grep rule and Task 2's dependency note. D5 → Task 6 Step 3. D6 → the Global Constraints
and step 3 of every task.

**Placeholders.** The per-task steps are deliberately shared rather than repeated; each task then names what
is specific to it — the block that carries an undocumented fact, the file with eight lots of history, the
claim #121 depends on, the group whose claims are tier 2. `<repo>` stands for a path that must not be
written into a committed file.

**Type consistency.** No code changes, so no signatures. The file names and block counts come from
`check_comment_length.py` on `main` at `b8d8109` and sum to 124: 26 + 20 + 23 + 26 + 29.
