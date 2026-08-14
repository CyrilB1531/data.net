using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// Finds the next <see cref="AddedToken"/> in a string. The one place either
/// tokenizer asks that question, so the two cannot answer it differently.
/// </summary>
internal sealed class AddedTokenScanner
{
    private readonly AddedToken[] _tokens;

    /// <summary>Keeps the entries that can match; the order does not matter.</summary>
    /// <remarks>
    /// An empty <see cref="AddedToken.Content"/> is dropped: it would match at
    /// every position without advancing the caller's scan, hanging the loop. The
    /// loader bounds a token's upper length but never rejects an empty one, so
    /// this cannot be assumed away.
    /// </remarks>
    internal AddedTokenScanner(IReadOnlyList<AddedToken> tokens) =>
        _tokens = [.. tokens.Where(t => t.Content.Length > 0)];

    /// <summary>Whether any entry can ever match.</summary>
    internal bool IsEmpty => _tokens.Length == 0;

    /// <summary>
    /// The earliest match at or after <paramref name="from"/> — the longest one,
    /// on a tie — with the span it consumes once stripping is applied.
    /// </summary>
    /// <param name="text">The text being scanned.</param>
    /// <param name="from">Where to start; a strip never reaches behind it.</param>
    /// <param name="start">The first index the match consumes.</param>
    /// <param name="end">One past the last index the match consumes.</param>
    /// <param name="token">The entry that matched.</param>
    internal bool TryNext(string text, int from, out int start, out int end, [MaybeNullWhen(false)] out AddedToken token)
    {
        AddedToken? best = BestMatch(text, from, out int bestAt);

        if (best is null)
        {
            start = -1;
            end = -1;
            token = null;
            return false;
        }

        // Ties break on the raw match position, before either side's strip applies —
        // untested for a whitespace-content candidate; see decision 0022 §9.
        start = bestAt;
        end = bestAt + best.Content.Length;
        if (best.Lstrip)
        {
            while (start > from && char.IsWhiteSpace(text[start - 1]))
            {
                start--;
            }
        }
        if (best.Rstrip)
        {
            while (end < text.Length && char.IsWhiteSpace(text[end]))
            {
                end++;
            }
        }
        token = best;
        return true;
    }

    /// <summary>
    /// The entry that wins at or after <paramref name="from"/> — earliest, and the
    /// longest of those tied on that position — or <see langword="null"/> when none
    /// matches.
    /// </summary>
    /// <param name="text">The text being scanned.</param>
    /// <param name="from">Where to start.</param>
    /// <param name="at">The raw index the winner matched at, before stripping; -1 when none matched.</param>
    private AddedToken? BestMatch(string text, int from, out int at)
    {
        int bestAt = -1;
        AddedToken? best = null;

        foreach (AddedToken candidate in _tokens)
        {
            // Bounded to bestAt + the candidate's own length: no candidate starting
            // later than bestAt could still win, so nothing here needs to scan further.
            int windowEnd = bestAt < 0 ? text.Length : Math.Min(text.Length, bestAt + candidate.Content.Length);
            int found = FirstMatch(text, candidate, from, windowEnd);
            if (found < 0)
            {
                continue;
            }
            if (bestAt < 0 || found < bestAt || (found == bestAt && candidate.Content.Length > best!.Content.Length))
            {
                bestAt = found;
                best = candidate;
            }
        }

        at = bestAt;
        return best;
    }

    /// <summary>The first index at or after <paramref name="from"/> where the entry may match.</summary>
    /// <remarks>
    /// A <see cref="AddedToken.SingleWord"/> entry rejected at one position can
    /// still match at a later one, so the search continues past a rejection
    /// rather than giving up.
    /// </remarks>
    private static int FirstMatch(string text, AddedToken candidate, int from, int windowEnd)
    {
        int at = from;
        while (at <= windowEnd - candidate.Content.Length)
        {
            int found = text.IndexOf(candidate.Content, at, windowEnd - at, StringComparison.Ordinal);
            if (found < 0)
            {
                return -1;
            }
            if (!candidate.SingleWord || IsWholeWord(text, found, found + candidate.Content.Length))
            {
                return found;
            }
            at = found + 1;
        }
        return -1;
    }

    private static bool IsWholeWord(string text, int start, int end) =>
        (start == 0 || !IsWordCharacter(text[start - 1]))
        && (end == text.Length || !IsWordCharacter(text[end]));

    private static bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';
}
