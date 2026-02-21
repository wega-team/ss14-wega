using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Upgrades.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(ClothingUpgradeSystem))]
public sealed partial class UpgradeableClothingComponent : Component
{
    [DataField]
    public string UpgradesContainerId = "clothing_upgrades";

    [DataField]
    public EntityWhitelist Whitelist = new();

    [DataField]
    public int MaxUpgradeCount = 3;
}
