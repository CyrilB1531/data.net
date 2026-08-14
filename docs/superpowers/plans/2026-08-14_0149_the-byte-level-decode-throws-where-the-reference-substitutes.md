# #149 — A lossy byte-level decode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Goal:** Make `BpeTokenizer.Decode` substitute U+FFFD where the reference substitutes, instead of
throwing, so that decoding a token at a time works for text outside Latin-1.

**Architecture:** One call site changes — the `GetString` at the end of `Finish`'s byte-level branch — from
the shared throwing UTF-8 to a decode-only encoding with the replacement fallback. Encoding stays strict.
The tests that pinned the divergence become parity assertions, and a new corpus covers the per-id decoding
the divergence made impossible.

**Tech Stack:** C# (`net10.0` + `netstandard2.0`), xunit, `tokenizers` 0.23.1 through
`tools/generate_oracles.py`.

**Spec:** `docs/superpowers/specs/2026-08-14_0149_the-byte-level-decode-throws-where-the-reference-substitutes.md`

## Global Constraints

- Branch `fix/149-lossy-bytelevel-decode`, **stacked on `feat/121-bpe-normalizer`** at `28c5232`. It cannot
  merge before #121. Do not push, do not open a pull request.
- **No inline comment exceeds two lines, and none uses the `long-comment:` escape.** #134 is landing
  `tools/check_comment_length.py` with budgets of two lines inline and eight lines of prose in XML
  documentation. What does not fit goes in the member's documentation, the spec, or the ADR.
- **Every `dotnet` invocation goes through `./.dotnet-guarded`**, never bare `dotnet` — another session
  benchmarks on this machine. It blocks with no deadline; let it wait.
- `dotnet build` gives **no analyzer diagnostics without `--no-incremental`**. Warnings are errors, plus
  nine extra `csharpsquid` rules.
- No `#if`: the same source serves `net10.0` and `netstandard2.0`.
- Each test file is linked into the mirrored `*.NetStandard.Tests` project, so every test counts **twice**.
  Baseline on this branch: **3 097 passing, 0 failed** across eight assemblies.
- `dotnet format DataNet.slnx --verify-no-changes` runs **once**, in the final task.
- Read pass/fail **counts**, never the exit code alone.
- Run the oracle generator from a neutral directory (`cd /tmp`) with `PYTHONSAFEPATH=1`, and read **its
  own** exit code, never a pipeline's.
- English everywhere. Commit messages carry no `feat:`/`fix:` prefix and no process prefix.
- **Every commit must be green.** A failing test is never committed, not even as a red step.

## What is already measured, and must not be re-derived

| Fact | Value |
| --- | --- |
| Tokens of `東京 👋` that decode alone to U+FFFD in `tokenizers` | 6 of 6 |
| Same, `日本語のテキスト` / `🇫🇷 emoji` / `déjà vu` | 6 of 10 / 6 of 7 / 0 of 6 |
| Models among the fifteen surveyed with non-ASCII added tokens | 1 — deepseek-coder, 18 of 22, refused today for its pre-tokenizer |
| Cases in `bpe_normalizer.json` that currently prove the divergence | 3, all containing `café` |

## File Structure

| File | Responsibility |
| --- | --- |
| `tools/generate_oracles.py` | `generate_bytelevel_decode_stream`: each id of a text decoded on its own. |
| `tests/oracles/bytelevel_decode_stream.json` | Generated; never hand-edited. |
| `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs` | The decode-only encoding, the one call site, and the documentation that stops promising a throw. |
| `tests/DataNet.Embeddings.Tests/Tokenization/ByteLevelDecodeTests.cs` | The per-id replay. |
| `tests/DataNet.Embeddings.Tests/Tokenization/BpeNormalizerTests.cs` | The two decode tests: one loses its exclusion, the other is deleted. |
| `docs/decisions/0023-byte-level-decode-substitutes.md`, `docs/equivalence.md`, `docs/guides/embeddings.md` | The contract change. |

---

### Task 1: The corpus the divergence made impossible

**Files:**

- Modify: `tools/generate_oracles.py`
- Create (generated): `tests/oracles/bytelevel_decode_stream.json`

