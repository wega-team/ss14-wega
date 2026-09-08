using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Events;

// Broodmother
public sealed partial class BroodmotherTentaclePatchActionEvent : EntityTargetActionEvent
{
    [DataField]
    public int PatchSize = 3;

    [DataField]
    public int CrossLength = 5;

    [DataField]
    public EntProtoId TentaclePrototype = "EffectGoliathTentacleSpawn";
}

public sealed partial class BroodmotherRageActionEvent : EntityTargetActionEvent
{
    [DataField]
    public float RageDuration = 6.5f;

    [DataField]
    public float SpeedMultiplier = 1.5f;

    [DataField]
    public float PostRageSlowDuration = 7f;

    [DataField]
    public float SlowMultiplier = 0.5f;
}

public sealed partial class BroodmotherSpawnChildrenActionEvent : EntityTargetActionEvent
{
    [DataField]
    public int ChildCount = 2;

    [DataField]
    public int MaxChildren = 8;

    [DataField]
    public EntProtoId ChildPrototype = "MobGoliathBaby";
}

// Pandora
public sealed partial class PandoraBlastLineActionEvent : EntityTargetActionEvent
{
    [DataField]
    public int LineLength = 8;
}

public sealed partial class PandoraMagicBoxActionEvent : EntityTargetActionEvent
{
    [DataField]
    public int BoxSize = 6;

    [DataField]
    public int SafeZoneSize = 3;
}

public sealed partial class PandoraTeleportActionEvent : EntityTargetActionEvent
{
}

public sealed partial class PandoraAOEBlastActionEvent : EntityTargetActionEvent
{
    [DataField]
    public int Radius = 3;
}

// Legionnaire
public sealed partial class LegionnaireChargeActionEvent : EntityTargetActionEvent
{
    [DataField]
    public float ChargeDistance = 4f;

    [DataField]
    public float ChargeForce = 15f;
}

public sealed partial class LegionnaireDetachHeadActionEvent : EntityTargetActionEvent
{
    [DataField] public EntProtoId HeadPrototype = "MobLegionnaireHead";
}

public sealed partial class LegionnaireBoneFireActionEvent : EntityTargetActionEvent
{
    [DataField] public EntProtoId BoneFirePrototype = "EffectLegionnaireBoneFire";
}

public sealed partial class LegionnaireSmokeActionEvent : EntityTargetActionEvent
{
    [DataField] public EntProtoId SmokePrototype = "AdminInstantEffectSmoke10";
}

// Herald
public sealed partial class HeraldTriShotActionEvent : EntityTargetActionEvent
{
    [DataField]
    public int ShotCount = 3;

    [DataField]
    public float Spread = 0.3f;

    [DataField]
    public float HealthThreshold = 0.5f;
}

public sealed partial class HeraldSpreadShotActionEvent : EntityTargetActionEvent
{
    [DataField]
    public float HealthThreshold = 0.5f;

    [DataField]
    public float ShotDistance = 8f;
}

public sealed partial class HeraldTeleShotActionEvent : EntityTargetActionEvent
{
    [DataField] public EntProtoId TeleBoltPrototype = "BulletHeraldTele";
}

public sealed partial class HeraldMirrorActionEvent : EntityTargetActionEvent
{
    [DataField] public EntProtoId MirrorPrototype = "HeraldMirror";
    [DataField] public int MaxMirrors = 3;
}
