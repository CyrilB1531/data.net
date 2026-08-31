using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests.Tokenization;

/// <summary>The 256 pieces a byte_fallback vocabulary carries, named in both directions.</summary>
/// <remarks>
/// The spelling is uppercase hexadecimal, measured: a vocabulary writing <c>&lt;0xc3&gt;</c>
/// resolves nothing in <c>tokenizers</c> 0.23.1 and falls to the unknown token, so a
/// case-insensitive reader here would accept a file the reference rejects.
/// </remarks>
public sealed class BytePiecesTests
{
    [Fact]
    public void Every_byte_has_a_name_and_the_name_reads_back()
    {
        for (int b = 0; b <= 255; b++)
        {
            string name = BytePieces.Name(b);

            Assert.True(BytePieces.TryValue(name, out byte value), name);
            Assert.Equal(b, value);
        }
    }

    [Fact]
    public void The_spelling_is_uppercase_hexadecimal()
    {
        Assert.Equal("<0x00>", BytePieces.Name(0));
        Assert.Equal("<0x0A>", BytePieces.Name(10));
        Assert.Equal("<0xC3>", BytePieces.Name(0xC3));
        Assert.Equal("<0xFF>", BytePieces.Name(255));
    }

    [Theory]
    [InlineData("<0xc3>")]
    [InlineData("<0Xc3>")]
    [InlineData("<0xC3")]
    [InlineData("0xC3>")]
    [InlineData("<0xC>")]
    [InlineData("<0xC33>")]
    [InlineData("<0xGG>")]
    [InlineData("")]
    [InlineData("a")]
    public void Nothing_else_is_a_byte_piece(string token) =>
        Assert.False(BytePieces.TryValue(token, out _));
}
