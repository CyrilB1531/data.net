using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace DataNet.Tests.Documentation;

/// long-comment: the rule this file enforces is four separate checks, and a
/// reader needs to know which one failed before they can fix a page
/// <summary>
/// Checks a reference page against the assembly it documents.
/// </summary>
/// <remarks>
/// Microsoft derives a declaration, a parameter list and an Applies-to from the
/// assembly. Here they are written by hand, so this is what replaces that
/// derivation: every exported type and public method of a covered namespace has
/// an entry, its declaration block lists exactly the overloads reflection
/// reports, every parameter is named, and Applies to names the targets that
/// really export the member. It runs once per target framework, because each
/// test assembly references a different build of the library — which is the
/// only way the fourth check can see the difference.
/// </remarks>
internal static class ReferenceDocumentation
{
    private const string DeclarationMarker = "<!-- docs-declaration -->";

    /// <summary>Every complaint the pages earn; empty when they are right.</summary>
    public static IReadOnlyList<string> Check(
        Assembly assembly, string package, string wikiMapPath, string referenceRoot)
    {
        List<string> complaints = [];
        string moniker = Moniker(assembly);

        foreach ((string space, string page) in Covered(wikiMapPath, package))
        {
            string path = Path.Combine(referenceRoot, Path.GetFileName(page));
            if (!File.Exists(path))
            {
                complaints.Add($"{page}: declared covered for {space}, and not next to the tests.");
                continue;
            }

            Page parsed = Page.Parse(File.ReadAllText(path));
            CheckNamespace(assembly, space, page, parsed, moniker, complaints);
            CheckOverClaims(assembly, space, page, parsed, moniker, complaints);
        }

        return complaints;
    }

    private static void CheckNamespace(
        Assembly assembly, string space, string page, Page parsed, string moniker, List<string> complaints)
    {
        foreach (Type type in assembly.GetExportedTypes()
                     .Where(candidate => candidate.Namespace == space)
                     .OrderBy(candidate => candidate.Name, StringComparer.Ordinal))
        {
            if (!parsed.Entries.ContainsKey(type.Name))
            {
                complaints.Add($"{page}: no entry for the type {type.Name}.");
                continue;
            }

            foreach (IGrouping<string, MethodInfo> overloads in Methods(type))
            {
                string title = $"{type.Name}.{overloads.Key}";
                if (!parsed.Entries.TryGetValue(title, out Entry? entry))
                {
                    complaints.Add($"{page}: no entry for {title}.");
                    continue;
                }

                CheckDeclarations(page, title, entry, overloads, complaints);
                CheckParameters(page, title, entry, overloads, complaints);
                CheckAppliesTo(page, title, entry, moniker, complaints);
            }
        }
    }

    private static void CheckDeclarations(
        string page, string title, Entry entry, IEnumerable<MethodInfo> overloads, List<string> complaints)
    {
        HashSet<string> expected = overloads.Select(RenderSignature).ToHashSet(StringComparer.Ordinal);
        HashSet<string> written = entry.Declarations.ToHashSet(StringComparer.Ordinal);

        foreach (string missing in expected.Except(written).OrderBy(text => text, StringComparer.Ordinal))
        {
            complaints.Add($"{page}: {title} does not declare '{missing}'.");
        }

        foreach (string extra in written.Except(expected).OrderBy(text => text, StringComparer.Ordinal))
        {
            complaints.Add($"{page}: {title} declares '{extra}', which the assembly does not export.");
        }
    }

    private static void CheckParameters(
        string page, string title, Entry entry, IEnumerable<MethodInfo> overloads, List<string> complaints)
    {
        foreach (string name in overloads
                     .SelectMany(method => method.GetParameters())
                     .Select(parameter => parameter.Name!)
                     .Distinct(StringComparer.Ordinal)
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            if (!entry.Parameters.Contains(name))
            {
                complaints.Add($"{page}: {title} never describes the parameter '{name}'.");
            }
        }
    }

