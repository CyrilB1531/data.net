# Lodestar.Text.Keywords Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship RAKE and TextRank in `Lodestar.Text.Keywords`, and MMR in `Lodestar.Embeddings.Search`, each replayed against a frozen corpus from its Python reference.

**Architecture:** Two extractors in a new namespace of `Lodestar.Text`, each an instance built from an options record and each returning `IReadOnlyList<KeywordMatch>` in descending score. They share a result type and a private phrase tokenizer, and nothing else — RAKE splits on stop words, TextRank ranks a word graph, and the two mechanisms are not derivable from one another. MMR is a static selector over vectors in `Lodestar.Embeddings.Search`, beside `VectorMath` and `EmbeddingIndex`, and knows nothing about text.

**Tech Stack:** C# on `net10.0;netstandard2.0`; xunit with a linked netstandard mirror per package; Python oracles `rake-nltk` 1.0.6, `summa` 1.2.0 and `keybert` 0.9.0, all MIT.

**Spec:** [`docs/superpowers/specs/2026-09-03_0525_lodestar-text-keywords-rake-textrank-and-mmr.md`](../specs/2026-09-03_0525_lodestar-text-keywords-rake-textrank-and-mmr.md)

**Branch:** `feat/525-lodestar-text-keywords`

## Global Constraints

- **Neither package's version moves.** `Lodestar.Text` stays `0.5.0` and `Lodestar.Embeddings` stays `0.6.0` — both are declared in `src/<Package>/Version.props` and not yet on nuget.org, so this work lands *in* those releases. Do not increment either.
- **Both target frameworks, always.** Every new file compiles on `net10.0` and `netstandard2.0`; `dotnet test Lodestar.slnx -c Release` runs each suite twice. **Read the test count, not the colour** — a `--filter` matching nothing exits zero.
- **No external dependency may be added to either package.** Both are core tier under ADR 0076, and `tools/check_nuspec_dependencies.py --require-all` fails a build that adds one.
- **A `docs/equivalence.md` row lands in the same commit as the function it describes**, never afterwards.
- **Every new public type needs a reference page** under `docs/reference/` once its namespace is in `docs/wiki-map.json`'s `covered` table, **and** a member reference from `samples/Lodestar.Sample`'s `Lot*.cs` for the packaging gate.
- **Comments: two lines inline, eight lines of prose in XML documentation.** `python3 tools/check_comment_length.py` counts them; `remarks` tags are **not** exempt and each costs a prose line. The `long-comment:` marker stays exceptional.
- **Oracle generators run from a neutral working directory** — not an ancestor of the checkout. Read the generator's own exit code, never a pipeline's.
- Everything written in English. Commit messages carry no `feat:`/`fix:` prefix.

## File Structure

| file | responsibility |
| --- | --- |
| `src/Lodestar.Text/Keywords/KeywordMatch.cs` | the ranked result pair, shared by both extractors |
| `src/Lodestar.Text/Keywords/PhraseTokenizer.cs` | internal: text → sentences → word runs delimited by stop words |
| `src/Lodestar.Text/Keywords/RakeOptions.cs` | `RakeOptions`, `RakeMetric` |
| `src/Lodestar.Text/Keywords/Rake.cs` | the extractor |
| `src/Lodestar.Text/Keywords/TextRankOptions.cs` | `TextRankOptions` |
| `src/Lodestar.Text/Keywords/WordGraph.cs` | internal: co-occurrence graph, unreachable-node removal, power iteration |
| `src/Lodestar.Text/Keywords/TextRank.cs` | the extractor: stem, rank, select, re-glue |
| `src/Lodestar.Embeddings/Search/Mmr.cs` | the selector |
| `tests/Lodestar.Text.Tests/Keywords/*.cs` | unit facts and oracle replays |
| `tests/Lodestar.Embeddings.Tests/Search/MmrTests.cs` | unit facts and oracle replay |
| `tools/generate_oracles.py` | three new generators |
| `tests/oracles/keywords_rake.json`, `keywords_textrank.json`, `mmr.json` | the frozen corpora |

---

### Task 1: `KeywordMatch` and the phrase tokenizer

**Files:**

- Create: `src/Lodestar.Text/Keywords/KeywordMatch.cs`
- Create: `src/Lodestar.Text/Keywords/PhraseTokenizer.cs`
- Test: `tests/Lodestar.Text.Tests/Keywords/PhraseTokenizerTests.cs`

**Interfaces:**

- Consumes: `StopWordSet` from `Lodestar.Text.Vectorization` (internal, `Contains(string)` and `Contains(ReadOnlySpan<char>)`), `RegexDefaults.MatchTimeout` from `Lodestar.Internal`.
- Produces: `public readonly record struct KeywordMatch(string Phrase, double Score)`; `internal sealed class PhraseTokenizer` with `IReadOnlyList<IReadOnlyList<string>> Split(string text)` returning the word runs between stop words and punctuation, and `IReadOnlyList<string> Words(string text)` returning every lower-cased token in order, stop words included.

- [ ] **Step 1: Write the failing test**

```csharp
using Lodestar.Text.Keywords;
using Xunit;

namespace Lodestar.Text.Tests.Keywords;

public sealed class PhraseTokenizerTests
{
    private static readonly string[] Stop =
        ["of", "the", "over", "a", "and", "are", "for", "all", "to", "in", "is", "this", "that"];

    private static PhraseTokenizer Tokenizer() => new(Stop, @"\b\w+\b");

    [Fact]
    public void Runs_between_stop_words_are_the_candidates()
    {
        IReadOnlyList<IReadOnlyList<string>> runs =
            Tokenizer().Split("Compatibility of systems of linear constraints over the set of natural numbers.");

        Assert.Equal(
            [["compatibility"], ["systems"], ["linear", "constraints"], ["set"], ["natural", "numbers"]],
            runs.Select(r => r.ToArray()).ToArray());
    }

    [Fact]
    public void Punctuation_ends_a_run_even_without_a_stop_word()
    {
        Assert.Equal(
            [["red"], ["green"], ["blue"]],
            Tokenizer().Split("red, green; blue").Select(r => r.ToArray()).ToArray());
    }

    [Fact]
    public void Words_keeps_the_stop_words_and_their_positions()
    {
        Assert.Equal(
            ["linear", "constraints", "over", "the", "set"],
            Tokenizer().Words("linear constraints over the set"));
    }

    [Fact]
    public void A_document_of_only_stop_words_has_no_candidate()
    {
        Assert.Empty(Tokenizer().Split("of the and over a"));
    }
}
```

- [ ] **Step 2: Run the test and watch it fail**

Run: `dotnet test tests/Lodestar.Text.Tests -c Release --filter "FullyQualifiedName~PhraseTokenizerTests"`
Expected: FAIL — `PhraseTokenizer` and `KeywordMatch` do not exist, so the suite does not compile.

- [ ] **Step 3: Write `KeywordMatch`**

```csharp
namespace Lodestar.Text.Keywords;

/// <summary>One extracted phrase and the score that ranked it.</summary>
/// <param name="Phrase">The phrase, as the extractor assembled it.</param>
/// <param name="Score">Higher is better. The scale is the extractor's, and is not comparable across extractors.</param>
public readonly record struct KeywordMatch(string Phrase, double Score);
```

- [ ] **Step 4: Write `PhraseTokenizer`**

```csharp
using System.Text.RegularExpressions;
using Lodestar.Text.Vectorization;

namespace Lodestar.Text.Keywords;

/// <summary>
/// Splits a document into the word runs between stop words and punctuation.
/// </summary>
/// <remarks>
/// Not <c>TextAnalyzer</c>: that one discards stop words, and a run's boundary is
/// exactly where one stood. RAKE's candidates are these runs.
/// </remarks>
internal sealed class PhraseTokenizer
{
    // Anything that is not a token character ends a run, which is what makes
    // "red, green" two candidates rather than one two-word phrase.
    private readonly Regex _token;
    private readonly StopWordSet _stopWords;

    public PhraseTokenizer(IReadOnlyCollection<string> stopWords, string tokenPattern)
    {
        _stopWords = StopWordSet.Adopt(stopWords);
        _token = new Regex(tokenPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);
    }

    /// <summary>Every token of the document, lower-cased, in order, stop words included.</summary>
    public IReadOnlyList<string> Words(string text)
    {
        var words = new List<string>();
        foreach (Match m in _token.Matches(text.ToLowerInvariant()))
        {
            words.Add(m.Value);
        }
        return words;
    }

    /// <summary>The runs of non-stop-word tokens, in order, split at every stop word and every gap.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Split(string text)
    {
        string lowered = text.ToLowerInvariant();
        var runs = new List<IReadOnlyList<string>>();
        var current = new List<string>();
        int previousEnd = -1;

        foreach (Match m in _token.Matches(lowered))
        {
            bool gap = previousEnd >= 0 && HasNonSpace(lowered, previousEnd, m.Index);
            if (gap || _stopWords.Contains(m.Value))
            {
                Flush(runs, current);
            }

            if (!_stopWords.Contains(m.Value))
            {
                current.Add(m.Value);
            }
            previousEnd = m.Index + m.Length;
        }

        Flush(runs, current);
        return runs;
    }

    private static bool HasNonSpace(string s, int from, int to)
    {
        for (int i = from; i < to; i++)
        {
            if (!char.IsWhiteSpace(s[i]))
            {
                return true;
            }
        }
        return false;
    }

    private static void Flush(List<IReadOnlyList<string>> runs, List<string> current)
    {
        if (current.Count > 0)
        {
            runs.Add(current.ToArray());
            current.Clear();
        }
    }
}
```

- [ ] **Step 5: Run the test and watch it pass**

Run: `dotnet test tests/Lodestar.Text.Tests -c Release --filter "FullyQualifiedName~PhraseTokenizerTests"`
Expected: PASS, **4 tests**. Read the count.

- [ ] **Step 6: Run the netstandard mirror**

Run: `dotnet test tests/Lodestar.Text.NetStandard.Tests -c Release --filter "FullyQualifiedName~PhraseTokenizerTests"`
Expected: PASS, 4 tests. The mirror links the same sources; a compile error here means the code used a `net10.0`-only API.

- [ ] **Step 7: Commit**

```bash
git add src/Lodestar.Text/Keywords tests/Lodestar.Text.Tests/Keywords
git commit -m "KeywordMatch, and the tokenizer that keeps stop-word boundaries

TextAnalyzer drops stop words; a RAKE candidate's boundary is exactly
where one stood, so this splits rather than filters."
```

---

### Task 2: RAKE

**Files:**

- Create: `src/Lodestar.Text/Keywords/RakeOptions.cs`
- Create: `src/Lodestar.Text/Keywords/Rake.cs`
- Test: `tests/Lodestar.Text.Tests/Keywords/RakeTests.cs`

