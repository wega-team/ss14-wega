using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition that is satisfied by physically holding a number of credits (a "Credit"
/// stack) on you. Progress is recomputed live, so spending or returning the money lowers it again.
/// </summary>
[RegisterComponent, Access(typeof(HoldCreditsConditionSystem))]
public sealed partial class HoldCreditsConditionComponent : Component
{
    /// <summary>Minimum required credits (the actual target is rolled between Min and Max).</summary>
    [DataField]
    public int Min = 6000;

    /// <summary>Maximum required credits.</summary>
    [DataField]
    public int Max = 10000;

    /// <summary>The rolled amount of credits required, set on assignment.</summary>
    [DataField]
    public int Target;
}
