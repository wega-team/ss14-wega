using Robust.Shared.GameStates;
using Content.Shared.Whitelist;
using Content.Shared.Mobs;
using Content.Shared.FixedPoint;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageInContainerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTickTime;

    [DataField("interval")]
    public float Interval = 1f;

    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = default!;

    [DataField, AutoNetworkedField]
    public GroupHealSpecifier DamageGroups = default!;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField(required: true)]
    public string SlotId = string.Empty;

    [DataField]
    public List<MobState> AllowedStates = new();

    [DataField]
    public FixedPoint2 DamageCap = 0;
}
