# #63 The control-piece filter, and the test that could not fail — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the guarantee real — `Encode` never emits a control piece, on a vocabulary whose ids the old guess would have got wrong — and make the parity claim in `docs/equivalence.md` precise about what it covers.

**Architecture:** No production change; #66 already replaced the id guess with `Types`-driven `IsMatchable`. What is missing is a test that can fail and an oracle that can contradict. The regression test gains an input containing the marker; the corpus gains XLM-R's own vocabulary, re-emitted at the fairseq ids with the normalizer set to `identity`; a fixture-free mirror states the same property in microseconds.

**Tech Stack:** xunit, `sentencepiece`, HuggingFace `tokenizers`, a pinned SHA-256 vocabulary fetch.

**Spec:** `2026-08-06_0063_sentencepiece-control-piece-filter-hardcoded-to-ids-0-1-2.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `fix/63-vacuous-control-piece-test`. Never commit to `main`.
- **No production code change.** If one seems needed, #66 left a bug in — stop and
  report rather than fixing here.
- **No model weights** (ADR 0003). Vocabulary only, pinned by SHA-256, attributed.
- A test is not accepted until it has been **seen to fail**.

### Reusable verification commands

```bash
cd <repo>

build_all() { dotnet build -c Release; }
test_sp()   { dotnet test -c Release --filter "FullyQualifiedName~SentencePiece|FullyQualifiedName~XlmRoberta"; }
test_all()  { dotnet test -c Release; }
```

---

### Task 1: Prove the existing test is vacuous

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the finding this branch exists for — and the reason not to trust a
green suite here.

- [x] **Step 1: Read the test**

```bash
grep -n -A12 "Controls_outside_the_first_three_ids" tests/DataNet.Embeddings.Tests/SentencePieceTokenizerTests.cs
```

It encodes `"as"` and asserts `"<s>"` is not among the tokens.

- [x] **Step 2: See why it cannot fail**

A SentencePiece piece matches only where its literal characters occur. `"as"`
preprocesses to `"▁as"`, which **contains no `<`**. The marker could not be
emitted either way.

- [x] **Step 3: Prove it by mutation, not by reading**

```bash
# Temporarily force IsMatchable to return true — remove the exclusion entirely.
test_sp 2>&1 | tail -5
```

Expected: **still green**. The only test that catches the break is
`SentencePieceModelLoaderTests.Control_and_unknown_pieces_are_excluded_from_matching`,
which restates `IsMatchable`'s own logic against `Types` — it does not test the
end-to-end property.

Revert the mutation.

- [x] **Step 4: Name what is uncovered**

*`Encode` never emits a control piece.* Nothing asserts it.

---

### Task 2: Prove the oracle cannot see it either

**Files:** none modified.

**Depends on:** Task 1.

- [x] **Step 1: Inspect the fixture the parity claim rests on**

```bash
ls -l tests/oracles/tiny_sp.model
python3 -c "
import sentencepiece as spm
sp = spm.SentencePieceProcessor(model_file='tests/oracles/tiny_sp.model')
print([(i, sp.id_to_piece(i)) for i in range(5)])
"
```

Expected: 984 bytes, self-trained, `<unk>`/`<s>`/`</s>` at 0/1/2 — **exactly the
layout the id guess got right**.

- [x] **Step 2: State the consequence**

"Exact parity" in `docs/equivalence.md` is asserted over a fixture **unable to
contradict it**. That is the same class of problem as Task 1, one level up.

---

### Task 3: Make the regression test able to fail

**Files:**

- Modify: `tests/DataNet.Embeddings.Tests/SentencePieceTokenizerTests.cs`

**Depends on:** Task 2.

- [x] **Step 1: Feed it an input containing the marker**

`"a<s>s"` rather than `"as"`, so the marker's own string is present.

- [x] **Step 2: Assert on `Ids` as well as on tokens**

The control id must be absent from `Ids`.

- [x] **Step 3: Add the same guarantee for the unknown piece**

`The_unknown_piece_is_never_matched_as_text` — where matching as text would let a
document name its own unknown token.

- [x] **Step 4: Mutate again, and confirm both now fail**

```bash
# IsMatchable => true
test_sp 2>&1 | grep -c "Failed"
```

Expected: non-zero. A regression test that has never been seen red is not known to
work. Revert.

---

### Task 4: A fixture-free mirror of the property

**Files:**

- Create: `tests/DataNet.Embeddings.Tests/XlmRobertaFairseqTests.cs` (the toy case)

**Depends on:** Task 3.

- [x] **Step 1: `A_fairseq_layout_matches_none_of_its_five_markers`**

18 pieces, microseconds. Markers scored **0 — the best score in the vocabulary** —
and every input character covered by a normal piece.

That construction is what makes the assertion sharp: an id from the marker set in
the output means the marker was matched as text, and nothing else.

- [x] **Step 2: State it separately from the corpus replay**

So it survives a regenerated corpus. A property that only exists inside a
generated fixture disappears the day the fixture changes.

---

### Task 5: An oracle that can contradict the claim

**Files:**

- Create: `tools/fetch_xlmr_vocab.py`
- Create: `tests/oracles/xlmr_fairseq.model`, `tests/oracles/xlmr_fairseq.json`
- Modify: `tools/generate_oracles.py`, `tools/requirements.txt`, `tools/README.md`

**Depends on:** Task 4.

- [x] **Step 1: Understand why the stock file will not do — two reasons, neither cosmetic**

```bash
python3 -c "
import sentencepiece as spm
sp = spm.SentencePieceProcessor(model_file='<stock sentencepiece.bpe.model>')
print([(i, sp.id_to_piece(i)) for i in range(4)])
"
```

- The stock model is laid out `<unk>`=0, `<s>`=1, `</s>`=2, with **no `<pad>` and
  no `<mask>`**. The fairseq numbering everyone meets lives in HuggingFace's
  wrapper, not in the file — so committing it would add 5 MB exercising the case
  that already worked.
- It is trained with **`nmt_nfkc`**, which `SentencePieceModelLoader` refuses on
  purpose. Left alone it would not load at all.

- [x] **Step 2: Re-emit the vocabulary**

Same 250 000 pieces, scores and types, at the ids HuggingFace gives them
(`<s>`=0, `<pad>`=1, `</s>`=2, `<unk>`=3, `<mask>`=250001), normalizer `identity`.

- [x] **Step 3: Generate the reference from that same file**

Both sides then normalize identically, so the comparison isolates the thing under
test rather than the normalizer.

- [x] **Step 4: Include inputs that can fail**

`un texte avec <s>, </s>, <pad>, <unk> et <mask> dedans`, plus Latin, Cyrillic and
Japanese. An input without a marker in it tests nothing here.

- [x] **Step 5: Pin, attribute, and state the cost**

SHA-256 pin; `--check` mode; MIT attribution in `THIRD-PARTY-NOTICES.md`.

**5.3 MB — by far the largest file in the repository**, the next being 984 bytes.
Say so in the pull request. A fixture that size is a decision, not a detail.

---

### Task 6: Make the documented claim match what is proven

**Files:**

- Create: `docs/decisions/0013-sentencepiece-parity-scope.md`
- Modify: `docs/equivalence.md`

**Depends on:** Task 5.

- [x] **Step 1: ADR 0013 — parity over the XLM-R *vocabulary*, not the *pipeline***

The stock pipeline's `nmt_nfkc` normalizer is refused by the loader, so the claim
cannot cover it. State the boundary rather than letting "exact parity" imply more.

- [x] **Step 2: Update the `equivalence.md` rows**

They currently claim unqualified exact parity. Replace with what the oracle
actually backs.

- [x] **Step 3: Retract the `minScore` criterion in the open**

The issue asked for `minScore` to be initialised to `double.MaxValue`. It is
wrong; say so with the reasoning. An acceptance criterion quietly dropped gets
re-raised by the next reader.

- [x] **Step 4: Full gate**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
git diff --stat -- src/   # must be empty: no production change
```

- [x] **Step 5: Commit**

```bash
git commit -m "Make the control-piece test able to fail"
git commit -m "Give the oracle a vocabulary the id guess would fail"
git commit -m "Assert the fairseq layout, on the real vocabulary and on a toy one"
```
