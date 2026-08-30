using System.Text;
using System.Buffers.Binary;
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
        // Our artifact refuses these on write; a .npy is somebody else's data, and
        // altering it on the way in would be the worse failure.
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
    [Fact]
    public void A_block_past_a_million_elements_reads_at_the_default_options()
    {
        // 2 605 x 384 is a small embedding block and 1 000 320 elements, refused before #468
        // because MaxArrayLength was applied to elements rather than to the vectors it counts.
        const int Rows = 2_605, Columns = 384;
        var values = new float[Rows * Columns];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = i % 97;
        }

        using var written = new MemoryStream();
        NpyFile.Write(written, values, Rows, Columns);
        written.Position = 0;

        NpyBlock read = NpyFile.Read(written);

        Assert.Equal(values.Length, read.Values.Length);
        Assert.Equal([Rows, Columns], read.Shape);
        Assert.Equal(values[^1], read.Values.Span[^1]);
    }

    [Fact]
    public void A_header_declaring_more_elements_than_MaxTotalBytes_allows_is_refused()
    {
        // Hand-built, because numpy will not write a header whose shape its data does not
        // hold. That is the only way to reach the bound: a real file is refused earlier.
        byte[] hostile = Declaring("1000000, 1000");

        InvalidDataException refused =
            Assert.Throws<InvalidDataException>(() => NpyFile.Read(new MemoryStream(hostile)));

        Assert.Contains("1000000000 elements", refused.Message, StringComparison.Ordinal);
        Assert.Contains("MaxTotalBytes", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_payload_past_MaxTotalBytes_is_refused_while_being_read()
    {
        const int Rows = 2_605, Columns = 384;
        using var written = new MemoryStream();
        NpyFile.Write(written, new float[Rows * Columns], Rows, Columns);
        written.Position = 0;

        // Enforced by Elements once the header is parsed, unlike the test above: a real
        // file's honest shape crosses a caller's own (tighter) MaxTotalBytes just as well.
        var options = new ArtifactLoadOptions { MaxTotalBytes = (Rows * Columns * sizeof(float)) - 1 };

        Assert.Throws<InvalidDataException>(() => NpyFile.Read(written, options));
    }

    /// <summary>A stream that reports no length and no seeking, like a network body.</summary>
    private sealed class ForwardOnlyStream(byte[] bytes) : Stream
    {
        private readonly MemoryStream _inner = new(bytes);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            _inner.Read(buffer, offset, Math.Min(count, 7));

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public void A_forward_only_stream_reads_the_same_block()
    {
        byte[] npy = WrittenBlock([1f, 2f, 3f, 4f], 2, 2);

        using var stream = new ForwardOnlyStream(npy);
        NpyBlock block = NpyFile.Read(stream);

        Assert.Equal([1f, 2f, 3f, 4f], block.Values.ToArray());
        Assert.Equal([2, 2], block.Shape);
    }

    [Fact]
    public void A_stream_truncated_inside_its_payload_is_refused()
    {
        byte[] npy = WrittenBlock([1f, 2f, 3f, 4f], 2, 2);

        // Four bytes short: the header is whole and the block is not, which is the case
        // the staged read detects at the stream rather than on a complete buffer.
        var truncated = new MemoryStream(npy[..(npy.Length - 4)]);

        InvalidDataException e = Assert.Throws<InvalidDataException>(() => NpyFile.Read(truncated));
        Assert.Contains("bytes of data where its shape needs", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_header_declaring_an_absurd_length_is_refused_before_it_is_measured()
    {
        // Version 2.0's length field is 4 bytes wide; a declared value this large would
        // overflow the offset arithmetic instead of being disbelieved by name (#466).
        byte[] bytes =
        [
            0x93, (byte)'N', (byte)'U', (byte)'M', (byte)'P', (byte)'Y',
            2, 0, // version 2.0
            0, 0, 0, 0, // declared header length, overwritten below
        ];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), uint.MaxValue - 1);

        InvalidDataException refused =
            Assert.Throws<InvalidDataException>(() => NpyFile.Read(new MemoryStream(bytes)));

        Assert.Contains("more than NpyFile.MaxHeaderLength (65536) allows", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_version_2_header_cut_short_inside_its_length_field_is_refused_by_name()
    {
        // Eleven bytes: magic, version 2.0, three of the four length bytes -- HeaderTotal
        // must see only what the stream gave it, not a stackalloc byte it never read (#466).
        byte[] bytes =
        [
            0x93, (byte)'N', (byte)'U', (byte)'M', (byte)'P', (byte)'Y',
            2, 0, // version 2.0
            1, 2, 3, // three of the four length bytes
        ];

        InvalidDataException refused =
            Assert.Throws<InvalidDataException>(() => NpyFile.Read(new MemoryStream(bytes)));

        Assert.Contains("ends inside its header length.", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_ten_byte_non_numpy_stream_is_refused_by_its_magic()
    {
        // At least MinPrefix bytes, so the staged path parses it directly rather than
        // falling back to the short-prefix buffered path (#466 regression).
        byte[] bytes = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        InvalidDataException refused =
            Assert.Throws<InvalidDataException>(() => NpyFile.Read(new MemoryStream(bytes)));

        Assert.Contains("does not open with numpy's magic.", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>A .npy of the given block, as NpyFile writes one.</summary>
    private static byte[] WrittenBlock(float[] values, params int[] shape)
    {
        using var stream = new MemoryStream();
        NpyFile.Write(stream, values, shape);
        return stream.ToArray();
    }

    /// <summary>A well-formed v1.0 header for <paramref name="shape"/>, and no data at all.</summary>
    private static byte[] Declaring(string shape)
    {
        string dictionary = $"{{'descr': '<f4', 'fortran_order': False, 'shape': ({shape}), }}";
        int unpadded = 10 + dictionary.Length + 1;
        string padded = dictionary + new string(' ', (64 - (unpadded % 64)) % 64) + "\n";
        byte[] header = Encoding.ASCII.GetBytes(padded);

        var file = new byte[10 + header.Length];
        Encoding.ASCII.GetBytes("\u0093NUMPY").CopyTo(file, 0);
        file[0] = 0x93;
        file[6] = 1;
        file[7] = 0;
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(8), (ushort)header.Length);
        header.CopyTo(file, 10);
        return file;
    }
}