**Interfaces:**

- Consumes: `PhraseTokenizer.Split(string)` and `KeywordMatch` from Task 1; `StopWords.English` from `Lodestar.Text.Vectorization`.
- Produces: `public sealed class Rake` with `Rake(RakeOptions? options = null)` and `IReadOnlyList<KeywordMatch> Extract(string text)`; `public sealed record RakeOptions`; `public enum RakeMetric { DegreeToFrequencyRatio, WordDegree, WordFrequency }`.

- [ ] **Step 1: Write the failing test**

The numbers were measured against `rake-nltk` 1.0.6 with the tokenizer and stop-word list injected, on the abstract from Rose et al.

```csharp
using Lodestar.Text.Keywords;
using Xunit;

namespace Lodestar.Text.Tests.Keywords;

public sealed class RakeTests
{
    private const string Abstract =
        "Compatibility of systems of linear constraints over the set of natural numbers.";

    private static readonly string[] Stop =
        ["of", "the", "over", "a", "and", "are", "for", "all", "to", "in", "is", "this", "that"];

    private static Rake Extractor(RakeOptions? options = null) =>
        new((options ?? new RakeOptions()) with { StopWords = Stop });

    [Fact]
    public void Degree_over_frequency_is_the_default_and_ranks_the_two_pairs_first()
    {
        IReadOnlyList<KeywordMatch> hits = Extractor().Extract(Abstract);

        Assert.Equal(5, hits.Count);
        Assert.Equal(4.0, hits[0].Score, 12);
        Assert.Equal(4.0, hits[1].Score, 12);
        Assert.Equal(
            ["linear constraints", "natural numbers"],
            hits.Take(2).Select(h => h.Phrase).Order(StringComparer.Ordinal));
        Assert.All(hits.Skip(2), h => Assert.Equal(1.0, h.Score, 12));
    }

    [Fact]
    public void Word_frequency_flattens_them_all_to_one()
    {
        IReadOnlyList<KeywordMatch> hits =
            Extractor(new RakeOptions { Metric = RakeMetric.WordFrequency }).Extract(Abstract);

        // Every word occurs once, so a one-word phrase scores 1 and a two-word phrase 2.
        Assert.Equal(2.0, hits[0].Score, 12);
        Assert.Equal(2.0, hits[1].Score, 12);
    }

    [Fact]
    public void Word_degree_scores_a_pair_by_its_span()
    {
        IReadOnlyList<KeywordMatch> hits =
            Extractor(new RakeOptions { Metric = RakeMetric.WordDegree }).Extract(Abstract);

        Assert.Equal(4.0, hits[0].Score, 12);
    }

    [Fact]
    public void Length_bounds_are_inclusive_and_count_words()
    {
        IReadOnlyList<KeywordMatch> pairs =
            Extractor(new RakeOptions { MinLength = 2 }).Extract(Abstract);

        Assert.Equal(2, pairs.Count);
        Assert.All(pairs, h => Assert.Contains(' ', h.Phrase));
    }

    [Fact]
    public void A_run_the_length_filter_dropped_contributes_to_no_table()
    {
        // "linear" occurs twice, once in a pair and once alone. With MinLength = 2 the
        // lone one is gone before the tables: linear is degree 2 over frequency 1, so the
        // pair scores 4. Counting the dropped run first would make it 3.5.
        IReadOnlyList<KeywordMatch> hits =
            Extractor(new RakeOptions { MinLength = 2 }).Extract("linear constraints and linear");

        Assert.Single(hits);
        Assert.Equal(4.0, hits[0].Score, 12);
    }

    [Fact]
    public void A_repeated_phrase_is_reported_once_when_repeats_are_excluded()
    {
        var options = new RakeOptions { IncludeRepeatedPhrases = false };
        IReadOnlyList<KeywordMatch> hits = Extractor(options).Extract("linear constraints and linear constraints");

        Assert.Single(hits);
        Assert.Equal("linear constraints", hits[0].Phrase, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData(RakeMetric.WordFrequency, 2.0)]
    [InlineData(RakeMetric.WordDegree, 4.0)]
    public void Excluding_repeats_removes_them_from_the_tables_too(RakeMetric metric, double expected)
    {
        // Measured against rake-nltk: include_repeated_phrases=False leaves degree 2 and
        // frequency 1, not 4 and 2. Deduplicating only the output would read 4.0 and 8.0.
        var options = new RakeOptions { IncludeRepeatedPhrases = false, Metric = metric };
        IReadOnlyList<KeywordMatch> hits = Extractor(options).Extract("linear constraints and linear constraints");

        Assert.Equal(expected, hits[0].Score, 12);
    }

    [Fact]
    public void An_empty_document_yields_nothing()
    {
        Assert.Empty(Extractor().Extract(string.Empty));
    }

    [Fact]
    public void Null_text_is_refused()
    {
        Assert.Throws<ArgumentNullException>(() => Extractor().Extract(null!));
    }

    [Fact]
    public void A_length_range_that_cannot_match_is_refused_at_construction()
    {
        Assert.Throws<ArgumentException>(() => new Rake(new RakeOptions { MinLength = 3, MaxLength = 2 }));
    }
}
```

- [ ] **Step 2: Run the test and watch it fail**

Run: `dotnet test tests/Lodestar.Text.Tests -c Release --filter "FullyQualifiedName~RakeTests"`
Expected: FAIL — `Rake`, `RakeOptions` and `RakeMetric` do not exist.

- [ ] **Step 3: Write `RakeOptions` and `RakeMetric`**

```csharp
namespace Lodestar.Text.Keywords;

/// <summary>How RAKE scores a word before the phrase sums it.</summary>
public enum RakeMetric
{
    /// <summary><c>deg(w) / freq(w)</c>. The paper's, and the reference implementation's default.</summary>
    DegreeToFrequencyRatio,

    /// <summary><c>deg(w)</c>: how many words it shares a candidate with, itself included, counted per occurrence.</summary>
    WordDegree,

    /// <summary><c>freq(w)</c>: how often it occurs at all.</summary>
    WordFrequency,
}

/// <summary>What <see cref="Rake"/> is built with.</summary>
public sealed record RakeOptions
{
    /// <summary>The stop words that delimit candidates. Null takes <c>StopWords.English</c>.</summary>
    public IReadOnlyCollection<string>? StopWords { get; init; }

    /// <summary>Which per-word score the phrase sums.</summary>
    public RakeMetric Metric { get; init; } = RakeMetric.DegreeToFrequencyRatio;

    /// <summary>Shortest candidate kept, in words, inclusive.</summary>
    public int MinLength { get; init; } = 1;

    /// <summary>Longest candidate kept, in words, inclusive.</summary>
    public int MaxLength { get; init; } = 100_000;

    /// <summary>When false, a candidate that occurs twice is reported once.</summary>
    public bool IncludeRepeatedPhrases { get; init; } = true;

    /// <summary>
    /// What counts as a word.
    /// </summary>
    /// <remarks>
    /// <c>\b\w+\b</c>, not the vectorizers' <c>\b\w\w+\b</c>: a one-letter word neighbours a
    /// boundary rather than being a stop word, and dropping it would merge two candidates.
    /// </remarks>
    public string TokenPattern { get; init; } = @"\b\w+\b";
}
```

- [ ] **Step 4: Write `Rake`**

```csharp
using Lodestar.Text.Vectorization;

namespace Lodestar.Text.Keywords;

/// <summary>
/// Rapid Automatic Keyword Extraction: candidates are the runs between stop words,
/// scored by summing a per-word score over the run.
/// </summary>
/// <remarks>
/// The co-occurrence degree is counted per candidate: a word in an <c>n</c>-word run
/// gains <c>n</c> degree from it, itself included, which is what makes a long run
/// outscore a repeated single word.
/// </remarks>
public sealed class Rake
{
    private readonly RakeOptions _options;
    private readonly PhraseTokenizer _tokenizer;

    /// <summary>Builds an extractor.</summary>
    /// <param name="options">Null takes every default.</param>
    /// <exception cref="ArgumentOutOfRangeException"><c>MinLength</c> is below 1.</exception>
    /// <exception cref="ArgumentException"><c>MaxLength</c> is below <c>MinLength</c>, so nothing can match.</exception>
    public Rake(RakeOptions? options = null)
    {
        _options = options ?? new RakeOptions();
        Guard.NotLessThan(_options.MinLength, 1);
        if (_options.MaxLength < _options.MinLength)
        {
            throw new ArgumentException(
                $"MaxLength {_options.MaxLength} is below MinLength {_options.MinLength}, so no candidate can match.",
                nameof(options));
        }

        _tokenizer = new PhraseTokenizer(_options.StopWords ?? StopWords.English, _options.TokenPattern);
    }

    /// <summary>Extracts the ranked candidates of one document.</summary>
    /// <param name="text">The document.</param>
    /// <returns>Candidates in descending score. Empty when the document has none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public IReadOnlyList<KeywordMatch> Extract(string text)
    {
        Guard.NotNull(text);

        IEnumerable<IReadOnlyList<string>> runs = _tokenizer.Split(text)
            .Where(run => run.Count >= _options.MinLength && run.Count <= _options.MaxLength);

        // Deduplication happens here, ahead of the tables, because that is where the
        // reference does it: measured, include_repeated_phrases=False leaves the repeated
        // phrase's words at degree 2 and frequency 1, not 4 and 2.
        if (!_options.IncludeRepeatedPhrases)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            runs = runs.Where(run => seen.Add(string.Join(" ", run)));
        }

        IReadOnlyList<IReadOnlyList<string>> candidates = runs.ToArray();

        var degree = new Dictionary<string, int>(StringComparer.Ordinal);
        var frequency = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (IReadOnlyList<string> run in candidates)
        {
            foreach (string word in run)
            {
                degree[word] = degree.TryGetValue(word, out int d) ? d + run.Count : run.Count;
                frequency[word] = frequency.TryGetValue(word, out int f) ? f + 1 : 1;
            }
        }

        var scored = new List<KeywordMatch>(candidates.Count);
        foreach (IReadOnlyList<string> run in candidates)
        {
            string phrase = string.Join(" ", run);
            double score = 0;
            foreach (string word in run)
            {
                score += _options.Metric switch
                {
                    RakeMetric.WordDegree => degree[word],
                    RakeMetric.WordFrequency => frequency[word],
                    _ => (double)degree[word] / frequency[word],
                };
            }
            scored.Add(new KeywordMatch(phrase, score));
        }

        scored.Sort((a, b) => b.Score.CompareTo(a.Score));
        return scored;
    }
}
```

- [ ] **Step 5: Run the tests and watch them pass**

Run: `dotnet test tests/Lodestar.Text.Tests -c Release --filter "FullyQualifiedName~RakeTests"`
Expected: PASS, **8 tests**.

