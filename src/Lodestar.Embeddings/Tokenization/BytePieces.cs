using System.Globalization;

namespace Lodestar.Embeddings.Tokenization;

/// <summary>The <c>&lt;0xXX&gt;</c> pieces a <c>byte_fallback</c> vocabulary carries.</summary>
/// <remarks>
/// Both directions live here because encode names a byte and decode reads one back, and a
/// spelling that drifted between the two would resolve on one side only. Uppercase
/// hexadecimal is the reference's spelling, not a preference — decision 0063.
/// </remarks>
internal static class BytePieces
{
    /// <summary>How many pieces a byte_fallback vocabulary is required to carry.</summary>
    internal const int Count = 256;

    private const string Prefix = "<0x";

    private const int NameLength = 6;

    private static readonly string[] Names = BuildNames();

    /// <summary>The piece <paramref name="value"/> resolves to.</summary>
    internal static string Name(int value) => Names[value];

    /// <summary>The byte <paramref name="token"/> names, or <see langword="false"/> when it is not one of the 256.</summary>
    internal static bool TryValue(string token, out byte value)
    {
        value = 0;
        if (token.Length != NameLength
            || !token.StartsWith(Prefix, StringComparison.Ordinal)
            || token[NameLength - 1] != '>')
        {
            return false;
        }
        if (!TryHex(token[3], out int high) || !TryHex(token[4], out int low))
        {
            return false;
        }
        value = (byte)((high << 4) | low);
        return true;
    }

    /// <summary>One uppercase hexadecimal digit, or false — <c>'c'</c> is not one.</summary>
    private static bool TryHex(char digit, out int value)
    {
        if (digit >= '0' && digit <= '9')
        {
            value = digit - '0';
            return true;
        }
        if (digit >= 'A' && digit <= 'F')
        {
            value = digit - 'A' + 10;
            return true;
        }
        value = 0;
        return false;
    }

    private static string[] BuildNames()
    {
        var names = new string[Count];
        for (int b = 0; b < Count; b++)
        {
            names[b] = string.Format(CultureInfo.InvariantCulture, "<0x{0:X2}>", b);
        }
        return names;
    }
}
