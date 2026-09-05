using System.Globalization;

namespace Lodestar.Sample;

/// <summary>Every number the sample prints, formatted the same way on every machine.</summary>
/// <remarks>
/// Interpolation formats through <c>CurrentCulture</c>, so one commit printed <c>0.807</c>
/// on CI and <c>0,807</c> on a French console with nothing failing, and <c>CA1305</c> never
/// reaches that syntax (<c>docs/decisions/0019</c>, issue #205). <c>Program</c> pins the
/// thread culture for what this cannot cover, and <c>tools/check_sample_culture.py</c>
/// keeps both. Called through the type name rather than a <c>using static</c>:
/// <c>Lodestar.Metrics</c> exports a type named <c>F1</c> that an import would hide.
/// </remarks>
internal static class Inv
{
    /// <summary>A number with no decimals — a count, or a total that happens to be whole.</summary>
    public static string F0(double value) => value.ToString("F0", CultureInfo.InvariantCulture);

    /// <summary>A number to one decimal.</summary>
    public static string F1(double value) => value.ToString("F1", CultureInfo.InvariantCulture);

    /// <summary>One decimal, or <c>null</c> for a value that is absent rather than zero.</summary>
    public static string? F1(double? value) => value is null ? null : F1(value.Value);

    /// <summary>A number to three decimals, which most of the sample prints.</summary>
    public static string F3(double value) => value.ToString("F3", CultureInfo.InvariantCulture);

    /// <summary>Three decimals, or <c>null</c> for a value that is absent rather than zero.</summary>
    public static string? F3(double? value) => value is null ? null : F3(value.Value);

    /// <summary>Three decimals of a single-precision value, which the poolers return.</summary>
    public static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);

    /// <summary>A number to four decimals.</summary>
    public static string F4(double value) => value.ToString("F4", CultureInfo.InvariantCulture);

    /// <summary>A number to five decimals.</summary>
    public static string F5(double value) => value.ToString("F5", CultureInfo.InvariantCulture);

    /// <summary>Three significant digits in scientific notation, invariant culture.</summary>
    /// <remarks>
    /// p-values span thirty orders of magnitude, so a fixed-point format prints
    /// most of them as zero. tools/check_sample_culture.py is why the culture is
    /// stated rather than inherited.
    /// </remarks>
    public static string E3(double value) =>
        value.ToString("E3", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>A row of numbers, bracketed and comma-separated, each to three decimals.</summary>
    public static string List(IEnumerable<double> values) =>
        "[" + string.Join(", ", values.Select(F3)) + "]";

    /// <summary>The same for the single-precision rows the poolers return.</summary>
    public static string List(IEnumerable<float> values) =>
        "[" + string.Join(", ", values.Select(F3)) + "]";
}
