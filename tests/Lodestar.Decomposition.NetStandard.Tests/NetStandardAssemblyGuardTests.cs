using System.Reflection;
using System.Runtime.Versioning;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// Guards the premise of this project: that the suite is replaying against the
/// netstandard2.0 assembly and not the net10.0 one.
/// </summary>
/// <remarks>
/// Without this, a reference that quietly resolved back to net10.0 would leave
/// every test passing while proving nothing. The assertion is cheap; the false
/// confidence it prevents is not.
/// </remarks>
public sealed class NetStandardAssemblyGuardTests
{
    [Fact]
    public void Suite_runs_against_the_netstandard2_0_build()
    {
        Assembly assembly = typeof(Lodestar.Decomposition.TruncatedSvd).Assembly;
        string? framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        Assert.Equal(".NETStandard,Version=v2.0", framework);
    }

    /// <summary>The same guarantee for Lodestar.Abstractions, reached through a package
    /// rather than a project reference.</summary>
    /// <remarks>
    /// <c>SetTargetFramework</c> does not cross a <c>PackageReference</c>: NuGet resolves
    /// package assets against this project's own framework, net10.0. Left alone the suite
    /// would run the netstandard2.0 Decomposition against the net10.0 Abstractions — half a
    /// mirror, every test green (#529).
    /// </remarks>
    [Fact]
    public void Suite_runs_against_the_netstandard2_0_build_of_Lodestar_Abstractions()
    {
        Assembly assembly = typeof(Lodestar.Abstractions.CsrMatrix).Assembly;
        string? framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        Assert.Equal(".NETStandard,Version=v2.0", framework);
    }
}
