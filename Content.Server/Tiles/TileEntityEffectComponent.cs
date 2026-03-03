using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Whitelist; // Corvax-Wega-Lavaland
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server.Tiles;

/// <summary>
/// Applies effects upon stepping onto a tile.
/// </summary>
[RegisterComponent, Access(typeof(TileEntityEffectSystem))]
public sealed partial class TileEntityEffectComponent : Component
{
    /// <summary>
    /// List of effects that should be applied.
    /// </summary>
    [DataField]
    public List<EntityEffect> Effects = default!;

    // Corvax-Wega-Lavaland-start
    [DataField]
    public EntityWhitelist Blacklist = new();
    // Corvax-Wega-Lavaland-end
}
