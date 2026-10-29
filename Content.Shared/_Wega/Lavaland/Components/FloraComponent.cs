using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FloraComponent : Component
{
    [DataField]
    public float GrowthTime = 1500f;

    [DataField]
    public float MinGrowthTime = 1200f;

    [DataField]
    public float MaxGrowthTime = 1800f;

    [DataField]
    public float NextGrowthTick { get; set; }

    [DataField]
    public bool IsGrown = true;

    [DataField(required: true)]
    public string HarvestPrototype { get; set; } = string.Empty;

    [DataField]
    public int MinYield = 1;

    [DataField]
    public int MaxYield = 3;

    [DataField("specialTool")]
    public ProtoId<ToolQualityPrototype>? SpecialTool = null;

    [DataField]
    public float HarvestDuration = 2f;

    public SoundSpecifier? HarvestSound = null;
}

[Serializable, NetSerializable]
public enum FloraVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum FloraState : byte
{
    Grown,
    Harvested
}

[Serializable, NetSerializable]
public sealed partial class FloraHarvestDoAfterEvent : SimpleDoAfterEvent
{
}
