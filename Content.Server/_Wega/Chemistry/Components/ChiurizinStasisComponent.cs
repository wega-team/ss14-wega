namespace Content.Server._Wega.Chemistry.Components;

/// <summary>
/// Applied to an entity that is currently in Chiurizin stasis.
/// When Timer reaches zero, godmode is disabled and the entity is teleported
/// back to their saved position (if any).
/// </summary>
[RegisterComponent]
public sealed partial class ChiurizinStasisComponent : Component
{
    public float Timer;
    public float HealAccumulator;
    public EntityUid? VisualEntity;

    /// <summary>Whether we added StealthComponent ourselves (so we know to remove it on stasis end).</summary>
    public bool AddedStealth;

    /// <summary>Visibility to restore when stasis ends, if the entity already had StealthComponent.</summary>
    public float OriginalVisibility = 1f;
}
