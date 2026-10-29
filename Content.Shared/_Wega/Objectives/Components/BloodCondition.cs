using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(BloodConditionSystem))]
public sealed partial class BloodConditionComponent : Component
{
    public Dictionary<EntityUid, float> BloodTargets = new();
	
    [DataField]
    public int TargetMin;

    [DataField]
    public int TargetMax;
}
