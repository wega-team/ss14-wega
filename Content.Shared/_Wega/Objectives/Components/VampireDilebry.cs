using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(VampireDilebrySystem))]
public sealed partial class VampireDilebryComponent : Component
{
    public Dictionary<EntityUid, float> BloodTargets = new();
}
