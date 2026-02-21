using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Events;

/// <summary>
/// An event triggered when megafauna is killed. For Reward and Specific logic
/// </summary>
/// <param name="Megafauna">ID of the megafauna entity</param>
/// <param name="Killer">The killer's ID, may be null</param>
[ByRefEvent]
public record struct MegafaunaKilledEvent(EntityUid Megafauna, EntityUid? Killer);

/// <summary>
/// An event triggered when a megafauna attacks a target.
/// </summary>
/// <param name="Target">ID of the attack target</param>
[ByRefEvent]
public record struct MegafaunaAttackEvent(EntityUid Target);

// BDM
public sealed partial class BloodDrunkMinerDashAction : WorldTargetActionEvent
{
    public SoundSpecifier DashSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg");
}

// Hierophant
public sealed partial class HierophantBlinkActionEvent : EntityTargetActionEvent
{
    [DataField] public EntProtoId BlinkEffect = "EffectHierophantBlink";
}

public sealed partial class HierophantCrossActionEvent : EntityTargetActionEvent
{
    [DataField] public int CrossLength = 8;
}

public sealed partial class HierophantChaserActionEvent : EntityTargetActionEvent
{
    [DataField] public int ChaserCount = 1;
    [DataField] public float ChaserDelay = 0.3f;
}

public sealed partial class HierophantDamageAreaActionEvent : EntityTargetActionEvent
{
    [DataField] public int MaxRadius = 6;
    [DataField] public float WaveDelay = 0.6f;
}

// Mega Legion
public sealed partial class MegaLegionAction : EntityTargetActionEvent
{
}

// Colossus
public sealed partial class ColossusFractionActionEvent : EntityTargetActionEvent
{
    [DataField] public float FractionSpread = 0.3f;
    [DataField] public int FractionCount = 5;
}

public sealed partial class ColossusCrossActionEvent : EntityTargetActionEvent
{
    [DataField] public float CrossLength = 10f;
    [DataField] public float CrossDelay = 1f;
}

public sealed partial class ColossusSpiralActionEvent : EntityTargetActionEvent
{
    [DataField] public int JudgementProjectileCount = 16;
    [DataField] public float JudgementProjectileDelay = 0.08f;
    [DataField] public float DieHealthModifier = 0.33f;
    [DataField] public int DieProjectileCount = 20;
    [DataField] public float DieProjectileDelay = 0.06f;
}

public sealed partial class ColossusTripleFractionActionEvent : EntityTargetActionEvent
{
    [DataField] public float FractionSpread = 0.3f;
    [DataField] public int FractionCount = 5;
    [DataField] public float TripleFractionDelay = 0.5f;
}

// Ash Drake
public sealed partial class AshDrakeConeFireActionEvent : EntityTargetActionEvent
{
}

public sealed partial class AshDrakeBreathingFireActionEvent : EntityTargetActionEvent
{
    [DataField] public float HealthModifier = 0.5f;
}

public sealed partial class AshDrakeLavaActionEvent : EntityTargetActionEvent
{
    [DataField] public float HealthModifier = 0.5f;
    [DataField] public EntProtoId Lava = "EffectAshDrakeFloorLavaTemp";
    [DataField] public EntProtoId LavaLess = "EffectAshDrakeFloorLavaLessTemp";
    [DataField] public EntProtoId Wall = "EffectAshDrakeFireWall";
    [DataField] public EntProtoId Marker = "EffectMegaFaunaMarker";
    [DataField] public EntProtoId SafeMarker = "EffectAshDrakeSafeMarker";
    [DataField(required: true)] public DamageSpecifier LandingDamage;
    [DataField(required: true)] public DamageSpecifier HealingSpec;
}

// Bubblegum
public sealed partial class BubblegumRageActionEvent : EntityTargetActionEvent
{
}

public sealed partial class BubblegumBloodDiveActionEvent : EntityTargetActionEvent
{
    [DataField] public float DiveRange = 5f;
    [DataField] public float PreDiveDelay = 0.8f;
}

public sealed partial class BubblegumTripleDashActionEvent : EntityTargetActionEvent
{
    [DataField] public List<float> DashDelays = new() { 0.9f, 0.6f, 0.3f };
    [DataField] public float DashDistance = 6f;
    [DataField] public float MoveSpeed = 0.1f;
    [DataField(required: true)] public DamageSpecifier DashDamage;
    [DataField] public bool UseSineWaveForLast = true;
}

public sealed partial class BubblegumIllusionDashActionEvent : EntityTargetActionEvent
{
    [DataField] public int IllusionCount = 3;
    [DataField] public float PlacementRadius = 4f;
    [DataField] public float PreDashDelay = 1f;
    [DataField(required: true)] public DamageSpecifier IllusionDamage;
    [DataField] public EntProtoId IllusionPrototype = "MobBubblegumIllusion";
}

public sealed partial class BubblegumPentagramDashActionEvent : EntityTargetActionEvent
{
    [DataField] public float PlacementRadius = 5f;
    [DataField] public float PreDashDelay = 1.2f;
    [DataField(required: true)] public DamageSpecifier IllusionDamage;
    [DataField] public EntProtoId IllusionPrototype = "MobBubblegumIllusion";
}

public sealed partial class BubblegumChaoticIllusionDashActionEvent : EntityTargetActionEvent
{
    [DataField] public int IllusionCount = 2;
    [DataField] public float PlacementRadius = 6f;
    [DataField] public float PreDashDelay = 1f;
    [DataField(required: true)] public DamageSpecifier IllusionDamage;
    [DataField] public EntProtoId IllusionPrototype = "MobBubblegumIllusion";
}
