using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.DirtVisuals;

[RegisterComponent]
public sealed partial class WashingMachineComponent : Component
{
    [DataField("washTime")]
    public float WashTime = 20f;

    [ViewVariables]
    public float RemainingTime;

    [ViewVariables]
    public bool IsWashing;

    [DataField("finishSound")]
    public SoundSpecifier FinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");
}

[Serializable, NetSerializable]
public enum WashingMachineVisuals : byte
{
    Washing,
    IsWashing
}