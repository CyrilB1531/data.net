using System.Reflection;
using DataNet.Tests.Documentation;
using DataNet.Text.Distances;
using Xunit;

namespace DataNet.Text.Tests.Documentation;

public sealed class ReferenceDocumentationTests
{
    private static string Root => Path.Combine(AppContext.BaseDirectory, "reference");

    private static string Map => Path.Combine(AppContext.BaseDirectory, "wiki-map.json");

    private static string Docs => Path.Combine(AppContext.BaseDirectory, "docs");

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
    public void A_generic_signature_carries_its_arity_and_its_constraints()
    {
        MethodInfo method = typeof(Levenshtein)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(candidate => candidate.Name == "Distance" && candidate.IsGenericMethodDefinition);

        // Without the arity and the where clause this reads as a declaration naming an
        // unbound T, which does not compile — and the gate would demand a page write it.
        Assert.Equal(
            "public static int Distance<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b) where T : IEquatable<T>",
            ReferenceDocumentation.RenderSignature(method));
    }

    [Fact]
    public void A_by_ref_parameter_keeps_the_keyword_it_was_declared_with()
    {
        MethodInfo method = typeof(ByRefFixture)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(candidate => candidate.Name == nameof(ByRefFixture.TryMeasure));

        Assert.Equal(
            "public static bool TryMeasure(in ReadOnlySpan<char> text, ref int budget, out int length)",
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
    public void A_type_missing_from_the_opening_table_link_is_reported()
    {
        // The real page, its one link to the Levenshtein entry undone -- everything
        // else stays exactly as published, so only this check has anything to say.
        string page = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(page);
        string original = File.ReadAllText(Path.Combine(Root, "distances.md"));
        string broken = original.Replace(
            "[`Levenshtein`](#levenshtein)", "`Levenshtein`", StringComparison.Ordinal);
        Assert.NotEqual(original, broken);
        File.WriteAllText(Path.Combine(page, "distances.md"), broken);
        File.Copy(Map, Path.Combine(page, "wiki-map.json"));

        IReadOnlyList<string> complaints = ReferenceDocumentation.Check(
            typeof(Levenshtein).Assembly, "DataNet.Text", Path.Combine(page, "wiki-map.json"), page);

        Assert.Contains(complaints, complaint =>
            complaint.Contains("Levenshtein", StringComparison.Ordinal) &&
            complaint.Contains("opening table", StringComparison.Ordinal));
        Directory.Delete(page, recursive: true);
    }

    [Fact]
    public void A_hard_wrapped_parameter_is_captured_and_a_remarks_backtick_is_not()
    {
        // long-comment: a fixture copied out of a page is only worth copying if a
        // reviewer can see which properties of the original it pins
        // The Levenshtein.Distance entry of docs/reference/text/distances.md, verbatim.
        // It is inlined rather than read from disk so those properties are visible here:
        // `element` is described mid-paragraph, three lines into Parameters; `Jaccard`
        // and `JaroWinkler` are backticked in Remarks, after that block has closed; and
        // an Example fence sits between the two, which must not reopen the declaration.
        const string text = """
            #### Levenshtein.Distance

            Counts the fewest insertions, deletions and substitutions that turn one string into the other.

            <!-- docs-declaration -->

            ```csharp
            public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
            public static int Distance<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b) where T : IEquatable<T>
            ```

            **Parameters** — `a` and `b` are the two strings to compare; a `string` converts implicitly, so
            nothing is allocated for them. `element` says what counts as one character:
            `TextElement.Utf16Unit` by default, the native and fastest choice, or `TextElement.CodePoint` to
            match Python outside the Basic Multilingual Plane.

            **Returns** — `int`, the number of edits. Zero when the two are equal, and never negative.

            **Example** — the textbook pair: two substitutions and one insertion.

            ```csharp
            using DataNet.Text.Distances;

            int d = Levenshtein.Distance("kitten", "sitting");   // => 3
            ```

            **Remarks** — this is the ordinary answer to "how different are these two texts", and the right
            tool for typing mistakes and mis-keyed names. To compare sets of words rather than characters,
            `Jaccard` is the better fit; to weight a common prefix, `JaroWinkler`.

            The trap is that the result is not bounded. Three edits are enormous between two six-letter words
            and negligible between two paragraphs, so a raw distance cannot be compared across pairs of
            different lengths — `NormalizedSimilarity` is what you want for a score in `[0, 1]`.

            **Applies to** — net10.0, netstandard2.0.

            **See also** — `Levenshtein.NormalizedSimilarity`, `Indel.Distance`, `DamerauLevenshtein.Distance`,
            the [Python equivalence table](../../equivalence.md).
            """;

        ReferenceDocumentation.Page page = ReferenceDocumentation.Page.Parse(text);

        Assert.True(page.Entries.ContainsKey("Levenshtein.Distance"));
        ReferenceDocumentation.Entry entry = page.Entries["Levenshtein.Distance"];
        Assert.Contains("element", entry.Parameters);
        Assert.DoesNotContain("Jaccard", entry.Parameters);
        Assert.DoesNotContain("JaroWinkler", entry.Parameters);

        // The Example fence follows the declaration block, so only the declaration's
        // own two lines may be read back as signatures.
        string[] declarations =
        [
            "public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, " +
            "TextElement element = TextElement.Utf16Unit)",
            "public static int Distance<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b) where T : IEquatable<T>",
        ];

        Assert.Equal(declarations, entry.Declarations);
    }

    [Fact]
    public void Every_documented_member_named_in_the_docs_links_to_its_entry()
    {
        IReadOnlyList<string> complaints = ReferenceDocumentation.CheckLinks(
            typeof(Levenshtein).Assembly, "DataNet.Text", Map, Root, Docs);

        Assert.Empty(complaints);
    }

    [Fact]
    public void A_backticked_member_outside_a_link_is_reported()
    {
        // The page already links the entry once, so only the second mention is at issue:
        // this pins the first obligation on its own, with the second one satisfied.
        IReadOnlyList<string> complaints = OnePage("""
            Read [`Levenshtein.Distance`](reference/text/distances.md#levenshteindistance) first.

            Then reach for `Levenshtein.Distance` on typing mistakes.
            """);

        string complaint = Assert.Single(complaints);
        Assert.Contains("page.md:3", complaint, StringComparison.Ordinal);
        Assert.Contains("not linked", complaint, StringComparison.Ordinal);
    }

    [Fact]
    public void A_member_used_only_inside_a_fence_still_owes_the_page_a_link()
    {
        IReadOnlyList<string> complaints = OnePage("""
            # Quickstart

            ```csharp
            int d = Levenshtein.Distance("kitten", "sitting");
            ```
            """);

        string complaint = Assert.Single(complaints);
        Assert.Contains("page.md", complaint, StringComparison.Ordinal);
        Assert.Contains("Levenshtein.Distance", complaint, StringComparison.Ordinal);
    }

    [Fact]
    public void One_link_in_prose_covers_every_later_use_in_a_fence()
    {
        IReadOnlyList<string> complaints = OnePage("""
            [`Levenshtein.Distance`](reference/text/distances.md#levenshteindistance) counts edits.

            ```csharp
            int d = Levenshtein.Distance("kitten", "sitting");
            ```

            ```csharp
            int e = Levenshtein.Distance("a", "b");
            ```

            ```csharp
            int f = Levenshtein.Distance("", "");
            ```
            """);

        Assert.Empty(complaints);
    }

    [Fact]
    public void A_near_namesake_is_not_read_as_a_use_of_the_shorter_name()
    {
        // `DamerauLevenshtein.Distance` ends with `Levenshtein.Distance`, and a
        // substring match would have this page owe a link it does not owe.
        IReadOnlyList<string> complaints = OnePage("""
            [`DamerauLevenshtein.Distance`](reference/text/distances.md#dameraulevenshteindistance) swaps.

            ```csharp
            int d = DamerauLevenshtein.Distance("CA", "ABC");
            ```
            """);

        Assert.Empty(complaints);
    }

    private static IReadOnlyList<string> OnePage(string markdown)
    {
        string docs = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(docs);
        try
        {
            File.WriteAllText(Path.Combine(docs, "page.md"), markdown);
            return ReferenceDocumentation.CheckLinks(
                typeof(Levenshtein).Assembly, "DataNet.Text", Map, Root, docs);
        }
        finally
        {
            Directory.Delete(docs, recursive: true);
        }
    }
}
