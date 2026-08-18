using System.Reflection;
using System.Runtime.CompilerServices;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Lodestar.Embeddings.Tokenization;
using Lodestar.Fuzzy;
using Lodestar.Metrics;
using Lodestar.Text.Distances;

namespace Lodestar.Sample;

/// <summary>
/// Fails the build when a public <em>member</em> of the four packages is not
/// reachable from this sample (ADR 0009, amended by #265). The exported surface
/// is read by reflection from the assemblies <em>NuGet resolved for this
/// project</em> — the packaged ones, not the <c>src/</c> outputs — and matched
/// against this assembly's own metadata: a <see cref="MemberReference"/>, not a
/// <see cref="TypeReference"/>, since <c>typeof(T)</c> emits only the latter.
/// An enum is the documented exception — its members never produce a member
/// reference, so naming the type is all a consumer can do.
/// </summary>
/// <remarks>
/// Type granularity hid three real gaps that shipped green — #262, #263 and
/// #264 — because one referenced member made every other overload, accessor and
/// optional parameter on that type invisible. Reverting any of those three now
/// fails this gate, which is the test that the granularity actually changed.
/// </remarks>
internal static class PackagingGate
{
    /// <summary>
    /// Types that cannot be exercised here, each with a reason a reviewer can
    /// disagree with — the standard CONTRIBUTING.md sets for analyzer
    /// suppressions. A key naming a type that no longer exists fails the gate,
    /// so this list cannot rot into a silent omission.
    /// </summary>
    /// <summary>The charsmap a normalizer needs is a model artifact, and none is committed.</summary>
    private const string NoCharsMap =
        "a precompiled charsmap is a binary trie inside a spiece.model, and model artifacts are "
        + "never committed (CONTRIBUTING.md). FromCharsMap refuses anything else — measured, an "
        + "empty blob and a four-zero-byte header are refused with different sentences — so there "
        + "is no input a sample could pass, and Normalize needs the instance it cannot build";

    /// <summary>A record's synthesised equality: compared with, never called.</summary>
    private const string RecordPlumbing =
        "a record's value equality — the synthesised member a consumer compares WITH rather than "
        + "calls, so no sample line produces a member reference to it";

    /// <summary>The awaitable twin of a loader the sample reads synchronously.</summary>
    private const string AsyncCounterpart =
        "the asynchronous counterpart of a loader the sample calls synchronously. A console sample "
        + "reading a committed fixture has no honest reason to await, and calling both would "
        + "demonstrate the API twice rather than the package once";

    /// <summary>A result record the library builds and the sample only reads.</summary>
    private const string ResultRecordCtor =
        "a result record the library CONSTRUCTS and the sample reads. Its properties are exercised; "
        + "constructing one by hand is what a consumer never does";

