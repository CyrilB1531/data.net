using System.Reflection;
using Lodestar.Tests.Documentation;
using Lodestar.Tests.Documentation.Fixtures;
using Lodestar.Text.Distances;
using Xunit;

namespace Lodestar.Text.Tests.Documentation;

public sealed class ReferenceDocumentationTests
{
    private static string Root => Path.Combine(AppContext.BaseDirectory, "reference");

    private static string Map => Path.Combine(AppContext.BaseDirectory, "wiki-map.json");

    private static string Docs => Path.Combine(AppContext.BaseDirectory, "docs");

    [Fact]
    public void Every_covered_namespace_is_documented()
    {
        IReadOnlyList<string> complaints = ReferenceDocumentation.Check(
            typeof(Levenshtein).Assembly, "Lodestar.Text", Map, Root);

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
    public void A_base_class_constraint_comes_before_the_interfaces_beside_it()
    {
        // C# refuses the other order, so sorting the constraint types made the gate
        // demand a declaration that does not compile.
        Assert.Equal(
            "public static T Pick<T>(T left, T right) where T : Random, IComparable<T>",
            RenderFixture(nameof(ConstraintFixture.Pick)));
    }

    [Fact]
    public void A_constructor_constraint_comes_last()
    {
        Assert.Equal(
            "public static T Fresh<T>() where T : class, new()",
            RenderFixture(nameof(ConstraintFixture.Fresh)));
    }

    [Fact]
    public void A_value_type_constraint_reads_as_struct()
    {
        Assert.Equal(
            "public static T Zero<T>() where T : struct",
            RenderFixture(nameof(ConstraintFixture.Zero)));
    }

    [Fact]
    public void An_unmanaged_constraint_is_not_documented_as_struct()
    {
        Assert.Equal(
            "public static int Size<T>(T value) where T : unmanaged",
            RenderFixture(nameof(ConstraintFixture.Size)));
    }

    [Fact]
    public void A_notnull_constraint_reaches_the_clause()
    {
        Assert.Equal(
            "public static string Name<T>(T value) where T : notnull",
            RenderFixture(nameof(ConstraintFixture.Name)));
    }

    [Fact]
    public void An_interface_constraint_alone_does_not_become_notnull()
    {
        // Under a `NullableContext(1)` type, a parameter that is not `notnull` carries an
        // explicit 0 of its own -- which is why inheriting the 1 can be read as `notnull`.
        Assert.Equal(
            "public static int Only<T>(T value) where T : IComparable<T>",
            RenderFixture(nameof(ConstraintFixture.Only)));
    }

    [Fact]
    public void Two_parameters_get_a_clause_each()
    {
        Assert.Equal(
            "public static bool Pair<TKey, TValue>(TKey key, TValue value) " +
            "where TKey : notnull where TValue : class",
            RenderFixture(nameof(ConstraintFixture.Pair)));
    }

    private static string RenderFixture(string name) => ReferenceDocumentation.RenderSignature(
        typeof(ConstraintFixture)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(candidate => candidate.Name == name));

    [Fact]
    public void A_missing_entry_is_reported_with_the_member_that_lacks_it()
    {
        string page = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(page);
        File.WriteAllText(Path.Combine(page, "empty.md"), "# Empty\n");
        string map = Path.Combine(page, "wiki-map.json");
        File.WriteAllText(map, """
            {"root":[],"packages":{"Lodestar.Text":{"wiki":"Text","pages":[],
             "covered":{"Lodestar.Text.Distances":"empty.md"}}}}
            """);

        IReadOnlyList<string> complaints = ReferenceDocumentation.Check(
            typeof(Levenshtein).Assembly, "Lodestar.Text", map, page);

        Assert.Contains(complaints, complaint => complaint.Contains("Levenshtein", StringComparison.Ordinal));
        Directory.Delete(page, recursive: true);
    }

    [Fact]
    public void A_type_the_index_does_not_link_is_reported()
    {
        // The real tree, the index's one link to the Levenshtein page undone -- everything
        // else stays exactly as published, so only this check has anything to say.
        string root = CopyReference();
        string index = Path.Combine(root, "distances.md");
        string original = File.ReadAllText(index);
        string broken = original.Replace(
            "[`Levenshtein`](distances/levenshtein.md)", "`Levenshtein`", StringComparison.Ordinal);
        Assert.NotEqual(original, broken);
        File.WriteAllText(index, broken);

        IReadOnlyList<string> complaints = ReferenceDocumentation.Check(
            typeof(Levenshtein).Assembly, "Lodestar.Text", Path.Combine(root, "wiki-map.json"), root);

        Assert.Contains(complaints, complaint =>
            complaint.Contains("Levenshtein", StringComparison.Ordinal) &&
            complaint.Contains("type table", StringComparison.Ordinal));
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_member_its_type_page_does_not_link_is_reported()
    {
        // The level the split adds: a member page can now exist and be reachable from
        // nothing, which a section of a combined page could not.
        string root = CopyReference();
        string page = Path.Combine(root, "distances", "levenshtein.md");
        File.WriteAllText(page, File.ReadAllText(page).Replace(
            "[`Levenshtein.Distance`](levenshtein-distance.md)",
            "`Levenshtein.Distance`",
            StringComparison.Ordinal));

        IReadOnlyList<string> complaints = ReferenceDocumentation.Check(
            typeof(Levenshtein).Assembly, "Lodestar.Text", Path.Combine(root, "wiki-map.json"), root);

        Assert.Contains(complaints, complaint =>
            complaint.Contains("Levenshtein.Distance", StringComparison.Ordinal) &&
            complaint.Contains("member table", StringComparison.Ordinal));
        Directory.Delete(root, recursive: true);
    }

    /// <summary>The published reference, copied so a test can break one page of it.</summary>
    private static string CopyReference()
    {
        string root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(root);

        // Every namespace's directory, rather than a named one: this copied only
        // "distances" until Vectorization became the second covered namespace (#225).
        foreach (string path in Directory.GetFiles(Root, "*.md", SearchOption.AllDirectories))
        {
            string destination = Path.Combine(root, Path.GetRelativePath(Root, path));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(path, destination);
        }

        File.Copy(Map, Path.Combine(root, "wiki-map.json"));
        return root;
    }

    [Fact]
    public void A_hard_wrapped_parameter_is_captured_and_a_remarks_backtick_is_not()
    {
        // The Levenshtein.Distance entry verbatim: `element` described mid-paragraph,
        // `Jaccard` backticked after Parameters closed, an Example fence between them.
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
            using Lodestar.Text.Distances;

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
            typeof(Levenshtein).Assembly, "Lodestar.Text", Map, Root, Docs);

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

    [Fact]
    public void One_namespace_may_be_split_over_several_pages()
    {
        // Cabinet on one page, Slip on the other: under a single-page `covered`
        // each would owe the other's type, and Lodestar.Metrics could not be split.
        IReadOnlyList<string> complaints = CheckFixtures(("cabinet.md", CabinetPage), ("slip.md", SlipPage));

        Assert.Empty(complaints);
    }

    [Fact]
    public void A_type_with_an_entry_on_no_page_is_still_reported()
    {
        // Only cabinet.md is declared, so Slip has an entry nowhere. The relaxation
        // of the previous test must not have turned into "some page will have it".
        IReadOnlyList<string> complaints = CheckFixtures(("cabinet.md", CabinetPage));

        string complaint = Assert.Single(complaints);
        Assert.Contains("no entry for the type Slip", complaint, StringComparison.Ordinal);
        Assert.Contains("cabinet.md", complaint, StringComparison.Ordinal);
    }

    [Fact]
    public void A_type_table_row_belongs_on_the_page_that_carries_the_entry()
    {
        // slip.md keeps its Slip entry and loses the link in its own table. The
        // complaint has to name slip.md, not cabinet.md, which also has a table.
        IReadOnlyList<string> complaints = CheckFixtures(
            ("cabinet.md", CabinetPage),
            ("slip.md", SlipPage.Replace("[`Slip`](#slip)", "`Slip`", StringComparison.Ordinal)));

        string complaint = Assert.Single(complaints);
        Assert.StartsWith("slip.md:", complaint, StringComparison.Ordinal);
        Assert.Contains("opening table", complaint, StringComparison.Ordinal);
    }

    [Fact]
    public void A_nested_exported_type_is_owed_no_entry_of_its_own()
    {
        // Cabinet.Drawer is exported and nested. Neither page mentions it, and the
        // pair above is silent — which is the whole of the rule.
        Assert.Contains(typeof(Cabinet.Drawer), typeof(Cabinet).Assembly.GetExportedTypes());
        Assert.True(typeof(Cabinet.Drawer).IsNested);

        IReadOnlyList<string> complaints = CheckFixtures(("cabinet.md", CabinetPage), ("slip.md", SlipPage));

        Assert.DoesNotContain(complaints, complaint => complaint.Contains("Drawer", StringComparison.Ordinal));
    }

    [Fact]
    public void A_records_synthesised_methods_are_owed_no_entry()
    {
        // Slip's page describes the record and no method of it. `<Clone>$` is the
        // one that makes this more than a convenience: it is not a name C# can spell.
        Assert.Contains(
            typeof(Slip).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            method => method.Name == "<Clone>$");

        IReadOnlyList<string> complaints = CheckFixtures(("cabinet.md", CabinetPage), ("slip.md", SlipPage));

        Assert.DoesNotContain(complaints, complaint => complaint.Contains("Slip.", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(nameof(Cabinet.Measure), "public static double[] Measure(int count)")]
    [InlineData(nameof(Cabinet.Grid), "public static double[,] Grid(int rows)")]
    public void An_array_return_reads_as_a_reader_writes_it(string name, string expected)
    {
        // Reflection spells these Double[] and Double[,]; no member of the distances
        // page returns an array, so the renderer went unmeasured until Lodestar.Metrics.
        MethodInfo method = typeof(Cabinet).GetMethod(name)!;

        Assert.Equal(expected, ReferenceDocumentation.RenderSignature(method));
    }

    private const string CabinetPage = """
        # Cabinet

        | Type | What it is |
        | --- | --- |
        | [`Cabinet`](#cabinet) | A fixture. |

        ### Cabinet

        A fixture type.

        #### Cabinet.Grid

        A rectangular reading.

        <!-- docs-declaration -->

        ```csharp
        public static double[,] Grid(int rows)
        ```

        **Parameters** — `rows` is the side of the square.

        **Applies to** — net10.0, netstandard2.0.

        #### Cabinet.Measure

        One number per drawer.

        <!-- docs-declaration -->

        ```csharp
        public static double[] Measure(int count)
        ```

        **Parameters** — `count` is how many drawers.

        **Applies to** — net10.0, netstandard2.0.
        """;

    private const string SlipPage = """
        # Slip

        | Type | What it is |
        | --- | --- |
        | [`Slip`](#slip) | A record fixture. |

        ### Slip

        A record, described by its own declaration.
        """;

    /// <summary>Runs the gate over the fixture namespace, with `covered` as a list of pages.</summary>
    private static IReadOnlyList<string> CheckFixtures(params (string Name, string Markdown)[] pages)
    {
        string root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        try
        {
            foreach ((string name, string markdown) in pages)
            {
                File.WriteAllText(Path.Combine(root, name), markdown);
            }

            // A placeholder rather than interpolation: the map ends on four closing
            // braces, which no number of '$' can tell apart from an interpolation hole.
            const string template = """
                {"root":[],"packages":{"Lodestar.Text":{"wiki":"Text","pages":[],
                 "covered":{"Lodestar.Tests.Documentation.Fixtures":[PAGES]}}}}
                """;

            string map = Path.Combine(root, "wiki-map.json");
            string listed = string.Join(",", pages.Select(page => $"\"{page.Name}\""));
            File.WriteAllText(map, template.Replace("PAGES", listed, StringComparison.Ordinal));

            return ReferenceDocumentation.Check(typeof(Cabinet).Assembly, "Lodestar.Text", map, root);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void A_guide_sharing_a_name_with_a_reference_page_is_still_checked()
    {
        // Excluding by BARE FILE NAME let a guide named like an index leave the gate
        // exactly when its members became linkable, and one did (#238).
        string docs = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(docs, "guides"));
        try
        {
            // Named after a real reference index — "distances.md" is one — and naming a
            // member it never links.
            File.WriteAllText(
                Path.Combine(docs, "guides", "distances.md"),
                "# A guide\n\nIt calls `Levenshtein.Distance` and links nothing.\n");

            IReadOnlyList<string> complaints = ReferenceDocumentation.CheckLinks(
                typeof(Levenshtein).Assembly, "Lodestar.Text", Map, Root, docs);

            Assert.Contains(complaints, complaint =>
                complaint.Contains("guides/distances.md", StringComparison.Ordinal) &&
                complaint.Contains("Levenshtein.Distance", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(docs, recursive: true);
        }
    }

    private static IReadOnlyList<string> OnePage(string markdown)
    {
        string docs = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(docs);
        try
        {
            File.WriteAllText(Path.Combine(docs, "page.md"), markdown);
            return ReferenceDocumentation.CheckLinks(
                typeof(Levenshtein).Assembly, "Lodestar.Text", Map, Root, docs);
        }
        finally
        {
            Directory.Delete(docs, recursive: true);
        }
    }
}