**Depends on:** nothing.

**Produces:** `cases[]` of `{id, text, ids[], tokens[], per_id_decoded[], replacement_count, decoded}`.
`per_id_decoded[i]` is what the reference returns for `ids[i]` **alone**. Task 2 replays it.

- [ ] **Step 1: Add the generator**

Beside `generate_bpe_normalizer` in `tools/generate_oracles.py`, using `_gpt2_tokenizer()` (which already
sets the `ByteLevel` decoder):

```python
# Two CJK texts, an emoji sequence and two controls. A byte-level token is a
# fragment of a multi-byte character far more often than not, which is what makes
# per-token decoding the case this corpus exists for.
BYTELEVEL_STREAM_TEXTS = [
    "東京 \U0001f44b",          # 東京 + waving hand
    "日本語のテキスト",  # a Japanese sentence
    "\U0001f1eb\U0001f1f7 emoji",       # a regional-indicator pair
    "déjà vu",                # Latin-1: no fragment, the control
    "hello world",                      # ASCII: the other control
]


def generate_bytelevel_decode_stream() -> dict:
    """Each id of a text decoded on its own, which is how a stream is consumed.

    tokenizers substitutes U+FFFD for a byte sequence that is not well-formed
    UTF-8; DataNet threw until issue #149. The `replacement_count` per case is
    carried so a corpus that stopped exercising the substitution would be noticed
    rather than pass silently.
    """
    tokenizer = _gpt2_tokenizer()

    cases = []
    for text in BYTELEVEL_STREAM_TEXTS:
        enc = tokenizer.encode(text)
        per_id = [tokenizer.decode([i]) for i in enc.ids]
        cases.append({
            "id": len(cases),
            "text": text,
            "ids": enc.ids,
            "tokens": enc.tokens,
            "per_id_decoded": per_id,
            "replacement_count": sum(1 for s in per_id if "�" in s),
            "decoded": tokenizer.decode(enc.ids),
        })
    return {
        "metadata": {
            "algorithm": "byte-level decode, one id at a time",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "count": len(cases),
        },
        "cases": cases,
    }
```

Register it in the generator table beside `"bpe_normalizer.json"`:

```python
        "bytelevel_decode_stream.json": generate_bytelevel_decode_stream,
```

- [ ] **Step 2: Generate, and read the generator's own exit code**

```bash
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py > /tmp/149-gen.log 2>&1
echo "generate=$?"
cd <repo> && git status --porcelain tests/oracles/
```

Expected: exit 0, and exactly one new file. **Any existing corpus moving is a stop condition.**

- [ ] **Step 3: Check the corpus is not vacuous**

```bash
cd <repo>
python3 - <<'EOF'
import json
d = json.load(open("tests/oracles/bytelevel_decode_stream.json"))
for c in d["cases"]:
    print(f"{c['text']!r}: {len(c['ids'])} ids, {c['replacement_count']} decode alone to U+FFFD")
EOF
```

Expected, from the measurement this lot is built on: 6 of 6 for the first text, 6 of 10 for the Japanese
sentence, 6 of 7 for the emoji pair, and **0** for both controls. If the controls are not zero, the corpus
is not measuring what it claims and the texts are wrong.

- [ ] **Step 4: Commit**

```bash
git add tools/generate_oracles.py tests/oracles/bytelevel_decode_stream.json
git commit -m "Freeze what the reference returns for one id at a time"
```

---

### Task 2: The fix, and the tests that stop pinning a divergence

**Files:**

- Modify: `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs`
- Create: `tests/DataNet.Embeddings.Tests/Tokenization/ByteLevelDecodeTests.cs`
- Modify: `tests/DataNet.Embeddings.Tests/Tokenization/BpeNormalizerTests.cs`

**Depends on:** Task 1.

**Interfaces:** consumes `bytelevel_decode_stream.json`. Produces no new public API — `Decode`'s signature
is unchanged; only what it does with malformed bytes changes.

- [ ] **Step 1: The decode-only encoding**

In `BpeTokenizer.cs`, beside the other private static fields:

