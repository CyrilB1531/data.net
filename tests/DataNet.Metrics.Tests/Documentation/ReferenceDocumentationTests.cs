using DataNet.Tests.Documentation;
using Xunit;

namespace DataNet.Metrics.Tests.Documentation;

/// <summary>
/// The gate of D7 and the linking rule of D9, over the two pages
/// <c>DataNet.Metrics</c> declares covered.
/// </summary>
/// <remarks>
/// The engine and its own unit tests live with <c>DataNet.Text</c>; what is here is
/// this package's half — its namespace against its pages, on whichever build of the
/// library the surrounding project references.
/// </remarks>
public sealed class ReferenceDocumentationTests
{
    private static string Root => Path.Combine(AppContext.BaseDirectory, "reference");

    private static string Map => Path.Combine(AppContext.BaseDirectory, "wiki-map.json");

    private static string Docs => Path.Combine(AppContext.BaseDirectory, "docs");

    [Fact]
    public void Every_covered_namespace_is_documented()
    {
        IReadOnlyList<string> complaints = ReferenceDocumentation.Check(
            typeof(ConfusionMatrix).Assembly, "DataNet.Metrics", Map, Root);

        Assert.Empty(complaints);
    }

    [Fact]
    public void Every_documented_member_named_in_the_docs_links_to_its_entry()
    {
        IReadOnlyList<string> complaints = ReferenceDocumentation.CheckLinks(
            typeof(ConfusionMatrix).Assembly, "DataNet.Metrics", Map, Root, Docs);

        Assert.Empty(complaints);
    }

    [Fact]
    public void Neither_page_covers_the_namespace_on_its_own()
    {
        // The point of the list `covered` takes, and the check that the relaxation
        // did not become "some page will have it": declared alone, in the scalar
        // form the other three packages still use, classification.md owes R2 too.
        string map = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        File.WriteAllText(map, """
            {"root":[],"packages":{"DataNet.Metrics":{"wiki":"Metrics","pages":[],
             "covered":{"DataNet.Metrics":"docs/reference/metrics/classification.md"}}}}
            """);

        try
        {
            IReadOnlyList<string> complaints = ReferenceDocumentation.Check(
                typeof(ConfusionMatrix).Assembly, "DataNet.Metrics", map, Root);

            Assert.Contains(complaints, complaint =>
                complaint.Contains("no entry for the type R2", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(map);
        }
    }

    [Fact]
    public void The_map_declares_both_pages()
    {
        // What the two tests above check the pages against; a page dropped from the
        // list would make them pass by covering nothing.
        string map = File.ReadAllText(Map);

        Assert.Contains("docs/reference/metrics/classification.md", map, StringComparison.Ordinal);
        Assert.Contains("docs/reference/metrics/regression.md", map, StringComparison.Ordinal);
    }
}
