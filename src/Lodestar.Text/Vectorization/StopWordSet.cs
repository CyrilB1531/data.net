#if NET9_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace Lodestar.Text.Vectorization;

/// <summary>
/// The ordinal string set behind the shipped stop-word lists and the analyzer's
/// filter: built once, then read once per token.
/// </summary>
/// <remarks>
/// Built-once-read-often is why this type exists: on net10 a <c>FrozenSet&lt;string&gt;</c> answers a stop
/// word from a span, never materialising it as a string. netstandard2.0 keeps the plain
/// <see cref="HashSet{T}"/> it always had — the single <c>#if</c> below is the only place either build is
/// named. Matching is always ordinal, against the analyzer's already-lowercased output.
/// </remarks>
internal sealed class StopWordSet
{
    private static int _frozen;

    /// <summary>How many of the shipped lists have been built since the type loaded.</summary>
    /// <remarks>
    /// The lists initialise lazily, one nested holder type each. This counter is
    /// the only way a test can watch a holder <em>not</em> initialise: any
    /// reflection that could read a holder's field would run its initialiser
    /// first. See <c>StopWordsLazinessTests</c>, which loads the assembly into a
    /// fresh <c>AssemblyLoadContext</c> — fresh statics — and reads this.
    /// </remarks>
    internal static int FrozenLists => Volatile.Read(ref _frozen);

#if NET9_0_OR_GREATER

    private readonly FrozenSet<string> _words;
    private readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> _lookup;

    private StopWordSet(FrozenSet<string> words)
    {
        _words = words;
        _lookup = words.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    /// <summary>The set itself, for callers that only need to enumerate or count it.</summary>
    public IReadOnlyCollection<string> Words => _words;

    /// <summary>Builds one of the shipped lists, once, for a <see cref="StopWords"/> property.</summary>
    public static IReadOnlyCollection<string> Freeze(string[] words)
    {
        Interlocked.Increment(ref _frozen);
        return words.ToFrozenSet(StringComparer.Ordinal);
    }

    /// <summary>Wraps a caller's collection for use as a filter.</summary>
    /// <remarks>
    /// A shipped list arrives already frozen with <see cref="StringComparer.Ordinal"/>
    /// and is reused as-is — <c>ToFrozenSet</c> returns its argument in that case,
    /// so nothing is re-hashed per vectorizer. Anything else is copied, which is
    /// not merely defensive: a caller's <see cref="HashSet{T}"/> is theirs to
    /// mutate, and aliasing it would let a later <c>Add</c> change what a fitted
    /// vectorizer removes.
    /// </remarks>
    public static StopWordSet Adopt(IReadOnlyCollection<string> words) =>
        new(words.ToFrozenSet(StringComparer.Ordinal));

    /// <summary>Whether the token spanning <paramref name="token"/> is a stop word.</summary>
    public bool Contains(ReadOnlySpan<char> token) => _lookup.Contains(token);

#else

    private readonly HashSet<string> _words;

    private StopWordSet(HashSet<string> words) => _words = words;

    /// <summary>The set itself, for callers that only need to enumerate or count it.</summary>
    public IReadOnlyCollection<string> Words => _words;

    /// <summary>Builds one of the shipped lists, once, for a <see cref="StopWords"/> property.</summary>
    public static IReadOnlyCollection<string> Freeze(string[] words)
    {
        Interlocked.Increment(ref _frozen);
        return new HashSet<string>(words, StringComparer.Ordinal);
    }

    /// <summary>Wraps a caller's collection for use as a filter, always by copy.</summary>
    /// <remarks>
    /// There is no immutable set to recognise here, so every collection is copied
    /// — including the shipped lists. That is the netstandard2.0 cost, and it is
    /// the behaviour this build has always had.
    /// </remarks>
    public static StopWordSet Adopt(IReadOnlyCollection<string> words) =>
        new(new HashSet<string>(words, StringComparer.Ordinal));

    /// <summary>Whether <paramref name="token"/> is a stop word.</summary>
    public bool Contains(string token) => _words.Contains(token);

#endif
}
