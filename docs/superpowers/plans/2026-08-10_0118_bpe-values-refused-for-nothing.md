# #118 — The BPE values refused for nothing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Goal:** Stop `LoadBpe` refusing two values that provably change nothing, stop an empty
`end_of_word_suffix` crashing `Decode`, and make a `Sequence`'s `ByteLevel` step default
`add_prefix_space` the way HuggingFace does.

**Architecture:** Three of the four changes are one line each. The fourth — the empty suffix — goes on
`BpeVocabulary` rather than in the loader, because the type is public and constructible and a loader-side
rule would leave a hand-built vocabulary crashing while making two equivalent vocabularies compare unequal.
Evidence comes from a new oracle corpus, because a test that loads a file without throwing proves only that
nothing was thrown.

**Tech Stack:** C# (`net10.0` + `netstandard2.0`), xunit, Python `tokenizers` 0.23.1 in `.venv-oracles`.

**Spec:** `docs/superpowers/specs/2026-08-10_0118_bpe-values-refused-for-nothing.md`

## Global Constraints

- Everything in English — code, comments, commit messages, PR body. Commit messages carry no
  `feat:`/`fix:` prefix and no process prefix such as `Fix round 1:`.
- Branch `fix/118-bpe-values-refused-for-nothing` in `/home/cyril/Documents/devs/data.net`, based on `main`
  at 38813b0. Never commit to `main`. Do not push or open a PR without asking.
- `src/` multi-targets **`netstandard2.0` and `net10.0`**. Every `src/` edit must compile on both. Test
  projects and `samples/` are `net10.0` only, and every test file is linked into the mirrored
  `*.NetStandard.Tests` project, so each new test counts **twice** in the suite total.
- The analysers run repo-wide at `AnalysisMode=All` and **warnings are errors**. Ordinal string comparison
  everywhere: `StringComparison.Ordinal`, `StringComparer.Ordinal`. Every suppression carries a reason in
  the source.
