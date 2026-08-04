using System;

namespace DataNet.Internal;

/// <summary>
/// Shared regular-expression policy (compiled into each assembly).
/// </summary>
/// <remarks>
/// Every <see cref="System.Text.RegularExpressions.Regex"/> in the libraries runs
/// over caller-supplied text, and <c>TextAnalyzer</c> additionally accepts a
/// caller-supplied <em>pattern</em>. Without a match timeout, backtracking is
/// unbounded, so a pathological pattern/input pair hangs the calling thread
/// instead of failing. A bounded match turns that into an exception the caller
/// can handle.
/// </remarks>
internal static class RegexDefaults
{
    /// <summary>
    /// Upper bound on a single match attempt. Generous enough that no realistic
    /// document reaches it, small enough that a catastrophic pattern fails fast.
    /// </summary>
    public static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);
}
