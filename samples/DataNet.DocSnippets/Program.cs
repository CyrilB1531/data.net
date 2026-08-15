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
            object instance;
            try
            {
                instance = Activator.CreateInstance(type)!;
            }
            catch (MissingMethodException error)
            {
                failures.Add($"{type.Name}: {error.Message}");
                continue;
            }
            catch (TargetInvocationException error)
            {
                failures.Add($"{type.Name}: {error.InnerException?.Message}");
                continue;
            }

            foreach (MethodInfo snippet in type
                         .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                         .OrderBy(method => method.Name, StringComparer.Ordinal))
            {
                if (snippet.GetCustomAttribute<SnippetSkipRunAttribute>() is not null)
                {
                    skipped++;
                    continue;
                }

                try
                {
                    snippet.Invoke(instance, null);
                    run++;
                }
                catch (TargetInvocationException error)
                {
                    failures.Add($"{type.Name}.{snippet.Name}: {error.InnerException?.Message}");
                }
            }
        }

        Console.WriteLine($"snippets run     : {run}");
        Console.WriteLine($"snippets skipped : {skipped}");
        foreach (string failure in failures)
        {
            Console.Error.WriteLine($"::error::{failure}");
        }

        return failures.Count == 0 ? 0 : 1;
    }
}
