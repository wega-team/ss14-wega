using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(Systems.KidnapConditionSystem))]
public sealed partial class KidnapConditionComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<JobPrototype>> AllowedJobs = new();

    [DataField]
    public int JobCount = 3;

    [DataField]
    public int MinScans = 3;

    [DataField]
    public int MaxScans = 6;

    public List<ProtoId<JobPrototype>> TargetJobs = new();
    public EntityUid? Target;
    public HashSet<EntityUid> ScannedMinds = new();
    public int RequiredScans;
    public bool TargetScanned;
}
