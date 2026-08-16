using System.Text.Json;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests.Tokenization;

public sealed class ByteLevelAlphabetTests
{
    [Fact]
    public void Table_matches_the_frozen_alphabet()
    {
        using JsonDocument doc = OracleLoader.Load("bytelevel_bpe.json");
        JsonElement expected = doc.RootElement.GetProperty("metadata").GetProperty("alphabet");

        Assert.Equal(256, expected.GetArrayLength());
        int b = 0;
        foreach (JsonElement entry in expected.EnumerateArray())
        {
            string mapped = entry.GetString()!;
            Assert.Equal(1, mapped.Length);
            Assert.Equal(mapped[0], ByteLevelAlphabet.ToChar((byte)b));
            b++;
        }
    }

    [Fact]
    public void Every_byte_round_trips_through_the_inverse()
    {
        for (int b = 0; b <= 255; b++)
        {
            char mapped = ByteLevelAlphabet.ToChar((byte)b);
            Assert.True(ByteLevelAlphabet.TryToByte(mapped, out byte back), $"0x{b:X2} -> '{mapped}' has no inverse");
            Assert.Equal((byte)b, back);
        }
    }

    [Fact]
    public void The_mapping_is_injective()
    {
        var seen = new HashSet<char>();
        for (int b = 0; b <= 255; b++)
        {
            Assert.True(seen.Add(ByteLevelAlphabet.ToChar((byte)b)), $"0x{b:X2} collides");
        }
    }

    [Fact]
    public void A_character_outside_the_alphabet_has_no_inverse()
    {
        Assert.False(ByteLevelAlphabet.TryToByte('東', out _));
    }
}
