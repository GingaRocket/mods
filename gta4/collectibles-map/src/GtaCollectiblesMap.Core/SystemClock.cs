using GtaCollectiblesMap.Core.Abstractions;

namespace GtaCollectiblesMap.Core;

/// <summary>The real clock.</summary>
public sealed class SystemClock : IClock
{
    public int TickCount => System.Environment.TickCount;
}