    private static readonly Dictionary<string, string> Excluded = new(StringComparer.Ordinal)
    {
        ["Lodestar.Embeddings.Onnx.OnnxTextEmbedder"] =
            "constructing it loads an ONNX model, and model weights are never committed "
            + "(CONTRIBUTING.md); ADR 0009 already records that the sample stops at the tokenizer",
        ["Lodestar.Metrics.UndefinedMetricException"] =
            "Lot5Metrics does catch it, under ZeroDivision.Throw — but its entire public surface "
            + "is constructors, and a consumer catches rather than constructs. A catch clause emits "
            + "a type reference and no member reference, and reading ex.Message re-parents to "
            + "System.Exception, which declares it. Same shape as the enum carve-out above: the "
            + "only use a consumer has leaves nothing for the member criterion to find",

        ["Lodestar.Embeddings.Tokenization.PrecompiledNormalizer.FromCharsMap"] = NoCharsMap,
        ["Lodestar.Embeddings.Tokenization.PrecompiledNormalizer.Normalize"] = NoCharsMap,

        ["Lodestar.Embeddings.Tokenization.AddedToken.Equals"] = RecordPlumbing,
        ["Lodestar.Embeddings.Tokenization.AddedToken.GetHashCode"] = RecordPlumbing,
        ["Lodestar.Embeddings.Tokenization.BpeVocabulary.Equals"] = RecordPlumbing,
        ["Lodestar.Embeddings.Tokenization.BpeVocabulary.GetHashCode"] = RecordPlumbing,
        ["Lodestar.Embeddings.Tokenization.PrecompiledNormalizer.Equals"] = RecordPlumbing,
        ["Lodestar.Embeddings.Tokenization.PrecompiledNormalizer.GetHashCode"] = RecordPlumbing,
        ["Lodestar.Embeddings.Tokenization.SentencePieceVocabulary.Equals"] = RecordPlumbing,
        ["Lodestar.Embeddings.Tokenization.SentencePieceVocabulary.GetHashCode"] = RecordPlumbing,
        ["Lodestar.Embeddings.Tokenization.SpecialTokenTemplate.Equals"] = RecordPlumbing,
        ["Lodestar.Embeddings.Tokenization.SpecialTokenTemplate.GetHashCode"] = RecordPlumbing,
        ["Lodestar.Embeddings.Tokenization.TokenizationResult.Equals"] = RecordPlumbing,
        ["Lodestar.Embeddings.Tokenization.TokenizationResult.GetHashCode"] = RecordPlumbing,
        ["Lodestar.Embeddings.Tokenization.WordPieceVocabulary.Equals"] = RecordPlumbing,
        ["Lodestar.Embeddings.Tokenization.WordPieceVocabulary.GetHashCode"] = RecordPlumbing,
        ["Lodestar.Metrics.ClassificationReport.ToString"] = RecordPlumbing,
        ["Lodestar.Text.Vectorization.CountVectorizerOptions.Equals"] = RecordPlumbing,
        ["Lodestar.Text.Vectorization.CountVectorizerOptions.GetHashCode"] = RecordPlumbing,
        ["Lodestar.Embeddings.Persistence.BpeFilesLoader.LoadAsync"] = AsyncCounterpart,
        ["Lodestar.Embeddings.Persistence.SentencePieceModelLoader.LoadAsync"] = AsyncCounterpart,
        ["Lodestar.Embeddings.Persistence.TokenizerJsonLoader.LoadBpeAsync"] = AsyncCounterpart,
        ["Lodestar.Embeddings.Persistence.TokenizerJsonLoader.LoadUnigramAsync"] = AsyncCounterpart,
        ["Lodestar.Embeddings.Persistence.TokenizerJsonLoader.LoadWordPieceAsync"] = AsyncCounterpart,
        ["Lodestar.Embeddings.Persistence.VocabTxtLoader.LoadAsync"] = AsyncCounterpart,
        ["Lodestar.Embeddings.Search.EmbeddingIndex.LoadAsync"] = AsyncCounterpart,
        ["Lodestar.Embeddings.Search.EmbeddingIndex.SaveAsync"] = AsyncCounterpart,
        ["Lodestar.Text.Vectorization.CountVectorizer.LoadAsync"] = AsyncCounterpart,
        ["Lodestar.Text.Vectorization.CountVectorizer.SaveAsync"] = AsyncCounterpart,
        ["Lodestar.Text.Vectorization.HashingVectorizer.LoadAsync"] = AsyncCounterpart,
        ["Lodestar.Text.Vectorization.HashingVectorizer.SaveAsync"] = AsyncCounterpart,
        ["Lodestar.Text.Vectorization.TfidfVectorizer.LoadAsync"] = AsyncCounterpart,
        ["Lodestar.Text.Vectorization.TfidfVectorizer.SaveAsync"] = AsyncCounterpart,
        ["Lodestar.Embeddings.Search.SearchResult..ctor"] = ResultRecordCtor,
        ["Lodestar.Embeddings.Tokenization.AddedToken..ctor"] = ResultRecordCtor,
        ["Lodestar.Embeddings.Tokenization.SpecialTokenTemplate..ctor"] = ResultRecordCtor,
        ["Lodestar.Embeddings.Tokenization.TokenizationResult..ctor"] = ResultRecordCtor,
        ["Lodestar.Embeddings.Tokenization.WordPieceVocabulary..ctor"] = ResultRecordCtor,
        ["Lodestar.Fuzzy.ExtractResult..ctor"] = ResultRecordCtor,
        ["Lodestar.Metrics.AverageRow..ctor"] = ResultRecordCtor,
        ["Lodestar.Metrics.ClassRow..ctor"] = ResultRecordCtor,
        ["Lodestar.Text.Vectorization.CsrMatrix..ctor"] = ResultRecordCtor,
    };

