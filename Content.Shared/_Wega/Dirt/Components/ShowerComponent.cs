using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.DirtVisuals;

[RegisterComponent]
public sealed partial class ShowerComponent : Component
{
    [DataField("sprayTime")]
    public float SprayTime = 2f;

    [ViewVariables]
    public float RemainingTime;

    [ViewVariables]
    public bool IsSpraying;

    [DataField("sprayStartSound")]
    public SoundSpecifier SprayStartSound = new SoundPathSpecifier("/Audio/_Wega/Effects/Objects/shower_start.ogg");

    [DataField("sprayEndSound")]
    public SoundSpecifier SprayEndSound = new SoundPathSpecifier("/Audio/_Wega/Effects/Objects/shower_end.ogg");

    [DataField("waterReagent")]
    public string WaterReagent = "Water";

    [DataField("waterAmount")]
    public float WaterAmount = 20f;
}

[Serializable, NetSerializable]
public enum ShowerVisuals : byte
{
    Spraying
}