```csharp
    /// <summary>UTF-8 for decoding only: a byte sequence that is not well-formed becomes U+FFFD.</summary>
    /// <remarks>
    /// Not <see cref="JsonArtifact.Utf8NoBom"/>, which throws and is shared with the
    /// persistence layer and with <see cref="Encode"/>'s own byte conversion, where
    /// refusing is right. The asymmetry is deliberate and matches the reference:
    /// strict on the way in, forgiving on the way out. See decision 0023.
    /// </remarks>
    private static readonly UTF8Encoding Utf8Lossy = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
```

- [ ] **Step 2: Change the one call site**

At the end of `Finish`'s byte-level branch, `return JsonArtifact.Utf8NoBom.GetString(bytes, 0, n);` becomes:

```csharp
        return Utf8Lossy.GetString(bytes, 0, n);
```

Nothing else in `Finish` changes, and no other use of `JsonArtifact.Utf8NoBom` in the file is touched —
`GetByteCount` and the two `GetBytes` calls are the encode path and must keep throwing.

- [ ] **Step 3: Correct `Decode`'s documentation**

Two things in the XML documentation of both `Decode` overloads are now false and must go:

- the `<exception cref="DecoderFallbackException">` entries;
- the `<remarks>` paragraph beginning "For a byte-level model, `ids` assembled by hand rather than produced
  by `Encode` can concatenate byte symbols into a sequence that is not valid UTF-8" — which was wrong twice
  over, since ids from `Encode` reach it too and it no longer throws.

Replace that paragraph with two lines of prose saying what happens now and how to detect it: a byte
sequence that is not well-formed UTF-8 becomes U+FFFD, as in the reference, which is what makes decoding
one id at a time possible; a caller who needs to know can test the result for U+FFFD. Keep it inside the
eight-line documentation budget.

- [ ] **Step 4: The per-id replay**

Create `tests/DataNet.Embeddings.Tests/Tokenization/ByteLevelDecodeTests.cs`. Read
`tests/DataNet.Embeddings.Tests/Tokenization/BpeNormalizerTests.cs` first and match its conventions —
namespace, usings, how it reaches `OracleLoader` and `OracleReplay`, how it builds a GPT-2 tokenizer from
the vendored fixtures, and its failure-accumulating style.

```csharp
    /// <summary>
    /// Each id decoded on its own, which is how a caller consumes a stream. Every
    /// token of a CJK or emoji text is a fragment of a multi-byte character, so this
    /// is the case that threw before issue #149 rather than an exotic one.
    /// </summary>
    [Fact]
    public void Decode_of_one_id_at_a_time_matches_the_reference()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_decode_stream.json");
        BpeTokenizer tokenizer = /* the vendored GPT-2 byte-level tokenizer, as the neighbouring tests build it */;

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            int[] ids = [.. c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32())];
            string[] expected = [.. c.GetProperty("per_id_decoded").EnumerateArray().Select(e => e.GetString()!)];

            for (int i = 0; i < ids.Length; i++)
            {
                string actual = tokenizer.Decode([ids[i]]);
                if (!string.Equals(expected[i], actual, StringComparison.Ordinal))
                {
                    failures.Add($"id {ids[i]}: expected {Escape(expected[i])}, got {Escape(actual)}");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The whole stream still decodes exactly. A complete, valid byte sequence never
    /// reaches the fallback, so the round trip this package promises is untouched by
    /// the substitution the test above measures.
    /// </summary>
    [Fact]
    public void Decode_of_the_whole_stream_is_unchanged_by_the_fallback()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_decode_stream.json");
        BpeTokenizer tokenizer = /* the same tokenizer */;

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            int[] ids = [.. c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32())];
            string expected = c.GetProperty("decoded").GetString()!;
            string actual = tokenizer.Decode(ids);

            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                failures.Add($"{Escape(expected)} != {Escape(actual)}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
        Assert.DoesNotContain('�', string.Join("", doc.RootElement.GetProperty("cases")
            .EnumerateArray().Select(c => c.GetProperty("decoded").GetString()!)));
    }
```

