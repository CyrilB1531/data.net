using DataNet.Text.Vectorization;
using Xunit;

namespace DataNet.Text.Tests.Vectorization;

/// <summary>
/// The shipped lists are vendored data, so what needs pinning is their identity:
/// the exact counts of their source, and the words that tell the two candidate
/// sources apart. See <c>docs/decisions/0010-stop-word-list-provenance.md</c>.
/// </summary>
public sealed class StopWordsTests
{
    public static TheoryData<string, int> SourceCounts => new()
    {
        { "English", 318 },     // scikit-learn ENGLISH_STOP_WORDS
        { "French", 154 },      // snowballstem.org/algorithms/french/stop.txt
        { "German", 231 },
        { "Italian", 279 },
        { "Portuguese", 203 },
        { "Spanish", 308 },
    };

    private static IReadOnlyCollection<string> List(string name) => name switch
    {
        "English" => StopWords.English,
        "French" => StopWords.French,
        "German" => StopWords.German,
        "Italian" => StopWords.Italian,
        "Portuguese" => StopWords.Portuguese,
        "Spanish" => StopWords.Spanish,
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    [Theory]
    [MemberData(nameof(SourceCounts))]
    public void List_has_the_word_count_of_its_source(string name, int expected)
    {
        Assert.Equal(expected, List(name).Count);
    }

    [Theory]
    [MemberData(nameof(SourceCounts))]
    public void Entries_are_lowercase_single_words(string name, int expectedCount)
    {
        IReadOnlyCollection<string> list = List(name);

        Assert.Equal(expectedCount, list.Count);
        foreach (string word in list)
        {
            Assert.NotEmpty(word);
            Assert.Equal(word.ToLowerInvariant(), word);
            Assert.DoesNotContain(word, char.IsWhiteSpace);
        }
    }

    // The words below are the measured Snowball/nltk divergence. They fail if the
    // nltk corpus is ever vendored in place of the Snowball lists, which 0010
    // rules out on licensing grounds.
    [Theory]
    [InlineData("ceci")]        // added to Snowball after the snapshot nltk froze
    [InlineData("cela")]
    [InlineData("quel")]
    [InlineData("sans")]
    public void French_has_the_words_Snowball_added(string word)
    {
        Assert.Contains(word, StopWords.French);
    }

    [Theory]
    [InlineData("est")]         // Snowball omits it: homonym of the compass point
    [InlineData("été")]         // homonym of "summer"
    [InlineData("son")]         // homonym of "sound"
    public void French_omits_the_homonyms_Snowball_omits(string word)
    {
        Assert.DoesNotContain(word, StopWords.French);
    }

    [Fact]
    public void German_follows_Snowball_rather_than_the_nltk_correction()
    {
        Assert.Contains("unse", StopWords.German);
        Assert.DoesNotContain("dass", StopWords.German);
    }

    [Fact]
    public void Portuguese_and_Spanish_follow_Snowball()
    {
        Assert.DoesNotContain("ser", StopWords.Portuguese);      // nltk-only
        Assert.Contains("sido", StopWords.Spanish);              // Snowball-only
        Assert.DoesNotContain("sentido", StopWords.Spanish);     // nltk-only
    }

    [Fact]
    public void Stop_words_are_removed_from_the_vocabulary()
    {
        var cv = new CountVectorizer(new CountVectorizerOptions { StopWords = StopWords.German });

        cv.Fit(["der Hund und die Katze", "die Katze schläft"]);

        Assert.Equal(["hund", "katze", "schläft"], cv.GetFeatureNames());
    }

    [Fact]
    public void French_produces_the_vocabulary_it_always_has()
    {
        // Pinned before the lookup was rewritten (#80): the words removed are a
        // property of the list, and no change to how the set is built or read may
        // alter them.
        var cv = new CountVectorizer(new CountVectorizerOptions { StopWords = StopWords.French });

        cv.Fit([
            "Le chat de la maison dort sur le canapé",
            "Les chiens et les chats sont dans le jardin",
            "Il a mangé une pomme que nous avions achetée hier",
            "Nous serons à la maison quand vous aurez fini",
        ]);

        Assert.Equal(
            [
                "achetée", "avions", "canapé", "chat", "chats", "chiens", "dort",
                "fini", "hier", "jardin", "maison", "mangé", "pomme", "quand",
            ],
            cv.GetFeatureNames());
    }

    [Fact]
    public void A_word_a_stop_word_merely_starts_is_kept()
    {
        // The net10 path tests a span of the document rather than a copy of the
        // token, so a wrong length would silently match a prefix: "the" against
        // "theatre", "for" against "forest".
        var cv = new CountVectorizer(new CountVectorizerOptions { StopWords = StopWords.English });

        cv.Fit(["the theatre", "for forest", "no nonetheless"]);

        Assert.Equal(["forest", "nonetheless", "theatre"], cv.GetFeatureNames());
    }

    [Fact]
    public void A_caller_list_mutated_afterwards_does_not_reach_the_vectorizer()
    {
        // The vectorizer copies a caller's set for exactly this reason: it is
        // theirs to keep mutating, and a fitted model that quietly followed along
        // would remove words its options never declared.
        var mine = new HashSet<string>(StringComparer.Ordinal) { "the" };
        var cv = new CountVectorizer(new CountVectorizerOptions { StopWords = mine });

        mine.Add("quick");
        cv.Fit(["the quick brown fox"]);

        Assert.Equal(["brown", "fox", "quick"], cv.GetFeatureNames());
    }

    [Fact]
    public void Accented_stop_words_survive_accent_stripping()
    {
        // Matching is ordinal against the analyzer's output, so "même" no longer
        // matches once preprocessing has turned it into "meme". Documented on the
        // StopWords class; scikit-learn behaves the same way.
        var cv = new CountVectorizer(new CountVectorizerOptions
        {
            StopWords = StopWords.French,
            StripAccents = true,
        });

        cv.Fit(["le même chien"]);

        Assert.Equal(["chien", "meme"], cv.GetFeatureNames());
    }
}
