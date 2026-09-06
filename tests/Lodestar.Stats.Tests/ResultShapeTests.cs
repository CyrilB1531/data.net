using Xunit;

namespace Lodestar.Stats.Tests;

/// <summary>
/// The four result shapes, pinned before any family fills them: eight families
/// return the shared record and three carry extras, and a later task must not
/// quietly widen the shared one.
/// </summary>
public sealed class ResultShapeTests
{
    [Fact]
    public void Shared_result_carries_a_statistic_and_a_p_value()
    {
        TestResult result = new(Statistic: 1.5, PValue: 0.25);

        Assert.Equal(1.5, result.Statistic);
        Assert.Equal(0.25, result.PValue);
    }

    [Fact]
    public void T_result_adds_degrees_of_freedom_that_need_not_be_whole()
    {
        // Welch-Satterthwaite degrees of freedom are not a count of anything, so
        // the field is a double and a fractional value must survive the record.
        TTestResult result = new(Statistic: -2.0, PValue: 0.06, Df: 12.7431)
        {
            Estimate = -1.5,
            StandardError = 0.75,
            Alternative = Alternative.TwoSided,
        };

        Assert.Equal(12.7431, result.Df);
        Assert.Equal(-1.5, result.Estimate);
    }

    [Fact]
    public void Contingency_result_keeps_the_expected_table_row_major()
    {
        double[][] expected = [[5.0, 15.0], [15.0, 45.0]];
        Chi2ContingencyResult result = new(0.0, 1.0, Dof: 1, ExpectedFrequencies: expected);

        Assert.Equal(1, result.Dof);
        Assert.Equal(45.0, result.ExpectedFrequencies[1][1]);
    }

    [Fact]
    public void Ks_result_keeps_where_the_supremum_was_reached_and_its_sign()
    {
        KsResult result = new(0.4, 0.3, StatisticLocation: 2.5, StatisticSign: -1);

        Assert.Equal(2.5, result.StatisticLocation);
        Assert.Equal(-1, result.StatisticSign);
    }
}
