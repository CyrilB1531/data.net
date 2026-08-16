using System.Reflection;
using System.Runtime.Versioning;
using Xunit;

namespace Lodestar.Fuzzy.Tests;

/// <summary>
/// Guards the premise of this project: that the suite is replaying against the
/// netstandard2.0 assembly and not the net10.0 one.
/// </summary>
/// <remarks>
/// Without this, a reference that quietly resolved back to net10.0 would leave
/// every test passing while proving nothing — the exact failure mode the
/// comparison benchmark hit before its isolation was fixed. The assertion is
/// cheap; the false confidence it prevents is not.
/// </remarks>
public sealed class NetStandardAssemblyGuardTests
{
    [Fact]
    public void Suite_runs_against_the_netstandard2_0_build()
    {
        Assembly assembly = typeof(Lodestar.Fuzzy.Fuzz).Assembly;
        string? framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        Assert.Equal(".NETStandard,Version=v2.0", framework);
    }

    /// <summary>The same guarantee for Lodestar.Text, reached through a package rather than a project reference.</summary>
    /// <remarks>
    /// <c>SetTargetFramework</c> does not cross a <c>PackageReference</c>: NuGet picks assets for
    /// the consuming project's framework, net10.0 here. Left alone this suite would run the
    /// netstandard2.0 Lodestar.Fuzzy against the net10.0 Text — half a mirror, every test green.
    /// The csproj pins it; this asserts the pin holds, and returns with the floor (#194).
    /// </remarks>
    [Fact]
    public void Suite_runs_against_the_netstandard2_0_build_of_Lodestar_Text()
    {
        Assembly assembly = typeof(Lodestar.Text.Distances.Indel).Assembly;
        string? framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        Assert.Equal(".NETStandard,Version=v2.0", framework);
    }
}
