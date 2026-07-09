using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class BossRewardsComponent : Component
{
    [DataField("rewards")]
    public List<EntProtoId> GuaranteedRewards = new();

    [DataField("randomReward")]
    public List<EntProtoId> RandomReward = new();

    [DataField("radius")]
    public float SpawnRadius = 0.5f;

    [DataField("deleteAfter")]
    public bool DeleteAfterRewards = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool RewardsGranted = false;
}
