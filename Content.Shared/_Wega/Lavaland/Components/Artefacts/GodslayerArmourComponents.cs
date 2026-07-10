using Content.Shared.Damage;

namespace Content.Shared.Lavaland.Artefacts.Components;

[RegisterComponent]
public sealed partial class GodslayerArmourComponent : Component
{
    [DataField(required: true)]
    public GroupHealSpecifier HealAmount;

    [DataField]
    public TimeSpan CooldownTime = TimeSpan.FromMinutes(10f);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextUseTime;
}

[RegisterComponent]
public sealed partial class GodslayerArmourAffectedComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid ArmourEntity;
}
