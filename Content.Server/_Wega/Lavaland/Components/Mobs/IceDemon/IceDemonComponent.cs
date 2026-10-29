using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(IceDemonSystem))]
public sealed partial class IceDemonComponent : Component
{
    [DataField]
    public EntProtoId AfterimagePrototype = "MobIceDemonAfterimage";

    [DataField]
    public int AfterimageCount = 2;

    [DataField]
    public float HealthThreshold = 0.5f;

    public bool AfterimagesSpawned = false;
}