- `dotnet build` is incremental — **without `--no-incremental` no analyzer diagnostic is produced at all**.
- The local build does **not** enforce the SonarCloud quality profile (issue #109). `csharpsquid:S3776`
  (cognitive complexity, threshold 15) is enforced server-side only and blocked #116 after it was pushed.
  Keep new methods small; there is no local command that will warn you.
- **Never write `echo "exit=$?"` after a pipeline** — it reports `tail`'s exit code. Redirect to a file and
  check separately.
- `dotnet format DataNet.slnx --verify-no-changes` must exit 0. Run it **bare**, no `env -u DOTNET_ROOT`.
- Oracle work runs in `.venv-oracles` (`tokenizers` 0.23.1 installed). Run the generator **from a neutral
  working directory** — from the repository root `nltk` refuses to import its dependencies — and read the
  generator's own exit code.
- Read the pass/fail **counts** of every test run. A `--filter` that matches nothing exits zero and reports
  success. Baseline on `main` at 38813b0: **2237 passing, 0 failed**.
- `docs/superpowers/` is tracked and inside CI's markdownlint glob.

## File Structure

| File | Responsibility |
| --- | --- |
| `src/DataNet.Embeddings/Tokenization/BpeVocabulary.cs` | `EndOfWordSuffix` gains a backing field so an empty suffix reads back as absent. |
| `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs` | Two refusals become conditional; one default is corrected. |
| `tests/DataNet.Embeddings.Tests/Tokenization/ValueEqualityTests.cs` | The type-level rule and its equality consequences. |
| `tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs` | The `Decode` regression that used to throw. |
| `tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs` | The loader now accepts what it refused, and defaults the flag correctly. |
| `tools/generate_oracles.py` | A corpus proving each accepted value is a no-op, and that the default changes tokens. |
| `tests/DataNet.Embeddings.Tests/Tokenization/BpeNoOpSettingsTests.cs` *(new)* | Replays that corpus. |
| `docs/equivalence.md`, `CHANGELOG.md`, `docs/guides/embeddings.md` | The record. |

---

### Task 1: Measure before deciding

**Files:**

- Create: `/tmp/probe_118.py` (scratch, not committed)

**Depends on:** nothing.

**Produces:** the answer to the spec's named unknown, which decides whether Task 4 can build a corpus for
the empty suffix at all, and whether Task 5 writes an ADR.

The spec records three possible outcomes for an empty `end_of_word_suffix`, and they are three different
changes. Guessing here would bake an unmeasured rule into a public type.

- [ ] **Step 1: Write the probe**

```python
import json
from tokenizers import Tokenizer, models, pre_tokenizers, decoders

VOCAB = {"a": 0, "b": 1, "ab": 2, "c": 3}
MERGES = [("a", "b")]

def build(**kwargs):
    tok = Tokenizer(models.BPE(vocab=dict(VOCAB), merges=list(MERGES), unk_token=None, **kwargs))
    tok.pre_tokenizer = pre_tokenizers.Whitespace()
    return tok

for label, kwargs in [
    ("baseline", {}),
    ("end_of_word_suffix=''", {"end_of_word_suffix": ""}),
    ("continuing_subword_prefix=''", {"continuing_subword_prefix": ""}),
    ("dropout=0.0", {"dropout": 0.0}),
]:
    try:
        tok = build(**kwargs)
    except Exception as exc:                      # noqa: BLE001 - the refusal IS the measurement
        print(f"{label}: CONSTRUCTION REFUSED -> {type(exc).__name__}: {exc}")
        continue
    enc = tok.encode("ab c")
    print(f"{label}: tokens={enc.tokens} ids={enc.ids}")
    declared = json.loads(tok.to_str())["model"]
    print(f"{label}: model declares "
          f"end_of_word_suffix={declared.get('end_of_word_suffix')!r} "
          f"continuing_subword_prefix={declared.get('continuing_subword_prefix')!r} "
          f"dropout={declared.get('dropout')!r}")
    try:
        again = Tokenizer.from_str(tok.to_str())
        print(f"{label}: round trip OK, tokens={again.encode('ab c').tokens}")
    except Exception as exc:                      # noqa: BLE001
        print(f"{label}: ROUND TRIP REFUSED -> {type(exc).__name__}: {exc}")
```

- [ ] **Step 2: Run it from a neutral directory and record every line**

```bash
cd /tmp && PYTHONSAFEPATH=1 /home/cyril/Documents/devs/data.net/.venv-oracles/bin/python /tmp/probe_118.py
```

Copy the whole output into your report verbatim. Three questions must be answered explicitly:

1. Does `tokenizers` accept an empty `end_of_word_suffix` — at construction, and through
   `to_str()`/`from_str()`?
2. If it accepts it, are the tokens identical to the baseline? If they are not, **stop**: the spec's
   outcome 2 applies, D1 becomes a deliberate divergence rather than a fix, and the controller must be told
   before any code is written.
3. Do `continuing_subword_prefix=""` and `dropout=0.0` produce baseline tokens?

- [ ] **Step 3: Probe the `Sequence` default separately**

The `add_prefix_space` default cannot be measured through the Python constructor — it is a
`tokenizer.json` parsing question. Build the JSON by hand, omitting the flag inside the `Sequence`'s
`ByteLevel` step:

```python
import json
from tokenizers import Tokenizer

doc = {
    "version": "1.0", "truncation": None, "padding": None,
    "added_tokens": [], "normalizer": None,
    "pre_tokenizer": {"type": "Sequence", "pretokenizers": [
        {"type": "Split", "pattern": {"Regex": " "}, "behavior": "Isolated", "invert": False},
        {"type": "ByteLevel", "trim_offsets": True, "use_regex": False},
    ]},
    "post_processor": None,
    "decoder": {"type": "ByteLevel", "add_prefix_space": True, "trim_offsets": True, "use_regex": True},
    "model": {"type": "BPE", "dropout": None, "unk_token": None,
              "continuing_subword_prefix": None, "end_of_word_suffix": None,
              "fuse_unk": False, "byte_fallback": False, "ignore_merges": False,
              "vocab": {"a": 0, "b": 1, "Ġ": 2, "Ġa": 3}, "merges": []},
}
tok = Tokenizer.from_str(json.dumps(doc))
print("omitted:", tok.encode("a b").tokens)
```

Then set `"add_prefix_space": True` and `False` explicitly in that `ByteLevel` step and print both. The
omitted case must match one of them; record which. If it matches `False`, **stop and report** — D3's
premise is wrong.

- [ ] **Step 4: Report, do not commit**

Nothing is committed by this task. Write the measurements into your report file; Tasks 2-5 cite them.

---

### Task 2: An empty end-of-word suffix is no suffix

**Files:**

- Modify: `src/DataNet.Embeddings/Tokenization/BpeVocabulary.cs:66-67`
- Modify: `tests/DataNet.Embeddings.Tests/Tokenization/ValueEqualityTests.cs`
- Modify: `tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs`

**Depends on:** Task 1 (outcome 1 or 3; on outcome 2 this task changes shape and the controller decides).

**Interfaces:**

- Produces: `BpeVocabulary.EndOfWordSuffix` reads back `null` when set to `""`. No signature changes.

- [ ] **Step 1: Write the failing tests**

In `ValueEqualityTests.cs`, add to the existing class:

```csharp
    /// <summary>
    /// An empty suffix marks nothing, so it means the same as no suffix. The rule lives on the
    /// type rather than in the loader because <see cref="BpeVocabulary"/> is public and
    /// constructible: a hand-built vocabulary reaches <c>Decode</c> without the loader ever
    /// running, and a loader-side rule would make two vocabularies that mean the same thing
    /// compare unequal. ADR 0022 section 4 records that failure for <c>AddedToken.Normalized</c>.
    /// </summary>
    [Fact]
    public void An_empty_end_of_word_suffix_reads_back_as_absent()
    {
        BpeVocabulary empty = new(new Dictionary<string, int> { ["a"] = 0 }, []) { EndOfWordSuffix = "" };
        BpeVocabulary absent = new(new Dictionary<string, int> { ["a"] = 0 }, []) { EndOfWordSuffix = null };

        Assert.Null(empty.EndOfWordSuffix);
        Assert.Equal(absent, empty);
        Assert.Equal(absent.GetHashCode(), empty.GetHashCode());
    }

    /// <summary>
    /// The <c>with</c> expression goes through the same <c>init</c> accessor, so a suffix
    /// emptied by a copy is absent too. #104 learned that a computed member has to be checked
    /// on this path specifically, not only on construction.
    /// </summary>
    [Fact]
    public void A_with_expression_emptying_the_end_of_word_suffix_reads_back_as_absent()
    {
        BpeVocabulary marked = new(new Dictionary<string, int> { ["a"] = 0 }, []) { EndOfWordSuffix = "</w>" };

        BpeVocabulary emptied = marked with { EndOfWordSuffix = "" };

        Assert.Null(emptied.EndOfWordSuffix);
    }
```

Check `BpeVocabulary`'s positional parameters before writing the constructor calls above — read
`src/DataNet.Embeddings/Tokenization/BpeVocabulary.cs:25` and use the real parameter list rather than the
two-argument shape sketched here if it differs.

In `BpeTokenizerTests.cs`, add the regression:

```csharp
    /// <summary>
    /// <c>StringBuilder.Replace("", " ")</c> throws, so an empty suffix used to crash the classic
    /// lineage's <c>Decode</c> after the file had loaded cleanly. Issue #118.
    /// </summary>
    [Fact]
    public void Decode_does_not_throw_when_the_end_of_word_suffix_is_empty()
    {
        BpeVocabulary vocabulary = new(
            new Dictionary<string, int> { ["a"] = 0, ["b"] = 1 },
            []) { EndOfWordSuffix = "" };
        BpeTokenizer tokenizer = new(vocabulary);

        string decoded = tokenizer.Decode([0, 1]);

        Assert.Equal("ab", decoded);
    }
```

Read a neighbouring test in the same file first and match how it builds a classic (non-byte-level)
vocabulary; the shape above is indicative, the file's own idiom wins. If `Decode`'s expected output is not
obviously `"ab"`, derive it from an equivalent test rather than asserting a guess.

- [ ] **Step 2: Run them and watch them fail**

```bash
dotnet test DataNet.slnx -c Release --filter "FullyQualifiedName~end_of_word_suffix" > /tmp/118-red.log 2>&1
echo "test=$?"
tail -20 /tmp/118-red.log
```

Expected: the two equality tests fail on `Assert.Null`, and the decode test fails with
`ArgumentException`. **Read the count** — three tests per project, six in total across the mirrors. A run
of 0 tests is not a red run.

- [ ] **Step 3: Put the rule on the type**

Replace `BpeVocabulary.cs:66-67` with:

```csharp
    private readonly string? _endOfWordSuffix;

    /// <summary>The marker closing a word, e.g. <c>&lt;/w&gt;</c>; <see langword="null"/> for byte-level models.</summary>
    /// <remarks>
    /// An empty marker marks nothing, so it reads back as <see langword="null"/>: a
    /// <c>tokenizer.json</c> may declare <c>"end_of_word_suffix": ""</c>, and the two spellings
    /// have to mean one thing on a public, constructible type — otherwise a loaded vocabulary and
    /// a hand-built one compare unequal while behaving identically.
    /// </remarks>
    public string? EndOfWordSuffix
    {
        get => _endOfWordSuffix;
        init => _endOfWordSuffix = string.IsNullOrEmpty(value) ? null : value;
    }
```

- [ ] **Step 4: Run them and watch them pass**

```bash
dotnet build DataNet.slnx -c Release --no-incremental > /tmp/118-build.log 2>&1
echo "build=$?"
tail -3 /tmp/118-build.log
dotnet test DataNet.slnx -c Release > /tmp/118-green.log 2>&1
echo "test=$?"
tail -12 /tmp/118-green.log
```

Expected: 0 warnings, and **2243 passing** (2237 + 3 new tests × 2 mirrors). If the total differs, account
for it before moving on.

- [ ] **Step 5: Commit**

```bash
git add src/DataNet.Embeddings/Tokenization/BpeVocabulary.cs \
        tests/DataNet.Embeddings.Tests/Tokenization/ValueEqualityTests.cs \
        tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs
git commit -m "Read an empty end-of-word suffix as the absent one it means"
```

---

### Task 3: The loader stops refusing what changes nothing

**Files:**

- Modify: `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs:583-604`
- Modify: `tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs`

**Depends on:** Task 1 (question 3 must have answered "baseline tokens").

**Interfaces:**

- Consumes: nothing from Task 2.
- Produces: `LoadBpe` accepts `"continuing_subword_prefix": ""` and `"dropout": 0.0`; every other value of
  either stays refused with the message it has today.

- [ ] **Step 1: Write the failing tests**

`TokenizerJsonLoaderTests.cs` already builds `tokenizer.json` documents through a `Bytes(json)`
`MemoryStream` helper — read the file and use its existing idiom rather than inventing one. Add four tests:

```csharp
    [Fact]
    public void LoadBpe_accepts_an_empty_continuing_subword_prefix()
    {
        // An empty prefix prefixes nothing, so the divergence the refusal guards cannot occur.
        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(BpeJson(@"""continuing_subword_prefix"": """"")));

        Assert.NotNull(vocabulary);
    }

    [Fact]
    public void LoadBpe_still_refuses_a_non_empty_continuing_subword_prefix()
    {
        NotSupportedException error = Assert.Throws<NotSupportedException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(BpeJson(@"""continuing_subword_prefix"": ""##"""))));

        Assert.Contains("continuing_subword_prefix", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadBpe_accepts_a_zero_dropout()
    {
        // At 0.0 no merge is ever skipped, which is the determinism the refusal protects.
        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(BpeJson(@"""dropout"": 0.0")));

        Assert.NotNull(vocabulary);
    }

    [Fact]
    public void LoadBpe_still_refuses_a_non_zero_dropout()
    {
        NotSupportedException error = Assert.Throws<NotSupportedException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(BpeJson(@"""dropout"": 0.1"))));

        Assert.Contains("dropout", error.Message, StringComparison.Ordinal);
    }
```

`BpeJson(...)` above stands for whatever the file already uses to build a minimal BPE document with an
extra model property. If no such helper exists, write one in the test file and use it for all four — do not
paste the same JSON literal four times.

- [ ] **Step 2: Run them and watch the two acceptance tests fail**

```bash
dotnet test DataNet.slnx -c Release --filter "FullyQualifiedName~LoadBpe_accepts_an_empty|FullyQualifiedName~LoadBpe_accepts_a_zero" > /tmp/118-t3-red.log 2>&1
echo "test=$?"
tail -20 /tmp/118-t3-red.log
```

Expected: both fail with `NotSupportedException`. The two refusal tests already pass — that is the point:
they pin what must not change.

- [ ] **Step 3: Make the conditions conditional**

In `EnsureBpeModelSettingsAreReproduced`, `TokenizerJsonLoader.cs:585`:

```csharp
        if (OptionalString(model, "continuing_subword_prefix") is { Length: > 0 } prefix)
```

and at `:597`, replace the presence check with a value check:

```csharp
        if (model.TryGetProperty("dropout", out JsonElement dropout)
            && dropout.ValueKind == JsonValueKind.Number
            && dropout.GetDouble() != 0.0)
```

Keep both refusal messages exactly as they are — they are still right for the values that still throw. Add
one comment above each condition saying why the empty and zero cases are exempt; a reader seeing
`Length: > 0` should not have to infer it.

Note the `ValueKind` narrowing: a `dropout` that is neither null nor a number is malformed, and this
condition would now let it through. Decide deliberately — either keep refusing it (an `else` naming it) or
say in the comment why a malformed value is someone else's problem. Do not leave it unaddressed.

- [ ] **Step 4: Run the whole suite**

```bash
dotnet build DataNet.slnx -c Release --no-incremental > /tmp/118-t3-build.log 2>&1
echo "build=$?"
tail -3 /tmp/118-t3-build.log
dotnet test DataNet.slnx -c Release > /tmp/118-t3-green.log 2>&1
echo "test=$?"
tail -12 /tmp/118-t3-green.log
```

Expected: 0 warnings, **2251 passing** (2243 + 4 × 2).

- [ ] **Step 5: Commit**

```bash
git add src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs \
        tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs
git commit -m "Refuse the model settings that change something, not the ones that do not"
```

---

### Task 4: A Sequence's ByteLevel step defaults add_prefix_space the way HuggingFace does

**Files:**

- Modify: `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs:797`
- Modify: `tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs`

**Depends on:** Task 1 Step 3, which must have shown the omitted flag behaving as `true`.

- [ ] **Step 1: Write the failing test**

The proof runs through `Encode`, not through the parsed flag: a test asserting
`vocabulary.AddPrefixSpace` would pass even if the flag were never used.

```csharp
    /// <summary>
    /// HuggingFace's <c>ByteLevel</c> defaults <c>add_prefix_space</c> to <see langword="true"/> in
    /// both positions. This reader defaulted it to <see langword="false"/> inside a
    /// <c>Sequence</c>, so a file omitting the flag loaded with the wrong value. Issue #118.
    /// </summary>
    [Fact]
    public void LoadBpe_defaults_add_prefix_space_on_a_sequence_byte_level_step()
    {
        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(SequenceJsonWithoutAddPrefixSpace));
        BpeTokenizer tokenizer = new(vocabulary);

        Assert.Equal(["Ġa"], tokenizer.Encode("a").Tokens);
    }
```

Build `SequenceJsonWithoutAddPrefixSpace` from the JSON your Task 1 Step 3 probe used, with the vocabulary
the probe declared, and assert the token stream the probe recorded. Do not invent the expected value —
copy it from the measurement.

- [ ] **Step 2: Run it and watch it fail**

```bash
dotnet test DataNet.slnx -c Release --filter "FullyQualifiedName~defaults_add_prefix_space" > /tmp/118-t4-red.log 2>&1
echo "test=$?"
tail -20 /tmp/118-t4-red.log
```

Expected: FAIL, the tokens lacking the `Ġ`.

- [ ] **Step 3: Correct the default**

`TokenizerJsonLoader.cs:797`:

```csharp
        bool addPrefixSpace = OptionalBoolean(byteLevelStep, "add_prefix_space") ?? true;
```

Add a comment naming the sibling at `:756` it now agrees with, so the next reader sees the two defaults are
deliberately the same rather than coincidentally.

- [ ] **Step 4: Run the whole suite**

```bash
dotnet test DataNet.slnx -c Release > /tmp/118-t4-green.log 2>&1
echo "test=$?"
tail -12 /tmp/118-t4-green.log
```

Expected: **2253 passing** (2251 + 1 × 2). Watch for a *pre-existing* test that asserted the old default:
if one fails, it encoded the bug, and its fix belongs in this commit with a comment saying so.

- [ ] **Step 5: Commit**

```bash
git add src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs \
        tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs
git commit -m "Default a Sequence's byte-level prefix space the way both readers should"
```

---

### Task 5: The corpus that proves the values are no-ops

**Files:**

- Modify: `tools/generate_oracles.py`
- Create: `tests/oracles/bpe_no_op_settings.json` (generated, committed)
- Create: `tests/DataNet.Embeddings.Tests/Tokenization/BpeNoOpSettingsTests.cs`

**Depends on:** Tasks 2-4.

**Interfaces:**

- Consumes: the accepted values from Tasks 2-4.
- Produces: nothing later tasks call.

Tasks 3 and 4 asserted that these values change nothing. That assertion is what this task turns into a
measured fact — until now, a load test only proved nothing was thrown.

- [ ] **Step 1: Add the generator**

Follow `generate_bpe_added_token_flags`'s shape exactly (`tools/generate_oracles.py:2507`): same metadata
keys, `tokenizer.to_str()` recorded in the metadata so the C# side parses the exact bytes HuggingFace was
handed, and one record per case with `tokens` and `ids`.

Cases, each paired with a baseline built from the same vocabulary and merges with the setting absent:

1. `end_of_word_suffix: ""` versus absent — **only if Task 1 showed `tokenizers` accepts it.** If it
   refuses, skip this case and say so in the corpus metadata and your report; the unit tests from Task 2
   carry the proof alone.
2. `continuing_subword_prefix: ""` versus absent.
3. `dropout: 0.0` versus absent.
4. The `Sequence` with `add_prefix_space` omitted, versus the same file declaring it `true` and declaring
   it `false` — three cases, so the corpus pins which one the default matches rather than asserting it.

Register it in `main`'s generators dict as `bpe_no_op_settings.json`.

- [ ] **Step 2: Generate, and read the generator's own exit code**

```bash
cd /tmp && PYTHONSAFEPATH=1 /home/cyril/Documents/devs/data.net/.venv-oracles/bin/python \
  /home/cyril/Documents/devs/data.net/tools/generate_oracles.py > /tmp/118-gen.log 2>&1
echo "generate=$?"
tail -5 /tmp/118-gen.log
cd /home/cyril/Documents/devs/data.net && git status --porcelain tests/oracles/
```

Expected: exit 0, and **exactly one new file**. If any other corpus moved, stop and report it — that is a
determinism failure in something else, not a result of this change.

- [ ] **Step 3: Replay it**

Create `BpeNoOpSettingsTests.cs` following `BpeAddedTokenFlagsTests.cs`. Use
`OracleReplay.AssertEncodings(doc, tokenizer.Encode, "tokens")` — **do not hand-roll the loop**. The helper
carries an `Assert.True(replayed > 0, …)` guard, and a hand-rolled copy dropping it is exactly the
finding that issue #104's review raised twice.

Add one test that the corpus's paired cases carry **equal** token streams, since "the value is a no-op" is
the claim and per-case replay alone does not state it:

```csharp
    /// <summary>
    /// Replaying each case proves the C# matches Python. This proves the claim the acceptance
    /// rests on: Python itself produces the same tokens with the setting as without it.
    /// </summary>
    [Fact]
    public void Each_no_op_setting_encodes_exactly_like_its_baseline()
    {
        // Compare the recorded token streams of every (case, baseline) pair in the corpus.
    }
```

Fill that body in against the corpus's actual shape — the comment is the specification, not a placeholder
to leave behind.

- [ ] **Step 4: Green**

```bash
dotnet test DataNet.slnx -c Release > /tmp/118-t5-green.log 2>&1
echo "test=$?"
tail -12 /tmp/118-t5-green.log
```

Read the count and state it. Then prove the replay **discriminates**: empty the `cases` array in the
**output-directory copy** of the corpus (`tests/DataNet.Embeddings.Tests/bin/Release/net10.0/oracles/`,
never the committed source file), confirm the test now fails, and restore it. Report the failure message.

- [ ] **Step 5: Commit**

```bash
git add tools/generate_oracles.py tests/oracles/bpe_no_op_settings.json \
        tests/DataNet.Embeddings.Tests/Tokenization/BpeNoOpSettingsTests.cs
git commit -m "Replay what tokenizers does with the settings that change nothing"
```

---

### Task 6: The record

**Files:**

- Modify: `docs/equivalence.md` (the `LoadBpe` row, ~:111)
- Modify: `CHANGELOG.md`
- Modify: `docs/guides/embeddings.md` *(only if the grep in Step 1 finds a falsified claim)*
- Create: `docs/decisions/00NN-….md` *(only on Task 1's outcome 2)*

**Depends on:** Tasks 2-5.

- [ ] **Step 1: Find what this branch falsified**

```bash
cd /home/cyril/Documents/devs/data.net
grep -rn "continuing_subword_prefix\|end_of_word_suffix\|dropout\|add_prefix_space" \
  --include=*.md docs README.md CONTRIBUTING.md > /tmp/118-doc-hits.txt
wc -l /tmp/118-doc-hits.txt
```

Read every hit. Counts, enumerations and "see X" pointers go stale silently; this repository has been
bitten by that twice on #104.

- [ ] **Step 2: The equivalence row**

`docs/equivalence.md:111` enumerates eleven refusals in prose, each verified against the code during #104.
Two become conditional: a **non-empty** `continuing_subword_prefix`, a **non-zero** `dropout`. Change only
those two clauses; the other nine are still exact.

- [ ] **Step 3: The CHANGELOG, after establishing which section it belongs in**

```bash
git tag --list 'DataNet.Embeddings/*'
cat src/DataNet.Embeddings/Version.props
git log --oneline -1 -- src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs
```

If the classic lineage and `end_of_word_suffix` shipped in a released version, the crash is a **Fixed**
entry that concerns real users. If they did not, it is a defect that never shipped and the entry says so.
Establish which before writing the sentence; do not guess from the version number alone.

Add the entry under `[Unreleased]`, in the file's existing sectioning and voice.

- [ ] **Step 4: The ADR, only if Task 1 returned outcome 2**

If `tokenizers` does something with an empty `end_of_word_suffix` other than ignoring it, then reading it
as absent is a deliberate divergence and belongs in `docs/decisions/`. Take the next free number — check
`ls docs/decisions/` rather than assuming, since 0020 landed with #93 and 0021 is reserved by #92 — and
follow `0022-added-token-matching-flags.md`'s shape. On outcomes 1 and 3, write no ADR and say why in your
report.

- [ ] **Step 5: Lint and commit**

```bash
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" \
  "tools/README.md" "bench/README.md" > /tmp/118-mdlint.log 2>&1
echo "markdownlint=$?"
tail -3 /tmp/118-mdlint.log
git add docs CHANGELOG.md
git commit -m "Record which values the BPE loader refuses, and which it stopped refusing"
```

---

### Task 7: Final verification

**Depends on:** Task 6. Nothing is committed here unless a gate fails and is fixed.

- [ ] **Step 1: Every gate, with real exit codes**

```bash
cd /home/cyril/Documents/devs/data.net
git status --porcelain                                                    # empty
dotnet build DataNet.slnx -c Release --no-incremental > /tmp/118-fv-b.log 2>&1; echo "build=$?"; tail -3 /tmp/118-fv-b.log
dotnet format DataNet.slnx --verify-no-changes > /tmp/118-fv-f.log 2>&1;   echo "format=$?"
dotnet test DataNet.slnx -c Release > /tmp/118-fv-t.log 2>&1;              echo "test=$?"; tail -12 /tmp/118-fv-t.log
python3 tools/check_version_floor.py > /tmp/118-fv-v.log 2>&1;             echo "floor=$?"
```

All must be 0, the build must show 0 warnings, and the test log's per-assembly counts must be read — all
eight, the four `*.NetStandard.Tests` mirrors included.

- [ ] **Step 2: The oracle drift gate**

```bash
cd /tmp && PYTHONSAFEPATH=1 /home/cyril/Documents/devs/data.net/.venv-oracles/bin/python \
  /home/cyril/Documents/devs/data.net/tools/generate_oracles.py > /tmp/118-fv-gen.log 2>&1
echo "generate=$?"
cd /home/cyril/Documents/devs/data.net && git status --porcelain tests/oracles/
```

Expected: empty. This gate is known to be flaky on this repository — if a corpus moves, regenerate once
more and compare before reporting drift.

- [ ] **Step 3: The two gates outside the solution**

```bash
SCRATCH=/tmp/claude-49201103/-home-cyril-Documents-devs-data-net/dc8f8ded-9994-4ad8-969c-b4d66b7527f8/scratchpad
rm -rf ./artifacts "$SCRATCH/pack-packages"
NUGET_PACKAGES="$SCRATCH/pack-packages" bash -c 'for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do dotnet pack "$p" -c Release -o ./artifacts || exit 1; done'
python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
rm -rf "$SCRATCH/sample-packages"
NUGET_PACKAGES="$SCRATCH/sample-packages" dotnet run --project samples/DataNet.Sample -c Release
python3 tools/extract_doc_snippets.py
NUGET_PACKAGES="$SCRATCH/sample-packages" dotnet build samples/DataNet.DocSnippets -c Release --no-incremental
```

`dotnet build DataNet.slnx` does not reach `samples/`, so running the sample is the only thing that
compiles it. This branch adds no public type, so the packaging gate should pass unchanged — but #104's
final verification failed here precisely because nobody expected it to.

- [ ] **Step 4: Stop and report**

Do not push and do not open a pull request. Report the state and let the user decide both.

---

## Self-Review

**Spec coverage.** D1 → Task 2. D2 → Task 3. D3 → Task 4. D4 (do not touch `ContinuingSubwordPrefix`) →
no task, deliberately, and Task 3 touches only the refusal condition. The named measurement → Task 1, whose
outcome 2 branches into Task 5 Step 1 and Task 6 Step 4. Evidence section → Tasks 2, 4 and 5. Documentation
section → Task 6. Risks: the probe branch is carried in Tasks 1, 5 and 6; the `Equals` change is covered by
Task 2's tests and Task 6's CHANGELOG entry; "no existing corpus can catch a mistake" is why Task 5 exists
and why its Step 4 mutates the output copy to prove the replay discriminates.

**Placeholders.** Task 5 Step 3 contains a test body written as a comment. That is deliberate and marked:
the corpus's shape is not known until Step 1 runs, and the comment states the assertion required. Every
other code block is complete. Three constructor shapes (`BpeVocabulary` in Tasks 2 and 3, `Bytes`/`BpeJson`
in Task 3) are marked "read the file's own idiom first" rather than guessed — the plan says so at each site
instead of pretending to know.

**Type consistency.** `EndOfWordSuffix` is the property name throughout; `_endOfWordSuffix` the field.
`OptionalString`, `OptionalBoolean`, `EnsureBpeModelSettingsAreReproduced` and
`OracleReplay.AssertEncodings` match the names in the current source. The corpus file is
`bpe_no_op_settings.json` in both Task 5 steps and the file table.
