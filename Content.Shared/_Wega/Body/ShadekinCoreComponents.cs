using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ShadekinCoreComponent : Component
{
    [DataField, AutoNetworkedField]
    public ShadekinCoreMode Mode = ShadekinCoreMode.Active;

    [DataField, AutoNetworkedField]
    public string ActiveState = "core";

    [DataField, AutoNetworkedField]
    public string InactiveState = "core-burned";

    [DataField, AutoNetworkedField]
    public Color BrightColor = Color.FromHex("#999999"); // V ≈ 0.6

    [DataField, AutoNetworkedField]
    public Color BurnedColor = Color.FromHex("#4d4d4d"); // V ≈ 0.30

    [DataField, AutoNetworkedField]
    public bool IsBurned;

    [DataField, AutoNetworkedField]
    public EntityUid? OwnerBody;

}

[Serializable, NetSerializable]
public enum ShadekinCoreMode : byte
{
    Inactive,
    Active,
}