- [ ] **Step 6: Run the mirror, then the whole suite**

Run: `dotnet test Lodestar.slnx -c Release`
Expected: every suite green; `Lodestar.Text` up by 12 facts over its previous count on both the net10 suite and the mirror.

- [ ] **Step 7: Commit**

```bash
git add src/Lodestar.Text/Keywords tests/Lodestar.Text.Tests/Keywords
git commit -m "RAKE, with all three of the paper's metrics

Degree is counted per candidate: a word in an n-word run gains n from it,
which is what makes a long run outscore a repeated single word."
```

---

### Task 3: The RAKE oracle

**Files:**

- Modify: `tools/generate_oracles.py` (add `generate_keywords_rake`, register `keywords_rake.json`)
- Modify: `tools/requirements.txt`, `tools/requirements.lock.txt`
- Create: `tests/oracles/keywords_rake.json` (generated, committed)
- Test: `tests/Lodestar.Text.Tests/Keywords/RakeOracleTests.cs`

**Interfaces:**

- Consumes: `Rake`, `RakeOptions`, `RakeMetric`, `KeywordMatch` from Task 2; `OracleLoader.Load(string)` from `tests/Lodestar.Text.Tests`.
- Produces: `tests/oracles/keywords_rake.json` with `metadata.stop_words` (the list the oracle used), `metadata.token_pattern`, and `cases[]` of `{ id, name, text, metric, min_length, max_length, include_repeated_phrases, expected: [{ phrase, score }] }`.

- [ ] **Step 1: Write the generator**

The tokenizer and stop words are injected, so the generator downloads no nltk data.

```python
KEYWORDS_STOP_WORDS = [
    "a", "all", "and", "are", "for", "in", "is", "of", "over", "that", "the", "this", "to",
]

KEYWORDS_TOKEN_PATTERN = r"\b\w+\b"

KEYWORDS_DOCUMENTS = [
    ("rose_abstract",
     "Compatibility of systems of linear constraints over the set of natural numbers. "
     "Criteria of compatibility of a system of linear Diophantine equations, strict "
     "inequations, and nonstrict inequations are considered. Upper bounds for components "
     "of a minimal set of solutions and algorithms of construction of minimal generating "
     "sets of solutions for all types of systems are given."),
    ("one_sentence",
     "Compatibility of systems of linear constraints over the set of natural numbers."),
    ("punctuation_only_boundaries", "red, green; blue"),
    ("all_stop_words", "of the and over a"),
    ("empty", ""),
]


def generate_keywords_rake() -> dict:
    import re  # noqa: PLC0415

    from rake_nltk import Metric, Rake  # noqa: PLC0415

    token = re.compile(KEYWORDS_TOKEN_PATTERN)
    stop = set(KEYWORDS_STOP_WORDS)

    # Injected rather than nltk's: the reference then tokenizes exactly as the C# does,
    # and generation needs no nltk.download of punkt_tab or stopwords.
    def sentences(text: str) -> list[str]:
        return [s for s in re.split(r"[.!?;:,\n]", text) if s.strip()]

    def words(sentence: str) -> list[str]:
        return token.findall(sentence.lower())

    metrics = {
        "DegreeToFrequencyRatio": Metric.DEGREE_TO_FREQUENCY_RATIO,
        "WordDegree": Metric.WORD_DEGREE,
        "WordFrequency": Metric.WORD_FREQUENCY,
    }

    cases = []
    for name, text in KEYWORDS_DOCUMENTS:
        for metric_name, metric in metrics.items():
            # Both settings, because the flag changes the degree and frequency tables and
            # not merely the output: freezing only True would leave the other half unread.
            for repeats in (True, False):
                rake = Rake(
                    stopwords=stop,
                    punctuations=set(),
                    ranking_metric=metric,
                    include_repeated_phrases=repeats,
                    sentence_tokenizer=sentences,
                    word_tokenizer=words,
                )
                rake.extract_keywords_from_text(text)
                cases.append({
                    "id": len(cases),
                    "name": f"{name}:{metric_name}:repeats={repeats}",
                    "text": text,
                    "metric": metric_name,
                    "min_length": 1,
                    "max_length": 100000,
                    "include_repeated_phrases": repeats,
                    "expected": [
                        {"phrase": phrase, "score": score}
                        for score, phrase in rake.get_ranked_phrases_with_scores()
                    ],
                })

    return {
        "metadata": {
            "algorithm": "Rake",
            "library": "rake-nltk",
            "library_version": version("rake-nltk"),
            "reference_calls": [
                "rake_nltk.Rake(stopwords=..., punctuations=set(), ranking_metric=...,"
                " sentence_tokenizer=..., word_tokenizer=...).get_ranked_phrases_with_scores()"
            ],
            "stop_words": KEYWORDS_STOP_WORDS,
            "token_pattern": KEYWORDS_TOKEN_PATTERN,
            "count": len(cases),
        },
        "cases": cases,
    }
```

Register it in `main`'s `generators` dict, in the order the file already uses:

```python
        "keywords_rake.json": generate_keywords_rake,
```

- [ ] **Step 2: Add the dependency**

Append `rake-nltk==1.0.6` to `tools/requirements.txt`, then regenerate the lock the way the file's own header says. Its transitive `nltk` is already pinned there.

- [ ] **Step 3: Generate the corpus from a neutral directory**

```bash
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py; echo "exit=$?"
```

Expected: `exit=0`, and `tests/oracles/keywords_rake.json` written. **Read that exit code, not a pipeline's.** `/tmp` is the wrong neutral directory when the checkout lives under it.

- [ ] **Step 4: Write the replay test**

```csharp
using System.Text.Json;
using Lodestar.Text.Keywords;
using Xunit;

namespace Lodestar.Text.Tests.Keywords;

/// <summary>Replays every case of <c>keywords_rake.json</c> against rake-nltk's own numbers.</summary>
public sealed class RakeOracleTests
{
    public static TheoryData<string> Cases()
    {
        var names = new TheoryData<string>();
        using JsonDocument doc = OracleLoader.Load("keywords_rake.json");
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            names.Add(c.GetProperty("name").GetString()!);
        }
        return names;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Matches_rake_nltk(string name)
    {
        using JsonDocument doc = OracleLoader.Load("keywords_rake.json");
        JsonElement metadata = doc.RootElement.GetProperty("metadata");
        string[] stop = metadata.GetProperty("stop_words").EnumerateArray().Select(e => e.GetString()!).ToArray();
        string pattern = metadata.GetProperty("token_pattern").GetString()!;

        JsonElement expected = doc.RootElement.GetProperty("cases").EnumerateArray()
            .First(c => c.GetProperty("name").GetString() == name);

        var options = new RakeOptions
        {
            StopWords = stop,
            TokenPattern = pattern,
            Metric = Enum.Parse<RakeMetric>(expected.GetProperty("metric").GetString()!),
            MinLength = expected.GetProperty("min_length").GetInt32(),
            MaxLength = expected.GetProperty("max_length").GetInt32(),
            IncludeRepeatedPhrases = expected.GetProperty("include_repeated_phrases").GetBoolean(),
        };

        IReadOnlyList<KeywordMatch> actual = new Rake(options).Extract(expected.GetProperty("text").GetString()!);
        JsonElement[] rows = [.. expected.GetProperty("expected").EnumerateArray()];

        Assert.Equal(rows.Length, actual.Count);

        // Compared as a multiset: rake-nltk's order among equal scores is its sort's,
        // and a tie-break neither implementation promises is not a behaviour to match.
        Assert.Equal(
            rows.Select(r => (r.GetProperty("phrase").GetString()!, r.GetProperty("score").GetDouble()))
                .OrderBy(p => p.Item1, StringComparer.Ordinal).ThenBy(p => p.Item2),
            actual.Select(m => (m.Phrase, m.Score))
                .OrderBy(p => p.Phrase, StringComparer.Ordinal).ThenBy(p => p.Score),
            new PhraseScoreComparer());
    }

    private sealed class PhraseScoreComparer : IEqualityComparer<(string Phrase, double Score)>
    {
        public bool Equals((string Phrase, double Score) a, (string Phrase, double Score) b) =>
            string.Equals(a.Phrase, b.Phrase, StringComparison.Ordinal) && Math.Abs(a.Score - b.Score) <= 1e-9;

        public int GetHashCode((string Phrase, double Score) value) => value.Phrase.GetHashCode(StringComparison.Ordinal);
    }
}
```

- [ ] **Step 5: Run it**

Run: `dotnet test tests/Lodestar.Text.Tests -c Release --filter "FullyQualifiedName~RakeOracleTests"`
Expected: PASS, **15 tests** — five documents × three metrics.

- [ ] **Step 6: Prove the corpus regenerates without drift**

```bash
cp tests/oracles/keywords_rake.json /var/tmp/keywords_rake.before.json
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
python3 tools/compare_oracles.py /var/tmp/keywords_rake.before.json tests/oracles/keywords_rake.json
```

Expected: no differences. This is what CI's *Oracles are reproducible* job does.

- [ ] **Step 7: Commit**

```bash
git add tools/generate_oracles.py tools/requirements.txt tools/requirements.lock.txt \
        tests/oracles/keywords_rake.json tests/Lodestar.Text.Tests/Keywords/RakeOracleTests.cs
git commit -m "RAKE replays rake-nltk 1.0.6, with its tokenizer injected

Injecting the tokenizer and the stop words means the reference tokenizes
exactly as the C# does, and that generation downloads no nltk data."
```

---

### Task 4: The word graph and its power iteration

**Files:**

- Create: `src/Lodestar.Text/Keywords/WordGraph.cs`
- Test: `tests/Lodestar.Text.Tests/Keywords/WordGraphTests.cs`

**Interfaces:**

- Consumes: nothing from earlier tasks.
- Produces: `internal sealed class WordGraph` with `WordGraph(IReadOnlyList<string?> stream, int window)`, `IReadOnlyList<string> Nodes { get; }` (first-occurrence order, unreachable nodes removed), and `double[] Rank(double damping, double tolerance, int maxIterations)` returning the dominant left eigenvector, L2-normalised, in `Nodes` order.
- **The stream is the raw token stream, with `null` at every position that is not a node.** A stop word occupies a position and forms no node, which is what keeps `compatibility` and `systems` from being adjacent across the `of` between them. Windowing the filtered stream instead gives this document 12 edges rather than summa's 5, and removes nothing.

- [ ] **Step 1: Write the failing test**

The graph and the four scores were measured against `summa` 1.2.0 on the two-sentence document below, after `remove_unreachable_nodes`.