    private static void CheckAppliesTo(
        string page, string title, Entry entry, string moniker, List<string> complaints)
    {
        if (!entry.AppliesTo.Contains(moniker, StringComparison.Ordinal))
        {
            complaints.Add(
                $"{page}: {title} is exported by {moniker}, and its 'Applies to' does not say so.");
        }
    }

    private static void CheckOverClaims(
        Assembly assembly, string space, string page, Page parsed, string moniker, List<string> complaints)
    {
        HashSet<string> exported = assembly.GetExportedTypes()
            .Where(type => type.Namespace == space)
            .SelectMany(type => Methods(type).Select(group => $"{type.Name}.{group.Key}")
                .Append(type.Name))
            .ToHashSet(StringComparer.Ordinal);

        foreach ((string title, Entry entry) in parsed.Entries)
        {
            if (entry.AppliesTo.Contains(moniker, StringComparison.Ordinal) && !exported.Contains(title))
            {
                complaints.Add(
                    $"{page}: {title} claims {moniker}, which does not export it.");
            }
        }
    }

    /// <summary>Every backticked mention of a documented member that is not linked to its entry.</summary>
    public static IReadOnlyList<string> CheckLinks(
        Assembly assembly, string package, string wikiMapPath, string referenceRoot, string docsRoot)
    {
        List<string> complaints = [];
        HashSet<string> linkable = LinkableMembers(assembly, wikiMapPath, package);
        HashSet<string> referencePageNames = ReferencePageNames(referenceRoot);

        foreach (string file in Directory.EnumerateFiles(docsRoot, "*.md", SearchOption.AllDirectories)
                     .Where(candidate => !referencePageNames.Contains(Path.GetFileName(candidate)!))
                     .OrderBy(candidate => candidate, StringComparer.Ordinal))
        {
            CheckFileLinks(file, docsRoot, linkable, complaints);
        }

        return complaints;
    }

    // Reuses Covered, GetExportedTypes and Methods -- the primitives CheckNamespace and
    // CheckOverClaims already build "documented" from -- so the two cannot disagree.
    private static HashSet<string> LinkableMembers(Assembly assembly, string wikiMapPath, string package)
    {
        HashSet<string> members = new(StringComparer.Ordinal);
        foreach ((string space, string _) in Covered(wikiMapPath, package))
        {
            foreach (Type type in assembly.GetExportedTypes().Where(candidate => candidate.Namespace == space))
            {
                foreach (IGrouping<string, MethodInfo> overloads in Methods(type))
                {
                    members.Add($"{type.Name}.{overloads.Key}");
                }
            }
        }

        return members;
    }

    /// <summary>The file names Check itself treats as reference pages, read off the same directory.</summary>
    private static HashSet<string> ReferencePageNames(string referenceRoot) => Directory.Exists(referenceRoot)
        ? Directory.GetFiles(referenceRoot, "*.md").Select(path => Path.GetFileName(path)!)
            .ToHashSet(StringComparer.Ordinal)
        : new HashSet<string>(StringComparer.Ordinal);

