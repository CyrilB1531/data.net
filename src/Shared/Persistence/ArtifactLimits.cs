using System.Globalization;

namespace Lodestar.Internal.Persistence;

/// <summary>
/// The numeric bounds applied while reading an artifact, in a form the shared
/// reading helpers can consume without knowing which assembly's public
/// <c>ArtifactLoadOptions</c> produced them.
/// </summary>
/// <remarks>
/// A loaded artifact is untrusted input: every count read from the file sizes a
/// buffer, so every count is checked against a limit before it is used. Exceeding
/// one is <see cref="InvalidDataException"/>, never <see cref="OutOfMemoryException"/>.
/// </remarks>
internal readonly struct ArtifactLimits
{
    /// <summary>Default cap on the number of vocabulary entries (1 000 000).</summary>
    public const int DefaultMaxVocabularySize = 1_000_000;

    /// <summary>Default cap on the length in characters of a single token (1024).</summary>
    public const int DefaultMaxTokenLength = 1024;

    /// <summary>Default cap on JSON nesting depth (32).</summary>
    public const int DefaultMaxJsonDepth = 32;

    /// <summary>Default cap on the total number of bytes read (256 MiB).</summary>
    public const long DefaultMaxTotalBytes = 256L * 1024 * 1024;

    /// <summary>Default cap on the length of any single JSON array (1 000 000).</summary>
    public const int DefaultMaxArrayLength = 1_000_000;

    /// <summary>Largest artifact read into one array, the CLR's own ceiling (#377).</summary>
    /// <remarks>
    /// Past this the read is segmented instead, which is not a limit a caller sets but a
    /// shape the runtime imposes. It is here rather than a <see langword="const"/> so a
    /// test can reach the segmented path at a few kilobytes: proving it by allocating two
    /// gibibytes is a test nobody runs, and an untested path is one nobody trusts.
    /// </remarks>
    public const long DefaultMaxSingleBuffer = 0x7FFFFFC7;

    public ArtifactLimits(
        int maxVocabularySize,
        int maxTokenLength,
        int maxJsonDepth,
        long maxTotalBytes,
        int maxArrayLength,
        long maxSingleBuffer = DefaultMaxSingleBuffer)
    {
        MaxVocabularySize = maxVocabularySize;
        MaxTokenLength = maxTokenLength;
        MaxJsonDepth = maxJsonDepth;
        MaxTotalBytes = maxTotalBytes;
        MaxArrayLength = maxArrayLength;
        MaxSingleBuffer = maxSingleBuffer;
    }

    /// <summary>The defaults, used when the caller passes no options.</summary>
    public static ArtifactLimits Default => new(
        DefaultMaxVocabularySize,
        DefaultMaxTokenLength,
        DefaultMaxJsonDepth,
        DefaultMaxTotalBytes,
        DefaultMaxArrayLength);

    public int MaxVocabularySize { get; }

    public int MaxTokenLength { get; }

    public int MaxJsonDepth { get; }

    public long MaxTotalBytes { get; }

    /// <summary>Largest artifact read into one array; past it the read is segmented.</summary>
    public long MaxSingleBuffer { get; }

    public int MaxArrayLength { get; }

    /// <summary>Throws if <paramref name="count"/> exceeds <see cref="MaxVocabularySize"/>.</summary>
    public void CheckVocabularySize(long count) =>
        Check(count, MaxVocabularySize, "vocabulary size", nameof(MaxVocabularySize));

    /// <summary>Throws if <paramref name="length"/> exceeds <see cref="MaxTokenLength"/>.</summary>
    public void CheckTokenLength(long length) =>
        Check(length, MaxTokenLength, "token length", nameof(MaxTokenLength));

    /// <summary>Throws if <paramref name="length"/> exceeds <see cref="MaxArrayLength"/>.</summary>
    public void CheckArrayLength(long length, string arrayName) =>
        Check(length, MaxArrayLength, $"length of array '{arrayName}'", nameof(MaxArrayLength));

    /// <summary>Throws if <paramref name="byteCount"/> exceeds <see cref="MaxTotalBytes"/>.</summary>
    public void CheckTotalBytes(long byteCount) =>
        Check(byteCount, MaxTotalBytes, "artifact size in bytes", nameof(MaxTotalBytes));

    private static void Check(long value, long limit, string what, string optionName)
    {
        if (value > limit)
        {
            throw new InvalidDataException(string.Format(
                CultureInfo.InvariantCulture,
                "The artifact exceeds the maximum {0}: {1} (limit {2}, from ArtifactLoadOptions.{3}).",
                what,
                value,
                limit,
                optionName));
        }
    }
}
