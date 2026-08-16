using System.Text;
using Lodestar.Embeddings.Search;
using Xunit;

namespace Lodestar.Embeddings.Tests.Persistence;

/// <summary>
/// The stream shapes the read path has to survive beyond an ordinary file: a
/// source that will not say how long it is, one that says it wrong, and one that
/// hands over a few bytes at a time. Each is a branch of
/// <c>JsonArtifact.ReadAllBytes</c> that no other suite reaches.
/// </summary>
public sealed class EmbeddingIndexReadPathTests
{
    [Fact]
    public void A_non_seekable_source_loads_the_same_index()
    {
        byte[] artifact = Artifact();

        using var pipe = new UnseekableStream(artifact);
        AssertSameIndex(Reference(artifact), EmbeddingIndex.Load(pipe));
    }

    [Fact]
    public async Task A_non_seekable_source_loads_the_same_index_asynchronously()
    {
        byte[] artifact = Artifact();

        using var pipe = new UnseekableStream(artifact);
        AssertSameIndex(Reference(artifact), await EmbeddingIndex.LoadAsync(pipe));
    }

    [Fact]
    public void A_source_that_under_declares_its_length_loads_in_full()
    {
        // The fast path sizes its buffer from Length. A stream that reports less
        // than it holds must not be silently truncated to what it declared.
        byte[] artifact = Artifact();

        using var liar = new ShortLengthStream(artifact, declared: artifact.Length - 16);
        AssertSameIndex(Reference(artifact), EmbeddingIndex.Load(liar));
    }

    [Fact]
    public async Task A_source_that_under_declares_its_length_loads_in_full_asynchronously()
    {
        byte[] artifact = Artifact();

        using var liar = new ShortLengthStream(artifact, declared: artifact.Length - 16);
        AssertSameIndex(Reference(artifact), await EmbeddingIndex.LoadAsync(liar));
    }

    [Fact]
    public void A_source_that_answers_in_small_reads_still_fills_the_buffer()
    {
        // Read is allowed to return fewer bytes than asked for; a MemoryStream
        // never does, so nothing else in the suite exercises the fill loop.
        byte[] artifact = Artifact();

        using var trickle = new TrickleStream(artifact, chunk: 7);
        AssertSameIndex(Reference(artifact), EmbeddingIndex.Load(trickle));
    }

    [Fact]
    public void An_escaped_base64_token_loads_the_same_vectors()
    {
        // One base64 character written as its \u00XX escape makes ValueSpan differ from the decoded
        // value -- the only reachable trigger for the fallback decode, since a JSON string can't hold a raw newline.
        string json = Encoding.UTF8.GetString(Artifact());
        int start = json.IndexOf("\"vectors\":\"", StringComparison.Ordinal) + "\"vectors\":\"".Length;
        string escaped = string.Concat(
            json.AsSpan(0, start),
            $"\\u{(int)json[start]:x4}".AsSpan(),
            json.AsSpan(start + 1));

        Assert.NotEqual(json, escaped);
        AssertSameIndex(
            Reference(Encoding.UTF8.GetBytes(json)),
            EmbeddingIndex.Load(new MemoryStream(Encoding.UTF8.GetBytes(escaped))));
    }

    /// <summary>
    /// The index every case is compared against: the same bytes, read through an
    /// ordinary seekable stream. Kept out of the async tests as its own method
    /// because a synchronous <c>Load</c> inside an <c>async</c> body is a rule
    /// violation there, and awaiting a second path would compare two unknowns.
    /// </summary>
    private static EmbeddingIndex Reference(byte[] artifact) => EmbeddingIndex.Load(new MemoryStream(artifact));

    /// <summary>Two vectors of three dimensions, with ids — the ordinary artifact.</summary>
    private static byte[] Artifact()
    {
        var index = new EmbeddingIndex(dimension: 3);
        index.Add([1f, 0f, 0f], "a");
        index.Add([0.6f, 0.8f, 0f], "b");
        using var stream = new MemoryStream();
        index.Save(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Equality by re-serialization: the artifact is the whole observable state,
    /// so two indexes that save to the same bytes are the same index.
    /// </summary>
    private static void AssertSameIndex(EmbeddingIndex expected, EmbeddingIndex actual)
    {
        using var left = new MemoryStream();
        using var right = new MemoryStream();
        expected.Save(left);
        actual.Save(right);
        Assert.Equal(left.ToArray(), right.ToArray());
    }

    /// <summary>A read-only stream with no length and no seek — a pipe, in effect.</summary>
    private sealed class UnseekableStream : Stream
    {
        private readonly MemoryStream _inner;

        public UnseekableStream(byte[] bytes) => _inner = new MemoryStream(bytes);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>A seekable stream that under-reports its length and holds more.</summary>
    private sealed class ShortLengthStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly long _declared;

        public ShortLengthStream(byte[] bytes, long declared)
        {
            _inner = new MemoryStream(bytes);
            _declared = declared;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _declared;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>A seekable, honest stream that never returns more than <c>chunk</c> bytes at a time.</summary>
    private sealed class TrickleStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly int _chunk;

        public TrickleStream(byte[] bytes, int chunk)
        {
            _inner = new MemoryStream(bytes);
            _chunk = chunk;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            _inner.Read(buffer, offset, Math.Min(count, _chunk));

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
