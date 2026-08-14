using System.Globalization;
using System.Text;

namespace DataNet.Metrics.Internal;

// CA1307 (specify StringComparison): the overload it asks for —
// string.IndexOf(char, StringComparison) / string.Replace(string, string?,
// StringComparison) — does not exist on netstandard2.0, which this assembly
// targets. Both calls are ordinal on every runtime that has them, so the
// suggestion would change nothing but the compilation.
#pragma warning disable CA1307

/// <summary>
/// Renders a <see cref="ClassificationReport"/> in scikit-learn's own layout,
/// character for character.
/// </summary>
/// <remarks>
/// The format strings are transcribed from <c>classification_report</c>: a name
/// column as wide as the longest heading, then four columns of width 9 each
/// preceded by a space. <c>ReportTextTests.Renders_the_sklearn_table_character_for_character</c>
/// string-compares every corpus case's <c>reports</c> field against the result.
/// </remarks>
internal static class ReportText
{
    private const int ColumnWidth = 9;
    private static readonly string[] Headers = ["precision", "recall", "f1-score", "support"];

    public static string Render(ClassificationReport report, int digits)
    {
        int width = ColumnNameWidth(report, digits);
        bool floatSupport = LooksLikeAFloatSupport(report);
        var text = new StringBuilder();

        text.Append(new string(' ', width)).Append(' ');
        foreach (string header in Headers)
        {
            text.Append(' ').Append(header.PadLeft(ColumnWidth));
        }
        text.Append('\n').Append('\n');

        foreach (ClassRow row in report.Classes)
        {
            AppendRow(text, NameOf(row), width, digits, floatSupport,
                      row.Precision, row.Recall, row.F1, row.Support);
        }
        text.Append('\n');

        if (report.MicroAverage is AverageRow micro)
        {
            AppendRow(text, micro.Name, width, digits, floatSupport,
                      micro.Precision, micro.Recall, micro.F1, micro.Support);
        }
        else
        {
            // The accuracy row leaves the precision and recall columns blank.
            text.Append("accuracy".PadLeft(width)).Append(' ');
            text.Append(' ').Append(new string(' ', ColumnWidth));
            text.Append(' ').Append(new string(' ', ColumnWidth));
            text.Append(' ').Append(Number(report.Accuracy, digits).PadLeft(ColumnWidth));
            text.Append(' ').Append(Support(report.TotalSupport, floatSupport).PadLeft(ColumnWidth));
            text.Append('\n');
        }

        AppendRow(text, report.MacroAverage.Name, width, digits, floatSupport,
                  report.MacroAverage.Precision, report.MacroAverage.Recall,
                  report.MacroAverage.F1, report.MacroAverage.Support);
        AppendRow(text, report.WeightedAverage.Name, width, digits, floatSupport,
                  report.WeightedAverage.Precision, report.WeightedAverage.Recall,
                  report.WeightedAverage.F1, report.WeightedAverage.Support);

        return text.ToString();
    }

    // Support prints as a NumPy float whenever weighted, or whenever
    // ConfusionMatrix.NoSampleCorrect holds; see that member — not "accuracy is zero".
    private static bool LooksLikeAFloatSupport(ClassificationReport report) =>
        report.IsWeighted || report.NoSampleCorrect;

    // SonarLint S107 warns above 7 parameters; this one row-renderer stands in for
    // what would otherwise be four near-identical call sites (class, micro,
    // macro, weighted), one per row of a scikit-learn table row, and splitting it
    // further would obscure rather than clarify the transcription.
#pragma warning disable S107
    private static void AppendRow(
        StringBuilder text, string name, int width, int digits, bool floatSupport,
        double precision, double recall, double f1, double support)
#pragma warning restore S107
    {
        text.Append(name.PadLeft(width)).Append(' ');
        text.Append(' ').Append(Number(precision, digits).PadLeft(ColumnWidth));
        text.Append(' ').Append(Number(recall, digits).PadLeft(ColumnWidth));
        text.Append(' ').Append(Number(f1, digits).PadLeft(ColumnWidth));
        text.Append(' ').Append(Support(support, floatSupport).PadLeft(ColumnWidth));
        text.Append('\n');
    }

    private static int ColumnNameWidth(ClassificationReport report, int digits)
    {
        int width = "weighted avg".Length;
        foreach (ClassRow row in report.Classes)
        {
            int length = NameOf(row).Length;
            if (length > width)
            {
                width = length;
            }
        }
        return width > digits ? width : digits;
    }

    private static string NameOf(ClassRow row) =>
        row.Name ?? row.Label.ToString(CultureInfo.InvariantCulture);

    private static string Number(double value, int digits) =>
        // .NET's "F" formatter rounds half-to-even like Python's str.format; pre-rounding
        // with Math.Round's decimal-places overload first drifts 0.695, 0.525's last digit.
        value.ToString("F" + digits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    private static string Support(double value, bool floatSupport)
    {
        if (!floatSupport)
        {
            // Whole, unweighted counts print as a NumPy integer.
            return ((long)Math.Round(value, MidpointRounding.AwayFromZero))
                .ToString(CultureInfo.InvariantCulture);
        }

        string text = value.ToString("R", CultureInfo.InvariantCulture);
        bool looksIntegral = text.IndexOf('.') < 0
            && text.IndexOf('e') < 0
            && text.IndexOf('E') < 0;

        // Python's float repr always carries a decimal point: 4.0, never 4.
        return looksIntegral ? text + ".0" : text;
    }
}