    private static void CheckFileLinks(
        string file, string docsRoot, HashSet<string> linkable, List<string> complaints)
    {
        string relative = Path.GetRelativePath(docsRoot, file).Replace('\\', '/');
        string[] lines = File.ReadAllLines(file);
        bool inFence = false;

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (!inFence)
            {
                CheckLineLinks(line, relative, index + 1, linkable, complaints);
            }
        }
    }

    private static void CheckLineLinks(
        string line, string relativeFile, int lineNumber, HashSet<string> linkable, List<string> complaints)
    {
        IReadOnlyList<(int Start, int End)> linked = LinkSpans(line);

        foreach ((int start, int end) in BacktickSpans(line))
        {
            if (linked.Any(span => start >= span.Start && end <= span.End))
            {
                continue;
            }

            string name = StripArguments(line[(start + 1)..(end - 1)]);
            if (linkable.Contains(name))
            {
                complaints.Add($"{relativeFile}:{lineNumber}: '{name}' has an entry and is not linked to it.");
            }
        }
    }

    private static string StripArguments(string text)
    {
        int index = text.IndexOf('(', StringComparison.Ordinal);
        return index < 0 ? text : text[..index];
    }

    /// <summary>Every backticked span on a line, as (start, end) offsets spanning both backticks.</summary>
    private static List<(int Start, int End)> BacktickSpans(string line)
    {
        List<(int Start, int End)> spans = [];
        int index = 0;
        while ((index = line.IndexOf('`', index)) >= 0)
        {
            int end = line.IndexOf('`', index + 1);
            if (end < 0)
            {
                break;
            }

            spans.Add((index, end + 1));
            index = end + 1;
        }

        return spans;
    }

    /// <summary>Every `[label](target)` span on a line, as (start, end) offsets spanning both brackets.</summary>
    /// <remarks>
    /// No nested brackets: a label never contains `]`, which is true of every link this repository
    /// writes -- the label is either plain prose or a single backticked span.
    /// </remarks>
    private static List<(int Start, int End)> LinkSpans(string line)
    {
        List<(int Start, int End)> spans = [];
        int index = 0;
        while ((index = line.IndexOf('[', index)) >= 0)
        {
            int labelEnd = line.IndexOf(']', index + 1);
            if (labelEnd < 0 || labelEnd + 1 >= line.Length || line[labelEnd + 1] != '(')
            {
                index++;
                continue;
            }

            int targetEnd = line.IndexOf(')', labelEnd + 2);
            if (targetEnd < 0)
            {
                index++;
                continue;
            }

            spans.Add((index, targetEnd + 1));
            index = targetEnd + 1;
        }

        return spans;
    }

    private static IEnumerable<IGrouping<string, MethodInfo>> Methods(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                        BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .GroupBy(method => method.Name, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal);

    /// <summary>A declaration as a reader would write it, not as reflection spells it.</summary>
    public static string RenderSignature(MethodInfo method)
    {
        StringBuilder text = new("public ");
        if (method.IsStatic)
        {
            text.Append("static ");
        }

        text.Append(RenderType(method.ReturnType)).Append(' ').Append(method.Name).Append('(');
        text.AppendJoin(", ", method.GetParameters().Select(RenderParameter)).Append(')');
        return text.ToString();
    }

    private static string RenderParameter(ParameterInfo parameter)
    {
        string modifier = parameter.ParameterType.IsByRef ? "ref " : string.Empty;
        Type type = parameter.ParameterType.IsByRef
            ? parameter.ParameterType.GetElementType()!
            : parameter.ParameterType;
        string rendered = $"{modifier}{RenderType(type)} {parameter.Name}";
        return parameter.HasDefaultValue
            ? $"{rendered} = {RenderDefault(parameter.DefaultValue, type)}"
            : rendered;
    }

    private static string RenderDefault(object? value, Type type) => value switch
    {
        null => type.IsValueType ? "default" : "null",
        bool flag => flag ? "true" : "false",
        string text => $"\"{text}\"",
        _ when type.IsEnum => $"{type.Name}.{Enum.GetName(type, value)}",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "default",
    };

    private static string RenderType(Type type)
    {
        if (Aliases.TryGetValue(type, out string? alias))
        {
            return alias;
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        string name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(RenderType))}>";
    }

    private static readonly Dictionary<Type, string> Aliases = new()
    {
        [typeof(void)] = "void",
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(char)] = "char",
        [typeof(double)] = "double",
        [typeof(float)] = "float",
        [typeof(int)] = "int",
        [typeof(long)] = "long",
        [typeof(object)] = "object",
        [typeof(string)] = "string",
        [typeof(uint)] = "uint",
        [typeof(ulong)] = "ulong",
    };

    /// <summary>The target framework of the build under test, as a page spells it.</summary>
    private static string Moniker(Assembly assembly)
    {
        string? name = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        return name is null || !name.Contains("NETStandard", StringComparison.Ordinal)
            ? "net10.0"
            : "netstandard2.0";
    }

    private static IEnumerable<(string Namespace, string Page)> Covered(string wikiMapPath, string package)
    {
        using JsonDocument map = JsonDocument.Parse(File.ReadAllText(wikiMapPath));
        JsonElement covered = map.RootElement
            .GetProperty("packages").GetProperty(package).GetProperty("covered");

        foreach (JsonProperty entry in covered.EnumerateObject())
        {
            yield return (entry.Name, entry.Value.GetString()!);
        }
    }

    // internal rather than private: Parse's rubric-boundary handling is pinned directly by
    // ReferenceDocumentationTests, and both types are compiled into that same test assembly.
    internal sealed record Entry(List<string> Declarations, HashSet<string> Parameters, string AppliesTo);

    internal sealed record Page(Dictionary<string, Entry> Entries)
    {
        /// <summary>Splits a page into its entries, keyed by the heading text.</summary>
        public static Page Parse(string text)
        {
            ParserState state = new();
            foreach (string line in text.Split('\n').Select(raw => raw.TrimEnd('\r')))
            {
                state.Consume(line);
            }

            state.Flush();
            return new Page(state.Entries);
        }

        /// <summary>Mutable line-by-line parse state, kept out of Parse to hold its own complexity down.</summary>
        private sealed class ParserState
        {
            public Dictionary<string, Entry> Entries { get; } = new(StringComparer.Ordinal);

            private string title = string.Empty;
            private Entry current = New();
            private bool inDeclaration;
            private bool inFence;
            private bool inParameters;

            public void Consume(string line)
            {
                if (line.StartsWith("###", StringComparison.Ordinal))
                {
                    Flush();
                    title = line.TrimStart('#').Trim();
                    current = New();
                    inDeclaration = false;
                    inParameters = false;
                    return;
                }

                if (line.Trim() == DeclarationMarker)
                {
                    inDeclaration = true;
                    return;
                }

                if (line.StartsWith("```", StringComparison.Ordinal))
                {
                    inFence = !inFence;
                    inDeclaration = inDeclaration && inFence;
                    return;
                }

                ConsumeBody(line);
            }

            public void Flush() => Store(Entries, title, current);

            private void ConsumeBody(string line)
            {
                if (inDeclaration && inFence && line.Trim().Length > 0)
                {
                    current.Declarations.Add(line.Trim());
                    return;
                }

                // A line starting with "**" is a rubric heading; it bounds where
                // Parameters ends as sharply as it bounds where it begins.
                if (line.StartsWith("**", StringComparison.Ordinal))
                {
                    ConsumeRubricStart(line);
                    return;
                }

                if (inParameters)
                {
                    AddParameters(line);
                }
            }

            private void ConsumeRubricStart(string line)
            {
                inParameters = line.StartsWith("**Parameters**", StringComparison.Ordinal);
                if (inParameters)
                {
                    AddParameters(line);
                }
                else if (line.StartsWith("**Applies to**", StringComparison.Ordinal))
                {
                    current = current with { AppliesTo = line };
                }
            }

            private void AddParameters(string line)
            {
                foreach (string name in Backticked(line))
                {
                    current.Parameters.Add(name);
                }
            }

            private static Entry New() => new([], new HashSet<string>(StringComparer.Ordinal), string.Empty);

            private static void Store(Dictionary<string, Entry> entries, string title, Entry entry)
            {
                if (title.Length > 0)
                {
                    entries[title] = entry;
                }
            }

            private static IEnumerable<string> Backticked(string line)
            {
                int index = 0;
                while ((index = line.IndexOf('`', index)) >= 0)
                {
                    int end = line.IndexOf('`', index + 1);
                    if (end < 0)
                    {
                        yield break;
                    }

                    yield return line[(index + 1)..end];
                    index = end + 1;
                }
            }
        }
    }
}