```csharp
using Lodestar.Text.Keywords;
using Xunit;

namespace Lodestar.Text.Tests.Keywords;

public sealed class WordGraphTests
{
    // The raw token stream of
    // "Compatibility of systems of linear constraints over the set of natural numbers.
    //  Criteria of compatibility of a system of linear Diophantine equations."
    // stemmed, with null wherever a stop word stood. The nulls are the point: they hold
    // the positions that keep compat and system from neighbouring each other.
    private static readonly string?[] Stream =
    [
        "compat", null, "system", null, "linear", "constraint", null, null, "set", null,
        "natur", "number", "criteria", null, "compat", null, null, "system", null,
        "linear", "diophantin", "equat",
    ];

    [Fact]
    public void A_node_with_no_edge_is_removed_before_ranking()
    {
        var graph = new WordGraph(Stream, window: 2);

        // compat, system and set only ever neighbour a node they equal or a removed one.
        Assert.Equal(
            ["linear", "constraint", "natur", "number", "criteria", "diophantin", "equat"],
            graph.Nodes);
    }

    [Fact]
    public void Rank_reproduces_the_scores_summa_publishes()
    {
        var graph = new WordGraph(Stream, window: 2);
        double[] scores = graph.Rank(damping: 0.85, tolerance: 1e-12, maxIterations: 1000);

        Dictionary<string, double> byStem = graph.Nodes
            .Select((s, i) => (s, scores[i]))
            .ToDictionary(p => p.s, p => p.Item2, StringComparer.Ordinal);

        Assert.Equal(0.526895906655717, byStem["number"], 12);
        Assert.Equal(0.4686942795397464, byStem["diophantin"], 12);
        Assert.Equal(0.46869427953974613, byStem["linear"], 12);
        Assert.Equal(0.27808395073496167, byStem["criteria"], 12);
    }

    [Fact]
    public void Only_tokens_adjacent_in_the_raw_stream_share_an_edge()
    {
        // summa's five, measured: a stop word between two words is a position, so
        // "compatibility of systems" makes no compat-system edge.
        var graph = new WordGraph(Stream, window: 2);

        Assert.Equal(5, graph.EdgeCount);
    }

    [Fact]
    public void The_ranking_vector_has_unit_L2_norm()
    {
        double[] scores = new WordGraph(Stream, window: 2).Rank(0.85, 1e-12, 1000);

        Assert.Equal(1.0, Math.Sqrt(scores.Sum(s => s * s)), 12);
    }

    [Fact]
    public void A_document_whose_words_never_co_occur_ranks_nothing()
    {
        var graph = new WordGraph(["alpha", null], window: 2);

        Assert.Empty(graph.Nodes);
        Assert.Empty(graph.Rank(0.85, 1e-12, 1000));
    }

    [Fact]
    public void A_null_stream_is_refused()
    {
        Assert.Throws<ArgumentNullException>(() => new WordGraph(null!, window: 2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_window_below_one_is_refused(int window)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WordGraph(Stream, window));
    }

    [Fact]
    public void Failing_to_converge_is_an_error_rather_than_a_half_iterated_vector()
    {
        var graph = new WordGraph(Stream, window: 2);

        Assert.Throws<InvalidOperationException>(() => graph.Rank(0.85, 1e-18, maxIterations: 2));
    }
}
```

- [ ] **Step 2: Run the test and watch it fail**

Run: `dotnet test tests/Lodestar.Text.Tests -c Release --filter "FullyQualifiedName~WordGraphTests"`
Expected: FAIL — `WordGraph` does not exist.

- [ ] **Step 3: Write `WordGraph`**

```csharp
namespace Lodestar.Text.Keywords;

/// <summary>
/// The undirected co-occurrence graph TextRank ranks, and the power iteration that ranks it.
/// </summary>
/// <remarks>
/// The window runs over the RAW token stream: a stop word is a null that occupies a
/// position and forms no node, so two words separated by one are not adjacent. Nodes of
/// zero weighted degree are then deleted in one pass, without which the transition matrix
/// is substochastic and its dominant eigenvector is a different vector. Both measured
/// against summa, whose pipeline does the same two things.
/// </remarks>
internal sealed class WordGraph
{
    private readonly List<string> _nodes = [];
    private readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);
    private double[,] _weights;

    public WordGraph(IReadOnlyList<string?> stream, int window)
    {
        foreach (string? token in stream)
        {
            if (token is not null && !_index.ContainsKey(token))
            {
                _index[token] = _nodes.Count;
                _nodes.Add(token);
            }
        }

        int n = _nodes.Count;
        var weights = new double[n, n];
        for (int i = 0; i < stream.Count; i++)
        {
            if (stream[i] is not string left)
            {
                continue;
            }
            for (int j = i + 1; j < Math.Min(i + window, stream.Count); j++)
            {
                if (stream[j] is not string right)
                {
                    continue;
                }
                int a = _index[left];
                int b = _index[right];
                if (a == b)
                {
                    continue;
                }
                weights[a, b] += 1;
                weights[b, a] += 1;
            }
        }

        _weights = weights;
        RemoveUnreachable();
    }

    /// <summary>The ranked words, in first-occurrence order, with the unreachable ones gone.</summary>
    public IReadOnlyList<string> Nodes => _nodes;

    /// <summary>How many undirected edges survive, which is what a window bug changes first.</summary>
    public int EdgeCount
    {
        get
        {
            int edges = 0;
            for (int i = 0; i < _nodes.Count; i++)
            {
                for (int j = i + 1; j < _nodes.Count; j++)
                {
                    if (_weights[i, j] != 0)
                    {
                        edges++;
                    }
                }
            }
            return edges;
        }
    }

    /// <summary>The dominant left eigenvector of <c>d·A + (1 − d)/n</c>, L2-normalised.</summary>
    /// <exception cref="InvalidOperationException">The iteration did not converge within <paramref name="maxIterations"/>.</exception>
    public double[] Rank(double damping, double tolerance, int maxIterations)
    {
        int n = _nodes.Count;
        if (n == 0)
        {
            return [];
        }

        double[,] m = new double[n, n];
        double teleport = (1 - damping) / n;
        for (int i = 0; i < n; i++)
        {
            double degree = 0;
            for (int j = 0; j < n; j++)
            {
                degree += _weights[i, j];
            }
            for (int j = 0; j < n; j++)
            {
                m[i, j] = teleport + (degree == 0 ? 0 : damping * _weights[i, j] / degree);
            }
        }

        double[] x = new double[n];
        double[] next = new double[n];
        for (int i = 0; i < n; i++)
        {
            x[i] = 1.0 / Math.Sqrt(n);
        }

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            for (int j = 0; j < n; j++)
            {
                double sum = 0;
                for (int i = 0; i < n; i++)
                {
                    sum += x[i] * m[i, j];
                }
                next[j] = sum;
            }

            double norm = Math.Sqrt(next.Sum(v => v * v));
            double delta = 0;
            for (int j = 0; j < n; j++)
            {
                next[j] /= norm;
                delta = Math.Max(delta, Math.Abs(next[j] - x[j]));
            }

            (x, next) = (next, x);
            if (delta < tolerance)
            {
                for (int j = 0; j < n; j++)
                {
                    x[j] = Math.Abs(x[j]);
                }
                return x;
            }
        }

        throw new InvalidOperationException(
            $"The power iteration did not converge to {tolerance} within {maxIterations} iterations.");
    }

    // One pass is enough: a node of zero weighted degree has no edges, so removing it
    // lowers nobody else's degree and can isolate no one. summa's is one `for` too.
    private void RemoveUnreachable()
    {
        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            double degree = 0;
            for (int j = 0; j < _nodes.Count; j++)
            {
                degree += _weights[i, j];
            }
            if (degree == 0)
            {
                // Descending, so Drop never invalidates an index still to visit.
                Drop(i);
            }
        }
    }

    private void Drop(int index)
    {
        int n = _nodes.Count;
        var trimmed = new double[n - 1, n - 1];
        for (int i = 0, a = 0; i < n; i++)
        {
            if (i == index)
            {
                continue;
            }
            for (int j = 0, b = 0; j < n; j++)
            {
                if (j == index)
                {
                    continue;
                }
                trimmed[a, b++] = _weights[i, j];
            }
            a++;
        }

        _nodes.RemoveAt(index);
        _weights = trimmed;
        _index.Clear();
        for (int i = 0; i < _nodes.Count; i++)
        {
            _index[_nodes[i]] = i;
        }
    }
}
```

- [ ] **Step 4: Run the tests and watch them pass**

Run: `dotnet test tests/Lodestar.Text.Tests -c Release --filter "FullyQualifiedName~WordGraphTests"`
Expected: PASS, **5 tests**. If `Rank` disagrees with summa's numbers, the cause is almost always the window: summa pairs token `i` with tokens `i+1 … i+window-1`, so a window of 2 pairs only adjacent tokens.

- [ ] **Step 5: Commit**

```bash
git add src/Lodestar.Text/Keywords/WordGraph.cs tests/Lodestar.Text.Tests/Keywords/WordGraphTests.cs
git commit -m "The TextRank graph, and the power iteration that ranks it

Zero-degree nodes are deleted before ranking: with them in, the matrix is
substochastic and its dominant eigenvector is a different vector. Measured
against summa, which deletes them too."
```

---

### Task 5: TextRank

**Files:**

- Create: `src/Lodestar.Text/Keywords/TextRankOptions.cs`
- Create: `src/Lodestar.Text/Keywords/TextRank.cs`
- Test: `tests/Lodestar.Text.Tests/Keywords/TextRankTests.cs`

**Interfaces:**

- Consumes: `WordGraph(IReadOnlyList<string?> stream, int window)`, `.Nodes` and `.Rank(damping, tolerance, maxIterations)` (Task 4); `PhraseTokenizer.Words(string)` and `KeywordMatch` (Task 1); `EnglishSnowballStemmer` from `Lodestar.Text.Stemming`; `StopWords.English`.
- **Build the stream `WordGraph` wants: one entry per raw token, the stem where the token is kept and `null` where a stop word stood.** Do not compact it — the nulls are what make the window match summa's.
- Produces: `public sealed class TextRank` with `TextRank(TextRankOptions? options = null)` and `IReadOnlyList<KeywordMatch> Extract(string text)`; `public sealed record TextRankOptions`.

- [ ] **Step 1: Write the failing test**

