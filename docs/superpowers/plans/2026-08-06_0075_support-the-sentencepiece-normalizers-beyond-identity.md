# #75 The precompiled normalizer — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Load the `spiece.model` and `tokenizer.json` files real models actually ship — `t5-small`, `albert-base-v2`, `camembert-base`, `xlm-roberta-base`, `google/mt5-small` — by reading the precompiled charsmap rather than refusing it.

**Architecture:** `PrecompiledNormalizer` walks the darts-clone double-array trie and applies the NUL-terminated replacements its values index, exactly as `sentencepiece`'s `Normalizer` does. The algorithm is established as a Python prototype validated against `sp.normalize` before any C# is written. Both loaders route through the same class, so the two formats cannot disagree about the same model.

**Tech Stack:** C# (net10.0 + netstandard2.0), Python `sentencepiece` for prototyping and oracles, xunit.

**Spec:** `2026-08-06_0075_support-the-sentencepiece-normalizers-beyond-identity.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `feat/75-precompiled-normalizer`. Never commit to `main`.
- **Never decide from `normalizer_spec.name`.** Refuse what cannot be *applied*,
  not what was not *enumerated*.
- **A charsmap that will not parse is refused whole.** Half-normalized text is the
  same silent failure with a better disguise.
- **No model weights** (ADR 0003). Vocabularies and charsmaps only.
- Every new public type reachable from `samples/DataNet.Sample` (ADR 0009).

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
test_norm() { dotnet test -c Release --filter "FullyQualifiedName~Normalizer"; }
test_all()  { dotnet test -c Release; }
```

---

### Task 1: Measure the premise before designing anything

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the fact that decides the route — and, if it comes out differently,
a different branch.

- [ ] **Step 1: Check what the five real models actually declare**

```bash
python3 - <<'EOF'
import hashlib, sentencepiece.sentencepiece_model_pb2 as pb
for name, path in [("t5-small", "..."), ("albert-base-v2", "..."),
                   ("camembert-base", "..."), ("xlm-roberta-base", "..."),
                   ("mt5-small", "...")]:
    m = pb.ModelProto(); m.ParseFromString(open(path, 'rb').read())
    cs = m.normalizer_spec.precompiled_charsmap
    print(name, m.normalizer_spec.name, len(cs), hashlib.sha256(cs).hexdigest()[:16])
EOF
```

Expected: all five `nmt_nfkc`, all five a **byte-identical** 237 539-byte charsmap.

- [ ] **Step 2: Confirm none of them loads today**

The only `spiece.model` this library can read is the one it trained itself. That
is the true state of the support claim.

---

### Task 2: Decide the route by measurement, not by preference

**Files:** none modified.

**Depends on:** Task 1.
**Produces:** the reason Route B is impossible rather than merely harder.

- [ ] **Step 1: Compare `nmt_nfkc` against Python's NFKC over every assigned code point**

Whitespace flags off, so only the map speaks. 149 251 code points.

```bash
python3 - <<'EOF'
import unicodedata, sentencepiece as spm
# normalize each assigned code point through the map and through NFKC, and classify
EOF
```

Expected:

| | Count |
| --- | ---: |
| Dropped by the map, kept by NFKC | 30 |
| Turned into a space by the map | 15 |
| **Kept by the map, changed by NFKC** | **136** |

181 divergences, 0.121 %.

- [ ] **Step 2: Look at *why* the third family exists**

Those 136 code points were added to Unicode **after the map was compiled** —
U+32FF in 12.1, the rest in 14. The map is frozen at the Unicode version of the
`sentencepiece` build that produced it; `string.Normalize(FormKC)` follows the
runtime's ICU.

**Route B cannot be byte-exact by construction:** the gap grows with every Unicode
release and differs between .NET versions for the same input and the same file.

- [ ] **Step 3: Post the measurement on the issue before building**

It is the whole argument, and it should be reviewable before an afternoon is spent
on the trie.

---

### Task 3: Prototype the walk in Python, and prove it

**Files:** scratch only.

**Depends on:** Task 2.
**Produces:** an algorithm known correct before it meets C#.

- [ ] **Step 1: Implement the darts-clone double-array trie walk in Python**

Longest match, with the NUL-terminated replacement blob indexed by the trie
values.

- [ ] **Step 2: Reproduce `sp.normalize` exhaustively**

- **all 1 112 064 code points**
- 25 hand-picked sequences
- 20 000 random strings

Expected: **no mismatch**.

Debugging a trie walk in C# against a 237 KB binary blob is a bad place to be.
Establishing it in the language that can talk to `sentencepiece` directly removes
the whole class of uncertainty for an afternoon's work.

---

### Task 4: The C# implementation of `PrecompiledNormalizer`

**Files:**

- Create: `src/DataNet.Embeddings/Persistence/PrecompiledNormalizer.cs`
- Modify: `src/DataNet.Embeddings/Persistence/SentencePieceModelLoader.cs`

**Depends on:** Task 3.

- [ ] **Step 1: Port the validated walk**

- [ ] **Step 2: Refuse on applicability, never on a name**

- a normalizer named with **no charsmap** to apply it with;
- a charsmap that will not parse — **refused whole**;
- `NFKC` in a `tokenizer.json`, which asks for the runtime's tables where the
  model asked for a frozen map.

- [ ] **Step 3: Both targets build**

```bash
build_all
```

---

### Task 5: One implementation, two formats

**Files:**

- Modify: `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs`

**Depends on:** Task 4.

- [ ] **Step 1: Read `Precompiled` through the same class**

It is the same blob, base64-encoded.

- [ ] **Step 2: Confirm the two formats agree on the same model**

Load a model both ways and compare the normalized output for a shared input set.
Two loaders with two implementations is two places for the same model to be
tokenized differently — the issue's "revisited in the same breath" criterion.

---

### Task 6: Oracles, documentation and the packaging gate

**Files:**

- Modify: `tools/generate_oracles.py`, `tools/build_normalizer_fixtures.py`
- Modify: `docs/decisions/0014-precompiled-normalizer.md` (new),
  `docs/decisions/0013-sentencepiece-parity-scope.md`, `docs/equivalence.md`,
  `docs/guides/embeddings.md`, `THIRD-PARTY-NOTICES.md`
- Modify: `samples/DataNet.Sample/Lot3Embeddings.cs`

**Depends on:** Task 5.

- [ ] **Step 1: ADR 0014 — the Route A/B measurement, in full**

Including the 136-code-point family and why it makes Route B impossible rather
than harder. That is the part a future reader will want to re-derive otherwise.

- [ ] **Step 2: Correct everything that said these normalizers are refused**

Several documents state it. They now load. A stale limitation tells a reader to
distrust something that works.

- [ ] **Step 3: The sample references the new type**

The packaging gate (#72) fails otherwise, and that failure is the gate working.

- [ ] **Step 4: Full gate, then Sonar**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
```

Clear the findings before the pull request, not after.

- [ ] **Step 5: Commit**

```bash
git commit -m "Read the normalizer out of the file instead of refusing it"
git commit -m "Say what loads, in the places that said otherwise"
git commit -m "Answer the three findings and the packaging gate"
```
