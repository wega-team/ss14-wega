using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(BubblegumSystem))]
public sealed partial class BubblegumBossComponent : Component
{
    [ViewVariables]
    public BubblegumPhase CurrentPhase = BubblegumPhase.Normal;

    [ViewVariables]
    public TimeSpan RageEndTime;

    [ViewVariables]
    public bool IsRaging;

    [DataField]
    public float RageDelayModifier = 0.5f;

    [DataField]
    public float RageDurationMin = 3.5f;

    [DataField]
    public float RageDurationMax = 7f;

    [ViewVariables]
    public TimeSpan NextBloodDiveTime;

    [DataField]
    public float BloodDiveCooldown = 25f;

    [DataField("rewards")]
    public List<EntProtoId> RewardsProto = new();

    [DataField]
    public List<EntProtoId<TargetActionComponent>> Phase1Actions = new();

    [DataField]
    public List<EntProtoId<TargetActionComponent>> Phase2Actions = new();

    [DataField]
    public Dictionary<EntProtoId<TargetActionComponent>, float> Phase1Chances = new();

    [DataField]
    public Dictionary<EntProtoId<TargetActionComponent>, float> Phase2Chances = new();

    [DataField]
    public EntProtoId BloodEffect = "PuddleBlood";

    [DataField]
    public EntProtoId DashMarker = "EffectMegaFaunaMarker";

    [DataField]
    public EntProtoId DashTrail = "EffectBubblegumDashTrail";

    [DataField]
    public EntProtoId HandEffect = "EffectBubblegumHandIn";

    [DataField]
    public SoundSpecifier DashSound = new SoundCollectionSpecifier("FootstepThud");

    /// <summary>
    /// HTN blackboard key for the target entity
    /// </summary>
    public string TargetKey = "Target";
}

[RegisterComponent]
public sealed partial class BubblegumIllusionComponent : Component
{
    [ViewVariables]
    public EntityUid? Master;

    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables]
    public EntityCoordinates? TargetPosition;

    [ViewVariables]
    public int CurrentStep;

    [ViewVariables]
    public int TotalSteps;

    [DataField]
    public DamageSpecifier? Damage;
}
