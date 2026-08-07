# BPE and byte-level BPE tokenizers — implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `BpeTokenizer` to `DataNet.Embeddings`, covering classic character-level BPE and GPT-2 byte-level BPE, byte-exact against HuggingFace `tokenizers`, with a reversible `Decode`.

**Architecture:** One tokenizer class driven by a `BpeVocabulary` data record, exactly as `WordPieceTokenizer`/`WordPieceVocabulary` already work. Merge pairs are resolved to id pairs at load time, so the merge loop touches nothing but `int`s in a rented buffer — no string allocation, no dictionary of strings in the hot path. Two loaders feed the vocabulary: `BpeFilesLoader` (`vocab.json` + `merges.txt`) and `TokenizerJsonLoader.LoadBpe`.

**Tech Stack:** C# (`net10.0` + `netstandard2.0`), xUnit, BenchmarkDotNet, Python 3.12 for oracle generation (`tokenizers`, HuggingFace).

**Spec:** [`docs/superpowers/specs/2026-08-06-bpe-tokenizers-design.md`](../specs/2026-08-06-bpe-tokenizers-design.md)

## Global Constraints

- **Branch:** `feat/59-bpe-tokenizers`, already created from `main`. One PR for the whole issue. Do not merge — the repository owner merges.
- **Language:** everything committed — code, comments, XML doc, commit messages, docs — is in **English**. Chat with the user is in French.
- **Every commit message** describes the behaviour change, not the mechanics. No `feat:`/`fix:` prefixes: this repository does not use Conventional Commits. Sign off with:
  `Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>`
- **Both targets must build:** `net10.0` and `netstandard2.0`. `FrozenDictionary` and `GetAlternateLookup` are `#if NET9_0_OR_GREATER` only, following `src/DataNet.Text/Vectorization/StopWordSet.cs`. `ArrayPool` and `stackalloc` work on both.
- **Formatting gate:** `dotnet format DataNet.slnx --verify-no-changes` must pass. Run `dotnet format DataNet.slnx` before each commit.
- **Analyzer gate:** SonarAnalyzer runs in the build since #84. A finding fails the build locally, before the push. Suppress only with an in-code `#pragma warning disable Sxxxx` carrying a comment that says *why*, matching the style at the top of `SentencePieceTokenizer.cs`.
- **Packaging gate:** every new public type must be reachable from `samples/DataNet.Sample/Lot3Embeddings.cs` with a **member reference** — `typeof(T)` does not count, and neither does a `const` field. Task 14 does this; it is not optional.
- **Oracle rule:** correctness is proven by replaying values captured from HuggingFace `tokenizers`, never by a test written alongside the implementation. Regenerate from a neutral working directory:
  ```bash
  cd /tmp && PYTHONSAFEPATH=1 /home/cyril/Documents/devs/data.net/.venv-oracles/bin/python /home/cyril/Documents/devs/data.net/tools/generate_oracles.py
  ```
  Check the generator's own exit code. Never pipe it into `tail` — that reports `tail`'s status and a failed generation then looks successful.
- **Licensing:** implement from the published algorithm description. Never transcribe library source. The `bytes_to_unicode` construction rule and the merge algorithm are both published; the oracle proves the result matches.
- **Build and test:**
  ```bash
  dotnet build DataNet.slnx --configuration Release
  ```
  ```bash
  dotnet test DataNet.slnx --configuration Release --no-build
  ```

---

## File Structure

**Created**

| File | Responsibility |
| --- | --- |
| `src/DataNet.Embeddings/Tokenization/ByteLevelAlphabet.cs` | The 256-entry byte↔unicode table and its inverse, built from the published rule |
| `src/DataNet.Embeddings/Tokenization/BpePatterns.cs` | The three named split patterns, compiled once |
| `src/DataNet.Embeddings/Tokenization/BpeVocabulary.cs` | `MergePair` and `BpeVocabulary` — the data the loaders produce |
| `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs` | Encode, Decode, TryGetId, and the merge loop |
| `src/DataNet.Embeddings/Persistence/BpeFilesLoader.cs` | `vocab.json` + `merges.txt` |
| `tests/DataNet.Embeddings.Tests/Tokenization/ByteLevelAlphabetTests.cs` | Alphabet unit tests |
| `tests/DataNet.Embeddings.Tests/Tokenization/BpePreTokenizeTests.cs` | Split-pattern oracle replay |
| `tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs` | Classic BPE oracle replay |
| `tests/DataNet.Embeddings.Tests/ByteLevelBpeTests.cs` | GPT-2 oracle replay, encode and decode |
| `tests/DataNet.Embeddings.Tests/Persistence/BpeFilesLoaderTests.cs` | `vocab.json` + `merges.txt` loading |
| `tools/fetch_gpt2_bpe.py` | Fetches and verifies the GPT-2 fixture |
| `bench/DataNet.Text.Benchmarks/BpeBenchmarks.cs` | BenchmarkDotNet, `[MemoryDiagnoser]` |
| `docs/decisions/0017-bpe-parity-scope.md` | The parity limits, in the shape of ADR 0013 |

**Modified**

| File | Change |
| --- | --- |
| `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs` | `LoadBpe` overloads + BPE pipeline validation |
| `tools/generate_oracles.py` | Four generator sections, registered in `main()` |
| `tools/build_tiny_models.py` | Emit `tiny_bpe.json` |
| `tests/DataNet.Embeddings.Tests/DataNet.Embeddings.Tests.csproj` | Copy `oracles/**/*.txt` |
| `tests/DataNet.Embeddings.NetStandard.Tests/DataNet.Embeddings.NetStandard.Tests.csproj` | Copy `oracles/**/*.txt` |
| `tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs` | BPE loading and refusals |
| `.github/workflows/ci.yml` | `tools/fetch_gpt2_bpe.py --check` |
| `bench/corpus/generate_vocabs.py`, `bench/DataNet.Text.Benchmarks/BenchCorpus.cs` | A 30k BPE tokenizer for the benchmark |
| `samples/DataNet.Sample/Lot3Embeddings.cs` | Exercise the five new public types |
| `docs/equivalence.md`, `docs/guides/embeddings.md`, `README.md`, `CHANGELOG.md`, `THIRD-PARTY-NOTICES.md` | Documentation and attribution |

---

### Task 1: The GPT-2 fixture

**Files:**
- Create: `tools/fetch_gpt2_bpe.py`
- Create (generated, committed): `tests/oracles/gpt2_vocab.json`, `tests/oracles/gpt2_merges.txt`
- Modify: `tests/DataNet.Embeddings.Tests/DataNet.Embeddings.Tests.csproj`, `tests/DataNet.Embeddings.NetStandard.Tests/DataNet.Embeddings.NetStandard.Tests.csproj`
- Modify: `.github/workflows/ci.yml`, `THIRD-PARTY-NOTICES.md`

**Interfaces:**
- Consumes: nothing.
- Produces: `tests/oracles/gpt2_vocab.json` (a JSON object, token → id, 50 257 entries) and `tests/oracles/gpt2_merges.txt` (`#version: 0.2` then one space-separated pair per line, in rank order). Every later task reads these two files.

- [ ] **Step 1: Read the model to copy**

Read `tools/fetch_stopwords.py` end to end. It is the script this one mirrors: pinned SHA-256, a `--check` mode, and a module docstring explaining the licence and why the file is redistributed. Match its structure and its tone.

- [ ] **Step 2: Write `tools/fetch_gpt2_bpe.py`**

```python
#!/usr/bin/env python3
"""Vendor the GPT-2 byte-level BPE vocabulary into tests/oracles/.

`ByteLevelBpeTests` claims byte-exact parity with HuggingFace `tokenizers` over
GPT-2's real 50 257-entry vocabulary. A self-trained toy model cannot support
that claim: it would never exercise a merge table with 50 000 ranks, and it
would not prove that DataNet reads the `merges.txt` layout a real model ships.

Only the vocabulary and the merge table are redistributed here — never the
weights, per docs/decisions/0003-provenance-and-licensing.md. `gpt2` is
MIT-licensed (https://huggingface.co/openai-community/gpt2); the attribution is
recorded in NOTICE.

    python tools/fetch_gpt2_bpe.py           # vendor
    python tools/fetch_gpt2_bpe.py --check   # verify the checked-in fixtures

Each download is checked against the SHA-256 pinned below before anything is
written. A mismatch means the upstream file changed: read the diff, update the
pin, regenerate the oracles in the same commit, and expect ids to move.
"""

from __future__ import annotations

import hashlib
import sys
import urllib.request
from pathlib import Path

ORACLE_DIR = Path(__file__).resolve().parent.parent / "tests" / "oracles"
BASE = "https://huggingface.co/openai-community/gpt2/resolve/main/"

# name in tests/oracles -> (upstream file, pinned sha256 of the upstream bytes)
FILES = {
    "gpt2_vocab.json": ("vocab.json", "PASTE_ME"),
    "gpt2_merges.txt": ("merges.txt", "PASTE_ME"),
}


def download(name: str) -> bytes:
    with urllib.request.urlopen(BASE + name) as response:  # noqa: S310
        return response.read()


def main() -> int:
    check = "--check" in sys.argv[1:]
    failures = []
    for local, (remote, pinned) in FILES.items():
        payload = download(remote)
        digest = hashlib.sha256(payload).hexdigest()
        if digest != pinned:
            failures.append(
                f"{BASE}{remote}\n  expected sha256 {pinned}\n  got      sha256 {digest}")
            continue
        path = ORACLE_DIR / local
        if check:
            if not path.exists() or path.read_bytes() != payload:
                failures.append(f"{path} differs from the verified upstream file.")
        else:
            path.write_bytes(payload)
            print(f"{local}: {len(payload)} bytes -> {path}")
    for failure in failures:
        print(failure, file=sys.stderr)
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 3: Run it once to learn the digests, then pin them**

Run: `python3 tools/fetch_gpt2_bpe.py`
Expected: FAIL, printing `expected sha256 PASTE_ME` and the real digest for each file.

Copy each real digest into `FILES` in place of `PASTE_ME`. Do **not** invent these values — they are whatever the run prints.

- [ ] **Step 4: Vendor the files and check them**

Run: `python3 tools/fetch_gpt2_bpe.py && python3 tools/fetch_gpt2_bpe.py --check && echo OK`
Expected: two byte counts printed, then `OK`.

Sanity-check what landed:

```bash
head -c 120 tests/oracles/gpt2_merges.txt && echo && python3 -c "import json;print(len(json.load(open('tests/oracles/gpt2_vocab.json'))))"
```

Expected: the first line is `#version: 0.2`, and the vocabulary has `50257` entries.

- [ ] **Step 5: Make the test projects copy `*.txt`**

Both `.csproj` files copy `tests/oracles/**` filtered to `*.json`, `*.onnx` and `*.model`. `merges.txt` matches none of them. In **both** files, after the `*.model` line, add:

```xml
    <!-- merges.txt is a fixture like any other; without this glob the BPE suite cannot find it. -->
    <None Include="../oracles/**/*.txt" CopyToOutputDirectory="PreserveNewest" LinkBase="oracles" />
```

- [ ] **Step 6: Prove the copy actually happens**

Run: `dotnet build tests/DataNet.Embeddings.NetStandard.Tests --configuration Release && ls tests/DataNet.Embeddings.NetStandard.Tests/bin/Release/net10.0/oracles/gpt2_merges.txt`
Expected: the path is listed. If it is not, the glob is wrong — fix it now rather than discovering it in Task 8.

- [ ] **Step 7: Wire `--check` into CI**

In `.github/workflows/ci.yml`, find the step running `python tools/fetch_stopwords.py --check` (around line 185) and add a sibling step immediately after it:

```yaml
      - name: Verify the vendored GPT-2 vocabulary
        run: python tools/fetch_gpt2_bpe.py --check
```

- [ ] **Step 8: Record the attribution**

`THIRD-PARTY-NOTICES.md` only — **not** `NOTICE`. The two files split by what ships: `NOTICE` carries what is compiled into an assembly, and `THIRD-PARTY-NOTICES.md` has a "Redistributed test fixtures (not shipped)" section where the XLM-R vocabulary already lives. A test fixture in `tests/oracles/` belongs there, and adding it to `NOTICE` would break the one distinction that file exists to make.

Add a GPT-2 row to that table and the prose block that follows it, in the shape of the XLM-R pair: what is vendored, from where, under which licence, that `tools/fetch_gpt2_bpe.py` pins and verifies it, and that the weights are never redistributed.

- [ ] **Step 9: Commit**

```bash
git add tools/fetch_gpt2_bpe.py tests/oracles/gpt2_vocab.json tests/oracles/gpt2_merges.txt tests/DataNet.Embeddings.Tests/DataNet.Embeddings.Tests.csproj tests/DataNet.Embeddings.NetStandard.Tests/DataNet.Embeddings.NetStandard.Tests.csproj .github/workflows/ci.yml NOTICE THIRD-PARTY-NOTICES.md
git commit -m "Vendor GPT-2's real vocabulary so BPE parity can be proven, not asserted

A self-trained toy model would never exercise a 50 000-rank merge table,
nor prove that the merges.txt layout a real model ships can be read at
all. The two files are pinned by SHA-256 and re-verified in CI, as the
stop-word lists already are.

Both test projects filtered tests/oracles to *.json, *.onnx and *.model,
so merges.txt was copied by neither. The *.txt glob is what stops the BPE
suite from failing on a missing file -- or worse, skipping quietly.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: The tiny classic-BPE model

**Files:**
- Modify: `tools/build_tiny_models.py`
- Create (generated, committed): `tests/oracles/tiny_bpe.json`

**Interfaces:**
- Consumes: nothing.
- Produces: `tests/oracles/tiny_bpe.json` — a HuggingFace `tokenizer.json` whose `model.type` is `"BPE"`, with `end_of_word_suffix: "</w>"`, `unk_token: "[UNK]"`, and a `Whitespace` pre-tokenizer. Task 3 and Task 7 read it.

- [ ] **Step 1: Add the builder**

In `tools/build_tiny_models.py`, above `main()`:

```python
# A character-level BPE, the subword-nmt lineage: no byte alphabet, an explicit
# end-of-word marker, and a vocabulary small enough to read in a diff. It exists
# to exercise the merge loop on its own, with none of the byte-level mapping the
# GPT-2 fixture brings.
BPE_CORPUS = [
    "the quick brown fox jumps over the lazy dog",
    "tokenization is embedding embeddings",
    "the cat sat on the mat and the cat sat again",
    "lovely love loved lover loving",
    "bigger biggest big",
    "natural language processing processes language naturally",
    "machine learning and data science",
    "programming programs a program",
]


def build_tiny_bpe() -> str:
    """A trained character-level BPE, serialized as a tokenizer.json."""
    from tokenizers import Tokenizer  # noqa: PLC0415
    from tokenizers.models import BPE  # noqa: PLC0415
    from tokenizers.pre_tokenizers import Whitespace  # noqa: PLC0415
    from tokenizers.trainers import BpeTrainer  # noqa: PLC0415

    tokenizer = Tokenizer(BPE(unk_token="[UNK]", end_of_word_suffix="</w>"))
    tokenizer.pre_tokenizer = Whitespace()
    tokenizer.train_from_iterator(
        BPE_CORPUS,
        BpeTrainer(
            vocab_size=200,
            min_frequency=1,
            special_tokens=["[UNK]"],
            end_of_word_suffix="</w>",
            show_progress=False,
        ),
    )
    return tokenizer.to_str(pretty=True)
```

- [ ] **Step 2: Register it in `main()`**

`main()` currently loops over ONNX builders and calls `SerializeToString()`. The BPE model is text, so write it separately — append to the body of `main()`:

```python
    path = ORACLE_DIR / "tiny_bpe.json"
    path.write_text(build_tiny_bpe() + "\n", encoding="utf-8")
    print(f"tiny_bpe.json: {path.stat().st_size} bytes -> {path}")