`Escape` renders a string as its code points so a failure names what differs; `BpeNormalizerTests` already
has one — copy it into this file rather than making the two files share a helper, unless the test project
already has a shared place for such helpers, in which case use that.

The second test's final assertion is the guard that the corpus's whole-stream expectations are themselves
clean: if a `decoded` field ever contains U+FFFD, the round-trip claim in its summary would be measuring
nothing.

- [ ] **Step 5: Invert the two tests in `BpeNormalizerTests.cs`**

- `Decode_returns_what_the_reference_returns_normalizer_included` currently skips any case whose `decoded`
  contains U+FFFD. Delete the `continue` and the `<remarks>` that explains it: those three cases are now
  ordinary parity assertions.
- `Decode_throws_where_the_reference_is_lossy` is **deleted entirely**, with its documentation. It existed
  to pin a divergence that no longer exists.

- [ ] **Step 6: Build and run the whole suite**

```bash
cd <repo>
./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental > /tmp/149-t2-b.log 2>&1; echo "build=$?"; grep -E "Avertissement\(s\)|Erreur\(s\)" /tmp/149-t2-b.log
./.dotnet-guarded dotnet test DataNet.slnx -c Release > /tmp/149-t2-t.log 2>&1; echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/149-t2-t.log
git status --porcelain tests/oracles/
```

Expected: 0 warnings, `tests/oracles/` unchanged, and a total of **3 097 − 2 + 4 = 3 099** passing (the
deleted test counted twice, the two new ones count twice each). State the real per-assembly numbers.

Every other decode oracle in the repository must pass **untouched**: a complete, valid byte sequence never
reaches the fallback, so a moved expectation there would mean the change reached further than one call
site.

- [ ] **Step 7: Commit**

```bash
git add src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs \
        tests/DataNet.Embeddings.Tests/Tokenization/ByteLevelDecodeTests.cs \
        tests/DataNet.Embeddings.Tests/Tokenization/BpeNormalizerTests.cs
git commit -m "Substitute where the reference substitutes, and keep encoding strict"
```

---

### Task 3: The decision record and the documentation

**Files:**

- Create: `docs/decisions/0023-byte-level-decode-substitutes.md`
- Modify: `docs/equivalence.md`, `docs/guides/embeddings.md`

**Depends on:** Task 2.

- [ ] **Step 1: Write the ADR**

`0023` is the next number — `0022-added-token-matching-flags.md` is the highest. Follow the shape of
`docs/decisions/0017-bpe-parity-scope.md`: numbered sections, each a decision with its evidence. It must
carry, in this order:

- **What changed**: `Decode` substituted U+FFFD instead of throwing `DecoderFallbackException`, at one call
  site.
- **The measurement that forced it**: 6 of 6 tokens of `東京 👋`, 6 of 10 of the Japanese sentence, 6 of 7
  of the emoji pair, 0 of 6 for `déjà vu` — so decoding a token at a time was impossible for any text
  outside Latin-1, and that is the normal way to consume a language model.
- **Why encoding stays strict**: a lone surrogate is not well-formed UTF-16, so there is no byte sequence
  for it to be lossless about — the asymmetry is the reference's own.
- **What a caller loses**: an exception that announced a truncated or hand-built id list. They now get a
  string containing U+FFFD, which is detectable but silent. Say this plainly; it is the cost.
- **What is unchanged**: a complete, valid byte sequence never reaches the fallback, so the byte-level
  round trip holds exactly where it held before.
- **What was rejected**: refusing such a model at load, which addresses only the added-token shape and
  leaves the streaming one; and an opt-in parameter, which doubles a public contract for a choice the
  reference does not offer.

- [ ] **Step 2: `docs/equivalence.md`**

Two rows are affected, and both were made false or incomplete by earlier work:

- the `decode` row promises "every UTF-8 byte round-trips exactly, including malformed-looking sequences
  that came from `Encode`" — it now needs to say what happens to a sequence that is not well-formed, and
  that per-id decoding matches the reference;
- the `LoadBpe` row carries a clause added by #121 naming this issue as an open divergence. It is closed.

- [ ] **Step 3: `docs/guides/embeddings.md`**

