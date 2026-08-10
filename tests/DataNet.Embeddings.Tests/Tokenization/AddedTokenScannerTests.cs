using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Tokenization;

public sealed class AddedTokenScannerTests
{
    private static AddedTokenScanner Scanner(params AddedToken[] tokens) => new(tokens);

    private static (int Start, int End, string Content) Next(AddedTokenScanner scanner, string text, int from = 0)
    {
        bool found = scanner.TryNext(text, from, out int start, out int end, out AddedToken? token);
        Assert.True(found);
        // Assert.True does not narrow nullability for the compiler; the runtime
        // check above is what actually guarantees token is non-null here.
        return (start, end, token!.Content);
    }

    [Fact]
    public void Plain_token_consumes_exactly_its_own_span()
    {
        var scanner = Scanner(new AddedToken("<mask>", 7));
        Assert.Equal((2, 8, "<mask>"), Next(scanner, "a <mask> b"));
    }

    [Fact]
    public void Lstrip_absorbs_every_contiguous_whitespace_character_on_the_left()
    {
        var scanner = Scanner(new AddedToken("<mask>", 7) { Lstrip = true });
        Assert.Equal((1, 9, "<mask>"), Next(scanner, "a  <mask>  b"));
    }

    [Fact]
    public void Lstrip_absorbs_tab_newline_and_no_break_space()
    {
        var scanner = Scanner(new AddedToken("<mask>", 7) { Lstrip = true });
        Assert.Equal(1, Next(scanner, "a\t<mask>").Start);
        Assert.Equal(1, Next(scanner, "a\n<mask>").Start);
        // \u00A0 is a no-break space, written as the escape sequence rather than the
        // literal character so no editor or heredoc can silently flatten it to U+0020.
        Assert.Equal(1, Next(scanner, "a\u00A0<mask>").Start);
    }

    [Fact]
    public void Lstrip_stops_at_a_non_whitespace_character()
    {
        var scanner = Scanner(new AddedToken("<mask>", 7) { Lstrip = true });
        Assert.Equal(2, Next(scanner, "a. <mask>").Start);
    }

    [Fact]
    public void Lstrip_never_reaches_behind_the_scan_start()
    {
        var scanner = Scanner(new AddedToken("<mask>", 7) { Lstrip = true });
        Assert.Equal(2, Next(scanner, "a <mask>", from: 2).Start);
    }

    [Fact]
    public void Rstrip_absorbs_every_contiguous_whitespace_character_on_the_right()
    {
        var scanner = Scanner(new AddedToken("<mask>", 7) { Rstrip = true });
        Assert.Equal((3, 11, "<mask>"), Next(scanner, "a  <mask>  b"));
    }

    [Fact]
    public void Single_word_matches_only_between_non_word_characters()
    {
        var scanner = Scanner(new AddedToken("<m>", 7) { SingleWord = true });
        Assert.Equal(2, Next(scanner, "a <m> b").Start);
        Assert.Equal(1, Next(scanner, ".<m>.").Start);
        Assert.Equal(1, Next(scanner, "-<m>-").Start);
        Assert.Equal(0, Next(scanner, "<m>").Start);
        Assert.False(scanner.TryNext("a<m>a", 0, out _, out _, out _));
        Assert.False(scanner.TryNext("1<m>1", 0, out _, out _, out _));
        Assert.False(scanner.TryNext("_<m>_", 0, out _, out _, out _));
        Assert.False(scanner.TryNext("é<m>é", 0, out _, out _, out _));
        Assert.False(scanner.TryNext("<m>b", 0, out _, out _, out _));
    }

    [Fact]
    public void Single_word_keeps_searching_past_a_rejected_position()
    {
        // The literal "<m>" occurs at indices 1 and 5 in this text and nowhere
        // else; the first is rejected because 'a' precedes it, so the only
        // possible next match starts at 5, regardless of how far the scanner
        // steps forward after a rejection.
        var scanner = Scanner(new AddedToken("<m>", 7) { SingleWord = true });
        Assert.Equal(5, Next(scanner, "a<m> <m> b").Start);
    }

    [Fact]
    public void Leftmost_wins_then_longest()
    {
        var scanner = Scanner(new AddedToken("<a>", 1), new AddedToken("<a><b>", 2));
        Assert.Equal("<a><b>", Next(scanner, "x <a><b> y").Content);
    }

    [Fact]
    public void An_empty_content_is_never_matched()
    {
        var scanner = Scanner(new AddedToken(string.Empty, 9), new AddedToken("<m>", 7));
        Assert.Equal("<m>", Next(scanner, "a <m>").Content);
    }

    [Fact]
    public void No_match_is_reported_when_none_remains()
    {
        var scanner = Scanner(new AddedToken("<m>", 7));
        Assert.False(scanner.TryNext("nothing here", 0, out _, out _, out _));
    }
}
