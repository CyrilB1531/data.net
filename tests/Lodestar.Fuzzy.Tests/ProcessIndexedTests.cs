using Lodestar.Fuzzy;
using Lodestar.Text.Distances;
using Lodestar.Text.Indexing;
using Xunit;

namespace Lodestar.Fuzzy.Tests;

/// <summary>
/// The prefilter contract: identical to <see cref="Process.Extract"/> when the cutoff is
/// consistent with the radius, and a strict subset when it is not.
/// </summary>
public sealed class ProcessIndexedTests
{
    private static readonly string[] Choices =
        ["book", "books", "boo", "cook", "cake", "boon", "cape", "back", "bookkeeper"];

    private static BkTree Index()
    {
        BkTree tree = BkTree.OverLevenshtein();
        tree.AddRange(Choices);
        return tree;
    }

    // A scorer that is a function of the radius, which is what makes the two agree.
    private static double ByLevenshtein(string a, string b) =>
        Math.Max(0.0, 100.0 - (10.0 * Levenshtein.Distance(a, b)));

    [Fact]
    public void It_matches_Extract_when_the_cutoff_excludes_everything_outside_the_radius()
    {
        // A cutoff of 81 keeps exactly the distances of 1 or less -- 2 already scores 80 --
        // and the radius-2 prefilter is a superset of those, so the two agree.
        IReadOnlyList<ExtractResult> linear =
            Process.Extract("book", Choices, ByLevenshtein, limit: null, scoreCutoff: 81.0);
        IReadOnlyList<ExtractResult> indexed =
            Process.ExtractIndexed("book", Index(), maxDistance: 2, ByLevenshtein, limit: null, scoreCutoff: 81.0);

        Assert.Equal(linear.Select(static r => r.Choice), indexed.Select(static r => r.Choice));
        Assert.Equal(linear.Select(static r => r.Score), indexed.Select(static r => r.Score));
    }

    [Fact]
    public void It_returns_a_strict_subset_when_the_cutoff_does_not()
    {
        IReadOnlyList<ExtractResult> linear =
            Process.Extract("book", Choices, ByLevenshtein, limit: null, scoreCutoff: 0.0);
        IReadOnlyList<ExtractResult> indexed =
            Process.ExtractIndexed("book", Index(), maxDistance: 1, ByLevenshtein, limit: null, scoreCutoff: 0.0);

        Assert.True(indexed.Count < linear.Count);
        Assert.Subset(
            new HashSet<string>(linear.Select(static r => r.Choice)),
            new HashSet<string>(indexed.Select(static r => r.Choice)));
    }

    [Fact]
    public void The_index_is_the_source_of_the_indices()
    {
        IReadOnlyList<ExtractResult> indexed =
            Process.ExtractIndexed("book", Index(), maxDistance: 1, ByLevenshtein, limit: null);

        // Index is the candidate's rank in this call's candidate list, not its rank in the
        // tree: the tree has no ordering a caller supplied to preserve.
        Assert.Equal(Enumerable.Range(0, indexed.Count), indexed.Select(static r => r.Index).Order());
    }

    [Fact]
    public void Limit_and_the_default_scorer_behave_as_they_do_in_Extract()
    {
        IReadOnlyList<ExtractResult> indexed =
            Process.ExtractIndexed("book", Index(), maxDistance: 2);

        Assert.True(indexed.Count <= 5);
        Assert.Equal("book", indexed[0].Choice);
        Assert.Equal(100.0, indexed[0].Score);
    }

    [Fact]
    public void Null_and_negative_arguments_are_refused()
    {
        Assert.Throws<ArgumentNullException>(() => Process.ExtractIndexed(null!, Index(), 1));
        Assert.Throws<ArgumentNullException>(() => Process.ExtractIndexed("book", null!, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Process.ExtractIndexed("book", Index(), -1));
    }
}
