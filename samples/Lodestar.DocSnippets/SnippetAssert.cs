using System.Globalization;

namespace Lodestar.DocSnippets;

/// <summary>
/// The check a <c>// =&gt;</c> comment in a reference page becomes.
/// </summary>
internal static class SnippetAssert
{
    /// <summary>
    /// Compares a value against what the page promises its reader.
    /// </summary>
    /// <param name="actual">The value the snippet produced.</param>
    /// <param name="expected">The text written after <c>// =&gt;</c> in the page.</param>
    /// <param name="origin">The page and line the promise is on.</param>
    /// <exception cref="InvalidOperationException">The value and the promise disagree.</exception>
    public static void Value(object? actual, string expected, string origin)
    {
        string rendered = actual switch
        {
            null => "null",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => actual.ToString() ?? "null",
        };

        // A trailing ellipsis means the page truncated an irrational result, which is
        // how the guides already write 0.5714… — so it is a prefix, not an equality.
        bool ok = expected.EndsWith('…')
            ? rendered.StartsWith(expected.TrimEnd('…'), StringComparison.Ordinal)
            : string.Equals(rendered, expected, StringComparison.Ordinal);

        if (!ok)
        {
            throw new InvalidOperationException(
                $"{origin}: the page promises '{expected}', the code produced '{rendered}'.");
        }
    }
}

/// <summary>
/// Marks a snippet that compiles but must not be executed.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class SnippetSkipRunAttribute(string reason) : Attribute
{
    /// <summary>Why this snippet cannot run — an ONNX model, a file, a network call.</summary>
    public string Reason { get; } = reason;
}
