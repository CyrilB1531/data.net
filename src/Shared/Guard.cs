using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lodestar.Internal;

/// <summary>
/// Small argument guards shared across the libraries (compiled into each assembly).
/// </summary>
/// <remarks>
/// On net10 this delegates to <c>ArgumentNullException.ThrowIfNull</c> (so the
/// CA1510 analyzer stays happy and no manual null-check pattern appears), while on
/// netstandard2.0 — where that helper does not exist — it does the equivalent check
/// by hand. The single <c>#if</c> keeps every call site clean.
/// </remarks>
internal static class Guard
{
    /// <summary>Throws <see cref="ArgumentNullException"/> if <paramref name="value"/> is null.</summary>
    public static void NotNull(
        [NotNull] object? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(value, paramName);
#else
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
#endif
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is below <paramref name="min"/>.</summary>
    public static void NotLessThan(
        int value,
        int min,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfLessThan(value, min, paramName);
#else
        if (value < min)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Must be at least {min}.");
        }
#endif
    }
}
