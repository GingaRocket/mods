using System.Reflection;
using Xunit;

namespace GtaCollectiblesMap.Features.Tests;

public class ArchitectureTests
{
    /// <summary>
    /// Features orchestrates through Core's abstractions. Reaching for the SDK directly would
    /// make tracker behaviour untestable without the game running.
    /// </summary>
    [Fact]
    public void FeaturesDoesNotReferenceTheGameSdk()
    {
        AssemblyName[] referenced = typeof(CollectibleTracker).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(
            referenced,
            name => name.Name is not null
                && (name.Name.Contains("IVSDKDotNet") || name.Name.Contains("ScriptHookDotNet")));
    }
}
