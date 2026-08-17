using Lodestar.Tests.Documentation;
using Xunit;

namespace Lodestar.Fuzzy.Tests.Documentation;

/// <summary>The reference gate for <c>Lodestar.Fuzzy</c>, whose pages arrived with #227.</summary>
/// <remarks>
/// The package had none until then, so a <c>covered</c> entry would have enforced
/// nothing — the state #204 exists to end. The checker's own fixtures live with the
/// checker and are not duplicated here.
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
            typeof(Fuzz).Assembly, "Lodestar.Fuzzy", Map, Root);

        Assert.Empty(complaints);
    }

    [Fact]
    public void Every_documented_member_named_in_the_docs_links_to_its_entry()
    {
        IReadOnlyList<string> complaints = ReferenceDocumentation.CheckLinks(
            typeof(Fuzz).Assembly, "Lodestar.Fuzzy", Map, Docs);

        Assert.Empty(complaints);
    }
}
