using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Core.Abstractions;

/// <summary>The slice of live game state the mod needs, expressed without game types.</summary>
public interface IGameContext
{
    /// <summary>False during menus and loading, when scanning would read a half-built world.</summary>
    bool IsInGame { get; }

    Vec3 PlayerPosition { get; }

    Episode Episode { get; }
}
