using System;

namespace DataNet.Internal;

/// <summary>
/// Shared regular-expression policy (compiled into each assembly).
/// </summary>
/// <remarks>
/// A caller-supplied pattern over caller-supplied text makes catastrophic
/// backtracking reachable from the public API; without a bound it hangs the
/// calling thread instead of throwing. See
/// <c>TextAnalyzerRegexTimeoutTests.Pathological_pattern_times_out_instead_of_hanging</c>.
/// </remarks>
internal static class RegexDefaults
{
    /// <summary>
    /// Upper bound on a single match attempt. Generous enough that no realistic
    /// document reaches it, small enough that a catastrophic pattern fails fast.
    /// </summary>
    public static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);
}