The note #121 added says a non-ASCII added token makes `Decode` throw. Replace it with what happens now,
in one or two sentences, and mention that decoding a token at a time works — that is the user-visible gain
and the reason the guide should mention it at all.

- [ ] **Step 4: Lint and commit**

```bash
cd <repo>
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" \
  "tools/README.md" "bench/README.md" > /tmp/149-t3-md.log 2>&1; echo "markdownlint=$?"
python3 tools/extract_doc_snippets.py > /tmp/149-t3-snip.log 2>&1; echo "snippets=$?"; tail -2 /tmp/149-t3-snip.log
git add docs
git commit -m "Record why the byte-level decode stopped throwing"
```

---

### Task 4: Final verification

**Depends on:** Tasks 1-3. Nothing is committed unless a gate fails and is fixed.

- [ ] **Step 1: Every gate**

```bash
cd <repo>
git status --porcelain                                                                     # empty
./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental > /tmp/149-fv-b.log 2>&1; echo "build=$?"; grep -E "Avertissement\(s\)|Erreur\(s\)" /tmp/149-fv-b.log
./.dotnet-guarded dotnet format DataNet.slnx --verify-no-changes > /tmp/149-fv-f.log 2>&1;  echo "format=$?"
./.dotnet-guarded dotnet test DataNet.slnx -c Release > /tmp/149-fv-t.log 2>&1;             echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/149-fv-t.log
python3 tools/check_version_floor.py > /tmp/149-fv-v.log 2>&1;                              echo "floor=$?"
python3 tools/check_machine_paths.py > /tmp/149-fv-p.log 2>&1;                              echo "paths=$?"
.venv-oracles/bin/python -m pytest tools/tests -q > /tmp/149-fv-py.log 2>&1;                echo "pytest=$?"; tail -2 /tmp/149-fv-py.log
```

- [ ] **Step 2: The packaging gate, end to end**

```bash
cd <repo>
rm -rf ./artifacts
for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do
  ./.dotnet-guarded dotnet pack "$p" -c Release -o ./artifacts > /tmp/149-pack.log 2>&1 || echo "PACK FAILED $p"
done
NUGET_PACKAGES=/tmp/149-nuget ./.dotnet-guarded dotnet build samples/DataNet.Sample -c Release > /tmp/149-fv-sample.log 2>&1; echo "sample=$?"
NUGET_PACKAGES=/tmp/149-nuget ./.dotnet-guarded dotnet build samples/DataNet.DocSnippets -c Release > /tmp/149-fv-snip.log 2>&1; echo "snippets-build=$?"
```

This lot adds no public type, so the gate is a regression check rather than a new obligation — but the
guide changed, and the snippet project compiles what the guide contains.

- [ ] **Step 3: The oracle drift gate**

```bash
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py > /tmp/149-fv-gen.log 2>&1
echo "generate=$?"
cd <repo> && git status --porcelain tests/oracles/
```

Expected: empty.

- [ ] **Step 4: Stop and report**

Do not push, do not open a pull request. Report the per-assembly counts, whether any existing corpus moved,
and confirm no inline comment added by this lot runs past two lines.

---

## Self-Review

**Spec coverage.** D1 → Task 2 Steps 1-2. D2 → the ADR's "what a caller loses" bullet, Task 3 Step 1.
D3 → Task 1 for the streaming corpus and Task 2 Steps 4-5 for the inverted tests. D4 → Task 3. D5 → the
Global Constraints, which carry both the stacking and the comment budgets.

**Placeholders.** Two code blocks in Task 2 Step 4 mark where the GPT-2 tokenizer is constructed with a
comment rather than a literal, because the neighbouring test file already has that construction and copying
a guess would be worse than reading it; the step says so explicitly. `<repo>` stands for a path that must
not be written into a committed file.

**Type consistency.** `Utf8Lossy` is defined in Task 2 Step 1 and used in Step 2. `Escape` is described
once, in Task 2 Step 4, with its provenance. The corpus field names — `per_id_decoded`, `replacement_count`,
`decoded`, `ids` — are defined in Task 1 and used under those names in Task 2.