```csharp
using Lodestar.Text.Keywords;
using Xunit;

namespace Lodestar.Text.Tests.Keywords;

public sealed class TextRankTests
{
    private const string TwoSentences =
        "Compatibility of systems of linear constraints over the set of natural numbers. " +
        "Criteria of compatibility of a system of linear Diophantine equations.";

    [Fact]
    public void The_four_highest_match_what_summa_publishes()
    {
        IReadOnlyList<KeywordMatch> hits = new TextRank(new TextRankOptions { Words = 4 }).Extract(TwoSentences);

        Assert.Equal(4, hits.Count);
        Assert.Equal("numbers", hits[0].Phrase, StringComparer.Ordinal);
        Assert.Equal(0.526895906655717, hits[0].Score, 12);
    }

    [Fact]
    public void Adjacent_survivors_are_glued_and_scored_by_their_mean()
    {
        IReadOnlyList<KeywordMatch> hits = new TextRank(new TextRankOptions { Words = 8 }).Extract(TwoSentences);

        KeywordMatch glued = hits.Single(h => h.Phrase.Contains(' ', StringComparison.Ordinal));
        double[] parts = glued.Phrase.Split(' ')
            .Select(word => hits.Concat(Singles(TwoSentences)).First(h => h.Phrase == word).Score)
            .ToArray();

        Assert.Equal(parts.Average(), glued.Score, 12);
    }

    private static IReadOnlyList<KeywordMatch> Singles(string text) =>
        new TextRank(new TextRankOptions { Words = 20 }).Extract(text)
            .Where(h => !h.Phrase.Contains(' ', StringComparison.Ordinal)).ToArray();

    [Fact]
    public void Words_overrides_ratio()
    {
        var byRatio = new TextRank(new TextRankOptions { Ratio = 0.2 }).Extract(TwoSentences);
        var byCount = new TextRank(new TextRankOptions { Ratio = 0.2, Words = 5 }).Extract(TwoSentences);

        Assert.NotEqual(byRatio.Count, byCount.Count);
    }

    [Fact]
    public void A_document_with_no_co_occurrence_yields_nothing()
    {
        Assert.Empty(new TextRank().Extract("Alpha."));
    }

    [Fact]
    public void An_empty_document_yields_nothing()
    {
        Assert.Empty(new TextRank().Extract(string.Empty));
    }

    [Fact]
    public void Null_text_is_refused()
    {
        Assert.Throws<ArgumentNullException>(() => new TextRank().Extract(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_window_below_one_is_refused(int window)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRank(new TextRankOptions { Window = window }));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void A_damping_outside_the_open_unit_interval_is_refused(double damping)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRank(new TextRankOptions { Damping = damping }));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.5)]
    public void A_ratio_outside_zero_to_one_is_refused(double ratio)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRank(new TextRankOptions { Ratio = ratio }));
    }
}
```

- [ ] **Step 2: Run the test and watch it fail**

Run: `dotnet test tests/Lodestar.Text.Tests -c Release --filter "FullyQualifiedName~TextRankTests"`
Expected: FAIL — `TextRank` and `TextRankOptions` do not exist.

- [ ] **Step 3: Write `TextRankOptions`**

```csharp
namespace Lodestar.Text.Keywords;

/// <summary>What <see cref="TextRank"/> is built with.</summary>
public sealed record TextRankOptions
{
    /// <summary>The stop words dropped before the graph is built. Null takes <c>StopWords.English</c>.</summary>
    public IReadOnlyCollection<string>? StopWords { get; init; }

    /// <summary>How many tokens share a co-occurrence window. 2 pairs adjacent tokens only.</summary>
    public int Window { get; init; } = 2;

    /// <summary>The random-surfer damping of the reference implementation.</summary>
    public double Damping { get; init; } = 0.85;

    /// <summary>
    /// How close two successive iterates must be before the ranking is taken as converged.
    /// </summary>
    /// <remarks>
    /// This implementation's, not the reference's: summa solves the eigenproblem outright and
    /// has no tolerance to expose.
    /// </remarks>
    public double Tolerance { get; init; } = 1e-12;

    /// <summary>Iterations allowed before <c>Extract</c> gives up rather than return a half-ranked vector.</summary>
    public int MaxIterations { get; init; } = 1_000;

    /// <summary>What proportion of the ranked words to keep. Ignored when <see cref="Words"/> is set.</summary>
    public double Ratio { get; init; } = 0.2;

    /// <summary>How many ranked words to keep, overriding <see cref="Ratio"/>.</summary>
    public int? Words { get; init; }

    /// <summary>What counts as a word.</summary>
    public string TokenPattern { get; init; } = @"\b\w+\b";
}
```

- [ ] **Step 4: Write `TextRank`**

```csharp
using Lodestar.Text.Stemming;
using Lodestar.Text.Vectorization;

namespace Lodestar.Text.Keywords;

/// <summary>
/// TextRank over a co-occurrence graph: rank the stems, keep the best, and re-glue the
/// ones that stood next to each other in the source.
/// </summary>
/// <remarks>
/// A glued phrase scores the mean of its parts, and is not required to be grammatical —
/// that is the reference implementation's behaviour and reproducing it is the point.
/// </remarks>
public sealed class TextRank
{
    private readonly TextRankOptions _options;
    private readonly PhraseTokenizer _tokenizer;
    private readonly StopWordSet _stopWords;
    private readonly EnglishSnowballStemmer _stemmer = new();

    /// <summary>Builds an extractor.</summary>
    /// <param name="options">Null takes every default.</param>
    /// <exception cref="ArgumentOutOfRangeException"><c>Window</c> is below 1, <c>Damping</c> is outside <c>(0, 1)</c>, <c>Ratio</c> is outside <c>(0, 1]</c>, or <c>MaxIterations</c> is below 1.</exception>
    public TextRank(TextRankOptions? options = null)
    {
        _options = options ?? new TextRankOptions();
        Guard.NotLessThan(_options.Window, 1);
        Guard.NotLessThan(_options.MaxIterations, 1);
        if (_options.Damping <= 0 || _options.Damping >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), _options.Damping, "Damping must lie in (0, 1).");
        }
        if (_options.Ratio <= 0 || _options.Ratio > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), _options.Ratio, "Ratio must lie in (0, 1].");
        }

        IReadOnlyCollection<string> stop = _options.StopWords ?? StopWords.English;
        _stopWords = StopWordSet.Adopt(stop);
        _tokenizer = new PhraseTokenizer(stop, _options.TokenPattern);
    }

    /// <summary>Extracts the ranked keywords of one document.</summary>
    /// <param name="text">The document.</param>
    /// <returns>Keywords in descending score, glued where their parts were adjacent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The ranking did not converge within <c>MaxIterations</c>.</exception>
    public IReadOnlyList<KeywordMatch> Extract(string text)
    {
        Guard.NotNull(text);

        IReadOnlyList<string> words = _tokenizer.Words(text);

        // One entry per raw token: the stem where the word is kept, null where a stop
        // word stood. The nulls hold the positions the co-occurrence window counts.
        var stream = new string?[words.Count];
        var surface = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        for (int i = 0; i < words.Count; i++)
        {
            string word = words[i];
            if (_stopWords.Contains(word))
            {
                continue;
            }

            string stem = _stemmer.Stem(word);
            stream[i] = stem;
            if (!surface.TryGetValue(stem, out Dictionary<string, int>? counts))
            {
                surface[stem] = counts = new Dictionary<string, int>(StringComparer.Ordinal);
            }
            counts[word] = counts.TryGetValue(word, out int c) ? c + 1 : 1;
        }

        var graph = new WordGraph(stream, _options.Window);
        if (graph.Nodes.Count == 0)
        {
            return [];
        }

        double[] ranked = graph.Rank(_options.Damping, _options.Tolerance, _options.MaxIterations);
        int take = _options.Words ?? (int)(graph.Nodes.Count * _options.Ratio);
        take = Math.Clamp(take, 0, graph.Nodes.Count);

        Dictionary<string, double> scoreByStem = graph.Nodes
            .Select((stem, i) => (stem, ranked[i]))
            .OrderByDescending(p => p.Item2)
            .Take(take)
            .ToDictionary(p => p.stem, p => p.Item2, StringComparer.Ordinal);

        return Glue(stream, scoreByStem, surface);
    }

    // A continuation must be selected, must equal its raw whitespace-split token (so
    // "numbers." breaks the run), and must not already be in the phrase being built.
    // Each keyword is consumed once per document, as summa's _keywords.pop does.
    private static IReadOnlyList<KeywordMatch> Glue(
        IReadOnlyList<string?> stream,
        Dictionary<string, double> scoreByStem,
        Dictionary<string, Dictionary<string, int>> surface)
    {
        var hits = new List<KeywordMatch>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int i = 0;
        while (i < stream.Count)
        {
            if (stream[i] is not string head || !scoreByStem.ContainsKey(head))
            {
                i++;
                continue;
            }

            int j = i;
            double total = 0;
            var parts = new List<string>();
            while (j < stream.Count
                   && stream[j] is string stem
                   && scoreByStem.TryGetValue(stem, out double score))
            {
                parts.Add(Best(surface[stem]));
                total += score;
                j++;
            }

            string phrase = string.Join(" ", parts);
            if (seen.Add(phrase))
            {
                hits.Add(new KeywordMatch(phrase, total / parts.Count));
            }
            i = j;
        }

        hits.Sort((a, b) => b.Score.CompareTo(a.Score));
        return hits;
    }

    private static string Best(Dictionary<string, int> counts) =>
        counts.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).First().Key;
}
```

- [ ] **Step 5: Run the tests and watch them pass**

Run: `dotnet test tests/Lodestar.Text.Tests -c Release --filter "FullyQualifiedName~TextRankTests"`
Expected: PASS, **12 tests** (the three `Theory` cases expand).

- [ ] **Step 6: Commit**

```bash
git add src/Lodestar.Text/Keywords tests/Lodestar.Text.Tests/Keywords
git commit -m "TextRank: rank the stems, keep the best, re-glue the adjacent

Tolerance and MaxIterations are this implementation's, not the reference's:
summa solves the eigenproblem outright and has no tolerance to expose."
```

---

### Task 6: The TextRank oracle

**Files:**

- Modify: `tools/generate_oracles.py` (add `generate_keywords_textrank`, register `keywords_textrank.json`)
- Modify: `tools/requirements.txt`, `tools/requirements.lock.txt`
- Create: `tests/oracles/keywords_textrank.json`
- Test: `tests/Lodestar.Text.Tests/Keywords/TextRankOracleTests.cs`

**Interfaces:**