    /// <summary>What one pass over the exported surface found.</summary>
    private readonly record struct Surface(HashSet<string> Exported, int Covered, List<string> Uncovered)
    {
        /// <summary>How many public members were judged, exclusions aside.</summary>
        public int Members => Covered + Uncovered.Count;
    }

    /// <summary>Runs the check.</summary>
    /// <returns><c>true</c> when every exported public type is accounted for.</returns>
    public static bool Verify()
    {
        Assembly[] packaged =
        [
            typeof(Levenshtein).Assembly,
            typeof(WordPieceTokenizer).Assembly,
            typeof(Fuzz).Assembly,
            typeof(ConfusionMatrix).Assembly,
        ];

        var packagedNames = packaged.Select(a => a.GetName().Name!).ToHashSet(StringComparer.Ordinal);
        References(packagedNames, out HashSet<string> typeRefs, out HashSet<string> memberRefs);

        Surface surface = Inspect(packaged, typeRefs, memberRefs);
        string[] stale = [.. Excluded.Keys.Where(k => !surface.Exported.Contains(k)).Order(StringComparer.Ordinal)];

        Report(surface, stale);
        return surface.Uncovered.Count == 0 && stale.Length == 0;
    }

    /// <summary>Matches every exported type against what this assembly references.</summary>
    private static Surface Inspect(
        Assembly[] packaged,
        HashSet<string> typeRefs,
        HashSet<string> memberRefs)
    {
        var exported = new HashSet<string>(StringComparer.Ordinal);
        var uncovered = new List<string>();
        int covered = 0;

        foreach (Type type in packaged.SelectMany(a => a.GetExportedTypes()))
        {
            if (type.IsEnum)
            {
                InspectEnum(type, typeRefs, exported, uncovered, ref covered);
                continue;
            }

            InspectMembers(type, memberRefs, exported, uncovered, ref covered);
        }

        return new Surface(exported, covered, uncovered);
    }

    /// <summary>An enum is judged whole: naming the type is all a consumer can do.</summary>
    private static void InspectEnum(
        Type type,
        HashSet<string> typeRefs,
        HashSet<string> exported,
        List<string> uncovered,
        ref int covered)
    {
        string name = type.FullName!;
        exported.Add(name);
        if (Excluded.ContainsKey(name))
        {
            return;
        }

        if (typeRefs.Contains(name))
        {
            covered++;
        }
        else
        {
            uncovered.Add($"{name} (enum) is never named");
        }
    }

    /// <summary>Every public member of one type, against what the sample referenced.</summary>
    private static void InspectMembers(
        Type type,
        HashSet<string> memberRefs,
        HashSet<string> exported,
        List<string> uncovered,
        ref int covered)
    {
        string typeName = type.FullName!;
        exported.Add(typeName);
        bool wholeTypeExcluded = Excluded.ContainsKey(typeName);

        foreach (string member in PublicMembers(type))
        {
            string name = $"{typeName}.{member}";
            exported.Add(name);
            if (wholeTypeExcluded || Excluded.ContainsKey(name))
            {
                continue;
            }

            // A property is reached by either accessor: read in a Console line, or
            // written in an object initializer, which is how the options records are used.
            bool reached = memberRefs.Contains(name)
                || (member.StartsWith("get_", StringComparison.Ordinal)
                    && memberRefs.Contains($"{typeName}.set_{member[4..]}"));

            if (reached)
            {
                covered++;
            }
            else
            {
                uncovered.Add($"{name} is never referenced");
            }
        }
    }

