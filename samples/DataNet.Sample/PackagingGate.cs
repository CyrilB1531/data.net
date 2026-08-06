using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using DataNet.Embeddings.Tokenization;
using DataNet.Fuzzy;
using DataNet.Text.Distances;

namespace DataNet.Sample;

/// <summary>
/// Fails the build when a public type of the three packages is not reachable
/// from this sample.
/// </summary>
/// <remarks>
/// <para>
/// ADR 0009 gives the sample one job: catch "a type that is <c>public</c> in
/// source but unreachable from outside the assembly". That guarantee only covers
/// types the sample actually references, so for every other public type the CI
/// job was green by construction — it cannot fail on a type it never mentions.
/// At the time this landed, 14 of 58 exported types were covered, and #65 and #66
/// had just added seven more without anyone noticing.
/// </para>
/// <para>
/// Enumerating by hand closes the gap until the next type lands. This closes it
/// permanently: the exported surface is read by reflection from the assemblies
/// <em>NuGet resolved for this project</em> — the packaged ones, not the
/// <c>src/</c> project outputs — and matched against what this assembly's own
/// metadata references.
/// </para>
/// <para>
/// The criterion is a <see cref="MemberReference"/>, not a
/// <see cref="TypeReference"/>: <c>typeof(T)</c> emits the latter and not the
/// former, so a type mentioned only to read an assembly attribute does not count
/// as exercised. Enums are the documented exception — an enum member is a
/// compile-time constant and never produces a member reference, so naming one is
/// all a consumer can do.
/// </para>
/// </remarks>
internal static class PackagingGate
{
    /// <summary>
    /// Types that cannot be exercised here, each with a reason a reviewer can
    /// disagree with — the standard CONTRIBUTING.md sets for analyzer
    /// suppressions. A key naming a type that no longer exists fails the gate,
    /// so this list cannot rot into a silent omission.
    /// </summary>
    private static readonly Dictionary<string, string> Excluded = new(StringComparer.Ordinal)
    {
        ["DataNet.Embeddings.Onnx.OnnxTextEmbedder"] =
            "constructing it loads an ONNX model, and model weights are never committed "
            + "(CONTRIBUTING.md); ADR 0009 already records that the sample stops at the tokenizer",
    };

    /// <summary>Runs the check.</summary>
    /// <returns><c>true</c> when every exported public type is accounted for.</returns>
    public static bool Verify()
    {
        Assembly[] packaged =
        [
            typeof(Levenshtein).Assembly,
            typeof(WordPieceTokenizer).Assembly,
            typeof(Fuzz).Assembly,
        ];

        var packagedNames = packaged.Select(a => a.GetName().Name!).ToHashSet(StringComparer.Ordinal);
        References(packagedNames, out HashSet<string> typeRefs, out HashSet<string> memberRefParents);

        var uncovered = new List<string>();
        var exported = new HashSet<string>(StringComparer.Ordinal);
        int covered = 0;

        foreach (Assembly assembly in packaged)
        {
            foreach (Type type in assembly.GetExportedTypes())
            {
                string name = type.FullName!;
                exported.Add(name);
                if (Excluded.ContainsKey(name))
                {
                    continue;
                }

                // An enum member is a compile-time constant, so referencing one
                // leaves a type reference and no member reference. Everything
                // else must show a member.
                bool reached = type.IsEnum ? typeRefs.Contains(name) : memberRefParents.Contains(name);
                if (reached)
                {
                    covered++;
                }
                else
                {
                    uncovered.Add(type.IsEnum
                        ? $"{name} (enum) is never named"
                        : $"{name} has no member referenced");
                }
            }
        }

        string[] stale = [.. Excluded.Keys.Where(k => !exported.Contains(k)).Order(StringComparer.Ordinal)];

        Console.WriteLine("packaging gate");
        Console.WriteLine($"  exported public types : {exported.Count}");
        Console.WriteLine($"  referenced by sample  : {covered}");
        Console.WriteLine($"  documented exclusions : {Excluded.Count}");

        foreach (string name in uncovered.Order(StringComparer.Ordinal))
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

        if (uncovered.Count == 0 && stale.Length == 0)
        {
            Console.WriteLine("  every public type is reachable.");
            Console.WriteLine();
            return true;
        }
        return false;
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
        out HashSet<string> memberRefParents)
    {
        typeRefs = new HashSet<string>(StringComparer.Ordinal);
        memberRefParents = new HashSet<string>(StringComparer.Ordinal);

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
                memberRefParents.Add(name);
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
