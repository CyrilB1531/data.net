using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Lodestar.Abstractions;
using Lodestar.Conformal;
using Lodestar.Decomposition;
using Lodestar.Embeddings.Tokenization;
using Lodestar.Fuzzy;
using Lodestar.Metrics;
using Lodestar.Sample;
using Lodestar.Text.Distances;

// A consumer of the published packages; runs in CI so it can't rot. Also ADR 0009's
// packaging gate: a new public type needs a call from <ClassName>Sample.cs (ADR 0041).

// Every number below goes through Inv.F3 and friends, which a hole carrying no
// format specifier cannot; this covers those, so the run reads the same everywhere (#205).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

Console.WriteLine("Lodestar sample — consuming the NuGet packages");
Console.WriteLine(new string('-', 46));

// Confirms NuGet resolved the package's net10.0 lib folder, not something else —
// must report .NETCoreApp,Version=v10.0.
Console.WriteLine($"running on        : {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"Lodestar.Text      : {FrameworkOf(typeof(Levenshtein))}");
Console.WriteLine($"Lodestar.Fuzzy     : {FrameworkOf(typeof(Fuzz))}");
Console.WriteLine($"Lodestar.Embeddings: {FrameworkOf(typeof(WordPieceTokenizer))}");
Console.WriteLine($"Lodestar.Metrics   : {FrameworkOf(typeof(ConfusionMatrix))}");
Console.WriteLine($"Lodestar.Conformal : {FrameworkOf(typeof(SplitConformal))}");
Console.WriteLine($"Lodestar.Abstractions: {FrameworkOf(typeof(CsrMatrix))}");
Console.WriteLine($"Lodestar.Decomposition: {FrameworkOf(typeof(TruncatedSvd))}");
Console.WriteLine();

TextSamples.Run();
Lot3Embeddings.Run();
Lot4Fuzzy.Run();
Lot5Metrics.Run();
Lot6Regression.Run();
RakeSample.Run();
RakeOptionsSample.Run();
TextRankSample.Run();
TextRankOptionsSample.Run();
KeywordMatchSample.Run();
SplitConformalSample.Run();
DecompositionSamples.Run();
BkTreeSample.Run();
BkTreeMatchSample.Run();

if (!PackagingGate.Verify())
{
    return 1;
}

Console.WriteLine("OK");
return 0;

static string FrameworkOf(Type probe) =>
    probe.Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "<unknown>";
