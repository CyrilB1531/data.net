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
}
