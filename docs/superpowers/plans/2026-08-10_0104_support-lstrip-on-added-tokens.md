# #104 Added-Token Matching Flags — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reproduce HuggingFace's `lstrip`, `rstrip`, `single_word` and `special` flags on `added_tokens`, in both `BpeTokenizer` and `WordPieceTokenizer`, so RoBERTa-family `tokenizer.json` files load.

**Architecture:** One public `AddedToken` record carries the four flags; one internal `AddedTokenScanner` owns all matching semantics and is called by both tokenizers, so the two cannot drift. `BpeVocabulary.AddedTokens` changes type (free — unreleased); `WordPieceVocabulary` gains the property and stops having added tokens folded into its `Vocab`.

**Tech Stack:** C# / .NET (`net10.0` + `netstandard2.0` for `src/`), xunit, `tokenizers` 0.23.1 in `.venv-oracles` for the oracle.

**Spec:** `2026-08-10-104-added-token-flags-design.md` (same directory). Read its **Measurements** section before Task 1 — it is the source of every expected value below.

## Global Constraints

- **Everything in English** — code, comments, ADR, commit messages, PR body.
- Branch `feat/104-added-token-lstrip` in `/home/cyril/Documents/devs/data.net`, based on `c09b95f`. Never commit to `main`. Do not push or open a PR without asking.
- `src/` multi-targets **`netstandard2.0` and `net10.0`**. Every `src/` edit must compile on both. Test projects and `samples/` are `net10.0` only.
- **The analysers are on repo-wide at `AnalysisMode=All`** since #107. A finding is a build error. Every suppression carries a reason in the source; area-wide `NoWarn` lives in that area's `Directory.Build.props`. Ordinal string comparison everywhere: `StringComparison.Ordinal`, `StringComparer.Ordinal`.
- `dotnet build` is incremental — **without `--no-incremental` no analyzer diagnostic is produced at all**. Use it on any build meant to show a finding.
- **Never write `echo "exit=$?"` after a pipeline** — it reports `tail`'s exit code. Redirect to a file and check separately.
- `dotnet format DataNet.slnx --verify-no-changes` must exit 0; CI runs it. Run it **bare** — no `env -u DOTNET_ROOT`.
- Oracle work runs in `.venv-oracles` (already created, `tokenizers` 0.23.1 installed). `generate_oracles.py` is deterministic; committing the regenerated JSON is part of the change.
- **ADR number is 0022**, not the next free one — 0020 and 0021 are taken by work in flight. Confirm before writing the file.
- Read the pass/fail **counts** of every test run. A suite that ran 0 tests is not a green suite. Baseline after #110: **1615 passing**.

## File Structure

| File | Responsibility |
| --- | --- |
| `src/DataNet.Embeddings/Tokenization/AddedToken.cs` *(new)* | The public record: content, id, four flags. Data only. |
| `src/DataNet.Embeddings/Tokenization/AddedTokenScanner.cs` *(new, internal)* | All matching semantics: leftmost-then-longest, `SingleWord` rejection, `Lstrip`/`Rstrip` span expansion. The single place either tokenizer asks "what is the next added token here". |
| `BpeVocabulary.cs` | `AddedTokens` becomes `IReadOnlyList<AddedToken>`; `Equals`/`GetHashCode` follow. |
| `WordPieceVocabulary.cs` | Gains `AddedTokens`; `Equals`/`GetHashCode` follow. |
| `BpeTokenizer.cs` | Delegates matching to the scanner; `Decode(skipSpecialTokens: true)` drops only `Special` entries. |
| `WordPieceTokenizer.cs` | Scans before normalizing; segments between matches keep today's lowercase-then-regex path. |
| `Persistence/TokenizerJsonLoader.cs` | Builds `AddedToken` values instead of folding; the three flag refusals go. |
| `tools/generate_oracles.py` | New flag corpora for both tokenizers. |
| `docs/decisions/0022-…md` *(new)*, `docs/equivalence.md` | The record. |

---

### Task 1: `AddedToken` and `AddedTokenScanner`

**Files:**

- Create: `src/DataNet.Embeddings/Tokenization/AddedToken.cs`
- Create: `src/DataNet.Embeddings/Tokenization/AddedTokenScanner.cs`
- Create: `tests/DataNet.Embeddings.Tests/Tokenization/AddedTokenScannerTests.cs`

**Depends on:** nothing. Nothing else references these yet, so the solution stays green.

**Produces:** `public sealed record AddedToken(string Content, int Id)` with `bool Lstrip/Rstrip/SingleWord/Special` init properties; `internal sealed class AddedTokenScanner` with `internal AddedTokenScanner(IReadOnlyList<AddedToken> tokens)` and `internal bool TryNext(string text, int from, out int start, out int end, out AddedToken token)`.

- [ ] **Step 1: Measure the one case the spec does not cover**

Two added tokens compete, and the one further right carries `Lstrip`. Does the left-strip expansion happen before or after the leftmost-wins comparison? Guessing here would bake in an unmeasured rule.

```bash
cd /home/cyril/Documents/devs/data.net
cat > /tmp/probe_tie.py <<'PY'
from tokenizers import Tokenizer, models, pre_tokenizers, decoders, AddedToken
base = ["a","b","Ġa","Ġb","Ġ","<x>","<y>"]
tok = Tokenizer(models.BPE(vocab={t:i for i,t in enumerate(base)}, merges=[], unk_token=None))
tok.pre_tokenizer = pre_tokenizers.ByteLevel(add_prefix_space=False, use_regex=True)
tok.decoder = decoders.ByteLevel()
tok.add_tokens([AddedToken("<x>"), AddedToken("<y>", lstrip=True)])
for s in ["a <x><y> b", "a<x> <y>b", "a <y><x> b"]:
    e = tok.encode(s); print(f"{s!r:14} -> {e.tokens} offsets={e.offsets}")
PY
.venv-oracles/bin/python /tmp/probe_tie.py
```

