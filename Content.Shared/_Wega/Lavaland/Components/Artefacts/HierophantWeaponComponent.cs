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
    public EntProtoId BeaconPrototype = "EffectHierophantBeacon";

    [DataField]
    public int MaxChasers = 2;

    [DataField]
    public int CrossLength = 4;
}

[RegisterComponent]
public sealed partial class HierophantBeaconComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid ClubEntity;
}
