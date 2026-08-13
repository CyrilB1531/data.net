# #121 — The BPE normalizer, in the shape its siblings already use Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Goal:** Load the four Unicode normalization forms and a `Sequence` of them instead of refusing every
normalizer, so the five measured BPE files — Qwen2, GPT-NeoX, Pythia, OLMo, deepseek — stop being refused
over an `NFC`.

**Architecture:** `LoadBpe` reads the normalizer into an ordered list of `NormalizationForm` carried on
`BpeVocabulary`, refusing every other type by name. `BpeTokenizer.Encode` takes `WordPieceTokenizer`'s
two-scanner shape — raw entries against raw text, normalized entries against normalized text — but
normalizes **each gap in isolation** rather than sharing indices between the raw and normalized strings,
because every form here changes length.

**Tech Stack:** C# (`net10.0` + `netstandard2.0`), xunit, `tokenizers` 0.23.1 through
`tools/generate_oracles.py`.

**Spec:** `docs/superpowers/specs/2026-08-13_0121_give-readbpe-the-normalizer-treatment.md`

## Global Constraints

- Everything in English — code, comments, commit messages, PR body. Commit messages carry no
  `feat:`/`fix:` prefix and no process prefix such as `Fix round 1:`.
- Branch `feat/121-bpe-normalizer`, in the main checkout, based on `main` at `6b0b316`. Never commit to
  `main`. Do not push or open a pull request without asking.
- **Every `dotnet` invocation goes through the lock guard at the repository root**:
  `./.dotnet-guarded dotnet test …`, never bare `dotnet`. Another session benchmarks on this machine and
  the guard makes you wait instead of corrupting its numbers. It blocks with no deadline; if it prints
  "waiting", let it wait.
- **`dotnet build` is incremental: without `--no-incremental` no analyzer diagnostic is produced at all.**
  Warnings are errors repository-wide, plus nine extra `csharpsquid` rules.
- `src/` multi-targets `netstandard2.0` and `net10.0`. `String.Normalize(NormalizationForm)` exists on
  both, so **no `#if`** anywhere in this lot.
- Every test file is linked into the mirrored `*.NetStandard.Tests` project, so each new test counts
  **twice**. Baseline on this branch: **3 061 passing, 0 failed** across eight assemblies.
- `dotnet format DataNet.slnx --verify-no-changes` runs **once**, in the final task.
- Read the pass/fail **counts** of every run, never the exit code alone: a `--filter` that matches nothing
  exits zero and reports success.
- **Run the oracle generator from a neutral directory** (`cd /tmp` first) with `PYTHONSAFEPATH=1`, and read
  **its own** exit code, never a pipeline's. `nltk` refuses to import under the repository.