Record the output in your report. Implement whichever rule it shows; if the output is ambiguous, implement "compare on the raw match position, expand afterwards" and say in the code comment that the tie case is unmeasured.

- [ ] **Step 2: Write the failing tests**

`tests/DataNet.Embeddings.Tests/Tokenization/AddedTokenScannerTests.cs`. Every expected value comes from the spec's measured table. Match the repository's test-naming style — sentence case with underscores, as in `All_ratios_match_rapidfuzz`.

```csharp
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Tokenization;

public sealed class AddedTokenScannerTests
{
    private static AddedTokenScanner Scanner(params AddedToken[] tokens) => new(tokens);

    private static (int Start, int End, string Content) Next(AddedTokenScanner scanner, string text, int from = 0)
    {
        Assert.True(scanner.TryNext(text, from, out int start, out int end, out AddedToken token));
        return (start, end, token.Content);
    }

    [Fact]
    public void Plain_token_consumes_exactly_its_own_span()
    {
        var scanner = Scanner(new AddedToken("<mask>", 7));
        Assert.Equal((2, 8, "<mask>"), Next(scanner, "a <mask> b"));
    }

    [Fact]
    public void Lstrip_absorbs_every_contiguous_whitespace_character_on_the_left()
    {
        var scanner = Scanner(new AddedToken("<mask>", 7) { Lstrip = true });
        Assert.Equal((1, 9, "<mask>"), Next(scanner, "a  <mask>  b"));
    }

    [Fact]
    public void Lstrip_absorbs_tab_newline_and_no_break_space()
    {
        var scanner = Scanner(new AddedToken("<mask>", 7) { Lstrip = true });
        Assert.Equal(1, Next(scanner, "a\t<mask>").Start);
        Assert.Equal(1, Next(scanner, "a\n<mask>").Start);
        Assert.Equal(1, Next(scanner, "a <mask>").Start);
    }

    [Fact]
    public void Lstrip_stops_at_a_non_whitespace_character()
    {
        var scanner = Scanner(new AddedToken("<mask>", 7) { Lstrip = true });
        Assert.Equal(2, Next(scanner, "a. <mask>").Start);
    }

    [Fact]
    public void Lstrip_never_reaches_behind_the_scan_start()
    {
        var scanner = Scanner(new AddedToken("<mask>", 7) { Lstrip = true });
        Assert.Equal(2, Next(scanner, "a <mask>", from: 2).Start);
    }

    [Fact]
    public void Rstrip_absorbs_every_contiguous_whitespace_character_on_the_right()
    {
        var scanner = Scanner(new AddedToken("<mask>", 7) { Rstrip = true });
        Assert.Equal((3, 11, "<mask>"), Next(scanner, "a  <mask>  b"));
    }

    [Fact]
    public void Single_word_matches_only_between_non_word_characters()
    {
        var scanner = Scanner(new AddedToken("<m>", 7) { SingleWord = true });
        Assert.True(scanner.TryNext("a <m> b", 0, out _, out _, out _));
        Assert.True(scanner.TryNext(".<m>.", 0, out _, out _, out _));
        Assert.True(scanner.TryNext("-<m>-", 0, out _, out _, out _));
        Assert.True(scanner.TryNext("<m>", 0, out _, out _, out _));
        Assert.False(scanner.TryNext("a<m>a", 0, out _, out _, out _));
        Assert.False(scanner.TryNext("1<m>1", 0, out _, out _, out _));
        Assert.False(scanner.TryNext("_<m>_", 0, out _, out _, out _));
        Assert.False(scanner.TryNext("é<m>é", 0, out _, out _, out _));
        Assert.False(scanner.TryNext("<m>b", 0, out _, out _, out _));
    }

    [Fact]
    public void Single_word_keeps_searching_past_a_rejected_position()
    {
        var scanner = Scanner(new AddedToken("<m>", 7) { SingleWord = true });
        Assert.Equal(4, Next(scanner, "a<m> <m> b").Start);
    }

    [Fact]
    public void Leftmost_wins_then_longest()
    {
        var scanner = Scanner(new AddedToken("<a>", 1), new AddedToken("<a><b>", 2));
        Assert.Equal("<a><b>", Next(scanner, "x <a><b> y").Content);
    }

    [Fact]
    public void An_empty_content_is_never_matched()
    {
        var scanner = Scanner(new AddedToken(string.Empty, 9), new AddedToken("<m>", 7));
        Assert.Equal("<m>", Next(scanner, "a <m>").Content);
    }

    [Fact]
    public void No_match_is_reported_when_none_remains()
    {
        var scanner = Scanner(new AddedToken("<m>", 7));
        Assert.False(scanner.TryNext("nothing here", 0, out _, out _, out _));
    }
}
```

- [ ] **Step 3: Run them and watch them fail for the right reason**

```bash
dotnet test tests/DataNet.Embeddings.Tests -c Release --filter "FullyQualifiedName~AddedTokenScannerTests" 2>&1 | tail -12
```

Expected: a compile error naming `AddedToken` / `AddedTokenScanner`. That is the correct RED — the types do not exist. **If it reports `Passed: 0` with no error, the filter matched nothing**; fix the filter before continuing.

- [ ] **Step 4: Write `AddedToken.cs`**

