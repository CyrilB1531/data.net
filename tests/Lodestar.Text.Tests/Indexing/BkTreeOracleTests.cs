using Lodestar.Text.Indexing;
using Lodestar.Text.Tests.Oracles;
using Xunit;

namespace Lodestar.Text.Tests.Indexing;

/// <summary>Replays <c>tests/oracles/text_bktree.json</c>, whose answers were computed by
/// scanning rather than by another tree.</summary>
public sealed class BkTreeOracleTests
{
    private static readonly OracleFile<BkTreeCase> Corpus =
        OracleCorpus.Load<BkTreeCase>("text_bktree.json");

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Corpus.Cases.Count; i++)
        {
            data.Add(i);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_radius_query_matches_the_scan(int index)
    {
        BkTreeCase expected = Corpus.Cases[index];
        BkTree tree = BkTree.OverLevenshtein();
        tree.AddRange(expected.Corpus);

        IReadOnlyList<BkTreeMatch> actual = tree.WithinDistance(expected.Query, expected.Radius);

        // The corpus orders ties by item; the tree orders them by insertion rank, so the
        // comparison is over the sorted sets rather than the two lists as they stand.
        Assert.Equal(
            expected.Hits.Select(static h => (h.Item, h.Distance)).OrderBy(static h => h),
            actual.Select(static h => (h.Item, h.Distance)).OrderBy(static h => h));
    }

    [Fact]
    public void The_corpus_is_the_one_that_was_committed()
    {
        Assert.Equal(Corpus.Metadata.Count, Corpus.Cases.Count);
        Assert.NotEmpty(Corpus.Cases);
    }
}
