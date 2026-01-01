using Content.Server.Objectives.Systems;
using Content.Shared.Objectives;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// A component for the common purpose of stealing blood brothers
/// </summary>
[RegisterComponent, Access(typeof(BloodBrotherSharedStealConditionSystem))]
public sealed partial class BloodBrotherSharedStealConditionComponent : Component
{
    /// <summary>
    /// A group of items to be stolen
    /// </summary>
    [DataField(required: true)]
    public ProtoId<StealTargetGroupPrototype> StealGroup;

    /// <summary>
    /// When enabled, disables generation of this target if there is no entity on the map
    /// </summary>
    [DataField]
    public bool VerifyMapExistence = true;

    /// <summary>
    /// If true, counts objects that are close to steal areas.
    /// </summary>
    [DataField]
    public bool CheckStealAreas = false;

    /// <summary>
    /// If the target may be alive but has died, it will not be counted
    /// </summary>
    [DataField]
    public bool CheckAlive = false;

    /// <summary>
    /// The minimum number of items you need to steal to fulfill a objective
    /// </summary>
    [DataField]
    public int MinCollectionSize = 1;

    /// <summary>
    /// The maximum number of items you need to steal to fulfill a objective
    /// </summary>
    [DataField]
    public int MaxCollectionSize = 1;

    /// <summary>
    /// Target collection size after calculation
    /// </summary>
    [DataField]
    public int CollectionSize;

    /// <summary>
    /// Help newer players by saying e.g. "steal the chief engineer's advanced magboots"
    /// </summary>
    [DataField("owner")]
    public string? OwnerText;

    [DataField(required: true)]
    public LocId ObjectiveText;

    [DataField(required: true)]
    public LocId ObjectiveNoOwnerText;

    [DataField(required: true)]
    public LocId DescriptionText;

    [DataField(required: true)]
    public LocId DescriptionMultiplyText;
}