```

- [ ] **Step 3: Build it and check the shape**

Run: `cd /tmp && PYTHONSAFEPATH=1 /home/cyril/Documents/devs/data.net/.venv-oracles/bin/python /home/cyril/Documents/devs/data.net/tools/build_tiny_models.py`
Expected: `tiny_bpe.json: … bytes -> …/tests/oracles/tiny_bpe.json`

Then:

```bash
python3 -c "import json;m=json.load(open('tests/oracles/tiny_bpe.json'))['model'];print(m['type'],m['end_of_word_suffix'],len(m['vocab']),len(m['merges']))"
```

Expected: `BPE </w>` followed by a vocabulary size and a merge count, both non-zero. If the merge count is 0 the trainer found nothing to merge — raise `vocab_size` or extend `BPE_CORPUS` until it does not.

- [ ] **Step 4: Check reproducibility — and expect it to fail**

Run the builder a second time and diff:

```bash
git diff --stat -- tests/oracles/tiny_bpe.json
```

**This came back non-empty when the task was executed, and that outcome is accepted.** `tokenizers` 0.23.1's `BpeTrainer` is not byte-reproducible across process runs: the vocabulary size and merge count are stable, but equal-frequency tokens and merges break ties differently per run, because of Rust's randomized hash seeding. `RAYON_NUM_THREADS=1` does not fix it.

The ruling is to commit whichever run is on disk, for three reasons:

- The repository already documents this situation for `build_normalizer_fixtures.py` in `tools/README.md`: those fixtures are committed inputs, "CI never retrains them, and training is not guaranteed reproducible across `sentencepiece` versions."
- The non-determinism is contained. `tiny_bpe.json` is an *input* to `generate_oracles.py`, and the CI drift job regenerates `bpe.json` from whatever is committed — so a re-trained model committed without a regenerated corpus fails CI by name.
- Nothing hardcodes an id from this model. Task 7's tests read the vocabulary and merges out of the committed file at test time, so tie order has no test to break.

Chasing the ties by editing `BPE_CORPUS` is rejected: flattening the ties you can find does not prove there are none left, and the next `tokenizers` release can reintroduce them.

Record it where a reader will hit it: a paragraph in the `build_tiny_bpe()` docstring, and one or two sentences beside the `build_tiny_models.py` entry in `tools/README.md`, both saying that the committed fixture is authoritative rather than the script, and that a diff there must not be committed without regenerating `bpe.json` in the same commit.

- [ ] **Step 5: Commit**

```bash
git add tools/build_tiny_models.py tests/oracles/tiny_bpe.json
git commit -m "Train a tiny character-level BPE to exercise the merge loop alone

GPT-2 proves the byte-level pipeline but proves it all at once: a failing
token there could be the alphabet, the split pattern or the merge order.
This model has no byte alphabet and an explicit </w>, so a failure in it
can only be the merge loop.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: The oracle corpora

**Files:**
- Modify: `tools/generate_oracles.py`
- Create (generated, committed): `tests/oracles/bpe.json`, `bytelevel_bpe.json`, `bpe_pretokenize.json`, `bpe_tokenizer_json.json`

**Interfaces:**
- Consumes: `tests/oracles/tiny_bpe.json`, `gpt2_vocab.json`, `gpt2_merges.txt`.
- Produces four corpora. Their exact JSON shape is fixed here and read by Tasks 6–12:
  - `bpe.json` — `{"metadata": {...}, "cases": [{"id", "text", "tokens", "ids"}]}`
  - `bytelevel_bpe.json` — same, plus `"decoded"` and `"decoded_skip_specials"` per case, and `"alphabet"` in the metadata (a 256-entry array, index = byte, value = the mapped character).
  - `bpe_pretokenize.json` — `{"metadata": {"patterns": {"gpt2": "...", "llama3": "...", "qwen2": "..."}}, "cases": [{"id", "pattern", "text", "pieces"}]}`, where `pieces` is the *split* output before merging.
  - `bpe_tokenizer_json.json` — `{"cases": [{"id", "name", "tokenizer_json", "text", "tokens", "ids"}]}`.

- [ ] **Step 1: Add the shared text corpus**

The cases the spec requires. Add near the other fixture constants at the top of `tools/generate_oracles.py`:

```python
# The inputs byte-level pre-tokenization diverges from intuition on. Whitespace
# runs first, because " a" and "a " are different tokens and a tokenizer that
# trims either is wrong; then the scripts whose UTF-8 spans several bytes, which
# is what turns one character into several byte-level symbols; then text naming
# the special-token strings literally, which a tokenizer that special-cases them
# by string rather than by table would get wrong.
BPE_TEXTS = [
    "",
    " ",
    "   ",
    "Hello, world!",
    " leading space",
    "trailing space ",
    "double  space",
    "a\tb\nc\r\nd",
    "Il était une fois, à Paris — déjà vu.",
    "naïve café résumé",
    "東京都から来ました",
    "中文分词测试",
    "emoji 👋🏽 family 👨‍👩‍👧‍👦 flag 🇫🇷",
    "<|endoftext|> is written here literally",
    "[UNK] [CLS] [SEP] as text",
    "123 4567 89.01 -42",
    "https://example.com/path?q=1&r=2",
    "snake_case camelCase kebab-case SCREAMING_CASE",
    "the quick brown fox jumps over the lazy dog",
    "tokenization is embedding embeddings",
]
```

- [ ] **Step 2: Add the byte-level generator**

```python
GPT2_VOCAB = "gpt2_vocab.json"
GPT2_MERGES = "gpt2_merges.txt"


def _gpt2_tokenizer(pattern: str | None = None):
    """GPT-2's byte-level BPE, optionally with another model's split pattern.

    `pattern=None` is stock GPT-2: `ByteLevel` does its own splitting. A pattern
    reproduces the Llama-3 / Qwen2 shape, where a `Split` runs first and
    `ByteLevel` is reduced to the byte mapping.
    """
    from tokenizers import Regex, Tokenizer  # noqa: PLC0415
    from tokenizers.decoders import ByteLevel as ByteLevelDecoder  # noqa: PLC0415
    from tokenizers.models import BPE  # noqa: PLC0415
    from tokenizers.pre_tokenizers import ByteLevel, Sequence, Split  # noqa: PLC0415

    tokenizer = Tokenizer(BPE.from_file(
        str(ORACLE_DIR / GPT2_VOCAB), str(ORACLE_DIR / GPT2_MERGES)))
    if pattern is None:
        tokenizer.pre_tokenizer = ByteLevel(add_prefix_space=False)
    else:
        tokenizer.pre_tokenizer = Sequence([
            Split(Regex(pattern), behavior="isolated"),
            ByteLevel(add_prefix_space=False, use_regex=False),
        ])
    tokenizer.decoder = ByteLevelDecoder()
    return tokenizer


def generate_bytelevel_bpe() -> dict:
    from tokenizers.pre_tokenizers import ByteLevel  # noqa: PLC0415

    tokenizer = _gpt2_tokenizer()
    # The alphabet is frozen alongside the cases: a mapping-table slip produces
    # tokens that look plausible, and this is what names the actual culprit.
    alphabet = ByteLevel.alphabet()
    table = [None] * 256
    for byte, char in zip(range(256), sorted(alphabet)):  # placeholder, replaced below
        table[byte] = char

    cases = []
    for i, text in enumerate(BPE_TEXTS):
        enc = tokenizer.encode(text)
        cases.append({
            "id": i,
            "text": text,
            "tokens": enc.tokens,
            "ids": enc.ids,
            "decoded": tokenizer.decode(enc.ids, skip_special_tokens=False),
            "decoded_skip_specials": tokenizer.decode(enc.ids, skip_special_tokens=True),
        })
    return {
        "metadata": {
            "algorithm": "ByteLevelBPE",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "gpt2 (vendored by tools/fetch_gpt2_bpe.py)",
            "alphabet": table,
            "count": len(cases),
        },
        "cases": cases,
    }
```

- [ ] **Step 3: Replace the placeholder alphabet with the real table**

`ByteLevel.alphabet()` returns an unordered set, so `sorted()` above is **wrong** — it does not tell you which byte maps to which character. Derive the table from the published rule instead, and replace the `table` construction in `generate_bytelevel_bpe` with:

```python
    # The published bytes_to_unicode construction: the three printable ranges map
    # to themselves, and the 68 bytes left over take 256, 257, ... in byte order.
    printable = (list(range(0x21, 0x7F)) + list(range(0xA1, 0xAD)) + list(range(0xAE, 0x100)))
    table = [None] * 256
    for byte in printable:
        table[byte] = chr(byte)
    spare = 0
    for byte in range(256):
        if table[byte] is None:
            table[byte] = chr(256 + spare)
            spare += 1
    assert set(table) == ByteLevel.alphabet(), "derived alphabet disagrees with tokenizers"
```

The `assert` is the point: the table is derived from the rule, and `tokenizers` confirms the derivation. Delete the `zip(...)` placeholder line and the now-unused `sorted`.

- [ ] **Step 4: Add the classic-BPE and split-pattern generators**

```python
def generate_bpe() -> dict:
    """Classic character-level BPE over the small self-trained model."""
    from tokenizers import Tokenizer  # noqa: PLC0415

    tokenizer = Tokenizer.from_file(str(ORACLE_DIR / "tiny_bpe.json"))
    cases = []
    for i, text in enumerate(BPE_TEXTS):
        enc = tokenizer.encode(text)
        cases.append({"id": i, "text": text, "tokens": enc.tokens, "ids": enc.ids})
    return {
        "metadata": {
            "algorithm": "BPE",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "tiny_bpe.json (self-trained, end_of_word_suffix </w>)",
            "count": len(cases),
        },
        "cases": cases,
    }


# Transcribe each of these from the model's own tokenizer.json rather than from
# memory: they differ from GPT-2 in newline handling and in the case-insensitive
# contraction group, and from each other only in a quantifier on \p{N}.
BPE_PATTERNS = {
    "gpt2": r"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+",
    "llama3": "TRANSCRIBE_FROM_MODEL",
    "qwen2": "TRANSCRIBE_FROM_MODEL",
}


def generate_bpe_pretokenize() -> dict:
    """Prove the split, not the vocabulary.

    The Llama-3 and Qwen2 rows of the parity table are claimed at the split
    level only (ADR 0017). Running their patterns over GPT-2's vocabulary is
    what proves the C# regex behaves as HuggingFace's does, without vendoring a
    second and third 150 000-entry vocabulary to prove a merge loop the GPT-2
    corpus already proves.
    """
    cases = []
    case_id = 0
    for name, pattern in BPE_PATTERNS.items():
        tokenizer = _gpt2_tokenizer(None if name == "gpt2" else pattern)
        for text in BPE_TEXTS:
            pieces = [piece for piece, _ in tokenizer.pre_tokenizer.pre_tokenize_str(text)]
            cases.append({"id": case_id, "pattern": name, "text": text, "pieces": pieces})
            case_id += 1
    return {
        "metadata": {
            "algorithm": "BPE pre-tokenization",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "patterns": BPE_PATTERNS,
            "count": len(cases),
        },
        "cases": cases,
    }
```

- [ ] **Step 5: Transcribe the two remaining patterns**

Fetch each model's `tokenizer.json` and read the `Split` pattern out of it — do not write these from memory:

```bash
python3 -c "
import json,urllib.request
for name,url in [('llama3','https://huggingface.co/meta-llama/Meta-Llama-3-8B/resolve/main/tokenizer.json'),('qwen2','https://huggingface.co/Qwen/Qwen2-0.5B/resolve/main/tokenizer.json')]:
    try:
        d=json.load(urllib.request.urlopen(url))
        print(name, json.dumps(d['pre_tokenizer']))
    except Exception as e:
        print(name,'FAILED',e)
"
```

**This was executed, and here is what it produced.** Qwen2 succeeded. `meta-llama/Meta-Llama-3-8B` returned HTTP 401 — it is gated. Guessing the pattern was refused; it was read instead from two independent ungated mirrors whose `pre_tokenizer` blocks are byte-identical to each other:

- `https://huggingface.co/NousResearch/Meta-Llama-3-8B/resolve/main/tokenizer.json`
- `https://huggingface.co/unsloth/llama-3-8b/resolve/main/tokenizer.json`

Both also report `model.ignore_merges = true` and a 128 000-entry vocabulary. The resulting entries:

```python
    "llama3": r"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+",
    "qwen2":  r"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+",
```

They differ in exactly one place — `\p{N}{1,3}` against `\p{N}`. A transcription that differs anywhere else is a typo. Record both mirror URLs in a comment above `BPE_PATTERNS`: a literal whose provenance is not written down is what the licensing rule is about.

- [ ] **Step 6: Add the loader corpus**

```python
def generate_bpe_tokenizer_json() -> dict:
    """The tokenizer.json shapes TokenizerJsonLoader.LoadBpe must read.

    Each case carries the file itself, so the C# side parses the exact bytes
    HuggingFace was handed rather than a second fixture that could drift.
    """
    from tokenizers import Tokenizer  # noqa: PLC0415

    text = "Hello, world! déjà 東京 👋"
    cases = []
    for i, (name, tokenizer) in enumerate([
        ("bytelevel", _gpt2_tokenizer()),
        ("split_sequence", _gpt2_tokenizer(BPE_PATTERNS["qwen2"])),
        ("classic", Tokenizer.from_file(str(ORACLE_DIR / "tiny_bpe.json"))),
    ]):
        enc = tokenizer.encode(text)
        cases.append({
            "id": i,
            "name": name,
            "tokenizer_json": tokenizer.to_str(),
            "text": text,
            "tokens": enc.tokens,
            "ids": enc.ids,
        })
    return {
        "metadata": {
            "algorithm": "BPE tokenizer.json",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "count": len(cases),
        },
        "cases": cases,
    }
```

`ignore_merges` is exercised in Task 10 rather than here: `tokenizers` sets it from the model, and forcing it on requires editing the serialized JSON, which Task 10 does where the behaviour it proves lives.

- [ ] **Step 7: Register the four generators**

In `main()`, add to the **end** of the `generators` dict, after the last existing entry:

```python
        "bpe.json": generate_bpe,
        "bytelevel_bpe.json": generate_bytelevel_bpe,
        "bpe_pretokenize.json": generate_bpe_pretokenize,
        "bpe_tokenizer_json.json": generate_bpe_tokenizer_json,
```

`tools/generate_oracles.py` is also being extended on `feat/61-classification-metrics`, which appends `classification_metrics.json` and `roc_auc.json` to this same dict. A merge conflict here is expected and is resolved by keeping both sides — the entries are independent. Do not try to avoid it by placing the entries elsewhere; a dict with four BPE corpora scattered through it is worse than one conflict the merger resolves in ten seconds.

That branch also adds `tools/seeded_random.py`, a seeded RNG wrapper. It does not apply here: none of the four generators above draw a random number, they iterate `BPE_TEXTS`. If you find yourself reaching for `random`, stop — that is a sign the corpus is being generated rather than enumerated, which is not what these four do.

- [ ] **Step 8: Generate, and check the exit code**

Run: `cd /tmp && PYTHONSAFEPATH=1 /home/cyril/Documents/devs/data.net/.venv-oracles/bin/python /home/cyril/Documents/devs/data.net/tools/generate_oracles.py; echo "exit=$?"`
Expected: `exit=0`, and four new `… cases -> …` lines. Do not pipe this into anything.

- [ ] **Step 9: Check the corpora say something**

```bash
python3 -c "
import json
d=json.load(open('tests/oracles/bytelevel_bpe.json'))
print('alphabet', len(d['metadata']['alphabet']), d['metadata']['alphabet'][32], d['metadata']['alphabet'][10])
for c in d['cases'][:8]: print(repr(c['text']), c['tokens'], c['decoded']==c['text'])
"
```