- **No absolute machine path in anything committed** — `tools/check_machine_paths.py` enforces it.
- **A comment asserting what the reference does is a claim, and a false one is a defect** (#134). Every
  claim about HuggingFace in this lot must be traceable to a corpus case, not to reasoning.
- `main` moves under this branch: #145 is in flight in a sibling checkout and edits
  `BpeTokenizer.cs`. Fetch before pushing; rebase after each of its merges.

## What is already measured, and must not be re-derived

| Fact | Value |
| --- | --- |
| Public BPE `tokenizer.json` files surveyed | 16 |
| Declaring a normalizer | 5 — `NFC` ×4 (gpt-neox, pythia, Qwen2-0.5B, OLMo), empty `Sequence[]` ×1 (deepseek) |
| Declaring `byte_fallback` or using `Metaspace` among those five | none — all five are the lineage `BpeTokenizer` implements |
| `normalized: true` added tokens in those files | 23/25 gpt-neox, 23/25 pythia, 26/28 OLMo, 22/22 deepseek, 0/3 Qwen2 |
| Why WordPiece's mechanism cannot be borrowed | it indexes the normalized string with raw positions, sound only because lowercase preserves length |

## File Structure

| File | Responsibility |
| --- | --- |
| `tools/generate_oracles.py` | A `generate_bpe_normalizer` corpus (whole `tokenizer.json` per case, as the sibling BPE generators do) and a `generate_unicode_forms` corpus that probes .NET's tables against Rust's. |
| `tests/oracles/bpe_normalizer.json`, `tests/oracles/unicode_forms.json` | Generated; never hand-edited. |
| `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs` | `ReadBpeNormalizer` replaces `EnsureBpeNormalizerIsAbsent`. |
| `src/DataNet.Embeddings/Tokenization/BpeVocabulary.cs` | `NormalizationForms`, and its place in `Equals`/`GetHashCode`. |
| `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs` | Two scanners; `EncodeGap` normalizes one gap and scans inside it. |
| `tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs` | What loads, and what is refused by name. |
| `tests/DataNet.Embeddings.Tests/Tokenization/BpeNormalizerTests.cs` | The corpus replay and the Unicode-form probe. |
| `docs/equivalence.md`, `docs/guides/embeddings.md`, `samples/DataNet.Sample/Lot3Embeddings.cs` | The two rows that say "refuses any normalizer", the round-trip note, and the packaging gate. |

---

### Task 1: The corpus, and the question that decides whether there is an ADR

**Files:**

- Modify: `tools/generate_oracles.py`
- Create (generated): `tests/oracles/bpe_normalizer.json`, `tests/oracles/unicode_forms.json`
- Create: `tests/DataNet.Embeddings.Tests/Tokenization/BpeNormalizerTests.cs`

**Depends on:** nothing.

**Produces:** `bpe_normalizer.json` with `cases[]` of `{id, name, tokenizer_json, text, tokens[], ids[],
decoded}`, and `unicode_forms.json` with `cases[]` of `{id, form, text, normalized}`. Task 3 replays the
first; nothing else consumes the second.

**Why this is first.** D5 says a divergence between .NET's Unicode tables and Rust's turns this lot into an
ADR and removes a form from the reproduced set. That has to be known before the loader decides which four
types it accepts.

- [ ] **Step 1: Add the two generators**

In `tools/generate_oracles.py`, beside `generate_bpe_tokenizer_json` (which is the shape to follow — it
freezes the whole `tokenizer.json` into each case so the C# side parses the exact bytes HuggingFace was
handed):

```python
# Texts chosen so each form actually changes them, rather than passing through:
# a combining sequence (NFC composes, NFD decomposes), a singleton whose
# canonical form is another character (U+212B ANGSTROM SIGN -> U+00C5), and two
# compatibility characters that only the K forms touch (U+FB01 LATIN SMALL
# LIGATURE FI, U+2460 CIRCLED DIGIT ONE).
NORMALIZER_TEXTS = [
    "école",            # e + COMBINING ACUTE
    "école",             # the precomposed form of the same word
    "Ångstrom unit",     # ANGSTROM SIGN
    "ﬁve o￦clock",  # the fi ligature, and a fullwidth macron
    "① ② café",
    "hello world",            # unchanged by every form: the control
]

# Code points whose normalization is where two implementations' Unicode tables
# would disagree if they were going to. .NET normalizes through the platform's
# tables and Rust through its own crate, so this corpus is the only thing that
# can say whether the four forms are safe to reproduce -- see the spec's D5.
UNICODE_FORM_PROBES = NORMALIZER_TEXTS + [
    "ẛ̣",   # LATIN SMALL LETTER LONG S WITH DOT ABOVE + DOT BELOW
    "İ",         # LATIN CAPITAL LETTER I WITH DOT ABOVE
    "Ω",         # OHM SIGN
    "̈́",         # COMBINING GREEK DIALYTIKA TONOS, a singleton decomposition
    "가한",   # Hangul syllables, algorithmic composition
    "豈",         # a CJK compatibility ideograph
    "ᾂ",         # a Greek letter with three stacked marks
    "ﷺ",         # ARABIC LIGATURE SALLALLAHOU..., expands to 18 characters
]


def generate_unicode_forms() -> dict:
    """What tokenizers' four normalization forms produce, character for character.

    The C# side asserts String.Normalize gives the same answer. Nothing in this
    corpus involves BPE: it isolates the one question the tokenizer corpus cannot
    answer, which is whether the two runtimes' Unicode tables agree at all.
    """
    from tokenizers import normalizers  # noqa: PLC0415

    forms = [("NFC", normalizers.NFC()), ("NFKC", normalizers.NFKC()),
             ("NFD", normalizers.NFD()), ("NFKD", normalizers.NFKD())]
    cases = []
    for text in UNICODE_FORM_PROBES:
        for name, normalizer in forms:
            cases.append({
                "id": len(cases),
                "form": name,
                "text": text,
                "normalized": normalizer.normalize_str(text),
            })
    return {
        "metadata": {
            "algorithm": "Unicode normalization forms",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_bpe_normalizer() -> dict:
    """GPT-2's byte-level BPE with a normalizer, which LoadBpe refused wholesale.

    Six pipelines: one per form, a Sequence of two, and an empty Sequence -- the
    deepseek shape, which does nothing and was refused for nothing. The seventh
    case is the one that matters: a normalizer beside both halves of the added
    token table, which is the gpt-neox shape (23 of its 25 entries are
    normalized: true) and the only case that separates the two scanners.
    """
    from tokenizers import AddedToken, normalizers  # noqa: PLC0415

    pipelines = [
        ("nfc", normalizers.NFC(), False),
        ("nfkc", normalizers.NFKC(), False),
        ("nfd", normalizers.NFD(), False),
        ("nfkd", normalizers.NFKD(), False),
        ("sequence", normalizers.Sequence([normalizers.NFD(), normalizers.NFC()]), False),
        ("empty_sequence", normalizers.Sequence([]), False),
        ("added_tokens", normalizers.NFC(), True),
    ]

    cases = []
    for name, normalizer, with_added in pipelines:
        tokenizer = _gpt2_tokenizer()
        tokenizer.normalizer = normalizer
        if with_added:
            # One of each half. The normalized entry is written decomposed, so it
            # can only match once its own content has been normalized too.
            tokenizer.add_tokens([AddedToken("café", normalized=True)])
            tokenizer.add_special_tokens([AddedToken("<|endoftext|>", special=True, normalized=False)])
        texts = NORMALIZER_TEXTS + (["a café<|endoftext|>b", "café tail"] if with_added else [])
        for text in texts:
            enc = tokenizer.encode(text)
            cases.append({
                "id": len(cases),
                "name": name,
                "tokenizer_json": tokenizer.to_str(),
                "text": text,
                "tokens": enc.tokens,
                "ids": enc.ids,
                "decoded": tokenizer.decode(enc.ids, skip_special_tokens=False),
            })
    return {
        "metadata": {
            "algorithm": "BPE normalizer",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "count": len(cases),
        },
        "cases": cases,
    }
```

Register both in the generator table beside `"bpe_tokenizer_json.json"`:

```python
        "bpe_normalizer.json": generate_bpe_normalizer,
        "unicode_forms.json": generate_unicode_forms,
```

- [ ] **Step 2: Generate, from a neutral directory, and read the generator's own exit code**

```bash
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py > /tmp/121-gen.log 2>&1
echo "generate=$?"
tail -5 /tmp/121-gen.log
cd <repo> && git status --porcelain tests/oracles/
```

Expected: exit 0, and `git status` shows exactly the two **new** files. **Any modification to an existing
corpus is a stop condition** — this lot changes no generator that feeds one.

- [ ] **Step 3: Write the Unicode-form probe test**

Create `tests/DataNet.Embeddings.Tests/Tokenization/BpeNormalizerTests.cs`. Follow the file conventions of
`tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs` — read it first for how it reaches
`OracleLoader`, how it accumulates failures rather than asserting per case, and its namespace and usings.

```csharp
    /// <summary>
    /// Whether .NET's normalization tables agree with the ones tokenizers uses.
    /// This is the question the spec's D5 makes the gate for reproducing a form at
    /// all: .NET normalizes through the platform's Unicode tables and Rust through
    /// its own crate, so agreement is measurable but not assumable, and a form that
    /// disagreed would have to be refused rather than reproduced wrongly.
    /// </summary>
    [Fact]
    public void The_four_forms_agree_with_the_reference_character_for_character()
    {
        using JsonDocument doc = OracleLoader.Load("unicode_forms.json");

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string form = c.GetProperty("form").GetString()!;
            string text = c.GetProperty("text").GetString()!;
            string expected = c.GetProperty("normalized").GetString()!;

            string actual = text.Normalize(form switch
            {
                "NFC" => NormalizationForm.FormC,
                "NFKC" => NormalizationForm.FormKC,
                "NFD" => NormalizationForm.FormD,
                "NFKD" => NormalizationForm.FormKD,
                _ => throw new InvalidOperationException($"the corpus names a form this test does not know: {form}"),
            });

            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                failures.Add($"[{form}] {Escape(text)}: expected {Escape(expected)}, got {Escape(actual)}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>Renders a string as its code points, so a failure names what differs rather than showing two identical-looking lines.</summary>
    private static string Escape(string text) =>
        string.Concat(text.Select(ch => ch < 0x20 || ch > 0x7e ? $"\\u{(int)ch:x4}" : ch.ToString()));
```

- [ ] **Step 4: Run it, and record which way D5 went**

```bash
cd <repo>
./.dotnet-guarded dotnet test DataNet.slnx -c Release --filter "FullyQualifiedName~The_four_forms_agree" > /tmp/121-t1.log 2>&1
echo "test=$?"
grep -E "^Réussi!|^Échoué!|^Passed!|^Failed!" /tmp/121-t1.log
```

Expected: **2 passing** (one per mirrored assembly).

- **All four agree** → the reproduced set stays as D1 wrote it, and Task 4 records the agreement in one
  sentence.
- **A form disagrees** → that form is **refused** by Task 2 instead of reproduced, and Task 4 writes an ADR
  naming the code point and both answers. Do not "fix" it in C#; the divergence is the finding.

Report which happened. Do not proceed to Task 2 without stating it.

- [ ] **Step 5: Commit**

```bash
git add tools/generate_oracles.py tests/oracles/bpe_normalizer.json tests/oracles/unicode_forms.json \
        tests/DataNet.Embeddings.Tests/Tokenization/BpeNormalizerTests.cs
git commit -m "Freeze what a BPE normalizer does, and whether .NET agrees about it"
```

---

### Task 2: The loader reads a named set and refuses the rest by name

**Files:**

- Modify: `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs` (`EnsureBpeNormalizerIsAbsent` at
  `:623`, and its call site at `:542`)
- Modify: `src/DataNet.Embeddings/Tokenization/BpeVocabulary.cs`
- Modify: `tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs`

**Depends on:** Task 1 (which forms are safe).

**Interfaces:**

- Produces `BpeVocabulary.NormalizationForms` — `IReadOnlyList<NormalizationForm>`, empty when the file
  declares no normalizer, in **declared order**. Task 3 consumes it.

- [ ] **Step 1: Carry the forms on the vocabulary**

In `BpeVocabulary.cs`, beside `AddPrefixSpace` and the other `init` properties:

```csharp
    /// <summary>
    /// The normalization forms the file declared, in the order it declared them,
    /// empty when it declared no normalizer.
    /// </summary>
    /// <remarks>
    /// A list rather than a single form because a <c>Sequence</c> may name several,
    /// and applied in order rather than collapsed to the last one: composing these
    /// four does reduce to the last through NFKC's idempotence, but a reader would
    /// have to verify that identity to trust the code, and the loop costs nothing.
    /// </remarks>
    public IReadOnlyList<NormalizationForm> NormalizationForms { get; init; } = [];
```

`Equals` compares the other scalars by value, so compare this one element-wise, beside the existing
`AddedTokens` loop, and count it in `GetHashCode` the way the other counts are counted:

```csharp
            || NormalizationForms.Count != other.NormalizationForms.Count)
```

```csharp
        for (int i = 0; i < NormalizationForms.Count; i++)
        {
            if (NormalizationForms[i] != other.NormalizationForms[i])
            {
                return false;
            }
        }
```

- [ ] **Step 2: Replace the blanket refusal**

In `TokenizerJsonLoader.cs`, delete `EnsureBpeNormalizerIsAbsent` and write:

```csharp
    /// <summary>
    /// Reads the BPE normalizer: the four Unicode forms, a <c>Sequence</c> of them,
    /// or nothing. The counterpart of <see cref="ReadLowercaseFrom"/> for WordPiece
    /// and <see cref="ReadUnigramNormalizer"/> for Unigram.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Measured across sixteen public BPE <c>tokenizer.json</c> files: five declare a
    /// normalizer, four of them <c>NFC</c> (Qwen2, GPT-NeoX, Pythia, OLMo) and one an
    /// empty <c>Sequence</c> (deepseek-coder). None of the five declares
    /// <c>byte_fallback</c> or uses <c>Metaspace</c>, so all five are the lineage
    /// <see cref="BpeTokenizer"/> implements and the blanket refusal was the only
    /// thing stopping them.
    /// </para>
    /// <para>
    /// An empty <c>Sequence</c> yields an empty list and normalizes nothing, which is
    /// the deepseek case: a declaration that provably changes nothing is accepted,
    /// as <c>dropout: 0.0</c> and <c>end_of_word_suffix: ""</c> already are.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<NormalizationForm> ReadBpeNormalizer(JsonElement root)
    {
        if (!root.TryGetProperty("normalizer", out JsonElement normalizer) || normalizer.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        var forms = new List<NormalizationForm>();
        CollectNormalizationForms(normalizer, forms);
        return forms;
    }

    private static void CollectNormalizationForms(JsonElement normalizer, List<NormalizationForm> forms)
    {
        string type = OptionalString(normalizer, "type") ?? UntypedName;
        switch (type)
        {
            case "NFC":
                forms.Add(NormalizationForm.FormC);
                return;

            case "NFKC":
                forms.Add(NormalizationForm.FormKC);
                return;

            case "NFD":
                forms.Add(NormalizationForm.FormD);
                return;

            case "NFKD":
                forms.Add(NormalizationForm.FormKD);
                return;

            case "Sequence":
                if (!normalizer.TryGetProperty("normalizers", out JsonElement inner) || inner.ValueKind != JsonValueKind.Array)
                {
                    throw Unsupported("its normalizer is a Sequence with no 'normalizers' array", "the file is not usable");
                }
                foreach (JsonElement step in inner.EnumerateArray())
                {
                    CollectNormalizationForms(step, forms);
                }
                return;

            case "Replace":
                throw Unsupported(
                    "its normalizer is 'Replace'",
                    "a Replace pattern may be a Rust regex, whose flavour .NET does not share, so reproducing it needs a measurement nobody has made");

            default:
                throw Unsupported(
                    $"its normalizer is '{type}'",
                    "only NFC, NFKC, NFD, NFKD and a Sequence of those are understood");
        }
    }
```

At the call site (`:542`), replace `EnsureBpeNormalizerIsAbsent(root);` with a read whose result reaches
the returned `BpeVocabulary`:

```csharp
        IReadOnlyList<NormalizationForm> normalizationForms = ReadBpeNormalizer(root);
```

and add `NormalizationForms = normalizationForms,` to the object initializer that already sets
`ByteLevel`, `AddPrefixSpace` and the rest.

If Task 1 found a form that disagrees with the reference, that form's `case` throws `Unsupported` naming the
divergence instead of adding to the list.

- [ ] **Step 3: Test what loads and what is refused**

In `TokenizerJsonLoaderTests.cs`, following the file's existing idiom for building a `tokenizer.json` string
and calling `TokenizerJsonLoader.LoadBpe` (read the neighbouring tests first — several already build a
minimal file inline):

```csharp
    [Theory]
    [InlineData("{\"type\":\"NFC\"}", 1)]
    [InlineData("{\"type\":\"NFKD\"}", 1)]
    [InlineData("{\"type\":\"Sequence\",\"normalizers\":[{\"type\":\"NFD\"},{\"type\":\"NFC\"}]}", 2)]
    [InlineData("{\"type\":\"Sequence\",\"normalizers\":[]}", 0)]
    [InlineData("null", 0)]
    public void LoadBpe_reads_the_normalization_forms_a_file_declares(string normalizer, int expected)
    {
        BpeVocabulary vocab = TokenizerJsonLoader.LoadBpe(Bytes(MinimalBpeJson(normalizer)), OracleReplay.BpeBounds());

        Assert.Equal(expected, vocab.NormalizationForms.Count);
    }

    /// <summary>
    /// Every normalizer outside the reproduced set is refused <em>by name</em>, which
    /// is the shape the sibling readers use: a message naming what was found is what
    /// lets a reader tell "not supported" from "not understood".
    /// </summary>
    [Theory]
    [InlineData("{\"type\":\"Replace\",\"pattern\":{\"String\":\" \"},\"content\":\"_\"}", "Replace")]
    [InlineData("{\"type\":\"Prepend\",\"prepend\":\"X\"}", "Prepend")]
    [InlineData("{\"type\":\"StripAccents\"}", "StripAccents")]
    [InlineData("{\"type\":\"Lowercase\"}", "Lowercase")]
    [InlineData("{\"type\":\"BertNormalizer\"}", "BertNormalizer")]
    [InlineData("{\"type\":\"Sequence\",\"normalizers\":[{\"type\":\"NFC\"},{\"type\":\"Strip\"}]}", "Strip")]
    public void LoadBpe_refuses_a_normalizer_it_does_not_reproduce_by_name(string normalizer, string named)
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(MinimalBpeJson(normalizer)), OracleReplay.BpeBounds()));

        Assert.Contains(named, ex.Message, StringComparison.Ordinal);
    }
```

Add the helper if the file has no equivalent — check first, and reuse whatever it already has:

```csharp
    /// <summary>The smallest loadable byte-level BPE file, with <paramref name="normalizer"/> spliced in.</summary>
    private static string MinimalBpeJson(string normalizer) =>
        $$"""
        {"version":"1.0","normalizer":{{normalizer}},
         "pre_tokenizer":{"type":"ByteLevel","add_prefix_space":false},
         "decoder":{"type":"ByteLevel"},
         "model":{"type":"BPE","vocab":{"a":0,"b":1,"ab":2},"merges":["a b"]}}
        """;
```

`Unsupported` must produce whatever exception type the neighbouring refusal tests already assert — check
one before writing `NotSupportedException`, and match it.

- [ ] **Step 4: Build with analyzers, and run the suite**

```bash
cd <repo>
./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental > /tmp/121-t2-b.log 2>&1; echo "build=$?"; grep -E "Avertissement\(s\)|Erreur\(s\)" /tmp/121-t2-b.log
./.dotnet-guarded dotnet test DataNet.slnx -c Release > /tmp/121-t2-t.log 2>&1; echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/121-t2-t.log
```

Expected: 0 warnings, and every previously passing test still passing. `BpeTokenizer` ignores the new
property until Task 3, so a file declaring `NFC` now **loads and tokenizes without normalizing** — which is
wrong and is why Task 3 exists. The corpus replay is not written until then, deliberately: it would fail
here for a reason this task cannot fix.

- [ ] **Step 5: Commit**

```bash
git add src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs \
        src/DataNet.Embeddings/Tokenization/BpeVocabulary.cs \
        tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs
git commit -m "Read the BPE normalizer instead of refusing every one of them"
```

---

### Task 3: The tokenizer normalizes each gap, and scans it

**Files:**

- Modify: `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs` (fields near `:121`, `Encode` at `:255`,
  `EncodeSegment` at `:311`)
- Modify: `tests/DataNet.Embeddings.Tests/Tokenization/BpeNormalizerTests.cs`

**Depends on:** Tasks 1 and 2.

**Interfaces:**

- Consumes `BpeVocabulary.NormalizationForms`. Produces no new public API.

- [ ] **Step 1: Split the scanner in two and hold the forms**

Replace the single `_scanner` field and its assignment (`:121`):

```csharp
    // Two scanners because the two halves of the added-token table are matched
    // against two different strings, exactly as WordPieceTokenizer does it:
    // AddedToken.Normalized is what puts an entry in one or the other, and Special
    // has nothing to do with it. Measured across the files that declare a
    // normalizer at all, the normalized half is the majority -- 23 of gpt-neox's 25
    // entries, 22 of 22 in deepseek-coder -- so this is not a rare path.
    private readonly AddedTokenScanner _rawScanner;
    private readonly AddedTokenScanner _normalizedScanner;
    private readonly NormalizationForm[] _forms;
```

```csharp
        _forms = [.. vocabulary.NormalizationForms];
        _rawScanner = new AddedTokenScanner([.. vocabulary.AddedTokens.Where(t => !t.Normalized)]);
        _normalizedScanner = new AddedTokenScanner(
            [.. vocabulary.AddedTokens.Where(t => t.Normalized).Select(t => t with { Content = Normalize(t.Content) })]);
```

Leave every other use of `vocabulary.AddedTokens` alone — the folded `_vocab`, `_tokens` and `_addedIds`
still take the whole table, and `TryGetId` must keep answering for both halves.

- [ ] **Step 2: Normalize per gap, and scan inside it**

`Encode`'s loop keeps its shape; only the scanner it consults and the call it makes change:

```csharp
            if (!_rawScanner.TryNext(text, pos, out int start, out int end, out var added))
            {
                EncodeGap(text, pos, text.Length, tokens, ids, pieces);
                break;
            }
            if (start > pos)
            {
                EncodeGap(text, pos, start, tokens, ids, pieces);
            }
```

and the new method sits between `Encode` and `EncodeSegment`:

```csharp
    /// <summary>Normalizes <c>text[from..to]</c>, which holds no raw added token, then splits it at the normalized ones.</summary>
    /// <remarks>
    /// <para>
    /// Each gap is normalized <em>on its own</em>, rather than the whole input once
    /// with the raw positions reused against it. <see cref="WordPieceTokenizer"/> can
    /// do the latter because <c>ToLowerInvariant</c> maps char to char and preserves
    /// length; all four normalization forms compose or decompose, so a position found
    /// in the raw text means nothing in the normalized one. Per-gap normalization
    /// removes the need for that correspondence instead of extending it.
    /// </para>
    /// <para>
    /// Order: normalization first, then <c>add_prefix_space</c> and the split inside
    /// <see cref="EncodeSegment"/>. That is what the corpus measures -- a normalizer
    /// that produced a leading space would otherwise meet the "only when the segment
    /// does not already begin with one" rule, and the two would have to be ordered
    /// by evidence rather than by preference.
    /// </para>
    /// <para>
    /// A file declaring no normalizer and no normalized added token takes the same
    /// path it took before this method existed, allocating nothing extra: the guard
    /// below is what keeps that true.
    /// </para>
    /// </remarks>
    private void EncodeGap(string text, int from, int to, List<string> tokens, List<int> ids, List<string> pieces)
    {
        if (_forms.Length == 0 && _normalizedScanner.IsEmpty)
        {
            EncodeSegment(text, from, to, tokens, ids, pieces);
            return;
        }

        string gap = Normalize(text.Substring(from, to - from));
        int pos = 0;
        while (pos < gap.Length)
        {
            if (!_normalizedScanner.TryNext(gap, pos, out int start, out int end, out var added))
            {
                EncodeSegment(gap, pos, gap.Length, tokens, ids, pieces);
                break;
            }
            if (start > pos)
            {
                EncodeSegment(gap, pos, start, tokens, ids, pieces);
            }
            tokens.Add(gap.Substring(start, end - start));
            ids.Add(added.Id);
            pos = end;
        }
    }

    /// <summary>Applies the declared forms in their declared order.</summary>
    private string Normalize(string text)
    {
        string normalized = text;
        foreach (NormalizationForm form in _forms)
        {
            normalized = normalized.Normalize(form);
        }
        return normalized;
    }
```

`EncodeSegment` itself does not change.

- [ ] **Step 3: Replay the corpus**

Add to `BpeNormalizerTests.cs`, in the same shape as
`TokenizerJsonLoaderTests.LoadBpe_reproduces_every_frozen_pipeline` — accumulate failures, assert once, so
one run names every case that differs:

```csharp
    /// <summary>
    /// Every pipeline in the frozen corpus: the four forms, a Sequence, an empty
    /// Sequence, and a normalizer beside both halves of the added-token table.
    /// </summary>
    [Fact]
    public void Encode_reproduces_every_normalizer_pipeline()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_normalizer.json");

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            string text = c.GetProperty("text").GetString()!;
            int[] expected = [.. c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32())];

            BpeVocabulary vocab = TokenizerJsonLoader.LoadBpe(
                Bytes(c.GetProperty("tokenizer_json").GetString()!), OracleReplay.BpeBounds());
            var tokenizer = new BpeTokenizer(vocab);
            int[] actual = [.. tokenizer.Encode(text).Ids];

            if (!expected.SequenceEqual(actual))
            {
                failures.Add($"[{name}] {Escape(text)}: expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// What the round trip becomes once a normalizer is declared. It does not return
    /// the input -- it returns the normalized input, in Python too -- so what is
    /// asserted is that it fails the same way, against the reference's own decoded
    /// string rather than against a rule this repository invented.
    /// </summary>
    [Fact]
    public void Decode_returns_what_the_reference_returns_normalizer_included()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_normalizer.json");

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            string expected = c.GetProperty("decoded").GetString()!;

            BpeVocabulary vocab = TokenizerJsonLoader.LoadBpe(
                Bytes(c.GetProperty("tokenizer_json").GetString()!), OracleReplay.BpeBounds());
            var tokenizer = new BpeTokenizer(vocab);
            string actual = tokenizer.Decode([.. tokenizer.Encode(c.GetProperty("text").GetString()!).Ids]);

            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                failures.Add($"[{name}] expected {Escape(expected)}, got {Escape(actual)}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
```

`Decode`'s exact signature and whether it takes a `skip_special_tokens`-shaped argument must be read from
`BpeTokenizer` before writing this — match what the existing decode tests call.

- [ ] **Step 4: Run everything, and read the counts**

```bash
cd <repo>
./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental > /tmp/121-t3-b.log 2>&1; echo "build=$?"; grep -E "Avertissement\(s\)|Erreur\(s\)" /tmp/121-t3-b.log
./.dotnet-guarded dotnet test DataNet.slnx -c Release > /tmp/121-t3-t.log 2>&1; echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/121-t3-t.log
git status --porcelain tests/oracles/
```

Expected: 0 warnings, every earlier test still passing, the new corpus green, and `tests/oracles/`
**unchanged** — the existing BPE corpora are the guard that files loading today keep their exact token
stream through the restructured `Encode`, and a moved byte there is a regression, not an update.

- [ ] **Step 5: Commit**

```bash
git add src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs \
        tests/DataNet.Embeddings.Tests/Tokenization/BpeNormalizerTests.cs
git commit -m "Normalize each gap, and match the normalized entries inside it"
```

---

### Task 4: The documentation, the sample, and the ADR if Task 1 asked for one

**Files:**

- Modify: `docs/equivalence.md` (the `BpeTokenizer.Encode` row, and the `LoadBpe` row)
- Modify: `docs/guides/embeddings.md`
- Modify: `samples/DataNet.Sample/Lot3Embeddings.cs`
- Create, only under Task 1's condition: `docs/decisions/00NN-<slug>.md`

**Depends on:** Tasks 1-3.

- [ ] **Step 1: Correct both equivalence rows**

Two rows carry the blanket refusal, and both are now false:

- The `Tokenizer(BPE(...)).encode(t)` row says "the raw-versus-normalized pass that flag table also carries
  is **moot here**, since `LoadBpe` refuses any normalizer at all". It is no longer moot — that pass is what
  Task 3 implemented. Replace the clause with what the two scanners now do.
- The `Tokenizer.from_file("tokenizer.json")` (BPE) row lists "any `normalizer`" among what is **refused**.
  Move it to what is read — naming the four forms and `Sequence`, empty included — and leave the refusals it
  keeps, naming `Replace` with its reason.

Say in the `LoadBpe` row what D3 settled: with a normalizer declared, `Decode(Encode(x))` returns the
normalized text rather than `x`, as it does in Python, and `bpe_normalizer.json` measures it.

- [ ] **Step 2: The guide**

`docs/guides/embeddings.md` is user-facing, and the round trip is user-visible behaviour. Add a short note
where the guide already discusses BPE loading. If the note needs a ` ```csharp ` fence, remember the
doc-snippets gate compiles every one of them against the packed packages — prefer prose, or keep the fence
to API that exists.

- [ ] **Step 3: The packaging gate**

`samples/DataNet.Sample/Lot3Embeddings.cs` is the file that already exercises BPE. Add a **member
reference** to `BpeVocabulary.NormalizationForms` — printing its count beside the vocabulary's other
properties is enough. New public API that no sample references fails ADR 0009's gate.

- [ ] **Step 4: The ADR, only if Task 1 found a divergence**

If all four forms agreed, **write no ADR**: record the agreement in one sentence in the `LoadBpe` row and
move on. If one disagreed, write the ADR in the shape of `docs/decisions/0017-bpe-parity-scope.md`: the
code point, both answers, which form is consequently refused, and what a caller should do instead. Number
it one above the highest existing ADR.

- [ ] **Step 5: Lint, and commit**

```bash
cd <repo>
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" \
  "tools/README.md" "bench/README.md" > /tmp/121-t4-md.log 2>&1; echo "markdownlint=$?"
python3 tools/extract_doc_snippets.py > /tmp/121-t4-snip.log 2>&1; echo "snippets=$?"; tail -3 /tmp/121-t4-snip.log
git add docs samples
git commit -m "Say what LoadBpe reads now, and what it still refuses"
```

---

### Task 5: Final verification

**Depends on:** Tasks 1-4. Nothing is committed here unless a gate fails and is fixed.

- [ ] **Step 1: Every gate**

```bash
cd <repo>
git status --porcelain                                                                   # empty
./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental > /tmp/121-fv-b.log 2>&1; echo "build=$?"; grep -E "Avertissement\(s\)|Erreur\(s\)" /tmp/121-fv-b.log
./.dotnet-guarded dotnet format DataNet.slnx --verify-no-changes > /tmp/121-fv-f.log 2>&1; echo "format=$?"
./.dotnet-guarded dotnet test DataNet.slnx -c Release > /tmp/121-fv-t.log 2>&1;           echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/121-fv-t.log
python3 tools/check_version_floor.py > /tmp/121-fv-v.log 2>&1;                            echo "floor=$?"
python3 tools/check_machine_paths.py > /tmp/121-fv-p.log 2>&1;                            echo "paths=$?"
.venv-oracles/bin/python -m pytest tools/tests -q > /tmp/121-fv-py.log 2>&1;              echo "pytest=$?"; tail -2 /tmp/121-fv-py.log
```

All 0, 0 warnings, and the eight per-assembly counts read and stated.

- [ ] **Step 2: The packaging gate, end to end**

The sample consumes the packages from `./artifacts` through `samples/NuGet.config`, so it needs a fresh
pack **and** an isolated `NUGET_PACKAGES` or it judges the published packages instead of this branch
(ADR 0009):

```bash
cd <repo>
for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do
  ./.dotnet-guarded dotnet pack "$p" -c Release -o ./artifacts > /tmp/121-fv-pack.log 2>&1 || echo "PACK FAILED $p"
done
NUGET_PACKAGES=/tmp/121-nuget ./.dotnet-guarded dotnet build samples/DataNet.Sample -c Release > /tmp/121-fv-sample.log 2>&1
echo "sample=$?"; grep -E "Avertissement\(s\)|Erreur\(s\)" /tmp/121-fv-sample.log
NUGET_PACKAGES=/tmp/121-nuget ./.dotnet-guarded dotnet build samples/DataNet.DocSnippets -c Release > /tmp/121-fv-snip.log 2>&1
echo "snippets-build=$?"
```

- [ ] **Step 3: The oracle drift gate**

```bash
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py > /tmp/121-fv-gen.log 2>&1
echo "generate=$?"
cd <repo> && git status --porcelain tests/oracles/
```

Expected: empty. The job is occasionally flaky — regenerate once before reporting drift.

- [ ] **Step 4: Stop and report**

Do not push and do not open a pull request. Report: which way Task 1's Unicode question went, the
per-assembly test counts, whether any existing corpus moved, and whether an ADR was written.

---

## Self-Review

**Spec coverage.** D1 → Task 2 Steps 2-3 (the four forms, `Sequence`, empty `Sequence`, refusals by name
including `Replace`'s reason). D2 → Task 2 Step 1 (`NormalizationForms` and its equality) and Task 3
Steps 1-2 (two scanners, per-gap normalization, order against `add_prefix_space`). D3 → Task 1's `decoded`
field and Task 3 Step 3's decode test, plus Task 4 Step 1. D4 → Task 1 Step 1, including the fixture with
both halves of the added-token table. D5 → Task 1 Steps 3-4 and Task 4 Step 4, which branches both ways.
The packaging gate → Task 4 Step 3 and Task 5 Step 2. "Not one oracle byte moves" → Task 3 Step 4.

**Placeholders.** Task 4 Step 4 branches on Task 1's measured result and says what to do in both cases;
`00NN-<slug>` is a filename that cannot be known before the divergence is, and the step says how to pick it.
`<repo>` stands for a path that must not be written into a committed file — `tools/check_machine_paths.py`
refuses it.

**Type consistency.** `NormalizationForms` (`IReadOnlyList<NormalizationForm>`) is defined in Task 2 and
consumed in Task 3 and Task 4 under that name. `ReadBpeNormalizer` / `CollectNormalizationForms` /
`EncodeGap` / `Normalize` appear only where defined. `_rawScanner`, `_normalizedScanner` and `_forms`
replace `_scanner`, and Task 3 Step 1 says so explicitly. `Escape` is defined in Task 1 Step 3 and reused in
Task 3 Step 3, in the same file. `MinimalBpeJson`, `Bytes` and `OracleReplay.BpeBounds()` are Task 2's, and
the plan tells the implementer to check the two latter against the file rather than assume them.
