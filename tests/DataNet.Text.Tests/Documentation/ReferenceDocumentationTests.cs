using System.Reflection;
using DataNet.Tests.Documentation;
using DataNet.Text.Distances;
using Xunit;

namespace DataNet.Text.Tests.Documentation;

public sealed class ReferenceDocumentationTests
{
    private static string Root => Path.Combine(AppContext.BaseDirectory, "reference");

    private static string Map => Path.Combine(AppContext.BaseDirectory, "wiki-map.json");

    [Fact]
    public void Every_covered_namespace_is_documented()
    {
        IReadOnlyList<string> complaints = ReferenceDocumentation.Check(
            typeof(Levenshtein).Assembly, "DataNet.Text", Map, Root);

        Assert.Empty(complaints);
    }

    [Fact]
    public void A_signature_reads_the_way_a_page_writes_it()
    {
        MethodInfo method = typeof(Levenshtein)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(candidate => candidate.Name == "Distance" && !candidate.IsGenericMethodDefinition);

        Assert.Equal(
            "public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, " +
            "TextElement element = TextElement.Utf16Unit)",
            ReferenceDocumentation.RenderSignature(method));
    }

    [Fact]
    public void A_missing_entry_is_reported_with_the_member_that_lacks_it()
    {
        string page = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(page);
        File.WriteAllText(Path.Combine(page, "empty.md"), "# Empty\n");
        string map = Path.Combine(page, "wiki-map.json");
        File.WriteAllText(map, """
            {"root":[],"packages":{"DataNet.Text":{"wiki":"Text","pages":[],
             "covered":{"DataNet.Text.Distances":"empty.md"}}}}
            """);

        IReadOnlyList<string> complaints = ReferenceDocumentation.Check(
            typeof(Levenshtein).Assembly, "DataNet.Text", map, page);

        Assert.Contains(complaints, complaint => complaint.Contains("Levenshtein", StringComparison.Ordinal));
        Directory.Delete(page, recursive: true);
    }

    [Fact]
    public void A_hard_wrapped_parameter_is_captured_and_a_remarks_backtick_is_not()
    {
        // The plan's own worked example: `unit` is described mid-paragraph, and
        // `Jaccard` / `JaroWinkler` are backticked in Remarks, not in Parameters.
        const string text = """
            #### Levenshtein.Distance

            Counts the fewest insertions, deletions and substitutions that turn one string into the other.

            <!-- docs-declaration -->

            ```csharp
            public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
            public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement unit)
            ```

            **Parameters** — `a` and `b` are the two strings to compare; a `string` converts implicitly, so
            nothing is allocated for them. `unit` says what counts as one character: `TextElement.Utf16` by
            default, the native and fastest choice, or `TextElement.CodePoint` to match Python outside the Basic
            Multilingual Plane.

            **Returns** — `int`, the number of edits. Zero when the two are equal, and never negative.

            **Remarks** — this is the ordinary answer to "how different are these two texts", and the right
            tool for typing mistakes and mis-keyed names. To compare sets of words rather than characters,
            `Jaccard` is the better fit; to weight a common prefix, `JaroWinkler`.

            **Applies to** — net10.0, netstandard2.0.

            **See also** — `Levenshtein.NormalizedSimilarity`, `Indel.Distance`, `DamerauLevenshtein.Distance`,
            the [Python equivalence table](../../equivalence.md).
            """;

        ReferenceDocumentation.Page page = ReferenceDocumentation.Page.Parse(text);

        Assert.True(page.Entries.ContainsKey("Levenshtein.Distance"));
        ReferenceDocumentation.Entry entry = page.Entries["Levenshtein.Distance"];
        Assert.Contains("unit", entry.Parameters);
        Assert.DoesNotContain("Jaccard", entry.Parameters);
        Assert.DoesNotContain("JaroWinkler", entry.Parameters);
    }
}
