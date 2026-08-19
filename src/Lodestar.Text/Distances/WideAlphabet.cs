using System.Runtime.CompilerServices;

namespace Lodestar.Text.Distances;

/// <summary>The side table that carries a pattern's characters above Latin-1, for both kernels.</summary>
/// <remarks>
/// The 256-entry equality table is indexed by the character, so a pattern holding anything above
/// U+00FF could not be represented in it and the kernels refused — CJK and emoji never took the
/// bit-parallel path in the UTF-16 mode. Generalising the whole table is what the code-point path
/// already does, and #208 measured it crossing the dynamic program later for that reason. This
/// keeps the dense table for the common characters and puts the rare ones beside it, so the
/// Latin-1 path is unchanged and only a pattern that needs the side table pays for it (#302).
/// </remarks>
internal static class WideAlphabet
{
    /// <summary>Slots, a power of two and twice what a single-word pattern can hold.</summary>
    /// <remarks>
    /// A free slot is therefore guaranteed and linear probing terminates. <c>'\0'</c> marks free,
    /// which costs nothing: it is Latin-1, so it never reaches this table.
    /// </remarks>
    internal const int Capacity = 128;

    /// <summary>The slot for <paramref name="c"/>, occupied by it or free.</summary>
    /// <remarks>
    /// Knuth's multiplicative hash rather than the low bits, because the characters that reach
    /// here cluster hard — an emoji pattern lives inside U+1F300..U+1FAFF, and masking those low
    /// bits alone would pile every symbol into a few slots. Myers' code-point path hashes for the
    /// same reason.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Probe(ReadOnlySpan<char> keys, char c)
    {
        int index = (int)(((uint)c * 2654435761u) >> 23) & (Capacity - 1);
        while (keys[index] != '\0' && keys[index] != c)
        {
            index = (index + 1) & (Capacity - 1);
        }
        return index;
    }

    /// <summary>Builds both tables at once: the dense one below U+0100, the side one above it.</summary>
    internal static void Fill(
        ReadOnlySpan<char> pattern, Span<ulong> table, Span<char> keys, Span<ulong> masks)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            if (c <= 0xFF)
            {
                table[c] |= 1UL << i;
            }
            else
            {
                int slot = Probe(keys, c);
                keys[slot] = c;
                masks[slot] |= 1UL << i;
            }
        }
    }

    /// <summary>The mask for a text character above Latin-1, or zero where the pattern lacks it.</summary>
    /// <remarks>
    /// <paramref name="keys"/> is empty on the Latin-1 path, which is the branch that keeps that
    /// path free of the probe: the callers pass <c>default</c>, so it folds away where the kernel
    /// is inlined rather than costing a test per text character.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Lookup(ReadOnlySpan<char> keys, ReadOnlySpan<ulong> masks, char c)
    {
        if (keys.IsEmpty)
        {
            return 0UL;
        }
        int slot = Probe(keys, c);
        return keys[slot] == c ? masks[slot] : 0UL;
    }
}
