using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Events;

public sealed partial class LegionSummonSkullAction : EntityTargetActionEvent
{
    /// <summary>
    /// The entity that must be invoked.
    /// </summary>
    [DataField]
    public EntProtoId EntityId = "MobLegionSkull";

    /// <summary>
    /// How many entities should be invoked.
    /// </summary>
    [DataField]
    public int MaxSpawns = 1;
}
