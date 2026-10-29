using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(AshDrakeSystem))]
public sealed partial class AshDrakeBossComponent : Component
{
    [DataField] public EntProtoId MeteorCircle = "EffectAshDrakeCircle";
    [DataField] public EntProtoId Shadow = "EffectAshDrakeShadow";

    [DataField] public SoundSpecifier AttackSound = new SoundPathSpecifier("/Audio/Magic/fireball.ogg");

    /// <summary>
    /// HTN blackboard key for the target entity
    /// </summary>
    public string TargetKey = "Target";
}
