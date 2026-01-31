namespace Content.Shared.Lavaland.Artefacts.Components;

using Robust.Shared.Prototypes;

[RegisterComponent]
public sealed partial class HierophantClubComponent : Component
{
    [DataField("chaserPrototype")]
    public EntProtoId ChaserPrototype = "HierophantChaser";

    [DataField("damageTilePrototype")]
    public EntProtoId DamageTilePrototype = "EffectHierophantSquare";

    [DataField("maxChasers")]
    public int MaxChasers = 2;

    [DataField("crossLength")]
    public int CrossLength = 4;
}
