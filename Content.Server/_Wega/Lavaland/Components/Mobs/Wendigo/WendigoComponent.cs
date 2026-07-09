using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(WendigoSystem))]
public sealed partial class WendigoBossComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsEnraged = false;

    [DataField]
    public float EnrageDelayModifier = 0.6f;

    [DataField]
    public float EnrageSpeedMultiplier = 1.5f;

    [DataField]
    public float EnrageThreshold = 0.5f;

    [DataField]
    public EntProtoId SmokePrototype = "EffectWendigoSmoke";

    [DataField]
    public EntProtoId DeathBoltPrototype = "ProjectileWendigoDeathBolt";
}
