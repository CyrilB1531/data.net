# #91 `TokenizationResult` structural equality — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `TokenizationResult` keep the promise its `record` declaration makes — two results with the same tokens and ids compare equal — without breaking any caller.

**Architecture:** The `record` is kept and `Equals`/`GetHashCode` are written by hand, comparing the two lists element by element. The reason lives on the member, because a hand-written `Equals` on a record reads as redundant otherwise.

**Tech Stack:** C# (net10.0 + netstandard2.0), xunit.

**Spec:** `2026-08-09_0091_tokenizationresult-compares-by-reference-not-by-value.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- **Keep the `record`.** Demoting it to a class breaks `with` and the
  deconstructor for any caller.
- **`Equals` and `GetHashCode` change together, always.** A record with an
  overridden `Equals` and a generated hash is worse than the original defect: it
  misbehaves in a dictionary.
- **No tokenization result changes.** Corpora untouched.
- Both frameworks build.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
test_tok()  { dotnet test -c Release --filter "FullyQualifiedName~Tokeniz"; }
test_all()  { dotnet test -c Release; }

oracles_unchanged() {
  test -z "$(git status --porcelain tests/oracles/)" && echo "ORACLES CLEAN" || echo "ORACLES MOVED"
}
```

---

### Task 1: Demonstrate the defect

**Files:** none modified.

**Depends on:** nothing.
**Produces:** a failing test, before any fix.

- [x] **Step 1: Write the test that should already pass**

Two `TokenizationResult` values built from equal-but-distinct lists must compare
equal.

```bash
test_tok 2>&1 | tail -5
```

Expected: **red**. The synthesised `Equals` compares `Tokens` and `Ids` by
reference.

- [x] **Step 2: Note where a caller actually hits this**

Asserting an encoding against a result written out by hand — the one place a
caller has every reason to compare two of these. That is the sentence the XML
documentation will carry.

- [x] **Step 3: Sweep for the same shape elsewhere**

```bash
grep -rn "record .*IReadOnlyList\|record .*\[\]" src --include='*.cs'
```

`ClassificationReport` was already made a plain class for this reason. Anything
else found is the same defect and should get its own issue rather than riding
along.

---

### Task 2: Implement structural equality

**Files:**

- Modify: `src/DataNet.Embeddings/Tokenization/WordPieceTokenizer.cs`

**Depends on:** Task 1.

- [x] **Step 1: `Equals(TokenizationResult? other)`**

Reference check, null and count checks, then element by element on both lists.

- [x] **Step 2: `GetHashCode` over the same elements**

Never leave the generated one in place beside a hand-written `Equals`.

- [x] **Step 3: The reason, in the XML documentation on the member**

The generated equality would compare `Tokens` and `Ids` by reference, so two
results holding the same tokens would be unequal — in the one place a caller has
every reason to compare.

**Without that remark the override reads as redundant and gets deleted.**

- [x] **Step 4: Both targets**

```bash
build_all
```

---

### Task 3: Verify the contract, not only the happy case

**Depends on:** Task 2.

- [x] **Step 1: The equality cases**

Equal contents from distinct lists; differing tokens; differing ids; differing
lengths; null; same reference.

- [x] **Step 2: The hash contract**

Equal values produce equal hashes. Add a dictionary round-trip — that is where the
`Equals`/`GetHashCode` mismatch shows up in real code rather than in an assertion.

- [x] **Step 3: Nothing else moved**

```bash
build_all && test_all 2>&1 | tail -3 && oracles_unchanged
```

Expected: green on both frameworks, `ORACLES CLEAN`. This changes how results are
*compared*, never what they *contain*.

- [x] **Step 4: Commit**

```bash
git add src/DataNet.Embeddings/Tokenization/WordPieceTokenizer.cs tests/
git commit -m "Compare a tokenization by its tokens, not by its list identity"
```
