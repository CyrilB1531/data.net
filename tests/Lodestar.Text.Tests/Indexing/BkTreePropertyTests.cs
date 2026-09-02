using Lodestar.Text.Distances;
using Lodestar.Text.Indexing;
using Xunit;

namespace Lodestar.Text.Tests.Indexing;

// SonarLint S2245 / CA5394: a seeded Random builds a reproducible corpus for these
// tests; the sequence is fixed by the seed and nothing here is a security decision.
#pragma warning disable S2245, CA5394

/// <summary>
/// What proves a BK-tree: its answers must equal a linear scan's, and must not depend on
/// the shape insertion order gave it.
/// </summary>
public sealed class BkTreePropertyTests
{
    private const int Seed = 20260902;
    private const string Alphabet = "abcd";

    private static string RandomWord(Random rng) =>
        new([.. Enumerable.Range(0, rng.Next(1, 8)).Select(_ => Alphabet[rng.Next(Alphabet.Length)])]);

    private static List<string> RandomCorpus(Random rng, int size) =>
        [.. new HashSet<string>(Enumerable.Range(0, size).Select(_ => RandomWord(rng)))];

    private static List<BkTreeMatch> Scan(IEnumerable<string> corpus, string query, int radius)
    {
        var hits = new List<BkTreeMatch>();
        foreach (string word in corpus)
        {
            int d = Levenshtein.Distance(word, query);
            if (d <= radius)
            {
                hits.Add(new BkTreeMatch(word, d));
            }
        }
        hits.Sort(static (x, y) => x.Distance.CompareTo(y.Distance));
        return hits;
    }

    [Fact]
    public void The_answer_equals_a_linear_scan()
    {
        var rng = new Random(Seed);
        for (int trial = 0; trial < 200; trial++)
        {
            List<string> corpus = RandomCorpus(rng, rng.Next(1, 60));
            BkTree tree = BkTree.OverLevenshtein();
            tree.AddRange(corpus);

            string query = RandomWord(rng);
            int radius = rng.Next(0, 5);

            IReadOnlyList<BkTreeMatch> fromTree = tree.WithinDistance(query, radius);
            List<BkTreeMatch> fromScan = Scan(corpus, query, radius);

            Assert.Equal(
                fromScan.Select(static h => h.Item).OrderBy(static s => s, StringComparer.Ordinal),
                fromTree.Select(static h => h.Item).OrderBy(static s => s, StringComparer.Ordinal));
        }
    }

    [Fact]
    public void The_answer_does_not_depend_on_insertion_order()
    {
        var rng = new Random(Seed + 1);
        for (int trial = 0; trial < 100; trial++)
        {
            List<string> corpus = RandomCorpus(rng, rng.Next(2, 40));
            string query = RandomWord(rng);
            int radius = rng.Next(0, 4);

            BkTree first = BkTree.OverLevenshtein();
            first.AddRange(corpus);
            HashSet<string> expected = [.. first.WithinDistance(query, radius).Select(static h => h.Item)];

            for (int shuffle = 0; shuffle < 5; shuffle++)
            {
                List<string> reordered = [.. corpus.OrderBy(_ => rng.Next())];
                BkTree other = BkTree.OverLevenshtein();
                other.AddRange(reordered);

                HashSet<string> actual = [.. other.WithinDistance(query, radius).Select(static h => h.Item)];
                Assert.True(expected.SetEquals(actual));
            }
        }
    }

    [Fact]
    public void Nearest_agrees_with_an_unbounded_radius()
    {
        var rng = new Random(Seed + 2);
        for (int trial = 0; trial < 100; trial++)
        {
            List<string> corpus = RandomCorpus(rng, rng.Next(1, 50));
            BkTree tree = BkTree.OverLevenshtein();
            tree.AddRange(corpus);

            string query = RandomWord(rng);
            int wanted = rng.Next(1, 10);

            IReadOnlyList<BkTreeMatch> nearest = tree.Nearest(query, wanted);
            IReadOnlyList<BkTreeMatch> all = tree.WithinDistance(query, int.MaxValue);

            Assert.Equal(all.Take(wanted), nearest);
        }
    }
}
