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

    /// <summary>Slots for a pattern of <paramref name="m"/>, which the blocked path sizes per call.</summary>
    /// <remarks>
    /// <see cref="Capacity"/> is sound for one word only because 64 characters hold at most 64
    /// distinct symbols. A blocked pattern has no length bound, so a fixed table can fill and the
    /// probe then has no free slot to stop on — a power of two above <c>2m</c> restores the
    /// guarantee it relies on.
    /// </remarks>
    internal static int CapacityFor(int m)
    {
        int slots = Capacity;
        while (slots <= m * 2)
        {
            slots *= 2;
        }
        return slots;
    }

    /// <summary>The slot for <paramref name="c"/>, occupied by it or free.</summary>
    /// <remarks>
    /// <paramref name="keys"/> is a power of two long, which is what makes the mask cover it.
    /// Knuth's multiplicative hash rather than the low bits, because the characters that reach
    /// here cluster hard — an emoji pattern lives inside U+1F300..U+1FAFF, and masking those low
    /// bits alone would pile every symbol into a few slots. Myers' code-point path hashes for the
    /// same reason.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Probe(ReadOnlySpan<char> keys, char c)
    {
        int mask = keys.Length - 1;
        int index = (int)(((uint)c * 2654435761u) >> 23) & mask;
        while (keys[index] != '\0' && keys[index] != c)
        {
            index = (index + 1) & mask;
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

    /// <summary>Where a wide character's masks start in a blocked table, or -1 where the pattern lacks it.</summary>
    /// <remarks>
    /// The blocked table is one row of <c>blocks</c> words per symbol, so the side entries extend it
    /// rather than sitting beside it: slot <c>k</c> is row <c>denseEntries + k</c>, read with the
    /// base arithmetic a Latin-1 character already uses.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int BlockBase(ReadOnlySpan<char> keys, char c, int blocks, int denseEntries)
    {
        int slot = Probe(keys, c);
        return keys[slot] == c ? (denseEntries + slot) * blocks : -1;
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
