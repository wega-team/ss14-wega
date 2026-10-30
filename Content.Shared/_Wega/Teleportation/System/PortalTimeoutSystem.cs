namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// Raised on an entity when it teleports through any linked portal, regardless of
/// whether PortalTimeoutComponent already existed on it.
/// </summary>
public sealed class EntityTeleportedFromPortalEvent : EntityEventArgs
{
    public readonly EntityUid EnteredPortal;

    public EntityTeleportedFromPortalEvent(EntityUid enteredPortal)
    {
        EnteredPortal = enteredPortal;
    }
}
