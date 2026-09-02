using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Lodestar.Text.Distances;
using Lodestar.Text.Indexing;

namespace Lodestar.Text.Benchmarks;

// CA1822 (mark members static): BenchmarkDotNet rejects static benchmarks —
// "Benchmarks MUST be instance methods, static methods are not supported."
// The build succeeds either way, so following this rule breaks the benchmarks
// at run time rather than compile time.
#pragma warning disable CA1822

/// <summary>
/// What the tree is worth, against the baseline a caller writes instead: a linear scan that
/// skips any word whose length already puts it out of range.
/// </summary>
[MemoryDiagnoser]
public class BkTreeBenchmarks
{
    private string[] words = [];
    private string[] queries = [];
    private BkTree tree = BkTree.OverLevenshtein();

    [Params(1, 2, 3, 4)]
    public int Radius { get; set; }

    [Params("uniform", "clustered")]
    public string Shape { get; set; } = "uniform";

    [GlobalSetup]
    public void Setup()
    {
        string path = Path.Combine(BenchCorpus.RepoRoot(), "bench", "corpus", "dictionary.json");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"The BK-tree benchmark dictionary is missing at '{path}'. " +
                "Generate it first: python3 bench/corpus/generate_dictionary.py");
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        this.words = [.. document.RootElement.GetProperty(this.Shape).EnumerateArray()
            .Select(static e => e.GetString()!)];

        this.tree = BkTree.OverLevenshtein();
        this.tree.AddRange(this.words);

        // Queries drawn from the corpus itself: looking up a word that is present is the
        // spelling-corrector case, and it is the one the tree has to work hardest for.
        this.queries = [.. Enumerable.Range(0, 200).Select(i => this.words[i * 97 % this.words.Length])];
    }

    [Benchmark(Baseline = true)]
    public int LengthFilteredScan()
    {
        int found = 0;
        foreach (string query in this.queries)
        {
            foreach (string word in this.words)
            {
                if (Math.Abs(word.Length - query.Length) > this.Radius)
                {
                    continue;
                }
                if (Levenshtein.Distance(word, query) <= this.Radius)
                {
                    found++;
                }
            }
        }
        return found;
    }

    [Benchmark]
    public int TreeWithinDistance()
    {
        int found = 0;
        foreach (string query in this.queries)
        {
            found += this.tree.WithinDistance(query, this.Radius).Count;
        }
        return found;
    }
}
