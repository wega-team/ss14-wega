using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition that requires both blood brothers to hijack the shuttle together
/// </summary>
[RegisterComponent, Access(typeof(BloodBrotherSharedHijackConditionSystem))]
public sealed partial class BloodBrotherSharedHijackConditionComponent : Component
{
}
