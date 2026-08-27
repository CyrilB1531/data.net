using System.Text;
using System.Text.Json;
using Lodestar.Embeddings.Search;
using Lodestar.Internal.Persistence;
using Xunit;

namespace Lodestar.Embeddings.Tests.Persistence;

/// <summary>
/// Holds the sliced write to the one-shot write it replaced, byte for byte.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Base64Numbers.WriteSinglesChunked"/> exists only to stop the writer's
/// buffer growing to hold a whole encoding; it is not allowed to change a single byte
/// of what reaches the file. <see cref="Base64Numbers.WriteSingles"/> is kept as the
/// reference implementation these compare against — it is no longer on any save path,
/// and that is precisely what makes it a useful oracle rather than dead code.
/// </para>
/// <para>
/// The sizes below are chosen around the 245 760-byte slice: one slice exactly, one
/// short of it, one past it, and the awkward remainders. A slice is a whole number of
/// base64 groups, so only the final one may pad — a boundary landing inside a group
/// would show up here as a run of <c>=</c> in the middle of the string.
/// </para>
/// </remarks>
public sealed class ChunkedBlockTests
{
    /// <summary>Floats per slice: 245 760 bytes over 4.</summary>
    private const int FloatsPerSlice = 61_440;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(FloatsPerSlice - 1)]
    [InlineData(FloatsPerSlice)]
    [InlineData(FloatsPerSlice + 1)]
    [InlineData(FloatsPerSlice + 3)]
    [InlineData((2 * FloatsPerSlice) + 17)]
    public void A_sliced_write_is_byte_identical_to_the_one_shot_write(int count)
    {
        float[] values = Block(count);

        Assert.Equal(OneShot(values), Sliced(values));
    }

    [Fact]
    public void A_sliced_write_never_pads_before_the_end()
    {
        // Two full slices and a remainder: the only '=' allowed is in the final group.
        string quoted = Sliced(Block((2 * FloatsPerSlice) + 5));
        string encoded = quoted[1..^1];
        int firstPad = encoded.IndexOf('=', StringComparison.Ordinal);

        Assert.True(firstPad < 0 || firstPad >= encoded.Length - 2, $"padding at {firstPad} of {encoded.Length}");
    }

    [Fact]
    public void An_index_saved_through_the_sliced_path_reloads_identical()
    {
        var index = new EmbeddingIndex(dimension: 48, normalize: false);
        float[] vector = new float[48];
        for (int item = 0; item < 500; item++)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] = (item * 48f) + i - 1000f;
            }
            index.Add(vector, $"doc-{item}");
        }

        using var stream = new MemoryStream();
        index.Save(stream);
        stream.Position = 0;
        EmbeddingIndex reloaded = EmbeddingIndex.Load(stream);

        Assert.Equal(index.Count, reloaded.Count);
        Assert.Equal(index.Dimension, reloaded.Dimension);
        Assert.Equal(index.Search(vector, 5), reloaded.Search(vector, 5));

        // Saving what was loaded reproduces the artifact byte for byte, which is a
        // stronger statement than any comparison of the two indexes: it holds the
        // vectors, the ids and the encoding to exactness in one assertion.
        using var again = new MemoryStream();
        reloaded.Save(again);
        Assert.Equal(stream.ToArray(), again.ToArray());
    }

    [Fact]
    public async Task The_asynchronous_save_writes_the_same_bytes_as_the_synchronous_one()
    {
        var index = new EmbeddingIndex(dimension: 32, normalize: true);
        float[] vector = new float[32];
        for (int item = 0; item < 2_000; item++)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] = ((item * 7) + i) % 101 / 50f;
            }
            index.Add(vector, $"id-{item}");
        }

        using var synchronous = new MemoryStream();
        // SonarLint S6966: awaiting SaveAsync here is exactly what this test must not do.
        // The synchronous call is one of the two things being compared.
#pragma warning disable S6966
        index.Save(synchronous);
#pragma warning restore S6966

        using var asynchronous = new MemoryStream();
        await index.SaveAsync(asynchronous, CancellationToken.None);

        Assert.Equal(synchronous.ToArray(), asynchronous.ToArray());
    }

    /// <summary>A block whose bits vary across every byte lane, so a swapped pair would show.</summary>
    private static float[] Block(int count)
    {
        var values = new float[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = BitConverter.Int32BitsToSingle(0x3F800000 + (i * 7919));
        }
        return values;
    }

    /// <summary>The reference: one <c>WriteBase64String</c> call over the whole block.</summary>
    private static string OneShot(ReadOnlySpan<float> values)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, JsonArtifact.WriterOptions))
        {
            writer.WriteStartObject();
            Base64Numbers.WriteSingles(writer, "v", values);
            writer.WriteEndObject();
        }

        // Everything between the property's colon and the object's closing brace: the
        // quoted value, quotes included, which is exactly what the sliced writer emits.
        string json = Encoding.UTF8.GetString(buffer.ToArray());
        return json["""{"v":""".Length..^1];
    }

    /// <summary>What the save path now writes: the same value, a slice at a time.</summary>
    private static string Sliced(ReadOnlySpan<float> values)
    {
        using var buffer = new MemoryStream();
        Base64Numbers.WriteSinglesChunked(buffer, values);

        // The quotes are part of what this writer emits, so no trimming: the two
        // helpers return the same shape and the comparison covers the quoting too.
        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
