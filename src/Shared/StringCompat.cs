namespace DataNet.Internal;


// CA2249 (use string.Contains instead of IndexOf): circular here — this file IS
// the netstandard2.0 polyfill for Contains(char), so it cannot call it.
#pragma warning disable CA2249
/// <summary>
/// Polyfill extensions for the <see cref="string"/> char overloads that exist on
/// net10 but not on netstandard2.0. On net10 the built-in instance methods take
/// priority, so these are used only on the netstandard2.0 build — no call site changes.
/// </summary>
internal static class StringCompat
{
    /// <summary>Whether the string starts with the given character.</summary>
    public static bool StartsWith(this string s, char c) => s.Length > 0 && s[0] == c;

    /// <summary>Whether the string ends with the given character.</summary>
    public static bool EndsWith(this string s, char c) => s.Length > 0 && s[s.Length - 1] == c;

    /// <summary>Whether the string contains the given character.</summary>
    public static bool Contains(this string s, char c) => s.IndexOf(c) >= 0;
}
