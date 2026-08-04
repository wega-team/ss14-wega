using Robust.Shared.GameStates;

namespace Content.Shared.Grab.Components;

/// <summary>
/// Added to the victim of a grab. Blocks movement until the victim escapes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GrabbedComponent : Component
{
    /// <summary>Who is grabbing this entity.</summary>
    [DataField]
    public EntityUid Grabber;

    /// <summary>Current grab phase (1–3). Higher phases are harder to escape and deal damage.</summary>
    [DataField]
    public int Phase = 1;

    /// <summary>Server-side: earliest time the victim can attempt another escape.</summary>
    public TimeSpan NextEscapeAttemptTime;

    /// <summary>Server-side: next time phase-3 asphyxiation damage is applied.</summary>
    public TimeSpan NextDamageTime;

    /// <summary>Server-side: whether phase-3 strangulation effects (alert, stutter, disarm penalty) have been applied.</summary>
    public bool Phase3Applied;
}
