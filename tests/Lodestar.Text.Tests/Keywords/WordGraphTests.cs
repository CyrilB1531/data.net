using Lodestar.Text.Keywords;
using Xunit;

namespace Lodestar.Text.Tests.Keywords;

public sealed class WordGraphTests
{
    // The raw stemmed token stream of summa's own sample document, null wherever a stop
    // word stood -- the nulls keep compat and system from neighbouring each other.
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

    // Measured against summa 1.2.0. linear-system repeats (0-1, 3-4) but never raises the
    // weight past 1, so system's two edges stay equal -- pre-fix, its degree was 3 (2+1), not 2.
    [Fact]
    public void A_repeated_adjacent_pair_leaves_the_edge_weight_at_one()
    {
        string?[] stream = ["linear", "system", null, "linear", "system", null, "system", "theori"];
        var graph = new WordGraph(stream, window: 2);

        Assert.Equal(["linear", "system", "theori"], graph.Nodes);
        Assert.Equal(2, graph.EdgeCount);

        double[] scores = graph.Rank(damping: 0.85, tolerance: 1e-12, maxIterations: 1000);
        Dictionary<string, double> byStem = graph.Nodes
            .Select((s, i) => (s, scores[i]))
            .ToDictionary(p => p.s, p => p.Item2, StringComparer.Ordinal);

        Assert.Equal(byStem["linear"], byStem["theori"], 12);
        Assert.Equal(0.42295388648078086, byStem["linear"], 10);
        Assert.Equal(0.8013863112267425, byStem["system"], 10);
    }

    // Measured against summa 1.2.0. "matrix matrix" alone survives RemoveUnreachable on its
    // self-loop; adding "theory" halves matrix's outgoing share to 1/2, where the pre-fix skip left it at 1/1.
    [Fact]
    public void A_self_loop_survives_removal_and_halves_the_outgoing_share()
    {
        var loopOnly = new WordGraph(["matrix", "matrix"], window: 2);
        Assert.Equal(["matrix"], loopOnly.Nodes);

        var withNeighbor = new WordGraph(["matrix", "matrix", "theori"], window: 2);
        double[] scores = withNeighbor.Rank(damping: 0.85, tolerance: 1e-12, maxIterations: 1000);
        Dictionary<string, double> byStem = withNeighbor.Nodes
            .Select((s, i) => (s, scores[i]))
            .ToDictionary(p => p.s, p => p.Item2, StringComparer.Ordinal);

        Assert.Equal(0.8056815791722831, byStem["matrix"], 10);
        Assert.Equal(0.5923488777590923, byStem["theori"], 10);
    }
}
