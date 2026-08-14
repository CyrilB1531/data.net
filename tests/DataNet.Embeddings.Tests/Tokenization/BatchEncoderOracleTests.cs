using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// Replays <c>batch_encoding.json</c>: the ids and the mask <c>tokenizers.Tokenizer.encode_batch</c>
/// produces, with the post-processor, padding and truncation enabled. Compared for equality, never a
/// tolerance -- these are integers, an id is the right one or it is not, and the only thing a tolerance
/// could absorb here is an off-by-one in the template, the defect this exercise exists to catch.
/// </summary>
public sealed class BatchEncoderOracleTests
{
    public static TheoryData<string> CaseNames()
    {
        var names = new TheoryData<string>();
        foreach (BatchCase batch in BatchCorpus.Oracle.Cases)
        {
            names.Add(batch.Name);
        }
        return names;
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void EncodeBatch_matches_huggingface(string name)
    {
        BatchCase expected = BatchCorpus.Oracle.Named(name);
        var encoder = new BatchEncoder(BatchCorpus.Tokenizer(), expected.Options);

        EncodedBatch actual = encoder.EncodeBatch(expected.Texts);

        Assert.Equal(expected.Texts.Length, actual.Count);
        Assert.Equal(expected.SequenceLength, actual.SequenceLength);
        for (int row = 0; row < expected.Texts.Length; row++)
        {
            ReadOnlySpan<long> ids = actual.InputIds.Slice(row * actual.SequenceLength, actual.SequenceLength);
            ReadOnlySpan<long> mask = actual.AttentionMask.Slice(row * actual.SequenceLength, actual.SequenceLength);
            Assert.Equal(expected.InputIds[row], ids.ToArray());
            Assert.Equal(expected.AttentionMask[row], mask.ToArray());
        }
    }

    /// <summary>
    /// The corpus froze the vocabulary with <c>[CLS]</c> at 45, <c>[SEP]</c> at 46
    /// and <c>[PAD]</c> at 47 — not at BERT's 101, 102 and 0, and not at the front
    /// of the file.
    /// </summary>
    /// <remarks>
    /// This is what makes the oracle above a test of the template rather than of a
    /// coincidence. An implementation that hardcoded any well-known id would
    /// disagree with every row of every case.
    /// </remarks>
    [Fact]
    public void Special_tokens_are_resolved_through_the_vocabulary()
    {
        Dictionary<string, int> vocabulary = BatchCorpus.Oracle.Vocabulary;
        Assert.Equal(45, vocabulary["[CLS]"]);
        Assert.Equal(46, vocabulary["[SEP]"]);
        Assert.Equal(47, vocabulary["[PAD]"]);

        long[] encoded = new BatchEncoder(BatchCorpus.Tokenizer()).Encode("the");
        Assert.Equal([45, vocabulary["the"], 46], encoded);
    }

    /// <summary>
    /// The four edges the issue asks for, read off the fixture the generator
    /// refuses to emit unless it still straddles the limit.
    /// </summary>
    [Fact]
    public void Edges_are_the_four_the_issue_names()
    {
        BatchCase edges = BatchCorpus.Oracle.Named("edges");
        int limit = edges.MaxLength!.Value;
        int[] lengths = edges.AttentionMask.Select(row => (int)row.Sum()).ToArray();

        Assert.Equal("", edges.Texts[0]);
        Assert.Equal(2, lengths[0]);            // nothing but [CLS] [SEP]
        Assert.Equal(3, lengths[1]);            // one token, wrapped
        Assert.Equal(limit, lengths[2]);        // exactly the limit, untouched
        Assert.Equal(limit, lengths[3]);        // one over the limit, truncated to it

        // The fourth text really is over the limit: without truncation it is longer.
        var unbounded = new BatchEncoder(BatchCorpus.Tokenizer());
        Assert.Equal(limit + 1, unbounded.Encode(edges.Texts[3]).Length);
    }
}
