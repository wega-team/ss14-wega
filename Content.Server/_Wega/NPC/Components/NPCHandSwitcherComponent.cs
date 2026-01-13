using Content.Server.NPC.Systems;

namespace Content.Server.NPC.Components;

[RegisterComponent, Access(typeof(NPCHandSwitcherSystem))]
public sealed partial class NPCHandSwitcherComponent : Component
{
    /// <summary>
    /// HTN blackboard key for the target entity
    /// </summary>
    [DataField]
    public string TargetKey = "Target";

    [DataField]
    public TimeSpan NextSwitch = TimeSpan.Zero;

    [DataField]
    public TimeSpan SwitchInterval = TimeSpan.FromSeconds(10);
}
