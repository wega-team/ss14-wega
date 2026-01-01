using Content.Shared.Damage;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent]
public sealed partial class HierophantBossComponent : Component
{
    [ViewVariables]
    public HierophantAttackType LastAttack = HierophantAttackType.None;

    [DataField("chaserPrototype")]
    public EntProtoId ChaserPrototype = "HierophantChaser";

    public EntityCoordinates HomePosition;

    public bool NeedComeBack;

    [ViewVariables]
    public TimeSpan NextReturnCheckTime;

    [ViewVariables]
    public float ReturnCheckInterval = 5f;

    [ViewVariables]
    public TimeSpan NextPassiveMoveTime;

    [ViewVariables]
    public float PassiveMoveInterval = 3f;

    [DataField("reward")]
    public EntProtoId RewardProto = "HierophantClubRod";
}

[RegisterComponent]
public sealed partial class HierophantChaserComponent : Component
{
    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables]
    public TimeSpan NextMoveTime;

    [ViewVariables]
    public float MoveInterval = 0.3f;

    [DataField("maxChaseSteps")]
    public int MaxChaseSteps = 18;

    [ViewVariables]
    public int CurrentSteps = 0;

    [DataField("damageOnSpawn")]
    public DamageSpecifier Damage = new();
}
