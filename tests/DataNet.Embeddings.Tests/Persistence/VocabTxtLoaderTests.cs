using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Persistence;

/// <summary>
/// Replays <c>vocab_txt.json</c>: the loader must read the exact file the Python
/// reference read, and the tokenizer built from it must produce the encoding
/// HuggingFace produced.
/// </summary>
public sealed class VocabTxtLoaderTests
{
    [Fact]
    public void Load_reads_the_ids_transformers_assigns()
    {
        using JsonDocument doc = OracleLoader.Load("vocab_txt.json");
        JsonElement meta = doc.RootElement.GetProperty("metadata");

        WordPieceVocabulary vocabulary = LoadFrom(meta.GetProperty("vocab_txt").GetString()!);

        var failures = new List<string>();
        foreach (JsonProperty expected in meta.GetProperty("vocab").EnumerateObject())
        {
            if (!vocabulary.Vocab.TryGetValue(expected.Name, out int id) || id != expected.Value.GetInt32())
            {
                failures.Add($"'{expected.Name}': expected {expected.Value.GetInt32()}, got {(vocabulary.Vocab.TryGetValue(expected.Name, out int a) ? a.ToString(System.Globalization.CultureInfo.InvariantCulture) : "absent")}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
        Assert.Equal(meta.GetProperty("vocab").EnumerateObject().Count(), vocabulary.Count);
        Assert.Equal(meta.GetProperty("unk_token").GetString(), vocabulary.UnkToken);
    }

    [Fact]
    public void The_loaded_vocabulary_drives_the_tokenizer_to_the_reference_encoding()
    {
        using JsonDocument doc = OracleLoader.Load("vocab_txt.json");
        JsonElement meta = doc.RootElement.GetProperty("metadata");
        var tokenizer = new WordPieceTokenizer(LoadFrom(meta.GetProperty("vocab_txt").GetString()!));

        OracleReplay.AssertEncodings(doc, tokenizer.Encode, "tokens");
    }

    [Fact]
    public void A_final_line_without_a_newline_is_still_a_token()
    {
        WordPieceVocabulary vocabulary = LoadFrom("[UNK]\nalpha\nbeta");

        Assert.Equal(3, vocabulary.Count);
        Assert.Equal(2, vocabulary.Vocab["beta"]);
    }

    [Fact]
    public void Carriage_returns_are_stripped_like_python_universal_newlines()
    {
        WordPieceVocabulary vocabulary = LoadFrom("[UNK]\r\nalpha\r\n");

        Assert.Equal(2, vocabulary.Count);
        Assert.Equal(1, vocabulary.Vocab["alpha"]);
    }

    [Fact]
    public void A_repeated_token_keeps_the_last_id_as_the_python_dictionary_does()
    {
        WordPieceVocabulary vocabulary = LoadFrom("[UNK]\nalpha\nalpha\n");

        Assert.Equal(2, vocabulary.Vocab["alpha"]);
    }

    [Fact]
    public void A_bare_carriage_return_terminates_a_line_like_python_universal_newlines()
    {
        // Python opens vocab.txt in text mode, where \r alone ends a line. Splitting on
        // \n only would fold a classic-Mac file into a single enormous token.
        WordPieceVocabulary vocabulary = LoadFrom("[UNK]\ralpha\rbeta\r");

        Assert.Equal(3, vocabulary.Count);
        Assert.Equal(2, vocabulary.Vocab["beta"]);
    }

    [Fact]
    public void A_byte_order_mark_is_not_part_of_the_first_token()
    {
        using var stream = new MemoryStream([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("[UNK]\nalpha\n")]);

        WordPieceVocabulary vocabulary = VocabTxtLoader.Load(stream);

        Assert.Equal(0, vocabulary.Vocab["[UNK]"]);
    }

    [Fact]
    public void An_empty_file_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(() => LoadFrom(string.Empty));
        Assert.Contains("empty", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_without_the_unknown_token_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(() => LoadFrom("alpha\nbeta\n"));
        Assert.Contains("no '[UNK]' entry", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_over_the_vocabulary_limit_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadFrom("[UNK]\na\nb\nc\n", new ArtifactLoadOptions { MaxVocabularySize = 2 }));

        Assert.Contains("MaxVocabularySize", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_token_over_the_length_limit_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadFrom("[UNK]\nabcdefghij\n", new ArtifactLoadOptions { MaxTokenLength = 5 }));

        Assert.Contains("MaxTokenLength", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_over_the_byte_limit_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadFrom("[UNK]\nalpha\n", new ArtifactLoadOptions { MaxTotalBytes = 4 }));

        Assert.Contains("MaxTotalBytes", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_leaves_the_caller_s_stream_open()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("[UNK]\nalpha\n"));

        _ = VocabTxtLoader.Load(stream);

        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task LoadAsync_reads_the_same_vocabulary()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("[UNK]\nalpha\nbeta\n"));

        WordPieceVocabulary vocabulary = await VocabTxtLoader.LoadAsync(stream);

        Assert.Equal(3, vocabulary.Count);
        Assert.Equal(2, vocabulary.Vocab["beta"]);
    }

    private static WordPieceVocabulary LoadFrom(string content, ArtifactLoadOptions? options = null)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return VocabTxtLoader.Load(stream, options);
    }
}
