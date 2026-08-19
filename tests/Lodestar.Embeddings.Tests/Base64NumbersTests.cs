using System.Text;
using System.Text.Json;
using Lodestar.Internal.Persistence;
using Xunit;

namespace Lodestar.Embeddings.Tests;

/// <summary>
/// Pins the wire format of the vector block, which is the largest thing in any
/// artifact and the one part no reader checks by eye.
/// </summary>
/// <remarks>
/// <c>bench/README.md</c> records that an index saved before #100 and after it is the
/// same file, verified by hash. Nothing held the writer to that: a round trip proves
/// the pair agree with each other, not that either agrees with what shipped. #323
/// changed this writer, so the encoding is pinned rather than re-derived from it.
/// </remarks>
public sealed class Base64NumbersTests
{
    [Fact]
    public void WriteSingles_emits_raw_little_endian_bits()
    {
        // 1f is 0x3F800000, -2f is 0xC0000000, 3.5f is 0x40600000 — least significant
        // byte first, which is what the property name promises a reader on any machine.
        Assert.Equal(
            """{"v":"AACAPwAAAMAAAGBA"}""",
            WriteSingles([1f, -2f, 3.5f]));
    }

    [Fact]
    public void WriteSingles_emits_nothing_for_an_empty_block()
    {
        Assert.Equal("""{"v":""}""", WriteSingles([]));
    }

    [Fact]
    public void WriteSingles_round_trips_through_ReadSingles()
    {
        float[] values = [0f, -0f, 1e-30f, 3.4e38f, -1.5f];

        string json = WriteSingles(values);
        // Two reads to land on the property value: the object start, then its name.
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        reader.Read();

        Assert.Equal(values, Base64Numbers.ReadSingles(ref reader, "test", "v"));
    }

    private static string WriteSingles(ReadOnlySpan<float> values)
    {
        var buffer = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            Base64Numbers.WriteSingles(writer, "v", values);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