```csharp
namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// One <c>added_tokens</c> entry: text matched ahead of the model, and the rules
/// that decide where it matches.
/// </summary>
/// <remarks>
/// The flags are HuggingFace's, reproduced as measured against
/// <c>tokenizers</c> 0.23.1 — see
/// <c>docs/decisions/0022-added-token-matching-flags.md</c>. All four default to
/// <see langword="false"/>, which is the plain literal match this library did
/// before they existed.
/// </remarks>
/// <param name="Content">The text matched, exactly and ordinally.</param>
/// <param name="Id">The id the match produces.</param>
public sealed record AddedToken(string Content, int Id)
{
    /// <summary>Absorbs the whitespace immediately to the left of a match into it.</summary>
    /// <remarks>
    /// All of it, not one character. The id is unchanged; what disappears is the
    /// piece that whitespace would otherwise have produced — a <c>Ġ</c> on a
    /// byte-level model. <c>roberta-base</c> sets this on <c>&lt;mask&gt;</c>.
    /// </remarks>
    public bool Lstrip { get; init; }

    /// <summary>The mirror of <see cref="Lstrip"/>, on the right.</summary>
    public bool Rstrip { get; init; }

    /// <summary>Matches only where both neighbours are non-word characters or the ends of the text.</summary>
    /// <remarks>
    /// A word character is a letter, a digit or <c>_</c>, Unicode-aware:
    /// <c>a</c>, <c>1</c>, <c>_</c> and <c>é</c> all block a match, while
    /// <c>.</c>, <c>-</c> and whitespace do not.
    /// </remarks>
    public bool SingleWord { get; init; }

    /// <summary>Whether the file marked this entry <c>special</c>.</summary>
    /// <remarks>
    /// Two consequences, both measured: a special entry is exempt from the
    /// model's normalizer, where an ordinary one is normalized along with the
    /// text; and it is the one a decoder drops for <c>skip_special_tokens</c>.
    /// </remarks>
    public bool Special { get; init; }
}
```

- [ ] **Step 5: Write `AddedTokenScanner.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// Finds the next <see cref="AddedToken"/> in a string. The one place either
/// tokenizer asks that question, so the two cannot answer it differently.
/// </summary>
internal sealed class AddedTokenScanner
{
    private readonly AddedToken[] _tokens;

    /// <summary>Keeps the entries that can match; the order does not matter.</summary>
    /// <remarks>
    /// An empty <see cref="AddedToken.Content"/> is dropped: it would match at
    /// every position without advancing the caller's scan, hanging the loop. The
    /// loader bounds a token's upper length but never rejects an empty one, so
    /// this cannot be assumed away.
    /// </remarks>
    internal AddedTokenScanner(IReadOnlyList<AddedToken> tokens) =>
        _tokens = [.. tokens.Where(t => t.Content.Length > 0)];

    /// <summary>Whether any entry can ever match.</summary>
    internal bool IsEmpty => _tokens.Length == 0;

    /// <summary>
    /// The earliest match at or after <paramref name="from"/> — the longest one,
    /// on a tie — with the span it consumes once stripping is applied.
    /// </summary>
    /// <param name="text">The text being scanned.</param>
    /// <param name="from">Where to start; a strip never reaches behind it.</param>
    /// <param name="start">The first index the match consumes.</param>
    /// <param name="end">One past the last index the match consumes.</param>
    /// <param name="token">The entry that matched.</param>
    internal bool TryNext(string text, int from, out int start, out int end, out AddedToken token)
    {
        int bestAt = -1;
        AddedToken? best = null;

        foreach (AddedToken candidate in _tokens)
        {
            // Once a candidate is found, only a match starting at or before it can
            // still win, so later entries need a window reaching bestAt plus their
            // own length -- just enough to still find a match starting exactly at
            // bestAt. Llama-3 alone declares 256 added tokens; without this bound
            // every one of them would rescan to the end of the remaining text on
            // every match found.
            int windowEnd = bestAt < 0 ? text.Length : Math.Min(text.Length, bestAt + candidate.Content.Length);
            int at = FirstMatch(text, candidate, from, windowEnd);
            if (at < 0)
            {
                continue;
            }
            if (bestAt < 0 || at < bestAt || (at == bestAt && candidate.Content.Length > best!.Content.Length))
            {
                bestAt = at;
                best = candidate;
            }
        }

        if (best is null)
        {
            start = -1;
            end = -1;
            token = null!;
            return false;
        }

        // The comparison above runs on the raw match position; stripping expands the
        // span only once the winner is known.
        start = bestAt;
        end = bestAt + best.Content.Length;
        if (best.Lstrip)
        {
            while (start > from && char.IsWhiteSpace(text[start - 1]))
            {
                start--;
            }
        }
        if (best.Rstrip)
        {
            while (end < text.Length && char.IsWhiteSpace(text[end]))
            {
                end++;
            }
        }
        token = best;
        return true;
    }

    /// <summary>The first index at or after <paramref name="from"/> where the entry may match.</summary>
    /// <remarks>
    /// A <see cref="AddedToken.SingleWord"/> entry rejected at one position can
    /// still match at a later one, so the search continues past a rejection
    /// rather than giving up.
    /// </remarks>
    private static int FirstMatch(string text, AddedToken candidate, int from, int windowEnd)
    {
        int at = from;
        while (at <= windowEnd - candidate.Content.Length)
        {
            int found = text.IndexOf(candidate.Content, at, windowEnd - at, StringComparison.Ordinal);
            if (found < 0)
            {
                return -1;
            }
            if (!candidate.SingleWord || IsWholeWord(text, found, found + candidate.Content.Length))
            {
                return found;
            }
            at = found + 1;
        }
        return -1;
    }

    private static bool IsWholeWord(string text, int start, int end) =>
        (start == 0 || !IsWordCharacter(text[start - 1]))
        && (end == text.Length || !IsWordCharacter(text[end]));

    private static bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';
}
```

- [ ] **Step 6: Run the tests until they pass**

```bash
dotnet test tests/DataNet.Embeddings.Tests -c Release --filter "FullyQualifiedName~AddedTokenScannerTests" 2>&1 | tail -8
```

Expected: `Passed: 12, Failed: 0`. Read the number. If Step 1's measurement contradicts a test's expectation, **change the test to what was measured** and say so in your report — the oracle wins.

- [ ] **Step 7: Whole solution green, analysers on**

```bash
dotnet build DataNet.slnx -c Release --no-incremental 2>&1 | tail -4
dotnet test DataNet.slnx -c Release 2>&1 | tail -8
```

Expected: 0 warnings; 1615 + 12 = 1627 passing (the mirror project links the same file, so if it reports 1639 that is the two mirrors and is also correct — read and report the number rather than assuming).

- [ ] **Step 8: Commit**

