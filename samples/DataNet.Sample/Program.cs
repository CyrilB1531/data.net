using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using DataNet.Embeddings.Tokenization;
using DataNet.Fuzzy;
using DataNet.Text.Distances;
using DataNet.Text.Stemming;
using DataNet.Text.Vectorization;

// A consumer of the published packages, exercising one thing per lot. It runs as
// part of CI, because a sample that is never built rots into documentation that
// lies.

Console.WriteLine("DataNet sample — consuming the NuGet packages");
Console.WriteLine(new string('-', 46));

// Which assets did NuGet actually hand us? On net10.0 this must report
// .NETCoreApp,Version=v10.0 — proving the package's lib/net10.0 folder resolved,
// rather than the sample silently picking up something else.
Console.WriteLine($"running on      : {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"DataNet.Text    : {FrameworkOf(typeof(Levenshtein))}");
Console.WriteLine($"DataNet.Fuzzy   : {FrameworkOf(typeof(Fuzz))}");
Console.WriteLine($"DataNet.Embeddings: {FrameworkOf(typeof(WordPieceTokenizer))}");
Console.WriteLine();

// Lot 1 — string distances.
Console.WriteLine("distances");
Console.WriteLine($"  Levenshtein(kitten, sitting)   = {Levenshtein.Distance("kitten", "sitting")}");
Console.WriteLine($"  JaroWinkler(martha, marhta)    = {JaroWinkler.Similarity("martha", "marhta"):F4}");
Console.WriteLine();

// Lot 2 — vectorization and stemming.
string[] documents =
[
    "the quick brown fox jumps over the lazy dog",
    "a quick brown dog outpaces a lazy fox",
    "the lazy dog sleeps",
];
var tfidf = new TfidfVectorizer();
CsrMatrix matrix = tfidf.FitTransform(documents);
Console.WriteLine("vectorization");
Console.WriteLine($"  TF-IDF: {matrix.RowCount} docs x {matrix.ColumnCount} terms, {matrix.Values.Length} non-zeros");
Console.WriteLine();

Console.WriteLine("stemming (six Snowball languages)");
Console.WriteLine($"  en  running      -> {EnglishSnowballStemmer.Stem("running")}");
Console.WriteLine($"  fr  continuellement -> {FrenchSnowballStemmer.Stem("continuellement")}");
Console.WriteLine($"  es  hermosos     -> {SpanishSnowballStemmer.Stem("hermosos")}");
Console.WriteLine($"  pt  esperanca    -> {PortugueseSnowballStemmer.Stem("esperança")}");
Console.WriteLine($"  it  rapidamente  -> {ItalianSnowballStemmer.Stem("rapidamente")}");
Console.WriteLine($"  de  freundliche  -> {GermanSnowballStemmer.Stem("freundliche")}");
Console.WriteLine();

// Lot 4 — fuzzy matching.
string[] candidates = ["apple pie", "banana bread", "cherry tart"];
ExtractResult? best = Process.ExtractOne("appel pie", candidates);
Console.WriteLine("fuzzy matching");
Console.WriteLine($"  ExtractOne(appel pie) = {best?.Choice} ({best?.Score:F1})");
Console.WriteLine();

// Lot 3 — the tokenizer only. ONNX inference needs a model, and model weights are
// deliberately not committed, so the sample stops short of it rather than failing.
var vocab = new Dictionary<string, int>
{
    ["[UNK]"] = 0,
    ["token"] = 1,
    ["##ize"] = 2,
    ["text"] = 3,
};
var tokenizer = new WordPieceTokenizer(vocab);
Console.WriteLine("embeddings (tokenizer only — no model weights are shipped)");
Console.WriteLine($"  WordPiece(tokenize text) = [{string.Join(", ", tokenizer.Encode("tokenize text").Tokens)}]");

Console.WriteLine();
Console.WriteLine("OK");
return 0;

static string FrameworkOf(Type probe) =>
    probe.Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "<unknown>";
