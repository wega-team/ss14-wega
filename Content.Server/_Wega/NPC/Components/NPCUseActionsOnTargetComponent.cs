using Content.Server.NPC.Systems;
using Content.Shared.Actions.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Components;

/// <summary>
/// This is used for an NPC that constantly tries to use an actions on a given target.
/// </summary>
[RegisterComponent, Access(typeof(NPCUseActionsOnTargetSystem))]
public sealed partial class NPCUseActionsOnTargetComponent : Component
{
    /// <summary>
    /// HTN blackboard key for the target entity
    /// </summary>
    [DataField]
    public string TargetKey = "Target";

    /// <summary>
    /// Actions that's going to attempt to be used.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId<TargetActionComponent>> ActionIds;

    [DataField]
    public Dictionary<EntProtoId<TargetActionComponent>, EntityUid?> ActionEnts = new();

    [DataField]
    public Dictionary<EntProtoId<TargetActionComponent>, float> ActionChances = new();

    [DataField] public float DefaultChance = 1f;

    /// <summary>
    /// Determines when the NPC can use the skill next time using UseDelay.
    /// The smaller the faster and vice versa.
    /// </summary>
    [DataField] public float DelayModifier = 1f;
    [DataField] public TimeSpan NextUseTime = TimeSpan.Zero;
}
