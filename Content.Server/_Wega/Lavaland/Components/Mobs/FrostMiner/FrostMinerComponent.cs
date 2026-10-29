using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(FrostMinerSystem))]
public sealed partial class FrostMinerBossComponent : Component
{
    [DataField]
    public EntProtoId DemonPrototype = "MobFrostMinerDemonic";

    [DataField]
    public bool IsTransitioning = false;
}

[RegisterComponent, Access(typeof(FrostMinerSystem))]
public sealed partial class FrostMinerIceOrbComponent : Component
{
    [DataField]
    public EntityUid Shooter;

    [DataField]
    public float ExplodeDelay = 5f;

    [DataField]
    public int ShardsPerOrb = 5;

    [DataField]
    public EntProtoId ShardPrototype = "ProjectileFrostMinerShard";

    [ViewVariables]
    public TimeSpan SpawnTime;
}
