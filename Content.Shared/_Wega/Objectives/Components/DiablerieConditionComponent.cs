using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(DiablerieConditionSystem))]
public sealed partial class DiablerieConditionComponent : Component
{
    public Dictionary<EntityUid, float> BloodTargets = new();
}