Expected: `alphabet 256 Ġ Ċ` — byte 0x20 maps to U+0120 and byte 0x0A to U+010A — and `True` on every round-trip line. A `False` means the fixture itself is wrong; stop and diagnose before writing any C#.

- [ ] **Step 10: Confirm the drift job would pass**

Run the generator a second time, then: `git diff --stat -- tests/oracles`
Expected: empty. The `Oracles are reproducible` CI job runs exactly this.

Note for when this job goes red **in CI** rather than here. It was flaky through 2026-08-07 — red then green on an identical commit — and both halves of that have since been fixed on `main`: #95 made the step print the drift instead of only its shape, and #97 found the cause. Three corpora recorded floats at full `float64` repr, so the last bits described whichever SIMD reduction order the host CPU's BLAS chose; the generator now rounds to twelve significant digits.

None of that reaches these four corpora — they carry tokens, ids and strings, and not one float — so a red here is a real difference, not the old flake. Read the diff the job now prints.

Do not confuse a red with Task 2's finding either. The BPE *trainer* is non-deterministic, but `tiny_bpe.json` is committed and CI never retrains it — `generate_oracles.py` only reads it. A red on `bpe.json` is a real bug in this task's generator; it cannot be the trainer.


- [ ] **Step 11: Commit**

```bash
git add tools/generate_oracles.py tests/oracles/bpe.json tests/oracles/bytelevel_bpe.json tests/oracles/bpe_pretokenize.json tests/oracles/bpe_tokenizer_json.json
git commit -m "Freeze what HuggingFace does, before writing what DataNet will do

Four corpora: the merge loop alone on the tiny model, the full byte-level
pipeline on GPT-2 with both decode modes, the three split patterns, and
the tokenizer.json shapes the loader must read.

The byte alphabet is derived from the published rule and then checked
against ByteLevel.alphabet(), so the table is proven rather than copied --
which is also what the provenance rule asks for.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: `ByteLevelAlphabet`

**Files:**
- Create: `src/DataNet.Embeddings/Tokenization/ByteLevelAlphabet.cs`
- Test: `tests/DataNet.Embeddings.Tests/Tokenization/ByteLevelAlphabetTests.cs`

**Interfaces:**
- Consumes: `tests/oracles/bytelevel_bpe.json` → `metadata.alphabet`.
- Produces: `internal static class ByteLevelAlphabet` with `static char ToChar(byte b)`, `static bool TryToByte(char c, out byte b)`, and `static ReadOnlySpan<char> Table` (256 entries, index = byte).

- [ ] **Step 1: Write the failing test**

`tests/DataNet.Embeddings.Tests/Tokenization/ByteLevelAlphabetTests.cs`:

```csharp
using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Tokenization;

public sealed class ByteLevelAlphabetTests
{
    [Fact]
    public void Table_matches_the_frozen_alphabet()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_bpe.json");
        JsonElement expected = doc.RootElement.GetProperty("metadata").GetProperty("alphabet");

        Assert.Equal(256, expected.GetArrayLength());
        int b = 0;
        foreach (JsonElement entry in expected.EnumerateArray())
        {
            string mapped = entry.GetString()!;
            Assert.Equal(1, mapped.Length);
            Assert.Equal(mapped[0], ByteLevelAlphabet.ToChar((byte)b));
            b++;
        }
    }

    [Fact]
    public void Every_byte_round_trips_through_the_inverse()
    {
        for (int b = 0; b <= 255; b++)
        {
            char mapped = ByteLevelAlphabet.ToChar((byte)b);
            Assert.True(ByteLevelAlphabet.TryToByte(mapped, out byte back), $"0x{b:X2} -> '{mapped}' has no inverse");
            Assert.Equal((byte)b, back);
        }
    }

    [Fact]
    public void The_mapping_is_injective()
    {
        var seen = new HashSet<char>();
        for (int b = 0; b <= 255; b++)
        {
            Assert.True(seen.Add(ByteLevelAlphabet.ToChar((byte)b)), $"0x{b:X2} collides");
        }
    }

    [Fact]
    public void A_character_outside_the_alphabet_has_no_inverse()
    {
        Assert.False(ByteLevelAlphabet.TryToByte('東', out _));
    }
}
```

`InternalsVisibleTo` already exposes the library's internals to the test assembly — check `Directory.Build.props` or the `.csproj`; if it does not, add it there rather than making the type public.

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~ByteLevelAlphabetTests"`
Expected: FAIL — `ByteLevelAlphabet` does not exist, so this is a compile error.

- [ ] **Step 3: Implement it**

`src/DataNet.Embeddings/Tokenization/ByteLevelAlphabet.cs`:

```csharp
namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// The reversible byte-to-character alphabet GPT-2 tokenizes through.
/// </summary>
/// <remarks>
/// <para>
/// Byte-level BPE never sees text: it sees the 256 possible byte values, each
/// standing in as one printable character so that a merge table over characters
/// can address arbitrary bytes. That substitution is what makes the tokenizer
/// lossless over any input at all, valid UTF-8 or not.
/// </para>
/// <para>
/// The table is <em>built from the published construction</em> rather than
/// transcribed: the printable ranges <c>!</c>–<c>~</c>, <c>¡</c>–<c>¬</c> and
/// <c>®</c>–<c>ÿ</c> stand for themselves, and the 68 bytes left over — the
/// control characters, the space, the delete, and the three holes in Latin-1 —
/// take <c>U+0100</c> onwards in byte order. A space therefore appears as
/// <c>Ġ</c> (U+0120) and a newline as <c>Ċ</c> (U+010A), which is why GPT-2
/// tokens look the way they do.
/// </para>
/// </remarks>
internal static class ByteLevelAlphabet
{
    private static readonly char[] Forward = BuildForward();

    // 188 of the 256 characters are Latin-1, and the rest run to U+0143, so a
    // dense array over that range costs 324 slots and turns the inverse into an
    // array index instead of a dictionary probe in the decode loop.
    private const char MaxMapped = 'Ń';
    private static readonly byte[] Inverse = BuildInverse();
    private static readonly bool[] Mapped = BuildMapped();

    /// <summary>The character standing for <paramref name="value"/>.</summary>
    public static char ToChar(byte value) => Forward[value];

    /// <summary>The byte <paramref name="mapped"/> stands for, when it stands for one.</summary>
    public static bool TryToByte(char mapped, out byte value)
    {
        if (mapped <= MaxMapped && Mapped[mapped])
        {
            value = Inverse[mapped];
            return true;
        }
        value = 0;
        return false;
    }

    private static char[] BuildForward()
    {
        var forward = new char[256];
        var taken = new bool[256];
        foreach ((int from, int to) in new[] { (0x21, 0x7E), (0xA1, 0xAC), (0xAE, 0xFF) })
        {
            for (int b = from; b <= to; b++)
            {
                forward[b] = (char)b;
                taken[b] = true;
            }
        }
        int spare = 0;
        for (int b = 0; b < 256; b++)
        {
            if (!taken[b])
            {
                forward[b] = (char)(256 + spare);
                spare++;
            }
        }
        return forward;
    }

    private static byte[] BuildInverse()
    {
        var inverse = new byte[MaxMapped + 1];
        for (int b = 0; b < 256; b++)
        {
            inverse[Forward[b]] = (byte)b;
        }
        return inverse;
    }

    private static bool[] BuildMapped()
    {
        var mapped = new bool[MaxMapped + 1];
        for (int b = 0; b < 256; b++)
        {
            mapped[Forward[b]] = true;
        }
        return mapped;
    }
}
```

- [ ] **Step 4: Run the tests**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~ByteLevelAlphabetTests"`
Expected: 4 passed. If `Table_matches_the_frozen_alphabet` fails at a specific byte, the range boundaries are off by one — `0xAD` (soft hyphen) is the hole between the two Latin-1 ranges.

- [ ] **Step 5: Format, build both targets, commit**

```bash
dotnet format DataNet.slnx && dotnet build DataNet.slnx --configuration Release
```

```bash
git add src/DataNet.Embeddings/Tokenization/ByteLevelAlphabet.cs tests/DataNet.Embeddings.Tests/Tokenization/ByteLevelAlphabetTests.cs
git commit -m "Build GPT-2's byte alphabet from its rule rather than copying it

The 256-entry table is derived from the published construction and then
checked against the one HuggingFace produced, so a transcription slip has
nowhere to hide. The inverse is a dense array over U+0000..U+0143, which
makes decoding an array index rather than a dictionary probe.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: `MergePair` and `BpeVocabulary`

**Files:**
- Create: `src/DataNet.Embeddings/Tokenization/BpeVocabulary.cs`
- Test: `tests/DataNet.Embeddings.Tests/Tokenization/ValueEqualityTests.cs` (modify — it already covers the other vocabularies)

**Interfaces:**
- Consumes: nothing.
- Produces:
  ```csharp
  public readonly record struct MergePair(string Left, string Right);

  public sealed record BpeVocabulary(
      IReadOnlyDictionary<string, int> Vocab,
      IReadOnlyList<MergePair> Merges)
  {
      public IReadOnlyDictionary<string, int> AddedTokens { get; init; }   // empty by default
      public bool ByteLevel { get; init; }
      public bool AddPrefixSpace { get; init; }
      public bool IgnoreMerges { get; init; }
      public int SkippedMerges { get; init; }
      public string? EndOfWordSuffix { get; init; }
      public string? ContinuingSubwordPrefix { get; init; }
      public string? UnkToken { get; init; }
      public string? PreTokenizerPattern { get; init; }
      public int Count { get; }
  }
  ```
  Tasks 7–12 all construct or consume this.

- [ ] **Step 1: Read the record this one mirrors**

Read `src/DataNet.Embeddings/Tokenization/WordPieceVocabulary.cs` in full. It hand-writes `Equals` and `GetHashCode` because the generated ones compare `Vocab` by reference — two vocabularies loaded from the same file would be unequal. `BpeVocabulary` has the same problem twice over (`Vocab` and `Merges`) and needs the same treatment, including the O(1) hash over counts and scalars.

- [ ] **Step 2: Write the failing test**

Append to `tests/DataNet.Embeddings.Tests/Tokenization/ValueEqualityTests.cs`, inside the existing class:

```csharp
    private static BpeVocabulary SampleBpe() => new(
        new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 0, ["b"] = 1, ["ab"] = 2 },
        [new MergePair("a", "b")])
    {
        ByteLevel = true,
        PreTokenizerPattern = BpePatterns.Gpt2,
    };

    [Fact]
    public void Two_BpeVocabularies_with_the_same_content_are_equal()
    {
        Assert.Equal(SampleBpe(), SampleBpe());
        Assert.Equal(SampleBpe().GetHashCode(), SampleBpe().GetHashCode());
    }

    [Fact]
    public void A_BpeVocabulary_differing_in_one_merge_is_not_equal()
    {
        BpeVocabulary other = SampleBpe() with { Merges = [new MergePair("b", "a")] };
        Assert.NotEqual(SampleBpe(), other);
    }

    [Fact]
    public void A_BpeVocabulary_differing_in_a_flag_is_not_equal()
    {
        Assert.NotEqual(SampleBpe(), SampleBpe() with { ByteLevel = false });
    }

    [Fact]
    public void Merge_order_is_rank_order()
    {
        BpeVocabulary vocab = SampleBpe();
        Assert.Equal(new MergePair("a", "b"), vocab.Merges[0]);
        Assert.Equal(3, vocab.Count);
    }
```

- [ ] **Step 3: Run it to verify it fails**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~ValueEqualityTests"`
Expected: FAIL — `BpeVocabulary`, `MergePair` and `BpePatterns` do not exist.

- [ ] **Step 4: Write `BpePatterns`**

`src/DataNet.Embeddings/Tokenization/BpePatterns.cs`. The three literals below are already the ones Task 3 transcribed and froze in `tools/generate_oracles.py`; that dict remains the reference, so diff against it rather than retyping. Task 6 has a test asserting the shipped constants equal the ones the corpus was generated with, so any drift between the two is caught.

Verbatim strings (`@"…"`) are used deliberately: these patterns are dense in backslashes, and a regular string would need every one doubled — which is how a pattern silently becomes a different pattern.

```csharp
namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// The pre-tokenization patterns the byte-level models split on.
/// </summary>
/// <remarks>
/// <para>
/// Each is the <c>Split</c> pattern from that model's own <c>tokenizer.json</c>.
/// They matter more than they look: the split decides where a token can begin,
/// so a model tokenized with the wrong one produces plausible tokens and wrong
/// embeddings.
/// </para>
/// <para>
/// Exposed as properties rather than <c>const</c> fields on purpose. A
/// <c>const</c> is a compile-time constant, so a consumer referencing it emits
/// no member reference — and the sample's packaging gate, which proves the
/// public surface is reachable, would be structurally unable to see it.
/// </para>
/// </remarks>
public static class BpePatterns
{
    /// <summary>GPT-2's pattern. Matches <c>pre_tokenizers.ByteLevel(use_regex=True)</c>.</summary>
    public static string Gpt2 { get; } =
        @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+";

    /// <summary>Llama-3's pattern, from its <c>tokenizer.json</c>.</summary>
    public static string Llama3 { get; } =
        @"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+";

    /// <summary>Qwen2's pattern, from its <c>tokenizer.json</c>.</summary>
    public static string Qwen2 { get; } =
        @"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+";
}
```

- [ ] **Step 5: Write `BpeVocabulary`**

`src/DataNet.Embeddings/Tokenization/BpeVocabulary.cs`:

