using Robust.Shared.Containers;

namespace Content.Server.Ninja.Components;

/// <summary>
/// Static NinjaCloningCapsule structure on the ninja planet.
/// Holds the clone body during the 30-second respawn wait.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaCloningCapsuleComponent : Component
{
    [ViewVariables]
    public ContainerSlot BodyContainer = default!;

    /// <summary>The ninja that claimed this capsule via the Cloning ability.</summary>
    [DataField]
    public EntityUid? BoundNinja;
}
