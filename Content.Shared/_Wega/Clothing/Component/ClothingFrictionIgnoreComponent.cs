using Robust.Shared.GameStates;

namespace Content.Shared.Clothing;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ClothingFrictionIgnoreSystem))]
public sealed partial class ClothingFrictionIgnoreComponent : Component
{
    [DataField]
    public bool IgnoreFriction = true;

    [DataField]
    public bool IgnoreAcceleration = true;

    [DataField]
    public float? OverrideFriction = null;

    [DataField]
    public float? OverrideAcceleration = null;
}