```csharp
namespace DataNet.Embeddings.Tokenization;

/// <summary>One line of a merge table: the two symbols it joins.</summary>
/// <remarks>
/// The pair's <em>index</em> in <see cref="BpeVocabulary.Merges"/> is its rank,
/// and rank is the whole algorithm — the lowest-ranked applicable merge is
/// always the next one applied. Reordering the list changes the tokenization.
/// </remarks>
/// <param name="Left">The symbol on the left of the join.</param>
/// <param name="Right">The symbol on the right of the join.</param>
public readonly record struct MergePair(string Left, string Right);

/// <summary>
/// A pretrained BPE model: its vocabulary, its ranked merge table, and the
/// pipeline flags that decide how text reaches them.
/// </summary>
/// <remarks>
/// Read from a <c>tokenizer.json</c> by <see cref="Persistence.TokenizerJsonLoader"/>
/// or from a <c>vocab.json</c>/<c>merges.txt</c> pair by
/// <c>BpeFilesLoader</c>. It restates what the file declared
/// and decides nothing itself.
/// </remarks>
/// <param name="Vocab">Token to id.</param>
/// <param name="Merges">The merge table in rank order; index 0 is rank 0.</param>
public sealed record BpeVocabulary(
    IReadOnlyDictionary<string, int> Vocab,
    IReadOnlyList<MergePair> Merges)
{
    /// <summary>Tokens added after training, which the model's own vocabulary does not contain.</summary>
    public IReadOnlyDictionary<string, int> AddedTokens { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Whether text is mapped through the byte alphabet before merging.</summary>
    public bool ByteLevel { get; init; }

    /// <summary>Whether a space is prepended to the input.</summary>
    public bool AddPrefixSpace { get; init; }

    /// <summary>Whether a whole pre-tokenized piece present in the vocabulary skips merging.</summary>
    public bool IgnoreMerges { get; init; }

    /// <summary>How many merge pairs named a token the vocabulary does not contain.</summary>
    /// <remarks>
    /// Such a pair cannot apply, so it is dropped rather than thrown on —
    /// HuggingFace tolerates it and refusing the file would be a divergence.
    /// Dropping it in silence would be worse, so it is counted here.
    /// </remarks>
    public int SkippedMerges { get; init; }

    /// <summary>The marker closing a word, e.g. <c>&lt;/w&gt;</c>; <see langword="null"/> for byte-level models.</summary>
    public string? EndOfWordSuffix { get; init; }

    /// <summary>The marker opening a non-initial piece; <see langword="null"/> when there is none.</summary>
    public string? ContinuingSubwordPrefix { get; init; }

    /// <summary>The unknown token, when the model declares one.</summary>
    public string? UnkToken { get; init; }

    /// <summary>The pattern text is split on before merging; <see langword="null"/> to split on whitespace.</summary>
    public string? PreTokenizerPattern { get; init; }

    /// <summary>Number of entries in the vocabulary.</summary>
    public int Count => Vocab.Count;

    /// <summary>Compares the flags, then every merge and every token-to-id mapping.</summary>
    /// <remarks>
    /// The generated equality compares <see cref="Vocab"/> and <see cref="Merges"/>
    /// by reference, so two vocabularies read from the same file would be
    /// unequal — the one comparison a caller has a reason to make.
    /// </remarks>
    public bool Equals(BpeVocabulary? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (other is null
            || ByteLevel != other.ByteLevel
            || AddPrefixSpace != other.AddPrefixSpace
            || IgnoreMerges != other.IgnoreMerges
            || SkippedMerges != other.SkippedMerges
            || !string.Equals(EndOfWordSuffix, other.EndOfWordSuffix, StringComparison.Ordinal)
            || !string.Equals(ContinuingSubwordPrefix, other.ContinuingSubwordPrefix, StringComparison.Ordinal)
            || !string.Equals(UnkToken, other.UnkToken, StringComparison.Ordinal)
            || !string.Equals(PreTokenizerPattern, other.PreTokenizerPattern, StringComparison.Ordinal)
            || Vocab.Count != other.Vocab.Count
            || Merges.Count != other.Merges.Count
            || AddedTokens.Count != other.AddedTokens.Count)
        {
            return false;
        }
        for (int i = 0; i < Merges.Count; i++)
        {
            if (!Merges[i].Equals(other.Merges[i]))
            {
                return false;
            }
        }
        return SameEntries(Vocab, other.Vocab) && SameEntries(AddedTokens, other.AddedTokens);
    }

    /// <summary>Hashes the scalars and the counts, which is O(1) and consistent with equality.</summary>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 31) + Vocab.Count;
            hash = (hash * 31) + Merges.Count;
            hash = (hash * 31) + AddedTokens.Count;
            hash = (hash * 31) + (ByteLevel ? 1 : 0);
            hash = (hash * 31) + (AddPrefixSpace ? 1 : 0);
            hash = (hash * 31) + (IgnoreMerges ? 1 : 0);
            hash = (hash * 31) + SkippedMerges;
            hash = (hash * 31) + (EndOfWordSuffix is null ? 0 : StringComparer.Ordinal.GetHashCode(EndOfWordSuffix));
            hash = (hash * 31) + (ContinuingSubwordPrefix is null ? 0 : StringComparer.Ordinal.GetHashCode(ContinuingSubwordPrefix));
            hash = (hash * 31) + (UnkToken is null ? 0 : StringComparer.Ordinal.GetHashCode(UnkToken));
            return (hash * 31) + (PreTokenizerPattern is null ? 0 : StringComparer.Ordinal.GetHashCode(PreTokenizerPattern));
        }
    }

    private static bool SameEntries(IReadOnlyDictionary<string, int> left, IReadOnlyDictionary<string, int> right)
    {
        foreach (KeyValuePair<string, int> entry in left)
        {
            if (!right.TryGetValue(entry.Key, out int id) || id != entry.Value)
            {
                return false;
            }
        }
        return true;
    }
}
```

- [ ] **Step 6: Run the tests**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~ValueEqualityTests"`
Expected: PASS, including the tests that were already there.

- [ ] **Step 7: Format, build, commit**

```bash
dotnet format DataNet.slnx && dotnet build DataNet.slnx --configuration Release
```

```bash
git add src/DataNet.Embeddings/Tokenization/BpeVocabulary.cs src/DataNet.Embeddings/Tokenization/BpePatterns.cs tests/DataNet.Embeddings.Tests/Tokenization/ValueEqualityTests.cs
git commit -m "Describe a BPE model as data the loaders fill in

BpeVocabulary is to BpeTokenizer what WordPieceVocabulary is to
WordPieceTokenizer: everything the file declared, and no decisions. Its
equality is hand-written for the same reason as the others -- the
generated one compares the vocabulary by reference, so two reads of one
file would come back unequal.

BpePatterns exposes properties rather than const fields: a const emits no
member reference, so the packaging gate could never see it.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Pre-tokenization

**Files:**
- Create: `src/DataNet.Embeddings/Tokenization/BpePreTokenizer.cs`
- Test: `tests/DataNet.Embeddings.Tests/Tokenization/BpePreTokenizeTests.cs`

**Interfaces:**
- Consumes: `BpePatterns`, `tests/oracles/bpe_pretokenize.json`.
- Produces: `internal sealed class BpePreTokenizer` with `BpePreTokenizer(string? pattern)` and `void Split(string text, List<string> pieces)`. `pattern: null` means split on whitespace, the classic-BPE path.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Tokenization;

public sealed class BpePreTokenizeTests
{
    /// <summary>
    /// The split is claimed for three model families but the vocabulary is
    /// vendored for one (ADR 0017), so this is the test carrying the Llama-3 and
    /// Qwen2 rows of the parity table. It compares the split output itself,
    /// before any merging, which is exactly what those rows promise.
    /// </summary>
    [Fact]
    public void Split_matches_tokenizers_for_every_pattern()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_pretokenize.json");
        JsonElement patterns = doc.RootElement.GetProperty("metadata").GetProperty("patterns");

        var failures = new List<string>();
        var pieces = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("pattern").GetString()!;
            string text = c.GetProperty("text").GetString()!;
            string[] expected = c.GetProperty("pieces").EnumerateArray().Select(e => e.GetString()!).ToArray();

            var splitter = new BpePreTokenizer(patterns.GetProperty(name).GetString());
            pieces.Clear();
            splitter.Split(text, pieces);

            if (!expected.SequenceEqual(pieces))
            {
                failures.Add($"[{name}] {JsonSerializer.Serialize(text)}\n  exp: [{string.Join(" | ", expected)}]\n  got: [{string.Join(" | ", pieces)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void The_patterns_shipped_are_the_patterns_proven()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_pretokenize.json");
        JsonElement patterns = doc.RootElement.GetProperty("metadata").GetProperty("patterns");

        Assert.Equal(patterns.GetProperty("gpt2").GetString(), BpePatterns.Gpt2);
        Assert.Equal(patterns.GetProperty("llama3").GetString(), BpePatterns.Llama3);
        Assert.Equal(patterns.GetProperty("qwen2").GetString(), BpePatterns.Qwen2);
    }

    [Fact]
    public void A_pathological_pattern_times_out_rather_than_hanging()
    {
        var splitter = new BpePreTokenizer("(a+)+$");
        var pieces = new List<string>();
        Assert.Throws<RegexMatchTimeoutException>(
            () => splitter.Split(new string('a', 40) + "!", pieces));
    }
}
```

Add `using System.Text.RegularExpressions;` at the top for the last test.

`The_patterns_shipped_are_the_patterns_proven` is the one that stops `BpePatterns` from drifting away from the corpus that proves it — the `"…"` placeholders left in Task 5 Step 4 fail here until they are filled in.

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~BpePreTokenizeTests"`
Expected: FAIL — `BpePreTokenizer` does not exist.

- [ ] **Step 3: Implement it**

```csharp
using System.Text.RegularExpressions;

namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// Splits text into the pieces the merge loop runs over, independently.
/// </summary>
/// <remarks>
/// <para>
/// A byte-level model declares the pattern it was trained with; the classic
/// lineage splits on whitespace instead. The split is not cosmetic — a merge
/// can never cross a piece boundary, so it decides which tokens are reachable
/// at all.
/// </para>
/// <para>
/// The pattern reaches here from a model file, so it is caller-supplied in every
/// sense that matters. It is compiled with <see cref="RegexDefaults.MatchTimeout"/>,
/// which turns unbounded backtracking into an exception instead of a hung thread.
/// </para>
/// </remarks>
internal sealed class BpePreTokenizer
{
    private static readonly Regex Whitespace =
        new(@"\S+", RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);

    private readonly Regex _pattern;

    public BpePreTokenizer(string? pattern) =>
        _pattern = pattern is null
            ? Whitespace
            : new Regex(pattern, RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);

    /// <summary>Appends the pieces of <paramref name="text"/> to <paramref name="pieces"/>.</summary>
    public void Split(string text, List<string> pieces)
    {
        foreach (Match match in _pattern.Matches(text))
        {
            pieces.Add(match.Value);
        }
    }
}
```

`RegexOptions.Compiled` is deliberately **not** used for the caller-supplied pattern: compiling costs milliseconds per distinct pattern and a tokenizer is built once per model, so it would be paid on a path that runs once. Revisit only if the benchmark says otherwise.

- [ ] **Step 4: Run the tests**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~BpePreTokenizeTests"`
Expected: PASS.

If `Split_matches_tokenizers_for_every_pattern` fails only on whitespace-heavy cases, the cause is almost certainly `\s`: HuggingFace's Rust regex reads it as the Unicode `White_Space` property, .NET as `[\f\n\r\t\v\x85\p{Z}]`. Report the diverging case to the user with both outputs before substituting an explicit character class — a silent rewrite of a model's own pattern is exactly the kind of change that must be visible in review.

- [ ] **Step 5: Format, build, commit**

```bash
dotnet format DataNet.slnx && dotnet build DataNet.slnx --configuration Release
```

```bash
git add src/DataNet.Embeddings/Tokenization/BpePreTokenizer.cs tests/DataNet.Embeddings.Tests/Tokenization/BpePreTokenizeTests.cs
git commit -m "Split text the way each model family was trained to

A merge never crosses a piece boundary, so the split decides which tokens
are reachable at all. The three patterns are replayed against the split
output HuggingFace produces, which is what the Llama-3 and Qwen2 rows of
the parity table actually claim -- and a test asserts the shipped patterns
are the ones the corpus proved, so they cannot drift apart.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 7: The merge loop and classic BPE

**Files:**
- Create: `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs`
- Test: `tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs`

**Interfaces:**
- Consumes: `BpeVocabulary`, `BpePreTokenizer`, `tests/oracles/bpe.json`, `tests/oracles/tiny_bpe.json`.
- Produces: `public sealed class BpeTokenizer : ISubwordTokenizer` with `BpeTokenizer(BpeVocabulary)`, `TokenizationResult Encode(string)`, `bool TryGetId(string, out int)`.

- [ ] **Step 1: Write the failing test**

`tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs`. It reads the tiny model straight from `tiny_bpe.json` with `System.Text.Json` rather than through the loader, because the loader is Task 12 — the merge loop is what is under test here.

```csharp
using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

public sealed class BpeTokenizerTests
{
    /// <summary>Reads tiny_bpe.json directly: this suite tests merging, not loading.</summary>
    internal static BpeVocabulary TinyVocabulary()
    {
        using JsonDocument doc = OracleLoader.Load("tiny_bpe.json");
        JsonElement model = doc.RootElement.GetProperty("model");

        var vocab = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (JsonProperty entry in model.GetProperty("vocab").EnumerateObject())
        {
            vocab[entry.Name] = entry.Value.GetInt32();
        }

        var merges = new List<MergePair>();
        foreach (JsonElement merge in model.GetProperty("merges").EnumerateArray())
        {
            if (merge.ValueKind == JsonValueKind.Array)
            {
                merges.Add(new MergePair(merge[0].GetString()!, merge[1].GetString()!));
            }
            else
            {
                string[] parts = merge.GetString()!.Split(' ');
                merges.Add(new MergePair(parts[0], parts[1]));
            }
        }

        return new BpeVocabulary(vocab, merges)
        {
            EndOfWordSuffix = model.GetProperty("end_of_word_suffix").GetString(),
            UnkToken = model.GetProperty("unk_token").GetString(),
        };
    }

