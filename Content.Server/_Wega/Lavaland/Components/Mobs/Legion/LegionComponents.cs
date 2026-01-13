using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent]
public sealed partial class LegionBossComponent : Component
{
    [ViewVariables]
    public LegionState CurrentState = LegionState.Summoning;
    [ViewVariables] public TimeSpan NextStateSwitchTime;
    [ViewVariables] public TimeSpan NextSummonTime;
    [ViewVariables] public TimeSpan NextChargeTime;

    [DataField("stateSwitchInterval")]
    public float StateSwitchInterval = 30f;

    [DataField("summonInterval")]
    public float SummonInterval = 6f;

    [DataField("chargeInterval")]
    public float ChargeInterval = 1.5f;

    [DataField("summonCount")]
    public int SummonCount = 2;

    [DataField("minionPrototype")]
    public EntProtoId MinionPrototype = "MobLegionSkull";

    [DataField("splitPrototypes")]
    public List<EntProtoId> SplitPrototypes = new()
    {
        "MobMegaLegionSplitLeft",
        "MobMegaLegionSplitRight",
        "MobMegaLegionSplitEye"
    };

    [DataField("lootPrototypes")]
    public Dictionary<EntProtoId, float> LootPrototypes = new();
}

[RegisterComponent]
public sealed partial class LegionSplitComponent : Component
{
    [DataField("nextSplitPrototype")]
    public string? NextSplitPrototype;
}
