using Lodestar.Text.Indexing;
using Xunit;

namespace Lodestar.Text.Tests.Indexing;

public sealed class BkTreeUnitTests
{
    [Fact]
    public void An_empty_tree_holds_nothing()
    {
        BkTree tree = BkTree.OverLevenshtein();

        Assert.Equal(0, tree.Count);
    }

    [Fact]
    public void Adding_counts_distinct_items_only()
    {
        BkTree tree = BkTree.OverLevenshtein();

        Assert.True(tree.Add("book"));
        Assert.True(tree.Add("books"));
        Assert.False(tree.Add("book"));

        Assert.Equal(2, tree.Count);
    }

    [Fact]
    public void AddRange_adds_every_item()
    {
        BkTree tree = BkTree.OverLevenshtein();

        tree.AddRange(["book", "books", "boo", "cook", "cake"]);

        Assert.Equal(5, tree.Count);
    }

    [Fact]
    public void A_null_metric_is_refused()
    {
        Assert.Throws<ArgumentNullException>(() => new BkTree(null!));
    }

    [Fact]
    public void A_null_item_is_refused()
    {
        BkTree tree = BkTree.OverLevenshtein();

        Assert.Throws<ArgumentNullException>(() => tree.Add(null!));
        Assert.Throws<ArgumentNullException>(() => tree.AddRange(null!));
    }

    [Fact]
    public void The_metric_the_caller_supplies_is_the_one_used()
    {
        int calls = 0;
        var tree = new BkTree((a, b) =>
        {
            calls++;
            return Math.Abs(a.Length - b.Length);
        });

        tree.AddRange(["a", "bb", "ccc"]);

        Assert.Equal(3, tree.Count);
        Assert.True(calls > 0);
    }

    private static BkTree Dictionary()
    {
        BkTree tree = BkTree.OverLevenshtein();
        tree.AddRange(["book", "books", "boo", "cook", "cake", "boon", "cape", "back"]);
        return tree;
    }

    [Fact]
    public void A_radius_of_zero_finds_the_exact_item_only()
    {
        IReadOnlyList<BkTreeMatch> hits = Dictionary().WithinDistance("book", 0);

        Assert.Equal([new BkTreeMatch("book", 0)], hits);
    }

    [Fact]
    public void A_radius_of_one_finds_every_neighbour()
    {
        IReadOnlyList<BkTreeMatch> hits = Dictionary().WithinDistance("book", 1);

        Assert.Equal(
            [
                new BkTreeMatch("book", 0),
                new BkTreeMatch("books", 1),
                new BkTreeMatch("boo", 1),
                new BkTreeMatch("cook", 1),
                new BkTreeMatch("boon", 1),
            ],
            hits);
    }

    [Fact]
    public void Limit_keeps_the_nearest_not_the_first_reached()
    {
        IReadOnlyList<BkTreeMatch> hits = Dictionary().WithinDistance("book", 2, limit: 2);

        Assert.Equal(2, hits.Count);
        Assert.Equal("book", hits[0].Item);
        Assert.Equal(0, hits[0].Distance);
        Assert.Equal(1, hits[1].Distance);
    }

    [Fact]
    public void Nearest_is_the_first_n_of_an_unbounded_radius()
    {
        BkTree tree = Dictionary();

        IReadOnlyList<BkTreeMatch> nearest = tree.Nearest("book", 3);
        IReadOnlyList<BkTreeMatch> all = tree.WithinDistance("book", int.MaxValue);

        Assert.Equal(all.Take(3), nearest);
    }

    [Fact]
    public void Querying_an_empty_tree_returns_nothing()
    {
        BkTree tree = BkTree.OverLevenshtein();

        Assert.Empty(tree.WithinDistance("book", 3));
        Assert.Empty(tree.Nearest("book", 3));
    }

    [Fact]
    public void A_negative_radius_and_a_null_query_are_refused()
    {
        BkTree tree = Dictionary();

        Assert.Throws<ArgumentNullException>(() => tree.WithinDistance(null!, 1));
        Assert.Throws<ArgumentNullException>(() => tree.Nearest(null!, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.WithinDistance("book", -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.WithinDistance("book", 1, limit: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.Nearest("book", -1));
    }
}
