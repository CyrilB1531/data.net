using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace Lodestar.Tests.Documentation;

/// <summary>
/// Checks a reference page against the assembly it documents.
/// </summary>
/// <remarks>
/// Microsoft derives a declaration, a parameter list and an Applies-to from the assembly;
/// hand-written here, so four checks replace that derivation — an entry per exported type
/// and public method, a declaration block listing exactly the overloads reflection reports,
/// every parameter named, and Applies to naming the targets that export the member. Each
/// test assembly references a different build, the only way the last of them sees anything.
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

        foreach (IGrouping<string, string> space in Covered(wikiMapPath, package)
                     .GroupBy(entry => entry.Namespace, entry => entry.Page, StringComparer.Ordinal))
        {
            List<Sheet> sheets = Load(space, referenceRoot, complaints);
            CheckNamespace(assembly, space.Key, sheets, moniker, complaints);
            foreach (Sheet sheet in sheets)
            {
                CheckOverClaims(assembly, space.Key, sheet, moniker, complaints);
            }
        }

        return complaints;
    }

    /// <summary>The pages of one namespace, parsed; a page declared and absent is a complaint.</summary>
    /// <remarks>
    /// A namespace declares either pages or the directory that holds them — one page per member,
    /// with a type page and an index above them (#189). A directory brings in that index too,
    /// which sits beside it as <c>&lt;directory&gt;.md</c> rather than being declared again.
    /// </remarks>
    private static List<Sheet> Load(
        IEnumerable<string> pages, string referenceRoot, List<string> complaints)
    {
        List<Sheet> sheets = [];
        foreach (string page in pages)
        {
            if (!page.EndsWith(".md", StringComparison.Ordinal))
            {
                LoadDirectory(page, referenceRoot, sheets, complaints);
                continue;
            }

            string path = Path.Combine(referenceRoot, Path.GetFileName(page));
            if (!File.Exists(path))
            {
                complaints.Add($"{page}: declared covered, and not next to the tests.");
                continue;
            }

            string text = File.ReadAllText(path);
            sheets.Add(new Sheet(page, text, Page.Parse(text)));
        }

        return sheets;
    }

    private static void LoadDirectory(
        string declared, string referenceRoot, List<Sheet> sheets, List<string> complaints)
    {
        string name = Path.GetFileName(declared.TrimEnd('/'));
        string directory = Path.Combine(referenceRoot, name);
        if (!Directory.Exists(directory))
        {
            complaints.Add($"{declared}: declared covered, and not next to the tests.");
            return;
        }

        string index = Path.Combine(referenceRoot, name + ".md");
        if (!File.Exists(index))
        {
            complaints.Add($"{declared}: no index page beside it — {name}.md is what a reader lands on.");
        }
        else
        {
            sheets.Add(Read(index, $"{declared}.md"));
        }

        foreach (string path in Directory.GetFiles(directory, "*.md").OrderBy(p => p, StringComparer.Ordinal))
        {
            sheets.Add(Read(path, $"{declared}/{Path.GetFileName(path)}"));
        }
    }

    private static Sheet Read(string path, string source)
    {
        string text = File.ReadAllText(path);
        return new Sheet(source, text, Page.Parse(text));
    }

    /// <summary>The type's row in the opening table of the page that carries its entry.</summary>
    /// <remarks>
    /// This is the third level of D10's hierarchy: the table already existed, and what it
    /// lacked was the link that turns a summary into navigation. The anchor is GitHub's slug
    /// rule -- lower-cased, dots dropped -- which <see cref="Anchor"/> already computes for a
    /// member link, and a bare type name slugifies the same way because it carries no dot.
    /// A namespace split over several pages has one table per page, and the row belongs on
    /// whichever of them describes the type.
    /// </remarks>
    private static void CheckTypeTable(Type type, Sheet sheet, List<string> complaints)
    {
        string link = $"[`{type.Name}`](#{Anchor(type.Name)})";
        if (!sheet.Text.Contains(link, StringComparison.Ordinal))
        {
            complaints.Add(
                $"{sheet.Source}: the opening table does not link {type.Name} to its entry as {link}.");
        }
    }

    private static void CheckNamespace(
        Assembly assembly, string space, List<Sheet> sheets, string moniker, List<string> complaints)
    {
        string pages = string.Join(", ", sheets.Select(sheet => sheet.Source));
        foreach (Type type in Documented(assembly, space))
        {
            Sheet? carrier = sheets.Find(sheet => sheet.Parsed.Entries.ContainsKey(type.Name));
            if (carrier is null)
            {
                complaints.Add($"{pages}: no entry for the type {type.Name}.");
                continue;
            }

            // The index of the directory this type is documented in -- a namespace may
            // hold several, as Lodestar.Metrics does, so it is not "the" index.
            string directory = Path.GetDirectoryName(carrier.Source)?.Replace('\\', '/') ?? string.Empty;
            Sheet? index = sheets.Find(sheet => sheet.Source == directory + ".md");
            if (index is null)
            {
                CheckTypeTable(type, carrier, complaints);
            }
            else
            {
                CheckIndexLinksTheType(type, index, complaints);
                CheckTypeLinksItsMembers(type, carrier, complaints);
            }

            CheckMembers(type, sheets, pages, moniker, complaints);
        }
    }

    /// <summary>A namespace index reaches a type by linking its page, not an anchor in itself.</summary>
    private static void CheckIndexLinksTheType(Type type, Sheet index, List<string> complaints)
    {
        string directory = Path.GetFileNameWithoutExtension(index.Source);
        string link = $"[`{type.Name}`]({directory}/{Anchor(type.Name)}.md)";
        if (!index.Text.Contains(link, StringComparison.Ordinal))
        {
            complaints.Add($"{index.Source}: the type table does not link {type.Name} as {link}.");
        }
    }

    /// <summary>A type page's member table reaches every member page of that type.</summary>
    private static void CheckTypeLinksItsMembers(Type type, Sheet page, List<string> complaints)
    {
        foreach (IGrouping<string, MethodInfo> overloads in Methods(type))
        {
            string title = $"{type.Name}.{overloads.Key}";
            string link = $"[`{title}`]({Anchor(type.Name)}-{Anchor(overloads.Key)}.md)";
            if (!page.Text.Contains(link, StringComparison.Ordinal))
            {
                complaints.Add($"{page.Source}: the member table does not link {title} as {link}.");
            }
        }
    }


    private static void CheckMembers(
        Type type, List<Sheet> sheets, string pages, string moniker, List<string> complaints)
    {
        foreach (IGrouping<string, MethodInfo> overloads in Methods(type))
        {
            string title = $"{type.Name}.{overloads.Key}";
            Sheet? sheet = sheets.Find(candidate => candidate.Parsed.Entries.ContainsKey(title));
            if (sheet is null)
            {
                complaints.Add($"{pages}: no entry for {title}.");
                continue;
            }

            Entry entry = sheet.Parsed.Entries[title];
            CheckDeclarations(sheet.Source, title, entry, overloads, complaints);
            CheckParameters(sheet.Source, title, entry, overloads, complaints);
            CheckAppliesTo(sheet.Source, title, entry, moniker, complaints);
        }
    }

    /// <summary>The exported types of one namespace that owe an entry of their own.</summary>
    /// <remarks>
    /// A nested exported type does not: the five residual kernels of
    /// <c>Lodestar.Metrics</c> are public only so that a generic constraint can name one,
    /// nobody calls them, and an entry each would be ceremony for a type a reader never
    /// types. They are described inside their declaring type's entry instead — D7.
    /// </remarks>
    private static IEnumerable<Type> Documented(Assembly assembly, string space) =>
        assembly.GetExportedTypes()
            .Where(candidate => candidate.Namespace == space && !candidate.IsNested)
            .OrderBy(candidate => candidate.Name, StringComparer.Ordinal);

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
        Assembly assembly, string space, Sheet sheet, string moniker, List<string> complaints)
    {
        HashSet<string> exported = Documented(assembly, space)
            .SelectMany(type => Methods(type).Select(group => $"{type.Name}.{group.Key}")
                .Append(type.Name))
            .ToHashSet(StringComparer.Ordinal);

        foreach ((string title, Entry entry) in sheet.Parsed.Entries)
        {
            if (entry.AppliesTo.Contains(moniker, StringComparison.Ordinal) && !exported.Contains(title))
            {
                complaints.Add(
                    $"{sheet.Source}: {title} claims {moniker}, which does not export it.");
            }
        }
    }

    /// <summary>One reference page: where it came from, its raw text, and its entries.</summary>
    private sealed record Sheet(string Source, string Text, Page Parsed);

    /// <summary>Every place a documented member is named and its entry is not reachable.</summary>
    /// <remarks>
    /// Two obligations, and a complaint says which it comes from. A backticked mention in
    /// prose is a link — a property of the line. Naming a member at all, inside a `csharp`
    /// fence included where Markdown carries no link, obliges the page to link its entry
    /// once somewhere — a property of the page, and what stops a guide from demonstrating
    /// a method three times while offering no way to find out what it does.
    /// </remarks>
    public static IReadOnlyList<string> CheckLinks(
        Assembly assembly, string package, string wikiMapPath, string referenceRoot, string docsRoot)
    {
        List<string> complaints = [];
        HashSet<string> linkable = LinkableMembers(assembly, wikiMapPath, package);
        HashSet<string> referencePageNames = ReferencePageNames(referenceRoot);

        foreach (string file in Directory.EnumerateFiles(docsRoot, "*.md", SearchOption.AllDirectories)
                     .Where(candidate => !referencePageNames.Contains(Path.GetFileName(candidate)))
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
            foreach (Type type in Documented(assembly, space))
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
        ? Directory.GetFiles(referenceRoot, "*.md", SearchOption.AllDirectories)
            .Select(path => Path.GetFileName(path))
            .ToHashSet(StringComparer.Ordinal)
        : new HashSet<string>(StringComparer.Ordinal);

    private static void CheckFileLinks(
        string file, string docsRoot, HashSet<string> linkable, List<string> complaints)
    {
        string relative = Path.GetRelativePath(docsRoot, file).Replace('\\', '/');
        string[] lines = File.ReadAllLines(file);
        HashSet<string> anchors = new(StringComparer.Ordinal);
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
                foreach (string anchor in Anchors(line))
                {
                    anchors.Add(anchor);
                }
            }
        }

        CheckPageLinksWhatItUses(lines, relative, linkable, anchors, complaints);
    }

    /// <summary>The second obligation: a member the page names, and links nowhere.</summary>
    private static void CheckPageLinksWhatItUses(
        string[] lines,
        string relative,
        HashSet<string> linkable,
        HashSet<string> anchors,
        List<string> complaints)
    {
        foreach (string member in linkable.OrderBy(name => name, StringComparer.Ordinal))
        {
            string page = Anchor(member.Replace('.', '-')) + ".md";
            bool reachable = anchors.Contains(Anchor(member))
                || anchors.Contains(page);
            if (!reachable && Array.Exists(lines, line => Names(line, member)))
            {
                complaints.Add(
                    $"{relative}: uses '{member}' and links its entry nowhere on the page. " +
                    "A fence cannot carry a link, so put one in the prose around it.");
            }
        }
    }

    /// <summary>The anchor GitHub gives an entry's heading: its text, lowercased, without the dot.</summary>
    private static string Anchor(string member)
    {
        StringBuilder anchor = new(member.Length);
        foreach (char character in member)
        {
            if (character != '.')
            {
                anchor.Append(char.ToLowerInvariant(character));
            }
        }

        return anchor.ToString();
    }

    /// <summary>What a line's links reach: a heading anchor without its '#', or a page's file name.</summary>
    /// <remarks>
    /// Both, because the two shapes coexist while the namespaces move one lot at a time:
    /// a combined page is reached by `#levenshteindistance`, a split one by
    /// `levenshtein-distance.md` (#189).
    /// </remarks>
    private static IEnumerable<string> Anchors(string line)
    {
        foreach ((int start, int end) in LinkSpans(line))
        {
            string span = line[start..end];
            int target = span.IndexOf("](", StringComparison.Ordinal);
            if (target < 0)
            {
                continue;
            }

            string destination = span[(target + 2)..^1];
            int hash = destination.IndexOf('#', StringComparison.Ordinal);
            yield return hash >= 0
                ? destination[(hash + 1)..]
                : destination[(destination.LastIndexOf('/') + 1)..];
        }
    }

    // Bounded on both sides, so `DamerauLevenshtein.Distance` is not read as a
    // use of `Levenshtein.Distance` and does not satisfy — or fail — its rule.
    private static bool Names(string line, string member)
    {
        int index = 0;
        while ((index = line.IndexOf(member, index, StringComparison.Ordinal)) >= 0)
        {
            int after = index + member.Length;
            if ((index == 0 || !IsWithinName(line[index - 1])) &&
                (after == line.Length || !IsWithinName(line[after])))
            {
                return true;
            }

            index = after;
        }

        return false;
    }

    private static bool IsWithinName(char character) =>
        char.IsLetterOrDigit(character) || character == '_' || character == '.';

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

    /// <summary>The public methods of a type that owe an entry, grouped by name.</summary>
    /// <remarks>
    /// A compiler-generated one does not: a <c>record</c> synthesises <c>Deconstruct</c>,
    /// <c>Equals</c>, <c>GetHashCode</c>, <c>ToString</c> and <c>&lt;Clone&gt;$</c> — the
    /// last not a name C# can spell, so its declaration could not be written even in
    /// principle. Its type entry shows them, as it does a nested type in
    /// <see cref="Documented"/>. A hand-written <c>ToString</c> carries no such attribute
    /// and still owes an entry.
    /// </remarks>
    private static IEnumerable<IGrouping<string, MethodInfo>> Methods(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                        BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName &&
                             !method.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
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

        text.Append(RenderType(method.ReturnType)).Append(' ').Append(method.Name);
        if (method.IsGenericMethodDefinition)
        {
            text.Append('<').AppendJoin(", ", method.GetGenericArguments().Select(RenderType)).Append('>');
        }

        text.Append('(');
        text.AppendJoin(", ", method.GetParameters().Select(RenderParameter)).Append(')');
        return text.Append(RenderConstraints(method)).ToString();
    }

    /// <summary>The `where` clauses of a generic method, as C# spells them, on one line.</summary>
    /// <remarks>
    /// Without them a page would document `Distance(ReadOnlySpan&lt;T&gt; a, ...)`, which names an
    /// unbound T and would not compile if a reader pasted it — and the gate would insist on it.
    /// </remarks>
    private static string RenderConstraints(MethodInfo method)
    {
        if (!method.IsGenericMethodDefinition)
        {
            return string.Empty;
        }

        StringBuilder text = new();
        foreach (Type argument in method.GetGenericArguments())
        {
            List<string> clauses = Constraints(argument);
            if (clauses.Count > 0)
            {
                text.Append(" where ").Append(argument.Name).Append(" : ").AppendJoin(", ", clauses);
            }
        }

        return text.ToString();
    }

    /// <summary>The nullable byte that spells `notnull`: 1, "not annotated".</summary>
    private const byte NotNullable = 1;

    /// <summary>One parameter's clause, in the order C# accepts: primary, secondary, <c>new()</c>.</summary>
    /// <remarks>
    /// A base class must come before the interfaces beside it, so sorting every constraint
    /// type together produced <c>where T : IComparable&lt;T&gt;, Random</c> — which does not
    /// compile, and which the gate then required the page to write. The secondary
    /// constraints are sorted among themselves, where the language leaves the order free.
    /// </remarks>
    private static List<string> Constraints(Type argument)
    {
        GenericParameterAttributes flags = argument.GenericParameterAttributes;
        bool value = flags.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint);
        List<string> clauses = [];
        string? primary = Primary(argument, flags, value);
        if (primary is not null)
        {
            clauses.Add(primary);
        }

        // Interfaces and other type parameters, which the language treats alike here.
        // ValueType is how `struct` is spelled in metadata, and Primary covered it.
        clauses.AddRange(argument.GetGenericParameterConstraints()
            .Where(type => type.IsInterface || type.IsGenericParameter)
            .Select(RenderType)
            .OrderBy(name => name, StringComparer.Ordinal));

        if (!value && flags.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
        {
            clauses.Add("new()");
        }

        return clauses;
    }

    /// <summary>The one constraint that has to come first, if the parameter has one.</summary>
    private static string? Primary(Type argument, GenericParameterAttributes flags, bool value)
    {
        Type? baseClass = Array.Find(
            argument.GetGenericParameterConstraints(),
            type => !type.IsInterface && !type.IsGenericParameter && type != typeof(ValueType));
        if (baseClass is not null)
        {
            return RenderType(baseClass);
        }

        if (flags.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
        {
            return "class";
        }

        if (value)
        {
            return IsUnmanaged(argument) ? "unmanaged" : "struct";
        }

        return NullableFlag(argument) == NotNullable ? "notnull" : null;
    }

    /// <summary>`unmanaged`, which metadata spells as `struct` plus a marker attribute.</summary>
    private static bool IsUnmanaged(Type argument) => argument.GetCustomAttributesData()
        .Any(attribute => attribute.AttributeType.Name == "IsUnmanagedAttribute");

    /// <summary>The nullable byte that applies to a type parameter, or none.</summary>
    /// <remarks>
    /// `notnull` is the value 1, written either on the parameter itself or, when the
    /// compiler compressed it, as a `NullableContext` default on the method or the
    /// declaring type. Measured: <c>where T : IComparable&lt;T&gt;</c> under a context of 1
    /// carries an explicit 0, so a parameter that inherits the 1 really is `notnull`.
    /// </remarks>
    private static byte? NullableFlag(Type argument)
    {
        byte? own = ByteArgument(argument.GetCustomAttributesData(), "NullableAttribute");
        if (own is not null)
        {
            return own;
        }

        MethodBase? method = argument.DeclaringMethod;
        byte? onMethod = method is null
            ? null
            : ByteArgument(method.GetCustomAttributesData(), "NullableContextAttribute");
        Type? declaring = argument.DeclaringType ?? method?.DeclaringType;
        return onMethod ?? (declaring is null
            ? null
            : ByteArgument(declaring.GetCustomAttributesData(), "NullableContextAttribute"));
    }

    private static byte? ByteArgument(IEnumerable<CustomAttributeData> attributes, string name) =>
        attributes.FirstOrDefault(attribute => attribute.AttributeType.Name == name)
            ?.ConstructorArguments is [{ Value: byte flag }] ? flag : null;

    private static string RenderParameter(ParameterInfo parameter)
    {
        string modifier = parameter.ParameterType.IsByRef ? ByRefModifier(parameter) : string.Empty;
        Type type = parameter.ParameterType.IsByRef
            ? parameter.ParameterType.GetElementType()!
            : parameter.ParameterType;
        string rendered = $"{modifier}{RenderType(type)} {parameter.Name}";
        return parameter.HasDefaultValue
            ? $"{rendered} = {RenderDefault(parameter.DefaultValue, type)}"
            : rendered;
    }

    // All three are by-ref in metadata, and only the keyword tells a reader whether
    // the value goes in, comes out, or is read without being copied.
    private static string ByRefModifier(ParameterInfo parameter) => parameter switch
    {
        { IsOut: true } => "out ",
        { IsIn: true } => "in ",
        _ => "ref ",
    };

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

        // Reflection spells double[] "Double[]" and double[,] "Double[,]", neither of
        // which is what a page writes; the element type goes back through the aliases.
        if (type.IsArray)
        {
            return $"{RenderType(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
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

    /// <summary>Every (namespace, page) pair a package declares covered.</summary>
    /// <remarks>
    /// A namespace maps to a page or to a list of them. <c>Lodestar.Metrics</c> is why:
    /// its 31 exported types on one page would be a scrolling exercise rather than a
    /// reference, so its classification and regression halves are two documents of one
    /// namespace. The gate takes the union — an entry counts wherever it is found —
    /// and still refuses a type or a method with no entry on any of them.
    /// </remarks>
    private static IEnumerable<(string Namespace, string Page)> Covered(string wikiMapPath, string package)
    {
        using JsonDocument map = JsonDocument.Parse(File.ReadAllText(wikiMapPath));
        JsonElement covered = map.RootElement
            .GetProperty("packages").GetProperty(package).GetProperty("covered");

        foreach (JsonProperty entry in covered.EnumerateObject())
        {
            if (entry.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement page in entry.Value.EnumerateArray())
                {
                    yield return (entry.Name, page.GetString()!);
                }
            }
            else
            {
                yield return (entry.Name, entry.Value.GetString()!);
            }
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
                // Any heading bounds an entry, `#` included: a member page's title is its H1
                // where a combined page's section was an H4. Never inside a fence.
                if (!inFence && line.StartsWith('#'))
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
