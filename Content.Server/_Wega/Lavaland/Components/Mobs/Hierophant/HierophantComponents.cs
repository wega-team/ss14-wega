using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(HierophantSystem))]
public sealed partial class HierophantBossComponent : Component
{
    [DataField("chaserPrototype")]
    public EntProtoId ChaserPrototype = "MobHierophantChaser";

    [ViewVariables]
    public TimeSpan NextPassiveMoveTime;

    [ViewVariables]
    public float PassiveMoveInterval = 3f;

    [ViewVariables]
    public TimeSpan NextReturnCheckTime;

    [ViewVariables]
    public float ReturnCheckInterval = 5f;

    public EntityCoordinates HomePosition;
    public bool NeedComeBack;

    [DataField("rewards")]
    public List<EntProtoId> RewardsProto = new();

    [DataField] public SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg");
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
