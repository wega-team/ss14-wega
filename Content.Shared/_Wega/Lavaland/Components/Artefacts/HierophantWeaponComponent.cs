using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Artefacts.Components;

[RegisterComponent]
public sealed partial class HierophantClubComponent : Component
{
    [DataField]
    public EntProtoId ChaserPrototype = "MobHierophantChaser";

    [DataField]
    public EntProtoId DamageTilePrototype = "EffectHierophantSquare";

    [DataField]
    public int MaxChasers = 2;

    [DataField]
    public int CrossLength = 4;
}