- Consumes: `TextRank`, `TextRankOptions`, `KeywordMatch` from Task 5.
- Produces: `tests/oracles/keywords_textrank.json` with `metadata.stop_words` (summa's own 339), and `cases[]` of `{ id, name, text, words, expected: [{ phrase, score }] }`.

- [ ] **Step 1: Write the generator, with the dominance guard**

`summa` reads `vecs[i][0]` — whichever column LAPACK returns first — so the generator recomputes the ranking and refuses to freeze a case where the two disagree.

```python
def generate_keywords_textrank() -> dict:
    import numpy as np  # noqa: PLC0415
    from summa import keywords as sk  # noqa: PLC0415
    from summa.commons import build_graph, remove_unreachable_nodes  # noqa: PLC0415
    from summa.preprocessing.stopwords import get_stopwords_by_language  # noqa: PLC0415

    stop_words = sorted(get_stopwords_by_language("english"))

    def stationary(text: str) -> dict:
        """The dominant left eigenvector, by power iteration, in summa's own node order."""
        tokens = sk._clean_text_by_word(text, "english", deacc=False, additional_stopwords=None)
        graph = build_graph(sk._get_words_for_graph(tokens))
        sk._set_graph_edges(graph, tokens, list(sk._tokenize_by_word(text)))
        remove_unreachable_nodes(graph)

        nodes = graph.nodes()
        n = len(nodes)
        if n == 0:
            return {}

        adjacency = np.zeros((n, n))
        for i, u in enumerate(nodes):
            total = sum(graph.edge_weight((u, v)) for v in graph.neighbors(u))
            for j, v in enumerate(nodes):
                weight = float(graph.edge_weight((u, v)))
                if i != j and weight != 0:
                    adjacency[i, j] = weight / total

        matrix = 0.85 * adjacency + 0.15 / n
        x = np.ones(n) / np.sqrt(n)
        for _ in range(100_000):
            y = x @ matrix
            y /= np.linalg.norm(y)
            if np.abs(y - x).max() < 1e-15:
                x = y
                break
            x = y
        return dict(zip(nodes, np.abs(x)))

    cases = []
    for name, text, words in KEYWORDS_TEXTRANK_DOCUMENTS:
        published = sk.keywords(text, words=words, scores=True) if text.strip() else []
        reference = stationary(text)

        # summa takes eig's first column, which is the dominant one only by LAPACK's
        # ordering. A corpus that froze a non-dominant eigenvector would make the C#
        # fail for being right, so the disagreement stops generation instead.
        for phrase, score in published:
            if " " in phrase:
                continue
            stem = sk._clean_text_by_word(phrase, "english", deacc=False, additional_stopwords=None)
            lemma = next(iter(stem.values())).token
            if abs(reference.get(lemma, float("nan")) - float(score)) > 1e-9:
                raise SystemExit(
                    f"keywords_textrank '{name}': summa's score for {phrase!r} ({score}) is not the "
                    f"dominant eigenvector's ({reference.get(lemma)}). Refusing to freeze it."
                )

        cases.append({
            "id": len(cases),
            "name": name,
            "text": text,
            "words": words,
            "expected": [{"phrase": phrase, "score": float(score)} for phrase, score in published],
        })

    return {
        "metadata": {
            "algorithm": "TextRank",
            "library": "summa",
            "library_version": version("summa"),
            "reference_calls": ["summa.keywords.keywords(text, words=n, scores=True)"],
            "stop_words": stop_words,
            "window": 2,
            "damping": 0.85,
            "count": len(cases),
        },
        "cases": cases,
    }
```

with the documents beside the RAKE ones:

```python
KEYWORDS_TEXTRANK_DOCUMENTS = [
    ("two_sentences",
     "Compatibility of systems of linear constraints over the set of natural numbers. "
     "Criteria of compatibility of a system of linear Diophantine equations.", 4),
    ("rose_abstract",
     "Compatibility of systems of linear constraints over the set of natural numbers. "
     "Criteria of compatibility of a system of linear Diophantine equations, strict "
     "inequations, and nonstrict inequations are considered. Upper bounds for components "
     "of a minimal set of solutions and algorithms of construction of minimal generating "
     "sets of solutions for all types of systems are given.", 6),
    ("no_co_occurrence", "Alpha.", 4),
    ("empty", "", 4),
]
```

Register it:

```python
        "keywords_textrank.json": generate_keywords_textrank,
```

- [ ] **Step 2: Add the dependency**

Append `summa==1.2.0` to `tools/requirements.txt` and regenerate the lock. Its transitive `scipy` is already pinned.

- [ ] **Step 3: Generate and read the exit code**

```bash
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py; echo "exit=$?"
```

Expected: `exit=0`. A non-zero exit naming a case is the dominance guard firing — that case must be replaced, never suppressed.

- [ ] **Step 4: Write the replay test**

```csharp
using System.Text.Json;
using Lodestar.Text.Keywords;
using Xunit;

namespace Lodestar.Text.Tests.Keywords;

/// <summary>Replays every case of <c>keywords_textrank.json</c> against summa's own numbers.</summary>
public sealed class TextRankOracleTests
{
    public static TheoryData<string> Cases()
    {
        var names = new TheoryData<string>();
        using JsonDocument doc = OracleLoader.Load("keywords_textrank.json");
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            names.Add(c.GetProperty("name").GetString()!);
        }
        return names;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Matches_summa(string name)
    {
        using JsonDocument doc = OracleLoader.Load("keywords_textrank.json");
        string[] stop = doc.RootElement.GetProperty("metadata").GetProperty("stop_words")
            .EnumerateArray().Select(e => e.GetString()!).ToArray();

        JsonElement expected = doc.RootElement.GetProperty("cases").EnumerateArray()
            .First(c => c.GetProperty("name").GetString() == name);

        var options = new TextRankOptions
        {
            StopWords = stop,
            Words = expected.GetProperty("words").GetInt32(),
        };

        IReadOnlyList<KeywordMatch> actual = new TextRank(options).Extract(expected.GetProperty("text").GetString()!);
        JsonElement[] rows = [.. expected.GetProperty("expected").EnumerateArray()];

        Assert.Equal(rows.Length, actual.Count);
        for (int i = 0; i < rows.Length; i++)
        {
            Assert.Equal(rows[i].GetProperty("phrase").GetString()!, actual[i].Phrase, StringComparer.Ordinal);
            Assert.Equal(rows[i].GetProperty("score").GetDouble(), actual[i].Score, 12);
        }
    }
}
```

- [ ] **Step 5: Run it, then prove reproducibility**

Run: `dotnet test tests/Lodestar.Text.Tests -c Release --filter "FullyQualifiedName~TextRankOracleTests"`
Expected: PASS, **4 tests**.

```bash
cp tests/oracles/keywords_textrank.json /var/tmp/textrank.before.json
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
python3 tools/compare_oracles.py /var/tmp/textrank.before.json tests/oracles/keywords_textrank.json
```

Expected: no differences.

- [ ] **Step 6: Commit**

```bash
git add tools/generate_oracles.py tools/requirements.txt tools/requirements.lock.txt \
        tests/oracles/keywords_textrank.json tests/Lodestar.Text.Tests/Keywords/TextRankOracleTests.cs
git commit -m "TextRank replays summa 1.2.0, with a dominance guard

summa reads eig's first column, dominant only by LAPACK's ordering. The
generator recomputes the stationary distribution and refuses to freeze a
case where the two disagree."
```

---

### Task 7: `Mmr`

**Files:**

- Create: `src/Lodestar.Embeddings/Search/Mmr.cs`
- Test: `tests/Lodestar.Embeddings.Tests/Search/MmrTests.cs`

**Interfaces:**

- Consumes: `VectorMath.Dot(ReadOnlySpan<float>, ReadOnlySpan<float>)` and `VectorMath.L2Norm(ReadOnlySpan<float>)`, both public in `Lodestar.Embeddings.Search`.
- Produces: `public static class Mmr` with `public static int[] Select(ReadOnlySpan<float> query, IReadOnlyList<float[]> candidates, int count, double lambda = 0.5)`.

- [ ] **Step 1: Write the failing test**

The expected selections were measured against `keybert` 0.9.0 on these four vectors.

```csharp
using Lodestar.Embeddings.Search;
using Xunit;

namespace Lodestar.Embeddings.Tests.Search;

public sealed class MmrTests
{
    private static readonly float[] Query = [1f, 0f, 0f];

    // sim to query: 1.0, 0.8, 0.6, 0.0
    private static readonly float[][] Candidates =
    [
        [1.00f, 0.00f, 0.00f],
        [0.80f, 0.60f, 0.00f],
        [0.60f, 0.00f, 0.80f],
        [0.00f, 1.00f, 0.00f],
    ];

    [Fact]
    public void Pure_relevance_takes_them_in_query_order()
    {
        Assert.Equal([0, 1, 2], Mmr.Select(Query, Candidates, count: 3, lambda: 1.0));
    }

    [Fact]
    public void Pure_diversity_takes_the_orthogonal_one_second()
    {
        // The order is the selection's, not a re-sort by relevance: candidate 3 is
        // orthogonal to candidate 0 and so is the least redundant available.
        Assert.Equal([0, 3, 2], Mmr.Select(Query, Candidates, count: 3, lambda: 0.0));
    }

    [Fact]
    public void The_first_pick_is_always_the_most_relevant()
    {
        foreach (double lambda in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
        {
            Assert.Equal(0, Mmr.Select(Query, Candidates, count: 1, lambda)[0]);
        }
    }

    [Fact]
    public void Asking_for_more_than_there_are_returns_them_all_once()
    {
        int[] chosen = Mmr.Select(Query, Candidates, count: 99);

        Assert.Equal(4, chosen.Length);
        Assert.Equal(4, chosen.Distinct().Count());
    }

    [Fact]
    public void Zero_selects_nothing()
    {
        Assert.Empty(Mmr.Select(Query, Candidates, count: 0));
    }

    [Fact]
    public void A_zero_vector_has_no_cosine_and_is_refused()
    {
        float[][] withZero = [[1f, 0f, 0f], [0f, 0f, 0f]];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Mmr.Select(Query, withZero, count: 2));

        Assert.Contains("index 1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_candidate_of_the_wrong_width_is_refused()
    {
        float[][] ragged = [[1f, 0f, 0f], [1f, 0f]];

        Assert.Throws<ArgumentException>(() => Mmr.Select(Query, ragged, count: 2));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void A_lambda_outside_the_unit_interval_is_refused(double lambda)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Mmr.Select(Query, Candidates, count: 2, lambda));
    }

    [Fact]
    public void A_negative_count_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Mmr.Select(Query, Candidates, count: -1));
    }

    [Fact]
    public void Null_candidates_are_refused()
    {
        Assert.Throws<ArgumentNullException>(() => Mmr.Select(Query, null!, count: 1));
    }
}
```

- [ ] **Step 2: Run the test and watch it fail**

Run: `dotnet test tests/Lodestar.Embeddings.Tests -c Release --filter "FullyQualifiedName~MmrTests"`
Expected: FAIL — `Mmr` does not exist.

- [ ] **Step 3: Write `Mmr`**

```csharp
namespace Lodestar.Embeddings.Search;

/// <summary>
/// Maximal Marginal Relevance: greedy selection that trades relevance to a query
/// against redundancy with what is already selected.
/// </summary>
/// <remarks>
/// Knows nothing about text. The candidates are vectors and the result is their
/// indices, so the same call serves keyword selection, passage reranking and any
/// other list a caller wants spread out rather than clustered.
/// </remarks>
public static class Mmr
{
    /// <summary>Selects up to <paramref name="count"/> candidates.</summary>
    /// <param name="query">What relevance is measured against.</param>
    /// <param name="candidates">The candidate vectors, all of <paramref name="query"/>'s length.</param>
    /// <param name="count">How many to select. More than there are selects them all.</param>
    /// <param name="lambda">1 is pure relevance, 0 pure diversity.</param>
    /// <returns>The chosen indices, <b>in selection order</b>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="candidates"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative, or <paramref name="lambda"/> is outside <c>[0, 1]</c>.</exception>
    /// <exception cref="ArgumentException">A candidate is null, of a different length, or the zero vector, whose cosine is undefined.</exception>
    public static int[] Select(
        ReadOnlySpan<float> query,
        IReadOnlyList<float[]> candidates,
        int count,
        double lambda = 0.5)
    {
        Guard.NotNull(candidates);
        Guard.NotLessThan(count, 0);
        if (lambda < 0 || lambda > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(lambda), lambda, "Lambda must lie in [0, 1].");
        }

        int n = Math.Min(count, candidates.Count);
        if (n == 0)
        {
            return [];
        }

        var norms = new float[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            float[]? candidate = candidates[i];
            if (candidate is null || candidate.Length != query.Length)
            {
                throw new ArgumentException(
                    $"Candidate at index {i} is null or is not {query.Length} wide.", nameof(candidates));
            }

            norms[i] = VectorMath.L2Norm(candidate);
            if (norms[i] == 0)
            {
                throw new ArgumentException(
                    $"Candidate at index {i} is the zero vector, whose cosine is undefined.", nameof(candidates));
            }
        }

        float queryNorm = VectorMath.L2Norm(query);
        if (queryNorm == 0)
        {
            throw new ArgumentException("The query is the zero vector, whose cosine is undefined.", nameof(query));
        }

        var toQuery = new double[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            toQuery[i] = VectorMath.Dot(query, candidates[i]) / ((double)queryNorm * norms[i]);
        }

        var chosen = new int[n];
        var taken = new bool[candidates.Count];
        var redundancy = new double[candidates.Count];

        int first = 0;
        for (int i = 1; i < candidates.Count; i++)
        {
            if (toQuery[i] > toQuery[first])
            {
                first = i;
            }
        }

        chosen[0] = first;
        taken[first] = true;
        for (int i = 0; i < candidates.Count; i++)
        {
            redundancy[i] = Cosine(candidates[i], candidates[first], norms[i], norms[first]);
        }

        for (int k = 1; k < n; k++)
        {
            int best = -1;
            double bestScore = double.NegativeInfinity;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (taken[i])
                {
                    continue;
                }
                double score = (lambda * toQuery[i]) - ((1 - lambda) * redundancy[i]);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }

            chosen[k] = best;
            taken[best] = true;
            for (int i = 0; i < candidates.Count; i++)
            {
                redundancy[i] = Math.Max(redundancy[i], Cosine(candidates[i], candidates[best], norms[i], norms[best]));
            }
        }

        return chosen;
    }

    private static double Cosine(ReadOnlySpan<float> a, ReadOnlySpan<float> b, float normA, float normB) =>
        VectorMath.Dot(a, b) / ((double)normA * normB);
}
```

- [ ] **Step 4: Run the tests and watch them pass**

Run: `dotnet test tests/Lodestar.Embeddings.Tests -c Release --filter "FullyQualifiedName~MmrTests"`
Expected: PASS, **11 tests**.

- [ ] **Step 5: Commit**

```bash
git add src/Lodestar.Embeddings/Search/Mmr.cs tests/Lodestar.Embeddings.Tests/Search/MmrTests.cs
git commit -m "MMR, as a selector over vectors

Indices in selection order, not scores: the caller owns the candidates, and
an index array composes with keyword phrases, passages or index rows alike."
```

---

### Task 8: The MMR oracle

**Files:**

- Modify: `tools/generate_oracles.py` (add `generate_mmr`, register `mmr.json`)
- Modify: `tools/requirements.txt`, `tools/requirements.lock.txt`
- Create: `tests/oracles/mmr.json`
- Test: `tests/Lodestar.Embeddings.Tests/Search/MmrOracleTests.cs`

**Interfaces:**

- Consumes: `Mmr.Select` from Task 7; `OracleLoader.Load(string)` from `tests/Lodestar.Embeddings.Tests`.
- Produces: `tests/oracles/mmr.json` with `cases[]` of `{ id, name, query, candidates, count, lambda, selected }`, where `selected` is the **set** keybert chose.

- [ ] **Step 1: Write the generator**

```python
MMR_CASES = [
    ("orthogonal_tail",
     [1.0, 0.0, 0.0],
     [[1.0, 0.0, 0.0], [0.8, 0.6, 0.0], [0.6, 0.0, 0.8], [0.0, 1.0, 0.0]],
     3, [0.0, 0.25, 0.5, 0.75, 1.0]),
    ("two_clusters",
     [1.0, 1.0, 0.0],
     [[1.0, 0.9, 0.0], [0.9, 1.0, 0.0], [0.0, 0.0, 1.0], [0.1, 0.0, 1.0], [1.0, 0.0, 0.0]],
     3, [0.0, 0.5, 1.0]),
]


def generate_mmr() -> dict:
    import numpy as np  # noqa: PLC0415
    from keybert._mmr import mmr  # noqa: PLC0415

    cases = []
    for name, query, candidates, count, lambdas in MMR_CASES:
        for lam in lambdas:
            labels = [str(i) for i in range(len(candidates))]
            chosen = mmr(
                np.array([query]), np.array(candidates), labels,
                top_n=count, diversity=1 - lam,
            )
            cases.append({
                "id": len(cases),
                "name": f"{name}:lambda={lam}",
                "query": query,
                "candidates": candidates,
                "count": count,
                "lambda": lam,
                # keybert sorts by similarity to the document, not by selection order,
                # so the set is what the two implementations can be held to.
                "selected": sorted(int(label) for label, _ in chosen),
            })

    return {
        "metadata": {
            "algorithm": "Mmr",
            "library": "keybert",
            "library_version": version("keybert"),
            "reference_calls": ["keybert._mmr.mmr(doc_embedding, word_embeddings, words, top_n, diversity)"],
            "note": "diversity = 1 - lambda; keybert returns its picks sorted by relevance, so only the set is compared",
            "count": len(cases),
        },
        "cases": cases,
    }
```

Register it:

```python
        "mmr.json": generate_mmr,
```

- [ ] **Step 2: Add the dependency, without its model stack**

Append `keybert==0.9.0` to `tools/requirements.txt` **with a comment recording why it is installed `--no-deps`**, and regenerate the lock without pulling `sentence-transformers` or torch:

```bash
<repo>/.venv-oracles/bin/python -m pip install --no-deps keybert==0.9.0
```

`keybert._mmr` imports only numpy and scikit-learn, both already pinned. Verify:

```bash
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python -c "from keybert._mmr import mmr; print('ok')"
```

Expected: `ok`. If it raises on `sentence_transformers`, the import moved and the `--no-deps` install is no longer viable — say so rather than installing torch.

- [ ] **Step 3: Generate and read the exit code**

```bash
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py; echo "exit=$?"
```

Expected: `exit=0`, `tests/oracles/mmr.json` written.

- [ ] **Step 4: Write the replay test**

```csharp
using System.Text.Json;
using Lodestar.Embeddings.Search;
using Xunit;

namespace Lodestar.Embeddings.Tests.Search;

/// <summary>Replays every case of <c>mmr.json</c> against keybert's own selections.</summary>
public sealed class MmrOracleTests
{
    public static TheoryData<string> Cases()
    {
        var names = new TheoryData<string>();
        using JsonDocument doc = OracleLoader.Load("mmr.json");
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            names.Add(c.GetProperty("name").GetString()!);
        }
        return names;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Matches_keybert(string name)
    {
        using JsonDocument doc = OracleLoader.Load("mmr.json");
        JsonElement expected = doc.RootElement.GetProperty("cases").EnumerateArray()
            .First(c => c.GetProperty("name").GetString() == name);

        float[] query = Row(expected.GetProperty("query"));
        float[][] candidates = [.. expected.GetProperty("candidates").EnumerateArray().Select(Row)];

        int[] chosen = Mmr.Select(
            query,
            candidates,
            expected.GetProperty("count").GetInt32(),
            expected.GetProperty("lambda").GetDouble());

        // The set, not the sequence: keybert re-sorts its picks by relevance.
        Assert.Equal(
            expected.GetProperty("selected").EnumerateArray().Select(e => e.GetInt32()).Order(),
            chosen.Order());
    }

    private static float[] Row(JsonElement array) =>
        [.. array.EnumerateArray().Select(e => (float)e.GetDouble())];
}
```

- [ ] **Step 5: Run it, then prove reproducibility**

Run: `dotnet test tests/Lodestar.Embeddings.Tests -c Release --filter "FullyQualifiedName~MmrOracleTests"`
Expected: PASS, **8 tests** — five lambdas on the first case, three on the second.

```bash
cp tests/oracles/mmr.json /var/tmp/mmr.before.json
cd /var/tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py
python3 tools/compare_oracles.py /var/tmp/mmr.before.json tests/oracles/mmr.json
```

Expected: no differences.

- [ ] **Step 6: Commit**

```bash
git add tools/generate_oracles.py tools/requirements.txt tools/requirements.lock.txt \
        tests/oracles/mmr.json tests/Lodestar.Embeddings.Tests/Search/MmrOracleTests.cs
git commit -m "MMR replays keybert 0.9.0, compared as a set

keybert sorts its picks by relevance rather than by selection order, so the
set is what the two implementations can be held to. Installed --no-deps:
_mmr needs only numpy and scikit-learn."
```

---

### Task 9: Reference pages, the guide, equivalence rows and the ADR

**Files:**

- Create: `docs/reference/text/keywords.md`, `docs/reference/text/keywords/{rake,rake-extract,rakeoptions,rakemetric,textrank,textrank-extract,textrankoptions,keywordmatch}.md`
- Create: `docs/reference/embeddings/search/mmr.md`, `docs/reference/embeddings/search/mmr-select.md`
- Modify: `docs/reference/embeddings/search.md` (type table gains `Mmr`)
- Create: `docs/guides/keywords.md`
- Modify: `docs/wiki-map.json`, `docs/equivalence.md`
- Create: `docs/decisions/0077-the-keyword-extractors-take-their-oracles-lists-and-not-their-own.md`
- Modify: `docs/decisions/README.md`

**Interfaces:**

- Consumes: every public type from Tasks 1, 2, 5 and 7.
- Produces: `docs/wiki-map.json` gains `"Lodestar.Text.Keywords": "docs/reference/text/keywords"` under `Lodestar.Text`'s `covered`, and `docs/guides/keywords.md` in `Lodestar.Text`'s `pages`.

- [ ] **Step 1: Extend `wiki-map.json`**

Add to `Lodestar.Text`'s `pages`: `"docs/guides/keywords.md"`. Add to its `covered`:

```json
        "Lodestar.Text.Keywords": "docs/reference/text/keywords",
```

`Mmr` needs no new `covered` entry — `Lodestar.Embeddings.Search` is already covered by `docs/reference/embeddings/search`.

- [ ] **Step 2: Run the reference gate and read what it demands**

Run: `dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~ReferenceDocumentation"`
Expected: FAIL, listing one complaint per undocumented type and member. Write the pages the complaints name.

The gate is exact about three things, and each has cost a session before:

- **Declarations must match reflection's rendering**, which drops nullable annotations: write `public int[] Select(ReadOnlySpan<float> query, IReadOnlyList<float[]> candidates, int count, double lambda = 0.5)`, and `int[] order = null` rather than `int[]? order = null` where one occurs.
- **The Exceptions block is compared as a set** against the `<exception>` tags in the source. Every tag needs a line, and a line with no tag fails just as loudly.
- **A member named anywhere on a page must be linked once on that page.** A fence cannot carry a link, so the prose around it must.

- [ ] **Step 3: Write the guide**

`docs/guides/keywords.md` covers the three extractors and, in its last section, the KeyBERT composition — candidates from `Rake`, vectors from `Lodestar.Onnx`, selection from `Mmr` — which is the only place that composition is written down. Every ` ```csharp ` fence is compiled by the doc-snippets gate, so add whatever the fences reference to `samples/Lodestar.DocSnippets/SnippetContext.cs` under a new `internal sealed partial class Keywords`.

- [ ] **Step 4: Add the equivalence rows**

Four rows in `docs/equivalence.md`, each naming the divergence the spec records:

| Python | library | C# |
| --- | --- | --- |
| `rake_nltk.Rake(...).get_ranked_phrases_with_scores()` | rake-nltk | `new Rake(options).Extract(text)` |
| `summa.keywords.keywords(text, scores=True)` | summa | `new TextRank(options).Extract(text)` |
| `keybert._mmr.mmr(doc, words, ...)` | keybert | `Mmr.Select(query, candidates, count, lambda)` |
| `nltk.stem.snowball.SnowballStemmer('english')` | nltk | *(already present — no new row)* |

- [ ] **Step 5: Write the ADR**

`0077`, recording the four divergences the spec lists: the stop-word list is the oracle's and not the API's default; TextRank's parity is numerical at `1e-12`; keybert parameterises `diversity = 1 − λ`; keybert sorts by relevance so only the set is compared. Add its row to `docs/decisions/README.md`.

- [ ] **Step 6: Run every documentation gate**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~ReferenceDocumentation"
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 tools/check_adr_immutable.py --base origin/main
python3 tools/check_comment_length.py
```

Expected: all four clean.

- [ ] **Step 7: Commit**

```bash
git add docs
git commit -m "Reference pages, guide, equivalence rows and ADR 0077 for the keyword extractors"
```

---

### Task 10: The packaging gate, the CHANGELOG, and the whole verification

**Files:**

- Modify: `samples/Lodestar.Sample/Lot2Vectorization.cs` (or a new `Lot6Keywords.cs`), `samples/Lodestar.Sample/Lot3Embeddings.cs`
- Modify: `samples/Lodestar.DocSnippets/SnippetContext.cs`
- Modify: `CHANGELOG.md`

**Interfaces:**

- Consumes: every public type from Tasks 1, 2, 5 and 7.
- Produces: nothing later depends on.

- [ ] **Step 1: Exercise every new public type in the sample**

The packaging gate needs a **member reference** to each: `Rake`, `RakeOptions`, `RakeMetric`, `TextRank`, `TextRankOptions`, `KeywordMatch`, `Mmr`. A type reference alone does not count — reading `KeywordMatch.Phrase` does, constructing one does not.

```csharp
// samples/Lodestar.Sample/Lot6Keywords.cs
using Lodestar.Text.Keywords;

namespace Lodestar.Sample;

/// <summary>Lot 6 — what one document is about, three ways.</summary>
internal static class Lot6Keywords
{
    private const string Document =
        "Compatibility of systems of linear constraints over the set of natural numbers. " +
        "Criteria of compatibility of a system of linear Diophantine equations.";

    public static void Run()
    {
        Console.WriteLine("keywords");

        IReadOnlyList<KeywordMatch> rake = new Rake().Extract(Document);
        Console.WriteLine(FormatInvariant($"  RAKE, deg/freq   : {rake[0].Phrase} ({rake[0].Score:F4})"));

        var byDegree = new Rake(new RakeOptions { Metric = RakeMetric.WordDegree, MinLength = 2 });
        Console.WriteLine(FormatInvariant($"  RAKE, degree     : {byDegree.Extract(Document)[0].Phrase}"));

        IReadOnlyList<KeywordMatch> ranked = new TextRank(new TextRankOptions { Words = 3 }).Extract(Document);
        Console.WriteLine(FormatInvariant($"  TextRank         : {ranked[0].Phrase} ({ranked[0].Score:F4})"));
    }
}
```

Every number printed goes through `CultureInfo.InvariantCulture` — `python3 tools/check_sample_culture.py` fails a sample that prints `0,807` on a French console, and `CA1305` never fires on an interpolated hole. Follow whatever helper the neighbouring `Lot*.cs` files already use.

Add the `Mmr` use to `Lot3Embeddings.cs`, where the vectors already exist:

```csharp
        // Diversity over the same synthetic vectors: the three least redundant of them.
        int[] spread = Mmr.Select(queryVector, corpusVectors, count: 3, lambda: 0.5);
        Console.WriteLine(FormatInvariant($"  MMR, 3 of {corpusVectors.Length}      : [{string.Join(", ", spread)}]"));
```

Call `Lot6Keywords.Run()` from `Program.cs` beside the other lots.

- [ ] **Step 2: Run the packaging gate the way CI does**

```bash
rm -rf ./artifacts
SCRATCH=$(mktemp -d)
for p in src/Lodestar.Abstractions src/Lodestar.Text src/Lodestar.Embeddings src/Lodestar.Fuzzy \
         src/Lodestar.Metrics src/Lodestar.Conformal src/Lodestar.Decomposition src/Lodestar.Onnx; do
  NUGET_PACKAGES=$SCRATCH/pack dotnet pack "$p" -c Release -o ./artifacts
done
NUGET_PACKAGES=$SCRATCH/sample dotnet run --project samples/Lodestar.Sample -c Release
```

Expected: `every public member is reachable.` then `OK`. A separate `NUGET_PACKAGES` per step is not optional — the global folder wins over the local feed whenever the versions match, and the gate would then judge the published package instead of the working tree.

- [ ] **Step 3: Run the doc-snippets gate**

```bash
python3 tools/extract_doc_snippets.py
NUGET_PACKAGES=$SCRATCH/snippets dotnet build samples/Lodestar.DocSnippets -c Release
NUGET_PACKAGES=$SCRATCH/snippets dotnet run --project samples/Lodestar.DocSnippets -c Release
```

Expected: builds clean, and the run reports more snippets than before with none failing. A ` ```csharp ` fence on a reference page whose last line carries `// =>` is an executed assertion on the value the page promises.

- [ ] **Step 4: Write the CHANGELOG entries**

Under `## [Unreleased]`, in `### Lodestar.Text`'s `#### Added`, one entry for the namespace naming both extractors, the oracles they replay and the ADR. In `### Lodestar.Embeddings`'s `#### Added`, one for `Mmr`. Neither mentions a version bump: both releases are already declared.

- [ ] **Step 5: Run everything**

```bash
dotnet build Lodestar.slnx -c Release
dotnet test Lodestar.slnx -c Release
dotnet format Lodestar.slnx --verify-no-changes
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 tools/check_version_floor.py --check-feed
python3 tools/check_machine_paths.py --no-environment
python3 tools/check_sample_culture.py
python3 tools/check_netstandard_guards.py
python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
python3 tools/check_adr_immutable.py --base origin/main
python3 tools/check_comment_length.py
python3 tools/check_repeated_literals.py --base origin/main
python3 -m pytest tools/tests/ -q
```

Expected: every one clean, 0 warnings, 0 errors. **Read the test count**: `Lodestar.Text` gains 44 facts and `Lodestar.Embeddings` 19, on both the net10 suite and its mirror.

`check_nuspec_dependencies.py` proves the point of ADR 0076 here: neither package acquired a dependency, so `Lodestar.Text` and `Lodestar.Embeddings` still carry nothing but the polyfills and `System.Text.Json` on `netstandard2.0`.

- [ ] **Step 6: Commit and push**

```bash
git add samples CHANGELOG.md
git commit -m "The sample exercises the keyword extractors and MMR

Every new public type earns a member reference, which is what the packaging
gate counts."
git push -u origin feat/525-lodestar-text-keywords
```

---

## Self-Review

**Spec coverage.** Every section of the spec maps to a task: the two failed assumptions inform Tasks 1 and 4; RAKE is Tasks 2–3; TextRank is Tasks 4–6; MMR is Tasks 7–8; placement is fixed by the file structure; oracles and their divergences are Tasks 3, 6, 8 and 9; testing is spread through each task's first step; versions are a Global Constraint and Task 10's CHANGELOG step. No gap found.

**Placeholders.** None: every code step carries the code, every run step names the command and the expected result, and every expected test count is stated.

**Type consistency.** `KeywordMatch(string Phrase, double Score)` is used identically in Tasks 1, 2, 5 and 9. `PhraseTokenizer.Split` and `.Words` are declared in Task 1 and consumed with those names in Tasks 2 and 5. `WordGraph.Nodes` and `.Rank(damping, tolerance, maxIterations)` are declared in Task 4 and consumed with that signature in Task 5. `Mmr.Select(query, candidates, count, lambda)` is declared in Task 7 and consumed with that argument order in Tasks 8 and 10.

**One risk worth naming.** Task 5's expected values assume this implementation's stemming and stop-word handling reproduce `summa`'s token stream exactly. If Task 5's first test disagrees with `summa` on the *set* of stems — not merely the last digits — the cause is the token stream, not the ranking: `summa` builds its graph over `_tokenize_by_word(text)`, which spans sentence boundaries. Task 4's facts isolate the ranking from that, so a failure in Task 5 with Task 4 green points at the tokenizer and nowhere else.
