using Lodestar.Embeddings.Persistence;
using Xunit;

namespace Lodestar.Embeddings.Tests.Persistence;

/// <summary>
/// Holds the <c>.npy</c> reader to files numpy actually wrote.
/// </summary>
/// <remarks>
/// Every fixture under <c>Fixtures/Npy</c> came out of <c>numpy.save</c> (2.4.6), not out
/// of a header this project built to match its own parser — which is the only way the
/// reader is held to the format rather than to itself. The refusals are fixtures too: a
/// big-endian file, a Fortran-ordered one, a float64 one and a pickled one all exist on
/// disk because a hand-built approximation of each would prove nothing.
/// </remarks>
public sealed class NpyFileTests
{
    private static string Fixture(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Npy", name);

    [Fact]
    public void A_matrix_numpy_wrote_reads_with_its_shape()
    {
        NpyBlock block = NpyFile.Read(Fixture("f4_2d_c.npy"));

        Assert.Equal([3, 4], block.Shape);
        Assert.Equal(12, block.Values.Length);
    }

    [Fact]
    public void A_vector_reads_as_one_dimension()
    {
        NpyBlock block = NpyFile.Read(Fixture("f4_1d.npy"));

        Assert.Equal([5], block.Shape);
        // Written as 1.0, -2.0, 3.5, 0.0, -0.0 — the signed zero included, since the
        // block carries raw bits and -0.0 is the cheapest way to notice a formatter.
        Assert.Equal([1.0f, -2.0f, 3.5f, 0.0f, -0.0f], block.Values.ToArray());
        Assert.True(float.IsNegative(block.Values.Span[4]));
    }

    [Fact]
    public void An_empty_matrix_reads_as_empty_rather_than_failing()
    {
        NpyBlock block = NpyFile.Read(Fixture("f4_2d_empty.npy"));

        Assert.Equal([0, 4], block.Shape);
        Assert.Equal(0, block.Values.Length);
    }

    [Fact]
    public void Non_finite_values_survive_the_read()
    {
        // The artifact format refuses these on write, because a NaN idf poisons every
        // later score. A .npy is not our artifact: it is somebody else's data, and
        // silently changing it on the way in would be worse than carrying it.
        float[] values = NpyFile.Read(Fixture("f4_nonfinite.npy")).Values.ToArray();

        Assert.True(float.IsNaN(values[0]));
        Assert.True(float.IsPositiveInfinity(values[1]));
        Assert.True(float.IsNegativeInfinity(values[2]));
    }

    [Theory]
    [InlineData("object_pickle.npy", "pickled objects")]
    [InlineData("f4_2d_bigendian.npy", "'>f4'")]
    [InlineData("f8_2d.npy", "'<f8'")]
    [InlineData("f4_2d_fortran.npy", "fortran_order=True")]
    [InlineData("f4_scalar.npy", "0-dimensional")]
    public void A_file_this_does_not_read_is_refused_by_name(string fixture, string says)
    {
        InvalidDataException refused =
            Assert.Throws<InvalidDataException>(() => NpyFile.Read(Fixture(fixture)));

        Assert.Contains(says, refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_pickled_file_is_refused_before_its_payload_is_touched()
    {
        // The whole point of the restricted parser: '|O' carries a pickle, which is
        // arbitrary code. It must fail on the header, not somewhere downstream.
        InvalidDataException refused =
            Assert.Throws<InvalidDataException>(() => NpyFile.Read(Fixture("object_pickle.npy")));

        Assert.Contains("executable", refused.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(new byte[] { 1, 2, 3 })]
    [InlineData(new byte[] { 0x93, (byte)'N', (byte)'U', (byte)'M', (byte)'P', (byte)'Y', 9, 0, 0, 0 })]
    public void A_file_that_is_not_one_is_refused(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);

        Assert.Throws<InvalidDataException>(() => NpyFile.Read(stream));
    }

    [Fact]
    public void A_truncated_payload_is_refused_rather_than_read_short()
    {
        byte[] whole = File.ReadAllBytes(Fixture("f4_2d_c.npy"));
        using var stream = new MemoryStream(whole[..^8]);

        InvalidDataException refused = Assert.Throws<InvalidDataException>(() => NpyFile.Read(stream));

        Assert.Contains("bytes of data where its shape needs", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void What_this_writes_is_what_numpy_wrote()
    {
        // Byte-for-byte against numpy's own file for the same array: the magic, the
        // version, the header text and its 64-byte padding, and the block.
        byte[] expected = File.ReadAllBytes(Fixture("f4_1d.npy"));

        using var written = new MemoryStream();
        NpyFile.Write(written, [1.0f, -2.0f, 3.5f, 0.0f, -0.0f], 5);

        Assert.Equal(expected, written.ToArray());
    }

    [Fact]
    public void A_matrix_this_writes_is_what_numpy_wrote()
    {
        byte[] expected = File.ReadAllBytes(Fixture("f4_2d_c.npy"));
        float[] values = NpyFile.Read(Fixture("f4_2d_c.npy")).Values.ToArray();

        using var written = new MemoryStream();
        NpyFile.Write(written, values, 3, 4);

        Assert.Equal(expected, written.ToArray());
    }

    [Fact]
    public void A_block_round_trips_through_this_reader_and_writer()
    {
        var values = new float[1_000];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = BitConverter.Int32BitsToSingle(0x3F800000 + (i * 7919));
        }

        using var written = new MemoryStream();
        NpyFile.Write(written, values, 250, 4);
        written.Position = 0;

        NpyBlock read = NpyFile.Read(written);

        Assert.Equal([250, 4], read.Shape);
        Assert.Equal(values, read.Values.ToArray());
    }

    [Theory]
    [InlineData(new[] { 3, 5 })]
    [InlineData(new[] { 0 })]
    [InlineData(new int[0])]
    [InlineData(new[] { 1, 2, 3 })]
    public void A_shape_that_does_not_describe_the_block_is_refused(int[] shape)
    {
        Assert.Throws<ArgumentException>(() => NpyFile.Write(new MemoryStream(), [1f, 2f, 3f, 4f], shape));
    }
}
