namespace DataNet.Text.Tests.Documentation;

/// <summary>Every constraint shape the renderer has to spell, one method each.</summary>
/// <remarks>
/// Reflected over by the constraint facts. The covered surface carries
/// <c>where T : IEquatable&lt;T&gt;</c> and nothing else, so ordering cannot be observed
/// there and neither can <c>notnull</c> or <c>unmanaged</c> — the same argument
/// <see cref="ByRefFixture"/> makes for the by-ref keywords.
/// </remarks>
internal static class ConstraintFixture
{
    // `Random` sorts after `IComparable`: an ordinal sort put the base class second,
    // and C# refuses that order. One sorting first would let the bug through.
    public static T Pick<T>(T left, T right)
        where T : Random, IComparable<T> => left.CompareTo(right) >= 0 ? left : right;

    public static T Fresh<T>()
        where T : class, new() => new();

    public static T Zero<T>()
        where T : struct => default;

    public static int Size<T>(T value)
        where T : unmanaged => value.GetHashCode();

    public static string Name<T>(T value)
        where T : notnull => value.ToString() ?? string.Empty;

    public static int Only<T>(T value)
        where T : IComparable<T> => value.GetHashCode();

    public static bool Pair<TKey, TValue>(TKey key, TValue value)
        where TKey : notnull
        where TValue : class => key.GetHashCode() == value.GetHashCode();
}
