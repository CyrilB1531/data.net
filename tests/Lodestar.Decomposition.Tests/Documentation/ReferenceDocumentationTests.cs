using Lodestar.Tests.Documentation;
using Xunit;

namespace Lodestar.Decomposition.Tests.Documentation;

/// <summary>
/// The gate of D7 and the linking rule of D9, over the page <c>Lodestar.Decomposition</c>
/// declares covered.
/// </summary>
/// <remarks>
/// The engine and its own unit tests live with <c>Lodestar.Text</c>; what is here is this
/// package's half — its namespace against its pages, on whichever build of the library the
/// surrounding project references.
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
            typeof(TruncatedSvd).Assembly, "Lodestar.Decomposition", Map, Root);

        Assert.Empty(complaints);
    }

    [Fact]
    public void Every_documented_member_named_in_the_docs_links_to_its_entry()
    {
        IReadOnlyList<string> complaints = ReferenceDocumentation.CheckLinks(
            typeof(TruncatedSvd).Assembly, "Lodestar.Decomposition", Map, Docs);

        Assert.Empty(complaints);
    }
}
