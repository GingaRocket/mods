using System.Linq;
using System.Reflection;
using GtaCollectiblesMap.Core.Model;
using Xunit;

namespace GtaCollectiblesMap.Core.Tests;

/// <summary>
/// Enforces the boundary the whole design rests on.
/// </summary>
/// <remarks>
/// Core staying free of the game SDK is what lets the collectible logic be tested at all —
/// GTA IV cannot run in CI. A single convenient game-SDK reference would quietly end
/// that, so it is asserted rather than trusted to review.
/// </remarks>
public class ArchitectureTests
{
    [Fact]
    public void CoreDoesNotReferenceTheGameSdk()
    {
        AssemblyName[] referenced = typeof(Vec3).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(
            referenced,
            name => name.Name is not null
                && (name.Name.Contains("IVSDKDotNet") || name.Name.Contains("ScriptHookDotNet")));
    }

    [Fact]
    public void CoreDependsOnNothingButTheFramework()
    {
        string[] offenders = typeof(Vec3).Assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name!)
            .Where(name => !name.StartsWith("System") && name != "mscorlib" && name != "netstandard")
            .ToArray();

        Assert.Empty(offenders);
    }
}
