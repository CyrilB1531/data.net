using Lodestar.Embeddings.Onnx;
using Lodestar.Tests.Documentation;
using Xunit;

namespace Lodestar.Embeddings.Tests.Documentation;

/// <summary>
/// The reference gate for <c>Lodestar.Embeddings</c>, whose first covered namespace
/// arrived with #235.
/// </summary>
/// <remarks>
/// Until then this package had none, so a <c>covered</c> entry would have been prose
/// nothing read — the state #204 exists to end rather than reproduce. The checker's
/// own fixtures are not duplicated here: it is one shared file, tested where it lives.
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
            typeof(OnnxTextEmbedder).Assembly, "Lodestar.Embeddings", Map, Root);

        Assert.Empty(complaints);
    }

    [Fact]
    public void Every_documented_member_named_in_the_docs_links_to_its_entry()
    {
        IReadOnlyList<string> complaints = ReferenceDocumentation.CheckLinks(
            typeof(OnnxTextEmbedder).Assembly, "Lodestar.Embeddings", Map, Docs);

        Assert.Empty(complaints);
    }
}
