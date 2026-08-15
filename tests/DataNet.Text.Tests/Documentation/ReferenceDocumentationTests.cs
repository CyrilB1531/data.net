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
}
