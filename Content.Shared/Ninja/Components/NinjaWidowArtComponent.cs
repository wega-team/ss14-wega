using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Placed on the ninja suit to enable the Creeping Widow martial art.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NinjaWidowArtComponent : Component
{
    [DataField]
    public EntityUid? WearerEntity;
}
