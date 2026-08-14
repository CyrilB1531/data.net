using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using DataNet.Text.Vectorization;
using Xunit;

namespace DataNet.Text.Tests.Vectorization;

/// <summary>
/// How the shipped lists are built, rather than what they contain: one list per
/// first use, and no re-hashing when one of them is handed to a vectorizer.
/// </summary>
public sealed class StopWordSetTests
{
    private static bool RunsAgainstNetStandard =>
        typeof(StopWords).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName
            == ".NETStandard,Version=v2.0";

    [Fact]
    public void A_shipped_list_is_used_as_it_is()
    {
        StopWordSet adopted = StopWordSet.Adopt(StopWords.English);

        if (RunsAgainstNetStandard)
        {
            // No immutable set to recognise on netstandard2.0: the copy stands, and
            // all that can be asserted is that it copied the right thing.
            Assert.NotSame(StopWords.English, adopted.Words);
            Assert.Equal(StopWords.English.Count, adopted.Words.Count);
        }
        else
        {
            // Every vectorizer built with StopWords.English shares this one set,
            // and none of them hashes its 318 words again.
            Assert.Same(StopWords.English, adopted.Words);
        }
    }

    [Fact]
    public void A_caller_set_is_copied()
    {
        var mine = new HashSet<string>(["the", "a"], StringComparer.Ordinal);

        StopWordSet adopted = StopWordSet.Adopt(mine);

        Assert.NotSame(mine, adopted.Words);
        Assert.Equal(2, adopted.Words.Count);
    }

    [Fact]
    public void Touching_one_list_does_not_build_the_others()
    {
        // A fresh AssemblyLoadContext gives the assembly private statics, so every
        // holder starts uninitialised whatever the rest of this suite has touched.
        var context = new AssemblyLoadContext("stop-words-laziness", isCollectible: true);
        try
        {
            Assembly copy = context.LoadFromAssemblyPath(typeof(StopWords).Assembly.Location);
            Assert.NotSame(typeof(StopWords).Assembly, copy);

            Type stopWords = copy.GetType("DataNet.Text.Vectorization.StopWords", throwOnError: true)!;
            PropertyInfo frozenLists = copy
                .GetType("DataNet.Text.Vectorization.StopWordSet", throwOnError: true)!
                .GetProperty("FrozenLists", BindingFlags.Static | BindingFlags.NonPublic)!;
            int Built() => (int)frozenLists.GetValue(null)!;

            Assert.Equal(0, Built());

            object? french = stopWords.GetProperty(nameof(StopWords.French))!.GetValue(null);

            // Counted rather than cast: the two builds return different set types,
            // and only the words are common to both.
            Assert.Equal(154, ((System.Collections.IEnumerable)french!).Cast<object>().Count());
            Assert.Equal(1, Built());

            // …and reading it again builds nothing, while a second language builds
            // exactly one more.
            _ = stopWords.GetProperty(nameof(StopWords.French))!.GetValue(null);
            Assert.Equal(1, Built());

            _ = stopWords.GetProperty(nameof(StopWords.German))!.GetValue(null);
            Assert.Equal(2, Built());
        }
        finally
        {
            context.Unload();
        }
    }
}
