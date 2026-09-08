using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(LegionSystem))]
public sealed partial class LegionnaireComponent : Component
{
    [ViewVariables]
    public EntityUid? HeadEntity;

    [ViewVariables]
    public EntityUid? BoneCampfire;
}

[RegisterComponent, Access(typeof(LegionSystem))]
public sealed partial class LegionnaireHeadComponent : Component
{
    [ViewVariables]
    public EntityUid OwnerLegionnaire;
}

[RegisterComponent, Access(typeof(LegionSystem))]
public sealed partial class LegionBossComponent : Component
{
    [ViewVariables]
    public LegionState CurrentState = LegionState.Summoning;
    [ViewVariables] public TimeSpan NextStateSwitchTime;
    [ViewVariables] public TimeSpan NextSummonTime;
    [ViewVariables] public TimeSpan NextChargeTime;

    [DataField]
    public float StateSwitchInterval = 30f;

    [DataField]
    public float SummonInterval = 6f;

    [DataField]
    public float ChargeInterval = 1.5f;

    [DataField]
    public int SummonCount = 2;

    [DataField]
    public EntProtoId MinionPrototype = "MobLegionSkull";

    [DataField]
    public List<EntProtoId> SplitPrototypes = new()
    {
        "MobMegaLegionSplitLeft",
        "MobMegaLegionSplitRight",
        "MobMegaLegionSplitEye"
    };

    [DataField]
    public Dictionary<EntProtoId, float> LootPrototypes = new();

    [DataField("rewards")] // Specific
    public List<EntProtoId> RewardsProto = new();
}

[RegisterComponent, Access(typeof(LegionSystem))]
public sealed partial class LegionSplitComponent : Component
{
    [DataField("nextSplit")]
    public string? NextSplitPrototype;
}
