using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(AshDrakeSystem))]
public sealed partial class AshDrakeBossComponent : Component
{
    [DataField("rewards")]
    public List<EntProtoId> RewardsProto = new();

    [DataField] public EntProtoId MeteorCircle = "EffectAshDrakeCircle";
    [DataField] public EntProtoId Shadow = "EffectAshDrakeShadow";

    [DataField] public SoundSpecifier AttackSound = new SoundPathSpecifier("/Audio/Magic/fireball.ogg");
}
