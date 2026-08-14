using System.Text.Json;

namespace DataNet.Internal.Persistence;

/// <summary>
/// The two properties every DataNet artifact opens with — <c>"$schema"</c>
/// identifying the artifact kind and <c>"version"</c> its format revision — plus
/// the reader-side state that proves both were present and understood.
/// </summary>
/// <remarks>
/// Numbered per artifact and checked before anything else is trusted (ADR 0011, "The header").
/// Written first for byte-reproducible output but read in any position — see
/// <c>ArtifactHardeningTests.A_vocabulary_written_before_the_feature_count_still_loads</c>.
/// </remarks>
internal struct ArtifactHeader
{
    /// <summary>The property naming the artifact kind.</summary>
    public const string SchemaProperty = "$schema";

    /// <summary>The property carrying the format revision.</summary>
    public const string VersionProperty = "version";

    private readonly string _artifact;
    private readonly int _supportedVersion;
    private bool _sawSchema;
    private bool _sawVersion;

    /// <summary>Prepares to read the header of a <paramref name="artifact"/> artifact.</summary>
    /// <param name="artifact">The artifact kind, e.g. <c>tfidf-vectorizer</c>.</param>
    /// <param name="supportedVersion">The highest revision this build can read.</param>
    public ArtifactHeader(string artifact, int supportedVersion)
    {
        _artifact = artifact;
        _supportedVersion = supportedVersion;
        _sawSchema = false;
        _sawVersion = false;
        Version = 0;
    }

    /// <summary>The revision declared by the file, valid once <see cref="EnsureComplete"/> has passed.</summary>
    public int Version { get; private set; }

    /// <summary>The <c>"$schema"</c> value for an artifact kind, e.g. <c>datanet/tfidf-vectorizer</c>.</summary>
    public static string SchemaFor(string artifact) => "datanet/" + artifact;

    /// <summary>Writes the header into an object the caller has already opened.</summary>
    public static void Write(Utf8JsonWriter writer, string artifact, int version)
    {
        writer.WriteString(SchemaProperty, SchemaFor(artifact));
        writer.WriteNumber(VersionProperty, version);
    }

    /// <summary>
    /// Consumes <paramref name="propertyName"/> if it is one of the header
    /// properties, leaving the reader positioned on its value.
    /// </summary>
    /// <returns><c>true</c> when the property was a header property and was validated.</returns>
    public bool TryConsume(ref Utf8JsonReader reader, string propertyName)
    {
        switch (propertyName)
        {
            case SchemaProperty:
                ReadSchema(ref reader);
                return true;
            case VersionProperty:
                ReadVersion(ref reader);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Throws unless both header properties were seen.</summary>
    public readonly void EnsureComplete()
    {
        if (!_sawSchema)
        {
            throw JsonArtifact.MissingProperty(_artifact, SchemaProperty);
        }
        if (!_sawVersion)
        {
            throw JsonArtifact.MissingProperty(_artifact, VersionProperty);
        }
    }

    private void ReadSchema(ref Utf8JsonReader reader)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.String)
        {
            throw JsonArtifact.UnexpectedToken(_artifact, SchemaProperty, reader.TokenType);
        }

        string expected = SchemaFor(_artifact);
        if (!reader.ValueTextEquals(expected))
        {
            throw new InvalidDataException(
                $"Expected a '{expected}' artifact but the file declares '{SchemaProperty}' = '{reader.GetString()}'.");
        }
        _sawSchema = true;
    }

    private void ReadVersion(ref Utf8JsonReader reader)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int version))
        {
            throw JsonArtifact.UnexpectedToken(_artifact, VersionProperty, reader.TokenType);
        }
        if (version < 1 || version > _supportedVersion)
        {
            throw new InvalidDataException(
                $"Unsupported '{SchemaFor(_artifact)}' artifact version {version}; this build of DataNet reads versions 1 to {_supportedVersion}.");
        }
        Version = version;
        _sawVersion = true;
    }
}
