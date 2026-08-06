using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using DataNet.Embeddings.Tokenization;
using DataNet.Fuzzy;
using DataNet.Sample;
using DataNet.Text.Distances;

// A consumer of the published packages, exercising one thing per lot. It runs as
// part of CI, because a sample that is never built rots into documentation that
// lies.
//
// It is also the packaging gate described in ADR 0009, and that is the stricter
// job: PackagingGate below fails this program when any public type of the three
// packages has stopped being reachable from outside its assembly. Adding a public
// type therefore means adding a call here — see the Lot*.cs files.

Console.WriteLine("DataNet sample — consuming the NuGet packages");
Console.WriteLine(new string('-', 46));

// Which assets did NuGet actually hand us? On net10.0 this must report
// .NETCoreApp,Version=v10.0 — proving the package's lib/net10.0 folder resolved,
// rather than the sample silently picking up something else.
Console.WriteLine($"running on        : {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"DataNet.Text      : {FrameworkOf(typeof(Levenshtein))}");
Console.WriteLine($"DataNet.Fuzzy     : {FrameworkOf(typeof(Fuzz))}");
Console.WriteLine($"DataNet.Embeddings: {FrameworkOf(typeof(WordPieceTokenizer))}");
Console.WriteLine();

Lot1Distances.Run();
Lot2Vectorization.Run();
Lot3Embeddings.Run();
Lot4Fuzzy.Run();

if (!PackagingGate.Verify())
{
    return 1;
}

Console.WriteLine("OK");
return 0;

static string FrameworkOf(Type probe) =>
    probe.Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "<unknown>";
