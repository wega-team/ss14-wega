using Content.Shared.FixedPoint;

namespace Content.Shared.Vampire.Components;

[RegisterComponent]
public sealed partial class VampireBloodAbsorptionComponent : Component
{
    [DataField]
    public FixedPoint2 BloodStealAmount = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid VampireOwner = default!;
}
