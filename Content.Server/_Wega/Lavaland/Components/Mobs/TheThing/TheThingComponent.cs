using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(TheThingSystem))]
public sealed partial class TheThingBossComponent : Component
{
    [DataField]
    public EntProtoId? NextStage = default!;
}
