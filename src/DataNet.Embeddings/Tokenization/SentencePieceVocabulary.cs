namespace DataNet.Embeddings.Tokenization;

// CA1008: these values mirror SentencePiece's own ModelProto.SentencePiece.Type
// numbering (from 1, below); a synthetic None = 0 would carry no model file's value.
#pragma warning disable CA1008

/// <summary>
/// What a piece is for, as recorded in the <c>spiece.model</c> proto.
/// </summary>
/// <remarks>
/// The numeric values are the ones in <c>sentencepiece_model.proto</c>
/// (<c>ModelProto.SentencePiece.Type</c>) and are part of the file format, not an
/// internal detail. <c>Control</c> and <c>Unknown</c> pieces must never match
/// real text: <c>&lt;s&gt;</c> is a marker the model emits, not a string a user
/// can type.
/// </remarks>
public enum SentencePieceType
{
    /// <summary>An ordinary piece, matched against text.</summary>
    Normal = 1,

    /// <summary>The unknown piece — surfaced only for characters nothing covers.</summary>
    Unknown = 2,

    /// <summary>A control marker such as <c>&lt;s&gt;</c> or <c>&lt;/s&gt;</c>; never matched against text.</summary>
    Control = 3,

    /// <summary>A user-defined piece; matched against text like a normal one.</summary>
    UserDefined = 4,

    /// <summary>A piece dropped during training.</summary>
    Unused = 5,

    /// <summary>A single byte, used by byte-fallback models.</summary>
    Byte = 6,
}

/// <summary>
/// A pretrained SentencePiece unigram vocabulary, as read from a
/// <c>spiece.model</c> or the <c>model</c> section of a <c>tokenizer.json</c>.
/// </summary>
/// <remarks>
/// The piece <em>types</em> are the point of this record: without them a
/// tokenizer has to guess which entries are control markers by id, which fails
/// silently for any model that does not happen to put <c>&lt;unk&gt;</c>,
/// <c>&lt;s&gt;</c> and <c>&lt;/s&gt;</c> at 0, 1 and 2.
/// </remarks>
/// <param name="Pieces">The pieces with their scores, indexed by id.</param>
/// <param name="Types">The type of each piece, aligned with <paramref name="Pieces"/>.</param>
/// <param name="UnkId">Id of the unknown piece.</param>
/// <param name="BosId">Id of the beginning-of-sentence piece, or <c>-1</c> if the model has none.</param>
/// <param name="EosId">Id of the end-of-sentence piece, or <c>-1</c> if the model has none.</param>
/// <param name="PadId">Id of the padding piece, or <c>-1</c> if the model has none.</param>
public sealed record SentencePieceVocabulary(
    IReadOnlyList<SentencePiece> Pieces,
    IReadOnlyList<SentencePieceType> Types,
    int UnkId,
    int BosId,
    int EosId,
    int PadId)
{
    /// <summary>Number of pieces in the vocabulary.</summary>
    public int Count => Pieces.Count;

    /// <summary>
    /// The normalization the model carries in its <c>precompiled_charsmap</c>, or
    /// <c>null</c> for the <c>identity</c> normalizer.
    /// </summary>
    /// <remarks>
    /// Not a constructor parameter: a vocabulary built by hand has no charsmap,
    /// and <c>null</c> — meaning "the text reaches the tokenizer as it was written"
    /// — is the only sound default. <see cref="Persistence.SentencePieceModelLoader"/>
    /// sets it from the file.
    /// </remarks>
    public PrecompiledNormalizer? Normalizer { get; init; }

    /// <summary>Compares the special-token ids and the normalizer, then every piece and every type.</summary>
    /// <param name="other">The vocabulary to compare against.</param>
    /// <remarks>
    /// The generated equality would compare <see cref="Pieces"/> and
    /// <see cref="Types"/> by reference, so two vocabularies read from the same
    /// <c>spiece.model</c> would be unequal.
    /// </remarks>
    public bool Equals(SentencePieceVocabulary? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (other is null
            || UnkId != other.UnkId || BosId != other.BosId
            || EosId != other.EosId || PadId != other.PadId
            || Pieces.Count != other.Pieces.Count
            || Types.Count != other.Types.Count
            || !Equals(Normalizer, other.Normalizer))
        {
            return false;
        }
        for (int i = 0; i < Pieces.Count; i++)
        {
            if (!Pieces[i].Equals(other.Pieces[i]))
            {
                return false;
            }
        }
        for (int i = 0; i < Types.Count; i++)
        {
            if (Types[i] != other.Types[i])
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Hashes the counts and the special-token ids, which is O(1).</summary>
    /// <remarks>
    /// Equal vocabularies necessarily agree on all of these; unequal ones are
    /// allowed to collide. Hashing a quarter of a million pieces would defeat the
    /// purpose of a hash.
    /// </remarks>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 31) + Pieces.Count;
            hash = (hash * 31) + Types.Count;
            hash = (hash * 31) + UnkId;
            hash = (hash * 31) + BosId;
            hash = (hash * 31) + EosId;
            return (hash * 31) + PadId;
        }
    }

    /// <summary>Whether the piece with this id may be matched against input text.</summary>
    /// <remarks>
    /// Control and unknown pieces are excluded; normal, user-defined, unused and
    /// byte pieces are not. This is the check that replaces guessing by id.
    /// </remarks>
    /// <param name="id">A piece id, in <c>[0, Types.Count)</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="id"/> is outside <see cref="Types"/>. The record is a data
    /// carrier and does not itself require <see cref="Pieces"/> and
    /// <see cref="Types"/> to agree in length — <see cref="SentencePieceTokenizer"/>
    /// is what refuses a vocabulary where they do not. Reporting that here as a
    /// raw index failure would name neither the argument nor the reason.
    /// </exception>
    public bool IsMatchable(int id)
    {
        if ((uint)id >= (uint)Types.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                id,
                $"The piece id is outside the vocabulary's {Types.Count} declared types.");
        }
        return Types[id] is not (SentencePieceType.Control or SentencePieceType.Unknown);
    }
}
