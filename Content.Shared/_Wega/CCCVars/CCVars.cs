using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

[CVarDefs]
public sealed class WegaCVars
{
    /*
        Ghost Respawn CVars
    */
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

    /*
        Barks CVars
    */
    /// <summary>
    /// Responsible for turning on and off the bark system.
    /// </summary>
    public static readonly CVarDef<bool> BarksEnabled =
        CVarDef.Create("wega.barks_enabled", false, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Default volume setting of Barks sound.
    /// </summary>
    public static readonly CVarDef<float> BarksVolume =
        CVarDef.Create("wega.barks_volume", 0f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
        Night Light System CVars
    */
    /// <summary>
    /// Responsible for switching the night light system.
    /// </summary>
    public static readonly CVarDef<bool> NightLightEnabled =
        CVarDef.Create("wega.night_light_enabled", false, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Switching adjusts all the lamps to the holiday mode according to the logic of updating the night lighting.
    /// </summary>
    public static readonly CVarDef<bool> PartyEnabled =
        CVarDef.Create("wega.party_enabled", false, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /*
        Vote CVars
    */
    /// <summary>
    /// If enabled forcibly, it will trigger a vote for the mode at the end of the round.
    /// </summary>
    public static readonly CVarDef<bool> VoteRoundEndEnabled =
        CVarDef.Create("wega.roundend_vote_enabled", false, CVar.SERVERONLY);

    /// <summary>
    ///     Sets the maximum length for flavor text (character descriptions).
    /// </summary>
    public static readonly CVarDef<int> OOCMaxFlavorTextLength =
        CVarDef.Create("ic.oocflavor_text_length", 512, CVar.SERVER | CVar.REPLICATED);
}
