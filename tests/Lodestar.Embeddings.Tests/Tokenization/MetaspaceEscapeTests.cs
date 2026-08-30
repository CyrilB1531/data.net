using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests;

/// <summary>
/// The transform both the unigram and the SentencePiece-BPE paths apply, alone.
/// </summary>
/// <remarks>
/// Decision 0050 makes it one thing rather than ten lines inside one tokenizer,
/// because the two spellings a file may use — a <c>Metaspace</c> pre-tokenizer and a
/// <c>Prepend</c>+<c>Replace</c> normalizer — are two writings of one value (#316),
/// bar the prepend guard decision 0062 measures and this class's last three cases pin.
/// </remarks>
public sealed class MetaspaceEscapeTests
{
    [Fact]
    public void Every_space_becomes_the_replacement_and_the_text_is_prefixed()
    {
        var escape = new MetaspaceEscape('▁', MetaspacePrependScheme.Always, removeExtraWhitespaces: false, skipPrependWhenAlreadyPrefixed: false);

        Assert.Equal("▁hello▁world", escape.Apply("hello world"));
    }

    [Fact]
    public void Never_replaces_without_prefixing()
    {
        var escape = new MetaspaceEscape('▁', MetaspacePrependScheme.Never, removeExtraWhitespaces: false, skipPrependWhenAlreadyPrefixed: false);

        Assert.Equal("hello▁world", escape.Apply("hello world"));
    }

    [Fact]
    public void First_and_Always_agree_while_nothing_splits()
    {
        // Both target models declare split: false, so there is one piece and the two
        // schemes cannot be told apart — pinned so a splitting model finds it deliberate.
        var first = new MetaspaceEscape('▁', MetaspacePrependScheme.First, removeExtraWhitespaces: false, skipPrependWhenAlreadyPrefixed: false);
        var always = new MetaspaceEscape('▁', MetaspacePrependScheme.Always, removeExtraWhitespaces: false, skipPrependWhenAlreadyPrefixed: false);

        Assert.Equal(always.Apply("hello world"), first.Apply("hello world"));
    }

    [Fact]
    public void Extra_whitespace_survives_when_the_file_does_not_ask_for_its_removal()
    {
        // Neither Llama-2 nor Mistral v0.1 declares remove_extra_whitespaces, so the runs
        // and the trailing space stay — the unigram path is the one that collapses them.
        var escape = new MetaspaceEscape('▁', MetaspacePrependScheme.Always, removeExtraWhitespaces: false, skipPrependWhenAlreadyPrefixed: false);

        Assert.Equal("▁a▁▁▁b▁", escape.Apply("a   b "));
    }

    [Fact]
    public void Removing_extra_whitespace_collapses_runs_and_trims()
    {
        // What SentencePieceTokenizer has always done, and what its oracles pin.
        var escape = new MetaspaceEscape('▁', MetaspacePrependScheme.Always, removeExtraWhitespaces: true, skipPrependWhenAlreadyPrefixed: false);

        Assert.Equal("▁a▁b", escape.Apply("  a   b  "));
    }

    [Fact]
    public void Only_the_space_is_collapsed_by_that_flag()
    {
        // remove_extra_whitespaces collapses runs of U+0020 only: a tab no normalizer
        // rewrote stays as it is, which docs/equivalence.md's Unigram row records.
        var escape = new MetaspaceEscape('▁', MetaspacePrependScheme.Always, removeExtraWhitespaces: true, skipPrependWhenAlreadyPrefixed: false);

        Assert.Equal("▁a\tb", escape.Apply("a\tb"));
    }

    [Fact]
    public void Text_that_collapses_to_nothing_is_not_prefixed()
    {
        // SentencePieceTokenizer returns before prepending when nothing survives the
        // collapse, and the extraction must not quietly start returning a lone symbol.
        var escape = new MetaspaceEscape('▁', MetaspacePrependScheme.Always, removeExtraWhitespaces: true, skipPrependWhenAlreadyPrefixed: false);

        Assert.Equal(string.Empty, escape.Apply("   "));
        Assert.Equal(string.Empty, escape.Apply(string.Empty));
    }

    [Fact]
    public void The_guard_skips_the_prepend_when_the_escaped_text_already_begins_with_the_symbol()
    {
        // A Metaspace block's own starts_with check, which reads the text after the
        // replace: a leading space becomes the symbol and so meets the guard.
        var escape = new MetaspaceEscape('▁', MetaspacePrependScheme.Always, removeExtraWhitespaces: false, skipPrependWhenAlreadyPrefixed: true);

        Assert.Equal("▁the▁cat", escape.Apply(" the cat"));
        Assert.Equal("▁the▁cat", escape.Apply("▁the cat"));
        Assert.Equal("▁the▁cat", escape.Apply("the cat"));
    }

    [Fact]
    public void Without_the_guard_the_prepend_is_unconditional()
    {
        // The normalizer spelling: Prepend runs before Replace, so it never sees the
        // symbol the leading space is about to become, and prepends a second one.
        var escape = new MetaspaceEscape('▁', MetaspacePrependScheme.First, removeExtraWhitespaces: false, skipPrependWhenAlreadyPrefixed: false);

        Assert.Equal("▁▁the▁cat", escape.Apply(" the cat"));
        Assert.Equal("▁▁the▁cat", escape.Apply("▁the cat"));
        Assert.Equal("▁the▁cat", escape.Apply("the cat"));
    }

    [Fact]
    public void The_guard_changes_nothing_under_never()
    {
        // Never prepends, so it has no prepend to guard — which is why the corpus
        // records no divergence on that scheme.
        var guarded = new MetaspaceEscape('▁', MetaspacePrependScheme.Never, removeExtraWhitespaces: false, skipPrependWhenAlreadyPrefixed: true);
        var bare = new MetaspaceEscape('▁', MetaspacePrependScheme.Never, removeExtraWhitespaces: false, skipPrependWhenAlreadyPrefixed: false);

        Assert.Equal(bare.Apply(" the cat"), guarded.Apply(" the cat"));
        Assert.Equal("▁the▁cat", guarded.Apply(" the cat"));
    }
}