    [Fact]
    public void Encode_matches_tokenizers()
    {
        using JsonDocument doc = OracleLoader.Load("bpe.json");
        var tokenizer = new BpeTokenizer(TinyVocabulary());

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            string[] expectedTokens = c.GetProperty("tokens").EnumerateArray().Select(e => e.GetString()!).ToArray();
            int[] expectedIds = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();

            TokenizationResult actual = tokenizer.Encode(text);
            if (!expectedTokens.SequenceEqual(actual.Tokens) || !expectedIds.SequenceEqual(actual.Ids))
            {
                failures.Add($"{JsonSerializer.Serialize(text)}\n  exp: [{string.Join(" | ", expectedTokens)}]\n  got: [{string.Join(" | ", actual.Tokens)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void TryGetId_finds_a_literal_entry()
    {
        var tokenizer = new BpeTokenizer(TinyVocabulary());
        Assert.True(tokenizer.TryGetId("[UNK]", out int unk));
        Assert.Equal(0, unk);
        Assert.False(tokenizer.TryGetId("definitely-not-a-token", out _));
    }

    [Fact]
    public void An_unknown_token_absent_from_the_vocabulary_is_refused()
    {
        BpeVocabulary broken = TinyVocabulary() with { UnkToken = "[NOPE]" };
        Assert.Throws<ArgumentException>(() => new BpeTokenizer(broken));
    }

    [Fact]
    public void A_merge_naming_a_missing_token_is_dropped()
    {
        BpeVocabulary vocab = TinyVocabulary();
        var merges = new List<MergePair>(vocab.Merges) { new("zzz", "qqq") };
        var tokenizer = new BpeTokenizer(vocab with { Merges = merges });

        // The pair cannot apply, so tokenization is unchanged.
        Assert.Equal(
            new BpeTokenizer(vocab).Encode("the quick brown fox").Ids,
            tokenizer.Encode("the quick brown fox").Ids);
    }
}
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~BpeTokenizerTests"`
Expected: FAIL — `BpeTokenizer` does not exist.

- [ ] **Step 3: Implement it**

```csharp
using System.Buffers;

namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// Byte-pair-encoding tokenizer, in both the character-level and byte-level
/// variants, reproducing HuggingFace <c>tokenizers</c>' <c>models.BPE</c>.
/// </summary>
/// <remarks>
/// <para>
/// A pre-tokenized piece starts as one symbol per character — or, byte-level,
/// one symbol per UTF-8 byte mapped through <see cref="ByteLevelAlphabet"/> —
/// and the lowest-ranked applicable merge is applied repeatedly until none
/// applies. Rank is the model: it is the order the pairs were learned in, and it
/// is what a merge table is for.
/// </para>
/// <para>
/// Merge pairs are resolved to pairs of ids once, at construction, so the merge
/// loop compares integers in a rented buffer and allocates nothing. Looking
/// candidates up by string in that loop is the cost this avoids.
/// </para>
/// <para>Thread-safe after construction: nothing here is mutable, and no result is cached.</para>
/// </remarks>
public sealed class BpeTokenizer : ISubwordTokenizer
{
    private const int StackThreshold = 256;

    private readonly Dictionary<string, int> _vocab;
    private readonly string[] _tokens;          // id -> token, the inverse of _vocab
    private readonly Dictionary<long, int> _ranks;   // (left << 32 | right) -> rank
    private readonly int[] _merged;             // rank -> the id the pair becomes
    private readonly BpePreTokenizer _split;
    private readonly BpeVocabulary _vocabulary;
    private readonly string? _endOfWord;
    private readonly int _unkId;
    private readonly bool _hasUnk;

    /// <summary>Creates a tokenizer from a loaded BPE model.</summary>
    /// <param name="vocabulary">A vocabulary from <c>BpeFilesLoader</c> or <see cref="Persistence.TokenizerJsonLoader"/>.</param>
    /// <exception cref="ArgumentException">The declared unknown token is not in the vocabulary.</exception>
    public BpeTokenizer(BpeVocabulary vocabulary)
    {
        Guard.NotNull(vocabulary);
        _vocabulary = vocabulary;
        _endOfWord = vocabulary.EndOfWordSuffix;

        _vocab = new Dictionary<string, int>(vocabulary.Vocab.Count, StringComparer.Ordinal);
        int maxId = -1;
        foreach (KeyValuePair<string, int> entry in vocabulary.Vocab)
        {
            _vocab[entry.Key] = entry.Value;
            maxId = Math.Max(maxId, entry.Value);
        }
        foreach (KeyValuePair<string, int> entry in vocabulary.AddedTokens)
        {
            _vocab[entry.Key] = entry.Value;
            maxId = Math.Max(maxId, entry.Value);
        }

        _tokens = new string[maxId + 1];
        foreach (KeyValuePair<string, int> entry in _vocab)
        {
            _tokens[entry.Value] = entry.Key;
        }

        if (vocabulary.UnkToken is { } unk)
        {
            if (!_vocab.TryGetValue(unk, out _unkId))
            {
                throw new ArgumentException(
                    $"The unknown token '{unk}' is not in the vocabulary.", nameof(vocabulary));
            }
            _hasUnk = true;
        }

        _ranks = new Dictionary<long, int>(vocabulary.Merges.Count);
        _merged = new int[vocabulary.Merges.Count];
        for (int rank = 0; rank < vocabulary.Merges.Count; rank++)
        {
            MergePair pair = vocabulary.Merges[rank];
            // A pair naming a token the vocabulary does not contain cannot apply.
            // HuggingFace tolerates it, so refusing the file would be a divergence;
            // BpeVocabulary.SkippedMerges is where the count is reported.
            if (!_vocab.TryGetValue(pair.Left, out int left)
                || !_vocab.TryGetValue(pair.Right, out int right)
                || !_vocab.TryGetValue(pair.Left + pair.Right, out int result))
            {
                _merged[rank] = -1;
                continue;
            }
            _ranks[Key(left, right)] = rank;
            _merged[rank] = result;
        }

        _split = new BpePreTokenizer(vocabulary.PreTokenizerPattern);
    }

    /// <summary>Tokenizes <paramref name="text"/> into sub-word tokens and their ids.</summary>
    /// <remarks>Matches <c>tokenizers.Tokenizer.encode(text)</c>, without the post-processor.</remarks>
    public TokenizationResult Encode(string text)
    {
        Guard.NotNull(text);
        var tokens = new List<string>();
        var ids = new List<int>();
        var pieces = new List<string>();
        _split.Split(_vocabulary.AddPrefixSpace ? " " + text : text, pieces);
        foreach (string piece in pieces)
        {
            EncodePiece(piece, tokens, ids);
        }
        return new TokenizationResult(tokens, ids);
    }

    /// <summary>Looks up a literal vocabulary entry, added tokens included.</summary>
    /// <remarks>Matches <c>tokenizers.Tokenizer.token_to_id(token)</c>.</remarks>
    /// <param name="token">The token string.</param>
    /// <param name="id">Receives the id when the token is present.</param>
    public bool TryGetId(string token, out int id)
    {
        Guard.NotNull(token);
        return _vocab.TryGetValue(token, out id);
    }

    private static long Key(int left, int right) => ((long)left << 32) | (uint)right;

    private void EncodePiece(string piece, List<string> tokens, List<int> ids)
    {
        if (piece.Length == 0)
        {
            return;
        }

        int[]? rented = null;
        // One symbol per character is the upper bound for the classic path; the
        // byte-level path rents against its byte count in Task 8.
        Span<int> symbols = piece.Length <= StackThreshold
            ? stackalloc int[piece.Length]
            : (rented = ArrayPool<int>.Shared.Rent(piece.Length)).AsSpan(0, piece.Length);
        try
        {
            int count = InitialSymbols(piece, symbols);
            if (count < 0)
            {
                EmitUnknown(tokens, ids);
                return;
            }
            count = Merge(symbols, count);
            for (int i = 0; i < count; i++)
            {
                ids.Add(symbols[i]);
                tokens.Add(_tokens[symbols[i]]);
            }
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<int>.Shared.Return(rented);
            }
        }
    }

    /// <summary>Fills <paramref name="symbols"/> with one id per character; -1 when a character is uncovered.</summary>
    private int InitialSymbols(string piece, Span<int> symbols)
    {
        for (int i = 0; i < piece.Length; i++)
        {
            bool last = i == piece.Length - 1;
            string symbol = last && _endOfWord is not null
                ? piece[i] + _endOfWord
                : piece[i].ToString();
            if (!_vocab.TryGetValue(symbol, out int id))
            {
                return -1;
            }
            symbols[i] = id;
        }
        return piece.Length;
    }

    /// <summary>Applies the lowest-ranked applicable merge until none applies. Returns the new symbol count.</summary>
    private int Merge(Span<int> symbols, int count)
    {
        while (count > 1)
        {
            int bestRank = int.MaxValue;
            int bestAt = -1;
            for (int i = 0; i + 1 < count; i++)
            {
                if (_ranks.TryGetValue(Key(symbols[i], symbols[i + 1]), out int rank) && rank < bestRank)
                {
                    bestRank = rank;
                    bestAt = i;
                }
            }
            if (bestAt < 0)
            {
                break;
            }
            symbols[bestAt] = _merged[bestRank];
            for (int i = bestAt + 1; i + 1 < count; i++)
            {
                symbols[i] = symbols[i + 1];
            }
            count--;
        }
        return count;
    }

    private void EmitUnknown(List<string> tokens, List<int> ids)
    {
        if (_hasUnk)
        {
            tokens.Add(_tokens[_unkId]);
            ids.Add(_unkId);
        }
    }
}
```

- [ ] **Step 4: Run the tests**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~BpeTokenizerTests"`
Expected: PASS.

If `Encode_matches_tokenizers` fails on a piece containing an uncovered character, check what HuggingFace does with it in `bpe.json` — the `[UNK]` behaviour here is per-piece, and the corpus is the authority on whether that matches. Do not adjust the corpus to fit the code.

- [ ] **Step 5: Format, build both targets, commit**

```bash
dotnet format DataNet.slnx && dotnet build DataNet.slnx --configuration Release && dotnet test DataNet.slnx --configuration Release --no-build
```

```bash
git add src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs
git commit -m "Merge byte pairs by rank, over integers rather than strings

Every merge pair is resolved to a pair of ids once, at construction, so
the loop that runs per word compares integers in a rented buffer and
allocates nothing -- the string lookup per candidate that the existing
tokenizers pay is designed out rather than optimised.

The scan for the lowest applicable rank is linear per merge, which on a
pre-tokenized piece of a handful of symbols beats a heap that would cost
an allocation or a reset per word.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 8: The byte-level path

**Files:**
- Modify: `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs`
- Test: `tests/DataNet.Embeddings.Tests/ByteLevelBpeTests.cs`

**Interfaces:**
- Consumes: `ByteLevelAlphabet`, `tests/oracles/bytelevel_bpe.json`, `gpt2_vocab.json`, `gpt2_merges.txt`.
- Produces: `BpeTokenizer` honouring `BpeVocabulary.ByteLevel`. Adds `internal static BpeVocabulary Gpt2Vocabulary()` to the test class, reused by Tasks 9 and 13.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

public sealed class ByteLevelBpeTests
{
    /// <summary>
    /// Builds GPT-2's model from the vendored files directly, so a failure here
    /// is the tokenizer rather than the loader (which Task 11 covers).
    /// </summary>
    internal static BpeVocabulary Gpt2Vocabulary()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "oracles");
        using JsonDocument vocabDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "gpt2_vocab.json")));

        var vocab = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (JsonProperty entry in vocabDoc.RootElement.EnumerateObject())
        {
            vocab[entry.Name] = entry.Value.GetInt32();
        }

        var merges = new List<MergePair>();
        foreach (string line in File.ReadAllLines(Path.Combine(dir, "gpt2_merges.txt")))
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }
            int space = line.IndexOf(' ');
            merges.Add(new MergePair(line.Substring(0, space), line.Substring(space + 1)));
        }

        return new BpeVocabulary(vocab, merges)
        {
            ByteLevel = true,
            PreTokenizerPattern = BpePatterns.Gpt2,
        };
    }

    [Fact]
    public void Encode_matches_tokenizers_over_the_gpt2_vocabulary()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_bpe.json");
        var tokenizer = new BpeTokenizer(Gpt2Vocabulary());

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            string[] expectedTokens = c.GetProperty("tokens").EnumerateArray().Select(e => e.GetString()!).ToArray();
            int[] expectedIds = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();

            TokenizationResult actual = tokenizer.Encode(text);
            if (!expectedTokens.SequenceEqual(actual.Tokens) || !expectedIds.SequenceEqual(actual.Ids))
            {
                failures.Add($"{JsonSerializer.Serialize(text)}\n  exp: [{string.Join(" | ", expectedTokens)}]\n  got: [{string.Join(" | ", actual.Tokens)}]\n  exp ids: [{string.Join(", ", expectedIds)}]\n  got ids: [{string.Join(", ", actual.Ids)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void The_vendored_vocabulary_is_the_one_the_corpus_was_built_from()
    {
        // 50 257 is GPT-2's size. A fixture that silently changed shape would
        // otherwise surface as a wall of token diffs rather than as itself.
        Assert.Equal(50257, Gpt2Vocabulary().Count);
    }
}
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~ByteLevelBpeTests"`
Expected: FAIL — the byte-level path is not implemented, so tokens come back wrong (or the piece is refused as unknown).

- [ ] **Step 3: Implement the byte-level symbol construction**

In `BpeTokenizer`, add a field set from the vocabulary:

```csharp
    private readonly bool _byteLevel;
```

```csharp
        _byteLevel = vocabulary.ByteLevel;
```

Replace `EncodePiece` with a version that sizes the buffer by byte count when byte-level, and replace `InitialSymbols` with one that maps bytes:

```csharp
    private void EncodePiece(string piece, List<string> tokens, List<int> ids)
    {
        if (piece.Length == 0)
        {
            return;
        }

        // Byte-level turns one character into up to four symbols, so the buffer is
        // sized in bytes; the classic path is one symbol per character.
        int capacity = _byteLevel ? JsonArtifact.Utf8NoBom.GetByteCount(piece) : piece.Length;
        if (capacity == 0)
        {
            return;
        }

        int[]? rented = null;
        Span<int> symbols = capacity <= StackThreshold
            ? stackalloc int[capacity]
            : (rented = ArrayPool<int>.Shared.Rent(capacity)).AsSpan(0, capacity);
        try
        {
            int count = _byteLevel ? ByteLevelSymbols(piece, symbols) : InitialSymbols(piece, symbols);
            if (count < 0)
            {
                EmitUnknown(tokens, ids);
                return;
            }
            count = Merge(symbols, count);
            for (int i = 0; i < count; i++)
            {
                ids.Add(symbols[i]);
                tokens.Add(_tokens[symbols[i]]);
            }
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<int>.Shared.Return(rented);
            }
        }
    }

    /// <summary>Fills <paramref name="symbols"/> with one id per UTF-8 byte of <paramref name="piece"/>.</summary>
    /// <remarks>
    /// One byte, one symbol: a four-byte emoji enters the merge loop as four
    /// symbols. That is where the round-trip guarantee comes from — every byte of
    /// the input is represented, so decoding can put them back.
    /// </remarks>
    private int ByteLevelSymbols(string piece, Span<int> symbols)
    {
        byte[] bytes = JsonArtifact.Utf8NoBom.GetBytes(piece);
        for (int i = 0; i < bytes.Length; i++)
        {
            string symbol = ByteLevelAlphabet.ToChar(bytes[i]).ToString();
            if (!_vocab.TryGetValue(symbol, out int id))
            {
                throw new ArgumentException(
                    $"The vocabulary has no entry for byte 0x{bytes[i]:X2} ('{symbol}'); it is not a byte-level model.");
            }
            symbols[i] = id;
        }
        return bytes.Length;
    }
```

Add `using DataNet.Internal.Persistence;` for `JsonArtifact.Utf8NoBom` — the repository's shared UTF-8 encoding without a byte-order mark. If that type is not reachable from `Tokenization`, use `new UTF8Encoding(false)` in a `static readonly` field rather than `Encoding.UTF8`, whose `GetBytes` is BOM-free but whose `GetString` is not the same object the rest of the repository uses.

The byte-to-string allocation in `ByteLevelSymbols` is one small string per byte and it is the first thing Task 13's benchmark will show. Leave it as written until there are numbers: a `FrozenDictionary` with `GetAlternateLookup<ReadOnlySpan<char>>` under `#if NET9_0_OR_GREATER`, keyed off a one-character span of a reused buffer, is the fix, and it belongs in the commit that shows it was needed.

- [ ] **Step 4: Run the tests**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~ByteLevelBpeTests"`
Expected: PASS on all 20 cases.

Diagnosis order if it fails:
- Wrong on **every** case including `"Hello, world!"` → the alphabet or the split. Task 4 and Task 6 pass, so suspect `AddPrefixSpace` or the vocabulary read.
- Wrong only on **whitespace** cases → the split pattern, see Task 6 Step 4.
- Wrong only on **CJK/emoji** → the UTF-8 encoding step, most likely a surrogate pair handled per-`char` rather than per-byte.
- Wrong only on **long** inputs → the merge order, i.e. the scan in `Merge` picking a later rank.

- [ ] **Step 5: Format, build, run the whole suite, commit**

```bash
dotnet format DataNet.slnx && dotnet build DataNet.slnx --configuration Release && dotnet test DataNet.slnx --configuration Release --no-build
```

The full run matters here: this is the first task whose fixture the netstandard suite must also find, so a missing `*.txt` glob surfaces now.

```bash
git add src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs tests/DataNet.Embeddings.Tests/ByteLevelBpeTests.cs
git commit -m "Tokenize GPT-2 byte for byte, emoji and all

The byte-level variant maps each UTF-8 byte to one symbol before merging,
so a four-byte emoji enters the loop as four symbols and every byte of the
input is represented. Replayed against HuggingFace over GPT-2's real
50 257-entry vocabulary: CJK, emoji, repeated whitespace, and text naming
the special tokens literally.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 9: `Decode`

**Files:**
- Modify: `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs`
- Modify: `tests/DataNet.Embeddings.Tests/ByteLevelBpeTests.cs`, `tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs`

**Interfaces:**
- Consumes: `bytelevel_bpe.json` fields `decoded` and `decoded_skip_specials`.
- Produces: `public string Decode(IReadOnlyList<int> ids, bool skipSpecialTokens = false)` and `public string Decode(ReadOnlySpan<int> ids, bool skipSpecialTokens = false)`.

- [ ] **Step 1: Write the failing tests**

Append to `ByteLevelBpeTests`:

```csharp
    [Fact]
    public void Decode_matches_tokenizers_in_both_modes()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_bpe.json");
        var tokenizer = new BpeTokenizer(Gpt2Vocabulary());

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            int[] ids = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();
            string expected = c.GetProperty("decoded").GetString()!;
            string expectedSkipping = c.GetProperty("decoded_skip_specials").GetString()!;

            string actual = tokenizer.Decode(ids);
            string actualSkipping = tokenizer.Decode(ids, skipSpecialTokens: true);
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                failures.Add($"decode {JsonSerializer.Serialize(c.GetProperty("text").GetString())}\n  exp: {JsonSerializer.Serialize(expected)}\n  got: {JsonSerializer.Serialize(actual)}");
            }
            if (!string.Equals(expectedSkipping, actualSkipping, StringComparison.Ordinal))
            {
                failures.Add($"decode(skip) {JsonSerializer.Serialize(c.GetProperty("text").GetString())}\n  exp: {JsonSerializer.Serialize(expectedSkipping)}\n  got: {JsonSerializer.Serialize(actualSkipping)}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The property byte-level BPE exists to guarantee. It is asserted separately
    /// from the oracle comparison because it is the claim a user relies on, and
    /// because a mapping-table error can satisfy neither while looking like only
    /// one is broken.
    /// </summary>
    [Fact]
    public void Decode_of_Encode_is_the_input()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_bpe.json");
        var tokenizer = new BpeTokenizer(Gpt2Vocabulary());

        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            Assert.Equal(text, tokenizer.Decode(tokenizer.Encode(text).Ids));
        }
    }

    [Fact]
    public void The_span_overload_agrees_with_the_list_one()
    {
        var tokenizer = new BpeTokenizer(Gpt2Vocabulary());
        int[] ids = [.. tokenizer.Encode("round trip 東京 👋").Ids];
        Assert.Equal(tokenizer.Decode(ids), tokenizer.Decode(ids.AsSpan()));
    }

    [Fact]
    public void Decode_rejects_an_id_outside_the_vocabulary()
    {
        var tokenizer = new BpeTokenizer(Gpt2Vocabulary());
        Assert.Throws<ArgumentOutOfRangeException>(() => tokenizer.Decode(new[] { 999_999 }));
    }
```

Append to `BpeTokenizerTests`:

```csharp
    [Fact]
    public void Decode_turns_the_end_of_word_marker_back_into_a_space()
    {
        var tokenizer = new BpeTokenizer(TinyVocabulary());
        TokenizationResult encoded = tokenizer.Encode("the quick brown fox");
        Assert.Equal("the quick brown fox", tokenizer.Decode(encoded.Ids));
    }
```

- [ ] **Step 2: Run them to verify they fail**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~ByteLevelBpeTests|FullyQualifiedName~BpeTokenizerTests"`
Expected: FAIL — `Decode` does not exist.

- [ ] **Step 3: Implement it**

Add to `BpeTokenizer` a set of added-token ids, populated in the constructor:

```csharp
    private readonly HashSet<int> _addedIds;
```

```csharp
        _addedIds = [.. vocabulary.AddedTokens.Values];
```

Then:

```csharp
    /// <summary>Reassembles the text <paramref name="ids"/> encode.</summary>
    /// <remarks>
    /// <para>
    /// Matches <c>tokenizers.Tokenizer.decode(ids, skip_special_tokens=…)</c>.
    /// For a byte-level model this is exact: every byte of the input was mapped
    /// to a symbol, so every byte comes back.
    /// </para>
    /// <para>
    /// The default is deliberately the opposite of HuggingFace's. Python skips
    /// special tokens unless told otherwise; here the round trip is exact unless
    /// asked otherwise, because a <c>Decode</c> that silently drops tokens makes
    /// <c>Decode(Encode(x)) == x</c> false in exactly the case a caller would
    /// write to check it.
    /// </para>
    /// </remarks>
    /// <param name="ids">Token ids, e.g. from <see cref="Encode"/>.</param>
    /// <param name="skipSpecialTokens">Drop added tokens instead of rendering them.</param>
    /// <exception cref="ArgumentOutOfRangeException">An id is outside the vocabulary.</exception>
    public string Decode(IReadOnlyList<int> ids, bool skipSpecialTokens = false)
    {
        Guard.NotNull(ids);
        var buffer = new StringBuilder();
        for (int i = 0; i < ids.Count; i++)
        {
            Append(buffer, ids[i], skipSpecialTokens);
        }
        return Finish(buffer);
    }

    /// <summary>Reassembles the text <paramref name="ids"/> encode.</summary>
    /// <param name="ids">Token ids, e.g. from <see cref="Encode"/>.</param>
    /// <param name="skipSpecialTokens">Drop added tokens instead of rendering them.</param>
    /// <exception cref="ArgumentOutOfRangeException">An id is outside the vocabulary.</exception>
    public string Decode(ReadOnlySpan<int> ids, bool skipSpecialTokens = false)
    {
        var buffer = new StringBuilder();
        for (int i = 0; i < ids.Length; i++)
        {
            Append(buffer, ids[i], skipSpecialTokens);
        }
        return Finish(buffer);
    }

    private void Append(StringBuilder buffer, int id, bool skipSpecialTokens)
    {
        if (id < 0 || id >= _tokens.Length || _tokens[id] is null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id), id, $"The id is outside the vocabulary [0, {_tokens.Length}).");
        }
        if (skipSpecialTokens && _addedIds.Contains(id))
        {
            return;
        }
        buffer.Append(_tokens[id]);
    }

    /// <summary>Turns the concatenated tokens back into text.</summary>
    private string Finish(StringBuilder buffer)
    {
        if (!_byteLevel)
        {
            // The classic lineage marks a word's end rather than its leading space,
            // so the marker is what a space was.
            return _endOfWord is null
                ? buffer.ToString()
                : buffer.Replace(_endOfWord, " ").ToString().TrimEnd();
        }

        // Every character stands for one byte; anything else never came from Encode.
        byte[] bytes = new byte[buffer.Length];
        int n = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (ByteLevelAlphabet.TryToByte(buffer[i], out byte value))
            {
                bytes[n] = value;
                n++;
            }
        }
        return JsonArtifact.Utf8NoBom.GetString(bytes, 0, n);
    }
```

Add `using System.Text;`.

- [ ] **Step 4: Run the tests**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~ByteLevelBpeTests|FullyQualifiedName~BpeTokenizerTests"`
Expected: PASS.

If `Decode_of_Encode_is_the_input` fails only on the trailing-space case, the classic `TrimEnd` in `Finish` has leaked into the byte-level path — it must not. If it fails on emoji, `TryToByte` is rejecting a character it should map; re-run Task 4's tests first.

- [ ] **Step 5: Format, build, commit**

```bash
dotnet format DataNet.slnx && dotnet build DataNet.slnx --configuration Release && dotnet test DataNet.slnx --configuration Release --no-build
```

```bash
git add src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs tests/DataNet.Embeddings.Tests/ByteLevelBpeTests.cs tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs
git commit -m "Decode back to the exact input, including CJK and emoji

A tokenizer that cannot decode is half a tokenizer, and byte-level BPE is
reversible by construction -- so the round trip is asserted as its own
property, not merely inferred from matching tokens.

The default is the opposite of HuggingFace's: special tokens are kept
unless asked otherwise. Dropping them by default would make
Decode(Encode(x)) == x false in exactly the case a caller writes to
check it.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 10: `ignore_merges`

**Files:**
- Modify: `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs`
- Modify: `tools/generate_oracles.py`, `tests/oracles/bytelevel_bpe.json`
- Modify: `tests/DataNet.Embeddings.Tests/ByteLevelBpeTests.cs`

**Interfaces:**
- Consumes: `BpeVocabulary.IgnoreMerges`.
- Produces: `Encode` short-circuiting a whole piece present in the vocabulary. New oracle field: `cases[].tokens_ignore_merges` and `cases[].ids_ignore_merges` in `bytelevel_bpe.json`.

- [ ] **Step 1: Extend the oracle**

In `generate_bytelevel_bpe`, build a second tokenizer with the flag on and add two fields per case. `tokenizers` reads `ignore_merges` from the model, so set it on the deserialized JSON:

```python
    import json as _json  # noqa: PLC0415
    from tokenizers import Tokenizer as _Tokenizer  # noqa: PLC0415

    spec = _json.loads(tokenizer.to_str())
    spec["model"]["ignore_merges"] = True
    ignoring = _Tokenizer.from_str(_json.dumps(spec))
```

and inside the case loop:

```python
        enc_ignoring = ignoring.encode(text)
        cases[-1]["tokens_ignore_merges"] = enc_ignoring.tokens
        cases[-1]["ids_ignore_merges"] = enc_ignoring.ids
```

- [ ] **Step 2: Regenerate and confirm the flag changes something**

Run: `cd /tmp && PYTHONSAFEPATH=1 /home/cyril/Documents/devs/data.net/.venv-oracles/bin/python /home/cyril/Documents/devs/data.net/tools/generate_oracles.py; echo "exit=$?"`

```bash
python3 -c "
import json
d=json.load(open('tests/oracles/bytelevel_bpe.json'))
diff=[c['text'] for c in d['cases'] if c['ids']!=c['ids_ignore_merges']]
print(len(diff),'cases differ:',diff[:5])
"
```

Expected: at least one case differs. If **zero** differ, the corpus cannot tell the two code paths apart and the test below would pass without the feature existing. Add a text to `BPE_TEXTS` that GPT-2's vocabulary contains as a single entry but would merge differently — try `" indivisible"` or `" tokenization"` — and regenerate until the count is non-zero. Report the chosen text to the user.

- [ ] **Step 3: Write the failing test**

Append to `ByteLevelBpeTests`:

```csharp
    /// <summary>
    /// Llama-3 declares ignore_merges, so a loader that refused it would put that
    /// family out of reach. With it on, a pre-tokenized piece that is itself a
    /// vocabulary entry is emitted whole instead of being merged up to.
    /// </summary>
    [Fact]
    public void IgnoreMerges_emits_a_whole_piece_present_in_the_vocabulary()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_bpe.json");
        var tokenizer = new BpeTokenizer(Gpt2Vocabulary() with { IgnoreMerges = true });

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            int[] expected = c.GetProperty("ids_ignore_merges").EnumerateArray().Select(e => e.GetInt32()).ToArray();
            int[] actual = [.. tokenizer.Encode(text).Ids];
            if (!expected.SequenceEqual(actual))
            {
                failures.Add($"{JsonSerializer.Serialize(text)}\n  exp: [{string.Join(", ", expected)}]\n  got: [{string.Join(", ", actual)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
```

- [ ] **Step 4: Run it to verify it fails**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~IgnoreMerges"`
Expected: FAIL on the cases identified in Step 2.

- [ ] **Step 5: Implement it**

Add the field and set it in the constructor:

```csharp
    private readonly bool _ignoreMerges;
```

```csharp
        _ignoreMerges = vocabulary.IgnoreMerges;
```

At the top of `EncodePiece`, after the empty check:

```csharp
        // ignore_merges: a piece that is itself a vocabulary entry is emitted whole.
        // Llama-3 declares this, and without it that family tokenizes differently
        // while looking entirely plausible.
        if (_ignoreMerges)
        {
            string mapped = _byteLevel ? MapBytes(piece) : piece;
            if (_vocab.TryGetValue(mapped, out int whole))
            {
                ids.Add(whole);
                tokens.Add(_tokens[whole]);
                return;
            }
        }
```

and a helper:

```csharp
    /// <summary>The piece as the byte alphabet renders it, which is how the vocabulary spells it.</summary>
    private static string MapBytes(string piece)
    {
        byte[] bytes = JsonArtifact.Utf8NoBom.GetBytes(piece);
        var mapped = new StringBuilder(bytes.Length);
        for (int i = 0; i < bytes.Length; i++)
        {
            mapped.Append(ByteLevelAlphabet.ToChar(bytes[i]));
        }
        return mapped.ToString();
    }
```

- [ ] **Step 6: Run the tests**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~ByteLevelBpeTests"`
Expected: PASS, including `Encode_matches_tokenizers_over_the_gpt2_vocabulary` — the flag is off there and must stay behaviour-neutral.

- [ ] **Step 7: Format, build, commit**

```bash
dotnet format DataNet.slnx && dotnet build DataNet.slnx --configuration Release && dotnet test DataNet.slnx --configuration Release --no-build
```

```bash
git add src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs tools/generate_oracles.py tests/oracles/bytelevel_bpe.json tests/DataNet.Embeddings.Tests/ByteLevelBpeTests.cs
git commit -m "Honour ignore_merges, which is what Llama-3 declares

A piece that is itself a vocabulary entry is emitted whole rather than
merged up to. Refusing the flag would have put the whole Llama-3 family
out of reach for five lines of work, and honouring it wrongly is invisible
-- the tokens stay plausible and only the embeddings are wrong.

The corpus was checked to actually distinguish the two paths before the
test was written; a flag no case exercises proves nothing.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 11: `BpeFilesLoader`

**Files:**
- Create: `src/DataNet.Embeddings/Persistence/BpeFilesLoader.cs`
- Test: `tests/DataNet.Embeddings.Tests/Persistence/BpeFilesLoaderTests.cs`

**Interfaces:**
- Consumes: `BpeVocabulary`, `ArtifactLoadOptions`, `JsonArtifact`, `tests/oracles/gpt2_vocab.json`, `gpt2_merges.txt`.
- Produces: `public static class BpeFilesLoader` with `Load(Stream, Stream, ArtifactLoadOptions?, bool)`, `Load(string, string, ArtifactLoadOptions?, bool)`, `LoadAsync(Stream, Stream, ArtifactLoadOptions?, bool, CancellationToken)`.

- [ ] **Step 1: Read the loader this one mirrors**

Read `src/DataNet.Embeddings/Persistence/VocabTxtLoader.cs` in full: the three-overload shape (`Stream`, `path`, `Async`), `ArtifactLoadOptions.LimitsOf`, `JsonArtifact.ReadAllBytes`/`OpenRead`, the `SourceName` constant used in every message, and the BOM handling. Follow it exactly, including calling the limits *before* allocating from the input.

- [ ] **Step 2: Write the failing test**

```csharp
using System.Text;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Persistence;

public sealed class BpeFilesLoaderTests
{
    private const string Vocab = """{"a":0,"b":1,"ab":2,"[UNK]":3}""";
    private const string Merges = "#version: 0.2\na b\n";

    private static Stream Utf8(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    [Fact]
    public void Load_reads_the_vocabulary_and_the_ranked_merges()
    {
        BpeVocabulary vocab = BpeFilesLoader.Load(Utf8(Vocab), Utf8(Merges));

        Assert.Equal(4, vocab.Count);
        Assert.Equal(2, vocab.Vocab["ab"]);
        Assert.Single(vocab.Merges);
        Assert.Equal(new MergePair("a", "b"), vocab.Merges[0]);
        Assert.True(vocab.ByteLevel);
        Assert.Equal(BpePatterns.Gpt2, vocab.PreTokenizerPattern);
    }

    [Fact]
    public void The_version_comment_is_not_a_merge()
    {
        BpeVocabulary vocab = BpeFilesLoader.Load(Utf8(Vocab), Utf8(Merges));
        Assert.DoesNotContain(vocab.Merges, m => m.Left.StartsWith('#'));
    }

    [Fact]
    public void A_merge_line_without_a_separator_is_refused()
    {
        Assert.Throws<InvalidDataException>(
            () => BpeFilesLoader.Load(Utf8(Vocab), Utf8("#version: 0.2\nab\n")));
    }

    [Fact]
    public void An_empty_vocabulary_is_refused()
    {
        Assert.Throws<InvalidDataException>(() => BpeFilesLoader.Load(Utf8("{}"), Utf8(Merges)));
    }

    [Fact]
    public void A_vocabulary_over_the_limit_is_refused()
    {
        var bounds = new ArtifactLoadOptions { MaxVocabularySize = 2 };
        Assert.Throws<InvalidDataException>(() => BpeFilesLoader.Load(Utf8(Vocab), Utf8(Merges), bounds));
    }

    [Fact]
    public void The_classic_layout_is_not_byte_level()
    {
        BpeVocabulary vocab = BpeFilesLoader.Load(Utf8(Vocab), Utf8(Merges), byteLevel: false);
        Assert.False(vocab.ByteLevel);
        Assert.Null(vocab.PreTokenizerPattern);
    }

    /// <summary>The real files, which is the layout the loader exists for.</summary>
    [Fact]
    public void Load_reads_the_vendored_gpt2_files()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "oracles");
        BpeVocabulary vocab = BpeFilesLoader.Load(
            Path.Combine(dir, "gpt2_vocab.json"),
            Path.Combine(dir, "gpt2_merges.txt"),
            new ArtifactLoadOptions { MaxTotalBytes = 8L * 1024 * 1024, MaxVocabularySize = 100_000, MaxArrayLength = 100_000 });

        Assert.Equal(50257, vocab.Count);
        Assert.Equal(0, vocab.SkippedMerges);
        Assert.Equal(new BpeTokenizer(ByteLevelBpeTests.Gpt2Vocabulary()).Encode("Hello, world!").Ids,
                     new BpeTokenizer(vocab).Encode("Hello, world!").Ids);
    }

    [Fact]
    public async Task LoadAsync_agrees_with_the_synchronous_overload()
    {
        BpeVocabulary sync = BpeFilesLoader.Load(Utf8(Vocab), Utf8(Merges));
        BpeVocabulary async = await BpeFilesLoader.LoadAsync(Utf8(Vocab), Utf8(Merges));
        Assert.Equal(sync, async);
    }
}
```

- [ ] **Step 3: Run it to verify it fails**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~BpeFilesLoaderTests"`
Expected: FAIL — `BpeFilesLoader` does not exist.

- [ ] **Step 4: Implement it**

```csharp
using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using DataNet.Internal.Persistence;

namespace DataNet.Embeddings.Persistence;

/// <summary>
/// Reads the <c>vocab.json</c> + <c>merges.txt</c> pair GPT-2 ships, the layout
/// that predates <c>tokenizer.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// Matches <c>tokenizers.models.BPE.from_file(vocab, merges)</c>. The two files
/// carry the model and nothing else: what pattern the text was split on, and
/// whether the model is byte-level, live in the tokenizer configuration beside
/// them. Pass them here, or use <see cref="TokenizerJsonLoader.LoadBpe(string, ArtifactLoadOptions?)"/>,
/// which reads them from the file.
/// </para>
/// <para>
/// The defaults describe GPT-2, because that is the model this layout is almost
/// always found in.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// BpeVocabulary vocab = BpeFilesLoader.Load("gpt2/vocab.json", "gpt2/merges.txt");
/// var tokenizer = new BpeTokenizer(vocab);
/// </code>
/// </example>
public static class BpeFilesLoader
{
    private const string SourceName = "merges.txt";
    private static readonly char[] LineTerminators = ['\n', '\r'];

    /// <summary>Reads a BPE model from two streams.</summary>
    /// <param name="vocabJson">A <c>vocab.json</c>: a JSON object of token to id. Never disposed by this method.</param>
    /// <param name="merges">A <c>merges.txt</c>: one space-separated pair per line, in rank order. Never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="byteLevel">Whether the model tokenizes through the byte alphabet; <see langword="true"/> describes GPT-2.</param>
    /// <exception cref="InvalidDataException">Either file is malformed, empty, or exceeds a limit.</exception>
    public static BpeVocabulary Load(
        Stream vocabJson,
        Stream merges,
        ArtifactLoadOptions? options = null,
        bool byteLevel = true)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        return Parse(
            JsonArtifact.ReadAllBytes(vocabJson, limits),
            JsonArtifact.ReadAllBytes(merges, limits),
            limits,
            byteLevel);
    }

    /// <summary>Reads a BPE model from two files.</summary>
    /// <param name="vocabJsonPath">Path to a <c>vocab.json</c>.</param>
    /// <param name="mergesPath">Path to a <c>merges.txt</c>.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="byteLevel">Whether the model tokenizes through the byte alphabet.</param>
    /// <exception cref="InvalidDataException">Either file is malformed, empty, or exceeds a limit.</exception>
    public static BpeVocabulary Load(
        string vocabJsonPath,
        string mergesPath,
        ArtifactLoadOptions? options = null,
        bool byteLevel = true)
    {
        using FileStream vocabFile = JsonArtifact.OpenRead(vocabJsonPath);
        using FileStream mergesFile = JsonArtifact.OpenRead(mergesPath);
        return Load(vocabFile, mergesFile, options, byteLevel);
    }

    /// <summary>Asynchronous counterpart of <see cref="Load(Stream, Stream, ArtifactLoadOptions?, bool)"/>.</summary>
    /// <param name="vocabJson">A <c>vocab.json</c>; never disposed by this method.</param>
    /// <param name="merges">A <c>merges.txt</c>; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="byteLevel">Whether the model tokenizes through the byte alphabet.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    public static async Task<BpeVocabulary> LoadAsync(
        Stream vocabJson,
        Stream merges,
        ArtifactLoadOptions? options = null,
        bool byteLevel = true,
        CancellationToken cancellationToken = default)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        byte[] vocabPayload = await JsonArtifact.ReadAllBytesAsync(vocabJson, limits, cancellationToken).ConfigureAwait(false);
        byte[] mergesPayload = await JsonArtifact.ReadAllBytesAsync(merges, limits, cancellationToken).ConfigureAwait(false);
        return Parse(vocabPayload, mergesPayload, limits, byteLevel);
    }

    private static BpeVocabulary Parse(
        byte[] vocabPayload,
        byte[] mergesPayload,
        in ArtifactLimits limits,
        bool byteLevel)
    {
        var vocab = new Dictionary<string, int>(StringComparer.Ordinal);
        using (JsonDocument doc = JsonArtifact.Parse(vocabPayload, limits))
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("The vocab.json is not a JSON object of token to id.");
            }
            foreach (JsonProperty entry in doc.RootElement.EnumerateObject())
            {
                limits.CheckTokenLength(entry.Name.Length);
                limits.CheckVocabularySize(vocab.Count + 1);
                vocab[entry.Name] = entry.Value.GetInt32();
            }
        }
        if (vocab.Count == 0)
        {
            throw new InvalidDataException("The vocab.json is empty: a vocabulary needs at least one token.");
        }

        var merges = new List<MergePair>();
        string text = JsonArtifact.Utf8NoBom.GetString(mergesPayload, 0, mergesPayload.Length);
        int start = 0;
        while (start < text.Length)
        {
            int terminator = text.IndexOfAny(LineTerminators, start);
            int stop = terminator < 0 ? text.Length : terminator;
            ParseMergeLine(text, start, stop, merges, limits);
            if (stop >= text.Length)
            {
                break;
            }
            start = stop + 1 < text.Length && text[stop] == '\r' && text[stop + 1] == '\n' ? stop + 2 : stop + 1;
        }

        int skipped = 0;
        foreach (MergePair pair in merges)
        {
            if (!vocab.ContainsKey(pair.Left) || !vocab.ContainsKey(pair.Right))
            {
                skipped++;
            }
        }

        return new BpeVocabulary(vocab, merges)
        {
            ByteLevel = byteLevel,
            SkippedMerges = skipped,
            PreTokenizerPattern = byteLevel ? BpePatterns.Gpt2 : null,
        };
    }

    private static void ParseMergeLine(string text, int start, int stop, List<MergePair> merges, in ArtifactLimits limits)
    {
        int length = stop - start;
        // A blank line separates nothing, and the leading "#version: 0.2" states
        // the file's format rather than a pair. Both are skipped, as in Python.
        if (length == 0 || text[start] == '#')
        {
            return;
        }
        limits.CheckTokenLength(length);
        limits.CheckArrayLength(merges.Count + 1, SourceName);

        int space = text.IndexOf(' ', start, length);
        if (space < 0)
        {
            throw new InvalidDataException(
                $"The {SourceName} has a line with no separator: '{text.Substring(start, length)}'. Each line is two symbols separated by a space.");
        }
        merges.Add(new MergePair(
            text.Substring(start, space - start),
            text.Substring(space + 1, stop - space - 1)));
    }
}
```

Check the exact names on `JsonArtifact` (`Parse`, `ReadAllBytes`, `ReadAllBytesAsync`, `OpenRead`, `Utf8NoBom`) and on `ArtifactLimits` (`CheckTokenLength`, `CheckVocabularySize`, `CheckArrayLength`) against `src/Shared/Persistence/JsonArtifact.cs` before assuming the signatures above compile; adapt to what is actually there.

- [ ] **Step 5: Run the tests**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~BpeFilesLoaderTests"`
Expected: PASS.

- [ ] **Step 6: Format, build, commit**

```bash
dotnet format DataNet.slnx && dotnet build DataNet.slnx --configuration Release && dotnet test DataNet.slnx --configuration Release --no-build
```

```bash
git add src/DataNet.Embeddings/Persistence/BpeFilesLoader.cs tests/DataNet.Embeddings.Tests/Persistence/BpeFilesLoaderTests.cs
git commit -m "Read the vocab.json and merges.txt pair a GPT-2 checkout ships

The layout that predates tokenizer.json, and still the one most BPE
checkouts carry. Line handling follows VocabTxtLoader: the same terminator
set Python's text mode accepts, and the limits checked on the line before
anything is allocated from it.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 12: `TokenizerJsonLoader.LoadBpe`

**Files:**
- Modify: `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs`
- Modify: `tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs`

**Interfaces:**
- Consumes: `tests/oracles/bpe_tokenizer_json.json`.
- Produces: `LoadBpe(Stream, ArtifactLoadOptions?)`, `LoadBpe(string, ArtifactLoadOptions?)`, `LoadBpeAsync(Stream, ArtifactLoadOptions?, CancellationToken)`.

- [ ] **Step 1: Read the loader's existing shape**

Read `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs` end to end — it is 710 lines and every convention matters: `EnsureModelType`, `EnsureByteFallbackIsOff`, the pre-tokenizer and normalizer validation with `UntypedName`, `RequireObject`/`RequireString`/`OptionalString`/`OptionalBoolean`, the added-token handling, and the message style used when refusing a pipeline. `LoadBpe` is a third sibling of `LoadWordPiece` and `LoadUnigram`, not a new design.

- [ ] **Step 2: Write the failing test**

Append to `tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs`:

```csharp
    private static Stream Bytes(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));

    private static ArtifactLoadOptions BpeBounds() => new()
    {
        MaxTotalBytes = 8L * 1024 * 1024,
        MaxVocabularySize = 100_000,
        MaxArrayLength = 100_000,
        MaxTokenLength = 512,
    };

    [Fact]
    public void LoadBpe_reproduces_every_frozen_pipeline()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_tokenizer_json.json");

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            string text = c.GetProperty("text").GetString()!;
            int[] expected = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();

            BpeVocabulary vocab = TokenizerJsonLoader.LoadBpe(
                Bytes(c.GetProperty("tokenizer_json").GetString()!), BpeBounds());
            int[] actual = [.. new BpeTokenizer(vocab).Encode(text).Ids];

            if (!expected.SequenceEqual(actual))
            {
                failures.Add($"[{name}] exp [{string.Join(", ", expected)}] got [{string.Join(", ", actual)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// byte_fallback is the Llama-2 / Mistral v0.1 pipeline (ADR 0017). Loading it
    /// anyway would produce a tokenization that looks right and embeddings that
    /// are not, so it is refused by name.
    /// </summary>
    [Fact]
    public void LoadBpe_refuses_byte_fallback()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[],"byte_fallback":true}}
        """;
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), BpeBounds()));
        Assert.Contains("byte_fallback", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadBpe_refuses_a_unigram_model()
    {
        const string Json = """{"model":{"type":"Unigram","vocab":[]}}""";
        Assert.Throws<InvalidDataException>(() => TokenizerJsonLoader.LoadBpe(Bytes(Json), BpeBounds()));
    }

    [Fact]
    public void LoadBpe_refuses_a_pre_tokenizer_it_does_not_reproduce()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "pre_tokenizer":{"type":"BertPreTokenizer"}}
        """;
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), BpeBounds()));
        Assert.Contains("BertPreTokenizer", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadBpe_reads_both_merge_encodings()
    {
        const string Pairs = """
        {"model":{"type":"BPE","vocab":{"a":0,"b":1,"ab":2},"merges":[["a","b"]]}}
        """;
        const string Lines = """
        {"model":{"type":"BPE","vocab":{"a":0,"b":1,"ab":2},"merges":["a b"]}}
        """;
        Assert.Equal(
            TokenizerJsonLoader.LoadBpe(Bytes(Pairs), BpeBounds()).Merges,
            TokenizerJsonLoader.LoadBpe(Bytes(Lines), BpeBounds()).Merges);
    }

    [Fact]
    public void LoadBpe_reads_ignore_merges()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[],"ignore_merges":true}}
        """;
        Assert.True(TokenizerJsonLoader.LoadBpe(Bytes(Json), BpeBounds()).IgnoreMerges);
    }
```

Add `using System.Text;` and `using DataNet.Embeddings.Tokenization;` if the file does not already have them.

- [ ] **Step 3: Run it to verify it fails**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~TokenizerJsonLoaderTests"`
Expected: FAIL — `LoadBpe` does not exist.

- [ ] **Step 4: Implement it**

Add the three public overloads directly after `LoadUnigramAsync`, copying the XML-doc shape of their siblings verbatim and substituting "BPE" for "Unigram". Then add the parsing method beside `ParseUnigram`. It must:

1. `EnsureModelType(model, "BPE")`.
2. Refuse `byte_fallback: true`, reusing `EnsureByteFallbackIsOff` — its message already names the reason and applies unchanged.
3. Read `model.vocab` as an object, checking `limits.CheckTokenLength` and `limits.CheckVocabularySize` per entry, exactly as `ParseWordPiece` does.
4. Read `model.merges` as an array, accepting both a two-element array and a space-separated string per element, with `limits.CheckArrayLength` on the array.
5. Read `model.unk_token`, `model.end_of_word_suffix`, `model.continuing_subword_prefix` via `OptionalString`, and `model.ignore_merges` via `OptionalBoolean`.
6. Reuse the existing added-token handling so `AddedTokens` is populated the way `LoadWordPiece` populates its own.
7. Validate the pre-tokenizer into `(bool byteLevel, bool addPrefixSpace, string? pattern)`:
   - `ByteLevel` → `(true, add_prefix_space ?? true, use_regex == false ? null : BpePatterns.Gpt2)`
   - `Sequence` whose elements are a `Split` followed by a `ByteLevel` → `(true, that ByteLevel's add_prefix_space ?? false, the Split's pattern.Regex)`
   - `Whitespace` or absent → `(false, false, null)`
   - anything else → `InvalidDataException` naming the type found, in the style of the existing `EnsurePreTokenizer`.
8. Count merges naming a token absent from the vocabulary into `SkippedMerges`, as `BpeFilesLoader.Parse` does.
9. Check the `decoder` declaration against the model rather than ignoring it: a `ByteLevel` model carrying a `BPEDecoder` (or the reverse) is a file that will not round trip, and `InvalidDataException` at load time beats corrupt text at decode time. An absent `decoder` is fine — it is what `models.BPE` built in code produces, and Task 3's fixtures include that case.

Keep the method under the cognitive-complexity threshold by extracting the pre-tokenizer validation into its own private method. If SonarAnalyzer still flags it, suppress with a comment in the style of the one at the top of `SentencePieceTokenizer.cs` — a faithful reproduction of a published pipeline whose 1:1 shape is what makes divergences auditable — and not otherwise.

- [ ] **Step 5: Run the tests**

Run: `dotnet test tests/DataNet.Embeddings.Tests --filter "FullyQualifiedName~TokenizerJsonLoaderTests"`
Expected: PASS, the pre-existing WordPiece and Unigram tests included.

- [ ] **Step 6: Format, build, run everything, commit**

```bash
dotnet format DataNet.slnx && dotnet build DataNet.slnx --configuration Release && dotnet test DataNet.slnx --configuration Release --no-build
```

```bash
git add src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs
git commit -m "Read a BPE tokenizer.json, and refuse the pipelines we do not run

LoadBpe joins LoadWordPiece and LoadUnigram with the same three overloads
and the same habit: what cannot be reproduced is refused by name rather
than ignored. byte_fallback is the Llama-2 and Mistral v0.1 pipeline, and
loading it anyway would give a caller plausible tokens and wrong
embeddings.

ignore_merges is read rather than refused, because Llama-3 declares it.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 13: The benchmark

**Files:**
- Create: `bench/DataNet.Text.Benchmarks/BpeBenchmarks.cs`
- Modify: `bench/corpus/generate_vocabs.py`, `bench/DataNet.Text.Benchmarks/BenchCorpus.cs`

**Interfaces:**
- Consumes: `BpeTokenizer`, `BpeFilesLoader`, `SentencePieceTokenizer`.
- Produces: measured figures for the PR description. No API.

- [ ] **Step 1: Add a 30k BPE tokenizer to the benchmark corpus**

Read `bench/corpus/generate_vocabs.py`. It already trains a 30 000-entry WordPiece, Unigram and SentencePiece over the same documents. Add a byte-level BPE beside them, written to `tokenizer_30k_bpe.json`, trained on the same corpus with the same vocabulary size — the comparison is only meaningful if both tokenizers saw the same text.

Then add `"tokenizer_30k_bpe.json"` to `BenchCorpus.RequiredFiles` in `bench/DataNet.Text.Benchmarks/BenchCorpus.cs`, so a run without it fails rather than silently skipping.

- [ ] **Step 2: Generate the corpus**

Run: `cd /tmp && PYTHONSAFEPATH=1 /home/cyril/Documents/devs/data.net/.venv-oracles/bin/python /home/cyril/Documents/devs/data.net/bench/corpus/generate_vocabs.py`
Expected: `tokenizer_30k_bpe.json` written alongside the existing files.

- [ ] **Step 3: Write the benchmark**

```csharp
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;

namespace DataNet.Text.Benchmarks;

/// <summary>
/// The #59 acceptance bar: byte-level BPE at a cost comparable to the unigram
/// tokenizer already shipped, on the same documents and the same vocabulary size.
/// </summary>
/// <remarks>
/// Both tokenizers are built once in <see cref="Setup"/>, so the numbers are
/// encoding cost rather than model loading. The corpus is the shared 30 000-entry
/// one, so a difference is the algorithm rather than the vocabulary.
/// </remarks>
[MemoryDiagnoser]
public class BpeBenchmarks
{
    private BpeTokenizer _bpe = null!;
    private SentencePieceTokenizer _unigram = null!;
    private string[] _documents = [];

    [GlobalSetup]
    public void Setup()
    {
        var bounds = new ArtifactLoadOptions
        {
            MaxTotalBytes = 32L * 1024 * 1024,
            MaxVocabularySize = 300_000,
            MaxArrayLength = 300_000,
        };
        _bpe = new BpeTokenizer(TokenizerJsonLoader.LoadBpe(BenchCorpus.Path("tokenizer_30k_bpe.json"), bounds));
        _unigram = new SentencePieceTokenizer(TokenizerJsonLoader.LoadUnigram(BenchCorpus.Path("tokenizer_30k_unigram.json"), bounds));
        _documents = JsonSerializer.Deserialize<string[]>(File.ReadAllBytes(BenchCorpus.Path("documents.json")))!;
    }

    [Benchmark(Baseline = true)]
    public int Unigram()
    {
        int total = 0;
        foreach (string document in _documents)
        {
            total += _unigram.Encode(document).Ids.Count;
        }
        return total;
    }

    [Benchmark]
    public int Bpe()
    {
        int total = 0;
        foreach (string document in _documents)
        {
            total += _bpe.Encode(document).Ids.Count;
        }
        return total;
    }

    /// <summary>A single long token with no split point, which is where a linear merge scan would hurt.</summary>
    [Benchmark]
    public int BpeOnOnePathologicalToken() => _bpe.Encode(new string('a', 2048)).Ids.Count;
}
```

- [ ] **Step 4: Run it**

Run: `dotnet run --project bench/DataNet.Text.Benchmarks --configuration Release -- --filter "*BpeBenchmarks*"`
Expected: a table with `Unigram`, `Bpe` and `BpeOnOnePathologicalToken`, with `Allocated` per operation.

- [ ] **Step 5: Read the numbers and act on them**

Record the machine (`lscpu | head -20`, the .NET SDK version) and the table. Then:

- **`Bpe` within roughly 2× of `Unigram`** → the acceptance bar is met. Continue.
- **`Bpe` far slower, and `Allocated` is large** → the per-byte `ToString()` in `ByteLevelSymbols` is the cause. Fix it with a `FrozenDictionary` built in the constructor plus `GetAlternateLookup<ReadOnlySpan<char>>()` over a one-character span of a reused buffer, under `#if NET9_0_OR_GREATER`, with the `netstandard2.0` path unchanged. Follow `src/DataNet.Text/Vectorization/StopWordSet.cs`. Re-run and record both figures.
- **`BpeOnOnePathologicalToken` dominates everything** → the linear scan in `Merge` is the cause, and the priority-queue arm the spec holds in reserve is now justified. Report the number to the user before implementing it; the spec says the heap is added only on evidence, and this is the evidence.

Do not skip this step and do not report figures you did not run.

- [ ] **Step 6: Commit**

```bash
git add bench/DataNet.Text.Benchmarks/BpeBenchmarks.cs bench/DataNet.Text.Benchmarks/BenchCorpus.cs bench/corpus/generate_vocabs.py
git commit -m "Measure the BPE tokenizer against the one it has to keep up with

Both tokenizers see the same documents at the same vocabulary size, so a
difference is the algorithm rather than the corpus. The third case is a
single 2048-character token with no split point -- the shape where a
linear merge scan would lose to a heap, and the evidence the spec asks for
before one is written.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 14: The sample and the packaging gate

**Files:**
- Modify: `samples/DataNet.Sample/Lot3Embeddings.cs`

**Interfaces:**
- Consumes: all five new public types.
- Produces: nothing. This task exists so CI passes.

- [ ] **Step 1: Understand what the gate checks**

Read `samples/DataNet.Sample/PackagingGate.cs`. It reads the exported types from the **packaged** assemblies NuGet resolved, and matches them against the `MemberReference` entries in the sample's own metadata. `typeof(T)` emits a `TypeReference` and does not count. A `const` never emits a member reference at all — which is why `BpePatterns` exposes properties.

- [ ] **Step 2: Add the section**

In `Lot3Embeddings.Run()`, after the SentencePiece section and before the batch-encoding one:

```csharp
        // BPE, the decoder-model side of the library. Byte-level is the variant
        // GPT-2, Llama-3 and Qwen2 use, and the one that round-trips exactly.
        var bpeVocab = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Ġ"] = 0, ["t"] = 1, ["o"] = 2, ["k"] = 3, ["e"] = 4, ["n"] = 5,
            ["to"] = 6, ["ken"] = 7, ["token"] = 8, ["Ġtoken"] = 9,
        };
        var bpeMerges = new List<MergePair> { new("t", "o"), new("k", "e"), new("ke", "n") };
        var bpeModel = new BpeVocabulary(bpeVocab, bpeMerges)
        {
            ByteLevel = true,
            PreTokenizerPattern = BpePatterns.Gpt2,
        };
        var bpe = new BpeTokenizer(bpeModel);
        TokenizationResult bpeEncoded = bpe.Encode("token");
        Console.WriteLine($"  BPE byte-level   : [{string.Join(", ", bpeEncoded.Tokens)}] -> [{string.Join(", ", bpeEncoded.Ids)}]");
        Console.WriteLine($"  BPE round trip   : \"{bpe.Decode(bpeEncoded.Ids)}\"");
        Console.WriteLine($"  merge rank 0     : {bpeModel.Merges[0].Left} + {bpeModel.Merges[0].Right}");
        Console.WriteLine($"  merges skipped   : {bpeModel.SkippedMerges}");

        // The same model as a consumer gets it: vocab.json + merges.txt.
        BpeVocabulary fromFiles = BpeFilesLoader.Load(
            Utf8("""{"Ġ":0,"t":1,"o":2,"k":3,"e":4,"n":5,"to":6,"ken":7,"token":8,"Ġtoken":9}"""),
            Utf8("#version: 0.2\nt o\nk e\nke n\n"));
        Console.WriteLine($"  BPE from files   : {fromFiles.Count} tokens, {fromFiles.Merges.Count} merges");
```

`Utf8(...)` is the helper the file already has for the other loaders.

`bpeModel.Merges[0].Left` and `.Right` are what give `MergePair` its member references; `BpePatterns.Gpt2`, `BpeVocabulary.SkippedMerges`, `BpeTokenizer.Decode` and `BpeFilesLoader.Load` cover the rest.

- [ ] **Step 3: Run the sample**

Run: `dotnet run --project samples/DataNet.Sample --configuration Release`
Expected: the new lines appear, the round trip prints `"token"`, and the gate does not fail.

If the gate reports an unreferenced type, it names it. Add a member reference for that type — reading a property is enough, `typeof` is not.

- [ ] **Step 4: Format, build, commit**

```bash
dotnet format DataNet.slnx && dotnet build DataNet.slnx --configuration Release
```

```bash
git add samples/DataNet.Sample/Lot3Embeddings.cs
git commit -m "Exercise the five new public types from the sample

ADR 0009 gives the sample one job: prove every exported type is reachable
from outside its assembly. A type the sample never mentions passes by
construction, so the gate only means something once these are named --
with a member reference each, since typeof does not count.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 15: Documentation

**Files:**
- Create: `docs/decisions/0017-bpe-parity-scope.md`
- Modify: `docs/equivalence.md`, `docs/guides/embeddings.md`, `README.md`, `CHANGELOG.md`

**Interfaces:**
- Consumes: the measured figures from Task 13.
- Produces: nothing.

- [ ] **Step 1: Write ADR 0017**

Read `docs/decisions/0013-sentencepiece-parity-scope.md` first and follow its structure exactly (Status / Context / Decision / Consequences). The three limits to record:

1. Parity is claimed end-to-end for GPT-2 and for the classic character-level lineage, proven over GPT-2's real vocabulary.
2. Llama-3 and Qwen2 are claimed **at the split level only** — the pattern is proven against HuggingFace, the vocabulary is the caller's. Say why: proving them end-to-end means vendoring 150 000-entry vocabularies to re-prove a merge loop GPT-2 already proves.
3. `byte_fallback` is refused. Llama-2 and Mistral v0.1 are SentencePiece BPE with `Metaspace`, a third pipeline that neither `BpeTokenizer` nor `SentencePieceTokenizer` reproduces. Name them, so a reader stops looking.
4. Llama-3's split pattern was read from two independent ungated mirrors — `NousResearch/Meta-Llama-3-8B` and `unsloth/llama-3-8b` — because `meta-llama/Meta-Llama-3-8B` is gated and returns HTTP 401. Their `pre_tokenizer` blocks are byte-identical, and that agreement is what stands in for reading the original. Record it: a reader auditing where a public constant came from must not have to reconstruct this.

Record the measured benchmark outcome in *Consequences*, including whether the priority-queue arm was needed.

- [ ] **Step 2: Add the equivalence rows**

Read the existing tokenizer rows in `docs/equivalence.md` — they name the Python call, the library, the C# call, and what is and is not reproduced. Add one row each for:

- `Tokenizer(BPE(vocab, merges)).encode(t)` with a `ByteLevel` pre-tokenizer → `new BpeTokenizer(vocab).Encode(t)`
- `tokenizer.decode(ids)` → `BpeTokenizer.Decode(ids)`, noting the inverted `skip_special_tokens` default
- `models.BPE.from_file(vocab, merges)` → `BpeFilesLoader.Load(...)`
- `Tokenizer.from_file("tokenizer.json")` (BPE) → `TokenizerJsonLoader.LoadBpe(path)`, naming what is refused

State the corpus each row is proven over, as the SentencePiece rows do.

- [ ] **Step 3: Write the guide section**

In `docs/guides/embeddings.md`, add **"Which tokenizer for which model family"** — the question a user actually arrives with. A table mapping family to class:

| Family | Class | How to load |
| --- | --- | --- |
| BERT, DistilBERT, and the WordPiece family | `WordPieceTokenizer` | `VocabTxtLoader` or `TokenizerJsonLoader.LoadWordPiece` |
| T5, ALBERT, camemBERT, XLM-R | `SentencePieceTokenizer` | `SentencePieceModelLoader` or `TokenizerJsonLoader.LoadUnigram` |
| GPT-2 and its byte-level descendants | `BpeTokenizer` | `BpeFilesLoader` or `TokenizerJsonLoader.LoadBpe` |
| Llama-3, Qwen2 | `BpeTokenizer` with `BpePatterns.Llama3` / `BpePatterns.Qwen2` | `TokenizerJsonLoader.LoadBpe` |
| **Llama-2, Mistral v0.1** | **none** | — |

Then a short paragraph on the last row: those models are SentencePiece BPE with `byte_fallback`, which is a third pipeline, and both loaders refuse it by name rather than producing a plausible-looking wrong answer. Point at ADR 0017.

Include a compiling code sample — `tools/extract_doc_snippets.py` compiles the snippets in this file in CI, so a sample that does not build fails the run.

- [ ] **Step 4: Update `README.md` and `CHANGELOG.md`**

`README.md`: the Lot 3 section claims tokenizer coverage; add BPE and byte-level BPE, and the model families they cover.

`CHANGELOG.md`: under `## [Unreleased]`, in a `### DataNet.Embeddings — 0.3.0` group (create it if the release does not have one yet), an `#### Added` entry in the style of the existing ones — what landed, which model families it covers, what it refuses and why, and the corpus proving it.

- [ ] **Step 5: Verify the documentation builds**

Run: `python3 tools/extract_doc_snippets.py && dotnet build samples/DataNet.DocSnippets --configuration Release`
Expected: the extracted snippets compile.

- [ ] **Step 6: Commit**

```bash
git add docs/decisions/0017-bpe-parity-scope.md docs/equivalence.md docs/guides/embeddings.md README.md CHANGELOG.md
git commit -m "Say which tokenizer a model family needs, and which have none

The question a user arrives with is not \"how does BPE work\" but \"what do
I use for Llama\". The guide answers it in a table, including the row that
says none of the three: Llama-2 and Mistral v0.1 are SentencePiece BPE
with byte_fallback, a pipeline this package refuses by name.

ADR 0017 records the three limits so they are stated once rather than
rediscovered, in the shape ADR 0013 used for SentencePiece.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 16: Final verification and the pull request

**Files:** none.

- [ ] **Step 1: Run everything CI runs**

```bash
dotnet format DataNet.slnx --verify-no-changes && dotnet build DataNet.slnx --configuration Release && dotnet test DataNet.slnx --configuration Release --no-build && dotnet run --project samples/DataNet.Sample --configuration Release && python3 tools/fetch_gpt2_bpe.py --check
```

Expected: every one passes. Report the actual output; do not summarise a run you did not make.

- [ ] **Step 2: Confirm the oracles do not drift**

Run: `cd /tmp && PYTHONSAFEPATH=1 /home/cyril/Documents/devs/data.net/.venv-oracles/bin/python /home/cyril/Documents/devs/data.net/tools/generate_oracles.py; echo "exit=$?"` then `git diff --stat -- tests/oracles`
Expected: `exit=0` and an empty diff.

- [ ] **Step 3: Confirm the netstandard suite really ran the new tests**

```bash
dotnet test tests/DataNet.Embeddings.NetStandard.Tests --configuration Release --no-build --filter "FullyQualifiedName~Bpe|FullyQualifiedName~ByteLevel" -v n | tail -30
```

Expected: a non-zero passed count. A green suite that ran **zero** BPE tests is the failure mode this step exists to catch — check the number, not the colour.

- [ ] **Step 4: Push and open the PR**

```bash
git push -u origin feat/59-bpe-tokenizers
```

Open the PR against `main`, closing #59. The body must carry:

- What landed and what it covers.
- The parity table from the spec — including the Llama-2 / Mistral row.
- The benchmark figures from Task 13, with the machine named, and whether the priority-queue arm was needed.
- A note that the GPT-2 vocabulary is vendored under MIT, weights excluded, verified in CI.

End with:

```
🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Do **not** merge. The repository owner merges.

- [ ] **Step 5: Wait for SonarCloud**

Findings arrive after the push, not before — SonarAnalyzer in the local build catches many but not all of them. Read the PR's SonarCloud report and fix what it raises, passing `resolved=false` when querying the API or the count will never drop.
