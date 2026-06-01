using Robust.Shared.GameStates;
using Robust.Shared.Physics;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Placed on a mob caught in an energy net.
/// Prevents movement. Non-caster melee attacks are redirected to the net entity.
/// Removed when the net entity is destroyed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EnergyNettedComponent : Component
{
    /// <summary>The ninja who cast this net. Their melee attacks pass through to the target normally.</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Caster;

    /// <summary>The net entity. Non-caster melee damage is redirected here.</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? NetEntity;

    /// <summary>Physics body type before the net was applied; restored on removal.</summary>
    [DataField]
    public BodyType OriginalBodyType = BodyType.Dynamic;
}