```bash
git add src/DataNet.Embeddings/Tokenization/AddedToken.cs \
        src/DataNet.Embeddings/Tokenization/AddedTokenScanner.cs \
        tests/DataNet.Embeddings.Tests/Tokenization/AddedTokenScannerTests.cs
git commit -m "Give added-token matching one home, with the flags measured"
```

---

### Task 2: Migrate `BpeVocabulary` and `BpeTokenizer` onto the scanner

**Files:**

- Modify: `src/DataNet.Embeddings/Tokenization/BpeVocabulary.cs`
- Modify: `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs`
- Modify: `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs`
- Modify: `tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs` (6 sites), `tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs` (2 sites)

**Depends on:** Task 1.
**Produces:** `BpeVocabulary.AddedTokens` is `IReadOnlyList<AddedToken>`; `BpeTokenizer` no longer has `NextAddedToken`.

**This task changes no behaviour.** The loader still sets every flag to `false` and still refuses the three it refuses today, so every existing oracle must stay green. It is a type migration and nothing more — which is exactly why it is its own commit.

- [ ] **Step 1: Change the property and its equality**

In `BpeVocabulary.cs`, replace the `AddedTokens` property (currently `IReadOnlyDictionary<string, int>`) with:

```csharp
    public IReadOnlyList<AddedToken> AddedTokens { get; init; } = [];
```

Keep the existing remarks, but delete the second paragraph's claim that the `special` flag "is not carried here" — Task 3 makes it false. Replace it with a sentence saying `AddedToken.Special` carries it.

In `Equals`, replace `AddedTokens.Count != other.AddedTokens.Count` with the same count check, and replace the `SameEntries(AddedTokens, other.AddedTokens)` call with an ordered element comparison:

```csharp
        for (int i = 0; i < AddedTokens.Count; i++)
        {
            if (!AddedTokens[i].Equals(other.AddedTokens[i]))
            {
                return false;
            }
        }
```

`GetHashCode` already hashes `AddedTokens.Count` only; leave it.

- [ ] **Step 2: Rewire `BpeTokenizer`**

In the constructor, replace the two `foreach` blocks over `vocabulary.AddedTokens` and the `_addedTokens`/`_addedIds` initialisation:

```csharp
        foreach (AddedToken added in vocabulary.AddedTokens)
        {
            _vocab[added.Content] = added.Id;
            maxId = Math.Max(maxId, added.Id);
        }
```

and, after `_tokens` is built:

```csharp
        _scanner = new AddedTokenScanner(vocabulary.AddedTokens);
        _addedIds = [.. vocabulary.AddedTokens.Select(a => a.Id)];
```

