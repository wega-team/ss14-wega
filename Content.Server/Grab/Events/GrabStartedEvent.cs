namespace Content.Server.Grab.Events;

/// <summary>
/// Raised directed on the grabber entity when a grab is initiated.
/// </summary>
public sealed class GrabStartedEvent : EntityEventArgs
{
    public EntityUid Grabber;
    public EntityUid Victim;
}
