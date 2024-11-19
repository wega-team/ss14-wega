using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

[CVarDefs]
public sealed class WegaCVars
{
    /// <summary>
    /// Whether or not respawning is enabled.
    /// </summary>
    public static readonly CVarDef<bool> GhostRespawnEnabled =
        CVarDef.Create("wega.respawn_enabled", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Respawn time, how long the player has to wait in seconds after death.
    /// </summary>
    public static readonly CVarDef<float> GhostRespawnTime =
        CVarDef.Create("wega.respawn_time", 1200.0f, CVar.SERVER | CVar.REPLICATED);
}