Declare `private readonly AddedTokenScanner _scanner;` and drop the `_addedTokens` field with its comment (the comment's content now lives on the scanner).

Delete `NextAddedToken` entirely and rewrite the `Encode` loop to use the scanner's span:

```csharp
        int pos = 0;
        while (pos < text.Length)
        {
            if (!_scanner.TryNext(text, pos, out int start, out int end, out AddedToken added))
            {
                EncodeSegment(text, pos, text.Length, tokens, ids, pieces);
                break;
            }
            if (start > pos)
            {
                EncodeSegment(text, pos, start, tokens, ids, pieces);
            }
            tokens.Add(text[start..end]);
            ids.Add(added.Id);
            pos = end;
        }
```

`text[start..end]` is the emitted surface, which carries any absorbed whitespace — `' <mask>'` — as HuggingFace's does. With every flag `false` it equals `added.Content`, so no existing corpus moves. **`netstandard2.0` has no range indexer**; use `text.Substring(start, end - start)`, which compiles on both targets.

- [ ] **Step 3: Update the loader's construction site**

`ReadBpeAddedTokens` returns `Dictionary<string, int>` today and its result is assigned to `AddedTokens`. Change its return type to `List<AddedToken>`, have `ReadAddedTokens` append `new AddedToken(content, id)` to it (flags still default), and keep every existing check — the negative-id refusal, the id-conflict refusal, and the three `EnsureAddedTokenFlagIsOff` calls, which Task 4 removes.

- [ ] **Step 4: Update the eight test sites**

Each currently builds `AddedTokens = new Dictionary<string, int>(StringComparer.Ordinal) { ["<x>"] = 1 }`. Replace with `AddedTokens = [new AddedToken("<x>", 1)]`. The empty-content site becomes `AddedTokens = [new AddedToken(string.Empty, 999)]` and must keep asserting what it asserted.

- [ ] **Step 5: Everything green, nothing moved**

```bash
dotnet build DataNet.slnx -c Release --no-incremental 2>&1 | tail -4
dotnet test DataNet.slnx -c Release 2>&1 | tail -8
```

Expected: 0 warnings, and the **same count as Task 1 left**. Any oracle failure here means the migration changed behaviour — find it rather than adjusting the oracle.

- [ ] **Step 6: Commit**

```bash
git add src/DataNet.Embeddings tests/DataNet.Embeddings.Tests
git commit -m "Describe an added token as a value, not a dictionary entry"
```

---

### Task 3: Carry `special`, and drop only specials on decode

**Files:**

- Modify: `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs`
- Modify: `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs` (the `_addedIds` build and the `skipSpecialTokens` doc)
- Modify: `docs/equivalence.md` (the `BpeTokenizer.Decode` row, ~line 94)
- Modify: `tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs`

**Depends on:** Task 2.

**Why the documentation moves with the code here.** Task 2 already rewrote
`BpeVocabulary.AddedTokens`'s remark to say `special` is carried on
`AddedToken.Special`. That claim is **false until this task lands**, and it
contradicts two places that are still telling the truth:
`BpeTokenizer.Decode`'s `skipSpecialTokens` parameter doc and
`docs/equivalence.md`'s `Decode` row. All three have to become true in this
commit — the `equivalence.md` row was originally scheduled for Task 8 and is
pulled forward for exactly that reason. Leaving it behind would ship a public
API doc contradicting the guide it points at.

- [ ] **Step 1: Write the failing test**

Add to `BpeTokenizerTests.cs`, in the file's own style:

```csharp
    [Fact]
    public void Decode_skipping_specials_keeps_an_ordinary_added_token()
    {
        var vocabulary = new BpeVocabulary(
            new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 0, ["<s>"] = 1, ["<x>"] = 2 },
            [])
        {
            AddedTokens =
            [
                new AddedToken("<s>", 1) { Special = true },
                new AddedToken("<x>", 2),
            ],
        };
        var tokenizer = new BpeTokenizer(vocabulary);

        Assert.Equal("<x>", tokenizer.Decode([1, 2], skipSpecialTokens: true));
        Assert.Equal("<s><x>", tokenizer.Decode([1, 2], skipSpecialTokens: false));
    }
```

- [ ] **Step 2: Run it and watch it fail**

```bash
dotnet test tests/DataNet.Embeddings.Tests -c Release --filter "FullyQualifiedName~Decode_skipping_specials" 2>&1 | tail -10
```

Expected: FAIL — `Decode([1,2], true)` returns `""` today, because `_addedIds` holds every added id. Read the actual-vs-expected line; a `Passed: 0` means the filter matched nothing.

- [ ] **Step 3: Read `special` in the loader**

In `ReadAddedTokens`, build `new AddedToken(content, id) { Special = OptionalBoolean(token, "special") is true }`.

- [ ] **Step 4: Narrow `_addedIds`**

```csharp
        _addedIds = [.. vocabulary.AddedTokens.Where(a => a.Special).Select(a => a.Id)];
```

Rewrite the `skipSpecialTokens` parameter documentation on `Decode`: it now drops exactly what Python's `skip_special_tokens` drops. Delete the sentence about `BpeVocabulary.AddedTokens` not carrying the flag and the "two coincide for every model in scope" caveat — both become false here.

- [ ] **Step 5: Green**

```bash
dotnet test tests/DataNet.Embeddings.Tests -c Release --filter "FullyQualifiedName~Decode_skipping_specials" 2>&1 | tail -6
dotnet test DataNet.slnx -c Release 2>&1 | tail -8
```

Expected: the focused test passes; the whole suite passes. The `bpe_added_tokens.json` corpus carries `decoded_skip_specials` for a table whose only added token **is** special, so it must not move.

- [ ] **Step 6: Commit**

```bash
git add src/DataNet.Embeddings tests/DataNet.Embeddings.Tests
git commit -m "Drop on decode what Python drops, not every added token"
```

---

### Task 4: Read the three flags, and stop refusing them on the BPE path

**Files:**

- Modify: `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs`
- Modify: `docs/equivalence.md` (the `LoadBpe` row, ~line 111)
- Modify: `tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs`

**Depends on:** Task 3.

**The guide row moves with the code, for the reason Task 3 established.**
`docs/equivalence.md`'s `LoadBpe` row lists `lstrip`, `rstrip` and `single_word`
among what the loader refuses. This task stops refusing them on the BPE path, so
that row becomes false in this commit and must be corrected in it. Drop only the
added-token clause — the row's other refusals are model settings that #105 owns
and that this branch does not touch.

`ReadAddedTokens` is shared by `ReadWordPiece` and `ReadBpeAddedTokens`. Only the BPE caller stops refusing here; WordPiece keeps refusing until Task 5 gives it a scanner. Thread that through with the parameter that already distinguishes them — `matchedLiterally` is non-null only on the BPE path — or add an explicit `bool reproducesFlags`. Prefer the explicit parameter: the existing one distinguishes them by accident.

- [ ] **Step 1: Write the failing test**

```csharp
    [Fact]
    public void LoadBpe_reads_the_added_token_matching_flags()
    {
        string path = WriteTempTokenizerJson("""
        {
          "added_tokens": [
            { "id": 2, "content": "<mask>", "lstrip": true, "rstrip": false, "single_word": false, "special": true }
          ],
          "pre_tokenizer": { "type": "ByteLevel", "add_prefix_space": false },
          "model": { "type": "BPE", "vocab": { "a": 0, "b": 1, "<mask>": 2 }, "merges": [] }
        }
        """);

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(path);

        AddedToken mask = Assert.Single(vocabulary.AddedTokens, t => t.Content == "<mask>");
        Assert.True(mask.Lstrip);
        Assert.False(mask.Rstrip);
        Assert.False(mask.SingleWord);
        Assert.True(mask.Special);
    }
```

Use the file's existing helper for writing a temporary `tokenizer.json`; if it is named differently, use that name and say so in your report.

- [ ] **Step 2: Run it and watch it fail**

Expected: `InvalidDataException`, with the message ``it adds token '<mask>' with lstrip on``. That refusal is the thing being removed.

- [ ] **Step 3: Read the flags and lift the BPE refusal**

Populate `Lstrip`, `Rstrip`, `SingleWord` from `OptionalBoolean` alongside `Special`, and call `EnsureAddedTokenMatchesPlainly` only when the caller does **not** reproduce them. Keep the WordPiece message as it is.

- [ ] **Step 4: Prove the WordPiece refusal still stands**

Add a test asserting `LoadWordPiece` on the same file still throws, naming `lstrip`. Without it, Task 5 could silently remove the guard.

- [ ] **Step 5: Green, and the flags reach the tokenizer**

```bash
dotnet test DataNet.slnx -c Release 2>&1 | tail -8
```

- [ ] **Step 6: Commit**

```bash
git add src/DataNet.Embeddings tests/DataNet.Embeddings.Tests
git commit -m "Read the added-token flags a BPE file declares"
```

---

### Task 5: WordPiece gains an added-token scan

**Files:**

- Modify: `src/DataNet.Embeddings/Tokenization/WordPieceVocabulary.cs`
- Modify: `src/DataNet.Embeddings/Tokenization/WordPieceTokenizer.cs`
- Modify: `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs`
- Modify: `tests/DataNet.Embeddings.Tests/WordPieceTokenizerTests.cs`

**Depends on:** Task 4. **This is the task that changes existing behaviour** — see Step 5.

- [ ] **Step 1: Add `AddedTokens` to the vocabulary**

```csharp
    /// <summary>The <c>added_tokens</c> table, matched as literal text ahead of the model.</summary>
    /// <remarks>
    /// Not folded into <see cref="Vocab"/>: a folded entry is matchable as a whole
    /// word only, which is a different tokenizer as soon as an entry carries a
    /// matching flag. See <c>docs/decisions/0022-added-token-matching-flags.md</c>.
    /// </remarks>
    public IReadOnlyList<AddedToken> AddedTokens { get; init; } = [];
```

Extend `Equals` with the count check and the ordered element comparison, exactly as Task 2 did for `BpeVocabulary`, and add `AddedTokens.Count` to `GetHashCode`.

- [ ] **Step 2: Write the failing test**

```csharp
    [Fact]
    public void A_special_added_token_survives_lowercasing_and_an_ordinary_one_does_not()
    {
        var vocabulary = new WordPieceVocabulary(
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["[UNK]"] = 0, ["a"] = 1, ["b"] = 2, ["[CLS]"] = 3, ["[cls]"] = 4,
            },
            "[UNK]",
            "##",
            Lowercase: true)
        {
            AddedTokens = [new AddedToken("[CLS]", 3) { Special = true }],
        };
        var tokenizer = new WordPieceTokenizer(vocabulary);

        Assert.Equal(["a", "[CLS]", "b"], tokenizer.Encode("a [CLS] b").Tokens);
        Assert.Equal(["a", "[UNK]", "b"], tokenizer.Encode("a [cls] b").Tokens);
    }
```

The second assertion is the measured behaviour: a special entry is matched against the **raw** text, so lowercase input does not reach it and falls through to the model. Confirm the fall-through token against the oracle in Task 6 and correct the expectation if it differs.

- [ ] **Step 3: Run it and watch it fail**

Expected: compile error on `AddedTokens` if Step 1 was skipped, otherwise a failure showing `[CLS]` lowercased away.

- [ ] **Step 4: Scan before normalizing**

Give `WordPieceTokenizer` two scanners — one over the `Special` entries, matched against the raw text, one over the ordinary entries, matched against the lowercased text — or one scanner and two passes. Rewrite `Encode`:

```csharp
    public TokenizationResult Encode(string text)
    {
        Guard.NotNull(text);
        var tokens = new List<string>();
        var ids = new List<int>();

        int pos = 0;
        while (pos < text.Length)
        {
            if (!TryNextAddedToken(text, pos, out int start, out int end, out AddedToken added))
            {
                EncodeSegment(text, pos, text.Length, tokens, ids);
                break;
            }
            if (start > pos)
            {
                EncodeSegment(text, pos, start, tokens, ids);
            }
            tokens.Add(text.Substring(start, end - start));
            ids.Add(added.Id);
            pos = end;
        }
        return new TokenizationResult(tokens, ids);
    }
```

`EncodeSegment` is today's body: lowercase the slice when `_lowercase`, run `PreTokenPattern` over it, call `TokenizeWord` per match. `TryNextAddedToken` runs the special scanner over `text` and the ordinary scanner over the lowercased `text`, and returns whichever matches leftmost — the ordinary scanner's indices are valid on the original string because `ToLowerInvariant` is length-preserving for the scripts in scope. **Say that assumption in a comment**; it is not true of every culture-sensitive mapping, which is why `ToLowerInvariant` and not `ToLower` is the one used.

- [ ] **Step 5: Stop folding, and read the diff that follows**

In `ReadAddedTokens`, the WordPiece caller now collects `AddedToken` values instead of writing into `vocab`. Keep the id-conflict and negative-id refusals.

```bash
dotnet test DataNet.slnx -c Release 2>&1 | tail -12
```

**Expect failures in the WordPiece oracle**, and do not adjust the oracle to match the code. Record which cases moved and how, in your report.

- [ ] **Step 5b: Regenerate the WordPiece corpus, and read its diff**

The regenerated file — not the code — is the arbiter. This step is inside this task, not the next one, so that the commit carries the behaviour change and the corpus that proves it together and no commit leaves the suite red.

```bash
.venv-oracles/bin/python tools/generate_oracles.py
git diff --stat tests/oracles/
git diff tests/oracles/tokenizer_json.json | head -80
dotnet test DataNet.slnx -c Release 2>&1 | tail -8
```

**The `tokenizer_json.json` diff is the evidence for this task's behaviour change.** Read it and describe in your report what moved and why — an added token that was folded is now matched as text. A diff of zero there would mean the fixture has no `added_tokens` after all, which contradicts the survey; say so if you see it. If a case still disagrees after regenerating, the code is wrong, not the corpus.

The suite must be green at the end of this step.

- [ ] **Step 6: Commit**

```bash
git add src/DataNet.Embeddings tests/DataNet.Embeddings.Tests
git commit -m "Match a WordPiece added token as text, not as a folded vocabulary entry"
```

---

### Task 6: Regenerate the oracles

**Files:**

- Modify: `tools/generate_oracles.py`
- Modify: `tests/oracles/tokenizer_json.json`, `tests/oracles/bpe_added_tokens.json`, and any new corpus
- Modify: the test classes that replay them

**Depends on:** Task 5.

- [ ] **Step 1: Add a flags corpus for BPE**

Follow `generate_bpe_added_tokens`'s shape exactly — it already carries `tokenizer.to_str()` in the metadata so the C# side parses the same bytes HuggingFace was handed. Add `generate_bpe_added_token_flags`, registered in `main`'s `generators` dict as `bpe_added_token_flags.json`, over a GPT-2 tokenizer with three added tokens: one `lstrip=True`, one `rstrip=True`, one `single_word=True`. Texts must include the measured edge cases: `"a <mask> b"`, `"a<mask>b"`, `"a  <mask>  b"`, `"<mask> a"`, `"a <mask>"`, `"a\t<mask>"`, `"a <mask>"`, `"a. <mask>"`, `".<m>."`, `"-<m>-"`, `"1<m>1"`, `"_<m>_"`, `"é<m>é"`.

Record `tokens`, `ids`, `decoded` and `decoded_skip_specials` per case, as the sibling generator does.

- [ ] **Step 2: Add the WordPiece equivalent — this one is load-bearing**

`generate_wordpiece_added_tokens`, with a `Lowercase` normalizer. **No committed WordPiece corpus carries an
`added_tokens` table at all** — `_wordpiece_tokenizer` never adds a token, so the fixture the plan assumed
would move under Task 5 does not exist. That makes this corpus the only replayed evidence for everything
Task 5 built, and it must cover four cases, not one:

1. An entry that runs in the **raw** pass (`normalized: false`) — matched against the un-lowercased text,
   emitting its own casing.
2. An entry that runs in the **normalized** pass (`normalized: true`) — matched against lowercased text,
   emitting the lowercased form.
3. **`normalized` disagreeing with `!special`** — an entry with `special: true, normalized: true`. This is
   the case that proves the discriminator is `normalized` and not `special`, and it is the one the natural
   implementation gets wrong.
4. **Outer-pass precedence** — a raw entry and a normalized entry that overlap, where the normalized one
   starts further left. The raw pass runs first and wins, which a single merged leftmost-wins scan would
   not reproduce.

Plus the `lstrip`/`rstrip`/`single_word` edge cases from Step 1, on this tokenizer too.

Note when writing the generator: `tokenizers` **refuses** a `tokenizer.json` that omits `normalized`, so
every entry it emits states the field explicitly. That is why the loader's default is a deliberate choice
rather than a measured behaviour, and the corpus cannot exercise the absent-field path — a C# unit test
covers that instead.

- [ ] **Step 3: Regenerate and read the diff**

```bash
.venv-oracles/bin/python tools/generate_oracles.py
git diff --stat tests/oracles/
```

Task 5 already regenerated and committed `tokenizer_json.json`, so the only new files here should be the two flag corpora. **If `tokenizer_json.json` moves again, something in Task 5 was not deterministic** — stop and report it rather than committing the second diff.

- [ ] **Step 4: Replay the new corpora**

Add the test classes that read the two new files, following `ByteLevelBpeTests`'s existing pattern for loading a corpus and asserting per case.

- [ ] **Step 5: Green**

```bash
dotnet test DataNet.slnx -c Release 2>&1 | tail -8
```

- [ ] **Step 6: Commit**

```bash
git add tools/generate_oracles.py tests/oracles tests/DataNet.Embeddings.Tests
git commit -m "Replay the four added-token flags against tokenizers 0.23.1"
```

---

### Task 7: The RoBERTa acceptance test

**Files:**

- Modify: `tools/generate_oracles.py` or a new fixture under `tests/oracles/`
- Modify: `tests/DataNet.Embeddings.Tests/Persistence/TokenizerJsonLoaderTests.cs`

**Depends on:** Task 6. This is the issue's own acceptance criterion.

- [ ] **Step 1: Build a RoBERTa-shaped `added_tokens` table**

The issue records `roberta-base`'s five entries verbatim: ids 0-3 `<s>`, `<pad>`, `</s>`, `<unk>` with all three flags `false` and `special=true`, and id 50264 `<mask>` with `lstrip=True`, `rstrip=False`, `single_word=False`, `special=True`. All five are also in `model.vocab` at the same ids.

**Do not fetch from the Hub in a test.** Build a small `tokenizer.json` with that exact table over a tiny byte-level vocabulary, in the style of `build_tiny_models.py`, and commit it.

- [ ] **Step 2: Write the acceptance test**

```csharp
    [Fact]
    public void LoadBpe_accepts_the_roberta_added_token_table()
    {
        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(RobertaShapedFixturePath);

        Assert.Equal(5, vocabulary.AddedTokens.Count);
        AddedToken mask = Assert.Single(vocabulary.AddedTokens, t => t.Content == "<mask>");
        Assert.True(mask.Lstrip);
        Assert.True(mask.Special);
        Assert.All(vocabulary.AddedTokens.Where(t => t.Content != "<mask>"), t => Assert.False(t.Lstrip));
    }
```

- [ ] **Step 3: Green, and commit**

```bash
dotnet test DataNet.slnx -c Release 2>&1 | tail -8
git add tests
git commit -m "Load the added-token table roberta-base actually ships"
```

---

### Task 8: ADR 0022 and the equivalence table

**Files:**

- Create: `docs/decisions/0022-added-token-matching-flags.md`
- Modify: `docs/equivalence.md`

**Depends on:** Task 7.

- [ ] **Step 1: Confirm the number is still free**

```bash
ls docs/decisions/
```

The user reserved **0022** because 0020 and 0021 are taken by work in flight. If 0022 has appeared, stop and ask rather than picking another.

- [ ] **Step 2: Write the ADR**

Follow `0017-bpe-parity-scope.md`'s shape: `# 0022 — …`, `**Status:** accepted · **Date:** 2026-08-10`, Context / Decision / Consequences. It must contain:

- The measured semantics of all four flags, as the spec's tables — including that `lstrip` absorbs *all* contiguous whitespace, that `\t`/`\n`/U+00A0 count and `.` does not, and that `single_word`'s word class is letter/digit/underscore, Unicode-aware.
- **That the id stream changes only by losing the piece the whitespace would have produced** — the token id itself is unchanged.
- **The normalization rule**: ordinary added tokens are normalized and matched against normalized text; special ones are exempt and matched against raw text. State plainly that "added tokens are matched before normalization" is the wrong summary, since that is the natural guess.
- **The round-trip loss**: `'a <mask> b'` decodes to `'a<mask> b'` under `lstrip`, in HuggingFace too. Parity, not defect.
- **That WordPiece's behaviour changed for every file with `added_tokens`**, flags or not, and point at the regenerated `tokenizer_json.json` diff as the evidence.
- **What #105 inherits**: the scan-versus-normalization order settled here, and the measurement of what `lstrip` does to a segment boundary — with the per-segment `add_prefix_space` rule left untouched for #105 to change.
- **The weaker footing**: `rstrip` and `single_word` have no carrier in any corpus this repository holds and are proven against fixtures built for the purpose, where `lstrip` has `roberta-base`.

- [ ] **Step 3: Update `docs/equivalence.md`**

Two rows move here; the third already moved.

- The `BpeTokenizer.Decode` row (line ~94) **was corrected in Task 3**, which had to land it with the loader change to stop a public API doc contradicting the guide. Do not redo it — read it and confirm it still says what Task 3 made it say.
- The `LoadBpe` row (line ~111) **was corrected in Task 4**, for the same reason: it listed the refusals that task lifts. Read it and confirm; do not redo it.
- Add the `lstrip` round-trip divergence to the `Decode` row: the absorbed whitespace is not restored, as in Python. **This one is genuinely new here** — no earlier task states it, and it is the divergence the spec insists must be recorded rather than discovered.
- Add the WordPiece rows this branch changed: it now matches added tokens as text instead of folding them, and honours the four flags.

- [ ] **Step 3b: The CHANGELOG**

`BpeVocabulary.AddedTokens` changed type, and `WordPieceVocabulary` gained a
property — a breaking change to public API, on a version that has not shipped.
No earlier task touches `CHANGELOG.md`, which has an `[Unreleased]` section
(~line 14). Add an entry there naming both vocabulary changes, the four flags,
and the WordPiece behaviour change for files carrying `added_tokens`.

Follow the file's existing sectioning and voice; read the entry #59 wrote for
the BPE loaders as the model.

- [ ] **Step 4: Re-read what this change falsified**

```bash
grep -rn "added_tokens\|skipSpecialTokens\|single_word\|lstrip" --include=*.md docs README.md CONTRIBUTING.md | grep -v "^docs/decisions/0022"
```

Counts, enumerations and "see X" pointers go stale silently. Check the BPE guide as well as `equivalence.md`.

- [ ] **Step 5: Verify the guides still compile**

```bash
SCRATCH=/tmp/claude-49201103/-home-cyril-Documents-devs-data-net/c134d377-25c6-4da3-8dec-8ffcbffa021b/scratchpad
rm -rf "$SCRATCH/pack-packages" ./artifacts
NUGET_PACKAGES="$SCRATCH/pack-packages" bash -c '
  for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do
    dotnet pack "$p" -c Release -o ./artifacts || exit 1; done'
python3 tools/extract_doc_snippets.py
rm -rf "$SCRATCH/sample-packages"
NUGET_PACKAGES="$SCRATCH/sample-packages" dotnet build samples/DataNet.DocSnippets -c Release --no-incremental 2>&1 | tail -3
```

- [ ] **Step 6: Commit**

```bash
git add docs
git commit -m "Record how an added token matches, and what it costs the round trip"
```

---

### Task 9: Final verification

**Depends on:** Task 8. Nothing is committed here unless a check fails and is fixed.

- [ ] **Step 1: Run every gate, reading real exit codes**

```bash
cd /home/cyril/Documents/devs/data.net
SCRATCH=/tmp/claude-49201103/-home-cyril-Documents-devs-data-net/c134d377-25c6-4da3-8dec-8ffcbffa021b/scratchpad

git status --porcelain                                                   # empty
dotnet build DataNet.slnx -c Release --no-incremental > "$SCRATCH/b.log" 2>&1; echo "build=$?"; tail -3 "$SCRATCH/b.log"
dotnet format DataNet.slnx --verify-no-changes > "$SCRATCH/f.log" 2>&1;    echo "format=$?"
dotnet test DataNet.slnx -c Release > "$SCRATCH/t.log" 2>&1;               echo "test=$?"; tail -8 "$SCRATCH/t.log"
```

All three must be `0`, the build must show 0 warnings, and the test log's counts must be read — the four `*.NetStandard.Tests` mirrors are what prove the `netstandard2.0` assemblies still work.

- [ ] **Step 2: Confirm the oracle is reproducible**

```bash
.venv-oracles/bin/python tools/generate_oracles.py
git status --porcelain tests/oracles/
```

Expected: empty. The generator is deterministic, so regenerating what was just committed must produce no diff. A diff here means something non-deterministic reached a corpus.

- [ ] **Step 3: Rebase if `main` moved**

```bash
git fetch origin
git log --oneline HEAD..origin/main
```

Anything listed means rebase — a long review is exactly when `main` moves, and #105's sibling work touches the same three files.

- [ ] **Step 4: Stop and report**

Do not push and do not open a pull request. Report the state and let the user decide both.

---

## Self-review

**Spec coverage.** D1 → Tasks 1, 2, 5. D2 → Task 1. D3 → Task 3. D4 → Task 5. D5 → Tasks 2-5. D6 → Task 8. Verification section → Tasks 6, 7, 9. Both spec risks are addressed where they land: the WordPiece behaviour change at Task 5 Step 5 and Task 6 Step 3, the weak footing of `rstrip`/`single_word` at Task 8 Step 2.

**Ordering.** Task 2 is a type migration that must change five files at once to compile — it cannot be split, which is why it carries no behaviour change and its gate is "the counts did not move". Task 5 is the only task expected to break an oracle, and Task 6 is where the arbiter is regenerated.

**Type consistency.** `AddedToken(string Content, int Id)` with `Lstrip`/`Rstrip`/`SingleWord`/`Special`, and `AddedTokenScanner.TryNext(string, int, out int, out int, out AddedToken)`, are used with those exact names in Tasks 2, 3, 4, 5 and 7. `IReadOnlyList<AddedToken> AddedTokens` is the property name on both vocabularies.

**Known soft spots, called out where they occur.** Task 1 Step 1 measures a tie case the spec does not cover rather than assuming it. Task 5 Step 2's second assertion is a prediction the Task 6 oracle arbitrates. Task 4 Step 1 and Task 7 Step 1 depend on helper and fixture names that must be re-derived from the files rather than trusted from this plan.