    /// <summary>The public members of a type that a consumer could reference.</summary>
    /// <remarks>
    /// A property is named by its accessors in metadata, so it is offered under both;
    /// <see cref="Inspect"/> accepts either. Compiler-generated members of a record —
    /// <c>&lt;Clone&gt;$</c> and friends — are excluded, as they are from the reference
    /// gate, because a name C# cannot spell is not one a sample can call.
    /// </remarks>
    private static IEnumerable<string> PublicMembers(Type type)
    {
        const BindingFlags Declared =
            BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        foreach (MemberInfo member in type.GetMembers(Declared))
        {
            if (member.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            {
                continue;
            }

            switch (member)
            {
                case ConstructorInfo:
                    yield return ".ctor";
                    break;
                case MethodInfo { IsSpecialName: false } method:
                    yield return method.Name;
                    break;
                case PropertyInfo property:
                    yield return "get_" + property.Name;
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>Prints the tally, then one <c>::error::</c> line per problem.</summary>
    private static void Report(Surface surface, string[] stale)
    {
        Console.WriteLine("packaging gate");
        Console.WriteLine($"  exported public members: {surface.Members}");
        Console.WriteLine($"  referenced by sample   : {surface.Covered}");
        Console.WriteLine($"  documented exclusions  : {Excluded.Count}");

        foreach (string name in surface.Uncovered.Order(StringComparer.Ordinal))
        {
            Console.Error.WriteLine(
                $"::error::{name}. The sample is the only thing that proves this type is reachable "
                + "from outside its assembly once packaged. Reference a member of it in one of the "
                + "Lot*.cs files, or add it to PackagingGate.Excluded with a reason.");
        }
        foreach (string name in stale)
        {
            Console.Error.WriteLine(
                $"::error::PackagingGate.Excluded names '{name}', which no longer exists in the "
                + "packages. Remove the entry.");
        }

        if (surface.Uncovered.Count == 0 && stale.Length == 0)
        {
            Console.WriteLine("  every public member is reachable.");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Reads this assembly's own metadata for what it references in the three
    /// packages.
    /// </summary>
    /// <remarks>
    /// Compiled metadata rather than a source scan: a name in a comment, a string
    /// or a <c>using</c> is not a reference, and only the tables the compiler
    /// emitted can tell the difference.
    /// </remarks>
    private static void References(
        HashSet<string> packagedNames,
        out HashSet<string> typeRefs,
        out HashSet<string> memberRefs)
    {
        typeRefs = new HashSet<string>(StringComparer.Ordinal);
        memberRefs = new HashSet<string>(StringComparer.Ordinal);

        using var file = File.OpenRead(Assembly.GetExecutingAssembly().Location);
        using var pe = new PEReader(file);
        MetadataReader metadata = pe.GetMetadataReader();

        foreach (TypeReferenceHandle handle in metadata.TypeReferences)
        {
            if (FullNameOf(metadata, handle, packagedNames) is { } name)
            {
                typeRefs.Add(name);
            }
        }
        foreach (MemberReferenceHandle handle in metadata.MemberReferences)
        {
            EntityHandle parent = metadata.GetMemberReference(handle).Parent;
            if (parent.Kind == HandleKind.TypeReference
                && FullNameOf(metadata, (TypeReferenceHandle)parent, packagedNames) is { } name)
            {
                memberRefs.Add($"{name}.{metadata.GetString(metadata.GetMemberReference(handle).Name)}");
            }
        }
    }

    /// <summary>
    /// The full name of a type reference, or <c>null</c> when it does not resolve
    /// to one of the three packages.
    /// </summary>
    private static string? FullNameOf(
        MetadataReader metadata,
        TypeReferenceHandle handle,
        HashSet<string> packagedNames)
    {
        TypeReference reference = metadata.GetTypeReference(handle);
        string name = metadata.GetString(reference.Name);

        switch (reference.ResolutionScope.Kind)
        {
            case HandleKind.TypeReference:
                // A nested type: qualify it with its declaring type, the way
                // Type.FullName does.
                string? declaring = FullNameOf(metadata, (TypeReferenceHandle)reference.ResolutionScope, packagedNames);
                return declaring is null ? null : declaring + "+" + name;

            case HandleKind.AssemblyReference:
                var assembly = metadata.GetAssemblyReference((AssemblyReferenceHandle)reference.ResolutionScope);
                if (!packagedNames.Contains(metadata.GetString(assembly.Name)))
                {
                    return null;
                }
                string @namespace = metadata.GetString(reference.Namespace);
                return string.IsNullOrEmpty(@namespace) ? name : @namespace + "." + name;

            default:
                return null;
        }
    }
}
