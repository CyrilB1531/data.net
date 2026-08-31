namespace Lodestar.Embeddings.Tokenization;

/// <summary>What a <c>decoder</c> block asks <see cref="BpeTokenizer.Decode(System.Collections.Generic.IReadOnlyList{int},bool)"/> to undo.</summary>
/// <remarks>
/// Three fields rather than a pipeline of steps: the SentencePiece-BPE lineage declares one
/// chain, <c>Sequence[Replace, ByteFallback, Fuse, Strip]</c>, and reproducing exactly it and
/// the bare <c>ByteFallback</c> is what decision 0063 decided. Any other shape is refused at
/// load, so nothing here has to describe it.
/// </remarks>
internal sealed class BpeDecoderSteps
{
    public BpeDecoderSteps(bool byteFallback, char? metaspaceReplacement, bool stripLeadingSpace)
    {
        ByteFallback = byteFallback;
        MetaspaceReplacement = metaspaceReplacement;
        StripLeadingSpace = stripLeadingSpace;
    }

    /// <summary>Whether <c>&lt;0xXX&gt;</c> pieces become the bytes they name.</summary>
    public bool ByteFallback { get; }

    /// <summary>The character a <c>Replace</c> step maps back onto U+0020, when one is declared.</summary>
    public char? MetaspaceReplacement { get; }

    /// <summary>Whether one leading space is stripped, which is what the prepended symbol became.</summary>
    public bool StripLeadingSpace { get; }
}
