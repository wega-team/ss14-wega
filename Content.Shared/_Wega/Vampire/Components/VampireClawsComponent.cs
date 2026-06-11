using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared.Vampire.Components;

[RegisterComponent]
public sealed partial class VampireClawsComponent : Component
{
    [DataField]
    public FixedPoint2 BloodStealAmount = 5;

    [DataField]
    public GroupHealSpecifier HealGroups = default!;

    [DataField]
    public float StaminaMod = -10f;
}
