using System.Reflection;

namespace DataNet.DocSnippets;

// Runs the fences of docs/reference/** only. The guides stay compile-only: they
// open files and load models on purpose, which an opt-out per fence would not fix.
internal static class Program
{
    private const string Runnable = "DataNet.DocSnippets.Reference";

    private static int Main()
    {
        int run = 0;
        int skipped = 0;
        List<string> failures = [];

        IEnumerable<Type> types = typeof(Program).Assembly
            .GetTypes()
            .Where(type => type.Namespace == Runnable)
            .OrderBy(type => type.Name, StringComparer.Ordinal);

        foreach (Type type in types)
        {
            object? instance = Instantiate(type, failures);
            if (instance is null)
            {
                continue;
            }

            foreach (MethodInfo snippet in Snippets(type))
            {
                if (snippet.GetCustomAttribute<SnippetSkipRunAttribute>() is not null)
                {
                    skipped++;
                }
                else if (Invoke(instance, snippet, failures))
                {
                    run++;
                }
            }
        }

        // Zero is not success: it is what a glob that stopped matching looks like,
        // and CI would go green having executed none of the promised results.
        if (run == 0)
        {
            failures.Add(
                "no snippet ran. At least one is expected: every csharp fence in a " +
                $"docs/reference/*/*.md page becomes a method of the {Runnable} namespace, " +
                "so an empty namespace means the glob matched no page.");
        }

        Console.WriteLine($"snippets run     : {run}");
        Console.WriteLine($"snippets skipped : {skipped}");
        foreach (string failure in failures)
        {
            Console.Error.WriteLine($"::error::{failure}");
        }

        return failures.Count == 0 ? 0 : 1;
    }

    private static IEnumerable<MethodInfo> Snippets(Type type) => type
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .OrderBy(method => method.Name, StringComparer.Ordinal);

    private static object? Instantiate(Type type, List<string> failures)
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch (MissingMethodException error)
        {
            failures.Add($"{type.Name}: {error.Message}");
        }
        catch (TargetInvocationException error)
        {
            failures.Add($"{type.Name}: {error.InnerException?.Message}");
        }

        return null;
    }

    private static bool Invoke(object instance, MethodInfo snippet, List<string> failures)
    {
        try
        {
            snippet.Invoke(instance, null);
            return true;
        }
        catch (TargetInvocationException error)
        {
            failures.Add(
                $"{instance.GetType().Name}.{snippet.Name}: {error.InnerException?.Message}");
            return false;
        }
    }
}
