namespace Content.Server.Ninja.Components;

/// <summary>
/// Marks an entity as a ninja caltrop trap and stores the caster UID
/// so the deploying ninja is immune to their own caltrops.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaCaltropComponent : Component
{
    [DataField]
    public EntityUid? Caster;
}
