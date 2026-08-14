using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using DataNet.Embeddings.Tokenization;
using DataNet.Fuzzy;
using DataNet.Metrics;
using DataNet.Sample;
using DataNet.Text.Distances;

// A consumer of the published packages, one thing per lot; runs in CI so it can't
// rot. Also ADR 0009's packaging gate: a new public type needs a call in Lot*.cs.

Console.WriteLine("DataNet sample — consuming the NuGet packages");
Console.WriteLine(new string('-', 46));

// Confirms NuGet resolved the package's net10.0 lib folder, not something else —
// must report .NETCoreApp,Version=v10.0.
Console.WriteLine($"running on        : {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"DataNet.Text      : {FrameworkOf(typeof(Levenshtein))}");
Console.WriteLine($"DataNet.Fuzzy     : {FrameworkOf(typeof(Fuzz))}");
Console.WriteLine($"DataNet.Embeddings: {FrameworkOf(typeof(WordPieceTokenizer))}");
Console.WriteLine($"DataNet.Metrics   : {FrameworkOf(typeof(ConfusionMatrix))}");
Console.WriteLine();

Lot1Distances.Run();
Lot2Vectorization.Run();
Lot3Embeddings.Run();
Lot4Fuzzy.Run();
Lot5Metrics.Run();
Lot6Regression.Run();

if (!PackagingGate.Verify())
{
    return 1;
}

Console.WriteLine("OK");
return 0;

static string FrameworkOf(Type probe) =>
    probe.Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "<unknown>";
