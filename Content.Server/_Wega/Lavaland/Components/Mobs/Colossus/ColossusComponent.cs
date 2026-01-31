using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(ColossusSystem))]
public sealed partial class ColossusBossComponent : Component
{
    [DataField("rewards")]
    public List<EntProtoId> RewardsProto = new();

    [DataField] public SoundSpecifier AttackSound = new SoundPathSpecifier("/Audio/_Wega/Effects/narsie_attack.ogg");
}
