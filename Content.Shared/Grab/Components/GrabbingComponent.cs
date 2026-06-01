using Robust.Shared.GameStates;

namespace Content.Shared.Grab.Components;

/// <summary>
/// Added to the entity that is performing a grab.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GrabbingComponent : Component
{
    /// <summary>The entity currently being grabbed.</summary>
    [DataField]
    public EntityUid Victim;

    /// <summary>Server-side: earliest time the grabber can escalate to the next phase.</summary>
    public TimeSpan NextPhaseChangeTime;

    /// <summary>Server-side: set by OnGrabberPullStopped to restart the pull on the next Update tick.</summary>
    public bool NeedsPullRestart;

    /// <summary>Server-side: set when the grabber intentionally releases at max phase so OnGrabberPullStopped releases instead of restarting.</summary>
    public bool PendingUserRelease;

    /// <summary>Server-side: set at the very start of ReleaseGrab to prevent re-entrant releases from event chains.</summary>
    public bool IsReleasing;
}
